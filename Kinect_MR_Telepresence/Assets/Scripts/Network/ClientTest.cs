using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;

using Extension;
using Microsoft.Azure.Kinect.Sensor;
using TMPro;
using CompressionUtilities;
using JetBrains.Annotations;

public class ClientTest : MonoBehaviour
{
    private NetManager client; //NetManager --> classe per effettuare operazioni di rete (start connessione, ricezione eventi di rete, connessione ad altro peer ecc...)
    private NetPeer serverPeer; //Riferimento al server (in modo da avere informazioni su ID, EndPoint ecc... di quest'ultimo a partire dalla connessione con esso)
    private EventBasedNetListener listener; //Utilizzato per gestire eventi di rete
    [SerializeField] private int hostPort = 45632; //Porta dalla quale il server è in ascolto 
    //[SerializeField] private string hostName = "172.20.10.6"; //Si indica host name o indirizzo del server di connessione
    [SerializeField] private string hostName = "localhost";
    [SerializeField] private string connectionKey = "key";
    [SerializeField] private GameObject remoteClientPrefab; //Prefab del client da spawnare quando un nuovo peer si connete al server

    private bool canSend;
    [SerializeField] private float sendRate = 0.1f;
    private float currSendTime;

    //Per kinect
    [SerializeField] private KinectManager kinectManager;
    [SerializeField] private KinectImage kinectImage;
    private bool hasSentCalibration;
    private Transformation tempTransformation;
    
    //Per audio
    [SerializeField] private AudioSource destSource; //Audio Source per riprodurre l'audio in arrivo dagli altri client connessi

