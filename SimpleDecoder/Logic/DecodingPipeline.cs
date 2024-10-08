/*
    SimpleDecoder mp3 decoding library build for educational purposes using derived works, AI, and self learning.
    Copyright (C) 2024  Michael Chand.

    This library is free software; you can redistribute it and/or
    modify it under the terms of the GNU Lesser General Public
    License as published by the Free Software Foundation; either
    version 2.1 of the License, or (at your option) any later version.

    This library is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
    Lesser General Public License for more details.

    You should have received a copy of the GNU Lesser General Public
    License along with this library; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301
    USA
*/

/*
 * Deravitive works:
 * Parts of the code is derived work from Mp3Sharp.
 */

using SimpleMp3Decoder.Lookups.Models;
using SimpleMp3Decoder.Lookups;
using SimpleMp3Decoder.Models;
using SimpleMp3Decoder.Intergration;
using SimpleMp3Decoder.Data;
using System.Drawing;
using System.Text;
using SimpleMp3Decoder.Logic.PipelineStages;

namespace SimpleMp3Decoder.Logic
{
    public class DecodingPipeline
    {
        #region Fields

        private HeaderModel _header;
        private double[] _samples1;
        private double[] _samples2;
        private PcmBuffer _buffer;
        private FrameModel _frame;
        private FileStream _stream;


        private static bool _isSeekingMainDataBeginZero = true;

        public static SynthesisFilter Filter1;
        public static SynthesisFilter Filter2;

        #endregion

        #region Properties

        public SideInfoModel SideInfo { get; private set; } = new();
        public List<int> SingleDimensionSpectralDataPoints { get; private set; } = new List<int>();

        public PcmBuffer Buffer => _buffer;

        private Equaliser _equaliser;
        public Equaliser Equaliser
        {
            set
            {
                if (value == null)
                    value = Equaliser.PASS_THRU_EQ;

                _equaliser.FromEqualizer = value;

                double[] factors = _equaliser.BandFactors;
                if (Filter1 != null)
                    Filter1.EQ = factors;

                if (Filter2 != null)
                    Filter2.EQ = factors;
            }
        }

        public bool IsFrameHealthy = true;

        public static float[][] Prevblck = new float[2][];

        #endregion

        #region Initialisation

        public DecodingPipeline(FileStream stream, FrameModel frame)
        {
            _header = frame.Header;
            _frame = frame;
            _stream = stream;

            _samples1 = new double[32];
            _samples2 = new double[32];
        }

        #endregion

        public void Decode()
        {
            Exception sideInfoException = null;

            try
            {
                DecodeSideInfo();
            }
            catch(Exception exception)
            {
                sideInfoException = exception;
            }

            try
            {
                var mainDataSize = _frame.Header.GetFrameSize() - (4 + (HeaderInfoUtils.GetNumberOfChannels(_header) == 1 ? 17 : 32) + (_header.ProtectionBit == 0 ? 2 : 0));
                var bytes = new byte[mainDataSize];

                _stream.Position = _frame.FramePosition + 4 + ((HeaderInfoUtils.GetNumberOfChannels(_header) == 1 ? 17 : 32) + (_header.ProtectionBit == 0 ? 2 : 0));
                _stream.Read(bytes);
                DecoderUtils.FrameBufferStream.PushRange(bytes);

                if (sideInfoException != null)
                {
                    throw new Exception(sideInfoException.Message, sideInfoException);
                }

                _buffer = _buffer ?? new PcmBuffer(HeaderInfoUtils.GetNumberOfChannels(_header));

                DecodeFrame();
            }
            catch (Exception)
            {
                _isSeekingMainDataBeginZero = true;

                DecoderUtils.AudioDataBufferStream?.Clear();
                IsFrameHealthy = false;
            }
        }

        // When seeking, we dont want full decode but at the seeked to frame, we want to keep the bit reservoir intact.
        // Virtualising the decode where only enough frame data is read to maintain the resoirvoir is fairly fast if we are to still keep things simple.
        // There are faster methods of course and is a TODO for now.
        public void VirtualisedDecode()
        {
            try
            {
                var mainDataSize = _frame.Header.GetFrameSize() - (4 + (HeaderInfoUtils.GetNumberOfChannels(_header) == 1 ? 17 : 32) + (_header.ProtectionBit == 0 ? 2 : 0));
                var bytes = new byte[mainDataSize];

                _stream.Position = _frame.FramePosition + 4 + ((HeaderInfoUtils.GetNumberOfChannels(_header) == 1 ? 17 : 32) + (_header.ProtectionBit == 0 ? 2 : 0));
                _stream.Read(bytes);
                DecoderUtils.FrameBufferStream.PushRange(bytes);

                _buffer = _buffer ?? new PcmBuffer(HeaderInfoUtils.GetNumberOfChannels(_header));

                VirtualisedDecodeFrame();
            }
            catch (Exception)
            {
                _isSeekingMainDataBeginZero = true;

                DecoderUtils.AudioDataBufferStream?.Clear();
                IsFrameHealthy = false;
            }
        }

        internal void GetBitReservoirBytes(List<FrameModel> frames, int offset)
        {
            for (int i = 0; i < offset; i++ )
            {
                frames.ElementAt(i).GetDecodingPipeline().VirtualisedDecode();
            }
        }

        internal void DecodeWithSeek(List<FrameModel> frames, int offset) => GetBitReservoirBytes(frames, offset);

        // TODO: Add CRC check.
        public void BypassCRCBytes()
        {
            // Skip over CRC bytes

            if (_header.ProtectionBit == 0)
            {
                DecoderUtils.FrameBufferStream.PopRange(2);
            }
        }

        internal void DecodeSideInfo()
        {
            _stream.Position = _frame.FramePosition + 4;

            var bytes = new byte[(HeaderInfoUtils.GetNumberOfChannels(_header) == 1 ? 17 : 32) + (_header.ProtectionBit == 0 ? 2 : 0)];
            _stream.Read(bytes);

            DecoderUtils.FrameBufferStream.PushRange(bytes);

            BypassCRCBytes();

            DecodeSideInfoInternal();
        }

        private void VirtualisedDecodeFrame()
        {
            if (DecoderUtils.AudioDataBufferStream != null)
            {
                DecoderUtils.AudioDataBufferStream.AlignToByteBoundary();
            }

            var invalidFrameByteCountToRemove = DecoderUtils.AudioDataBufferStream == null ? 0 : (DecoderUtils.AudioDataBufferStream.Count) - SideInfo.MainDataBegin;

            if (invalidFrameByteCountToRemove > 0)
            {
                _ = DecoderUtils.AudioDataBufferStream?.PopRange(invalidFrameByteCountToRemove);
            }

            if (_isSeekingMainDataBeginZero
                && SideInfo.MainDataBegin != 0)
            {
                IsFrameHealthy = false;
                return;
            }

            _isSeekingMainDataBeginZero = false;

            DecoderUtils.UpdateAudioDataBufferStream(DecoderUtils.FrameBufferStream.PopToNewBufferStream());

            DecoderUtils.AudioDataBufferStream.PopBits(GetMainDataActualSize());
        }

