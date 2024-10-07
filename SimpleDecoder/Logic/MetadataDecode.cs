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
    public static class MetadataDecode
    {
        #region Properties

        public static long MetadataPosition { get; private set; }

        #endregion

        #region Statics

        public static readonly string TAG_ID3V2 = "ID3";
        public static readonly string TAG_ID3V1 = "TAG";

        #endregion

        public static List<MetadataModel> Decode(FileStream fileStream)
        {
            var metadataModels = new List<MetadataModel>();

            fileStream.Position = 0;

            var bytes = new byte[3];

            fileStream.Read(bytes);

            if (GetTagBytes(TAG_ID3V2).SequenceEqual(bytes))
            {
                // TODO: Implement proper Id3 version 2 metadata handling
                // metadataModels.Add(GetId3Version2(fileStream));
            }

            fileStream.Position = fileStream.Length - 128;
            MetadataPosition = fileStream.Position;
            bytes = new byte[128];
            fileStream.Read(bytes);

            if (GetTagBytes(TAG_ID3V1).SequenceEqual(bytes.Take(3)))
            {
                var metaData = GetId3Version1(bytes.Skip(3).ToArray());

                if (metaData != null)
                {
                    metadataModels.Add(metaData);
                }
            }
            else
            {
                MetadataPosition = -1;
            }

            return metadataModels;
        }

        private static MetadataModel? GetId3Version1(byte[] metadataBytes)
            => GetMetadataModel(metadataBytes, metadataVersion: 1);

        private static MetadataModel? GetId3Version2(FileStream fileStream)
        {
            var metadataBytes = new List<byte>();

            byte[] bytes = new byte[2];
            fileStream.Read(bytes);

            while (!((bytes[0] == 0xFF) && ((bytes[1] & 0xE0) == 0xE0)))
            {
                metadataBytes.Add(bytes[0]);
                fileStream.Position = fileStream.Position == 0 ? 0 : fileStream.Position - 1;
                fileStream.Read(bytes);
            }

            return GetMetadataModel(metadataBytes: metadataBytes.ToArray(), metadataVersion: 2);
        }

        private static MetadataModel? GetMetadataModel(byte[] metadataBytes, int metadataVersion)
        {
            switch (metadataVersion)
            {
                case 1:
                    return GetMetadataVersion1(metadataBytes);
                case 2:
                    return GetMetadataVersion2(metadataBytes);
                default:
                    return null;
            }
        }

        private static MetadataModel GetMetadataVersion1(byte[] metadataBytes)
        {
            var trackNumberBytes = metadataBytes.Skip(122).Take(2);

            var model = new MetadataModel(
                title: string.Join("", (metadataBytes.Take(30).Where(b => b != 0x00).Select(b => (char)b))).TrimEnd(),
                artist: string.Join("", (metadataBytes.Skip(30).Take(30).Where(b => b != 0x00).Select(b => (char)b))).TrimEnd(),
                album: string.Join("", (metadataBytes.Skip(60).Take(30).Where(b => b != 0x00).Select(b => (char)b))).TrimEnd(),
                year: string.Join("", (metadataBytes.Skip(90).Take(4).Where(b => b != 0x00).Select(b => (char)b))).TrimEnd(),
                comment: trackNumberBytes.First() == 0x00
                    ? string.Join("", (metadataBytes.Skip(94).Take(28).Where(b => b != 0x00).Select(b => (char)b))).TrimEnd()
                    : string.Join("", (metadataBytes.Skip(94).Take(30).Where(b => b != 0x00).Select(b => (char)b))).TrimEnd(),
                genreCode: metadataBytes.Skip(124).Take(1).First(),
                trackNumber: trackNumberBytes.First() == 0x00 && trackNumberBytes.Last() != 0x00 ? (int?)trackNumberBytes.Last() : null);

            return model;
        }

        private static MetadataModel GetMetadataVersion2(byte[] metadataBytes)
        {
            throw new NotImplementedException();
        }

        public static byte[] GetTagBytes(string tag)
            => tag
            .ToArray<char>()
            .Select(c => (byte)c)
            .ToArray();
    }
}
