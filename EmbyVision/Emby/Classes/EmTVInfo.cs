using Newtonsoft.Json;
using System.Collections.Generic;

namespace EmbyVision.Emby.Classes
{
    public class EmTVInfo
    {
        public List<EmTVService> Services { get; set; }
        public bool IsEnabled { get; set; }
        [JsonProperty("EnabledUsers")]
        public List<string> EnabledUsers { get; set; }
    }
}