        internal void DecodeFrame()
        {
            if (DecoderUtils.AudioDataBufferStream != null)
            {
                DecoderUtils.AudioDataBufferStream.AlignToByteBoundary();
            }

            var invalidFrameByteCountToRemove = DecoderUtils.AudioDataBufferStream == null ? 0 : (DecoderUtils.AudioDataBufferStream.Count) - SideInfo.MainDataBegin;

            if (invalidFrameByteCountToRemove > 0)
            {
                _ = DecoderUtils.AudioDataBufferStream?.PopRange(invalidFrameByteCountToRemove);
            }

            if (_isSeekingMainDataBeginZero
                && SideInfo.MainDataBegin != 0)
            {
                IsFrameHealthy = false;
                return;
            }

            _isSeekingMainDataBeginZero = false;

            if (invalidFrameByteCountToRemove < 0)
            {
                DecoderUtils.UpdateAudioDataBufferStream(DecoderUtils.FrameBufferStream.PopToNewBufferStream());

                return;
            }

            DecoderUtils.UpdateAudioDataBufferStream(DecoderUtils.FrameBufferStream.PopToNewBufferStream());

            for (int gr = 0; gr < 2; gr++)
            {
                var hybrid = new HybridComputationLogic(SideInfo);

                for (int ch = 0; ch < SideInfo.Granules[gr].NumberOfChannels; ch++)
                {
                    DecoderUtils.AudioDataBufferStream.Begin();

                    DecodeScaleFactors(gr, ch);
                    DecodeHuffman(gr, ch);
                    OverScanCorrection(gr, ch, DecoderUtils.AudioDataBufferStream.End());
                    DequantiseSample(gr, ch);

                    SingleDimensionSpectralDataPoints = new List<int>();  // finished processing this channel so we dont need this channels spectral data anymore. Ready for next channel
                }

                StereoProcessing(SideInfo, _header, gr);

                int sb = 0;
                for (int ch = 0; ch < SideInfo.Granules[gr].NumberOfChannels; ch++)
                {
                    Reorder(gr, ch);
                    AliasReduction(SideInfo.Granules[gr].ChannelDataModels[ch].DequantisedSamples);
                    HybridComputationLogic.FrameId = _frame.FrameId;
                    hybrid.Hybrid(gr, ch, Prevblck);
                    FrequencyInversion(SideInfo.Granules[gr].ChannelDataModels[ch].DequantisedSamples);

                    if (ch == 0)
                    {
                        for (int ss = 0; ss < DecoderTableLookups.SUBBANDSAMPLESIZE; ss++)
                        {
                            sb = 0;
                            for (int sb18 = 0; sb18 < 576; sb18 += 18)
                            {
                                _samples1[sb] = SideInfo.Granules[gr].ChannelDataModels[ch].DequantisedSamples[sb18 + ss];

                                sb++;
                            }

                            Filter1.WriteAllSamples(_samples1);
                            Filter1.calculate_pcm_samples(_buffer);
                        }
                    }
                    else
                    {
                        for (int ss = 0; ss < DecoderTableLookups.SUBBANDSAMPLESIZE; ss++)
                        {
                            sb = 0;
                            for (int sb18 = 0; sb18 < 576; sb18 += 18)
                            {
                                var data = SideInfo.Granules[gr].ChannelDataModels[ch].DequantisedSamples[sb18 + ss];
                                _samples2[sb] = SideInfo.Granules[gr].ChannelDataModels[ch].DequantisedSamples[sb18 + ss];
                                sb++;
                            }

                            Filter2.WriteAllSamples(_samples2);
                            Filter2.calculate_pcm_samples(_buffer);

                        }
                    }
                }
            }
        }

        private void OverScanCorrection(int gr, int ch, int totalBits)
        {
            DiscardUnusedBits(gr, ch, totalBits);

            if (totalBits > SideInfo.Granules[gr].ChannelDataModels[ch].Part2_3_Length)
            {
                DecoderUtils.AudioDataBufferStream.RestorePopBits(totalBits - SideInfo.Granules[gr].ChannelDataModels[ch].Part2_3_Length);
            }
        }

        #region Stereo processing Pipeline

        public static void StereoProcessing(SideInfoModel sideInfo, HeaderModel header, int granule)
        {
            if (HeaderInfoUtils.GetNumberOfChannels(header) == 1)
            {
                // No stereo processing for single channels.
                return;
            }

            float[] left = new float[576];
            float[] right = new float[576];
            int[] iStereoPanningPosition = new int[576];
            float[] is_ratio = new float[576];
            bool midSideStereo = HeaderInfoUtils.GetModeExtensionMsStereo(header);
            bool intensityStereo = HeaderInfoUtils.GetModeExtensionIntensityStereo(header);
            var modeExtension = HeaderInfoUtils.GetModeExtensionType(header);
            bool lsf = false; // SZD
            float[][] k = new float[2][];

            var dequantisedSamplesLeft = sideInfo.Granules[granule].ChannelDataModels[0].DequantisedSamples;
            var dequantisedSamplesRight = sideInfo.Granules[granule].ChannelDataModels[1].DequantisedSamples;

            if (intensityStereo)
            {
                k[0] = new float[576];
                k[1] = new float[576];

                Array.Fill(iStereoPanningPosition, 7);

                IntensityStereoPreProcessing(header, sideInfo, granule, iStereoPanningPosition, is_ratio, k);
            }

            switch (modeExtension)
            {
                case ModeExtensionType.MSStereo:
                    MidSideStereoProcessing(dequantisedSamplesLeft, dequantisedSamplesRight, left, right, stereoProcessingOff: false);
                    break;
                case ModeExtensionType.Off:
                    StereoModeExtensionOff(dequantisedSamplesLeft, dequantisedSamplesRight, ref left, ref right);
                    break;
                case ModeExtensionType.IntensityStereo:
                    IntensityStereoPostProcessing(dequantisedSamplesLeft, dequantisedSamplesRight, left, right, iStereoPanningPosition, is_ratio, k, lsf);
                    break;
                case ModeExtensionType.MS_IS_Stereo:
                    MSISStereoProcessing(dequantisedSamplesLeft, dequantisedSamplesRight, left, right, iStereoPanningPosition, is_ratio, k, midSideStereo, intensityStereo, lsf);
                    break;
                default:
                    break;
            }

            sideInfo.Granules[granule].ChannelDataModels[0].DequantisedSamples = left;
            sideInfo.Granules[granule].ChannelDataModels[1].DequantisedSamples = right;
        }

        private static void StereoModeExtensionOff(float[] dequantisedSamplesLeft, float[] dequantisedSamplesRight, ref float[] left, ref float[] right)
        {
            left = dequantisedSamplesLeft;
            right = dequantisedSamplesRight;
        }

        private static void MidSideStereoProcessingFromIndex(float[] dequantisedSamplesLeft, float[] dequantisedSamplesRight, float[] left, float[] right, int bandIndex)
        {
            left[bandIndex] = (dequantisedSamplesLeft[bandIndex] + dequantisedSamplesRight[bandIndex]) * DecoderTableLookups.NormaliseFactor;
            right[bandIndex] = (dequantisedSamplesLeft[bandIndex] - dequantisedSamplesRight[bandIndex]) * DecoderTableLookups.NormaliseFactor;
        }

