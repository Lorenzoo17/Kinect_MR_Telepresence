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
