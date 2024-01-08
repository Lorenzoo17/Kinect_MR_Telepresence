using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Microsoft.Azure.Kinect.Sensor;

public class KinectImage : MonoBehaviour
{
    private Transformation transformation;
    private int depthWidth, depthHeight;
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
}