        private static void MidSideStereoProcessing(float[] dequantisedSamplesLeft, float[] dequantisedSamplesRight, float[] left, float[] right, bool stereoProcessingOff)
        {
            var bandSize = DecoderTableLookups.SUBBANDSIZE * DecoderTableLookups.SUBBANDSAMPLESIZE;

            for (int index = 0; index < bandSize; index++)
            {
                MidSideStereoProcessingFromIndex(dequantisedSamplesLeft, dequantisedSamplesRight, left, right, index);
            }
        }

        private static void MSISStereoProcessing(float[] dequantisedSamplesLeft, float[] dequantisedSamplesRight, float[] left, float[] right, int[] iStereoPanningPosition, float[] is_ratio, float[][] k, bool midSideStereo, bool intensityStereo, bool lsf)
        {
            var bandSize = DecoderTableLookups.SUBBANDSIZE * DecoderTableLookups.SUBBANDSAMPLESIZE;

            for (int index = 0; index < bandSize; index++)
            {
                if (iStereoPanningPosition[index] == 7)
                {
                    MidSideStereoProcessingFromIndex(dequantisedSamplesLeft, dequantisedSamplesRight, left, right, index);
                }
                else
                {
                    IntensityStereoPostProcessingFromIndex(dequantisedSamplesLeft, dequantisedSamplesRight, left, right, iStereoPanningPosition, is_ratio, k, lsf, index);
                }
            }
        }

        private static void IntensityStereoPostProcessingFromIndex(float[] dequantisedSamplesLeft, float[] dequantisedSamplesRight, float[] left, float[] right, int[] iStereoPanningPosition, float[] is_ratio, float[][] k, bool lsf, int index)
        {
            if (iStereoPanningPosition[index] == 7)
            {
                left[index] = dequantisedSamplesLeft[index];
                right[index] = dequantisedSamplesRight[index];
            }
            else
            {
                right[index] = lsf
                    ? right[index] = dequantisedSamplesLeft[index] * k[1][index]
                    : right[index] = dequantisedSamplesLeft[index] / (1 + is_ratio[index]);

                left[index] = lsf
                    ? dequantisedSamplesLeft[index] * k[0][index]
                    : left[index] = right[index] * is_ratio[index];
            }
        }

        private static void IntensityStereoPostProcessing(float[] dequantisedSamplesLeft, float[] dequantisedSamplesRight, float[] left, float[] right, int[] iStereoPanningPosition, float[] is_ratio, float[][] k, bool lsf)
        {
            var bandSize = DecoderTableLookups.SUBBANDSIZE * DecoderTableLookups.SUBBANDSAMPLESIZE;

            for (int index = 0; index < bandSize; index++)
            {
                IntensityStereoPostProcessingFromIndex(dequantisedSamplesLeft, dequantisedSamplesRight, left, right, iStereoPanningPosition, is_ratio, k, lsf, index);
            }
        }

        private static void IntensityStereoKValues(float[][] k, int iStereoPanningPosition, int io_type, int i)
        {
            int index = (iStereoPanningPosition + 1) / 2; // Calculate the index directly

            if (iStereoPanningPosition == 0)
            {
                k[0][i] = 1.0f;
                k[1][i] = 1.0f;
            }
            else if (iStereoPanningPosition % 2 != 0) // Odd iStereoPanningPosition
            {
                k[0][i] = DecoderTableLookups.IntensityStereoGainTable[io_type][index];
                k[1][i] = 1.0f;
            }
            else // Even iStereoPanningPosition
            {
                k[0][i] = 1.0f;
                k[1][i] = DecoderTableLookups.IntensityStereoGainTable[io_type][index - 1];
            }
        }

        #region Intensity Stereo pre processing code.

        private static void IntensityStereoPreProcessing(HeaderModel header, SideInfoModel sideInfo, int granule, int[] iStereoPanningPosition, float[] is_ratio, float[][] k)
        {
            if (sideInfo.Granules[granule].ChannelDataModels[0].WindowSwitchingFlag == 1)
            {
                // Window switching in effect. Handle Blocks 1 to 3.
                IntensityStereoProcessingSwitched(header, sideInfo, granule, iStereoPanningPosition, is_ratio, k);
            }
            else
            {
                // Long Windows, handling block type 0.
                StereoProcessingIntensityStereoLongBlocks(header, sideInfo, granule, iStereoPanningPosition, is_ratio, k);
            }
        }

        private static void StereoProcessingIntensityStereoLongBlocks(HeaderModel header, SideInfoModel sideInfo, int granule, int[] iStereoPanningPosition, float[] is_ratio, float[][] k)
        {
            double sampleRate = HeaderInfoUtils.GetSampleRateShort(header.SampleRate);
            var scalefactorBandStartIndex = DecoderTableLookups.ScalefactorBandTables.FirstOrDefault(table => table.Frequency == sampleRate && table.BlockType == BlockType.Long).StartIndexes;
            var granuleData = sideInfo.Granules[granule];

            int sbStart = 0; // Starting index for the current scalefactor band
            int sbEnd = 0;   // Ending index for the current scalefactor band
            int index = 0;
            int isScalingType = sideInfo.Granules[granule].ChannelDataModels[0].ScaleFactorCompress & 1;

            for (int sfb = 0; sfb < scalefactorBandStartIndex.Count - 1; sfb++)
            {
                sbStart = scalefactorBandStartIndex[sfb];
                sbEnd = scalefactorBandStartIndex[sfb + 1];

                for (int i = sbStart; i < sbEnd; i++)
                {
                    if (iStereoPanningPosition[i] != 7) // 7 means that this band should not be processed
                    {
                        // Calculate the right channel sample using intensity stereo processing
                        k[1][i] = k[0][i] * is_ratio[i];

                        // Store the calculated left and right channel samples
                        k[0][i] = k[0][i];  // Left channel remains the same
                    }
                    else
                    {
                        IntensityStereoKValues(k, iStereoPanningPosition[i], isScalingType, index);
                        // Handle the case where is_pos[i] == 7, copying left channel to right channel
                        k[1][i] = k[0][i];
                    }
                }

                // Update is_ratio and is_pos based on the current granule data
                is_ratio[sbStart] = is_ratio[sfb] = DecoderTableLookups.IntensityStereoTangentTable[iStereoPanningPosition[sfb]];

                index++;
            }

            // This loop ensures the handling of the last band if necessary
            for (int i = sbEnd; i < 576; i++)
            {
                k[1][i] = k[0][i];
            }
        }

