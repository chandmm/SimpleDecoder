using SimpleMp3Decoder.Lookups;
using SimpleMp3Decoder.Models;

namespace SimpleMp3Decoder.Logic.PipelineStages
{
    public static class DecodeScalefactorPipeline
    {
        public static void DecodeScaleFactors(int granule, int channel, SideInfoModel sideInfo)
        {
            sideInfo.Granules[granule].ChannelDataModels[channel].ScalefactorData = sideInfo.Granules[0].ChannelDataModels[channel].ScalefactorData ?? new ScalefactorDataModel();

            var length0 = DecoderTableLookups.ScalefactorLength1[sideInfo.Granules[granule].ChannelDataModels[channel].ScaleFactorCompress];
            var length1 = DecoderTableLookups.ScalefactorLength2[sideInfo.Granules[granule].ChannelDataModels[channel].ScaleFactorCompress];

            if (sideInfo.Granules[granule].ChannelDataModels[channel].WindowSwitchingFlag == 1
                && sideInfo.Granules[granule].ChannelDataModels[channel].BlockType == 2)
            {
                if (sideInfo.Granules[granule].ChannelDataModels[channel].MixedBlockFlag == 1)
                {
                    for (int scalefactorBand = 0; scalefactorBand < 8; scalefactorBand++)
                        sideInfo.Granules[granule].ChannelDataModels[channel].ScalefactorData.LongBlocks[scalefactorBand] = DecoderUtils.AudioDataBufferStream.PopBits(length0);
                    for (int scalefactorBand = 3; scalefactorBand < 6; scalefactorBand++)
                        for (int window = 0; window < 3; window++)
                            sideInfo.Granules[granule].ChannelDataModels[channel].ScalefactorData.ShortBlocks[window][scalefactorBand] = DecoderUtils.AudioDataBufferStream.PopBits(length0);
                    for (int scalefactorBand = 6; scalefactorBand < 12; scalefactorBand++)
                        for (int window = 0; window < 3; window++)
                            sideInfo.Granules[granule].ChannelDataModels[channel].ScalefactorData.ShortBlocks[window][scalefactorBand] = DecoderUtils.AudioDataBufferStream.PopBits(length1);
                    for (int scalefactorBand = 12, window = 0; window < 3; window++)
                        sideInfo.Granules[granule].ChannelDataModels[channel].ScalefactorData.ShortBlocks[window][scalefactorBand] = 0;
                }
                else
                {
                    for (int i = 0; i < 13; i++)
                    {
                        sideInfo.Granules[granule].ChannelDataModels[channel].ScalefactorData.ShortBlocks[0][i] = i < 12
                        ? DecoderUtils.AudioDataBufferStream.PopBits(i < 6 ? length0 : length1)
                            : 0;
                        sideInfo.Granules[granule].ChannelDataModels[channel].ScalefactorData.ShortBlocks[1][i] = i < 12
                        ? DecoderUtils.AudioDataBufferStream.PopBits(i < 6 ? length0 : length1)
                            : 0;
                        sideInfo.Granules[granule].ChannelDataModels[channel].ScalefactorData.ShortBlocks[2][i] = i < 12
                            ? DecoderUtils.AudioDataBufferStream.PopBits(i < 6 ? length0 : length1)
                            : 0;
                    }
                }
            }
            else
            {
                if ((sideInfo.ScaleFactorSelectionInfo[channel][0] == 0) || (granule == 0))
                {
                    sideInfo.Granules[granule].ChannelDataModels[channel].ScalefactorData.LongBlocks[0] = DecoderUtils.AudioDataBufferStream.PopBits(length0);
                    sideInfo.Granules[granule].ChannelDataModels[channel].ScalefactorData.LongBlocks[1] = DecoderUtils.AudioDataBufferStream.PopBits(length0);
                    sideInfo.Granules[granule].ChannelDataModels[channel].ScalefactorData.LongBlocks[2] = DecoderUtils.AudioDataBufferStream.PopBits(length0);
                    sideInfo.Granules[granule].ChannelDataModels[channel].ScalefactorData.LongBlocks[3] = DecoderUtils.AudioDataBufferStream.PopBits(length0);
                    sideInfo.Granules[granule].ChannelDataModels[channel].ScalefactorData.LongBlocks[4] = DecoderUtils.AudioDataBufferStream.PopBits(length0);
                    sideInfo.Granules[granule].ChannelDataModels[channel].ScalefactorData.LongBlocks[5] = DecoderUtils.AudioDataBufferStream.PopBits(length0);
                }
                if ((sideInfo.ScaleFactorSelectionInfo[channel][1] == 0) || (granule == 0))
                {
                    sideInfo.Granules[granule].ChannelDataModels[channel].ScalefactorData.LongBlocks[6] = DecoderUtils.AudioDataBufferStream.PopBits(length0);
                    sideInfo.Granules[granule].ChannelDataModels[channel].ScalefactorData.LongBlocks[7] = DecoderUtils.AudioDataBufferStream.PopBits(length0);
                    sideInfo.Granules[granule].ChannelDataModels[channel].ScalefactorData.LongBlocks[8] = DecoderUtils.AudioDataBufferStream.PopBits(length0);
                    sideInfo.Granules[granule].ChannelDataModels[channel].ScalefactorData.LongBlocks[9] = DecoderUtils.AudioDataBufferStream.PopBits(length0);
                    sideInfo.Granules[granule].ChannelDataModels[channel].ScalefactorData.LongBlocks[10] = DecoderUtils.AudioDataBufferStream.PopBits(length0);
                }
                if ((sideInfo.ScaleFactorSelectionInfo[channel][2] == 0) || (granule == 0))
                {
                    sideInfo.Granules[granule].ChannelDataModels[channel].ScalefactorData.LongBlocks[11] = DecoderUtils.AudioDataBufferStream.PopBits(length1);
                    sideInfo.Granules[granule].ChannelDataModels[channel].ScalefactorData.LongBlocks[12] = DecoderUtils.AudioDataBufferStream.PopBits(length1);
                    sideInfo.Granules[granule].ChannelDataModels[channel].ScalefactorData.LongBlocks[13] = DecoderUtils.AudioDataBufferStream.PopBits(length1);
                    sideInfo.Granules[granule].ChannelDataModels[channel].ScalefactorData.LongBlocks[14] = DecoderUtils.AudioDataBufferStream.PopBits(length1);
                    sideInfo.Granules[granule].ChannelDataModels[channel].ScalefactorData.LongBlocks[15] = DecoderUtils.AudioDataBufferStream.PopBits(length1);
                }
                if ((sideInfo.ScaleFactorSelectionInfo[channel][3] == 0) || (granule == 0))
                {
                    sideInfo.Granules[granule].ChannelDataModels[channel].ScalefactorData.LongBlocks[16] = DecoderUtils.AudioDataBufferStream.PopBits(length1);
                    sideInfo.Granules[granule].ChannelDataModels[channel].ScalefactorData.LongBlocks[17] = DecoderUtils.AudioDataBufferStream.PopBits(length1);
                    sideInfo.Granules[granule].ChannelDataModels[channel].ScalefactorData.LongBlocks[18] = DecoderUtils.AudioDataBufferStream.PopBits(length1);
                    sideInfo.Granules[granule].ChannelDataModels[channel].ScalefactorData.LongBlocks[19] = DecoderUtils.AudioDataBufferStream.PopBits(length1);
                    sideInfo.Granules[granule].ChannelDataModels[channel].ScalefactorData.LongBlocks[20] = DecoderUtils.AudioDataBufferStream.PopBits(length1);
                }
            }
        }
    }
}