    public void StartClient(string desiredHost){
        listener = new(); //istanziazion di oggetto listener
        client = new(listener); //Le operazioni di rete fanno riferimento agli eventi del listener specificato
    
        client.Start(); //Client si mette in ascolto da una porta qualsiasi che trova disponibile e dall'IP del terminale dal quale si connette
        Debug.Log($"Client start listening from port {client.LocalPort}");

        client.Connect(desiredHost, hostPort, connectionKey);

        //Listener per connessione con peer (server)
        listener.PeerConnectedEvent += (peer) =>{
            serverPeer = peer; //Alla connessione con un peer (server) si salva per avere sempre informazioni su di lui
            Debug.Log("Client connected to " + serverPeer.EndPoint.Address + ", " + serverPeer.EndPoint.Port);
        };

        //Listener per ricezione di informazioni in rete
        listener.NetworkReceiveEvent += (peer, reader, channel, deliveryMethod) => {
            NetworkDataType packetTypeReceived = (NetworkDataType)reader.GetInt(); //Per capire il tipo di pacchetto ricevutos

            if(packetTypeReceived == NetworkDataType.PingPacket){
                int dimension = reader.GetInt();
                byte[] dataReceived = new byte[dimension];

                reader.GetBytes(dataReceived, dimension);

                Debug.Log($"Client has received ping data from server of lenght {dimension}");
            }
            else if(packetTypeReceived == NetworkDataType.PositionPacket){
                Vector3 positionOfRemoteClient = reader.GetVector3(); //Nuova posizione
                int clientToMoveId = reader.GetInt(); //ID del client che ha cambiato posizione
                Debug.Log($"Necessary to move client with ID {clientToMoveId} in position ({positionOfRemoteClient.x}, {positionOfRemoteClient.y}, {positionOfRemoteClient.z}");

                //Spostare il client presente nella scena locale nella posizione indicata, ciclando fra tutti quelli presenti (che hanno script
                //ClientRemoteView) il cui ID coincide con clientToMoveId
                UpdateRemoteClient(clientToMoveId, positionOfRemoteClient, Quaternion.identity);
            }
            else if(packetTypeReceived == NetworkDataType.PositionRotationPacket){
                Vector3 positionRemoteClient = reader.GetVector3();
                Quaternion rotationRemoteClient = reader.GetQuaternion();
                int clientToMoveId = reader.GetInt();
                Debug.Log($"Necessary to move client with ID {clientToMoveId} in position ({positionRemoteClient.x}, {positionRemoteClient.y}, {positionRemoteClient.z} with certain rotation");

                //Aggiornamento posizione e rotazione
                UpdateRemoteClient(clientToMoveId, positionRemoteClient, rotationRemoteClient);
            }
            else if(packetTypeReceived == NetworkDataType.UserSpawnPacket){ //Si riceve il pacchetto che indica di spawnare il nuovo client connesso
                RemoteUser[] remoteUsers = FindObjectsOfType<RemoteUser>(); //Si controlla se ci sono altri remoteClient locali

                int idToSpawn = reader.GetInt();
                bool alreadySpawned = false;

                if(remoteUsers.Length > 0){ //Se ci sono altri client, si controlla se il nuovo è già presente o meno
                    foreach(RemoteUser user in remoteUsers){
                        if(user.GetId() == idToSpawn){
                            alreadySpawned = true;
                            Debug.Log($"Client {idToSpawn} already spawned here");
                            break;
                        }
                    }
                }

                if(!alreadySpawned){ //Se non è presente si spawna il nuovo client
                    Vector3 randomPos = new Vector3(UnityEngine.Random.Range(-5f, 5f), 0f, UnityEngine.Random.Range(-5f, 5f));

                    GameObject newUser = Instantiate(remoteClientPrefab, randomPos, Quaternion.identity);
                    newUser.GetComponent<RemoteUser>().SetId(idToSpawn);
                    Debug.Log($"Spawned client {idToSpawn}");

                    //Si spawna anche il realSenseImage per quel client
                    /*
                    float posSpawnMultiplier = 3f;
                    GameObject newRsImage = Instantiate(rsImageClientPrefab, new Vector3(idToSpawn * posSpawnMultiplier, 1f, 1f), Quaternion.identity);
                    newRsImage.transform.localRotation = Quaternion.Euler(0f, 0f, 180f); //Ruoto di 180 rispetto ad asse z
                    newRsImage.GetComponent<RealSenseImage>().SetRsImageClientID(idToSpawn); //Si assegna al realsense image lo stesso id del client
                    newRsImage.GetComponent<NetworkItem>().SetNetworkID(idToSpawn); //Vado anche ad aggiornare il networkID con lo stesso ID del client
                    Debug.Log($"Spawned rs image for client {newRsImage.GetComponent<RealSenseImage>().GetRsImageClientID()}");
                    */
                }
            }
            else if(packetTypeReceived == NetworkDataType.UserDespawnPacket){
                RemoteUser[] remoteUsers = FindObjectsOfType<RemoteUser>(); //Si controlla se ci sono altri remoteClient locali

                int clientIdToDespawn = reader.GetInt();

                if(remoteUsers.Length > 0){
                    foreach(RemoteUser user in remoteUsers){
                        if(user.GetId() == clientIdToDespawn){
                            Destroy(user.gameObject);
                            Debug.Log($"Client {clientIdToDespawn} despawned");
                        }
                    }
                }

                //Si despawna anche il realsense image
                /*
                RealSenseImage[] rsImages = FindObjectsOfType<RealSenseImage>();

                if(rsImages.Length > 0) {
                    foreach(RealSenseImage rsImage in rsImages) {
                        if(rsImage.GetRsImageClientID() == clientIdToDespawn) {
                            Destroy(rsImage.gameObject);
                            Debug.Log($"Also rsImage for client {clientIdToDespawn} despawned");
                        }
                    }
                }
                */
            }
            else if(packetTypeReceived == NetworkDataType.CalibrationPacket){
                int calibrationLength = reader.GetInt(); //Reading calibration lenght
                byte[] rawCalibration = new byte[calibrationLength];
                reader.GetBytes(rawCalibration, calibrationLength); //Reading calibration rawData
                int width = reader.GetInt(); //Reading width
                int height = reader.GetInt(); //Reading height
                OnReceiveCalibration(rawCalibration, width, height);
            }
            else if(packetTypeReceived == NetworkDataType.DepthColorPacket){
                int lenght = reader.GetInt();
                byte[] rawDepth = new byte[lenght];
                reader.GetBytes(rawDepth, lenght);

                int colorLenght = reader.GetInt();
                byte[] rawColor = new byte[colorLenght];
                reader.GetBytes(rawColor, colorLenght);

                //Debug.Log($"Compressed received data {rawDepth.Length + rawColor.Length}");
                //decompressione dei dati ricevuti
                rawDepth = DataCompression.DeflateDecompress(rawDepth);
                rawColor = DataCompression.DeflateDecompress(rawColor);

                OnReceiveDepthAndColorData(rawDepth, rawColor);
            }
            else if(packetTypeReceived == NetworkDataType.ItemStatePacket){
                int itemID = reader.GetInt(); //ID dell'oggetto da aggiornare
                Vector3 newPosition = reader.GetVector3(); //Lettura posizione
                Quaternion newRotation = reader.GetQuaternion(); //Lettura rotazione

                NetworkItem[] networkItems = FindObjectsOfType<NetworkItem>(); //Si ricavano tutti gli oggetti di rete presenti localmente che possono essere oggetti di aggiornamento

                if(networkItems.Length > 0){ 
                    foreach(NetworkItem item in networkItems){
                        if(item.GetNetworkID() == itemID){ //Si cerca l'oggetto con l'ID uguale a quello da aggiornare ricevuto in rete
                            item.SetPositionRemotely(newPosition, newRotation); //Si imposta la posizione e rotazione
                            break;
                        }
                    }
                }
            }
            else if(packetTypeReceived == NetworkDataType.ItemOwnership){ //Per aggiornamenti su l'ownership dell'oggetto in rete
                int itemID = reader.GetInt();
                bool isBeingManipulated = reader.GetBool(); //Booleano che indica se l'oggetto di rete sta venendo o meno manipolato da un client connesso

                NetworkItem[] networkItems = FindObjectsOfType<NetworkItem>();

                if(!isBeingManipulated)
                    Debug.Log("Can move item " + itemID);
                else
                    Debug.Log("Can not move item " + itemID);

                if(networkItems.Length > 0){
                    foreach(NetworkItem item in networkItems){
                        if(item.GetNetworkID() == itemID){ //Se si trova l'oggetto
                            item.isBeingManipulated = isBeingManipulated; //Si assegna lo stato della variabile, in modo da permettere o meno la manipolazione da parte dell'utente locale
                            break;
                        }
                    }
                }
            }
            else if(packetTypeReceived == NetworkDataType.AudioPacket){
                Debug.Log("Client audio received");
                int lenght = reader.GetInt(); //Si legge la lunghezza della stringa di byte dell'audio compresso 
                byte[] compressedAudio = new byte[lenght];
                reader.GetBytes(compressedAudio, lenght); //si legge l'array di byte contenente l'informazione audio compressa

                Debug.Log("Compressed audio lenght : " + compressedAudio.Length);

                int channels = reader.GetInt(); //si ricavano i canali 

                //decompressione
                byte[] rawAudio = DataCompression.DeflateDecompress(compressedAudio); //decompressione dell'audio ricevuto 

                float[] samples = DataCompression.ToFloatArray(rawAudio); //si passa da byte[] a float[] in modo da poter ricostruire l'audioclip
                destSource.clip = AudioClip.Create("test", samples.Length, channels, TestAudio.FREQUENCY, false); //si assegna all'audioSource specificata una nuova clip la cui frequenza e numero di canali deve coincidere con quella specificata in TestAudio
                destSource.clip.SetData(samples, 0); // si assegnano ii sample alla clip creata
                if(!destSource.isPlaying) destSource.Play(); //si fa partire l'audio
            }
        };
    }