        private static void IntensityStereoProcessingSwitched(HeaderModel header, SideInfoModel sideInfo, int granule, int[] is_pos, float[] is_ratio, float[][] k)
        {
            var granuleData = sideInfo.Granules[granule].ChannelDataModels[0];

            // Handle Block Type 2 (Short Blocks)
            if (granuleData.BlockType == 2)
            {
                if (granuleData.MixedBlockFlag == 1) // Mixed blocks
                {
                    // Process the first two bands as long blocks
                    var scalefactorBandStartIndexLong = DecoderTableLookups.ScalefactorBandTables
                        .FirstOrDefault(table => table.Frequency == HeaderInfoUtils.GetSampleRateShort(header.SampleRate) && table.BlockType == BlockType.Long)
                        .StartIndexes;

                    // Process the first two bands as long blocks
                    for (int sfb = 0; sfb < 2; sfb++)
                    {
                        int sbStart = scalefactorBandStartIndexLong[sfb];
                        int sbEnd = scalefactorBandStartIndexLong[sfb + 1];

                        for (int i = sbStart; i < sbEnd; i++)
                        {
                            if (is_pos[i] != 7)
                            {
                                k[1][i] = k[0][i] * is_ratio[i];
                            }
                            else
                            {
                                k[1][i] = k[0][i];
                            }
                        }
                        is_ratio[sfb] = DecoderTableLookups.IntensityStereoTangentTable[is_pos[sfb]];
                    }

                    // Process the remaining bands as short blocks
                    var scalefactorBandStartIndexShort = DecoderTableLookups.ScalefactorBandTables
                        .FirstOrDefault(table => table.Frequency == HeaderInfoUtils.GetSampleRateShort(header.SampleRate) && table.BlockType == BlockType.Short)
                        .StartIndexes;

                    for (int sfb = 2; sfb < scalefactorBandStartIndexShort.Count - 1; sfb++)
                    {
                        for (int win = 0; win < 3; win++) // Process each of the 3 short windows
                        {
                            int winStart = scalefactorBandStartIndexShort[sfb] + win * 18;
                            int winEnd = scalefactorBandStartIndexShort[sfb + 1] + win * 18;

                            for (int i = winStart; i < winEnd; i++)
                            {
                                if (is_pos[i] != 7)
                                {
                                    k[1][i] = k[0][i] * is_ratio[i];
                                }
                                else
                                {
                                    k[1][i] = k[0][i];
                                }
                            }
                            is_ratio[sfb * 3 + win] = DecoderTableLookups.IntensityStereoTangentTable[is_pos[sfb * 3 + win]];
                        }
                    }
                }
                else // Regular short block processing
                {
                    var scalefactorBandStartIndexShort = DecoderTableLookups.ScalefactorBandTables
                        .FirstOrDefault(table => table.Frequency == HeaderInfoUtils.GetSampleRateShort(header.SampleRate) && table.BlockType == BlockType.Short)
                        .StartIndexes;

                    for (int sfb = 0; sfb < scalefactorBandStartIndexShort.Count - 1; sfb++)
                    {
                        for (int win = 0; win < 3; win++) // Process each of the 3 short windows
                        {
                            int winStart = scalefactorBandStartIndexShort[sfb] + win * 18;
                            int winEnd = scalefactorBandStartIndexShort[sfb + 1] + win * 18;

                            for (int i = winStart; i < winEnd; i++)
                            {
                                if (is_pos[i] != 7)
                                {
                                    k[1][i] = k[0][i] * is_ratio[i];
                                }
                                else
                                {
                                    k[1][i] = k[0][i];
                                }
                            }
                            is_ratio[sfb * 3 + win] = DecoderTableLookups.IntensityStereoTangentTable[is_pos[sfb * 3 + win]];
                        }
                    }
                }
            }
            // Handle Block Type 1 (Start Block)
            else if (granuleData.BlockType == 1)
            {
                var scalefactorBandStartIndexShort = DecoderTableLookups.ScalefactorBandTables
                    .FirstOrDefault(table => table.Frequency == HeaderInfoUtils.GetSampleRateShort(header.SampleRate) && table.BlockType == BlockType.Short)
                    .StartIndexes;
                var scalefactorBandStartIndexLong = DecoderTableLookups.ScalefactorBandTables
                    .FirstOrDefault(table => table.Frequency == HeaderInfoUtils.GetSampleRateShort(header.SampleRate) && table.BlockType == BlockType.Long)
                    .StartIndexes;

                // Process the short windows first
                for (int sfb = 0; sfb < scalefactorBandStartIndexShort.Count - 1; sfb++)
                {
                    for (int win = 0; win < 3; win++) // Process each of the 3 short windows
                    {
                        int winStart = scalefactorBandStartIndexShort[sfb] + win * 18;
                        int winEnd = scalefactorBandStartIndexShort[sfb + 1] + win * 18;

                        for (int i = winStart; i < winEnd; i++)
                        {
                            if (is_pos[i] != 7)
                            {
                                k[1][i] = k[0][i] * is_ratio[i];
                            }
                            else
                            {
                                k[1][i] = k[0][i];
                            }
                        }
                        is_ratio[sfb * 3 + win] = DecoderTableLookups.IntensityStereoTangentTable[is_pos[sfb * 3 + win]];
                    }
                }

                // Process the remaining bands as long blocks
                for (int sfb = 2; sfb < scalefactorBandStartIndexLong.Count - 1; sfb++)
                {
                    int sbStart = scalefactorBandStartIndexLong[sfb];
                    int sbEnd = scalefactorBandStartIndexLong[sfb + 1];

                    for (int i = sbStart; i < sbEnd; i++)
                    {
                        if (is_pos[i] != 7)
                        {
                            k[1][i] = k[0][i] * is_ratio[i];
                        }
                        else
                        {
                            k[1][i] = k[0][i];
                        }
                    }
                    is_ratio[sfb] = DecoderTableLookups.IntensityStereoTangentTable[is_pos[sfb]];
                }
            }
            // Handle Block Type 3 (End Block)
            else if (granuleData.BlockType == 3)
            {
                var scalefactorBandStartIndexShort = DecoderTableLookups.ScalefactorBandTables
                    .FirstOrDefault(table => table.Frequency == HeaderInfoUtils.GetSampleRateShort(header.SampleRate) && table.BlockType == BlockType.Short)
                    .StartIndexes;
                var scalefactorBandStartIndexLong = DecoderTableLookups.ScalefactorBandTables
                    .FirstOrDefault(table => table.Frequency == HeaderInfoUtils.GetSampleRateShort(header.SampleRate) && table.BlockType == BlockType.Long)
                    .StartIndexes;

                // Process the initial bands as long blocks
                for (int sfb = 0; sfb < 2; sfb++) // Handle the long block part
                {
                    int sbStart = scalefactorBandStartIndexLong[sfb];
                    int sbEnd = scalefactorBandStartIndexLong[sfb + 1];

                    for (int i = sbStart; i < sbEnd; i++)
                    {
                        if (is_pos[i] != 7)
                        {
                            k[1][i] = k[0][i] * is_ratio[i];
                        }
                        else
                        {
                            k[1][i] = k[0][i];
                        }
                    }
                    is_ratio[sfb] = DecoderTableLookups.IntensityStereoTangentTable[is_pos[sfb]];
                }

                // Process the remaining bands as short blocks
                for (int sfb = 2; sfb < scalefactorBandStartIndexShort.Count - 1; sfb++)
                {
                    for (int win = 0; win < 3; win++) // Process each of the 3 short windows
                    {
                        int winStart = scalefactorBandStartIndexShort[sfb] + win * 18;
                        int winEnd = scalefactorBandStartIndexShort[sfb + 1] + win * 18;

                        for (int i = winStart; i < winEnd; i++)
                        {
                            if (is_pos[i] != 7)
                            {
                                k[1][i] = k[0][i] * is_ratio[i];
                            }
                            else
                            {
                                k[1][i] = k[0][i];
                            }
                        }
                        is_ratio[sfb * 3 + win] = DecoderTableLookups.IntensityStereoTangentTable[is_pos[sfb * 3 + win]];
                    }
                }
            }
        }

