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
        /// <summary>
        /// Finds the next episode after the one givem
        /// </summary>
        /// <param name="Episode"></param>
        /// <returns></returns>
        public EmMediaItem FindNext(EmMediaItem Episode)
        {
            if (Episode == null || Episode.Type != "Episode")
                return null;
            bool Triggered = false;
            // Find the matching Season.
            foreach (EmSeason Season in this.Seasons)
                foreach (EmMediaItem Ep in Season.Episodes)
                    if (Ep.Id == Episode.Id)
                        Triggered = true;
                    else if (Triggered)
                        return Ep;
            return null;
        }
        /// <summary>
        /// Finds the episode before this
        /// </summary>
        /// <param name="Episode"></param>
        /// <returns></returns>
        public EmMediaItem FindPrev(EmMediaItem Episode)
        {
            if (Episode == null || Episode.SeriesId != this.Id)
                return null;
            EmMediaItem LastEpisode = null;
            // Find the matching Season.
            foreach (EmSeason Season in this.Seasons)
                foreach (EmMediaItem Ep in Season.Episodes)
                    if (Ep.Id == Episode.Id)
                        return LastEpisode;
                    else
                        LastEpisode = Ep;
            return null;
        }
    }
}
