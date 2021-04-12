using SpotiLy.Driver;

namespace SpotiLy.Config
{
    public class Configuration
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string AccountPath { get; set; }

        public string ProxyPath { get; set; }

        public string AlbumPath { get; set; }

        public NavigatorEnum Driver { get; set; }

        public int Threads { get; set; }

        public int MinSkip { get; set; }

        public int MaxSkip { get; set; }
        public double GeneratePlaylistRatio { get; set; }
    }
}