using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatisticWMG.Model
{
    public class SpotifyInfo
    {
        public string TrackId { get; set; }
        public string TrackTitle { get; set; }
        public string AlbumId { get; set; }
        public string AlbumTitle { get; set; }
        public string Artists { get; set; }
        public string ArtistId { get; set; }
        public long StreamCount { get; set; }
        public string ReleaseDate { get; set; }
        public string Code { get; set; }
        public string Genres { get; set; }
        public string Country { get; set; }
        public string CreateDate { get; set; }
        public string LinkSpotify { get; set; }
        public string ViralArtist { get; set; }
        public string Popularity { get; set; }
        public string YoutubeLink { get; set; }
        public string ViewYoutube { get; set; }
    }
}
