using EmbyVision.Emby.Classes;
using EmbyVision.Speech;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmbyVision.Emby
{
    /// <summary>
    /// Just a bunch of commands to state information, helps remove the bulk of the crap code 
    /// from the connector class
    /// </summary>
    public class EmbyTalkHelper
    {
        private EmbyCore Store { get; set; }

        public EmbyTalkHelper(EmbyCore Store)
        {
            this.Store = Store;
        }
        public void TalkCatalog(EmbyServer Server)
        {

            int Count = (Server.Movies.Count > 0 ? 1 : 0) + (Server.TVShows.Count > 0 ? 1 : 0) + (Server.TVChannels.Count > 0 ? 1 : 0);
            int UsedCount = 0;
            if (Count == 0)
            {
                Store.Talker.Queue("You currently have no media available");
                return;
            }
            string Sentence = "There are ";
            if (Server.Movies.Count > 0)
            {
                Sentence += UsedCount == 0 ? "" : (UsedCount == Count - 1 ? " and " : ", ");
                Sentence += string.Format("{0} movie{1}", Server.Movies.Count, Server.Movies.Count == 1 ? "" : "s");
                UsedCount++;
            }
            if (Server.TVShows.Count > 0)
            {
                Sentence += UsedCount == 0 ? "" : (UsedCount == Count - 1 ? " and " : ", ");
                Sentence += string.Format("{0} tv show{1}", Server.TVShows.Count, Server.TVShows.Count == 1 ? "" : "s");
                UsedCount++;
            }
            if (Server.TVChannels.Count > 0)
            {
                Sentence += UsedCount == 0 ? "" : (UsedCount == Count - 1 ? " and " : ", ");
                Sentence += string.Format("{0} tv channel{1}", Server.TVChannels.Count, Server.TVChannels.Count == 1 ? "" : "s");
                UsedCount++;
            }

            Sentence += " available";

            Store.Talker.Queue(Sentence);
        }
        /// <summary>
        /// Talks some additional info on the given program
        /// </summary>
        /// <param name="Program"></param>
        /// <param name="Level"></param>
        public void ListAdditionalInfo(EmMediaItem Media, byte Level)
        {
            EmMediaItem CurrentProgram = Media.CurrentProgram ?? Media;
            if (Media.CurrentProgram != null && !string.IsNullOrEmpty(Media.CurrentProgram.Name))
                Store.Talker.Queue(string.Format(Media.CurrentProgram.Name));
            if (CurrentProgram.StartDate > DateTime.Now.AddDays(-2))
            {
                Store.Talker.Queue(string.Format("Started at {0} {1}", CurrentProgram.StartDate.Hour > 12 ? CurrentProgram.StartDate.Hour - 12 : CurrentProgram.StartDate.Hour, CurrentProgram.StartDate.Minute == 0 ? "o'clock" : CurrentProgram.StartDate.Minute.ToString()));
            }
            if (CurrentProgram.EndDate > DateTime.Now)
            {
                TimeSpan EndingDiff = CurrentProgram.EndDate - DateTime.Now;
                if (EndingDiff.Minutes > 0 && EndingDiff.Hours < 4)
                    Store.Talker.Queue(string.Format("Ending in {0} minute{1}", EndingDiff.Minutes, EndingDiff.Minutes == 1 ? "" : "s"));
            }
            if (Level == 2 && !string.IsNullOrEmpty(CurrentProgram.Overview))
                Store.Talker.Queue(CurrentProgram.Overview);
        }
    }
}
