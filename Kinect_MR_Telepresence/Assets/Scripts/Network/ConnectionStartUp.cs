using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ConnectionStartUp : MonoBehaviour
{
    [SerializeField] private ServerTest server;
    [SerializeField] private ClientTest client;
    [SerializeField] private TMP_InputField ipField;
    [SerializeField] private GameObject ipFieldWindow;
    private Transform connectionWindow;
    [SerializeField] private GameObject mrtkWindow;

    /*
    //Link : https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk3-input/packages/input/system-keyboard
    
    private TouchScreenKeyboard keyboard; //per tastiera MRTK
    private TextMeshProUGUI textComponent;
    public void OpenSystemKeyboard() {
        keyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default, false, false, false, false);
    }

    private void ReadInput(){ //In update
        if(keyboard != null){
            textComponent.text = keyboard.text;
        }
    }
    */
    private void Start() {
        connectionWindow = transform.Find("ConnectionWindow").transform; //Lo script è assegnato al canvas, quindi connectionWindow è direttamente un suo child
    }

    public void StartAsHost(){
        server.StartServer();
        TurnWindows(false);
        //StartAsClient(); //Per fare host
    }

    public void OpenClientConnection(){
        ipFieldWindow.SetActive(true);
        connectionWindow.gameObject.SetActive(false);
    }

    public void StartAsClient(){
        Debug.Log(ipField.text);
        if(ipField.text != "")
            client.StartClient(ipField.text);
        else
            client.StartClient("localhost");
        ipFieldWindow.SetActive(false);
        if(mrtkWindow != null){
            mrtkWindow.SetActive(false);
        }
    }

    public void DisconnectClient() { 
        client.DisconnectLocalClient(); //Per disconnettere il client della scena, al fine di riconnettersi al server
        TurnWindows(true);
    }
        
    public void DisconnectServer(){
        server.DisconnectServer();
        TurnWindows(true);
    }

    private void TurnWindows(bool state){
        connectionWindow.gameObject.SetActive(state);
        if(mrtkWindow != null){
            mrtkWindow.SetActive(state);
        }
    }
}
