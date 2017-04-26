using System;

namespace EmbyVision.Emby.Classes
{
    public class EmSessionInfo
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string ApplicationVersion { get; set; }
        public string Client { get; set; }
        public DateTime LastActivityDate { get; set; }
        public string DeviceName { get; set; }
        public string DeviceId { get; set; }
        public bool SupportsRemoteControl { get; set; }
        public EmPlayState PlayState { get; set; }
    }
}
