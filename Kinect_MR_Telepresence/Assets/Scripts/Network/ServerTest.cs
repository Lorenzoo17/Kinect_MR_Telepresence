using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Azure.Kinect.Sensor;
using Intel.RealSense;

using Extension;
using CompressionUtilities;

public class ServerTest : MonoBehaviour
{
    private NetManager server;
    private EventBasedNetListener listener;

    [SerializeField] private int port = 45632;
    [SerializeField] private string connectionKey = "key";
    [SerializeField] private int maxNumberOfClients = 10;

    private List<NetPeer> peersConnected = new List<NetPeer>(); //Lista dei client attualmente connessi alla rete
    [SerializeField] private bool pingToAllClients = true; //Se true quando il server riceve un ping lo inoltra a tutti i client connessi

    //[SerializeField] private GameObject remoteClientPrefab; //prefab di client da spawnare quando un client si connette

    //Per kinect
    private Transformation tempTransformation;
    [SerializeField] private KinectImage kinectImage;
    [SerializeField] private AudioSource destSource;

    //Per RealSense
    [SerializeField] private RealSenseManager rsManager;
    [SerializeField] private RealSenseImage rsImage;

    private byte[] rawCalibrationSaved = null;
    private int depthWidht, depthHeight;
    public void StartServer(){
        listener = new();
        server = new(listener);

        server.Start(port);
        Debug.Log("Server start listening from port : " + port);


        //Listener per gestire richieste di connessione
        listener.ConnectionRequestEvent += (request) =>{
            if(server.ConnectedPeersCount < maxNumberOfClients){
                request.AcceptIfKey(connectionKey);
            }else{
                request.Reject();
                Debug.Log("Connection rejected because the client's connection limit has been reached");
            }
        };

        //Listener per quando un peer (client in questo caso) si connette
        listener.PeerConnectedEvent += (clientPeer) => {
            Debug.Log($"Client {clientPeer.Id} connected");
            peersConnected.Add(clientPeer); //Si aggiunge il client appenna connesso alla lista, in modo da avere un riferimento ai client connessi

            if(rawCalibrationSaved != null){ //Se la calibrazione è già stata impostata (quindi un nuovo client si connette mentre la scansione sta già venendo trasmessa)
                NetDataWriter calibrationWriter = new();
                calibrationWriter.Put((int)NetworkDataType.CalibrationPacket);
                calibrationWriter.Put(rawCalibrationSaved.Length);
                calibrationWriter.Put(rawCalibrationSaved);
                calibrationWriter.Put(depthWidht);
                calibrationWriter.Put(depthHeight);

                SendData(clientPeer, calibrationWriter, DeliveryMethod.ReliableUnordered); //Si invia al nuovo client la calibrazione, affinche possa ricevere correttamente la scansione
            }

            SpawnPeerClientSide(clientPeer); //Per tutti i client si spawna il nuovo
            SpanwPeersInClient(clientPeer); //Si spawnano i client già connessi per il nuovo client
        };

        //Listener per quando un client si disconnette
        listener.PeerDisconnectedEvent += (clientPeer, disconnectiondInfo) => {
            Debug.Log($"Client {clientPeer.Id} disconnected from server for : {disconnectiondInfo.Reason}");
            peersConnected.Remove(clientPeer); //Si rimuove il client dalla lista quando si disconnette

            DespawnPeerClientSide(clientPeer); //Si rimuove il client anche per gli altri client connessi
        };

        //Listener per ricezione di informazioni in rete
        listener.NetworkReceiveEvent += (peer, reader, channel, deliveryMethod) => {
            NetworkDataType packetTypeReceived = (NetworkDataType)reader.GetInt(); //Per capire il tipo di pacchetto ricevutos

            if(packetTypeReceived == NetworkDataType.PingPacket){
                int dimension = reader.GetInt();
                byte[] dataReceived = new byte[dimension];

                reader.GetBytes(dataReceived, dimension);

                Debug.Log($"Server has received ping data from client n° {peer.Id} of lenght {dimension}");

                SendPingToClients(peer, packetTypeReceived, dimension, dataReceived); //Si inoltra il pacchetto ricevuto agli altri client connessi
            }else if(packetTypeReceived == NetworkDataType.PositionPacket){
                Vector3 positionToForward = reader.GetVector3();
                NetPeer peerToShare = peer;

                ForwardClientPosition(packetTypeReceived, peerToShare, positionToForward);
            }
            else if(packetTypeReceived == NetworkDataType.PositionRotationPacket){ //Quando arriva pacchetto da parte di un client contenente la sua posizione e rotazione
                Vector3 positionToForward = reader.GetVector3();
                Quaternion rotationToForward = reader.GetQuaternion();
                NetPeer peerToShare = peer;

                ForwardClientPositionAndRotation(packetTypeReceived, peerToShare, positionToForward, rotationToForward); //Si inoltrano queste informazioni agli altri client connessi
            }
            else if(packetTypeReceived == NetworkDataType.CalibrationPacket){
                int calibrationLength = reader.GetInt(); //Reading calibration lenght
                byte[] rawCalibration = new byte[calibrationLength];
                reader.GetBytes(rawCalibration, calibrationLength); //Reading calibration rawData
                int width = reader.GetInt(); //Reading width
                int height = reader.GetInt(); //Reading height
                OnReceiveCalibration(rawCalibration, width, height);

                rawCalibrationSaved = rawCalibration;
                depthWidht = width;
                depthHeight = height;

                NetDataWriter calibrationWriter = new();
                calibrationWriter.Put((int)NetworkDataType.CalibrationPacket);
                calibrationWriter.Put(calibrationLength);
                calibrationWriter.Put(rawCalibration);
                calibrationWriter.Put(width);
                calibrationWriter.Put(height);

                foreach(NetPeer client in peersConnected){
                    if(client.Id != peer.Id)
                        SendData(client, calibrationWriter, DeliveryMethod.ReliableUnordered);
                }
            }
            else if(packetTypeReceived == NetworkDataType.DepthColorPacket){
                int lenght = reader.GetInt();
                byte[] rawDepth = new byte[lenght];
                reader.GetBytes(rawDepth, lenght);

                int colorLenght = reader.GetInt();
                byte[] rawColor = new byte[colorLenght];
                reader.GetBytes(rawColor, colorLenght);

                Debug.Log($"Compressed received data {rawDepth.Length + rawColor.Length}");

                //Invio dati compressi agli altri client
                NetDataWriter kinectWriter = new();
                kinectWriter.Put((int)NetworkDataType.DepthColorPacket);
                kinectWriter.Put(lenght);
                kinectWriter.Put(rawDepth);
                kinectWriter.Put(colorLenght);
                kinectWriter.Put(rawColor);

                foreach(NetPeer client in peersConnected){
                    if(client.Id != peer.Id)
                        SendData(client, kinectWriter, DeliveryMethod.ReliableUnordered);
                }

                //decompressione
                rawDepth = DataCompression.DeflateDecompress(rawDepth);
                rawColor = DataCompression.DeflateDecompress(rawColor);

                OnReceiveDepthAndColorData(rawDepth, rawColor);
            }
            else if(packetTypeReceived == NetworkDataType.ItemStatePacket){
                int itemId = reader.GetInt();
                Vector3 position = reader.GetVector3();
                Quaternion rotation = reader.GetQuaternion();

                Debug.Log($"Necessary to move object {itemId} to {position.x}, {position.y}, {position.z}");

                NetworkItem itemToMove = FindObjectOfType<NetworkItem>();
                if(itemToMove != null && itemToMove.GetNetworkID() == itemId){
                    itemToMove.SetPositionRemotely(position, rotation);
                }

                ForwardItemPosition(peer, itemId, position, rotation);
            }
            else if(packetTypeReceived == NetworkDataType.ItemOwnership){
                int itemID = reader.GetInt();
                bool clientOwnership = reader.GetBool();

                ForwardItemState(peer, itemID, clientOwnership);

                if(clientOwnership){
                    Debug.Log($"Clients can not move item {itemID}");
                }else{
                    Debug.Log($"Clients can move item {itemID}");
                }
            }
            else if(packetTypeReceived == NetworkDataType.AudioPacket){
                Debug.Log("Audio received");
                int lenght = reader.GetInt();
                byte[] compressedAudio = new byte[lenght];
                reader.GetBytes(compressedAudio, lenght);

                Debug.Log("Compressed audio lenght : " + compressedAudio.Length);

                int channels = reader.GetInt();

                ForwardAudioToClients(peer, compressedAudio, channels);

                //decompressione
                byte[] rawAudio = DataCompression.DeflateDecompress(compressedAudio);

                Debug.Log("Decompressed lenght : " + rawAudio.Length);

                float[] samples = DataCompression.ToFloatArray(rawAudio);
                destSource.clip = AudioClip.Create("test", samples.Length, channels, TestAudio.FREQUENCY, false);
                destSource.clip.SetData(samples, 0);
                if(!destSource.isPlaying) destSource.Play();
            }
            else if(packetTypeReceived == NetworkDataType.DepthColorRs) {
                //Debug.Log("RealSense received");

                int rawDepthLenght = reader.GetInt();
                byte[] rawDepth = new byte[rawDepthLenght];

                reader.GetBytes(rawDepth, rawDepthLenght);

                int rawColorLenght = reader.GetInt();
                byte[] rawColor = new byte[rawColorLenght];

                reader.GetBytes(rawColor, rawColorLenght);

                Intel.RealSense.Intrinsics intrinsics = reader.GetIntrinsics();

                Vector2 bb_center = reader.GetVector2();
                int faceArea = reader.GetInt();

                Debug.Log($"Bytes received : depth {rawDepth.Length} , color {rawColor.Length}");

                int clientSenderID = peer.Id;

                //Invio dati compressi agli altri client
                NetDataWriter rsWriter = new();
                rsWriter.Put((int)NetworkDataType.DepthColorRs);
                rsWriter.Put(rawDepthLenght);
                rsWriter.Put(rawDepth);
                rsWriter.Put(rawColorLenght);
                rsWriter.Put(rawColor);
                rsWriter.Put(intrinsics);
                rsWriter.Put(bb_center);
                rsWriter.Put(faceArea);
                rsWriter.Put(clientSenderID); //Si trasmette anche l'id del client per aggiornare il corretto rsImage

                foreach (NetPeer client in peersConnected) {
                    if (client.Id != peer.Id)
                        SendData(client, rsWriter, DeliveryMethod.ReliableUnordered);
                }

                //Eventuale decompressione

                rawDepth = DataCompression.DeflateDecompress(rawDepth);
                rawColor = DataCompression.DeflateDecompress(rawColor);

                OnReceiveRealSendeDepthColor(rawDepth, rawColor, intrinsics, bb_center, faceArea);
            }
        };
    }

