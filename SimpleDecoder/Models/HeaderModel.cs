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
using SimpleMp3Decoder.Logic;
using System.Text;

namespace SimpleMp3Decoder.Models
{
    public class HeaderModel
    {
        public int Id { get; set; }
        public int MpegVersion { get; set; }
        public int Layer { get; set; }
        public int ProtectionBit { get; set; }
        public int BitRate { get; set; }
        public int SampleRate { get; set; }
        public int Padding { get; set; }
        public int Private { get; set; }
        public int ChannelMode { get; set; }
        public int ModeExtension { get; set; }
        public int Copyright { get; set; }
        public int Original { get; set; }
        public int Emphasis { get; set; }

        public int GetFrameSize()
        {
            var slots = 0;

            if (HeaderInfoUtils.GetLayer(this) == 3)
            {
                var factor = 144;

                var samplingRateFrequencyIndexInfoAndProtectionBit = HeaderInfoUtils.GetSampleRate(SampleRate) + (ProtectionBit == 1 ? 2 : 0);
                var samplingRateFrequencyIndex = HeaderInfoUtils.GetSampleRate(SampleRate);

                if (samplingRateFrequencyIndex == 0)
                {
                    return 0;
                }

                // Calculate Frame size which is the number of bytes (slots) in the frame.
                slots = ((HeaderInfoUtils.GetBitrate(BitRate) * factor * 1000) / samplingRateFrequencyIndex);

                if (Padding == 1)
                {
                    slots += 1;
                }
            }

            return slots;
        }

        public override string ToString() => HeaderInfoAsDisplayFormatedText();

        private string HeaderInfoAsDisplayFormatedText()
        {
            var protection = ProtectionBit == 1 ? "No CRC" : "CRC 16 bit";
            var paddingInfo = Padding == 1 ? "Yes" : "No";
            var privateInfo = Private == 1 ? "Yes" : "No";

            var info = new StringBuilder();
            info.AppendLine($"Frame Size: {GetFrameSize()}");
            info.AppendLine();
            info.AppendLine($"Mpeg Standard: {HeaderInfoUtils.GetMpegVersion(this)} Layer {HeaderInfoUtils.GetLayer(this)}");
            info.AppendLine($"Protected: {protection}");
            info.AppendLine($"Bitrate: {HeaderInfoUtils.GetBitrate(BitRate)}kbps");
            info.AppendLine($"Sampling Rate: {HeaderInfoUtils.GetSampleRate(SampleRate)}Hz");
            info.AppendLine($"Padding: {paddingInfo}");
            info.AppendLine($"Private: {privateInfo}");
            info.AppendLine($"Channel: {HeaderInfoUtils.GetChannelModeType(ChannelMode)}");

            info.AppendLine($"Channel Mode Extension: {HeaderInfoUtils.GetModeExtensionType(this)}");

            info.AppendLine($"Copyright: {HeaderInfoUtils.IsCopyRight(this)}");
            info.AppendLine($"Original: {HeaderInfoUtils.IsOriginal(this)}");
            info.AppendLine($"Emphasis: {HeaderInfoUtils.GetEmphasisType(this)}");

            return info.ToString();
        }
    }
}
