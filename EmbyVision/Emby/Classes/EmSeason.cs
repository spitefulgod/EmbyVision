using System;
using System.Collections.Generic;

namespace EmbyVision.Emby.Classes
{
    public class EmSeason : IComparable<EmSeason>
    {
        public string Id { get; set; }
        public int Index { get; set; }
        public string Name { get { return "Season " + Index.ToString(); } }
        public List<EmMediaItem> Episodes { get; set; }

        public int CompareTo(EmSeason other)
        {
            return this.Index.CompareTo(other.Index);
        }
    }
}