    private void Update() {
        if(server != null && server.IsRunning){
            server.PollEvents();
        }
    }

    //INVIO PACCHETTO GENERICO
    private void SendData(NetPeer peerToSend, NetDataWriter writer, DeliveryMethod deliveryMethod){
        peerToSend.Send(writer, deliveryMethod);
        Debug.Log("Client has just sent data to server");
    }

    
    //METODI INVIO PACCHETTI
    
    //Pacchetto per ping(test)
    public void SendPingToClients(NetPeer peer, NetworkDataType packetType, int dimension, byte[] dataToSend){
        if(pingToAllClients){
            //Si crea il writer con i dati ricevuti
            NetDataWriter writer = new();
            writer.Put((int)packetType);
            writer.Put(dimension);
            writer.Put(dataToSend);

            foreach(NetPeer client in peersConnected){ //Per ogni client connesso 
                if(client.Id != peer.Id) //Se il client è diverso da quello che ha inviato questi dati al server
                    SendData(client, writer, DeliveryMethod.ReliableUnordered); //Si trasmettono i dati al server
            }

            Debug.Log("Server has just forwarded data to all clients connected");
        }
    }

    //Pacchetto per inoltrare posizione di un client agli altri
    public void ForwardClientPosition(NetworkDataType packetType, NetPeer clientToShare, Vector3 positionToForward){ //ClientToShare è il client che ha cambiato posizione, positionToForward è la sua nuova posizione
        int clientId = clientToShare.Id; //ID del client che ha cambiato posizione

        NetDataWriter writer = new();
        writer.Put((int)packetType);
        writer.Put(positionToForward);
        writer.Put(clientId);

        foreach(NetPeer client in peersConnected){ //Per ogni client connesso 
            if(client.Id != clientId) //Se il client è diverso da quello che ha inviato questi dati al server
                SendData(client, writer, DeliveryMethod.ReliableUnordered); //Si trasmettono i dati al server
        }

        Debug.Log($"Server has just forwarded position data coming from client {clientId} to all clients connected");
    }

