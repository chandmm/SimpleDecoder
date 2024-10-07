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
using SimpleMp3Decoder.Lookups.Models;

namespace SimpleMp3Decoder.Lookups.Huffman
{
    public class HuffmanNode
    {
        private static int _internamNodeCounter;

        public HuffmanNode Left { get; set; }
        public HuffmanNode Right { get; set; }
        public bool IsRoot { get; private set; }
        public bool IsLeaf => Left == null && Right == null;
        public HuffmanData Data { get; private set; }

        public HuffmanNode(bool isRoot = false)
        {
            Left = null;
            Right = null;
            IsRoot = isRoot;
            Data = null;
        }

        public void SetData(HuffmanData data)
        {
            Data = data;
        }
    }
}

