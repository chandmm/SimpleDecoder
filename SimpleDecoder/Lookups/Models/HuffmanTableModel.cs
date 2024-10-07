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
using SimpleMp3Decoder.Lookups.Huffman;
using System.Drawing;

namespace SimpleMp3Decoder.Lookups.Models
{
    public class HuffmanTableModel
    {
        #region Statics and Constants and readonlys

        public static readonly string[] TableCodes = { "NORMAL", "ESC", "NOTUSED", "QUADRUPLE_A", "QUADRUPLE_B" };

        #endregion

        #region Properties

        public string TableTypeCode { get; set; } = TableCodes[0];
        public int Id { get; set; }
        public int Linbits { get; set; }
        public Point[] SpectralDataPoints { get; set; }
        public int[] HuffmanCodeLengths { get; set; }
        public string[] HuffmanCodes { get; set; }

        //public int[] HuffmanCodeValues { get; private set; } = null;

        /// <summary>
        /// Used only for Quadruple tables A and B.
        /// </summary>
        public int[] Values { get; set; }

        public int MaxHlen { get; private set; }
        public int MinHlen { get; private set; }
        public int MaxLengthX { get; private set; }
        public int MaxLengthY { get; private set; }

        public HuffmanTreeGraph HuffmanTree { get; private set; }

        #endregion

        #region Methods

        public void Initialise()
        {
            GetMaxSpectralDataPairLengths();
            BuildHuffmanTree();
        }

        private void GetMaxSpectralDataPairLengths()
        {
            if (SpectralDataPoints.Any())
            {
                MaxLengthX = SpectralDataPoints.Max(x => x.X);
                MaxLengthY = SpectralDataPoints.Max(y => y.Y);
            }
        }

        private void BuildHuffmanTree()
        {
            HuffmanTree = new HuffmanTreeGraph();

            for (int i = 0; i < HuffmanCodes.Length; i++)
            {
                var data = new HuffmanData()
                {
                    Id = Id,
                };

                if (TableTypeCode == TableCodes[3] || TableTypeCode == TableCodes[4])
                {
                    data.Value = Values[i];
                }
                else
                {
                    data.SpectralDataPoint = SpectralDataPoints[i];
                }


                HuffmanTree.Add(data, HuffmanCodes[i]);
            }
        }

        /// <summary>
        /// CAUTION: SHALLOW COPY.
        /// </summary>
        /// <remarks>
        /// After copy please explicitly set Linbits and TableCode on the clone
        /// appropriate to the cloned table where applicable.
        /// </remarks>
        /// <returns>HuffmanTable</returns>
        public HuffmanTableModel Clone()
            => (HuffmanTableModel)MemberwiseClone();
        #endregion
    }
}
