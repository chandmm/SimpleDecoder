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
using System.Text;

namespace SimpleMp3Decoder.Models
{
    public class MetadataModel
    {
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public string Year { get; set; }
        public string Comment { get; set; }
        public int GenreCode { get; set; }
        public string GenreInfo { get; set; }
        public int? TrackNumber { get; set; } = null;

        public MetadataModel(string title, string artist, string album, string year, string comment, int genreCode, int? trackNumber)
        {
            Title = title;
            Artist = artist;
            Album = album;
            Year = year;
            Comment = comment;
            GenreCode = genreCode;
            TrackNumber = trackNumber;
            GenreInfo = ConvertGenreByteToFormattedString(genreCode);
        }

        public override string ToString()
        {
            var id3StringBuilder = new StringBuilder();
            id3StringBuilder.AppendLine($"Title: {Title}");
            id3StringBuilder.AppendLine($"Artist: {Artist}");
            id3StringBuilder.AppendLine($"Album: {Album}");
            id3StringBuilder.AppendLine($"Year: {Year}");
            id3StringBuilder.AppendLine($"Comments: {Comment}");
            id3StringBuilder.AppendLine($"Genre Info: {GenreInfo}");
            id3StringBuilder.AppendLine($"Track Number: {TrackNumber.ToString()}");

            return id3StringBuilder.ToString();
        }

        private static string ConvertGenreByteToFormattedString(int genreCode)
        {
            if (genreCode < GenreLookup.Count())
            {
                return $"Code={genreCode}. Catagory={GenreLookup[genreCode]}";
            }

            return $"Code={genreCode}. ERROR: Genre code exceeded known types";
        }

        #region Genre List Lookup

        private static string[] GenreLookup = new string[] {
            "Blues", "Classic Rock", "Country", "Dance", "Disco", "Funk", "Grunge", "Hip-Hop", "Jazz", "Metal",
            "New Age", "Oldies", "Other", "Pop", "R&B", "Rap", "Reggae", "Rock", "Techno", "Industrial",
            "Alternative", "Ska", "Death Metal", "Pranks", "Soundtrack", "Euro-Techno", "Ambient", "Trip-Hop", "Vocal", "Jazz+Funk",
            "Fusion", "Trance", "Classical", "Instrumental", "Acid", "House", "Game", "Sound Clip", "Gospel", "Noise",
            "AlternRock", "Bass", "Soul", "Punk", "Space", "Meditative", "Instrumental Pop", "Instrumental Rock", "Ethnic", "Gothic",
            "Darkwave", "Techno-Industrial", "Electronic", "Pop-Folk", "Eurodance", "Dream", "Southern Rock", "Comedy", "Cult", "Gangsta Rap",
            "Top 40", "Christian Rap", "Pop/Funk", "Jungle", "Native American", "Cabaret", "New Wave", "Psychedelic", "Rave", "Showtunes",
            "Trailer", "Lo-Fi", "Tribal", "Acid Punk", "Acid Jazz", "Polka", "Retro", "Musical", "Rock & Roll", "Hard Rock",
            // Genres added by Winamp from index 80 onwards
            "Winamp: Folk", "Winamp: Folk-Rock", "Winamp: National Folk", "Winamp: Swing", "Winamp: Fast Fusion", "Winamp: Bebob", "Winamp: Latin", "Winamp: Revival", "Winamp: Celtic", "Winamp: Bluegrass",
            "Winamp: Avantgarde", "Winamp: Gothic Rock", "Winamp: Progressive Rock", "Winamp: Psychedelic Rock", "Winamp: Symphonic Rock", "Winamp: Slow Rock", "Winamp: Big Band", "Winamp: Chorus", "Winamp: Easy Listening", "Winamp: Acoustic",
            "Winamp: Humour", "Winamp: Speech", "Winamp: Chanson", "Winamp: Opera", "Winamp: Chamber Music", "Winamp: Sonata", "Winamp: Symphony", "Winamp: Booty Bass", "Winamp: Primus", "Winamp: Porn Groove",
            "Winamp: Satire", "Winamp: Slow Jam", "Winamp: Club", "Winamp: Tango", "Winamp: Samba", "Winamp: Folklore", "Winamp: Ballad", "Winamp: Power Ballad", "Winamp: Rhythmic Soul", "Winamp: Freestyle",
            "Winamp: Duet", "Winamp: Punk Rock", "Winamp: Drum Solo", "Winamp: A Capella", "Winamp: Euro-House", "Winamp: Dance Hall", "Winamp: Goa", "Winamp: Drum & Bass", "Winamp: Club-House", "Winamp: Hardcore",
            "Winamp: Terror", "Winamp: Indie", "Winamp: BritPop", "Winamp: Negerpunk", "Winamp: Polsk Punk", "Winamp: Beat", "Winamp: Christian Gangsta Rap", "Winamp: Heavy Metal", "Winamp: Black Metal", "Winamp: Crossover",
            "Winamp: Contemporary Christian", "Winamp: Christian Rock", "Winamp: Merengue", "Winamp: Salsa", "Winamp: Thrash Metal", "Winamp: Anime", "Winamp: JPop", "Winamp: Synthpop", "Winamp: Abstract", "Winamp: Art Rock",
            "Winamp: Baroque", "Winamp: Bhangra", "Winamp: Big Beat", "Winamp: Breakbeat", "Winamp: Chillout", "Winamp: Downtempo", "Winamp: Dub", "Winamp: EBM", "Winamp: Eclectic", "Winamp: Electro",
            "Winamp: Electroclash", "Winamp: Emo", "Winamp: Experimental", "Winamp: Garage", "Winamp: Global", "Winamp: IDM", "Winamp: Illbient", "Winamp: Industro-Goth", "Winamp: Jam Band", "Winamp: Krautrock",
            "Winamp: Leftfield", "Winamp: Lounge", "Winamp: Math Rock", "Winamp: New Romantic", "Winamp: Nu-Breakz", "Winamp: Post-Punk", "Winamp: Post-Rock", "Winamp: Psytrance", "Winamp: Shoegaze", "Winamp: Space Rock",
            "Winamp: Trop Rock", "Winamp: World Music", "Winamp: Neoclassical", "Winamp: Audiobook", "Winamp: Audio Theatre", "Winamp: Neue Deutsche Welle", "Winamp: Podcast", "Winamp: Indie Rock", "Winamp: G-Funk", "Winamp: Dubstep",
            "Winamp: Garage Rock", "Winamp: Psybient"
        };

        #endregion
    }
}
