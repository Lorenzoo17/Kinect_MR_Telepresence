using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MixedReality.Toolkit.SpatialManipulation;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(ObjectManipulator))]
public class NetworkItem : MonoBehaviour
{
    [SerializeField] private int networkID;
    [SerializeField] private bool syncPos;
    [SerializeField] private bool syncRot;
    [SerializeField] private float sendRate;
    [SerializeField] private float lerpTime;
    private float currentSendTime;
    private Vector3 lastPosition;
    private ClientTest localClient;
    private ObjectManipulator om;
    public bool isBeingManipulated;
    public bool canManipulate;
    public int GetNetworkID(){
        return networkID;
    }

    public void SetNetworkID(int id) {
        networkID = id;
    }
    // Start is called before the first frame update
    void Start()
    {
        lastPosition = transform.position;

        localClient = FindObjectOfType<ClientTest>();

        om = GetComponent<ObjectManipulator>();

        om.firstSelectEntered.AddListener((SelectEnterEventArgs e)=>{
            if(!isBeingManipulated){ //Se gli altri client non lo stanno muovendo
                canManipulate = true;
                localClient.SendItemOwnership(networkID, canManipulate); //Si trasmette l'attuale stato della manipolazione, ovvero, se sto iniziando ad interagire con l'oggetto dico agli altri client che non possono muoverlo
                //Invia dati
            }
        });

        om.lastSelectExited.AddListener((SelectExitEventArgs e) => {
            SynchronizePosition();
            canManipulate = false;
            localClient.SendItemOwnership(networkID, canManipulate); //Appena finisco di muoverlo dico agli altri client che possono spostarlo
            //Non invia più dati
        });
    }

    // Update is called once per frame
    void Update()
    {
        if(localClient != null && !isBeingManipulated && canManipulate){ //Se posso manipolare l'oggetto, allora posso anche trasmettere la sua posizione in rete
            if(currentSendTime <= 0){
                if(syncPos) 
                    SynchronizePosition();
                currentSendTime = sendRate;
            }else{
                currentSendTime -= Time.deltaTime;
            }
        }

        om.enabled = !isBeingManipulated; //L'oggetto può essere mosso se e solo se nessun altro client lo sta muovendo a sua volta
    }

    private void SynchronizePosition() {
        if(transform.position != lastPosition){
            //lastPosition = transform.position;

            localClient.UpdateRemoteItem(networkID, transform.position, transform.localRotation);
        }
    }

    public void SetPositionRemotely(Vector3 desiredPosition, Quaternion desiredRotation){
        //controllo se sta venendo o meno modificato da altro utente
        //transform.position = Vector3.Lerp(transform.position, desiredPosition, lerpTime);
        transform.localRotation = desiredRotation;
        transform.position = desiredPosition;
    }
}
