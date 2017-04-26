using System;
using System.Collections.Generic;

namespace EmbyVision.Emby.Classes
{
    public class EmSeries : IComparable<EmSeries>
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<EmSeason> Seasons { get; set; }

        public int CompareTo(EmSeries other)
        {
            return this.Name.CompareTo(other.Name);
        }

        public int SeasonIndex(int Index)
        {
            if (Seasons == null)
                return -1;
            int Counter = 0;
            foreach (EmSeason Season in Seasons)
            {
                if (Season.Index == Index)
                    return Counter;
                Counter++;
            }
            return -1;
        }
    }
}
