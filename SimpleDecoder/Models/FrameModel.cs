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
using SimpleMp3Decoder.Logic;

namespace SimpleMp3Decoder.Models
{
    public class FrameModel
    {
        #region Fields

        private double[] _samples1;
        private double[] _samples2;
        //private static float[][] lr;
        private static bool _isSeekingMainDataBeginZero = true;

        private PcmBuffer _buffer;

        private DecodingPipeline _decodingPipeline;

        #endregion

        #region Properties

        public int FrameId { get; private set; }
        public long FramePosition { get; private set; }
        public SideInfoModel SideInfo { get; private set; } = new();
        public HeaderModel Header { get; private set; }

        public PcmBuffer Buffer => _buffer;

        public bool IsFrameHealthy = true;

    #endregion

        #region Initialisation

        public FrameModel(FileStream stream, HeaderModel header, int frameId)
        {
            Header = header;
            FrameId = frameId;
            FramePosition = stream.Position - 4;

            _decodingPipeline = new DecodingPipeline(stream, this);
        }

        //public void Decode() => _decodingPipeline.Decode();
        public DecodingPipeline GetDecodingPipeline() => _decodingPipeline;

        #endregion

        #region Buffer Management

        public PcmBuffer GetBuffer() => _decodingPipeline.GetBuffer();

        public int GetBufferLength() => _decodingPipeline.GetBufferLength();

        public void Flush() => _decodingPipeline.Flush();

        #endregion

        #region Metadata

        public override string ToString() => _decodingPipeline.ToString();

        public string ToStringShort()
        {
            var rawMetadata = ToString();

            var sideInfoStripped = rawMetadata.Remove(rawMetadata.IndexOf("SIDEINFO"));

            return sideInfoStripped.Remove(0, sideInfoStripped.IndexOf("Mpeg"));
        }

        #endregion
    }
}
