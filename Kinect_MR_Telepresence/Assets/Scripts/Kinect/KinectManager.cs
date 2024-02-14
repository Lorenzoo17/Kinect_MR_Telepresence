using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.Azure.Kinect.Sensor;
using System;
using System.IO;
using CompressionUtilities;

public class KinectManager : MonoBehaviour
{
    private Device device;
    private DeviceConfiguration config;
    private Transformation transformation;
    private int colorWidth, colorHeight;
    public int depthWidth, depthHeight;

    [SerializeField] private GameObject kinectImage;

    //[SerializeField] private UnityEngine.UI.RawImage rawImage;
    //[SerializeField] private Texture2D colorTexture;

    [SerializeField] private int jpgCompressionQuality;

    [SerializeField] private ushort depthRange = 1000;

    /*
    [SerializeField] private UnityEngine.UI.RawImage testColorization;
    [SerializeField] private UnityEngine.UI.RawImage colorizationCompressed;
    [SerializeField] private UnityEngine.UI.RawImage reconstructedDepthGrayscale;

    [SerializeField] private UnityEngine.UI.RawImage jpgColorImage;
    [SerializeField] private UnityEngine.UI.RawImage jpgDifferenceImage;
    [SerializeField] private UnityEngine.UI.RawImage ReconstructedJpgImage;

    private List<byte[]> colorImagesList = new List<byte[]>();
    private int derivedFramesCount = 0;
    [SerializeField] private int maxDerivedFrames = 5;
    [SerializeField] private byte thresholdValues = 100;
    */

    public bool KinectAvailable(){
        return (device != null) ? true : false;
    }
    private void Awake() {
        //derivedFramesCount = maxDerivedFrames;
    }
    public GameObject GetLocalKinectImage() {
        return kinectImage;
    }
    private void Start() {

        InitializeKinect(0);

        depthWidth = device.GetCalibration().DepthCameraCalibration.ResolutionWidth;
        depthHeight = device.GetCalibration().DepthCameraCalibration.ResolutionHeight;

        colorWidth = device.GetCalibration().ColorCameraCalibration.ResolutionWidth;
        colorHeight = device.GetCalibration().ColorCameraCalibration.ResolutionHeight;

        kinectImage.GetComponent<MeshFilter>().mesh = new Mesh();

        //colorTexture = new Texture2D(colorWidth, colorHeight, TextureFormat.BGRA32, false);

    }

    private void SaveTexture(Texture2D myTexture){
        File.WriteAllBytes("text.jpg", myTexture.EncodeToJPG(30));
    }

    private void InitializeKinect(int kinectIndex = 0) {
        try {
            device = Device.Open(kinectIndex);
        }catch(AzureKinectOpenDeviceException e) {
            Debug.Log("Cannot open camera because of " + e.ToString());
        }
        config = new DeviceConfiguration();
        config.CameraFPS = FPS.FPS30;
        config.DepthMode = DepthMode.NFOV_Unbinned;
        config.ColorFormat = ImageFormat.ColorBGRA32;
        config.ColorResolution = ColorResolution.R720p;
        config.SynchronizedImagesOnly = true;

        try {
            device.StartCameras(config);

            transformation = device.GetCalibration().CreateTransformation();
        }
        catch (Exception e) {
            Debug.Log("Cannot open kinect device");
        }
    }

    public byte[] GetRawCalibration() {
        return device.GetRawCalibration();
    }

    public (byte[], byte[]) GetCaptureDepthCompressed() {
        try {
            using (Capture capture = device.GetCapture()) {
                Image depthImage = capture.Depth;

                byte[] rawDepth = depthImage.Memory.ToArray();

                short[] rawShort = new short[(int)Math.Ceiling((double)rawDepth.Length / 2)];
                Buffer.BlockCopy(rawDepth, 0, rawShort, 0, rawDepth.Length);

                for(int i = 0; i < rawShort.Length; i++) {
                    if(rawShort[i] > depthRange) {
                        rawShort[i] = 0;
                    }
                }

                Buffer.BlockCopy(rawShort, 0, rawDepth, 0, rawShort.Length * 2);

                byte[] colorBytes = transformation.ColorImageToDepthCamera(capture).Memory.ToArray();

                colorBytes = CompressColorsByteToJPG(colorBytes); //Compressione colori in jpeg

                kinectImage.GetComponent<KinectImage>().SetMeshGivenDepthAndColorCompressed(rawDepth, colorBytes);

                return (rawDepth, colorBytes);
            }
        }catch(AzureKinectException e) {
            InfoWindow.Instance.SpawnWindow("Azure Kinect not recognized, restart the application, reconnect the device and try again", 5f);
            return (null, null);
        }
    }

    public short[] GetCaptureDepthShort() {
        using (Capture capture = device.GetCapture()) {
            Image depthImage = capture.Depth;

            short[] rawDepth = depthImage.GetPixels<short>().ToArray();

            return rawDepth;
        }
    }
    private byte[] CompressColorsByteToJPG(byte[] colorByteArray) { //Per comprimere in JPG dai colori grezzi ottengo una texture, dalla quale poi ottengo byte in JPG

        Texture2D texture = new Texture2D(depthWidth, depthHeight, TextureFormat.RGBA32, false);
        texture.LoadRawTextureData(colorByteArray);
        texture.Apply();

        byte[] compressed = ImageConversion.EncodeToJPG(texture, jpgCompressionQuality); //Quality --> 1 (bassa) - 100 (alta) ==> 75 default ; 50 impercettibile ; 25 si nota ma buono per diminuzione dimensione

        Destroy(texture);

        return compressed;
    }

    private void OnDestroy() {
        if(device != null) {
            device.StopCameras();
            device.Dispose();
        }
    }
}