    private void Update(){
        if(client != null && client.IsRunning){
            client.PollEvents(); //Inizio ricezione di eventi di rete
        }


        //Per ping test
        if(Input.GetKeyDown(KeyCode.K)){
            canSend = !canSend;
            if(!hasSentCalibration){
                SendCalibration();
                hasSentCalibration = true;
            }
        }

        if(canSend && serverPeer != null){
            if(currSendTime <= 0){
                //SendPing(5000);
                //SendDepthAndColor();
                SendDepthAndColor();

                /*
                SendDepthAndColorRealSense(); //Invio dati depht e color
                faceDetectionTexture.ExecuteBlit(); //Computazione faceDetection (texture)
                faceDetection.ExecuteDetection(); //Computazione faceDetection (qui si distrugge anche la texture in SendDepthAndColorRealSense() per liberare memoria)
                */
                currSendTime = sendRate;
            }else{
                currSendTime -= Time.deltaTime;
            }
        }

        /*
        if(Input.GetKeyDown(KeyCode.P)){
            SendPosition(new Vector3(10, 15, 5));
        }
        */
    }

    //--SCANSIONE DA ALTRO CODICE
    public bool ClientSendScan(){
        if(serverPeer != null){
            canSend = !canSend;
            if(!hasSentCalibration){
                SendCalibration();
                hasSentCalibration = true;
            }
        }

        return canSend;
    }

