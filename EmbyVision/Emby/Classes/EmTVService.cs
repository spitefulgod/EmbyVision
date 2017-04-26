using System.Collections.Generic;

namespace EmbyVision.Emby.Classes
{
    public class EmTVService
    {
        public string Name { get; set; }
        public string HomePageUrl { get; set; }
        public string Status { get; set; }
        public string Version { get; set; }
        public bool HasUpdateAvailable { get; set; }
        public bool IsVisible { get; set; }
        public List<EmTuner> Tuners { get; set; }
    }
}