        #endregion

        #endregion

        #region Frequency inversion

        private void FrequencyInversion(float[] samples)
        {
            for (int i = 1; i < DecoderTableLookups.SUBBANDSAMPLESIZE; i += 2)
            {
                for (int j = 1; j < 32; j += 2)
                {
                    samples[j * 18 + i] = -samples[j * 18 + i];
                }
            }
        }

        #endregion

        #region Inverse Modified Discrete Cosine Transform

        // TODO:
        // Learnings on how to do this. Meanwhile this is handle using Mp3Sharp code.

        #endregion

        #region Alias Reduction

        private void AliasReduction(float[] xr)
        {
            for (int sb = 1; sb < 32; sb++)
            {
                for (int i = 0; i < 8; i++)
                {
                    int offset1 = sb * 18 - 1 - i;
                    int offset2 = sb * 18 + i;

                    double bu = xr[offset1] * DecoderTableLookups.COSINE_SINE_COEFFICIENTS[i] - xr[offset2] * DecoderTableLookups.COSINE_ANTIALIAS_COEFFICIENTS[i];
                    double bd = xr[offset2] * DecoderTableLookups.COSINE_SINE_COEFFICIENTS[i] + xr[offset1] * DecoderTableLookups.COSINE_ANTIALIAS_COEFFICIENTS[i];

                    xr[offset1] = (float)bu;
                    xr[offset2] = (float)bd;
                }
            }
        }

        #endregion

        #region Reorder Pipeline

        private void Reorder(int granule, int channel)
        {
            var channelData = SideInfo.Granules[granule].ChannelDataModels[channel];
            var xr = channelData.DequantisedSamples;

            float[] xrReordered = new float[576];
            double sampleRate = HeaderInfoUtils.GetSampleRateShort(_header.SampleRate);
            var scalefactorBandTable = DecoderTableLookups.ScalefactorBandTables.FirstOrDefault(x => x.Frequency == sampleRate && x.BlockType == BlockType.Short);

            if (channelData.WindowSwitchingFlag == 1
                && channelData.BlockType == 2)
            {
                // No reorder for low 2 subbands
                Array.Copy(xr, xrReordered, 36);

                if (channelData.MixedBlockFlag == 1)
                {
                    // Reordering for rest switched short
                    for (int sfb = 3, sfb_start = scalefactorBandTable.StartIndexes[3],
                             bandwidth = scalefactorBandTable.BandWidths[sfb];
                         sfb < 13;
                         sfb++, sfb_start = scalefactorBandTable.StartIndexes[sfb],
                             bandwidth = scalefactorBandTable.BandWidths[sfb])
                    {
                        int sfb_start3 = (sfb_start << 2) - sfb_start;

                        for (int freq = 0, freq3 = 0; freq < bandwidth; freq++, freq3 += 3)
                        {
                            int src_line = sfb_start3 + freq;
                            int des_line = sfb_start3 + freq3;

                            xrReordered[des_line] = channelData.DequantisedSamples[src_line];
                            src_line += bandwidth;
                            des_line++;

                            xrReordered[des_line] = channelData.DequantisedSamples[src_line];
                            src_line += bandwidth;
                            des_line++;

                            xrReordered[des_line] = channelData.DequantisedSamples[src_line];
                        }
                    }
                }
                else
                {
                    // Pure short blocks
                    for (int index = 0; index < 576; index++)
                    {
                        xrReordered[index] = channelData.DequantisedSamples[scalefactorBandTable.GetReorderTable()[index]];
                    }
                }
            }
            else
            {
                // No reordering needed for long blocks
                xrReordered = channelData.DequantisedSamples;
            }

            channelData.DequantisedSamples = xrReordered;
        }

        #endregion

        #region Dequantise
#pragma warning disable CS8602

