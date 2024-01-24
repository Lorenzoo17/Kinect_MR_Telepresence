using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Microsoft.Azure.Kinect.Sensor;

public class KinectImage : MonoBehaviour
{
    private Transformation transformation;
    private int depthWidth, depthHeight;

    private byte[] originalFrame = null;
    [SerializeField] private UnityEngine.UI.RawImage testRec;
    // Start is called before the first frame update
    private void Start()
    {
        this.GetComponent<MeshFilter>().mesh = new Mesh();
    }

    public void SetTransformation(Transformation transformation, int width, int height)
    {
        this.transformation = transformation;
        depthHeight = height;
        depthWidth = width;
    }

    public void SetMeshGivenDepthAndColor(byte[] rawDepth, byte[] rawColors)
    {
        Image depthReconstructed = new Image(ImageFormat.Depth16, depthWidth, depthHeight); //Si definisce un immagine di tipo Depth

        short[] rawShort = new short[(int)Math.Ceiling((double)rawDepth.Length / 2)]; //Per ottenere correttamente la depth image devo convertire byte[] in short[]
        Buffer.BlockCopy(rawDepth, 0, rawShort, 0, rawDepth.Length);

        int index = 0;
        for (int i = 0; i < depthReconstructed.HeightPixels; i++)
        {
            for (int j = 0; j < depthReconstructed.WidthPixels; j++)
            {
                depthReconstructed.SetPixel<short>(i, j, rawShort[index]); //Si assegnano i pixel dell'immagine depth sulla base dei dati grezzi trasmessi
                index++;
            }
        }

        //Debug.Log($"Depth image obtained {depthReconstructed.ToString()} : {depthReconstructed.WidthPixels}, {depthReconstructed.HeightPixels}");

        Image xyzImage = transformation.DepthImageToPointCloud(depthReconstructed);
        Short3[] xyzArray = xyzImage.GetPixels<Short3>().ToArray();

        Vector3[] vertices = new Vector3[xyzArray.Length];
        Color32[] colors = new Color32[xyzArray.Length];
        int[] indices = new int[xyzArray.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = new Vector3(xyzArray[i].X, xyzArray[i].Y, xyzArray[i].Z);
            colors[i] = new Color32(rawColors[i * 4 + 2], rawColors[i * 4 + 1], rawColors[i * 4], rawColors[i * 4 + 3]);
            indices[i] = i;
        }

        //this.GetComponent<MeshFilter>().mesh = new Mesh();
        this.GetComponent<MeshFilter>().mesh.vertices = new Vector3[vertices.Length];
        this.GetComponent<MeshFilter>().mesh.colors32 = new Color32[colors.Length];
        this.GetComponent<MeshFilter>().mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        this.GetComponent<MeshFilter>().mesh.vertices = vertices;
        this.GetComponent<MeshFilter>().mesh.colors32 = colors;
        this.GetComponent<MeshFilter>().mesh.SetIndices(indices, MeshTopology.Points, 0);
        this.GetComponent<MeshFilter>().mesh.RecalculateBounds();
    }

    public void SetMeshGivenDepthAndColorCompressed(byte[] rawDepth, byte[] rawColors) {
        Image depthReconstructed = new Image(ImageFormat.Depth16, depthWidth, depthHeight); //Si definisce un immagine di tipo Depth

        short[] rawShort = new short[(int)Math.Ceiling((double)rawDepth.Length / 2)]; //Per ottenere correttamente la depth image devo convertire byte[] in short[]
        Buffer.BlockCopy(rawDepth, 0, rawShort, 0, rawDepth.Length);
        
        //PER COLORIZATION
        /*
        ushort[] depthUshort = RestoreDeptColorization(rawDepth);

        short[] rawShort = new short[depthUshort.Length];

        for (int i = 0; i < rawShort.Length; i++) {
            rawShort[i] = Convert.ToInt16(depthUshort[i]);
        }
        */

        int index = 0;
        for (int i = 0; i < depthReconstructed.HeightPixels; i++) {
            for (int j = 0; j < depthReconstructed.WidthPixels; j++) {
                depthReconstructed.SetPixel<short>(i, j, rawShort[index]); //Si assegnano i pixel dell'immagine depth sulla base dei dati grezzi trasmessi
                index++;
            }
        }

        //Debug.Log($"Depth image obtained {depthReconstructed.ToString()} : {depthReconstructed.WidthPixels}, {depthReconstructed.HeightPixels}");



        Image xyzImage = transformation.DepthImageToPointCloud(depthReconstructed);
        Short3[] xyzArray = xyzImage.GetPixels<Short3>().ToArray();

        Vector3[] vertices = new Vector3[xyzArray.Length];
        Color32[] colors = new Color32[xyzArray.Length];
        int[] indices = new int[xyzArray.Length];
        for (int i = 0; i < vertices.Length; i++) {
            vertices[i] = new Vector3(xyzArray[i].X, xyzArray[i].Y, xyzArray[i].Z);
            //colors[i] = new Color32(rawColors[i * 4 + 2], rawColors[i * 4 + 1], rawColors[i * 4], rawColors[i * 4 + 3]);
            indices[i] = i;
        }

        colors = DecompressJPG(rawColors);
        colors = FitColor(colors, vertices.Length);

        //this.GetComponent<MeshFilter>().mesh = new Mesh();
        this.GetComponent<MeshFilter>().mesh.vertices = new Vector3[vertices.Length];
        this.GetComponent<MeshFilter>().mesh.colors32 = new Color32[colors.Length];
        this.GetComponent<MeshFilter>().mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        this.GetComponent<MeshFilter>().mesh.vertices = vertices;
        this.GetComponent<MeshFilter>().mesh.colors32 = colors;
        this.GetComponent<MeshFilter>().mesh.SetIndices(indices, MeshTopology.Points, 0);
        this.GetComponent<MeshFilter>().mesh.RecalculateBounds();
    }

