using System;

namespace SpotiLy.Manager
{
    public sealed class StatManager
    {
        private static readonly Lazy<StatManager> lazy =
            new Lazy<StatManager>(() => new StatManager(), true);

        public static StatManager Instance { get { return lazy.Value; } }
        public int FreeStreams { get; set; }
        public int FreeStreamsInPlaylist { get; set; }
        public int PremiumStreams { get; set; }
        public int Follows { get; set; }
        public int SongLikes { get; set; }
        public int AlbumLikes { get; set; }
        public int FreeGenerate { get; set; }
        public int Attemps { get; set; }
        public DateTime StartDate { get; private set; }
        private StatManager()
        {
            StartDate = DateTime.Now;
        }
    }
}