        private void DequantiseSample(int gr, int ch)
        {
            var channelData = SideInfo.Granules[gr].ChannelDataModels[ch];
            float[] xr = new float[576];
            int nonZeroCount = channelData.NonZeroCount;

            int cb = 0;
            int next_cb_boundary;
            int cb_begin = 0;
            int cb_width = 0;
            int index = 0, t_index, j;
            float g_gain;

            if ((channelData.WindowSwitchingFlag == 1) && (channelData.BlockType == 2))
            {
                if (channelData.MixedBlockFlag == 1)
                {
                    next_cb_boundary = DecoderTableLookups
                        .ScalefactorBandTables
                        .FirstOrDefault(x => x.Frequency == HeaderInfoUtils.GetSampleRateShort(_header.SampleRate) && x.BlockType == BlockType.Long).StartIndexes[1];
                    // LONG blocks: 0,1,3
                }
                else
                {
                    cb_width = DecoderTableLookups
                        .ScalefactorBandTables
                        .FirstOrDefault(x => x.Frequency == HeaderInfoUtils.GetSampleRateShort(_header.SampleRate) && x.BlockType == BlockType.Short).StartIndexes[1];
                    next_cb_boundary = (cb_width << 2) - cb_width;
                    cb_begin = 0;
                }
            }
            else
            {
                next_cb_boundary = DecoderTableLookups
                        .ScalefactorBandTables
                        .FirstOrDefault(x => x.Frequency == HeaderInfoUtils.GetSampleRateShort(_header.SampleRate) && x.BlockType == BlockType.Long).StartIndexes[1];
            }

            // Compute overall (global) scaling
            g_gain = (float)Math.Pow(2.0, (0.25 * (channelData.GlobalGain - 210.0)));

            for (j = 0; j < SingleDimensionSpectralDataPoints.Count; j++)
            {
                int abv = SingleDimensionSpectralDataPoints[j];
                xr[j] = DequantizeValue(abv, g_gain);
            }

            channelData.DequantisedSamples = xr;


            for (j = 0; j < nonZeroCount; j++)
            {
                if (index == next_cb_boundary)
                {
                    // Adjust critical band boundary
                    if ((channelData.WindowSwitchingFlag == 1) && (channelData.BlockType == 2))
                    {
                        if (channelData.MixedBlockFlag == 1)
                        {
                            if (index == DecoderTableLookups
                                    .ScalefactorBandTables
                                    .FirstOrDefault(x => x.Frequency == HeaderInfoUtils.GetSampleRateShort(_header.SampleRate) && x.BlockType == BlockType.Long).StartIndexes[8])
                            {
                                next_cb_boundary = DecoderTableLookups
                                    .ScalefactorBandTables
                                    .FirstOrDefault(x => x.Frequency == HeaderInfoUtils.GetSampleRateShort(_header.SampleRate) && x.BlockType == BlockType.Short).StartIndexes[4];
                                next_cb_boundary = (next_cb_boundary << 2) - next_cb_boundary;
                                cb = 3;
                                cb_width = DecoderTableLookups.ScalefactorBandTables
                                    .FirstOrDefault(x => x.Frequency == HeaderInfoUtils.GetSampleRateShort(_header.SampleRate) && x.BlockType == BlockType.Short).StartIndexes[4]
                                    - DecoderTableLookups.ScalefactorBandTables
                                    .FirstOrDefault(x => x.Frequency == HeaderInfoUtils.GetSampleRateShort(_header.SampleRate) && x.BlockType == BlockType.Short).StartIndexes[3];

                                cb_begin = DecoderTableLookups
                                    .ScalefactorBandTables
                                    .FirstOrDefault(x => x.Frequency == HeaderInfoUtils.GetSampleRateShort(_header.SampleRate) && x.BlockType == BlockType.Short).StartIndexes[3];
                                cb_begin = (cb_begin << 2) - cb_begin;
                            }
                            else if (index < DecoderTableLookups
                                    .ScalefactorBandTables
                                    .FirstOrDefault(x => x.Frequency == HeaderInfoUtils.GetSampleRateShort(_header.SampleRate) && x.BlockType == BlockType.Long).StartIndexes[8])
                            {
                                next_cb_boundary = DecoderTableLookups
                                    .ScalefactorBandTables
                                    .FirstOrDefault(x => x.Frequency == HeaderInfoUtils.GetSampleRateShort(_header.SampleRate) && x.BlockType == BlockType.Long).StartIndexes[(++cb) + 1];
                            }
                            else
                            {
                                next_cb_boundary = DecoderTableLookups
                                    .ScalefactorBandTables
                                    .FirstOrDefault(x => x.Frequency == HeaderInfoUtils.GetSampleRateShort(_header.SampleRate) && x.BlockType == BlockType.Short).StartIndexes[(++cb) + 1];
                                next_cb_boundary = (next_cb_boundary << 2) - next_cb_boundary;

                                cb_begin = DecoderTableLookups
                                    .ScalefactorBandTables
                                    .FirstOrDefault(x => x.Frequency == HeaderInfoUtils.GetSampleRateShort(_header.SampleRate) && x.BlockType == BlockType.Short).StartIndexes[cb];
                                cb_width = DecoderTableLookups
                                    .ScalefactorBandTables
                                    .FirstOrDefault(x => x.Frequency == HeaderInfoUtils.GetSampleRateShort(_header.SampleRate) && x.BlockType == BlockType.Short).StartIndexes[cb + 1] - cb_begin;
                                cb_begin = (cb_begin << 2) - cb_begin;
                            }
                        }
                        else
                        {
                            next_cb_boundary = DecoderTableLookups
                                    .ScalefactorBandTables
                                    .FirstOrDefault(x => x.Frequency == HeaderInfoUtils.GetSampleRateShort(_header.SampleRate) && x.BlockType == BlockType.Short).StartIndexes[(++cb) + 1];
                            next_cb_boundary = (next_cb_boundary << 2) - next_cb_boundary;

                            cb_begin = DecoderTableLookups
                                    .ScalefactorBandTables
                                    .FirstOrDefault(x => x.Frequency == HeaderInfoUtils.GetSampleRateShort(_header.SampleRate) && x.BlockType == BlockType.Short).StartIndexes[cb];
                            cb_width = DecoderTableLookups
                                    .ScalefactorBandTables
                                    .FirstOrDefault(x => x.Frequency == HeaderInfoUtils.GetSampleRateShort(_header.SampleRate) && x.BlockType == BlockType.Short).StartIndexes[cb + 1] - cb_begin;
                            cb_begin = (cb_begin << 2) - cb_begin;
                        }
                    }
                    else
                    {
                        next_cb_boundary = DecoderTableLookups
                                    .ScalefactorBandTables
                                    .FirstOrDefault(x => x.Frequency == HeaderInfoUtils.GetSampleRateShort(_header.SampleRate) && x.BlockType == BlockType.Long).StartIndexes[(++cb) + 1];
                    }
                }

                if ((channelData.WindowSwitchingFlag == 1) &&
                    (((channelData.BlockType == 2) && (channelData.MixedBlockFlag == 0)) ||
                        ((channelData.BlockType == 2) && (channelData.MixedBlockFlag != 0) && (j >= 36))))
                {
                    t_index = (index - cb_begin) / cb_width;
                    int idx = channelData.ScalefactorData.ShortBlocks[t_index][cb] << channelData.ScaleFactorScale;
                    idx += (channelData.SubblockGain[t_index] << 2);
                    xr[j] *= DecoderTableLookups.TwoToNegativeHalfPower[idx];
                }
                else
                {
                    int idx = channelData.ScalefactorData.LongBlocks[cb];
                    if (channelData.PreFlag != 0)
                        idx += DecoderTableLookups.PreEmphasisTable[cb];
                    idx = idx << channelData.ScaleFactorScale;
                    xr[j] *= DecoderTableLookups.TwoToNegativeHalfPower[idx];
                }
                index++;
            }

            for (j = nonZeroCount; j < 576; j++)
            {
                Array.Fill(xr, 0.0f, j, 576 - j);
            }

            channelData.DequantisedSamples = xr;
        }


#pragma warning restore CS8602

        private float DequantizeValue(int quantizedValue, float g_gain)
        {
            if (quantizedValue == 0)
            {
                return 0.0f;
            }

            float value = (float)Math.Pow(Math.Abs(quantizedValue), 4.0f / 3.0f);
            return Math.Sign(quantizedValue) * value * g_gain;
        }

        #endregion

        #region Decode Side Info

