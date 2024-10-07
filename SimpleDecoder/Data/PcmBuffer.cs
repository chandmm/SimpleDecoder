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
namespace SimpleMp3Decoder.Data
{
    public class PcmBuffer : IDisposable
    {
        private const int MaxChannels = 2;
        private const int SampleSize = 1152;
        private const float UpperClampLimit = 32767.0f;
        private const float LowerClampLimit = -32768.0f;

        private static int ChannelCount;

        private int _end;
        private bool _disposedValue;

        public byte[] _buffer; // all channels interleaved
        private int _leftChannelWriteBufferOffset;
        private int _rightChannelWriteBufferOffset;
        private Func<int> GetCurrentPosition;
        private Func<int, Func<int>> UpdateBufferPositionInfo = (int p) =>
        {
            return () => p;
        };

        public PcmBuffer(int channels)
        {
            ChannelCount = channels;
            _buffer = new byte[(SampleSize * 2) * ChannelCount]; // all channels interleaved
            _leftChannelWriteBufferOffset = 0;
            _rightChannelWriteBufferOffset = 2;
            GetCurrentPosition = UpdateBufferPositionInfo(0);
        }

        public int Read(byte[] bufferOut, int offset, int count)
        {
            if (bufferOut == null
                || (count + offset) > bufferOut.Length)
            {
                throw new ArgumentNullException(bufferOut == null 
                    ? "Uninitialised output buffer" 
                    : "Request buffer exceeded output buffer limit.");
            }

            int bytesToRead = Math.Min(_buffer.Count() - GetCurrentPosition(), count);

            Array.Copy(_buffer, GetCurrentPosition(), bufferOut, offset, bytesToRead);

            GetCurrentPosition = UpdateBufferPositionInfo(GetCurrentPosition() + bytesToRead);

            return bytesToRead;
        }

        private int Clamping(double sample)
            => (int)Math.Max(LowerClampLimit, Math.Min(UpperClampLimit, sample));

        public void WriteSamples(int channel, double[] samples)
        {
            if (samples == null)
            {
                throw new ArgumentNullException("samples");
            }
            if (samples.Length < 32)
            {
                throw new ArgumentException("samples must have 32 values");
            }

            int pos = channel == 0 ? _leftChannelWriteBufferOffset : _rightChannelWriteBufferOffset;

            for (int i = 0; i < 32; i++)
            {
                var clampedSample = Clamping(samples[i]);

                _buffer[pos] = (byte)(clampedSample & 0xff);
                _buffer[pos + 1] = (byte)(clampedSample >> 8);

                pos += ChannelCount * 2;
            }

            if (channel == 0)
            {
                _leftChannelWriteBufferOffset = pos;
            }
            else
            {
                _rightChannelWriteBufferOffset = pos;
            }
        }

        public void Reset() => GetCurrentPosition = UpdateBufferPositionInfo(0);

        public int GetBufferLength() => _buffer.Length;

        #region Dispose

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    Array.Fill(_buffer, (byte)0);
                    _buffer = null;
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