    //INVIO PACCHETTO GENERICO
    private void SendData(NetDataWriter writer, DeliveryMethod deliveryMethod){
        if(serverPeer != null){
            serverPeer.Send(writer, deliveryMethod);
            Debug.Log("Client has just sent data to server");
            //Debug.Log("With ping : " + serverPeer.Ping.ToString());
        }
        else{
            Debug.Log("No server available");
        }
    }

    //METODI DI INVIO PACCHETTI 

    //Pacchetto Ping (per testing)
    public void SendPing(int packetDimension = 1000){ //Per trasmissione di prova, la dimensione fa riferimento alla grandezza in bytes
        NetworkDataType packetType = NetworkDataType.PingPacket; //Definizione del tipo di pacchetto da inviares

        int lenght = packetDimension; //Si definisce la lunghezza, che è necessario inviare separatamente all'informazione, per far capire al reader quanto leggere
        byte[] dataToSend = new byte[lenght]; //Dato da trasmettere

        NetDataWriter writer = new();

        //Si riempie il writer con le singole informazioni, che dovranno essere lette nello stesso ordine con cui sono state inviate
        writer.Put((int)packetType);
        writer.Put(lenght);
        writer.Put(dataToSend);

        SendData(writer, DeliveryMethod.ReliableUnordered); //Trasmissione dati al server
        Debug.Log($"Client has sent {lenght} data to server");
    }

    //Pacchetto trasmissione posizione, da richiamare nello script del player locale per poter essere utilizzato, non si trasmette l'id del client, esso verrà trasmesso agli altri client connessi quando questi dati arriveranno al server
    public void SendPosition(Vector3 position){
        NetworkDataType packetType = NetworkDataType.PositionPacket;

        NetDataWriter writer = new();
        writer.Put((int)packetType);
        writer.Put(position);

        SendData(writer, DeliveryMethod.ReliableUnordered);
        Debug.Log("Sent position to server");
    }

    //Pacchetto per trasmissione posizione e rotazione
    public void SendPositionAndRotation(Vector3 position, Quaternion rotation){
        NetworkDataType packetType = NetworkDataType.PositionRotationPacket; //Imposto il tipo di pacchetto 

        NetDataWriter writer = new();
        writer.Put((int)packetType);
        writer.Put(position); //Metto posizione
        writer.Put(rotation); //Metto rotazione

        SendData(writer, DeliveryMethod.ReliableUnordered); //Trasmetto posizione e rotazione
    }

    //---PACCHETTI GESTIONE KINECT

    //Pacchetto trasmissione calibrazione del kinect
    private void SendCalibration() {
        byte[] rawCalibration = kinectManager.GetRawCalibration();

        NetworkDataType packetType = NetworkDataType.CalibrationPacket;
        int calibrationLength = rawCalibration.Length;
        int depthWidth = kinectManager.depthWidth;
        int depthHeight = kinectManager.depthHeight;

        NetDataWriter writer = new();
        //Loading packets in order, THE SAME ORDER MUST BE FOLLOWED IN SERVER IN ORDER TO READ DATA CORRECTLY
        writer.Put((int)packetType);
        writer.Put(calibrationLength);
        writer.Put(rawCalibration);
        writer.Put(depthWidth);
        writer.Put(depthHeight);
        SendData(writer, DeliveryMethod.ReliableUnordered);
    }

    //Pacchetto trasmissione dati depth camera e color camera
    private void SendDepthAndColor() {
        byte[] rawDepth, rawColor;
        (rawDepth, rawColor) = kinectManager.GetCaptureDepthCompressed();
        //(rawDepth, rawColor) = kinectManager.GetCaptureDepthOptimized(); //

        //compressione dati da inviare (necessario decomprimerli lato server)
        rawDepth = DataCompression.DeflateCompress(rawDepth);
        rawColor = DataCompression.DeflateCompress(rawColor);

        Debug.Log("Depth deflated : " + rawDepth.Length + "\nColor deflated : " + rawColor.Length);

        NetworkDataType packetType = NetworkDataType.DepthColorPacket;
        int rawDepthLenght = rawDepth.Length;
        NetDataWriter writer = new();

        writer.Put((int)packetType);
        writer.Put(rawDepthLenght);
        writer.Put(rawDepth);

        int rawColorLenght = rawColor.Length;
        writer.Put(rawColorLenght);
        writer.Put(rawColor);

        SendData(writer, DeliveryMethod.ReliableUnordered);
    }