        private void DecodeSideInfoInternal()
        {
            var bufferStream = new BufferStream();

            bufferStream.PushRange(DecoderUtils.FrameBufferStream.PopRange(_header.ChannelMode == 3 ? 17 : 32));

            SideInfo.ScaleFactorSelectionInfo = new int[_header.ChannelMode == 3 ? 1 : 2][];
            SideInfo.ScaleFactorSelectionInfo[0] = new int[4];

            if (_header.ChannelMode != 3)
            {
                SideInfo.ScaleFactorSelectionInfo[1] = new int[4];
            }

            SideInfo.MainDataBegin = bufferStream.PopBits(9);
            SideInfo.PrivateBits = bufferStream.PopBits(_header.ChannelMode == 3 ? 5 : 3);

            for (var i = 0; i < SideInfo.ScaleFactorSelectionInfo.Count(); i++)
            {
                for (int j = 0; j < 4; j++)
                    SideInfo.ScaleFactorSelectionInfo[i][j] = bufferStream.PopBit();
            }

            SideInfo.Granules[0] = new GranuleModel(_header.ChannelMode == 3 ? 1 : 2);
            SideInfo.Granules[1] = new GranuleModel(_header.ChannelMode == 3 ? 1 : 2);

            for (int gr = 0; gr < 2; gr++)
            {
                for (int ch = 0; ch < SideInfo.Granules[gr].NumberOfChannels; ch++)
                {
                    SideInfo.Granules[gr].ChannelDataModels[ch].Part2_3_Length = bufferStream.PopBits(12);
                    SideInfo.Granules[gr].ChannelDataModels[ch].BigValues = bufferStream.PopBits(9);
                    SideInfo.Granules[gr].ChannelDataModels[ch].GlobalGain = bufferStream.PopBits(8);
                    SideInfo.Granules[gr].ChannelDataModels[ch].ScaleFactorCompress = bufferStream.PopBits(4);
                    SideInfo.Granules[gr].ChannelDataModels[ch].WindowSwitchingFlag = bufferStream.PopBit();

                    if (SideInfo.Granules[gr].ChannelDataModels[ch].WindowSwitchingFlag == 1)
                    {
                        SideInfo.Granules[gr].ChannelDataModels[ch].BlockType = bufferStream.PopBits(2);
                        SideInfo.Granules[gr].ChannelDataModels[ch].MixedBlockFlag = bufferStream.PopBit();

                        SideInfo.Granules[gr].ChannelDataModels[ch].TableSelect[0] = bufferStream.PopBits(5);
                        SideInfo.Granules[gr].ChannelDataModels[ch].TableSelect[1] = bufferStream.PopBits(5);
                        SideInfo.Granules[gr].ChannelDataModels[ch].TableSelect[2] = 0;

                        SideInfo.Granules[gr].ChannelDataModels[ch].SubblockGain[0] = bufferStream.PopBits(3);
                        SideInfo.Granules[gr].ChannelDataModels[ch].SubblockGain[1] = bufferStream.PopBits(3);
                        SideInfo.Granules[gr].ChannelDataModels[ch].SubblockGain[2] = bufferStream.PopBits(3);

                        if (SideInfo.Granules[gr].ChannelDataModels[ch].BlockType == 0)
                        {
                            throw new DataMisalignedException("Bad Frame. Block type cannot be 0 when window switching flag is set.");
                        }
                        if (SideInfo.Granules[gr].ChannelDataModels[ch].BlockType == 2 && SideInfo.Granules[gr].ChannelDataModels[ch].MixedBlockFlag == 0)
                        {
                            SideInfo.Granules[gr].ChannelDataModels[ch].Region0Count = 8;
                        }
                        else
                        {
                            SideInfo.Granules[gr].ChannelDataModels[ch].Region0Count = 7;
                        }
                        SideInfo.Granules[gr].ChannelDataModels[ch].Region1Count = 20 - SideInfo.Granules[gr].ChannelDataModels[ch].Region0Count;
                    }
                    else
                    {
                        SideInfo.Granules[gr].ChannelDataModels[ch].TableSelect[0] = bufferStream.PopBits(5);
                        SideInfo.Granules[gr].ChannelDataModels[ch].TableSelect[1] = bufferStream.PopBits(5);
                        SideInfo.Granules[gr].ChannelDataModels[ch].TableSelect[2] = bufferStream.PopBits(5);
                        SideInfo.Granules[gr].ChannelDataModels[ch].Region0Count = bufferStream.PopBits(4);
                        SideInfo.Granules[gr].ChannelDataModels[ch].Region1Count = bufferStream.PopBits(3);
                        SideInfo.Granules[gr].ChannelDataModels[ch].BlockType = 0;
                    }

                    SideInfo.Granules[gr].ChannelDataModels[ch].PreFlag = bufferStream.PopBit();
                    SideInfo.Granules[gr].ChannelDataModels[ch].ScaleFactorScale = bufferStream.PopBit();
                    SideInfo.Granules[gr].ChannelDataModels[ch].Count1TableSelect = bufferStream.PopBit();
                }
            }
        }

        #endregion

        #region Decode Scalefactors

        private void DecodeScaleFactors(int granule, int channel)
        {
            DecodeScalefactorPipeline.DecodeScaleFactors(granule, channel, SideInfo);
        }

        #endregion

        #region Huffman Decoding

        private void DecodeHuffman(int gr, int ch)
        {
            DecodeBigValuesSpectralData(gr, ch);
            DecodeCount1SpectralData(gr, ch);
        }

        #region Big Values Spectral Data

        private void DecodeBigValuesSpectralData(int gr, int ch)
        {
            var regionBoundry = GetRegionBoundries(gr, ch);

            for (int i = 0; i < SideInfo.Granules[gr].ChannelDataModels[ch].BigValues * 2; i += 2)
            {
                var huffmanTable = DecoderTableLookups.HuffmanTables[GetTableIndexForRegion(GetRegion(i, regionBoundry.region1Start, regionBoundry.region2Start), gr, ch)];

                if (huffmanTable.Id == 0)
                {
                    SingleDimensionSpectralDataPoints.Add(0);
                    SingleDimensionSpectralDataPoints.Add(0);
                    continue;
                }

                var data = GetHuffmanCodedSpectralData(huffmanTable);

                if (data.Item1)
                {
                    SingleDimensionSpectralDataPoints.Add(data.Item2.X);
                    SingleDimensionSpectralDataPoints.Add(data.Item2.Y);
                }
            }
        }

        private int GetTableIndexForRegion(int region, int gr, int ch)
            => (int)SideInfo.Granules[gr].ChannelDataModels[ch].TableSelect[region];

        private int GetRegion(int index, int region1Start, int region2Start)
        {
            if (index < region1Start)
            {
                return 0;
            }
            else if (index < region2Start)
            {
                return 1;
            }
            else
            {
                return 2;
            }
        }

        private (int region1Start, int region2Start) GetRegionBoundries(int gr, int ch)
        {
            int region1Start;
            int region2Start;
            double sfreq = HeaderInfoUtils.GetSampleRate(_header.SampleRate) / 1000.0;
            int buf;
            int buf1;

            if (((SideInfo.Granules[gr].ChannelDataModels[ch].WindowSwitchingFlag) != 0) && (SideInfo.Granules[gr].ChannelDataModels[ch].BlockType == 2))
            {
                // Region2.
                //MS: Extrahandling for 8KHZ
                region1Start = (sfreq == 8) ? 72 : 36; // sfb[9/3]*3=36 or in case 8KHZ = 72
                region2Start = 576; // No Region2 for short block case
            }
            else
            {
                // Find region boundary for long block case

                buf = SideInfo.Granules[gr].ChannelDataModels[ch].Region0Count + 1;
                buf1 = buf + SideInfo.Granules[gr].ChannelDataModels[ch].Region1Count + 1;

                if (buf1 > DecoderTableLookups.ScalefactorBandTables.Where(x => x.BlockType == Lookups.Models.BlockType.Long
                    && x.Frequency == sfreq).First().ScalefactorBands.Count - 1)
                    buf1 = DecoderTableLookups.ScalefactorBandTables.Where(x => x.BlockType == Lookups.Models.BlockType.Long
                    && x.Frequency == sfreq).First().ScalefactorBands.Count - 1;

                region1Start = DecoderTableLookups.ScalefactorBandTables.Where(x => x.BlockType == Lookups.Models.BlockType.Long
                    && x.Frequency == sfreq).First().StartIndexes[buf];
                region2Start = DecoderTableLookups.ScalefactorBandTables.Where(x => x.BlockType == Lookups.Models.BlockType.Long
                    && x.Frequency == sfreq).First().StartIndexes[buf1]; /* MI */
            }

            return (region1Start, region2Start);
        }

        #endregion

        #region Count 1 Spectral Data

