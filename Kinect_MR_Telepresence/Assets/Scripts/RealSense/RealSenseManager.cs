using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Intel.RealSense;
using System.Linq;
using DepthStreamCompression;
using System;
using CompressionUtilities;
using JetBrains.Annotations;

public class RealSenseManager : MonoBehaviour
{
    private Pipeline pipeline;

    [SerializeField] private int width;
    [SerializeField] private int height;
    [SerializeField] private int framerate;

    [SerializeField] private int jpgCompressionQuality;

    [SerializeField] private ushort depthRange;
    [SerializeField] private int x_min_limit;
    [SerializeField] private int x_max_limit;

    [SerializeField] private int y_min_dec;
    [SerializeField] private int y_max_dec;
    [SerializeField] private int decimationRate;
    [SerializeField] private RealSenseImage localImage;

    [SerializeField] private int faceAreaDimension;

    private Texture2D colorImageFromDepthTexture;

    private ushort[] depthData;
    private Intrinsics intrinsics;

    private Vector2 boundingBoxCenter;
    private Vector2 boundingBoxSize;

    [SerializeField] private FCSourceTexture sourceTextureForFaceDetection;

    [SerializeField] private UnityEngine.UI.RawImage testColorization;
    [SerializeField] private UnityEngine.UI.RawImage colorizationCompressed;
    [SerializeField] private UnityEngine.UI.RawImage reconstructedDepthGrayscale;

    // Start is called before the first frame update
    void Start()
    {
        InitializeRealSense();
    }

    public (byte[], byte[], Intrinsics) RealSenseCaptureDepthAndColor() {
        using(var frames = pipeline.WaitForFrames()) {
            using(var colorFrame = frames.ColorFrame)
            using(var depthFrame = frames.DepthFrame) {

                if(depthFrame != null) {
                    depthFrame.CopyTo(depthData);

                    intrinsics = depthFrame.Profile.As<VideoStreamProfile>().GetIntrinsics(); //Si prendono gli intrinsics della camera (dalla depth)
                    
                    // PER ALLINEARE COLOR TO DEPTH
                    Align align = new Align(Stream.Depth).DisposeWith(frames);
                    Frame aligned = align.Process(frames).DisposeWith(frames);
                    FrameSet alignedframeset = aligned.As<FrameSet>().DisposeWith(frames);
                    var color_frame_aligned_to_depth = alignedframeset.ColorFrame.DisposeWith(alignedframeset);
                    //var d_frame = alignedframeset.DepthFrame.DisposeWith(alignedframeset);

                    // SI OTTIENE L'ARRAY DI BYTE DAL COLOR FRAME ALLINEATO
                    int colorDataSize = color_frame_aligned_to_depth.DataSize;
                    byte[] colorByteArray = new byte[colorDataSize]; //array di colori (dalla prospettiva della depth camera)

                    unsafe {
                        byte* colorData = (byte*)color_frame_aligned_to_depth.Data.ToPointer();

                        for (int i = 0; i < colorDataSize; i++) {
                            colorByteArray[i] = colorData[i];
                        }
                    }

                    colorImageFromDepthTexture = new Texture2D(color_frame_aligned_to_depth.Width, color_frame_aligned_to_depth.Height, TextureFormat.RGB24, false);
                    colorImageFromDepthTexture.LoadRawTextureData(colorByteArray);
                    colorImageFromDepthTexture.Apply();

                    Debug.Log($"Depth : {depthData.Length}, color : {colorByteArray.Length}");

                    //DepthColorization(depthData);

                    depthData = ElaborateDepth(depthData);

                    int alternateCompressionValue = 5; // 5 o 10 
                    ushort[] newdepthData = SkipDepthCompression.AlternateDepthCompressFaceDetection(depthData, alternateCompressionValue, width, height, boundingBoxCenter, faceAreaDimension);
                    //ushort[] newdepthData = SkipDepthCompression.SkipDepthCompress(depthData, 5);

                    //Destroy(colorImageFromDepthTexture);

                    byte[] depthByteArray = new byte[newdepthData.Length * 2];

                    System.Buffer.BlockCopy(newdepthData, 0, depthByteArray, 0, newdepthData.Length * 2); //Si converte la depth (come ushort) in byte array

                    byte[] compressedJPGColors = CompressColorsByteToJPG(colorByteArray); //Comprimo i colori grezzi in JPG

                    //short[] to_compress_rvl = Array.ConvertAll(depthData, i => (short)i);

                    //RVL.CompressRVL(to_compress_rvl, depthByteArray);

                    localImage.SetMeshGivenDepthAndColor(depthByteArray, compressedJPGColors, intrinsics, boundingBoxCenter, faceAreaDimension); //Aggiorno il mesh locale

                    return (depthByteArray, compressedJPGColors, intrinsics); //trasmetto depth e colori compressi
                }
                else {
                    return (null, null, new Intrinsics());
                }
            }
        }
    }