    public void SetMeshGivenDepthAndColorCompressedTemporal(byte[] rawDepth, byte[] rawColors, int derivedFrameCountValue) {
        Image depthReconstructed = new Image(ImageFormat.Depth16, depthWidth, depthHeight); //Si definisce un immagine di tipo Depth

        short[] rawShort = new short[(int)Math.Ceiling((double)rawDepth.Length / 2)]; //Per ottenere correttamente la depth image devo convertire byte[] in short[]
        Buffer.BlockCopy(rawDepth, 0, rawShort, 0, rawDepth.Length);

        //PER COLORIZATION
        /*
        ushort[] depthUshort = RestoreDeptColorization(rawDepth);

        short[] rawShort = new short[depthUshort.Length];

        for (int i = 0; i < rawShort.Length; i++) {
            rawShort[i] = Convert.ToInt16(depthUshort[i]);
        }
        */

        int index = 0;
        for (int i = 0; i < depthReconstructed.HeightPixels; i++) {
            for (int j = 0; j < depthReconstructed.WidthPixels; j++) {
                depthReconstructed.SetPixel<short>(i, j, rawShort[index]); //Si assegnano i pixel dell'immagine depth sulla base dei dati grezzi trasmessi
                index++;
            }
        }

        //Debug.Log($"Depth image obtained {depthReconstructed.ToString()} : {depthReconstructed.WidthPixels}, {depthReconstructed.HeightPixels}");

        if (derivedFrameCountValue == 0) { //Vuol dire che ho ricevuto un keyframe, quindi mi comporto normalmente
            originalFrame = rawColors;

            Image xyzImage = transformation.DepthImageToPointCloud(depthReconstructed);
            Short3[] xyzArray = xyzImage.GetPixels<Short3>().ToArray();

            Vector3[] vertices = new Vector3[xyzArray.Length];
            Color32[] colors = new Color32[xyzArray.Length];
            int[] indices = new int[xyzArray.Length];
            for (int i = 0; i < vertices.Length; i++) {
                vertices[i] = new Vector3(xyzArray[i].X, xyzArray[i].Y, xyzArray[i].Z);
                //colors[i] = new Color32(rawColors[i * 4 + 2], rawColors[i * 4 + 1], rawColors[i * 4], rawColors[i * 4 + 3]);
                indices[i] = i;
            }

            colors = DecompressJPG(rawColors);
            colors = FitColor(colors, vertices.Length);

            //this.GetComponent<MeshFilter>().mesh = new Mesh();
            this.GetComponent<MeshFilter>().mesh.vertices = new Vector3[vertices.Length];
            this.GetComponent<MeshFilter>().mesh.colors32 = new Color32[colors.Length];
            this.GetComponent<MeshFilter>().mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            this.GetComponent<MeshFilter>().mesh.vertices = vertices;
            this.GetComponent<MeshFilter>().mesh.colors32 = colors;
            this.GetComponent<MeshFilter>().mesh.SetIndices(indices, MeshTopology.Points, 0);
            this.GetComponent<MeshFilter>().mesh.RecalculateBounds();
        }
        else { //Ho ricevuto un frame "differenza"
            Color32[] newImage = ReconstructWithDifference(originalFrame, rawColors); //Calcolo il nuovo frame unendo il keyframe precedentemente salvato e l'immagine "differenza" ricevuta

            Image xyzImage = transformation.DepthImageToPointCloud(depthReconstructed);
            Short3[] xyzArray = xyzImage.GetPixels<Short3>().ToArray();

            Vector3[] vertices = new Vector3[xyzArray.Length];
            Color32[] colors = newImage;
            int[] indices = new int[xyzArray.Length];
            for (int i = 0; i < vertices.Length; i++) {
                vertices[i] = new Vector3(xyzArray[i].X, xyzArray[i].Y, xyzArray[i].Z);
                //colors[i] = new Color32(rawColors[i * 4 + 2], rawColors[i * 4 + 1], rawColors[i * 4], rawColors[i * 4 + 3]);
                indices[i] = i;
            }

            colors = FitColor(colors, vertices.Length);

            //this.GetComponent<MeshFilter>().mesh = new Mesh();
            this.GetComponent<MeshFilter>().mesh.vertices = new Vector3[vertices.Length];
            this.GetComponent<MeshFilter>().mesh.colors32 = new Color32[colors.Length];
            this.GetComponent<MeshFilter>().mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            this.GetComponent<MeshFilter>().mesh.vertices = vertices;
            this.GetComponent<MeshFilter>().mesh.colors32 = colors;
            this.GetComponent<MeshFilter>().mesh.SetIndices(indices, MeshTopology.Points, 0);
            this.GetComponent<MeshFilter>().mesh.RecalculateBounds();
        }

        
    }

