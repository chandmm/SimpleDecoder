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
using SimpleMp3Decoder.Models;

namespace SimpleMp3Decoder.Logic
{
    public static class HeaderInfoUtils
    {
        #region Lookup Tables

        private static int[] _layers = new int[] { 0, 3, 2, 1 };
        private static int[] _bitrateIndex = new int[] { 0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 0 };
        private static int[] _sampleRates = new int[] { 44100, 48000, 32000, 0 };
        private static bool[] _modeExtensionIntensityStereo = new bool[] { false, true, false, true };
        private static bool[] modeExtensionMsStereo = new bool[] { false, false, true, true };
        private static EmphasisType[] _emphasisOptions = new EmphasisType[]
        {
            EmphasisType.Emphasis_None,
            EmphasisType.Emphasis_50_15_ms,
            EmphasisType.Emphasis_Reserved,
            EmphasisType.Emphasis_CCIT_J_17
        };

        #endregion

        #region Get Lookup Methods

        public static int GetLayer(HeaderModel header) => _layers[header.Layer];

        public static int GetBitrate(int bitrate) => _bitrateIndex[bitrate];

        public static int GetSampleRate(int sampleRate) => _sampleRates[sampleRate];
        public static double GetSampleRateShort(int sampleRate) => _sampleRates[sampleRate] / 1000.0;

        public static bool GetModeExtensionIntensityStereo(HeaderModel header) => _modeExtensionIntensityStereo[header.ModeExtension];

        public static bool GetModeExtensionMsStereo(HeaderModel header) => modeExtensionMsStereo[header.ModeExtension];

        public static ModeExtensionType GetModeExtensionType(HeaderModel header)
        {
            if (GetModeExtensionMsStereo(header)
                && GetModeExtensionIntensityStereo(header))
            {
                return ModeExtensionType.MS_IS_Stereo;
            }

            if (GetModeExtensionIntensityStereo(header))
            {
                return ModeExtensionType.IntensityStereo;
            }

            if (GetModeExtensionMsStereo(header))
            {
                return ModeExtensionType.MSStereo;
            }

            if (GetModeExtensionIntensityStereo(header)
                && GetModeExtensionMsStereo(header))
            {
                return ModeExtensionType.MS_IS_Stereo;
            }

            return ModeExtensionType.Off;
        }

        public static int GetNumberOfChannels(HeaderModel header) => header.ChannelMode == 3 ? 1 : 2;

        public static ChannelModeType GetChannelModeType(int channelMode)
        {
            switch (channelMode)
            {
                case 0:
                    return ChannelModeType.Stereo;
                case 2:
                    return ChannelModeType.DualChannel;
                case 3:
                    return ChannelModeType.SingleChannel;
                default:
                    return ChannelModeType.JointStereo;
            }
        }

        public static string GetMpegVersion(HeaderModel header) => header.Id == 1 ? "MPEG 1.0" : "MPEG 2.5";

        public static int GetMpegLayerNumber(HeaderModel header)
        {
            switch(header.Layer)
            {
                case 1:
                    return 3;
                case 2:
                    return 2;
                case 3:
                    return 1;
                default:
                    return 0;
            }
        }

        public static bool IsCopyRight(HeaderModel header) => header.Copyright == 1;

        public static bool IsOriginal(HeaderModel header) => header.Original == 1;

        public static EmphasisType GetEmphasisType(HeaderModel header)
            => _emphasisOptions[header.Emphasis];

        #endregion
    }

    public enum ModeExtensionType
    {
        IntensityStereo,
        MSStereo,
        MS_IS_Stereo,
        Off
    }

    public enum ChannelModeType
    {
        Stereo,
        JointStereo,
        DualChannel,
        SingleChannel
    }

    public enum EmphasisType
    {
        /// <summary>
        /// No emphasis has been applied to the audio.
        /// </summary>
        Emphasis_None,
        /// <summary>
        /// A 50/15 ms emphasis filter has been applied.
        /// </summary>
        Emphasis_50_15_ms,
        /// <summary>
        /// Reserved for future use or proprietary formats.
        /// </summary>
        Emphasis_Reserved,
        /// <summary>
        /// CCIT J.17 emphasis, rarely used in MP3s.
        /// </summary>
        Emphasis_CCIT_J_17
    }

}
