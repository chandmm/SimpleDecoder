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
namespace SimpleMp3Decoder.Logic
{
    public static class Crc16
    {
        private const ushort Polynomial = 0x8005;
        private const ushort InitialValue = 0xFFFF;

        public static ushort ComputeCrc(byte[] data, int start, int length)
        {
            ushort crc = InitialValue;
            for (int i = start; i < start + length; i++)
            {
                crc ^= (ushort)(data[i] << 8);
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 0x8000) != 0)
                        crc = (ushort)((crc << 1) ^ Polynomial);
                    else
                        crc = (ushort)(crc << 1);
                }
            }
            return crc;
        }
    }
}