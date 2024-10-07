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
    public class BufferStream : List<byte>
    {
        private int _currentBitIndex = 0;
        private int _beginBitCount;
        private byte _previousByte;

        public void Push(byte value)
        {
            base.Add(value);
        }

        public void PushRange(IEnumerable<byte> values)
        {
            base.AddRange(values);
        }

        public byte Pop()
        {
            var value = this.First();

            base.RemoveAt(0);

            return value;
        }

        public IEnumerable<byte> PopRange(int count)
        {
            var values = base.GetRange(0, count);

            base.RemoveRange(0, count);

            return values;
        }

        #region Region for returning Bits from the Byte buffer


        /// <summary>
        /// Gets count of bits and only pops the current byte once it has been fully read.
        /// </summary>
        /// <remarks>
        /// Virtualises the popping of bits until the current byte has been fully read.
        /// Once all the bits of the current byte has been read then the pop operation takes place where that byte is 
        /// virtulised removed from the buffer. This allows rollback of bits by up to one byte if needed. 
        /// </remarks>
        public int PopBits(int bitCount)
        {
            if (bitCount <= 0 || bitCount > 32)
            {
                return 0;
            }

            int bits = 0;

            for (int i = 0; i < bitCount; i++)
            {
                bits <<= 1;

                if (Count == 0)
                {
                    throw new InvalidOperationException("Attempted to read beyond the end of the bit stream.");
                    //return bits;
                }
                
                bits |= PopBit();
            }

            return bits;
        }

        public int PopBit()
        {
            var bit = (this.First() >> (7 - _currentBitIndex)) & 1;

            _currentBitIndex++;
            _beginBitCount++;

            if (_currentBitIndex > 7)
            {
                _previousByte = Pop();
                _currentBitIndex = 0;
            }

            return bit;
        }

        public void AlignToByteBoundary()
        {
            if (_currentBitIndex > 0
                && this.Count > 0)
            {
                _currentBitIndex = 0;
                _ = Pop();
            }
        }

        public void RestorePopBits(int size)
        {
            _currentBitIndex -= size;

            if (_currentBitIndex < 0)
            {
                _currentBitIndex = 8 - Math.Abs(_currentBitIndex);
                this.Insert(0, _previousByte);
            }

            _beginBitCount -= size;
        }

        #endregion

        public BufferStream PopToNewBufferStream()
        {
            var bufferStream = new BufferStream
            {
                _currentBitIndex = _currentBitIndex
            };

            bufferStream.AddRange(this);

            this.Clear();

            return bufferStream;
        }

        public void Begin()
        {
            _beginBitCount = 0;
        }

        public int End() => _beginBitCount;

        public int GetBitsReadInTransaction() => _beginBitCount;

        #region Remap base class methods

        new void Add(byte value)
        {
            Push(value);
        }

        new void AddRange(IEnumerable<byte> values)
        {
            PushRange(values);
        }

        #endregion
    }
}
