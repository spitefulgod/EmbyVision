using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmbyVision.Emby.Classes
{
    public class EmMediaSource
    {
        public string Protocol { get; set; }
        public string Id { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public bool IsRemote { get; set; }
        public bool ReadAtNativeFramerate { get; set; }
        public bool SupportsTranscoding { get; set; }
        public bool SupportsDirectStream { get; set; }
        public bool SupportsDirectPlay { get; set; }
        public bool IsInfiniteStream { get; set; }
        public bool RequiresOpening { get; set; }
        public bool RequiresClosing { get; set; }
        public bool SupportsProbing { get; set; }
        public bool RequiresLooping { get; set; }
    }
}
