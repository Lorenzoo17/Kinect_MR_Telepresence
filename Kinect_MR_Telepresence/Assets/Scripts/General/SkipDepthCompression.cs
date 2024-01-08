using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CompressionUtilities {
    public static class SkipDepthCompression {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="raw_depth"> Raw depth image to compress</param>
        /// <param name="jump"> Number of pixels to jump before a new sampling </param>
        /// <returns>Compressed depth</returns>
        public static ushort[] SkipDepthCompress(ushort[] raw_depth, int jump) {
            List<ushort> compressedDepth = new List<ushort>();

            int lenght = raw_depth.Length;
            int jumped = 0; //current pixel jumped

            for (int i = 0; i < lenght; i++) {
                if (jumped != jump && jump != 0 && i != 0) {
                    jumped++;
                }
                else {
                    compressedDepth.Add(raw_depth[i]);
                    jumped = 0;
                }
            }

            return compressedDepth.ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="compressed_depth"> Compressed depth image to decompress </param>
        /// <param name="jump"> Number of pixels to jump before a new sampling </param>
        /// <param name="width"> Original depth width </param>
        /// <param name="height"> Original depth height </param>
        /// <returns>Decompressed depth</returns>
        public static ushort[] SkipDepthDecompress(ushort[] compressed_depth, int jump, int width, int height, bool interpolation) {

            ushort[] decompressedDepth = new ushort[width * height];

            int jumped = 0;
            int compressedIndex = 0;

            for (int i = 0; i < width * height; i++) {
                if (jumped == jump || i == 0) {
                    decompressedDepth[i] = compressed_depth[compressedIndex];
                    compressedIndex++;
                    jumped = 0;
                }
                else {
                    decompressedDepth[i] = 0;
                    jumped++;
                }
            }

            if (interpolation) {
                int width_index = 0;
                bool found_first = false;

                ushort first = 0;
                for (int i = 0; i < (width * height); i++) {
                    if (width_index < width) {

                        if (decompressedDepth[i] != 0 && !found_first) {
                            found_first = true;
                            first = decompressedDepth[i];
                        }
                        else if (decompressedDepth[i] != 0 && found_first) {

                            ushort second = decompressedDepth[i];

                            ushort interpolationValue = (ushort)((second + first) / 2);
                            for (int j = i - jump; j < i; j++) {
                                decompressedDepth[j] = interpolationValue;
                            }

                            found_first = false;
                        }
                        else {
                            //decompressedDepth è 0, quindi si va avanti
                        }

                        width_index++;
                    }
                    else {
                        width_index = 0;
                        found_first = false;
                    }
                }
            }

            return decompressedDepth;
        }

        /*

        public static ushort[] AlternateDepthCompress(ushort[] raw_depth, int rowsToJump, int width, int height) {
            List<ushort> compressedDepth = new List<ushort>();

            int width_index = 0;
            int jumpCounter = 0;
            bool toJump = false;
            for(int i = 0; i < raw_depth.Length; i++) {
                if(width_index < width) {
                    if (!toJump) {
                        compressedDepth.Add(raw_depth[i]);
                    }

                    width_index++;
                }
                else {
                    jumpCounter++;
                    width_index = 0;
                    toJump = !toJump;
                    jumpCounter = 0;
                }
            }

            return compressedDepth.ToArray();
        }

        public static ushort[] AlternateDepthDeCompress(ushort[] compressedDepth, int rowsToJump, int width, int height, bool interpolation) {
            ushort[] decompressedDepth = new ushort[width * height];

            int width_index = 0;
            bool toJump = false;
            int jumpCounter = 0;
            int compressedIndex = 0;
            for (int i = 0; i < (width * height); i++) {
                if (width_index < width) {
                    if (!toJump) {
                        decompressedDepth[i] = compressedDepth[compressedIndex];
                        compressedIndex++;
                    }
                    else {
                        decompressedDepth[i] = 0;
                    }

                    width_index++;
                }
                else {
                    jumpCounter++;
                    width_index = 0;
                    toJump = !toJump;
                    jumpCounter = 0;
                }
            }

            ushort[] interpolatedDepth = new ushort[width * height];
            if (interpolation) {
                width_index = 0;
                toJump = false;
                for (int i = 0; i < (width * height); i++) {
                    if (width_index < width) {
                        if (!toJump) {
                            interpolatedDepth[i] = decompressedDepth[i];
                        }
                        else {
                            if (i > width && i < (width * height) - width) {
                                ushort prevValue = decompressedDepth[i - width];
                                ushort nextValue = decompressedDepth[i + width];

                                interpolatedDepth[i] = (ushort)((nextValue + prevValue) / 2);
                            }
                        }

                        width_index++;
                    }
                    else {
                        width_index = 0;
                        toJump = !toJump;
                        jumpCounter = 0;
                    }
                }
            }

            return (interpolation) ? interpolatedDepth : decompressedDepth;
        }

        */

        public static ushort[] AlternateDepthCompress(ushort[] raw_depth, int rowsToJump, int width, int height) {
            List<ushort> compressedDepth = new List<ushort>();

            int index = 0;
            int jumpCounter = 0;
            for (int y = 0; y < height; y++) { //Ciclo fra le varie righe                                  
                if (jumpCounter == rowsToJump) jumpCounter = 0; //Resetto jumpCounter a 0 (salvo la riga solo quando jumpCounter è a 0)
                for (int x = 0; x < width; x++) { //Ciclo sui pixel della riga y-esima
                    if (jumpCounter == 0) { //Se jump counter è a 0 salvo i pixel della riga y-esima, altrimenti salto l'intera riga
                        compressedDepth.Add(raw_depth[index]);
                    }
                    index++;
                }
                jumpCounter++;
            }

            return compressedDepth.ToArray();
        }

        public static ushort[] AlternateDepthCompressFaceDetection(ushort[] raw_depth, int rowsToJump, int width, int height, Vector2 bb_center, int faceAreaSize) {
            List<ushort> compressedDepth = new List<ushort>();

            int index = 0;
            int jumpCounter = 0;
            for (int y = 0; y < height; y++) { //Ciclo fra le varie righe                                  
                if (jumpCounter == rowsToJump) jumpCounter = 0; //Resetto jumpCounter a 0 (salvo la riga solo quando jumpCounter è a 0)
                for (int x = 0; x < width; x++) { //Ciclo sui pixel della riga y-esima
                    if(bb_center != null && Vector2.Distance(new Vector2(x, height - y), bb_center) < faceAreaSize) {
                        compressedDepth.Add(raw_depth[index]);
                    }
                    else if (jumpCounter == 0) { //Se jump counter è a 0 salvo i pixel della riga y-esima, altrimenti salto l'intera riga
                        compressedDepth.Add(raw_depth[index]);
                    }
                    index++;
                }
                jumpCounter++;
            }

            return compressedDepth.ToArray();
        }

        public static ushort[] AlternateDepthDeCompress(ushort[] compressedDepth, int rowsToJump, int width, int height, bool interpolation) {
            ushort[] decompressedDepth = new ushort[width * height]; 

            int index = 0;
            int compressedIndex = 0;
            int jumpCounter = 0;
            //In questo primo creo un array (depth image) sulla base dell'array ottenuto in AlternateDepthCompress, visto che in questo array ho saltato qualche riga, ed è di dimensione minore di w*h, vado a creare
            //un nuovo array e vado a riempire le righe saltate con degli 0
            for (int y = 0; y < height; y++) {
                if (jumpCounter == rowsToJump) jumpCounter = 0;
                for(int x = 0; x < width; x++) {

                    if(jumpCounter == 0) {
                        decompressedDepth[index] = compressedDepth[compressedIndex];
                        compressedIndex++;
                    }
                    else {
                        decompressedDepth[index] = 0;
                    }

                    index++;
                }
                jumpCounter++;
            }

            ushort[] interpolatedDepth = new ushort[width * height];
            //Qui invece eseguo un interpolazione tra le varie righe --> Ovvero prendo le righe saltate e le riempio con i valori di interpolazione tra le righe che non sono state saltate
            if (interpolation) {
                index = 0;
                jumpCounter = 0;

                for (int y = 0; y < height; y++) {
                    if (jumpCounter == rowsToJump) jumpCounter = 0;
                    for (int x = 0; x < width; x++) {

                        if (jumpCounter == 0) { //La riga non saltata la salvo così com'è
                            interpolatedDepth[index] = decompressedDepth[index];
                        }
                        else { //Se capito su una riga saltata
                            if(index - (width * jumpCounter) > 0 && index + (width * (rowsToJump - jumpCounter)) < (width * height) - (width * (rowsToJump - jumpCounter))) { //Per non andare oltre i limiti dell'array
                                int prevIndex = index - (width * jumpCounter);
                                int nextIndex = index + (width * (rowsToJump - jumpCounter));
                                ushort prevValue = decompressedDepth[prevIndex]; //Valore precedente --> ultima riga non saltata --> quindi appunto  : indice - (width * jumpCounter)
                                ushort nextValue = decompressedDepth[nextIndex]; //Valore successivo --> prossima riga non saltata --> indice + (width * (rowsToJump - jumpCounter))
                                ushort interpolatedValue = 0;

                                
                                if (!(prevValue == 0 || nextValue == 0)) { //Se sia prevValue che nextValue sono diversi da 0!!! 
                                    //Per interpolazione costante
                                    //interpolatedValue = (ushort)Mathf.Abs(((nextValue + prevValue) / 2)); //assegno a quel pixel della riga y-esima il valore di interpolazione tra il pixel precedente e successivo

                                    //Interpolazione lineare --> x = (x0 + (y-y0) * (x1-x0) / (y1-y0)) --> dove x0 = prevValue ; y = index ; y0 = index - (width * jumpCounter) ; x1 = nextValue ; y1 = (index + (width * (rowsToJump - jumpCounter)) 
                                    interpolatedValue = (ushort)(prevValue + ((index - prevIndex)) * (nextValue - prevValue) / (nextIndex - prevIndex));
                                }

                                interpolatedDepth[index] = interpolatedValue;
                            }
                            else {
                                interpolatedDepth[index] = 0;
                            }
                        }

                        index++;
                    }
                    jumpCounter++;
                }
            }

            return (interpolation)? interpolatedDepth : decompressedDepth;
        }

        public static ushort[] AlternateDepthDeCompressFaceDetection(ushort[] compressedDepth, int rowsToJump, int width, int height, bool interpolation, Vector2 bb_center, int faceAreaSize) {
            ushort[] decompressedDepth = new ushort[width * height];

            int index = 0;
            int compressedIndex = 0;
            int jumpCounter = 0;
            //In questo primo creo un array (depth image) sulla base dell'array ottenuto in AlternateDepthCompress, visto che in questo array ho saltato qualche riga, ed è di dimensione minore di w*h, vado a creare
            //un nuovo array e vado a riempire le righe saltate con degli 0

            for (int y = 0; y < height; y++) {
                if (jumpCounter == rowsToJump) jumpCounter = 0;
                for (int x = 0; x < width; x++) {
                    if (bb_center != null && Vector2.Distance(new Vector2(x, height - y), bb_center) < faceAreaSize) {
                        decompressedDepth[index] = compressedDepth[compressedIndex];
                        compressedIndex++;
                    }
                    else {
                        if (jumpCounter == 0) {
                            decompressedDepth[index] = compressedDepth[compressedIndex];
                            compressedIndex++;
                        }
                        else {
                            decompressedDepth[index] = 0;
                        }
                    }

                    index++;
                }
                jumpCounter++;
            }

            ushort[] interpolatedDepth = new ushort[width * height];
            //Qui invece eseguo un interpolazione tra le varie righe --> Ovvero prendo le righe saltate e le riempio con i valori di interpolazione tra le righe che non sono state saltate
            if (interpolation) {
                index = 0;
                jumpCounter = 0;

                for (int y = 0; y < height; y++) {
                    if (jumpCounter == rowsToJump) jumpCounter = 0;
                    for (int x = 0; x < width; x++) {
                        if (bb_center != null && Vector2.Distance(new Vector2(x, height - y), bb_center) < faceAreaSize) {
                            interpolatedDepth[index] = decompressedDepth[index];
                        }
                        else {
                            if (jumpCounter == 0) { //La riga non saltata la salvo così com'è
                                interpolatedDepth[index] = decompressedDepth[index];
                            }
                            else { //Se capito su una riga saltata
                                if (index - (width * jumpCounter) > 0 && index + (width * (rowsToJump - jumpCounter)) < (width * height) - (width * (rowsToJump - jumpCounter))) { //Per non andare oltre i limiti dell'array
                                    int prevIndex = index - (width * jumpCounter);
                                    int nextIndex = index + (width * (rowsToJump - jumpCounter));
                                    ushort prevValue = decompressedDepth[prevIndex]; //Valore precedente --> ultima riga non saltata --> quindi appunto  : indice - (width * jumpCounter)
                                    ushort nextValue = decompressedDepth[nextIndex]; //Valore successivo --> prossima riga non saltata --> indice + (width * (rowsToJump - jumpCounter))
                                    ushort interpolatedValue = 0;


                                    if (!(prevValue == 0 || nextValue == 0)) { //Se sia prevValue che nextValue sono diversi da 0!!! 

                                        //Interpolazione lineare --> x = (x0 + (y-y0) * (x1-x0) / (y1-y0)) --> dove x0 = prevValue ; y = index ; y0 = index - (width * jumpCounter) ; x1 = nextValue ; y1 = (index + (width * (rowsToJump - jumpCounter)) 
                                        interpolatedValue = (ushort)(prevValue + ((index - prevIndex)) * (nextValue - prevValue) / (nextIndex - prevIndex));
                                    }

                                    interpolatedDepth[index] = interpolatedValue;
                                }
                                else {
                                    interpolatedDepth[index] = 0;
                                }
                            }
                        }

                        index++;
                    }
                    jumpCounter++;
                }
            }

            return (interpolation) ? interpolatedDepth : decompressedDepth;
        }
    }

}