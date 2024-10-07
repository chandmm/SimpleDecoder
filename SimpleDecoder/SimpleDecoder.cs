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
using SimpleMp3Decoder.Intergration;
using SimpleMp3Decoder.Logic;
using SimpleMp3Decoder.Lookups;
using SimpleMp3Decoder.Models;

namespace SimpleMp3Decoder
{
    public class SimpleDecoder : IDisposable
    {
        private string _filename;
        private List<FrameModel> _frames = new List<FrameModel>();
        private Action<int> _decodingPositionCallback;
        private int _streamSize;
        private List<MetadataModel> _metadata;
        private FileStream _stream;
        private bool _isDisposed;

        public SimpleDecoder(string filename, Action<int> decodingPositionCallback)
        {
            DecodingPipeline.PipelineReset();

            _filename = filename;

            DecoderTableLookups.DecoderLookupsInitialise();

            for (int i5 = 0; i5 < 2; i5++)
            {
                DecodingPipeline.Prevblck[i5] = new float[DecoderTableLookups.SUBBANDSIZE * DecoderTableLookups.SUBBANDSAMPLESIZE];
            }

            for (int ch = 0; ch < 2; ch++)
            {
                for (int j = 0; j < 576; j++)
                {
                    DecodingPipeline.Prevblck[ch][j] = 0.0f;
                }
            }

            var equaliser = new Equaliser();
            var eqParams = new Params();

            Equaliser eq = eqParams.InitialEqualizerSettings;

            if (eq != null)
            {
                equaliser.FromEqualizer = eq;
            }

            float scalefactor = 32700.0f;
            double[] factors1 = equaliser.BandFactors;
            double[] factors2 = equaliser.BandFactors;

            DecodingPipeline.Filter1 = new SynthesisFilter(0, scalefactor, factors1);
            DecodingPipeline.Filter2 = new SynthesisFilter(1, scalefactor, factors2);
            DecodingPipeline.Filter1.EQ = factors1;
            DecodingPipeline.Filter2.EQ = factors2;

            _decodingPositionCallback = decodingPositionCallback;

            DecodeStream(new FileStream(_filename, FileMode.Open, FileAccess.Read));
        }

        private void DecodeStream(FileStream stream)
        {
            _stream = stream;

            Decode();

            foreach (var frame in _frames)
            {
                frame.GetDecodingPipeline().DecodeSideInfo();
            }
        }

        private void Decode()
        {
            _streamSize = (int)_stream.Length;

            while (_stream.Position < (_stream.Length - 4))
            {
                var header = DecoderUtils.DecodeHeader(_stream);

                if (!DecoderUtils.ValidateHeader(header, _stream))
                {
                    _stream.Position -= 3;
                    continue;
                }

                _frames.Add(new FrameModel(_stream, header, _frames.Count));
                _stream.Position = _stream.Position + (header.GetFrameSize() - 4);

                if (((_streamSize + _stream.Position) % 100) == 0)
                {
                    _decodingPositionCallback?.Invoke((int)_stream.Position);
                }
            }

            _metadata = MetadataDecode.Decode(_stream);

            _decodingPositionCallback?.Invoke(_streamSize);

            CleanupDecoderResources();
        }

        private void CleanupDecoderResources()
        {
            DecoderUtils.Clean();
        }

        public List<FrameModel> GetFrames() => _frames;

        public int GetStreamSize() => _streamSize;

        public List<MetadataModel> GetMetadata() => _metadata;

        public int GetFrameCount() => _frames.Count() == 0 ? 0 : _frames.Last().FrameId + 1;
        public int GetPaddedFrameCount() => _frames.Count(frame => frame.Header.Padding == 1);

        public int GetUnPaddedFrameCount() => _frames.Count(frame => frame.Header.Padding == 0);

        public DecoderSimplePcmStream GetStream()
        {
            var stream = new DecoderSimplePcmStream(_frames);

            return stream;
        }


        private void Dispose(bool isDisposing)
        {
            if (!_isDisposed)
            {
                if (isDisposing)
                {
                    _stream.Dispose();
                    _frames.ForEach(frame => frame.Flush());
                    _frames.Clear();
                    _frames = null;
                }

                _isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
