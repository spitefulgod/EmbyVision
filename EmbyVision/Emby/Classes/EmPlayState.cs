namespace EmbyVision.Emby.Classes
{
    public class EmPlayState
    {
        public bool CanSeek { get; set; }
        public bool IsPaused { get; set; }
        public bool IsMuted { get; set; }
        public string RepeatMode { get; set; }
        public int VolumeLevel { get; set; }
    }
}
