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
using SimpleMp3Decoder.Data;
using SimpleMp3Decoder.Models;
using System.Drawing;

namespace SimpleMp3Decoder.Logic
{
    public static class DecoderUtils
    {
        #region Properties

        public static BufferStream FrameBufferStream { get; private set; } = new BufferStream();
        public static BufferStream AudioDataBufferStream { get; private set; }

        #endregion

        public static void UpdateAudioDataBufferStream(BufferStream bufferStream)
        {
            if (AudioDataBufferStream != null)
            {
                AudioDataBufferStream.PushRange(bufferStream.PopRange(bufferStream.Count));
            }
            else
            {
                AudioDataBufferStream = bufferStream;
            }
        }

        public static void AddToAudioDataBufferStream(byte[] bytes)
        {
            if (AudioDataBufferStream == null)
            {
                throw new ArgumentNullException(nameof(AudioDataBufferStream), "AudioDataBufferStream cannot be null");
            }

            AudioDataBufferStream.PushRange(bytes);
        }

        public static HeaderModel DecodeHeader(FileStream stream)
        {
            var bytes = new byte[4];
            HeaderModel header = null;

            stream.Read(bytes, 0, 4);

            if (bytes[0] == 0xFF && (bytes[1] & 0xF0) == 0xF0)
            {
                header = BuildHeader(bytes);
            }

            return header;
        }

        public static HeaderModel BuildHeader(byte[] bytes)
        {
            var header = new HeaderModel();
            header.Id = (bytes[1] & 0x01);
            header.Layer = (bytes[1] & 0x06) >>> 1;
            header.ProtectionBit = (bytes[1] & 0x01);
            header.BitRate = (bytes[2] & 0xF0) >>> 4;
            header.SampleRate = (bytes[2] & 0x0C) >>> 2;
            header.Padding = (bytes[2] & 0x02) >>> 1;
            header.Private = (bytes[2] & 0x01);
            header.ChannelMode = (bytes[3] & 0xC0) >>> 6;
            header.ModeExtension = (bytes[3] & 0x30) >>> 4;
            header.Copyright = (bytes[3] & 0x08) >>> 3;
            header.Original = (bytes[3] & 0x04) >>> 2;
            header.Emphasis = (bytes[3] & 0x03);

            return header;
        }

        public static bool ValidateHeader(HeaderModel header, FileStream stream)
        {
            if (header == null)
            {
                return false;
            }

            var currentStreamPosition = stream.Position;

            stream.Position += header.GetFrameSize() - 4;

            var bytes = new byte[4];
            stream.Read(bytes, 0, 4);

            HeaderModel nextHeader = null;
            stream.Position = currentStreamPosition;

            if (bytes[0] == 0xFF && (bytes[1] & 0xF0) == 0xF0)
            {
                nextHeader = BuildHeader(bytes);
            }

            stream.Position = currentStreamPosition;

            return nextHeader != null && nextHeader.GetFrameSize() > 0;
        }

        internal static void Clean()
        {
            AudioDataBufferStream?.Clear();
            AudioDataBufferStream = null;
            FrameBufferStream?.Clear();
            FrameBufferStream = new BufferStream();
        }

        public static int[] ConvertSpectraDataPointsToSingleDimensionArray(List<Point> spectralData, List<Point[]> count1SpectralData)
        {
            var points = new int[spectralData.Count*2 + count1SpectralData.Count*4];

            for(int i = 0; i < spectralData.Count; i += 2)
            {
                points[i] = spectralData[i].X;
                points[i + 1] = spectralData[i].Y;
            }

            for (int i = (spectralData.Count * 2); i < count1SpectralData.Count; i += 4)
            {
                points[i] = count1SpectralData[i][0].X; // v
                points[i + 1] = count1SpectralData[i][0].Y; // w
                points[i + 1] = count1SpectralData[i][1].X;
                points[i + 1] = count1SpectralData[i][1].Y;
            }

            return points;
        }

        public static void CreateEmptyAudioDataBufferStream()
            => AudioDataBufferStream = new BufferStream();
    }
}