    public Intrinsics GetIntrinsics() {
        return intrinsics;
    }

    public Texture2D GetTextureColorFromDepth() {
        return colorImageFromDepthTexture;
    }

    public void FreeColorFromDepthTexture() {
        Destroy(colorImageFromDepthTexture);
    }

    public int GetWidth() {
        return width;
    }

    public int GetHeight() {
        return height;
    }

    public void SetBoundingBox(Vector2 center, Vector2 size) {
        boundingBoxCenter = center;
        boundingBoxSize = size;
    }

    public (Vector2, int) GetBBData() {
        Vector2 bb = (boundingBoxCenter != null) ? boundingBoxCenter : Vector2.zero;
        return (bb, faceAreaDimension);
    }

    private ushort[] ElaborateDepth(ushort[] depth) {
        /*
        for(int i = 0; i < depth.Length; i++) {
            if(depth[i] > depthRange) {
                depth[i] = 0;
            }
        }

        return depth;
        */

        int index = 0;

        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {

                if(depth[index] > depthRange || x < x_min_limit || x > x_max_limit) {
                    depth[index] = 0;
                }

                index++;
            }
        }

        depth = DecimateDepth(depth); //Per decimazione e face detection

        return depth;
    }

    private ushort[] DecimateDepth(ushort[] depth) {
        int index = 0;
        for(int i = 0; i < height; i++) {
            for(int j = 0; j < width; j++) {

                if(i < y_min_dec || i > y_max_dec) {
                    int rand = UnityEngine.Random.Range(0, decimationRate);

                    if (boundingBoxCenter != null && boundingBoxSize != null) {
                        if(Vector2.Distance(new Vector2(j, height - i), boundingBoxCenter) > faceAreaDimension) {
                            if (rand != 0) {
                                depth[index] = 0;
                            }
                        }
                    }
                }

                index++;
            }
        }

        return depth;
    }

    private byte[] CompressColorsByteToJPG(byte[] colorByteArray) { //Per comprimere in JPG dai colori grezzi ottengo una texture, dalla quale poi ottengo byte in JPG

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        texture.LoadRawTextureData(colorByteArray);
        texture.Apply();

        byte[] compressed = ImageConversion.EncodeToJPG(texture, jpgCompressionQuality); //Quality --> 1 (bassa) - 100 (alta) ==> 75 default ; 50 impercettibile ; 25 si nota ma buono per diminuzione dimensione

        Destroy(texture);

        return compressed;
    }

    private byte[] DecompressJPG(byte[] compressedColor, int originalLenght = 0) {
        /*
        byte[] decompressedColor = new byte[originalLenght];

        for(int i = 0; i < originalLenght; i++) {
            if(i < compressedColor.Length) {
                decompressedColor[i] = compressedColor[i];
            }
            else {
                decompressedColor[i] = 255;
            }
        }

        return decompressedColor;
        */
        Texture2D compTexture = new Texture2D(424, 240);
        compTexture.LoadImage(compressedColor);

        byte[] textureRaw = compTexture.GetRawTextureData();

        Destroy(compTexture);

        return textureRaw;
    }

    private void InitializeRealSense() {

        pipeline = new Pipeline();

        var config = new Config();

        using (Context cxt = new()) {
            if(cxt.QueryDevices().Count > 0) {
                config.EnableStream(Stream.Depth, width, height, Format.Z16, framerate);
                config.EnableStream(Stream.Color, width, height, Format.Rgb8, framerate);
                pipeline.Start(config);

                Debug.Log("RealSense starder correctly");

                depthData = new ushort[width * height];
            }
        }
    }

    private void OnDestroy() {
        pipeline.Dispose();
    }

    private void DepthToGrayscale(ushort[] depthImage, UnityEngine.UI.RawImage rawImage) {
        Texture2D grayscaleTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);

        Color[] pixels = new Color[width * height];

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

    private void DepthColorization(ushort[] depthImage) {
        //Si limita il range di depth

        
        for(int i = 0; i < depthImage.Length; i++) {
            if (depthImage[i] > depthRange) {
                depthImage[i] = 0;
            }
        }
        
        ushort dmin = 0;
        ushort dmax = depthRange;

        Color32[] colorizedImage = new Color32[depthImage.Length];

        for(int i = 0; i < depthImage.Length; i++) {
            
            ushort d = depthImage[i];

            float dnormal = (float)(d - dmin) / (dmax - dmin) * 1529; //Ricorda cast float --> visto che d / dmax da un numero tra 0 ed 1, altrimenti da sempre 0

            byte pr = 0;
            byte pg = 0;
            byte pb = 0;

            if (dnormal >= 0 && dnormal <= 255 || dnormal > 1275 && dnormal <= 1529) {
                pr = 255;
            }
            else if(dnormal > 255 && dnormal <= 510) {
                pr = (byte)(255 - dnormal);
            }
            else if(dnormal > 510 && dnormal <= 1020) {
                pr = 0;
            }
            else if(dnormal > 1020 && dnormal <= 1275) {
                if(dnormal - 1020 > 0)
                    pr = (byte)(dnormal - 1020);
            }

            if(dnormal > 0 && dnormal <= 255) {
                pg = (byte)dnormal;
            }
            else if(dnormal > 255 && dnormal <= 510) {
                pg = 255;
            }
            else if(dnormal > 510 && dnormal <= 765) {
                pg = (byte)(765 - dnormal);
            }
            else if(dnormal > 765 && dnormal <= 1529) {
                pg = 0;
            }

            if(dnormal > 0 && dnormal <= 765) {
                pb = 0;
            }
            else if(dnormal > 765 && dnormal <= 1020) {
                pb = (byte)(dnormal - 765);
            }
            else if(dnormal > 1020 && dnormal <= 1275) {
                pb = 255;
            }
            else if(dnormal > 1275 && dnormal <= 1529) {
                pb = (byte)(1275 - dnormal);
            }

            colorizedImage[i] = new Color32(pr, pg, pb, 255);
        }

        Texture2D test = new Texture2D(width, height, TextureFormat.RGBA32, false);

        test.SetPixels32(colorizedImage);

        test.Apply();

        testColorization.texture = test;

        //JPEG Compression

        byte[] colorizedByteArray = test.GetRawTextureData();

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.LoadRawTextureData(colorizedByteArray);
        texture.Apply();

        byte[] compressed = ImageConversion.EncodeToJPG(texture, jpgCompressionQuality); //Quality --> 1 (bassa) - 100 (alta) ==> 75 default ; 50 impercettibile ; 25 si nota ma buono per diminuzione dimensione

        Debug.Log("Compressed colorized depth : " + compressed.Length);

        Texture2D compressedTexture = new Texture2D(width, height);
        compressedTexture.LoadImage(compressed);
        compressedTexture.Apply();

        colorizationCompressed.texture = compressedTexture;

        RestoreDeptColorization(compressedTexture);
    }

    private void RestoreDeptColorization(Texture2D compressedColorization) {

        Color32[] compressedPixels = compressedColorization.GetPixels32();

        int dnormal = 0;

        int dmin = 0;
        int dmax = depthRange;

        ushort[] recoveryDepth = new ushort[compressedPixels.Length];

        //Debug.Log("Recovery depth length : " + recoveryDepth.Length);

        for(int i = 0; i < compressedPixels.Length; i++) {
            byte prr = compressedPixels[i].r;
            byte prg = compressedPixels[i].g;
            byte prb = compressedPixels[i].b;

            if(prr >= prg && prr >= prb && prg >= prb) {
                dnormal = prg - prb;
            }
            else if(prr >= prg && prr >= prb && prg < prb) {
                dnormal = prg - prb + 1529;
            }
            else if(prg >= prr && prg >= prb) {
                dnormal = prb - prr + 510;
            }
            else if(prb >= prg && prb >= prr) {
                dnormal = prr - prg + 1020;
            }

            float drecovery_fp = dmin + ((float)((dmax - dmin) * dnormal) / 1529);

            ushort drecovery = (ushort)drecovery_fp;

            recoveryDepth[i] = drecovery;
        }

        DepthToGrayscale(recoveryDepth, reconstructedDepthGrayscale);
    }
}
