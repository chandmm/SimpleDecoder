// Mostly code derived from Mp3Sharp:
using SimpleMp3Decoder.Lookups;
using SimpleMp3Decoder.Models;

namespace SimpleMp3Decoder.Intergration
{
    public class HybridComputationLogic
    {
        private SideInfoModel _sideInfo;

        public float[][][] lr;
        public float[] tsOutCopy = new float[18];
        public float[] rawout = new float[36];

        public HybridComputationLogic(SideInfoModel sideInfo)
        {
            _sideInfo = sideInfo;

            lr = new float[2][][];
            for (int i3 = 0; i3 < 2; i3++)
            {
                lr[i3] = new float[DecoderTableLookups.SUBBANDSIZE][];
                for (int i4 = 0; i4 < DecoderTableLookups.SUBBANDSIZE; i4++)
                {
                    lr[i3][i4] = new float[DecoderTableLookups.SUBBANDSAMPLESIZE];
                }
            }

            InitBlock();
        }

        private void InitBlock()
        {
            rawout = new float[36];
            tsOutCopy = new float[18];
        }

        public static int FrameId = 0;

        public void Hybrid(int gr, int ch, float[][] previousBlock)
        {
            var gr_info = _sideInfo.Granules[gr].ChannelDataModels[ch];
            int bt;
            int sb18;
            float[] tsOut;

            float[][] prvblk;

            for (sb18 = 0; sb18 < 576; sb18 += 18)
            {
                bt = ((gr_info.WindowSwitchingFlag != 0) && (gr_info.MixedBlockFlag != 0) && (sb18 < 36))
                    ? 0
                    : gr_info.BlockType;

                tsOut = gr_info.DequantisedSamples; //out_1d;
                // Modif E.B 02/22/99
                for (int cc = 0; cc < 18; cc++)
                    tsOutCopy[cc] = tsOut[cc + sb18];

                InverseMDCT(tsOutCopy, rawout, bt);

                for (int cc = 0; cc < 18; cc++)
                    tsOut[cc + sb18] = tsOutCopy[cc];
                // Fin Modif

                // overlap addition
                prvblk = previousBlock;

                tsOut[0 + sb18] = rawout[0] + prvblk[ch][sb18 + 0];
                prvblk[ch][sb18 + 0] = rawout[18];
                tsOut[1 + sb18] = rawout[1] + prvblk[ch][sb18 + 1];
                prvblk[ch][sb18 + 1] = rawout[19];
                tsOut[2 + sb18] = rawout[2] + prvblk[ch][sb18 + 2];
                prvblk[ch][sb18 + 2] = rawout[20];
                tsOut[3 + sb18] = rawout[3] + prvblk[ch][sb18 + 3];
                prvblk[ch][sb18 + 3] = rawout[21];
                tsOut[4 + sb18] = rawout[4] + prvblk[ch][sb18 + 4];
                prvblk[ch][sb18 + 4] = rawout[22];
                tsOut[5 + sb18] = rawout[5] + prvblk[ch][sb18 + 5];
                prvblk[ch][sb18 + 5] = rawout[23];
                tsOut[6 + sb18] = rawout[6] + prvblk[ch][sb18 + 6];
                prvblk[ch][sb18 + 6] = rawout[24];
                tsOut[7 + sb18] = rawout[7] + prvblk[ch][sb18 + 7];
                prvblk[ch][sb18 + 7] = rawout[25];
                tsOut[8 + sb18] = rawout[8] + prvblk[ch][sb18 + 8];
                prvblk[ch][sb18 + 8] = rawout[26];
                tsOut[9 + sb18] = rawout[9] + prvblk[ch][sb18 + 9];
                prvblk[ch][sb18 + 9] = rawout[27];
                tsOut[10 + sb18] = rawout[10] + prvblk[ch][sb18 + 10];
                prvblk[ch][sb18 + 10] = rawout[28];
                tsOut[11 + sb18] = rawout[11] + prvblk[ch][sb18 + 11];
                prvblk[ch][sb18 + 11] = rawout[29];
                tsOut[12 + sb18] = rawout[12] + prvblk[ch][sb18 + 12];
                prvblk[ch][sb18 + 12] = rawout[30];
                tsOut[13 + sb18] = rawout[13] + prvblk[ch][sb18 + 13];
                prvblk[ch][sb18 + 13] = rawout[31];
                tsOut[14 + sb18] = rawout[14] + prvblk[ch][sb18 + 14];
                prvblk[ch][sb18 + 14] = rawout[32];
                tsOut[15 + sb18] = rawout[15] + prvblk[ch][sb18 + 15];
                prvblk[ch][sb18 + 15] = rawout[33];
                tsOut[16 + sb18] = rawout[16] + prvblk[ch][sb18 + 16];
                prvblk[ch][sb18 + 16] = rawout[34];
                tsOut[17 + sb18] = rawout[17] + prvblk[ch][sb18 + 17];
                prvblk[ch][sb18 + 17] = rawout[35];
            }
        }

