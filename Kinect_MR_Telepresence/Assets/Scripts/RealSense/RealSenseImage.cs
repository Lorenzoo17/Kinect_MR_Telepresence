using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Intel.RealSense;
using DepthStreamCompression;
using CompressionUtilities;

public class RealSenseImage : MonoBehaviour
{

    private int rsImageClientID;

    public int GetRsImageClientID() => rsImageClientID;
    public void SetRsImageClientID(int id) {
        rsImageClientID = id;
    }

    private MeshFilter meshFilter;
    [SerializeField] private bool interpolation;

    // Start is called before the first frame update
    void Awake()
    {
        meshFilter = this.GetComponent<MeshFilter>();

        rsImageClientID = -1; //Di base è a -1, per essere usato localmente, quando viene spawnato in altri client questo -1 verrà poi sostituito con l'id del client
    }

    public void SetMeshGivenDepthAndColor(byte[] depthByteArray, byte[] colorByteArray, Intrinsics intrinsics, Vector2 bb_center, int faceArea) { //I dati integer e floating point derivano dagli intrinsics
        //short[] rvl_decompressed = new short[intrinsics.width * intrinsics.height];
        //RVL.DecompressRVL(depthByteArray, rvl_decompressed);

        ushort[] depthData = new ushort[depthByteArray.Length / 2];

        System.Buffer.BlockCopy(depthByteArray, 0, depthData, 0, depthByteArray.Length); //Si riconverte la depth da byte array a short array

        int alternateCompressionValue = 5; // 5 o 10
        depthData = SkipDepthCompression.AlternateDepthDeCompressFaceDetection(depthData, alternateCompressionValue, intrinsics.width, intrinsics.height, interpolation, bb_center, faceArea);
        //depthData = SkipDepthCompression.SkipDepthDecompress(depthData, 5, 424, 240, interpolation);

        Color32[] decompressed = DecompressJPG(colorByteArray); //Ricevo i colori compressi in JPG che trasformo in Texture e dalla texture ottengo i colori

        Vector3[] pointCloud = GeneratePointCloud(depthData, intrinsics);

        Color32[] meshColors = FitColor(decompressed, pointCloud.Length); //Visto che usando DecompressJPG() i colori sono più dei vertici, riduco il numero di colori sulla base del numero dei vertici, per poi usare i colori risultanti per realizzare il mesh

        UpdateMesh(pointCloud, meshColors); //Si aggiorna il mesh a partire da pointCloud e meshColors
    }

    private Vector3[] GeneratePointCloud(ushort[] depthData, Intrinsics intrinsics) {
        Vector3[] pointCloud = new Vector3[depthData.Length];

        // Calculate the depth scale (millimeter to meter conversion)
        float depthScale = 0.001f;

        int index = 0;
        // Loop through each pixel in the depth frame
        for (int y = 0; y < intrinsics.height; y++) {
            for (int x = 0; x < intrinsics.width; x++) {
                ushort depthValue = depthData[index];

                // Calculate the 3D coordinates using the depth value and camera intrinsics
                float depthInMeters = depthValue * depthScale;
                float depthX = (x - intrinsics.ppx) / intrinsics.fx;
                float depthY = (y - intrinsics.ppy) / intrinsics.fy;

                // Populate the point cloud array with the calculated coordinates
                pointCloud[index] = new Vector3(depthX * depthInMeters, depthY * depthInMeters, depthInMeters);
                index++;
            }
        }

        return pointCloud;
    }

    private void UpdateMesh(Vector3[] pointCloud, Color32[] meshColors) {
        if (meshFilter.sharedMesh == null) {
            meshFilter.sharedMesh = new Mesh();
            meshFilter.sharedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }
        else {
            int[] indices = new int[pointCloud.Length];
            for (int i = 0; i < indices.Length; i++) indices[i] = i;
            meshFilter.sharedMesh.vertices = pointCloud;
            if (meshColors != null)
                meshFilter.sharedMesh.colors32 = meshColors;
            meshFilter.sharedMesh.SetIndices(indices, MeshTopology.Points, 0);
        }
    }

    private Color32[] DecompressJPG(byte[] compressedColor) { //Da compressedColor (colori in JPG) ottengo una Texture (immagine jpg) dalla quale ottengo i colori
        Texture2D compTexture = new Texture2D(424, 240);
        compTexture.LoadImage(compressedColor);

        Color32[] compTextureColors = compTexture.GetPixels32();

        Destroy(compTexture);

        return compTextureColors; //????   Perche questa texture in JPG ha dimensione maggiore della grezza?   ?????
    }

    private Color32[] FitColor(Color32[] colors, int desiredSize) {
        if (colors.Length == desiredSize)
            return colors;
        else {
            Color32[] fitted = new Color32[desiredSize];

            for(int i = 0; i < desiredSize; i++) {
                fitted[i] = colors[i];
            }

            return fitted;
        }
    }
}
