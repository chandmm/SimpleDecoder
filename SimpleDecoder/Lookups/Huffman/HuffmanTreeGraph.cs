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
    public class HuffmanTreeGraph
    {
        private HuffmanNode _root;
        private HuffmanNode _currentNode;

        public HuffmanTreeGraph()
        {
            _root = new HuffmanNode(isRoot: true);
            ResetToRoot();
        }

        public void Add(HuffmanData data, string code)
        {
            var index = 0;
            _currentNode = _root;

            foreach (var bit in code.ToCharArray())
            {
                var foundNode = FindNode(int.Parse(bit.ToString()), _currentNode);

                if (foundNode == _currentNode)
                {
                    var node = new HuffmanNode();

                    if (bit == '1')
                    {
                        _currentNode.Right = node;
                    }
                    else
                    {
                        _currentNode.Left = node;
                    }

                    foundNode = node;
                }

                index++;
                _currentNode = foundNode;
            }

            _currentNode.SetData(data);
        }

        private HuffmanNode FindNode(int bit, HuffmanNode currentNode)
        {
            var node = currentNode;

            if (bit == 1)
            {
                node = currentNode.Right;
            }
            else if (bit == 0)
            {
                node = currentNode.Left;
            }

            return node ?? currentNode;
        }

        public HuffmanNode Find(int bit)
        {
            _currentNode = FindNode(bit, _currentNode);

            return _currentNode;
        }

        public HuffmanNode GetRoot()
        {
            _currentNode = _root;

            return _currentNode;
        }

        public void ResetToRoot() => _currentNode = _root;
    }

}
