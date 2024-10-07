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

            _dbRMSValues = GetVuAdjustedValues(buffer);

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

        private (float left, float right, float difference) CalculateVULevels(byte[] pcmBuffer)
        {
            int bytesPerSample = 2; // Should be 2 for 16-bit audio
            int channels = HeaderInfoUtils.GetNumberOfChannels(_frames[_framePosition].Header);

            int totalSamples = pcmBuffer.Length / (bytesPerSample * channels);
            double sumSquaresLeft = 0;
            double sumSquaresRight = 0;
            double reference = 32768.0; // Reference value for 16-bit audio

            if (channels == 2) // Stereo
            {
                for (int i = 0; i < pcmBuffer.Length; i += bytesPerSample * 2)
                {
                    short leftSample = BitConverter.ToInt16(pcmBuffer, i);
                    short rightSample = BitConverter.ToInt16(pcmBuffer, i + bytesPerSample);

                    sumSquaresLeft += leftSample * leftSample;
                    sumSquaresRight += rightSample * rightSample;
                }

                double rmsLeft = Math.Sqrt(sumSquaresLeft / totalSamples) / reference;
                double rmsRight = Math.Sqrt(sumSquaresRight / totalSamples) / reference;

                float dBLeft = 20 * (float)Math.Log10(rmsLeft + 1e-10f);
                float dBRight = 20 * (float)Math.Log10(rmsRight + 1e-10f);

                // Calculate the difference to detect panning
                float difference = dBLeft - dBRight;

                return (dBLeft, dBRight, difference);
            }
            else // Mono
            {
                for (int i = 0; i < pcmBuffer.Length; i += bytesPerSample)
                {
                    short sample = BitConverter.ToInt16(pcmBuffer, i);
                    sumSquaresLeft += sample * sample;
                }

                double rms = Math.Sqrt(sumSquaresLeft / totalSamples) / reference;
                float dB = 20 * (float)Math.Log10(rms + 1e-10f);

                return (dB, dB, 0.0f); // No difference in mono
            }
        }

        private (float, float, float) GetVuAdjustedValues(byte[] pcmBuffer)
        {
            if (_frames.Count == 0
                || _frames.Count() == _framePosition)
            {
                return (0.0f, 0.0f, 0.0f);
            }

            (float dBLeft, float dBRight, float difference) calculatedValues = CalculateVULevels(pcmBuffer);

            // Adjust the values based on panning
            float adjustedLeft = calculatedValues.dBLeft;
            float adjustedRight = calculatedValues.dBRight;

            float panSensitivity = 0.5f; // Adjust this sensitivity factor as needed

            if (calculatedValues.difference > 0) // Panning towards the left
            {
                adjustedRight -= calculatedValues.difference * panSensitivity;
            }
            else if (calculatedValues.difference < 0) // Panning towards the right
            {
                adjustedLeft += calculatedValues.difference * panSensitivity;
            }

            // Ensure the values are within a reasonable range
            adjustedLeft = Math.Max(-60, Math.Min(0, adjustedLeft));  // Example range: -60 dB to 0 dB
            adjustedRight = Math.Max(-60, Math.Min(0, adjustedRight));  // Example range: -60 dB to 0 dB

            return (adjustedLeft, adjustedRight, calculatedValues.difference);
        }

        public (float, float, float) GetRmsValues() => _dbRMSValues;
    }
}