    //Pacchetto per inoltrare posizione e rotazione
    public void ForwardClientPositionAndRotation(NetworkDataType packetType, NetPeer clientToShare, Vector3 positionToForward, Quaternion rotationToForward){ //ClientToShare è il client che ha cambiato posizione, positionToForward è la sua nuova posizione
        int clientId = clientToShare.Id; //ID del client che ha cambiato posizione

        NetDataWriter writer = new();
        writer.Put((int)packetType);
        writer.Put(positionToForward);
        writer.Put(rotationToForward);
        writer.Put(clientId);

        foreach(NetPeer client in peersConnected){ //Per ogni client connesso 
            if(client.Id != clientId) //Se il client è diverso da quello che ha inviato questi dati al server
                SendData(client, writer, DeliveryMethod.ReliableUnordered); //Si trasmettono i dati al server
        }

        Debug.Log($"Server has just forwarded position data coming from client {clientId} to all clients connected");
    }

    //Pacchetto per dire agli altri client di spawnare un prefab di un client (quello nuovo connesso)
    public void SpawnPeerClientSide(NetPeer clientToSpawn){
        NetworkDataType packetType = NetworkDataType.UserSpawnPacket;

        int clientId = clientToSpawn.Id;

        NetDataWriter writer = new();
        writer.Put((int)packetType);
        writer.Put(clientId);

        foreach(NetPeer client in peersConnected){ //Per ogni client connesso 
            if(client.Id != clientId) //Se il client è diverso da quello che ha inviato questi dati al server
                SendData(client, writer, DeliveryMethod.ReliableUnordered); //Si trasmettono i dati al server
        }
        Debug.Log($"Server sent data to spawn client n° {clientId} client-side");
    }

