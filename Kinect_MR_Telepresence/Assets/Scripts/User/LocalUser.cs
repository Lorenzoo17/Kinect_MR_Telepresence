using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalUser : MonoBehaviour
{
    private Transform sceneCamera;
    [SerializeField] private Vector3 userOffset;
    [SerializeField] private ClientTest localClient;
    //public int userId; //Non serve qui, si può ricavare lato server direttamente

    [SerializeField] private float positionForwardingRate;
    private float currentPositionForwardingTime;

    private void Start() {
        sceneCamera = Camera.main.transform;
        if(FindObjectOfType<ClientTest>() != null){
            localClient = FindObjectOfType<ClientTest>();
        }
    }

    private void Update() {
        if(transform.position != sceneCamera.position || transform.localRotation != sceneCamera.localRotation){ //In modo da trasmettere le informazioni solo al cambiamento di posizione e/o rotazione
            transform.position = sceneCamera.position - userOffset;
            transform.localRotation = sceneCamera.localRotation;
            //Trasmettere ogni tot secondi all'oggetto clientTest presente nella scena la sua posizione affinche venga trasmessa agli altri client connessi tramite server

            if(localClient != null && localClient.IsClientConnected()){ //Se il client è connesso
                SharePosition();
            }
        }
    }

    private void SharePosition(){
        if(currentPositionForwardingTime <= 0){
            //SendPositionToServer();
            SendPositionRotationToServer();
            currentPositionForwardingTime = positionForwardingRate;
        }else{
            currentPositionForwardingTime -= Time.deltaTime;
        }
    }

    public void SendPositionRotationToServer(){
        localClient.SendPositionAndRotation(transform.position, transform.localRotation); //Accedo al client locale e tramite quest'ultimo trasmetto la posizione e rotazione al server
    }

    public void SendPositionToServer(){
        localClient.SendPosition(transform.position);
    }
}
