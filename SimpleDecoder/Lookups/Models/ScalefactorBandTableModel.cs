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
namespace SimpleMp3Decoder.Lookups.Models
{
    public class ScalefactorBandTableModel
    {
        #region Fields

        private int[] _reorderedTable = null;

        #endregion

        #region Properties

        public string Id => GetCompositeId();
        public double Frequency { get; set; }
        public int MaxFrequencyLines { get; set; }
        public BlockType BlockType { get; set; }
        public List<int> ScalefactorBands { get; set; } = new List<int>();
        public List<int> BandWidths { get; set; } = new List<int>();
        public List<int> StartIndexes { get; set; } = new List<int>();
        public List<int> EndIndexes { get; set; } = new List<int>();

        #endregion

        #region Methods

        private string GetCompositeId()
        {
            if (MaxFrequencyLines == 0)
            {
                return string.Empty;
            }

            return $"{Frequency}{BlockType}";
        }

        #endregion

        public int[] GetReorderTable()
        {
            if (_reorderedTable != null)
            {
                return _reorderedTable;
            }

            // SZD: converted from LAME
            int j = 0;

            _reorderedTable = new int[576];

            for (int sfb = 0; sfb < 13; sfb++)
            {
                int start = StartIndexes[sfb];
                int end = EndIndexes[sfb];
                for (int window = 0; window < 3; window++)
                    for (int i = start; i <= end; i++)
                        _reorderedTable[3 * i + window] = j++;
            }

            return _reorderedTable;
        }
    }

    public enum BlockType
    {
        Long,
        Short,
        Unknown
    }
}
