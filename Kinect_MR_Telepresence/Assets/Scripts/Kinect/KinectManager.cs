using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.Azure.Kinect.Sensor;
using System;
using System.IO;

public class KinectManager : MonoBehaviour
{
    private Device device;
    private DeviceConfiguration config;
    private Transformation transformation;
    private int colorWidth, colorHeight;
    public int depthWidth, depthHeight;

    [SerializeField] private GameObject kinectImage;

    //[SerializeField] private UnityEngine.UI.RawImage rawImage;
    [SerializeField] private Texture2D colorTexture;

    public bool KinectAvailable(){
        return (device != null) ? true : false;
    }
    private void Start() {
        depthWidth = 320;
        depthHeight = 288;

        InitializeKinect(0);

        depthWidth = device.GetCalibration().DepthCameraCalibration.ResolutionWidth;
        depthHeight = device.GetCalibration().DepthCameraCalibration.ResolutionHeight;

        colorWidth = device.GetCalibration().ColorCameraCalibration.ResolutionWidth;
        colorHeight = device.GetCalibration().ColorCameraCalibration.ResolutionHeight;

        kinectImage.GetComponent<MeshFilter>().mesh = new Mesh();

        colorTexture = new Texture2D(colorWidth, colorHeight, TextureFormat.BGRA32, false);

    }

    private void Update(){
        if(Input.GetKeyDown(KeyCode.U)){
            using(Capture capture = device.GetCapture()){
                colorTexture.LoadRawTextureData(capture.Color.Memory.ToArray());
                colorTexture.Apply();
                SaveTexture(colorTexture);
                //rawImage.texture = colorTexture;
            }
        }
    }

    private void SaveTexture(Texture2D myTexture){
        File.WriteAllBytes("text.jpg", myTexture.EncodeToJPG(30));
    }

