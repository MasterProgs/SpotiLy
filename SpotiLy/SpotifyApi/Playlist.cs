using System.Collections.Generic;

namespace SpotiLy.SpotifyApi
{
    public class Playlist
    {
        public string id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string uri { get; set; }
        public Tracks tracks { get; set; }
    }
}