using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.IO.Compression;
using Microsoft.SqlServer.Server;
using System.Runtime.InteropServices;
using System;

namespace CompressionUtilities
{
    public static class DataCompression
    {
        // Used to convert the audio clip float array to bytes
        public static byte[] ToByteArray(float[] floatArray)
        {
            int len = floatArray.Length * 4;
            byte[] byteArray = new byte[len];
            int pos = 0;
            foreach (float f in floatArray)
            {
                byte[] data = System.BitConverter.GetBytes(f);
                System.Array.Copy(data, 0, byteArray, pos, 4);
                pos += 4;
            }
            return byteArray;
        }
        // Used to convert the byte array to float array for the audio clip
        public static float[] ToFloatArray(byte[] byteArray)
        {
            int len = byteArray.Length / 4;
            float[] floatArray = new float[len];
            for (int i = 0; i < byteArray.Length; i += 4)
            {
                floatArray[i / 4] = System.BitConverter.ToSingle(byteArray, i);
            }
            return floatArray;
        }

        // -------- DEFLATE COMPRESSION ------------
        //Deflate è come GZip, con la differenza che quest'ultimo metodo di compressione aggiunge un CRC ulteriore
        public static byte[] DeflateCompress(byte[] data)
        {
            MemoryStream output = new MemoryStream();
            using (DeflateStream dstream = new DeflateStream(output, System.IO.Compression.CompressionLevel.Optimal))
            {
                dstream.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }

        public static byte[] DeflateDecompress(byte[] data)
        {
            MemoryStream input = new MemoryStream(data);
            MemoryStream output = new MemoryStream();
            using (DeflateStream dstream = new DeflateStream(input, CompressionMode.Decompress))
            {
                dstream.CopyTo(output);
            }
            return output.ToArray();
        }

        // ----------- BROTLI COMPRESSION ------------
        // Comprime maggiormente ma è più pesante a livello di risorse hardware richieste --> velocità di compressione maggiore causa lag nella trasmissione lato trasmittente
        public static byte[] BrotliCompress(byte[] data)
        {
            MemoryStream output = new();
            using (BrotliStream bstream = new BrotliStream(output, System.IO.Compression.CompressionLevel.Optimal))
            {
                bstream.Write(data, 0, data.Length);
            }

            return output.ToArray();
        }

        public static byte[] BrotliDecompress(byte[] data)
        {
            MemoryStream input = new MemoryStream(data);
            MemoryStream output = new();
            using (BrotliStream bstream = new BrotliStream(input, CompressionMode.Decompress))
            {
                bstream.CopyTo(output);
            }

            return output.ToArray();
        }

        // --------- CONVERT COLOR32 ARRAY TO BYTE ARRAY ---------

        public static byte[] Color32ArrayToByteArray(Color32[] colors)
        {
            if (colors == null || colors.Length == 0)
                return null;

            int lengthOfColor32 = Marshal.SizeOf(typeof(Color32));
            int length = lengthOfColor32 * colors.Length;
            byte[] bytes = new byte[length];

            GCHandle handle = default(GCHandle);
            try
            {
                handle = GCHandle.Alloc(colors, GCHandleType.Pinned);
                IntPtr ptr = handle.AddrOfPinnedObject();
                Marshal.Copy(ptr, bytes, 0, length);
            }
            finally
            {
                if (handle != default(GCHandle))
                    handle.Free();
            }

            return bytes;
        }
        public static Color32[] ByteArrayToColor32(byte[] byteArray)
        {
            var colorArray = new Color32[byteArray.Length/4];
            for(var i = 0; i < byteArray.Length; i+=4)
            {
                var color = new Color32(byteArray[i + 0], byteArray[i + 1], byteArray[i + 2],byteArray[i + 3]);
                colorArray[i/4] = color;
            }

            return colorArray;
        }
    }
}
