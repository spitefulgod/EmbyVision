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
    /// Used to store working variables that can be shared between the classes, all just a big
    /// farve to reduce the amount of code that was in a single class.
    /// </summary>
    public class EmbyCore : IDisposable
    {
        public List<EmbyServer> Servers { get; set; }
        public EmbyServer SelectedServer { get; set; }
        public EmSeries SelectedSeries { get; set; }
        public EmSeason SelectedSeason { get; set; }
        public EmMediaItem SelectedEpisode { get; set; }
        public Talker Talker { get; set; }
        public Listener Listener { get; set; }
        public EmbyConnectionHelper ConnectionHelper { get; private set; }
        public EmbyInterpretCommands InterpretCommands { get; private set; }
        public EmbySetCommands SetCommands { get; private set; }
        public EmbyTalkHelper TalkHelper { get; private set; }
        public bool IsConnected
        {
            get
            {
                return SelectedServer != null && SelectedServer.SelectedClient != null;
            }
        }
        /// <summary>
        /// Clears the selected series information
        /// </summary>
        public void ClearMedia()
        {
            SelectedSeason = null;
            SelectedSeries = null;
            SelectedEpisode = null;
        }
        /// <summary>
        /// Clean up.
        /// </summary>
        public void Dispose()
        {
            // CLass clean up
            ConnectionHelper = null;
            SetCommands = null;
            if(InterpretCommands != null)
                InterpretCommands.Dispose();
            InterpretCommands = null;
            TalkHelper = null;
            // Var clean up
            ClearMedia();
            SelectedSeries = null;
            Talker = null;
            Listener = null;
            if (Servers != null)
                Servers.Clear();
            Servers = null;
        }

        //Set up
        public EmbyCore(Talker Talker, Listener Listener)
        {
            this.Listener = Listener;
            this.Talker = Talker;
            Servers = new List<EmbyServer>();
            ConnectionHelper = new EmbyConnectionHelper(this);
            SetCommands = new EmbySetCommands(this);
            InterpretCommands = new EmbyInterpretCommands(this);
            TalkHelper = new EmbyTalkHelper(this);
        }
        /// <summary>
        /// Starts the main class
        /// </summary>
        public async Task Start()
        {
            if(ConnectionHelper != null)
                await ConnectionHelper.RefreshServerList();
        }
    }
}