    private void VisualizeJPG32(Color32[] compressedColors, UnityEngine.UI.RawImage rawImage) {
        Texture2D jpgColors = new Texture2D(depthWidth, depthHeight);
        jpgColors.SetPixels32(compressedColors);
        jpgColors.Apply();

        rawImage.texture = jpgColors;
    }

    private Color32[] ReconstructWithDifference(byte[] originalFrame, byte[] derivedFrame) {
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

        for (int i = 0; i < derivedFrameColors.Length; i++) {
            if (derivedFrameColors[i].r != 0 && derivedFrameColors[i].g != 0 && derivedFrameColors[i].b != 0) { //Se il pixel non appartiene a quelli scartati (indica cambiamento)
                newFrame[i] = derivedFrameColors[i]; //Al nuovo frame assegno il valore dell'immagine "differenza"
            }
            else {
                newFrame[i] = originalFrameColors[i]; //Altrimenti gli assegno il valore del keyframe
            }
        }

        //VisualizeJPG32(newFrame, testRec);

        return newFrame;
    }

    private ushort[] RestoreDeptColorization(byte[] compressedColorization) {

        Texture2D compressedTexture = new Texture2D(depthWidth, depthHeight, TextureFormat.RGBA32, false);
        compressedTexture.LoadImage(compressedColorization);
        compressedTexture.Apply();

        Color32[] compressedPixels = compressedTexture.GetPixels32();

        int dnormal = 1;

        int dmin = 1;
        int dmax = 1000;

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

        return recoveryDepth;
    }

    //--------------------- VECCHIO METODO DI OTTIMIZZAZIONE -------------------------
    /*

    public void SetMeshGivenDepthAndColorOptimized(byte[] rawDepth, byte[] rawColors)
    {
        Image depthReconstructed = new Image(ImageFormat.Depth16, depthWidth, depthHeight); //Si definisce un immagine di tipo Depth

        short[] rawShort = new short[(int)Math.Ceiling((double)rawDepth.Length / 2)]; //Per ottenere correttamente la depth image devo convertire byte[] in short[]
        Buffer.BlockCopy(rawDepth, 0, rawShort, 0, rawDepth.Length);

        short[] optimizedShort = new short[rawShort.Length];

        for (int i = 0; i < rawShort.Length; i++)
        {
            if (rawShort[i] < 1000 && rawShort[i] > 500)
            {
                optimizedShort[i] = rawShort[i];
            }
            else
            {
                optimizedShort[i] = 0;
            }
        }

        int index = 0;
        for (int i = 0; i < depthReconstructed.HeightPixels; i++)
        {
            for (int j = 0; j < depthReconstructed.WidthPixels; j++)
            {
                depthReconstructed.SetPixel<short>(i, j, optimizedShort[index]); //Si assegnano i pixel dell'immagine depth sulla base dei dati grezzi trasmessi
                index++;
            }
        }

        //Debug.Log($"Depth image obtained {depthReconstructed.ToString()} : {depthReconstructed.WidthPixels}, {depthReconstructed.HeightPixels}");

        Image xyzImage = transformation.DepthImageToPointCloud(depthReconstructed);
        Short3[] xyzArray = xyzImage.GetPixels<Short3>().ToArray();

        Color32[] colorOptimized = CompressionUtilities.DataCompression.ByteArrayToColor32(rawColors);

        List<Vector3> list_vertices = new List<Vector3>();
        List<int> list_indices = new List<int>();

        int indeces_index = 0;
        for (int i = 0; i < xyzArray.Length; i++)
        {
            if (xyzArray[i].X != 0 && xyzArray[i].Y != 0 && xyzArray[i].Z != 0)
            {
                list_vertices.Add(new Vector3(xyzArray[i].X, xyzArray[i].Y, xyzArray[i].Z));
                list_indices.Add(indeces_index);
                indeces_index++;
            }
        }

        this.GetComponent<MeshFilter>().mesh.Clear();
        this.GetComponent<MeshFilter>().mesh.vertices = new Vector3[list_vertices.Count];
        this.GetComponent<MeshFilter>().mesh.colors32 = new Color32[colorOptimized.Length];
        this.GetComponent<MeshFilter>().mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        this.GetComponent<MeshFilter>().mesh.vertices = list_vertices.ToArray();
        this.GetComponent<MeshFilter>().mesh.colors32 = colorOptimized;
        this.GetComponent<MeshFilter>().mesh.SetIndices(list_indices, MeshTopology.Points, 0);
        this.GetComponent<MeshFilter>().mesh.RecalculateBounds();
    }

    */

