using System.Collections;
using System.Collections.Generic;
using MixedReality.Toolkit.UX;
using UnityEngine;
using TMPro;
using MixedReality.Toolkit;
using UnityEngine.XR;

public class HandMenuManager : MonoBehaviour
{
    //SELF MENU CODE
    /*
    [Header("Menu behaviour settings")]
    [SerializeField] private GameObject sphere;
    private GameObject handMenuTest;
    private GameObject indexSphere = null;
    private GameObject thumbSphere = null;
    [SerializeField] private LayerMask whatIsEyeInteractable;
    [SerializeField] private GameObject eyeCheckCollider;
    private GameObject eyeCheckSpawned;
    private bool isPalmFacingCamera;
    */
    [Header("Button settings")]
    private PressableButton scanButton;
    private PressableButton endConnectionButton;
    [SerializeField] private TextMeshPro scanButtonIcon;
    [SerializeField] private Color32 scanInactiveColor;
    [SerializeField] private Color32 scanActiveColor;
    [SerializeField] private TextMeshPro endConnectionButtonIcon;
    [SerializeField] private ClientTest client;

    private KinectManager kinectManager;
    [SerializeField] private ConnectionStartUp connection;
    // Start is called before the first frame update
    void Start()
    {
        /*
        indexSphere = Instantiate(sphere, transform.position, Quaternion.identity);
        thumbSphere = Instantiate(sphere, transform.position, Quaternion.identity);

        eyeCheckSpawned = Instantiate(eyeCheckCollider, transform.position, Quaternion.identity);

        handMenuTest = gameObject;
        */
        kinectManager = FindObjectOfType<KinectManager>();
        SetButtons();
    }

    private void SetButtons(){
        scanButton = transform.Find("Content").Find("ScanButton").GetComponent<PressableButton>();
        endConnectionButton = transform.Find("Content").Find("EndConnectionButton").GetComponent<PressableButton>();

        scanButton.OnClicked.AddListener(()=>{
            if(kinectManager != null){
                if(kinectManager.KinectAvailable()){
                    bool isSendingData = client.ClientSendScan(); //Si fa la scansione e la si trasmette in rete ereditando il metodo di clientTest
            
                    if(isSendingData){
                        scanButtonIcon.GetComponent<FontIconSelector>().CurrentIconName = "Icon 128"; //Icona di registrazione
                    }else{
                        scanButtonIcon.GetComponent<FontIconSelector>().CurrentIconName = "Icon 53"; //Si torna all'icona iniziale
                    }
                }else{
                    Debug.Log("Kinect not connected");
                    if(FindObjectOfType<InfoWindow>() != null){
                        FindObjectOfType<InfoWindow>().SpawnWindow("Kinect device not available for connection");
                    }
                }
            }
        });

        endConnectionButton.OnClicked.AddListener(()=>{
            Debug.Log("EndConnection Button clicked");
            connection.DisconnectClient();
            connection.DisconnectServer();
        });
    }
    /*
    // Update is called once per frame
    void Update()
    {
        if(XRSubsystemHelpers.HandsAggregator != null && XRSubsystemHelpers.HandsAggregator.TryGetJoint(TrackedHandJoint.IndexTip, XRNode.RightHand, out HandJointPose index) && 
            XRSubsystemHelpers.HandsAggregator.TryGetJoint(TrackedHandJoint.ThumbTip, XRNode.RightHand, out HandJointPose thumb)) 
            {
            Vector3 indexTipPose = index.Pose.position;
            Vector3 thumbTipPose = thumb.Pose.position;

            if(indexSphere != null)
                indexSphere.transform.position = indexTipPose;
            if (thumbSphere != null)
                thumbSphere.transform.position = thumbTipPose;

            //Debug.Log(Vector3.Angle(indexTipPose, thumbTipPose));
            Debug.Log("Index radius : " + index.Pose.rotation); //Mediante questa rotation e' sicuramente possibile capire se il dito e' abbassato o meno
        }
        if (XRSubsystemHelpers.HandsAggregator != null && XRSubsystemHelpers.HandsAggregator.TryGetJoint(TrackedHandJoint.Palm, XRNode.RightHand, out HandJointPose palm)) {
            Vector3 palmPosition = palm.Pose.position;

            eyeCheckSpawned.transform.position = palmPosition;//Assegno al collider la posizione del palmo della mano
        }
        GazeRay();
        CheckPalmPose();
    }

    private void CheckPalmPose() {
        if (XRSubsystemHelpers.HandsAggregator != null && XRSubsystemHelpers.HandsAggregator.TryGetPalmFacingAway(XRNode.RightHand, out bool isFacingAway)) {
            //Debug.Log(isFacingAway ? "hidden" : "correct");
            //handMenuTest.SetActive(!isFacingAway);
            isPalmFacingCamera = !isFacingAway;
        }
    }

    private void GazeRay() {
        if(Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, 5f, whatIsEyeInteractable)) { //Raycast interagisce con eyeCheckSpawned
            //CheckPalmPose();
            handMenuTest.SetActive(isPalmFacingCamera);
        }
        else {
            if (handMenuTest.activeSelf && !isPalmFacingCamera)
                handMenuTest.SetActive(false);
        }
    }
    */
}
