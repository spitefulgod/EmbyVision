namespace EmbyVision.Emby.Classes
{
    public class EmConfiguration
    {
        public bool PlayDefaultAudioTrack { get; set; }
        public bool DisplayMissingEpisodes { get; set; }
        public bool DisplayUnairedEpisodes { get; set; }
        public string SubtitleMode { get; set; }
        public bool DisplayCollectionsView { get; set; }
        public bool EnableLocalPassword { get; set; }
        public bool HidePlayedInLatest { get; set; }
        public bool RememberAudioSelections { get; set; }
        public bool RememberSubtitleSelections { get; set; }
        public bool EnableNextEpisodeAutoPlay { get; set; }
    }
}