    //Metodo per far spawnare tutti i client già connessi localmente nel nuovo client connesso
    public void SpanwPeersInClient(NetPeer clientToUpdate){ 
        NetworkDataType packetType = NetworkDataType.UserSpawnPacket;

        NetDataWriter writer = new();
        
        foreach(NetPeer client in peersConnected){ //Si spawna ciascun client connesso lato nuovo client
            if(client.Id != clientToUpdate.Id){
                writer.Reset();

                writer.Put((int)packetType);
                writer.Put(client.Id);

                clientToUpdate.Send(writer, DeliveryMethod.ReliableUnordered);
            }
        }

        Debug.Log($"Client {clientToUpdate.Id} has been updatede");
    }

    //Metodo per far despawnare un client, lato altri client, quando si disconnette
    public void DespawnPeerClientSide(NetPeer clientToDespawn){
        NetworkDataType packetType = NetworkDataType.UserDespawnPacket;

        NetDataWriter writer = new();
        writer.Put((int)packetType);
        writer.Put(clientToDespawn.Id);

        foreach(NetPeer client in peersConnected){ //Per ogni client connesso si avvisa quale client despawnare sulla base dell'ID di quest'ultimo (assegnato lato client a script RemoteUser)
            if(client.Id != clientToDespawn.Id){
                SendData(client, writer, DeliveryMethod.ReliableUnordered);
            }
        }
        Debug.Log($"Server communicate to all clients that client {clientToDespawn.Id} disconnected");
    }


