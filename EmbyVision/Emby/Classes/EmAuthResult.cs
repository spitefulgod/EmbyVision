namespace EmbyVision.Emby.Classes
{
    public class EmAuthResult
    {
        public EmUser User { get; set; }
        public EmSessionInfo SessionInfo { get; set; }
        public string AccessToken { get; set; }
        public string ServerId { get; set; }
    }
}