        /// <summary>
        ///     *
        /// </summary>
        private void doDownMix()
        {
            for (int sb = 0; sb < DecoderTableLookups.SUBBANDSAMPLESIZE; sb++)
            {
                for (int ss = 0; ss < DecoderTableLookups.SUBBANDSAMPLESIZE; ss += 3)
                {
                    lr[0][sb][ss] = (lr[0][sb][ss] + lr[1][sb][ss]) * 0.5f;
                    lr[0][sb][ss + 1] = (lr[0][sb][ss + 1] + lr[1][sb][ss + 1]) * 0.5f;
                    lr[0][sb][ss + 2] = (lr[0][sb][ss + 2] + lr[1][sb][ss + 2]) * 0.5f;
                }
            }
        }

        /// <summary>
        ///     Fast Inverse Modified discrete cosine transform.
        /// </summary>
        public void InverseMDCT(float[] inValues, float[] outValues, int blockType)
        {
            float tmpf_0, tmpf_1, tmpf_2, tmpf_3, tmpf_4, tmpf_5, tmpf_6, tmpf_7, tmpf_8, tmpf_9;
            float tmpf_10, tmpf_11, tmpf_12, tmpf_13, tmpf_14, tmpf_15, tmpf_16, tmpf_17;
            tmpf_0 = tmpf_1 = tmpf_2 = tmpf_3 = tmpf_4 = tmpf_5 = tmpf_6 = tmpf_7 =
                tmpf_8 = tmpf_9 = tmpf_10 = tmpf_11 = tmpf_12 = tmpf_13 = tmpf_14 = tmpf_15 =
                tmpf_16 = tmpf_17 = 0.0f;

            if (blockType == 2)
            {
                /*
				*
				*		Under MicrosoftVM 2922, This causes a GPF, or
				*		At best, an ArrayIndexOutOfBoundsExceptin.
				for(int p=0;p<36;p+=9)
				{
				out[p]   = out[p+1] = out[p+2] = out[p+3] =
				out[p+4] = out[p+5] = out[p+6] = out[p+7] =
				out[p+8] = 0.0f;
				}
				*/
                outValues[0] = 0.0f;
                outValues[1] = 0.0f;
                outValues[2] = 0.0f;
                outValues[3] = 0.0f;
                outValues[4] = 0.0f;
                outValues[5] = 0.0f;
                outValues[6] = 0.0f;
                outValues[7] = 0.0f;
                outValues[8] = 0.0f;
                outValues[9] = 0.0f;
                outValues[10] = 0.0f;
                outValues[11] = 0.0f;
                outValues[12] = 0.0f;
                outValues[13] = 0.0f;
                outValues[14] = 0.0f;
                outValues[15] = 0.0f;
                outValues[16] = 0.0f;
                outValues[17] = 0.0f;
                outValues[18] = 0.0f;
                outValues[19] = 0.0f;
                outValues[20] = 0.0f;
                outValues[21] = 0.0f;
                outValues[22] = 0.0f;
                outValues[23] = 0.0f;
                outValues[24] = 0.0f;
                outValues[25] = 0.0f;
                outValues[26] = 0.0f;
                outValues[27] = 0.0f;
                outValues[28] = 0.0f;
                outValues[29] = 0.0f;
                outValues[30] = 0.0f;
                outValues[31] = 0.0f;
                outValues[32] = 0.0f;
                outValues[33] = 0.0f;
                outValues[34] = 0.0f;
                outValues[35] = 0.0f;

                int six_i = 0;

                int i;
                for (i = 0; i < 3; i++)
                {
                    // 12 point IMDCT
                    // Begin 12 point IDCT
                    // Input aliasing for 12 pt IDCT
                    inValues[15 + i] += inValues[12 + i];
                    inValues[12 + i] += inValues[9 + i];
                    inValues[9 + i] += inValues[6 + i];
                    inValues[6 + i] += inValues[3 + i];
                    inValues[3 + i] += inValues[0 + i];

                    // Input aliasing on odd indices (for 6 point IDCT)
                    inValues[15 + i] += inValues[9 + i];
                    inValues[9 + i] += inValues[3 + i];

                    // 3 point IDCT on even indices6666
                    float pp1, pp2, sum;
                    pp2 = inValues[12 + i] * 0.500000000f;
                    pp1 = inValues[6 + i] * 0.866025403f;
                    sum = inValues[0 + i] + pp2;
                    tmpf_1 = inValues[0 + i] - inValues[12 + i];
                    tmpf_0 = sum + pp1;
                    tmpf_2 = sum - pp1;

                    // End 3 point IDCT on even indices
                    // 3 point IDCT on odd indices (for 6 point IDCT)
                    pp2 = inValues[15 + i] * 0.500000000f;
                    pp1 = inValues[9 + i] * 0.866025403f;
                    sum = inValues[3 + i] + pp2;
                    tmpf_4 = inValues[3 + i] - inValues[15 + i];
                    tmpf_5 = sum + pp1;
                    tmpf_3 = sum - pp1;
                    // End 3 point IDCT on odd indices
                    // Twiddle factors on odd indices (for 6 point IDCT)

                    tmpf_3 *= 1.931851653f;
                    tmpf_4 *= 0.707106781f;
                    tmpf_5 *= 0.517638090f;

                    // Output butterflies on 2 3 point IDCT's (for 6 point IDCT)
                    float save = tmpf_0;
                    tmpf_0 += tmpf_5;
                    tmpf_5 = save - tmpf_5;
                    save = tmpf_1;
                    tmpf_1 += tmpf_4;
                    tmpf_4 = save - tmpf_4;
                    save = tmpf_2;
                    tmpf_2 += tmpf_3;
                    tmpf_3 = save - tmpf_3;

                    // End 6 point IDCT
                    // Twiddle factors on indices (for 12 point IDCT)

                    tmpf_0 *= 0.504314480f;
                    tmpf_1 *= 0.541196100f;
                    tmpf_2 *= 0.630236207f;
                    tmpf_3 *= 0.821339815f;
                    tmpf_4 *= 1.306562965f;
                    tmpf_5 *= 3.830648788f;

                    // End 12 point IDCT

                    // Shift to 12 point modified IDCT, multiply by window type 2
                    tmpf_8 = -tmpf_0 * 0.793353340f;
                    tmpf_9 = -tmpf_0 * 0.608761429f;
                    tmpf_7 = -tmpf_1 * 0.923879532f;
                    tmpf_10 = -tmpf_1 * 0.382683432f;
                    tmpf_6 = -tmpf_2 * 0.991444861f;
                    tmpf_11 = -tmpf_2 * 0.130526192f;

                    tmpf_0 = tmpf_3;
                    tmpf_1 = tmpf_4 * 0.382683432f;
                    tmpf_2 = tmpf_5 * 0.608761429f;

                    tmpf_3 = -tmpf_5 * 0.793353340f;
                    tmpf_4 = -tmpf_4 * 0.923879532f;
                    tmpf_5 = -tmpf_0 * 0.991444861f;

                    tmpf_0 *= 0.130526192f;

                    outValues[six_i + 6] += tmpf_0;
                    outValues[six_i + 7] += tmpf_1;
                    outValues[six_i + 8] += tmpf_2;
                    outValues[six_i + 9] += tmpf_3;
                    outValues[six_i + 10] += tmpf_4;
                    outValues[six_i + 11] += tmpf_5;
                    outValues[six_i + 12] += tmpf_6;
                    outValues[six_i + 13] += tmpf_7;
                    outValues[six_i + 14] += tmpf_8;
                    outValues[six_i + 15] += tmpf_9;
                    outValues[six_i + 16] += tmpf_10;
                    outValues[six_i + 17] += tmpf_11;

                    six_i += 6;
                }
            }
            else
            {
                // 36 point IDCT
                // input aliasing for 36 point IDCT
                inValues[17] += inValues[16];
                inValues[16] += inValues[15];
                inValues[15] += inValues[14];
                inValues[14] += inValues[13];
                inValues[13] += inValues[12];
                inValues[12] += inValues[11];
                inValues[11] += inValues[10];
                inValues[10] += inValues[9];
                inValues[9] += inValues[8];
                inValues[8] += inValues[7];
                inValues[7] += inValues[6];
                inValues[6] += inValues[5];
                inValues[5] += inValues[4];
                inValues[4] += inValues[3];
                inValues[3] += inValues[2];
                inValues[2] += inValues[1];
                inValues[1] += inValues[0];

                // 18 point IDCT for odd indices
                // input aliasing for 18 point IDCT
                inValues[17] += inValues[15];
                inValues[15] += inValues[13];
                inValues[13] += inValues[11];
                inValues[11] += inValues[9];
                inValues[9] += inValues[7];
                inValues[7] += inValues[5];
                inValues[5] += inValues[3];
                inValues[3] += inValues[1];

                float tmp0, tmp1, tmp2, tmp3, tmp4, tmp0_, tmp1_, tmp2_, tmp3_;
                float tmp0o, tmp1o, tmp2o, tmp3o, tmp4o, tmp0_o, tmp1_o, tmp2_o, tmp3_o;

                // Fast 9 Point Inverse Discrete Cosine Transform
                //
                // By  Francois-Raymond Boyer
                //         mailto:boyerf@iro.umontreal.ca
                //         http://www.iro.umontreal.ca/~boyerf
                //
                // The code has been optimized for Intel processors
                //  (takes a lot of time to convert float to and from iternal FPU representation)
                //
                // It is a simple "factorization" of the IDCT matrix.

                // 9 point IDCT on even indices

                // 5 points on odd indices (not realy an IDCT)
                float i00 = inValues[0] + inValues[0];
                float iip12 = i00 + inValues[12];

                tmp0 = iip12 + inValues[4] * 1.8793852415718f + inValues[8] * 1.532088886238f +
                       inValues[16] * 0.34729635533386f;
                tmp1 = i00 + inValues[4] - inValues[8] - inValues[12] - inValues[12] - inValues[16];
                tmp2 = iip12 - inValues[4] * 0.34729635533386f - inValues[8] * 1.8793852415718f +
                       inValues[16] * 1.532088886238f;
                tmp3 = iip12 - inValues[4] * 1.532088886238f + inValues[8] * 0.34729635533386f -
                       inValues[16] * 1.8793852415718f;
                tmp4 = inValues[0] - inValues[4] + inValues[8] - inValues[12] + inValues[16];

                // 4 points on even indices
                float i66_ = inValues[6] * 1.732050808f; // Sqrt[3]

                tmp0_ = inValues[2] * 1.9696155060244f + i66_ + inValues[10] * 1.2855752193731f +
                        inValues[14] * 0.68404028665134f;
                tmp1_ = (inValues[2] - inValues[10] - inValues[14]) * 1.732050808f;
                tmp2_ = inValues[2] * 1.2855752193731f - i66_ - inValues[10] * 0.68404028665134f +
                        inValues[14] * 1.9696155060244f;
                tmp3_ = inValues[2] * 0.68404028665134f - i66_ + inValues[10] * 1.9696155060244f -
                        inValues[14] * 1.2855752193731f;

                // 9 point IDCT on odd indices
                // 5 points on odd indices (not realy an IDCT)
                float i0 = inValues[0 + 1] + inValues[0 + 1];
                float i0p12 = i0 + inValues[12 + 1];

                tmp0o = i0p12 + inValues[4 + 1] * 1.8793852415718f + inValues[8 + 1] * 1.532088886238f +
                        inValues[16 + 1] * 0.34729635533386f;
                tmp1o = i0 + inValues[4 + 1] - inValues[8 + 1] - inValues[12 + 1] - inValues[12 + 1] -
                        inValues[16 + 1];
                tmp2o = i0p12 - inValues[4 + 1] * 0.34729635533386f - inValues[8 + 1] * 1.8793852415718f +
                        inValues[16 + 1] * 1.532088886238f;
                tmp3o = i0p12 - inValues[4 + 1] * 1.532088886238f + inValues[8 + 1] * 0.34729635533386f -
                        inValues[16 + 1] * 1.8793852415718f;
                tmp4o = (inValues[0 + 1] - inValues[4 + 1] + inValues[8 + 1] - inValues[12 + 1] +
                         inValues[16 + 1]) * 0.707106781f; // Twiddled

                // 4 points on even indices
                float i6_ = inValues[6 + 1] * 1.732050808f; // Sqrt[3]

                tmp0_o = inValues[2 + 1] * 1.9696155060244f + i6_ + inValues[10 + 1] * 1.2855752193731f +
                         inValues[14 + 1] * 0.68404028665134f;
                tmp1_o = (inValues[2 + 1] - inValues[10 + 1] - inValues[14 + 1]) * 1.732050808f;
                tmp2_o = inValues[2 + 1] * 1.2855752193731f - i6_ - inValues[10 + 1] * 0.68404028665134f +
                         inValues[14 + 1] * 1.9696155060244f;
                tmp3_o = inValues[2 + 1] * 0.68404028665134f - i6_ + inValues[10 + 1] * 1.9696155060244f -
                         inValues[14 + 1] * 1.2855752193731f;

                // Twiddle factors on odd indices
                // and
                // Butterflies on 9 point IDCT's
                // and
                // twiddle factors for 36 point IDCT

                float e, o;
                e = tmp0 + tmp0_;
                o = (tmp0o + tmp0_o) * 0.501909918f;
                tmpf_0 = e + o;
                tmpf_17 = e - o;
                e = tmp1 + tmp1_;
                o = (tmp1o + tmp1_o) * 0.517638090f;
                tmpf_1 = e + o;
                tmpf_16 = e - o;
                e = tmp2 + tmp2_;
                o = (tmp2o + tmp2_o) * 0.551688959f;
                tmpf_2 = e + o;
                tmpf_15 = e - o;
                e = tmp3 + tmp3_;
                o = (tmp3o + tmp3_o) * 0.610387294f;
                tmpf_3 = e + o;
                tmpf_14 = e - o;
                tmpf_4 = tmp4 + tmp4o;
                tmpf_13 = tmp4 - tmp4o;
                e = tmp3 - tmp3_;
                o = (tmp3o - tmp3_o) * 0.871723397f;
                tmpf_5 = e + o;
                tmpf_12 = e - o;
                e = tmp2 - tmp2_;
                o = (tmp2o - tmp2_o) * 1.183100792f;
                tmpf_6 = e + o;
                tmpf_11 = e - o;
                e = tmp1 - tmp1_;
                o = (tmp1o - tmp1_o) * 1.931851653f;
                tmpf_7 = e + o;
                tmpf_10 = e - o;
                e = tmp0 - tmp0_;
                o = (tmp0o - tmp0_o) * 5.736856623f;
                tmpf_8 = e + o;
                tmpf_9 = e - o;

                // end 36 point IDCT */
                // shift to modified IDCT
                float[] win_bt = DecoderTableLookups.win[blockType];

                outValues[0] = -tmpf_9 * win_bt[0];
                outValues[1] = -tmpf_10 * win_bt[1];
                outValues[2] = -tmpf_11 * win_bt[2];
                outValues[3] = -tmpf_12 * win_bt[3];
                outValues[4] = -tmpf_13 * win_bt[4];
                outValues[5] = -tmpf_14 * win_bt[5];
                outValues[6] = -tmpf_15 * win_bt[6];
                outValues[7] = -tmpf_16 * win_bt[7];
                outValues[8] = -tmpf_17 * win_bt[8];
                outValues[9] = tmpf_17 * win_bt[9];
                outValues[10] = tmpf_16 * win_bt[10];
                outValues[11] = tmpf_15 * win_bt[11];
                outValues[12] = tmpf_14 * win_bt[12];
                outValues[13] = tmpf_13 * win_bt[13];
                outValues[14] = tmpf_12 * win_bt[14];
                outValues[15] = tmpf_11 * win_bt[15];
                outValues[16] = tmpf_10 * win_bt[16];
                outValues[17] = tmpf_9 * win_bt[17];
                outValues[18] = tmpf_8 * win_bt[18];
                outValues[19] = tmpf_7 * win_bt[19];
                outValues[20] = tmpf_6 * win_bt[20];
                outValues[21] = tmpf_5 * win_bt[21];
                outValues[22] = tmpf_4 * win_bt[22];
                outValues[23] = tmpf_3 * win_bt[23];
                outValues[24] = tmpf_2 * win_bt[24];
                outValues[25] = tmpf_1 * win_bt[25];
                outValues[26] = tmpf_0 * win_bt[26];
                outValues[27] = tmpf_0 * win_bt[27];
                outValues[28] = tmpf_1 * win_bt[28];
                outValues[29] = tmpf_2 * win_bt[29];
                outValues[30] = tmpf_3 * win_bt[30];
                outValues[31] = tmpf_4 * win_bt[31];
                outValues[32] = tmpf_5 * win_bt[32];
                outValues[33] = tmpf_6 * win_bt[33];
                outValues[34] = tmpf_7 * win_bt[34];
                outValues[35] = tmpf_8 * win_bt[35];
            }
        }

        public static float[] create_t_43()
        {
            float[] t43 = new float[8192];
            float d43 = (4.0f / 3.0f);

            for (int i = 0; i < 8192; i++)
            {
                t43[i] = (float)Math.Pow(i, d43);
            }
            return t43;
        }
    }
}
