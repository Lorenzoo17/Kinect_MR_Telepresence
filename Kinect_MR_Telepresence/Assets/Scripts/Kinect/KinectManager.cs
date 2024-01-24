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
    [SerializeField] private Texture2D colorTexture;

    [SerializeField] private int jpgCompressionQuality;

    [SerializeField] private ushort depthRange = 1000;

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

    public bool KinectAvailable(){
        return (device != null) ? true : false;
    }
    private void Awake() {
        derivedFramesCount = maxDerivedFrames;
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

    public (byte[], byte[]) GetCaptureDepthCompressed() {
        using (Capture capture = device.GetCapture()) {
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

            //COLORIZATION locale (ATTENZIONE!!!! --> L'USO DI QUESTO PROCEDIMENTO CAUSA FILL DELLA RAM, NECESSARIO DISTRUGGERE LE TEXTURE NON IN USO
            /*
            ushort[] depthUshort = new ushort[rawShort.Length];

            for(int i = 0; i < rawShort.Length; i++) {
                depthUshort[i] = Convert.ToUInt16(rawShort[i]);
            }

            byte[] depthColorized = DepthColorization(depthUshort);
            */
            
            byte[] colorBytes = transformation.ColorImageToDepthCamera(capture).Memory.ToArray();

            colorBytes = CompressColorsByteToJPG(colorBytes); //Compressione colori in jpeg

            kinectImage.GetComponent<KinectImage>().SetMeshGivenDepthAndColorCompressed(rawDepth, colorBytes);

            return (rawDepth, colorBytes);
        }
    }

    public (byte[], byte[], int) GetCaptureDepthCompressedTemporal() {
        using (Capture capture = device.GetCapture()) {
            Image depthImage = capture.Depth;

            byte[] rawDepth = depthImage.Memory.ToArray();

            short[] rawShort = new short[(int)Math.Ceiling((double)rawDepth.Length / 2)];
            Buffer.BlockCopy(rawDepth, 0, rawShort, 0, rawDepth.Length);

            /*
            for(int i = 0; i < rawShort.Length; i++) {
                if (rawShort[i] > depthRange)
                    rawShort[i] = 0;
            }

            rawDepth = new byte[rawShort.Length * 2];
            Buffer.BlockCopy(rawShort, 0, rawDepth, 0, rawShort.Length);
            */

            //COLORIZATION locale (ATTENZIONE!!!! --> L'USO DI QUESTO PROCEDIMENTO CAUSA FILL DELLA RAM, NECESSARIO DISTRUGGERE LE TEXTURE NON IN USO
            /*
            ushort[] depthUshort = new ushort[rawShort.Length];

            for(int i = 0; i < rawShort.Length; i++) {
                depthUshort[i] = Convert.ToUInt16(rawShort[i]);
            }

            byte[] depthColorized = DepthColorization(depthUshort);
            */

            byte[] colorBytes = transformation.ColorImageToDepthCamera(capture).Memory.ToArray();
            byte[] derivedColors = null;
            colorImagesList.Add(colorBytes);//Aggiungo immagine attuale alla lista

            if (derivedFramesCount < maxDerivedFrames) { //Se non devo ancora inviare un keyFrame
                derivedFramesCount++;
                derivedColors = JpgDifference(colorImagesList[derivedFramesCount-1], colorImagesList[derivedFramesCount]); //Calcolo l'immagine "differenza" da inviare al posto dell'immagine completa
                //Trasmetti differenza
            }
            else { //altrimenti
                derivedFramesCount = 0; //rinizio da capo, invierò quindi il keyframe
                colorImagesList.Clear(); //pulisco la lista

                colorImagesList.Add(colorBytes);
                //Trasmetti immagine completa
            }

            colorBytes = CompressColorsByteToJPG(colorBytes); //Compressione colori in jpeg

            //kinectImage.GetComponent<KinectImage>().SetMeshGivenDepthAndColorCompressed(rawDepth, colorBytes);

            if (derivedFramesCount == 0) {
                kinectImage.GetComponent<KinectImage>().SetMeshGivenDepthAndColorCompressedTemporal(rawDepth, colorBytes, derivedFramesCount); //uso immagine completa (keyframe)
            }
            else {
                kinectImage.GetComponent<KinectImage>().SetMeshGivenDepthAndColorCompressedTemporal(rawDepth, CompressColorsByteToJPG(derivedColors), derivedFramesCount); //uso immagine "differenza"
            }

            //VisualizeJPG(colorBytes, jpgColorImage);

            if(derivedFramesCount == 0) {
                return (rawDepth, colorBytes, derivedFramesCount);//Invio keyframe
            }
            else {
                return (rawDepth, CompressColorsByteToJPG(derivedColors), derivedFramesCount); //Invio immagine differenza
            }
        }
    }

    private void VisualizeJPG(byte[] compressedColors, UnityEngine.UI.RawImage rawImage) {
        Texture2D jpgColors = new Texture2D(depthWidth, depthHeight);
        jpgColors.LoadImage(compressedColors);
        jpgColors.Apply();

        rawImage.texture = jpgColors;
    }

    private void VisualizeJPG32(Color32[] compressedColors, UnityEngine.UI.RawImage rawImage) {
        Texture2D jpgColors = new Texture2D(depthWidth, depthHeight);
        jpgColors.SetPixels32(compressedColors);
        jpgColors.Apply();

        rawImage.texture = jpgColors;
    }

    private byte[] JpgDifference(byte[] originalFrame, byte[] newFrame) {
        Color32[] originalFrameColors = DataCompression.ByteArrayToColor32(originalFrame);

        Color32[] newFrameColors = DataCompression.ByteArrayToColor32(newFrame);

        Color32 threshold = new Color32(thresholdValues, thresholdValues, thresholdValues, 255);

        Color32[] derivedFrame = new Color32[originalFrameColors.Length];

        for(int i = 0; i < originalFrameColors.Length; i++) {
            if(Mathf.Abs(originalFrameColors[i].r - newFrameColors[i].r) < threshold.r && Mathf.Abs(originalFrameColors[i].g - newFrameColors[i].g) < threshold.g 
                && Mathf.Abs(originalFrameColors[i].b - newFrameColors[i].b) < threshold.b && Mathf.Abs(originalFrameColors[i].a - newFrameColors[i].a) < threshold.a) {
                derivedFrame[i] = new Color32(0, 0, 0, 255); //Se il valore del pixel è inferiore al threshold lo setto a 0 (non c'è stato un cambiamento significativo)
            }
            else {
                derivedFrame[i] = newFrameColors[i]; //Altrimenti lo setto pari alla nuova immagine
            }
        }

        VisualizeJPG32(derivedFrame, jpgDifferenceImage);

        return DataCompression.Color32ArrayToByteArray(derivedFrame);
    }

    private void ReconstructWithDifference(byte[] originalFrame, byte[] derivedFrame) {
        Texture2D originalFrameTexture = new Texture2D(depthWidth, depthHeight);
        originalFrameTexture.LoadImage(originalFrame);
        originalFrameTexture.Apply();

        Color32[] originalFrameColors = originalFrameTexture.GetPixels32();
        Destroy(originalFrameTexture);

        Texture2D derivedFrameTexture = new Texture2D(depthWidth, depthHeight);
        derivedFrameTexture.LoadImage(derivedFrame);
        derivedFrameTexture.Apply();

        Color32[] derivedFrameColors = derivedFrameTexture.GetPixels32();
        Destroy(derivedFrameTexture);

        Color32[] newFrame = new Color32[originalFrameColors.Length];

        for(int i = 0; i < derivedFrameColors.Length; i++) {
            if(derivedFrameColors[i].r != 0 && derivedFrameColors[i].g != 0 && derivedFrameColors[i].b != 0) {
                newFrame[i] = derivedFrameColors[i];
            }
            else {
                newFrame[i] = originalFrameColors[i];
            }
        }

        //VisualizeJPG32(newFrame, ReconstructedJpgImage);
    }

    //--------------------- VECCHIO METODO DI OTTIMIZZAZIONE -------------------------
    /*
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

    */

    private void WriteInFile(byte[] depth) {
        using(StreamWriter sw = new StreamWriter("prova.txt", false)) {
            for(int i = 0; i < depth.Length; i++) {
                sw.WriteLine(depth[i].ToString());
            }
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

    #region COLORIZATION METHODS
    private byte[] DepthColorization(ushort[] depthImage) {
        //Si limita il range di depth

        ushort dmin = 1;
        ushort dmax = depthRange;

        Color32[] colorizedImage = new Color32[depthImage.Length];

        for (int i = 0; i < depthImage.Length; i++) {

            ushort d = depthImage[i];

            float dnormal = (float)(d - dmin) / (dmax - dmin) * 1529; //Ricorda cast float --> visto che d / dmax da un numero tra 0 ed 1, altrimenti da sempre 0

            byte pr = 0;
            byte pg = 0;
            byte pb = 0;

            if (dnormal >= 0 && dnormal <= 255 || dnormal > 1275 && dnormal <= 1529) {
                pr = 255;
            }
            else if (dnormal > 255 && dnormal <= 510) {
                pr = (byte)(255 - dnormal);
            }
            else if (dnormal > 510 && dnormal <= 1020) {
                pr = 0;
            }
            else if (dnormal > 1020 && dnormal <= 1275) {
                if (dnormal - 1020 > 0)
                    pr = (byte)(dnormal - 1020);
            }

            if (dnormal > 0 && dnormal <= 255) {
                pg = (byte)dnormal;
            }
            else if (dnormal > 255 && dnormal <= 510) {
                pg = 255;
            }
            else if (dnormal > 510 && dnormal <= 765) {
                pg = (byte)(765 - dnormal);
            }
            else if (dnormal > 765 && dnormal <= 1529) {
                pg = 0;
            }

            if (dnormal > 0 && dnormal <= 765) {
                pb = 0;
            }
            else if (dnormal > 765 && dnormal <= 1020) {
                pb = (byte)(dnormal - 765);
            }
            else if (dnormal > 1020 && dnormal <= 1275) {
                pb = 255;
            }
            else if (dnormal > 1275 && dnormal <= 1529) {
                pb = (byte)(1275 - dnormal);
            }

            colorizedImage[i] = new Color32(pr, pg, pb, 255);
        }

        Texture2D test = new Texture2D(depthWidth, depthHeight, TextureFormat.RGBA32, false);

        test.SetPixels32(colorizedImage);

        test.Apply();

        testColorization.texture = test;

        //JPEG Compression

        byte[] colorizedByteArray = test.GetRawTextureData();

        Texture2D texture = new Texture2D(depthWidth, depthHeight, TextureFormat.RGBA32, false);
        texture.SetPixels32(colorizedImage);
        texture.Apply();

        byte[] compressed = ImageConversion.EncodeToJPG(texture, jpgCompressionQuality); //Quality --> 1 (bassa) - 100 (alta) ==> 75 default ; 50 impercettibile ; 25 si nota ma buono per diminuzione dimensione

        Debug.Log("Compressed colorized depth : " + compressed.Length);

        Texture2D compressedTexture = new Texture2D(depthWidth, depthHeight, TextureFormat.RGBA32, false);
        compressedTexture.LoadImage(compressed);
        compressedTexture.Apply();

        colorizationCompressed.texture = compressedTexture;

        return compressed;
    }

    private ushort[] RestoreDeptColorization(byte[] compressedColorization) {

        Texture2D compressedTexture = new Texture2D(depthWidth, depthHeight, TextureFormat.RGBA32, false);
        compressedTexture.LoadImage(compressedColorization);
        compressedTexture.Apply();

        colorizationCompressed.texture = compressedTexture;

        Color32[] compressedPixels = compressedTexture.GetPixels32();

        int dnormal = 1;

        int dmin = 1;
        int dmax = depthRange;

        ushort[] recoveryDepth = new ushort[compressedPixels.Length];

        //Debug.Log("Recovery depth length : " + recoveryDepth.Length);

        for (int i = 0; i < compressedPixels.Length; i++) {
            byte prr = compressedPixels[i].r;
            byte prg = compressedPixels[i].g;
            byte prb = compressedPixels[i].b;

            if (prr >= prg && prr >= prb && prg >= prb) {
                dnormal = prg - prb;
            }
            else if (prr >= prg && prr >= prb && prg < prb) {
                dnormal = prg - prb + 1529;
            }
            else if (prg >= prr && prg >= prb) {
                dnormal = prb - prr + 510;
            }
            else if (prb >= prg && prb >= prr) {
                dnormal = prr - prg + 1020;
            }

            float drecovery_fp = dmin + ((float)((dmax - dmin) * dnormal) / 1529);

            ushort drecovery = (ushort)drecovery_fp;

            recoveryDepth[i] = drecovery;
        }

        DepthToGrayscale(recoveryDepth, reconstructedDepthGrayscale);

        return recoveryDepth;
    }

    private void DepthToGrayscale(ushort[] depthImage, UnityEngine.UI.RawImage rawImage) {
        Texture2D grayscaleTexture = new Texture2D(depthWidth, depthHeight, TextureFormat.R16, false);

        Color[] pixels = new Color[depthWidth * depthHeight];

        for (int i = 0; i < depthImage.Length; i++) {
            if (depthImage[i] > depthRange) {
                depthImage[i] = 0;
            }
        }

        // Normalizza i valori depth e converte in livelli di grigio
        for (int i = 0; i < depthImage.Length; i++) {
            ushort depthValue = depthImage[i];
            //float normalizedDepth = Mathf.InverseLerp(ushort.MinValue, depthRange, depthValue);
            float normalizedDepth = ((float)depthValue / depthRange);

            pixels[i] = new Color(normalizedDepth, normalizedDepth, normalizedDepth);
        }

        // Imposta i pixel sulla texture
        grayscaleTexture.SetPixels(pixels);

        // Applica i cambiamenti alla texture
        grayscaleTexture.Apply();

        rawImage.texture = grayscaleTexture;
    }
    #endregion

    private void OnDestroy() {
        if(device != null) {
            device.StopCameras();
            device.Dispose();
        }
    }
}