    public void SetMeshGivenDepth(byte[] rawDepth)
    {
        Image depthReconstructed = new Image(ImageFormat.Depth16, depthWidth, depthHeight); //Si definisce un immagine di tipo Depth

        short[] rawShort = new short[(int)Math.Ceiling((double)rawDepth.Length / 2)];
        Buffer.BlockCopy(rawDepth, 0, rawShort, 0, rawDepth.Length);

        int index = 0;
        for (int i = 0; i < depthReconstructed.HeightPixels; i++)
        {
            for (int j = 0; j < depthReconstructed.WidthPixels; j++)
            {
                depthReconstructed.SetPixel<short>(i, j, rawShort[index]); //Si assegnano i pixel dell'immagine depth sulla base dei dati grezzi trasmessi
                index++;
            }
        }

        Debug.Log($"Depth image obtained {depthReconstructed.ToString()} : {depthReconstructed.WidthPixels}, {depthReconstructed.HeightPixels}");

        Image xyzImage = transformation.DepthImageToPointCloud(depthReconstructed);
        Short3[] xyzArray = xyzImage.GetPixels<Short3>().ToArray();

        Vector3[] vertices = new Vector3[xyzArray.Length];
        int[] indices = new int[xyzArray.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = new Vector3(xyzArray[i].X, xyzArray[i].Y, xyzArray[i].Z);
            indices[i] = i;
        }

        //this.GetComponent<MeshFilter>().mesh = new Mesh();
        this.GetComponent<MeshFilter>().mesh.vertices = new Vector3[vertices.Length];
        //this.GetComponent<MeshFilter>().mesh.colors32 = new Color32[finalColors.Count];
        this.GetComponent<MeshFilter>().mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        this.GetComponent<MeshFilter>().mesh.vertices = vertices;
        //this.GetComponent<MeshFilter>().mesh.colors32 = finalColors.ToArray();
        this.GetComponent<MeshFilter>().mesh.SetIndices(indices, MeshTopology.Points, 0);
        this.GetComponent<MeshFilter>().mesh.RecalculateBounds();
    }


    private Color32[] DecompressJPG(byte[] compressedColor) { //Da compressedColor (colori in JPG) ottengo una Texture (immagine jpg) dalla quale ottengo i colori
        Texture2D compTexture = new Texture2D(320, 288);
        compTexture.LoadImage(compressedColor);

        Color32[] compTextureColors = compTexture.GetPixels32();

        Destroy(compTexture);

        return compTextureColors; //????   Perche questa texture in JPG ha dimensione maggiore della grezza?   ?????
    }

    private Color32[] FitColor(Color32[] colors, int desiredSize) {
        if (colors.Length == desiredSize) {
            for(int i = 0; i < desiredSize; i++) {
                colors[i] = new Color32(colors[i].b, colors[i].g, colors[i].r, 255); //Il kinect ha di base un'immagine BGRA quindi la converto in RGBA
            }

            return colors;
        }
        else {
            Color32[] fitted = new Color32[desiredSize];

            for (int i = 0; i < desiredSize; i++) {
                fitted[i] = new Color32(colors[i].r, colors[i].r, colors[i].r, 255);
            }

            return fitted;
        }
    }
}