    private void InitializeKinect(int kinectIndex = 0) {
        device = Device.Open(kinectIndex);
        config = new DeviceConfiguration();
        config.CameraFPS = FPS.FPS30;
        config.DepthMode = DepthMode.NFOV_2x2Binned;
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

    public byte[] GetCaptureColors() {
        using (Capture capture = device.GetCapture()) {
            Image colorImage = transformation.ColorImageToDepthCamera(capture);

            byte[] colorsByte = colorImage.Memory.ToArray();

            return colorsByte;
        }
    }

    public (byte[], byte[]) GetCaptureDepth() {
        using(Capture capture = device.GetCapture()) {
            Image depthImage = capture.Depth;

            byte[] rawDepth = depthImage.Memory.ToArray();

            short[] rawShort = new short[(int)Math.Ceiling((double)rawDepth.Length / 2)];
            Buffer.BlockCopy(rawDepth, 0, rawShort, 0, rawDepth.Length);

            int index = 0;
            for (int i = 0; i < depthImage.HeightPixels; i++) {
                for (int j = 0; j < depthImage.WidthPixels; j++) {
                    depthImage.SetPixel<short>(i, j, rawShort[index]); //Si assegnano i pixel dell'immagine depth sulla base dei dati grezzi trasmessi
                    index++;
                }
            }

            Image xyzImage = transformation.DepthImageToPointCloud(depthImage);
            BGRA[] colorArray = transformation.ColorImageToDepthCamera(capture).GetPixels<BGRA>().ToArray();
            byte[] colorBytes = transformation.ColorImageToDepthCamera(capture).Memory.ToArray();
            Short3[] xyzArray = xyzImage.GetPixels<Short3>().ToArray();

            Vector3[] vertices = new Vector3[xyzArray.Length];
            Color32[] colors = new Color32[xyzArray.Length];
            int[] indices = new int[xyzArray.Length];

            for (int i = 0; i < vertices.Length; i++) {
                vertices[i] = new Vector3(xyzArray[i].X, xyzArray[i].Y, xyzArray[i].Z);
                colors[i] = new Color32(colorBytes[i*4 + 2], colorBytes[i*4 + 1], colorBytes[i*4], colorBytes[i*4 + 3]); //Convertendo i colori in byte[] ho RGBA del primo pixel poi del secondo e così via. Essendo RGBA 4 valori per passare da un pixel all'altro mi muovo così
                indices[i] = i;
            }

            //kinectImage.GetComponent<MeshFilter>().mesh = new Mesh();
            kinectImage.GetComponent<MeshFilter>().mesh.Clear();
            kinectImage.GetComponent<MeshFilter>().mesh.vertices = new Vector3[vertices.Length];
            kinectImage.GetComponent<MeshFilter>().mesh.colors32 = new Color32[colors.Length];
            kinectImage.GetComponent<MeshFilter>().mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            kinectImage.GetComponent<MeshFilter>().mesh.vertices = vertices;
            kinectImage.GetComponent<MeshFilter>().mesh.colors32 = colors;
            kinectImage.GetComponent<MeshFilter>().mesh.SetIndices(indices, MeshTopology.Points, 0);
            kinectImage.GetComponent<MeshFilter>().mesh.RecalculateBounds();

            //SetImageColorTexture(capture.Color.Memory.ToArray());

            return (rawDepth, colorBytes);
        }
    }

    public (byte[], byte[]) GetCaptureDepthOptimized(){
        using(Capture capture = device.GetCapture()){
            Image depthImage = capture.Depth;

            byte[] rawDepth = depthImage.Memory.ToArray();

            short[] rawShort = new short[(int)Math.Ceiling((double)rawDepth.Length / 2)];
            Buffer.BlockCopy(rawDepth, 0, rawShort, 0, rawDepth.Length);

            short[] optimizedShort = new short[rawShort.Length];

            for(int i = 0; i < rawShort.Length; i++){
                if(rawShort[i] < 1000 && rawShort[i] > 500){
                    optimizedShort[i] = rawShort[i];
                }else{
                    optimizedShort[i] = 0;
                }
            }

            int index = 0;
            for (int i = 0; i < depthImage.HeightPixels; i++) {
                for (int j = 0; j < depthImage.WidthPixels; j++) {
                    depthImage.SetPixel<short>(i, j, optimizedShort[index]); //Si assegnano i pixel dell'immagine depth sulla base dei dati grezzi trasmessi
                    index++;
                }
            }

            Image xyzImage = transformation.DepthImageToPointCloud(depthImage);
            BGRA[] colorArray = transformation.ColorImageToDepthCamera(capture).GetPixels<BGRA>().ToArray();
            byte[] colorBytes = transformation.ColorImageToDepthCamera(capture).Memory.ToArray();
            Short3[] xyzArray = xyzImage.GetPixels<Short3>().ToArray();

            Vector3[] vertices = new Vector3[xyzArray.Length];
            Color32[] colors = new Color32[xyzArray.Length];
            int[] indices = new int[xyzArray.Length];

            List<Color32> list_colors = new List<Color32>();

            for (int i = 0; i < vertices.Length; i++) {
                //vertices[i] = new Vector3(xyzArray[i].X, xyzArray[i].Y, xyzArray[i].Z);
                //colors[i] = new Color32(colorBytes[i*4 + 2], colorBytes[i*4 + 1], colorBytes[i*4], colorBytes[i*4 + 3]); //Convertendo i colori in byte[] ho RGBA del primo pixel poi del secondo e così via. Essendo RGBA 4 valori per passare da un pixel all'altro mi muovo così
                if(xyzArray[i].X != 0 && xyzArray[i].Y != 0 && xyzArray[i].Z != 0){
                    list_colors.Add(new Color32(colorBytes[i*4 + 2], colorBytes[i*4 + 1], colorBytes[i*4], colorBytes[i*4 + 3]));
                }
                //indices[i] = i;
            }

            //conversione list_colors in byte e trasmissione in rete dei colori convertiti in byte

            byte[] optimizedColorBytes = CompressionUtilities.DataCompression.Color32ArrayToByteArray(list_colors.ToArray());

            Color32[] colorOptimized = CompressionUtilities.DataCompression.ByteArrayToColor32(optimizedColorBytes);

            List<Vector3> list_vertices = new List<Vector3>();
            List<int> list_indices = new List<int>();

            int indeces_index = 0;
            for(int i = 0; i < vertices.Length; i++){
                if(xyzArray[i].X != 0 && xyzArray[i].Y != 0 && xyzArray[i].Z != 0){
                    list_vertices.Add(new Vector3(xyzArray[i].X, xyzArray[i].Y, xyzArray[i].Z));
                    list_indices.Add(indeces_index);
                    indeces_index++;
                }
            }
            kinectImage.GetComponent<MeshFilter>().mesh.Clear();
            kinectImage.GetComponent<MeshFilter>().mesh.vertices = new Vector3[list_vertices.Count];
            kinectImage.GetComponent<MeshFilter>().mesh.colors32 = new Color32[colorOptimized.Length];
            kinectImage.GetComponent<MeshFilter>().mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            kinectImage.GetComponent<MeshFilter>().mesh.vertices = list_vertices.ToArray();
            kinectImage.GetComponent<MeshFilter>().mesh.colors32 = colorOptimized;
            kinectImage.GetComponent<MeshFilter>().mesh.SetIndices(list_indices, MeshTopology.Points, 0);
            kinectImage.GetComponent<MeshFilter>().mesh.RecalculateBounds();

            return (rawDepth, optimizedColorBytes);
        }
    }

    public short[] GetCaptureDepthShort() {
        using (Capture capture = device.GetCapture()) {
            Image depthImage = capture.Depth;

            short[] rawDepth = depthImage.GetPixels<short>().ToArray();

            return rawDepth;
        }
    }

    private void OnDestroy() {
        if(device != null)
            device.StopCameras();
    }
}