    //---METODI GESTIONE UTENTI IN REMOTO E IN LOCALE

    //Aggiornamento posizione e rotazione del client che ha cambiato posizione e inviato quest'ultima al server
    private void UpdateRemoteClient(int clientId, Vector3 newPosition, Quaternion newRotation){
        RemoteUser[] users = FindObjectsOfType<RemoteUser>();

        if(users.Length > 0){
            foreach(RemoteUser user in users){
                if(user.GetId() == clientId){ //Se tra i remoteClient presenti si trova quello che ha cambiato posizione o rotazione, si aggiornano queste ulime
                    //user.transform.position = newPosition;
                    user.SetPosition(newPosition); //Imposto nuova posizione per il remoteClient
                    //user.transform.localRotation = newRotation;
                    user.SetRotation(newRotation); //Imposto nuova rotazione per il remote client
                    break;
                }
            }
        }
    }

    //---METODI GESTIONE OGGETTI IN REMOTO E LOCALE

    public void UpdateRemoteItem(int itemToUpdateID, Vector3 newPosition, Quaternion newRotation){
        NetworkDataType packetType = NetworkDataType.ItemStatePacket;

        NetDataWriter writer = new();
        writer.Put((int)packetType);
        writer.Put(itemToUpdateID);
        writer.Put(newPosition);
        writer.Put(newRotation);

        SendData(writer, DeliveryMethod.ReliableUnordered);
    }

    public void SendItemOwnership(int itemID, bool ownership){
        NetworkDataType packetType = NetworkDataType.ItemOwnership;

        NetDataWriter writer = new();
        writer.Put((int)packetType);
        writer.Put(itemID);
        writer.Put(ownership);

        SendData(writer, DeliveryMethod.ReliableUnordered);
    }

    //---METODI GESTIONE KINECT
    private void OnReceiveDepthAndColorData(byte[] rawDepth, byte[] rawColor) { //Quando si riceve un pacchetto relativo a kinect 
        Debug.Log($"Client : received depth and color data decompressed : {rawDepth.Length + rawColor.Length} bytes");
        kinectImage.SetMeshGivenDepthAndColorCompressed(rawDepth, rawColor); //Si imposta il mesh del kinectImage locale con le informazioni di profondità e colori ricevute via rete
    }

    private void OnReceiveCalibration(byte[] rawCalibration, int depthWidth, int depthHeight) {
        tempTransformation = Calibration.GetFromRaw(rawCalibration, DepthMode.NFOV_2x2Binned, ColorResolution.R720p).CreateTransformation(); //For now let's keep static DepthMode and static ColorResolution

        kinectImage.SetTransformation(tempTransformation, depthWidth, depthHeight);
        Debug.Log($"Transformation : {tempTransformation.ToString()} received, width {depthWidth}, height {depthHeight}");
    }

    //---METODI TRASMISSIONE AUDIO

    public void SendAudioRaw(float[] samples, int channels){
        byte[] rawSamples = DataCompression.ToByteArray(samples); //Si convertono i samples dell'audioclip da inviare da float[] a byte[]

        byte[] compressedSamples = DataCompression.DeflateCompress(rawSamples); //Si effettua la compressione dei dati mediante Deflate

        int rawLenght = rawSamples.Length;
        int compressedLenght = compressedSamples.Length;

        if(compressedLenght > 0){
            Debug.Log("Raw lenght : " + rawLenght);
            Debug.Log("Compressed lenght : " + compressedLenght);

            NetDataWriter writer = new();

            writer.Put((int)NetworkDataType.AudioPacket);
            writer.Put(compressedLenght);
            writer.Put(compressedSamples);
            writer.Put(channels);

            SendData(writer, DeliveryMethod.ReliableUnordered); //Si trasmette il campione audio 
        }
    }

    //---- METODI PER ACCESSO A FUNZIONALITà DALL'ESTERNO ----
    public bool IsClientConnected(){ //Per far capire all'utente in locale che può iniziare la trasmissione della posizione
        return client != null && client.IsRunning && serverPeer != null;
    }

    public void DisconnectLocalClient(){
        if(client != null && client.IsRunning){
            client.Stop();
        }
    }
    private void OnDestroy() {
        if(client != null && this.client.IsRunning){
            client.Stop(); //Si ferma l'operazione di ascolto del client dalla porta scelta
        }
    }
}
