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
namespace SimpleMp3Decoder.Models
{
    public class ChannelDataModel
    {
        public int Part2_3_Length { get; set; }
        public int BigValues { get; set; }
        public int GlobalGain { get; set; }
        public int ScaleFactorCompress { get; set; }
        public int WindowSwitchingFlag { get; set; }
        public int BlockType { get; set; }
        public int[] TableSelect { get; set; } = new int[3];
        public int[] SubblockGain { get; set; } = new int[3];
        public int Region0Count { get; set; }
        public int Region1Count { get; set; }
        public int PreFlag { get; set; }
        public int ScaleFactorScale { get; set; }
        public int Count1TableSelect { get; set; }
        public int MixedBlockFlag { get; set; }
        public ScalefactorDataModel ScalefactorData { get; set; }
        public float[] DequantisedSamples = new float[576];
        public int NonZeroCount { get; set; }
    }
}