        private void DecodeCount1SpectralData(int gr, int ch)
        {
            var count1Table = DecoderTableLookups.HuffmanTables[SideInfo.Granules[gr].ChannelDataModels[ch].Count1TableSelect + 32];
            int index = SideInfo.Granules[gr].ChannelDataModels[ch].BigValues * 2;

            while ((DecoderUtils.AudioDataBufferStream.GetBitsReadInTransaction() < SideInfo.Granules[gr].ChannelDataModels[ch].Part2_3_Length)
                && index <= 572)
            {
                var (isValid, spectralDataPoint) = GetHuffmanCodedSpectralData(count1Table);

                if (!isValid)
                {
                    throw new InvalidDataException("This frame is corrupted. Count1 data is invalid, misaligned, or corrupted.");
                }

                var v = spectralDataPoint.X >> 3 & 1;
                var w = spectralDataPoint.X >> 2 & 1;
                var x = spectralDataPoint.X >> 1 & 1;
                var y = spectralDataPoint.X & 1;

                if (v != 0)
                {
                    v = DecoderUtils.AudioDataBufferStream.PopBit() == 1 ? -v : v;
                }
                if (w != 0)
                {
                    w = DecoderUtils.AudioDataBufferStream.PopBit() == 1 ? -w : w;
                }
                if (x != 0)
                {
                    x = DecoderUtils.AudioDataBufferStream.PopBit() == 1 ? -x : x;
                }
                if (y != 0)
                {
                    y = DecoderUtils.AudioDataBufferStream.PopBit() == 1 ? -y : y;
                }

                var points = new Point[2];
                points[0] = new Point(v, w);
                points[1] = new Point(x, y);

                SingleDimensionSpectralDataPoints.Add(v);
                SingleDimensionSpectralDataPoints.Add(w);
                SingleDimensionSpectralDataPoints.Add(x);
                SingleDimensionSpectralDataPoints.Add(y);

                index += 4;
            }

            SideInfo.Granules[gr].ChannelDataModels[ch].NonZeroCount = index;

            for (; index < 576; index++)
            {
                SingleDimensionSpectralDataPoints.Add(0);
            }
        }

        #endregion

        #endregion

        #region Discard unused frame bytes

        private void DiscardUnusedBits(int gr, int ch, int totalBits)
        {
            var remainderBitsToRemove = SideInfo.Granules[gr].ChannelDataModels[ch].Part2_3_Length - totalBits;

            for (int i = 0; i < remainderBitsToRemove; i++)
            {
                DecoderUtils.AudioDataBufferStream.PopBit();
            }
        }
        #endregion

        #region Huffman Code searching and matching algorithm

        /// <summary>
        /// Matches bitstream with huffman code and returns a tuple containing spectral data.
        /// </summary>
        /// <remarks>
        /// The Tuples first item is a boolean representing if the contained spectral data is valid.
        /// Item2 contains the actual spectral data if data is valid.
        /// If the stream has finished, item1 boolean is set to false when the tuple is returned 
        /// indicating to disregard this tuple.
        /// </remarks>
        /// <param name="bitstream"></param>
        /// <param name="huffmanTable"></param>
        /// <returns>Tuple (bool, point)</returns>
        public static (bool, Point) GetHuffmanCodedSpectralData(HuffmanTableModel huffmanTable)
        {
            var currentNode = huffmanTable.HuffmanTree.GetRoot();

            try
            {
                while (!currentNode.IsLeaf)
                {
                    currentNode = huffmanTable.HuffmanTree.Find(DecoderUtils.AudioDataBufferStream.PopBit());
                }

                if (currentNode != null)
                {
                    return GetDecodedSpectralDataPoint(huffmanTable, currentNode.Data);
                }
            }
            catch (Exception)
            {
                return (false, new Point());
            }

            return (false, new Point());
        }

        private static (bool, Point) GetDecodedSpectralDataPoint(HuffmanTableModel huffmanTable, HuffmanData data)
        {
            var spectralDataPoint = new Point();

            if (huffmanTable.Id > 31)
            {
                var point = new Point(data.Value, 0);

                return (true, point);
            }

            spectralDataPoint.X = data == null ? 0 : data.SpectralDataPoint.X; // TODO: If null then 0. Need better correction for this scenario.
            spectralDataPoint.Y = data == null ? 0 : data.SpectralDataPoint.Y;

            if (huffmanTable.Linbits > 0)
            {
                if (huffmanTable.MaxLengthX == spectralDataPoint.X)
                {
                    spectralDataPoint.X += DecoderUtils.AudioDataBufferStream.PopBits(huffmanTable.Linbits);
                }
            }

            if (spectralDataPoint.X != 0)
            {
                if (DecoderUtils.AudioDataBufferStream.PopBit() == 1)
                {
                    spectralDataPoint.X = -spectralDataPoint.X;
                }
            }

            if (huffmanTable.Linbits > 0)
            {
                if (huffmanTable.MaxLengthY == spectralDataPoint.Y)
                {
                    spectralDataPoint.Y += DecoderUtils.AudioDataBufferStream.PopBits(huffmanTable.Linbits);
                }
            }

            if (spectralDataPoint.Y != 0)
            {
                if (DecoderUtils.AudioDataBufferStream.PopBit() == 1)
                {
                    spectralDataPoint.Y = -spectralDataPoint.Y;
                }
            }

            return (true, spectralDataPoint);
        }

        #endregion

        #region Buffer Management

        public PcmBuffer GetBuffer() => _buffer;

        public int GetBufferLength() => _buffer == null ? 0 : _buffer.GetBufferLength();

        public void Flush()
        {
            _buffer = null;
        }

        #endregion

        #region Metadata

        public override string ToString() => GetFrameMetadata();

        private string GetFrameMetadata()
        {
            var info = new StringBuilder();
            info.AppendLine($"Frame File Position: {_frame.FramePosition}");
            info.AppendLine(_header.ToString());
            info.AppendLine("SIDEINFO:");
            info.AppendLine($"Granule 0 Channel 0: Part 2/3 Length: {SideInfo.Granules[0].ChannelDataModels[0].Part2_3_Length}");

            if (HeaderInfoUtils.GetChannelModeType(_header.ChannelMode) != ChannelModeType.SingleChannel)
            {
                info.AppendLine($"Granule 0 Channel 1: Part 2/3 Length: {SideInfo.Granules[0].ChannelDataModels[1].Part2_3_Length}");
            }

            info.AppendLine($"Granule 1 Channel 0: Part 2/3 Length: {SideInfo.Granules[1].ChannelDataModels[0].Part2_3_Length}");

            if (HeaderInfoUtils.GetChannelModeType(_header.ChannelMode) != ChannelModeType.SingleChannel)
            {
                info.AppendLine($"Granule 1 Channel 1: Part 2/3 Length: {SideInfo.Granules[1].ChannelDataModels[1].Part2_3_Length}");
            }

            info.AppendLine($"Total Main Data(part2_3_length combined): {GetMainDataActualSize()}");
            info.AppendLine($"Main Data Begin: {SideInfo.MainDataBegin}");

            return info.ToString();
        }

        private int GetMainDataActualSize()
        {
            int mainActualDataSize = SideInfo.Granules[0].ChannelDataModels[0].Part2_3_Length + SideInfo.Granules[1].ChannelDataModels[0].Part2_3_Length;

            if (HeaderInfoUtils.GetChannelModeType(_header.ChannelMode) != ChannelModeType.SingleChannel)
            {
                mainActualDataSize += (SideInfo.Granules[0].ChannelDataModels[1].Part2_3_Length + SideInfo.Granules[1].ChannelDataModels[1].Part2_3_Length);
            }

            return mainActualDataSize;
        }

        internal static void PipelineReset()
        {
            DecoderUtils.Clean();
            _isSeekingMainDataBeginZero = true;
        }

        #endregion

    }
}