    //---METODI GESTIONE OGGETTI IN RETE

    private void ForwardItemPosition(NetPeer sender, int itemID, Vector3 itemPosition, Quaternion itemRotation){ //Si inoltra agli altri client connessi la pozione dell'oggetto di rete che è stato spostato dal client "sender"
        NetworkDataType packetType = NetworkDataType.ItemStatePacket;

        NetDataWriter writer = new();
        writer.Put((int)packetType);
        writer.Put(itemID);
        writer.Put(itemPosition);
        writer.Put(itemRotation);

        foreach(NetPeer peer in peersConnected){
            if(peer.Id != sender.Id){
                SendData(peer, writer, DeliveryMethod.ReliableUnordered);
            }
        }
    }

    private void ForwardItemState(NetPeer sender, int itemID, bool isBeingManipulated){
        NetworkDataType packetType = NetworkDataType.ItemOwnership;

        NetDataWriter writer = new();
        writer.Put((int)packetType);
        writer.Put(itemID);
        writer.Put(isBeingManipulated);

        foreach(NetPeer client in peersConnected){ //Per ogni client connesso si avvisa quale client despawnare sulla base dell'ID di quest'ultimo (assegnato lato client a script RemoteUser)
            if(client.Id != sender.Id){
                SendData(client, writer, DeliveryMethod.ReliableUnordered);
            }
        }
    }


    //---METODI GESTIONE KINECT
    private void OnReceiveDepthAndColorData(byte[] rawDepth, byte[] rawColor) {
        Debug.Log($"Received depth and color data : {rawDepth.Length + rawColor.Length} bytes");
        kinectImage.SetMeshGivenDepthAndColor(rawDepth, rawColor); //Commentare per HOST e al contempo scommentare in ConnectionStartUp
        //kinectImage.SetMeshGivenDepthAndColorOptimized(rawDepth, rawColor);
    }

    private void OnReceiveCalibration(byte[] rawCalibration, int depthWidth, int depthHeight) {
        tempTransformation = Calibration.GetFromRaw(rawCalibration, DepthMode.NFOV_2x2Binned, ColorResolution.R720p).CreateTransformation(); //For now let's keep static DepthMode and static ColorResolution

        kinectImage.SetTransformation(tempTransformation, depthWidth, depthHeight);
        Debug.Log($"Transformation : {tempTransformation.ToString()} received, width {depthWidth}, height {depthHeight}");
    }

    //---METODI GESTIONE REAL SENSE
    private void OnReceiveRealSendeDepthColor(byte[] rawDepth, byte[] rawColor, Intel.RealSense.Intrinsics intrinsics, Vector2 bb_center, int faceArea) {
        rsImage.SetMeshGivenDepthAndColor(rawDepth, rawColor, intrinsics, bb_center, faceArea);
    }

    //---METODI TRASMISSIONE AUDIO
    private void ForwardAudioToClients(NetPeer audioSender, byte[] compressedAudio, int channels){ //Per trasmettere l'informazione audio agli altri client connessi, ad eccezione di quello che ha inviato l'informazione
        NetDataWriter writer = new();

        writer.Put((int)NetworkDataType.AudioPacket);
        writer.Put(compressedAudio.Length);
        writer.Put(compressedAudio);
        writer.Put(channels);

        foreach(NetPeer peer in peersConnected){
            if(peer.Id != audioSender.Id){
                SendData(peer, writer, DeliveryMethod.ReliableUnordered);
            }
        }
    }

    //---- METODI PER ACCESSO A FUNZIONALITà DALL'ESTERNO ----
    public void DisconnectServer() {
        if(server != null && server.IsRunning){
            server.Stop();
        }
    }
    
    private void OnDestroy() {
        if(server != null && server.IsRunning){
            server.Stop();
        }
    }
}
