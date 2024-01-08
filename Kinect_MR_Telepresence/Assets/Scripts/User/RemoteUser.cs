using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RemoteUser : MonoBehaviour
{
    public int clientId;
    [SerializeField] private TextMeshProUGUI idText;

    private Vector3 desiredPosition;
    private Quaternion desiredRotation;

    [SerializeField] private float lerpPositionTime;
    [SerializeField] private float lerpRotationTime;
    
    public void SetId(int id){
        clientId = id;
        if(idText != null){
            idText.text = $"User {id}";
        }
    }
    public int GetId(){
        return clientId;
    }
    public void SetPosition(Vector3 newPosition){
        desiredPosition = newPosition;
    }
    public void SetRotation(Quaternion newRotation){
        desiredRotation = newRotation;
    }

    private void Update() {
        if(desiredPosition != null){ //Se la nuova posizione è disponibile 
            transform.position = Vector3.Lerp(transform.position, desiredPosition, lerpPositionTime); //Effettuo un'interpolazione lineare tra posizione attuale e nuova posizione
        }

        if(desiredRotation != null){
            transform.localRotation = Quaternion.Lerp(transform.localRotation, desiredRotation, lerpRotationTime); //Effettuo interpolazione lineare tra rotazione attuale e nuova rotazione
        }
    }
}
