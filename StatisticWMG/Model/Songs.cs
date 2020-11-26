using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Threading.Tasks;

namespace StatisticWMG
{
    public class Songs
    { 
        public string TrackName { get; set; }
        public string Code { get; set; }
        public string TrackArtist  { get; set; }
        public string Genres { get; set; }
        public string YoutubeUrl { get; set; }
        public string ReleaseYear { get; set; }
        public long YoutubeViewCount { get; set; }
        public string Region { get; set; }
    }
}
