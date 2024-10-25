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
using SimpleMp3Decoder.Logic;

namespace SimpleMp3Decoder.Data
{
    public class DecoderSimplePcmStream : Stream, IDisposable
    {
        private const double SamplesPerFrame = 1152d;

        private List<FrameModel> _frames;
        private int _position;
        private FrameModel _primaryFrame;
        private BufferStream _bufferStream;
        private HeaderModel _header;
        private bool _isDisposed = false;
        private bool _requestStop;
        private double _duration;
        private int _framePosition = 0;
        private (float, float, float) _dbRMSValues;

        public int DurationMinutes => (int)(_duration / 60);
        public int DurationSeconds => (int)(_duration % 60);
        public Action<int, int> UpdateInfoCallback { get; private set; }

        public DecoderSimplePcmStream(List<FrameModel> frames)
        {
            _bufferStream = new BufferStream();
            _frames = frames;
            _primaryFrame = _frames[_position];
            _header = _primaryFrame.Header;

            _duration = CalculateDuration();
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        //public override long Length => _frames.Select(x => x.GetBufferLength()).Sum(); // TODO: Find a way to get actual buffer length.
        public override long Length => _frames.Count() * 4608;

        public override long Position
        {
            get => _position;
            set
            {
                _position = (int)value;
            }
        }

        public override void Flush()
        {
            _bufferStream.Clear();
        }

        public Stream GetStream()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_requestStop
                || _framePosition == _frames.Count())
            {
                return 0;
            }

            var bytesRead = WriteInternal(buffer, offset, count);

            InvokeCallbackMethods();

            return bytesRead;
        }

        private void InvokeCallbackMethods()
        {
            if (UpdateInfoCallback != null
                && _framePosition < _frames.Count()
                && _frames[_framePosition] != null)
            {
                UpdateInfoCallback(HeaderInfoUtils.GetBitrate(_frames[_framePosition].Header.BitRate), _frames[_framePosition].FrameId);
            }
        }

        public int GetFrameIndexFromPosition()
        {
            return this._position / _primaryFrame.GetBuffer().GetBufferLength();
        }

        public void ReleaseStream(Stream stream)
        {
            _bufferStream.Clear();
            _bufferStream = null;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            DiscardFrameBuffers();

            switch (origin)
            {
                case SeekOrigin.Begin:
                    if (offset < 0)
                    {
                        throw new IOException("Cannot seek before begin.");
                    }

                    DecoderResetToSeek((int)offset);
                    break;
                case SeekOrigin.Current:

                    if (_frames.Count() <= (_framePosition + offset))
                    {
                        throw new IOException("Cannot seek past end of stream.");
                    }

                    _framePosition += (int)offset;
                    break;
                case SeekOrigin.End:
                    if (((_frames.Count() - offset) - 1) < 0)
                    {
                        throw new IOException("Cannot seek before begin.");
                    }

                    _framePosition = (_frames.Count() - (int)offset) - 1;
                    break;
            }

            return _framePosition;
        }

        private void DecoderResetToSeek(int offset)
        {
            if (offset > 0
                && offset < _frames.Count())
            {
                _frames.ElementAt(offset).GetDecodingPipeline().DecodeWithSeek(_frames, offset);
                _framePosition = offset;
            }
            else
            {
                _framePosition = offset;
            }

            _bufferStream.Clear();
        }

        private void DiscardFrameBuffers()
        {
            foreach (var frame in _frames)
            {
                frame.Flush();
            }
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        private void BufferFill(int offset, int count)
        {
            while (_bufferStream.Count() < (count - offset)
                && _framePosition < _frames.Count())
            {
                BufferFill();
            }
        }

        private void BufferFill()
        {
            var frame = _frames[_framePosition++];

            frame.GetDecodingPipeline().Decode();

            if (frame.GetBuffer() == null)
            {
                return;
            }

            frame.GetBuffer()?.Reset();
            var pcmBytes = new byte[frame.GetBufferLength()];
            frame.GetBuffer().Read(pcmBytes, 0, pcmBytes.Length);
            _bufferStream.AddRange(pcmBytes);
            frame.Flush();
        }

        private int WriteInternal(byte[] buffer, int offset, int count)
        {
            BufferFill(offset, count);

            var bytesRead = Math.Min(count, _bufferStream.Count());

            Write(buffer, offset, bytesRead);

            _position += bytesRead;

            return bytesRead;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _bufferStream.PopRange(count).ToArray().CopyTo(buffer, offset);
        }

        public int GetSampleRate() => HeaderInfoUtils.GetSampleRate(_header.SampleRate);

        public int GetNumberOfChannels() => HeaderInfoUtils.GetNumberOfChannels(_header);

        public void RequestStop() => _requestStop = true;

        private double CalculateDurationInternal(List<FrameModel> frames)
            => frames.Select(frame => CalculateFrameDuration(frame)).Sum();

        public double CalculateDuration(List<FrameModel> frames = null)
        {
            return CalculateDurationInternal(frames ?? _frames);
        }

        public double CalculateFrameDuration(FrameModel frame)
            => SamplesPerFrame / GetSampleRate();

        public int GetBitRate() => HeaderInfoUtils.GetBitrate(_header.BitRate);

        public void SetBitrateCallback(Action<int, int> callback) => UpdateInfoCallback = callback;

        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed
                && disposing)
            {
                ReleaseStream(this);

                base.Dispose(disposing);
                _isDisposed = true;
            }
        }
    }
}
