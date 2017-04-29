using EmbyVision.Base;
using EmbyVision.Emby.Classes;
using EmbyVision.Rest;
using EmbyVision.Speech;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EmbyVision.Rest.RestClient;

namespace EmbyVision.Emby
{
    public class EmbyConnector : IDisposable
    {
        private class MediaContextStore
        {
            public EmSeries SelectedSeries { get; set; }
            public EmSeason SelectedSeason { get; set; }
            public EmMediaItem SelectedEpisode { get; set; }
            public void Clear()
            {
                SelectedSeason = null;
                SelectedSeason = null;
            }
        }
        private MediaContextStore Store { get; set; }
        private Listener Listener { get; set; }
        private Talker Talker { get; set; }
        public string ExternalIPAddr { get; private set; }
        private EmbyServerHelper ConnectedServer { get; set; }
        private EmbyServer SelectedServer { get; set; }
        public bool IsConnected
        {
            get
            {
                return SelectedServer != null && SelectedServer.SelectedClient != null;
            }
        }

        public EmbyConnector(Talker Talker, Listener Listener)
        {
            this.Talker = Talker;
            this.Listener = Listener;
            Listener.SpeechRecognised += Listener_SpeechRecognised;
            ConnectedServer = new EmbyServerHelper();
            Store = new MediaContextStore();
        }
        /// <summary>
        /// Received a speech command, check its for us and the action it.
        /// </summary>
        private async void Listener_SpeechRecognised(object sender, string Assembly, string Context, string SpokenCommand, int CommandIndex, Dictionary<string, object> SelectList)
        {
            if (Assembly != "EmbyBase" && Assembly != "Emby")
                return;
            switch (Context)
            {
                case "HowMany":
                    if (SelectList["Type"].ToString() == "Server")
                    {
                        Talker.Speak(string.Format("You currently have {0} server{1} available", ConnectedServer.Servers.Count, ConnectedServer.Servers.Count == 1 ? "" : "s"));
                        break;
                    }
                    else
                    {
                        if (SelectedServer == null)
                        { 
                            Talker.Speak("You are currently not connected to a server, try saying List Servers and then connect to a server");
                            break;
                        }
                        switch (SelectList["Type"])
                        {
                            case "Movie":
                                Talker.Speak(string.Format("You currently have {0} Movie{1} available", SelectedServer.Movies.Count, SelectedServer.Movies.Count == 1 ? "" : "s"));
                                break;
                            case "TV":
                                Talker.Speak(string.Format("You currently have {0} TV Series{1} available", SelectedServer.TVShows.Count, SelectedServer.TVShows.Count == 1 ? "" : "s"));
                                break;
                            case "Channel":
                                Talker.Speak(string.Format("You currently have {0} Channel{1} available", SelectedServer.TVChannels.Count, SelectedServer.TVChannels.Count == 1 ? "" : "s"));
                                break;
                        }
                    }
                    break;
                case "ChangeClient":
                    EmClient SelectedClient = (EmClient)SelectList["Client"];
                    if(SelectedServer.SetClient(SelectedClient))
                        Talker.Speak(string.Format("Connected to the client {0}", SelectedClient.Client));
                    else
                        Talker.Speak("Unable to find the select client, try saying List Clients to refresh");

                    break;
                case "ListClients":
                    // Refresh the server client list
                    RestResult<List<EmClient>> ClientResult = SelectedServer.RefreshClients();
                    if (ClientResult.Success)
                    {
                        Talker.Speak(SelectedServer.Clients, "Client", "Client", true);
                        SetCommands();
                    }
                    else
                        Talker.Speak("Unable to refresh the servers client list");
                    break;
                case "ListItems":
                    if (SelectList["Type"].ToString() == "Server")
                    {
                        Talker.Speak(ConnectedServer.Servers, "ServerName", "Server", true);
                        if(SelectedServer != null)
                            Talker.Queue(string.Format("You are currently connected to the server {0}", SelectedServer.ServerName));
                        break;
                    }
                    else
                    {
                        if (SelectedServer == null)
                        {
                            Talker.Speak("You are currently not connected to a server, try saying List Servers and then connect to a server");
                            break;
                        }
                        switch (SelectList["Type"].ToString())
                        {
                            case "Movie":
                                Talker.Speak(SelectedServer.Movies, "Name", "Movie", true);
                                break;
                            case "Channel":
                                Talker.Speak(SelectedServer.TVChannels, "Name", "Channel", true);
                                break;
                            case "TV":
                                Talker.Speak(SelectedServer.TVShows, "Name", "TV Series", true);
                                break;
                        }
                    }
                    Store.Clear();
                    break;
                case "ChangeServer":
                    EmbyServer NewServer = (EmbyServer)SelectList["Server"];
                    if(SelectedServer != null && NewServer.Conn.Id == SelectedServer.Conn.Id)
                    {
                        Talker.Speak(string.Format("You are already connected to the server {0}", SelectedServer.ServerName));
                        break;
                    }
                    Talker.Stop();
                    EmbyServer CurrentServer = SelectedServer;
                    Options.Instance.ConnectedClientId = null;
                    Options.Instance.SaveOptions();
                    if(!ConnectToServer(NewServer, true).Success) { 
                        if(CurrentServer == null)
                            Talker.Speak(string.Format("Unable to connect to the server {0}", NewServer.ServerName));
                        else
                            Talker.Speak(string.Format("Unable to connect to the server {0}, will attempt reconnection to {1}", NewServer.ServerName, CurrentServer.ServerName));
                        ConnectToServer(CurrentServer, true);
                    }
                    break;
                case "PlayItem":
                    // Play a movie or TV channel
                    EmMediaItem PlayItem = (EmMediaItem)SelectList["PlayItem"];
                    switch (PlayItem.Type)
                    {
                        case "Movie":
                            Talker.Speak(string.Format("Playing the movie {0}", PlayItem.Name));
                            break;
                        case "TvChannel":
                            Talker.Speak(string.Format("Switching to channel {0}", PlayItem.Name));
                            break;
                    }
                    RestResult PlayResult = await SelectedServer.PlayFile(PlayItem, true);
                    // Additional information on what is left
                    Talker.Queue(GetStartEnd(PlayItem));
                    Store.Clear();
                    break;
                case "Pause":
                    // Pause or play an item if the client is currently playing.
                    SelectedServer.SelectedClient.Refresh();
                    if (SelectedServer.SelectedClient.NowPlayingItem != null)
                        if (SelectedServer.SelectedClient.PlayState.IsPaused)
                        {
                            // Send a resume command
                            RestResultBase PauseResult = SelectedServer.SelectedClient.Resume(false);
                            if (PauseResult.Success)
                                Talker.Speak(string.Format("Resuming {0}", SelectedServer.SelectedClient.NowPlayingItem.Name));
                        }
                        else
                        {
                            // Send a pause command
                            RestResultBase PauseResult = SelectedServer.SelectedClient.Pause(false);
                            if (PauseResult.Success)
                                Talker.Speak(string.Format("Pausing {0}", SelectedServer.SelectedClient.NowPlayingItem.Name));
                        }
                    break;
                case "Stop":
                    SelectedServer.SelectedClient.Refresh();
                    if (SelectedServer.SelectedClient.NowPlayingItem != null)
                    {
                        string Name = SelectedServer.SelectedClient.NowPlayingItem.Name;
                        RestResultBase StopResult = SelectedServer.SelectedClient.Stop(false);
                        if (StopResult.Success)
                            Talker.Speak(string.Format("Stopped {0}", Name));
                    }
                    break;
                case "CheckAudioTrack":
                    if (SelectedServer == null)
                    {
                        Talker.Speak("You are currently not connected to a server, try saying List Servers and then connect to a server");
                        break;
                    }
                    SelectedServer.SelectedClient.Refresh();
                    if (SelectedServer.SelectedClient.NowPlayingItem == null)
                    {
                        Talker.Speak("There is no media currently playing");
                        return;
                    }
                    List<string> AudioTracks = new List<string>();
                    if (SelectedServer.SelectedClient.NowPlayingItem.MediaStreams != null)
                        foreach (EmMediaStream Stream in SelectedServer.SelectedClient.NowPlayingItem.MediaStreams)
                            if (Stream.Type == "Audio")
                                AudioTracks.Add(Stream.DisplayTitle);

                    Talker.Speak(AudioTracks, "Audio Track", true);
                    break;
                case "SwitchAudioTrack":
                    if(SelectedServer == null)
                    {
                        Talker.Speak("You are currently not connected to a server, try saying List Servers and then connect to a server");
                        break;
                    }
                    SelectedServer.SelectedClient.Refresh();
                    if (SelectedServer.SelectedClient.NowPlayingItem == null)
                    {
                        Talker.Speak("There is no media currently playing");
                        return;
                    }
                    int Track = int.Parse(SelectList["Track"].ToString());
                    // Send the command to switch to the next audio track.
                    RestResultBase Result = SelectedServer.SelectedClient.SwitchAudioChannel(Track, false);
                    if (!Result.Success)
                        Talker.Speak("Unable to switch audio channel", Result.Error);
                    else
                        Talker.Speak(string.Format("Audio switched to track {0}", Track));
                    break;
                case "RefreshMedia":
                    if (SelectedServer == null)
                    {
                        Talker.Speak("You are currently not connected to a server, try saying List Servers and then connect to a server");
                        break;
                    }
                    SelectedServer.RefreshCatalog();
                    TalkCatalog(true);
                    // Say what we have.
                    break;
                case "ProgramInfo":
                    if(SelectedServer == null)
                    {
                        Talker.Speak("You are currently not connected to a server, try saying List Servers and then connect to a server");
                        break;
                    }
                    RestResult<EmClient> PIClient = SelectedServer.SelectedClient.Refresh();
                    if (!PIClient.Success)
                    {
                        Talker.Speak(string.Format("Unable to retrieve client information : {0}", PIClient.Error));
                        break;
                    }
                    if (PIClient.Response.NowPlayingItem == null)
                    {
                        Talker.Speak("You are currently not watching anything");
                        break;
                    }
                    EmMediaItem PIWatchingItem = SelectedServer.SelectedClient.NowPlayingItem;
                    await PIWatchingItem.Refresh(this.SelectedServer);
                    // Overview of current playing things.
                    Talker.Stop();
                    ListAdditionalInfo(PIWatchingItem, 2);
                    break;
                case "WhatAmIWatching":
                    if (SelectedServer == null)
                    {
                        Talker.Speak("You are currently not connected to a server, try saying List Servers and then connect to a server");
                        break;
                    }
                    RestResult<EmClient> WIAClient = SelectedServer.SelectedClient.Refresh();
                    if (!WIAClient.Success)
                    {
                        Talker.Speak(string.Format("Unable to retrieve client information : {0}", WIAClient.Error));
                        break;
                    }
                    if (WIAClient.Response.NowPlayingItem == null)
                    {
                        Talker.Speak("You are currently not watching anything");
                        break;
                    }
                    EmMediaItem NWWatchingItem = SelectedServer.SelectedClient.NowPlayingItem;
                    await NWWatchingItem.Refresh(this.SelectedServer);
                    bool StateEnd = false;
                    switch (NWWatchingItem.Type)
                    {
                        case "TvChannel":
                            Talker.Speak(string.Format("You are currently watching the TV Channel {0}", NWWatchingItem.Name));
                            StateEnd = true;
                            break;
                        case "Movie":
                            Talker.Speak(string.Format("You are currently watching the Movie {0}", NWWatchingItem.Name));
                            StateEnd = true;
                            break;
                        case "Episode":
                            Talker.Speak(string.Format("You are currently watching season {0} episode {1} of the TV Series {2}", NWWatchingItem.ParentIndexNumber, NWWatchingItem.IndexNumber, NWWatchingItem.SeriesName));
                            if (!string.IsNullOrEmpty(NWWatchingItem.Name))
                                Talker.Queue(NWWatchingItem.Name);
                            StateEnd = true;
                            break;
                    }
                    // Tell the user when the program is ending.
                    if (StateEnd)
                        ListAdditionalInfo(NWWatchingItem, 1);
                    break;
                case "RefreshServerList":
                    RefreshServerList(false);
                    break;
                case "GotoTVShow":
                    Store.SelectedSeries = (EmSeries)SelectList["TVShow"];
                    SelectedServer.SelectedClient.ShowContent(Store.SelectedSeries);
                    Talker.Speak(Store.SelectedSeries.Name);
                    Talker.Queue(string.Format("There {0} {1} season{2}", Store.SelectedSeries.Seasons.Count == 1 ? "is" : "are", Store.SelectedSeries.Seasons.Count, Store.SelectedSeries.Seasons.Count == 1 ? "" : "s"));
                    //Talker.Queue(SelectedSeries.Seasons, "Name", "Season", false);
                    Store.SelectedSeason = null;
                    break;
                case "ListTVSeasons":
                    if (SelectList != null && SelectList.ContainsKey("TVShow"))
                        Store.SelectedSeries = (EmSeries)SelectList["TVShow"];
                    if (Store.SelectedSeries == null)
                    {
                        Talker.Speak("You have not selected any TV show, try saying List TV Shows");
                        break;
                    }
                    Talker.Speak(Store.SelectedSeries.Seasons, "Name", "Season", false);
                    Store.SelectedSeason = null;
                    break;
                case "GoToSeason":
                    if (SelectList.ContainsKey("TVShow"))
                        Store.SelectedSeries = (EmSeries)SelectList["TVShow"];
                    if (Store.SelectedSeries == null)
                    {
                        Talker.Speak("You have not selected any TV show, try saying List TV Shows");
                        break;
                    }
                    if (SelectList != null && SelectList.ContainsKey("Season"))
                    {
                        int EpSeason = int.Parse(SelectList["Season"].ToString());
                        int SeasonIndex = Store.SelectedSeries.SeasonIndex(EpSeason);
                        if (SeasonIndex < 0)
                        {
                            Talker.Speak(string.Format("You do not have season {0} of {1}", EpSeason, Store.SelectedSeries.Name));
                            break;
                        }
                        Store.SelectedSeason = Store.SelectedSeries.Seasons[SeasonIndex];
                    }
                    SelectedServer.SelectedClient.ShowContent(Store.SelectedSeason);
                    Talker.Speak(Store.SelectedSeries.Name);
                    Talker.Queue(Store.SelectedSeason.Name);
                    Talker.Queue(string.Format("There {0} {1} episode{2}", Store.SelectedSeason.Episodes.Count == 1 ? "is" : "are", Store.SelectedSeason.Episodes.Count, Store.SelectedSeason.Episodes.Count == 1 ? "" : "s"));
                    break;
                case "ListTVEpisodes":
                    if (SelectList != null && SelectList.ContainsKey("TVShow"))
                        Store.SelectedSeries = (EmSeries)SelectList["TVShow"];
                    if (Store.SelectedSeries == null)
                    {
                        Talker.Speak("You have not selected any TV Show, try saying List TV Shows");
                        break;
                    }
                    if (SelectList != null && SelectList.ContainsKey("Season"))
                    {
                        int EpSeason = int.Parse(SelectList["Season"].ToString());
                        int SeasonIndex = Store.SelectedSeries.SeasonIndex(EpSeason);
                        if (SeasonIndex < 0)
                        {
                            Talker.Speak(string.Format("You do not have season {0} of {1}", EpSeason, Store.SelectedSeries.Name));
                            break;
                        }
                        Store.SelectedSeason = Store.SelectedSeries.Seasons[SeasonIndex];
                    }
                    if (Store.SelectedSeason == null)
                    {
                        Talker.Speak("You have not selected any season, try saying List Seasons");
                        break;
                    }
                    Talker.Speak(Store.SelectedSeason.Episodes, "EpisodeName", "Episode", false);
                    break;
                case "PlayTVEpisode":
                    if (SelectList != null && SelectList.ContainsKey("TVShow"))
                        Store.SelectedSeries = (EmSeries)SelectList["TVShow"];
                    if (Store.SelectedSeries == null)
                    {
                        Talker.Speak("You have not selected any TV Show, try saying List TV Shows");
                        break;
                    }
                    if (SelectList != null && SelectList.ContainsKey("Season"))
                    {
                        int EpSeason = int.Parse(SelectList["Season"].ToString());
                        int SeasonIndex = Store.SelectedSeries.SeasonIndex(EpSeason);
                        if (SeasonIndex < 0)
                        {
                            Talker.Speak(string.Format("You do not have season {0} of {1}", EpSeason, Store.SelectedSeries.Name));
                            break;
                        }
                        Store.SelectedSeason = Store.SelectedSeries.Seasons[SeasonIndex];
                    }
                    if (Store.SelectedSeason == null)
                    {
                        Talker.Speak("You have not selected any season, try saying List Seasons");
                        break;
                    }
                    int EpEpisode = 0;
                    if (SelectList != null && SelectList.ContainsKey("Episode"))
                        EpEpisode = int.Parse(SelectList["Episode"].ToString());
                    if (EpEpisode == 0)
                    {
                        Talker.Speak("You have not selected an episode, try saying List Episodes");
                        break;
                    }
                    Store.SelectedEpisode = null;
                    // Make sure the episode exists
                    foreach (EmMediaItem Episode in Store.SelectedSeason.Episodes)
                        if (Episode.IndexNumber == EpEpisode)
                        {
                            Store.SelectedEpisode = Episode;
                            break;
                        }
                    if (Store.SelectedEpisode == null)
                    {
                        Talker.Speak(string.Format("Episode {0} for {1} Season {2} does not exist", EpEpisode, Store.SelectedSeason.Index, Store.SelectedSeries.Name));
                        break;
                    }
                    PlayEpisode();
                    break;
                case "ContinueTVShow":
                    if (SelectList != null && !SelectList.ContainsKey("TVShow"))
                        break;
                    // We have a series, we should go through it and see the last watchde episode then start from there, unfortunately this means updating all the shows so we can get the last watched data.
                    Store.SelectedSeries = (EmSeries)SelectList["TVShow"];
                    // Update these items.
                    await Common.UpdateMediaItems(SelectedServer, Store.SelectedSeries);
                    // Now we need to get the last moved item, then play from there.
                    Store.SelectedEpisode = Store.SelectedSeries.Seasons[0].Episodes[0];
                    Store.SelectedSeason = Store.SelectedSeries.Seasons[0];
                    DateTime LastEpDate = DateTime.MinValue;
                    foreach (EmSeason Season in Store.SelectedSeries.Seasons)
                        foreach(EmMediaItem Episode in Season.Episodes)
                            if(Episode.UserData != null && Episode.UserData.LastPlayedDate != null && Episode.UserData.LastPlayedDate > LastEpDate)
                            {
                                LastEpDate = (DateTime)Episode.UserData.LastPlayedDate;
                                Store.SelectedEpisode = Episode;
                                Store.SelectedSeason = Season;
                            }
                    if (Store.SelectedEpisode != null)
                        PlayEpisode();
                    break;
                case "Restart":
                    // Restarts the current media item
                    SelectedServer.SelectedClient.Refresh();
                    if (SelectedServer.SelectedClient.NowPlayingItem == null)
                    {
                        Talker.Speak("You are not currently watching any media");
                        break;
                    }
                    if(SelectedServer.SelectedClient.NowPlayingItem.Type == "TvChannel")
                    {
                        Talker.Speak("You cannot restart Live TV programs");
                        break;
                    }
                    // Send the command to restart the current media
                    RestResultBase RestartResult = SelectedServer.SelectedClient.SetPosition(0, false);
                    if (RestartResult.Success)
                        Talker.Speak("Restart");
                    else
                        Talker.Speak(string.Format("Unable to restart the current item {0}", RestartResult.Error));
                    
                    break;
            }
        }
        /// <summary>
        /// Plays the episode.
        /// </summary>
        /// <param name="Episode"></param>
        public async void PlayEpisode()
        {
            RestResult TVPlayResult = await SelectedServer.PlayFile(Store.SelectedEpisode, true);
            if (!TVPlayResult.Success)
                Talker.Queue(string.Format("Unable to play the specified item, {0}", TVPlayResult.Error));
            else
                Talker.Speak(string.Format("Playing season {0} episode {1} of {2} {3}", Store.SelectedSeason.Index, Store.SelectedEpisode.IndexNumber, Store.SelectedSeries.Name, GetStartEnd(Store.SelectedEpisode)));
        }
        /// <summary>
        /// Gets the start / end position of a media item
        /// </summary>
        /// <param name="Item"></param>
        /// <returns></returns>
        private string GetStartEnd(EmMediaItem Item)
        {
            string EpPlayState = "";
            if(Item.UserData != null && Item.UserData.PlaybackPositionTicks > 0)
                EpPlayState += string.Format("starting from {0}", GetPositionTime(Item.UserData.PlaybackPositionTicks));
            TimeSpan Left = TimeSpan.FromTicks(Item.RunTimeTicks) - (Item.UserData == null ? TimeSpan.FromSeconds(0) : TimeSpan.FromTicks(Item.UserData.PlaybackPositionTicks));
            EpPlayState += GetPositionTime(Left.Ticks, " there is", "remaining");
            return EpPlayState;
        }
        /// <summary>
        /// returns as speech entry for a tick time
        /// </summary>
        /// <param name="Ticks"></param>
        private string GetPositionTime(long Ticks, string Prefix = null, string Suffix = null)
        {
            string Return = "";
            TimeSpan Length = TimeSpan.FromTicks(Ticks);
            if (Length.Hours > 0) {
                Return += string.Format("{0} hour{1} ", Length.Hours, Length.Hours > 1 ? "s" : "");
                Length.Add(new TimeSpan(-Length.Hours, 0, 0));
            }
            if (Length.Minutes > 0)
            {
                Return += string.Format("{0} minute{1}", Length.Minutes, Length.Minutes > 1 ? "s" : "");
                Length.Add(new TimeSpan(0, -Length.Minutes, 0));
            }

            return (Return != "" && !string.IsNullOrEmpty(Prefix) ? Prefix + " " : "") + Return + (Return != "" && !string.IsNullOrEmpty(Suffix) ? " " + Suffix : "");
        }
        /// <summary>
        /// Sets default commands and any media content commands from the selected server.
        /// </summary>
        /// <returns></returns>
        public void SetCommands()
        {
            // Defautl context stuff, just to avoid duplications later on the commands
            List<SpeechContextItem> TV = new List<SpeechContextItem>()
            {
                new SpeechContextItem("TV Show", "TV"),
                new SpeechContextItem("TV Shows", "TV"),
                new SpeechContextItem("Series", "TV"),
                new SpeechContextItem("TV Series", "TV"),
                new SpeechContextItem("TV Programs", "TV"),
                new SpeechContextItem("TV Program", "TV"),
                new SpeechContextItem("Box Set", "TV"),
                new SpeechContextItem("Box Sets", "TV")
            };
            List<SpeechContextItem> Movie = new List<SpeechContextItem>()
            {
                new SpeechContextItem("Film", "Movie"),
                new SpeechContextItem("Films", "Movie"),
                new SpeechContextItem("Movie", "Movie"),
                new SpeechContextItem("Movies", "Movie"),
                new SpeechContextItem("Feature", "Movie"),
                new SpeechContextItem("Feature Film", "Movie")
            };
            List<SpeechContextItem> Channels = new List<SpeechContextItem>()
            {
                new SpeechContextItem("TV Channel", "Channel"),
                new SpeechContextItem("Channel", "Channel"),
                new SpeechContextItem("TV Channels", "Channel"),
                new SpeechContextItem("Channels", "Channel"),
                new SpeechContextItem("Program", "Channel")
            };
            List<SpeechContextItem> Servers = new List<SpeechContextItem>()
            {
                new SpeechContextItem("Servers", "Server"),
                new SpeechContextItem("Server", "Server"),
                new SpeechContextItem("Media Center", "Server"),
                new SpeechContextItem("Media Centers", "Server"),
                new SpeechContextItem("Machines", "Server"),
                new SpeechContextItem("Computer", "Server"),
                new SpeechContextItem("Computers", "Server")
            };
            List<string> PlayCommands = new List<string>(new string[] { "Play", "Watch", "Start", "Continue", "Resume" });
            List<string> Clients = new List<string>(new string[] { "Client", "Clients", "Software Clients", "Software Client" });
            List<SpeechContextItem> All = new List<SpeechContextItem>();
            All.AddRange(Movie);
            All.AddRange(TV);
            All.AddRange(Channels);
            All.AddRange(Servers);

            List<VoiceCommand> Commands = new List<VoiceCommand>();
            // Commands, first the standard, non server restricted stuff, this only needs to be set once, as it works regardless of server.
            if (!Listener.HasCommands("EmbyBase"))
            {
                Commands.Add(new VoiceCommand()
                {
                    Name = "ChangeServer",
                    Commands = new List<SpeechItem>()
                    {
                        new SpeechItem(
                            new CommandList("Switch", "Change", "Connect"),
                            new OptionalCommandList("to"),
                            new OptionalCommandList("the"),
                            new SelectCommandList("Temp", false, Servers),
                            new SelectCommandList("Server", true, ConnectedServer.Servers, "ServerName")
                        )
                    }
                });
                Commands.Add(new VoiceCommand()
                {
                    Name = "RefreshServerList",
                    Commands = new List<SpeechItem>()
                    {
                        new SpeechItem(
                            new CommandList("Refresh", "Update"),
                            new OptionalCommandList("the"),
                            new OptionalCommandList("current", "available"),
                            new OptionalCommandList("list of"),
                            new OptionalCommandList("current", "available"),
                            new SelectCommandList("Temp", false, Servers),
                            new OptionalCommandList("list")
                        )
                    }
                });
                Commands.Add(new VoiceCommand()
                {
                    Name = "HowMany",
                    Commands = new List<SpeechItem>()
                    {
                        new SpeechItem(
                            new CommandList("How many"),
                            new SelectCommandList("Type", false, All),
                            new OptionalCommandList("do I have","are available", "are there", "are connected")
                        )
                    }
                });
                Commands.Add(new VoiceCommand()
                {
                    Name = "ListItems",
                    Commands = new List<SpeechItem>()
                    {
                        new SpeechItem(
                                new CommandList("List", "Read", "Show"),
                                new OptionalCommandList("all"),
                                new OptionalCommandList("available"),
                                new SelectCommandList("Type", false, All),
                                new OptionalCommandList("available"),
                                new OptionalCommandList("on the server")
                                ),
                        new SpeechItem(
                                new CommandList("What"),
                                new SelectCommandList("Type", false, All),
                                new CommandList("are available"),
                                new OptionalCommandList("on the server")
                               )
                    }
                });
                Commands.Add(new VoiceCommand()
                {
                    Name = "CheckAudioTrack",
                    Commands = new List<SpeechItem>()
                    {
                        new SpeechItem(
                                new CommandList("How Many", "What"),
                                new CommandList("audio", "sound", "language"),
                                new CommandList("tracks", "streams", "channel", "channels"),
                                new OptionalCommandList("are", "are available", "does this", "are there"),
                                new OptionalCommandList("on this"),
                                new OptionalCommandList(All),
                                new OptionalCommandList("have", "contain")
                            )
                    }
                });
                Commands.Add(new VoiceCommand()
                {
                    Name = "SwitchAudioTrack",
                    Commands = new List<SpeechItem>()
                    {
                        new SpeechItem(
                                new CommandList("Play","Switch","Swap","Listen"),
                                new OptionalCommandList("to"),
                                new CommandList("audio", "sound", "language"),
                                new CommandList("track", "stream", "channel"),
                                new OptionalCommandList("number"),
                                new SelectCommandList("Track", false, Common.NumberList(1, 10)))
                    }
                });
                Commands.Add(new VoiceCommand()
                {
                    Name = "RefreshMedia",
                    Commands = new List<SpeechItem>()
                    {
                        new SpeechItem(
                            new CommandList("Refresh", "Update"),
                            new CommandList("Media")
                            )
                    }
                });
                Commands.Add(new VoiceCommand()
                {
                    Name = "WhatAmIWatching",
                    Commands = new List<SpeechItem>()
                    {
                        new SpeechItem(
                            new CommandList("What am i watching")
                            )
                    }
                });
                Listener.CreateGrammarList("EmbyBase", Commands);
            }

            // Now the server specific items.
            Commands.Clear();
            if (SelectedServer != null)
            {
                Commands.Add(new VoiceCommand()
                {
                    Name = "PlayItem",
                    Commands = new List<SpeechItem>()
                    {
                        new SpeechItem(
                                new CommandList(PlayCommands),
                                new OptionalCommandList("viewing", "watching"),
                                new OptionalCommandList("the"),
                                new SelectCommandList("Type", false, Movie),
                                new OptionalCommandList("number"),
                                new SelectCommandList("PlayItem", true, SelectedServer.Movies, "Name")
                            ),
                        new SpeechItem(
                                new CommandList(PlayCommands),
                                new OptionalCommandList("viewing", "watching"),
                                new OptionalCommandList("the"),
                                new SelectCommandList("Type", false, Channels),
                                new SelectCommandList("PlayItem", true, SelectedServer.TVChannels, "Name")
                            ),
                        new SpeechItem(
                                new CommandList(PlayCommands),
                                new OptionalCommandList("viewing", "watching"),
                                new OptionalCommandList("the"),
                                new SelectCommandList("Type", false, Channels),
                                new CommandList("number"),
                                new SelectCommandList("PlayItem", true, SelectedServer.TVChannels, "ChannelNumber")
                            )
                    }
                });
                Commands.Add(new VoiceCommand()
                {
                    Name = "Pause",
                    Commands = new List<SpeechItem>()
                    {
                        new SpeechItem(
                                new CommandList("Pause"),
                                new OptionalCommandList("the"),
                                new OptionalCommandList(All)
                            ),
                        new SpeechItem(
                                new CommandList("Unpause", "Resume", "Play"),
                                new OptionalCommandList("the"),
                                new OptionalCommandList(All)
                            )
                    }
                });
                Commands.Add(new VoiceCommand()
                {
                    Name = "Stop",
                    Commands = new List<SpeechItem>()
                    {
                        new SpeechItem(
                                new CommandList("Stop"),
                                new OptionalCommandList("the"),
                                new OptionalCommandList(All)
                            )
                    }
                });
                Commands.Add(new VoiceCommand()
                {
                    Name = "GotoTVShow",
                    Commands = new List<SpeechItem>()
                    {
                        new SpeechItem(
                                new CommandList("Go to", "Show", "Select"),
                                new OptionalCommandList(TV),
                                new SelectCommandList("TVShow",true,SelectedServer.TVShows,"Name")
                            )
                    }
                });
                Commands.Add(new VoiceCommand()
                {
                    Name = "ListTVSeasons",
                    Commands = new List<SpeechItem>()
                    {
                        new SpeechItem(
                                new CommandList("List", "Read"),
                                new OptionalCommandList("all"),
                                new CommandList("seasons")
                            ),
                        new SpeechItem(
                                new CommandList("List", "Read"),
                                new OptionalCommandList("all"),
                                new CommandList("seasons"),
                                new OptionalCommandList("for"),
                                new OptionalCommandList("the"),
                                new OptionalCommandList(TV),
                                new SelectCommandList("TVShow",true,SelectedServer.TVShows,"Name")
                            )
                    }
                });
                Commands.Add(new VoiceCommand()
                {
                    Name = "GoToSeason",
                    Commands = new List<SpeechItem>()
                    {
                        new SpeechItem(
                                new CommandList("Go to", "Show", "Select"),
                                new OptionalCommandList("Season"),
                                new SelectCommandList("Season", false, Common.NumberList(1, 20))
                            ),
                        new SpeechItem(
                                new CommandList("Go to", "Show", "Select"),
                                new OptionalCommandList("Season"),
                                new SelectCommandList("Season", false, Common.NumberList(1, 20)),
                                new CommandList("of"),
                                new OptionalCommandList(TV),
                                new SelectCommandList("TVShow",true,SelectedServer.TVShows,"Name")
                            ),
                        new SpeechItem(
                                new CommandList("Go to", "Show", "Select"),
                                new OptionalCommandList(TV),
                                new SelectCommandList("TVShow",true,SelectedServer.TVShows,"Name"),
                                new OptionalCommandList("Season"),
                                new SelectCommandList("Season", false, Common.NumberList(1, 20))
                            )
                    }
                });
                Commands.Add(new VoiceCommand()
                {
                    Name = "ListClients",
                    Commands = new List<SpeechItem>()
                    {
                        new SpeechItem(
                                new CommandList("List", "Read", "Show"),
                                new OptionalCommandList("all"),
                                new OptionalCommandList("available"),
                                new CommandList(Clients),
                                new OptionalCommandList("available", "connected"),
                                new OptionalCommandList("on the server")
                                ),
                        new SpeechItem(
                                new CommandList("What"),
                                new CommandList(Clients),
                                new CommandList("are available", "are connected"),
                                new OptionalCommandList("on the server")
                               )
                    }
                });
                Commands.Add(new VoiceCommand()
                {
                    Name = "ChangeClient",
                    Commands = new List<SpeechItem>()
                    {
                        new SpeechItem(
                            new CommandList("Switch", "Change", "Connect"),
                            new OptionalCommandList("to"),
                            new OptionalCommandList("the"),
                            new CommandList(Clients),
                            new SelectCommandList("Client", true, SelectedServer.Clients, "Client")
                        )
                    }
                });
                Commands.Add(new VoiceCommand()
                {
                    Name = "Restart",
                    Commands = new List<SpeechItem>()
                    {
                        new SpeechItem(
                            new CommandList("Restart", "Start"),
                            new OptionalCommandList("the"),
                            new OptionalCommandList("current"),
                            new CommandList(All),
                            new OptionalCommandList("from the begining")
                        ),
                        new SpeechItem(
                            new CommandList("Go to", "Restart", "Start"),
                            new OptionalCommandList("from"),
                            new OptionalCommandList("the"),
                            new CommandList("begining", "start"), 
                            new OptionalCommandList("of", "of the"),
                            new OptionalCommandList("current"),
                            new OptionalCommandList(All)
                        )
                    }
                });
                Commands.Add(new VoiceCommand()
                {
                    Name = "ProgramInfo",
                    Commands = new List<SpeechItem>()
                    {
                        new SpeechItem(
                            new CommandList("Tell me"),
                            new OptionalCommandList("more"),
                            new OptionalCommandList("information", "details"),
                            new CommandList("about what i'm watching")
                        ),
                        new SpeechItem(
                            new CommandList("Tell me"),
                            new OptionalCommandList("more"),
                            new OptionalCommandList("information", "details"),
                            new CommandList("about", "what happens in"),
                            new OptionalCommandList("this", "the current"),
                            new CommandList(All)
                        ),
                        new SpeechItem(
                            new CommandList("What happens", "What is", "Tell me what"),
                            new OptionalCommandList("in"),
                            new OptionalCommandList("this"),
                            new CommandList(All),
                            new OptionalCommandList("is"),
                            new OptionalCommandList("about")
                        )
                    }
                });
                Commands.Add(new VoiceCommand()
                {
                    Name = "ListTVEpisodes",
                    Commands = new List<SpeechItem>()
                    {
                        new SpeechItem(
                                new CommandList("List", "Read"),
                                new OptionalCommandList("all"),
                                new CommandList("episodes")
                            ),
                        new SpeechItem(
                                new CommandList("List", "Read"),
                                new CommandList("all episodes", "episodes"),
                                new OptionalCommandList("for"),
                                new OptionalCommandList("the"),
                                new OptionalCommandList(TV),
                                new SelectCommandList("TVShow",true,SelectedServer.TVShows,"Name"),
                                new CommandList("season"),
                                new SelectCommandList("Season", false, Common.NumberList(1, 20))
                        ),
                        new SpeechItem(
                                new CommandList("List", "Read"),
                                new CommandList("all episodes", "episodes"),
                                new OptionalCommandList("for"),
                                new CommandList("season"),
                                new SelectCommandList("Season", false, Common.NumberList(1, 20))
                         )
    ,               }
                });
                Commands.Add(new VoiceCommand()
                {
                    Name = "PlayTVEpisode",
                    Commands = new List<SpeechItem>()
                    {
                        new SpeechItem(
                                new CommandList(PlayCommands),
                                new OptionalCommandList("viewing", "watching"),
                                new CommandList("episode"),
                                new SelectCommandList("Episode", false, Common.NumberList(1, 100))
                            ),
                        new SpeechItem(
                                new CommandList(PlayCommands),
                                new OptionalCommandList("viewing", "watching"),
                                new CommandList("episode"),
                                new SelectCommandList("Episode", false, Common.NumberList(1, 100)),
                                new OptionalCommandList("for", "of"),
                                new CommandList("season"),
                                new SelectCommandList("Season", false, Common.NumberList(1, 20))
                            ),
                        new SpeechItem(
                                new CommandList(PlayCommands),
                                new OptionalCommandList("viewing", "watching"),
                                new CommandList("season"),
                                new SelectCommandList("Season", false, Common.NumberList(1, 20)),
                                new CommandList("episode"),
                                new SelectCommandList("Episode", false, Common.NumberList(1, 100))
                            ),
                         new SpeechItem(
                                new CommandList(PlayCommands),
                                new OptionalCommandList("viewing", "watching"),
                                new CommandList("episode"),
                                new SelectCommandList("Episode", false, Common.NumberList(1, 100)),
                                new OptionalCommandList("for", "of"),
                                new CommandList("season"),
                                new SelectCommandList("Season", false, Common.NumberList(1, 20)),
                                new OptionalCommandList("for", "of"),
                                new OptionalCommandList("the"),
                                new OptionalCommandList(TV),
                                new SelectCommandList("TVShow",true,SelectedServer.TVShows,"Name")
                            ),
                        new SpeechItem(
                                new CommandList(PlayCommands),
                                new OptionalCommandList("viewing", "watching"),
                                new OptionalCommandList("the"),
                                new OptionalCommandList(TV),
                                new SelectCommandList("TVShow",true,SelectedServer.TVShows,"Name"),
                                new CommandList("season"),
                                new SelectCommandList("Season", false, Common.NumberList(1, 20)),
                                new CommandList("episode"),
                                new SelectCommandList("Episode", false, Common.NumberList(1, 100))
                            ),
                         new SpeechItem(
                                new CommandList(PlayCommands),
                                new OptionalCommandList("viewing", "watching"),
                                new CommandList("season"),
                                new SelectCommandList("Season", false, Common.NumberList(1, 20)),
                                new CommandList("episode"),
                                new SelectCommandList("Episode", false, Common.NumberList(1, 100)),
                                new OptionalCommandList("for", "of"),
                                new OptionalCommandList("the"),
                                new OptionalCommandList(TV),
                                new SelectCommandList("TVShow",true,SelectedServer.TVShows,"Name")
                            )
                    }
                });
                Commands.Add(new VoiceCommand()
                {
                    Name = "ContinueTVShow",
                    Commands = new List<SpeechItem>()
                    {
                        new SpeechItem(
                            new CommandList(PlayCommands),
                            new OptionalCommandList("viewing", "watching"),
                            new OptionalCommandList("the"),
                            new CommandList(TV),
                            new SelectCommandList("TVShow",true,SelectedServer.TVShows,"Name")
                        )
                    }
                });
                
            }
            Listener.CreateGrammarList("Emby", Commands);
        }
        /// <summary>
        /// Talks some additional info on the given program
        /// </summary>
        /// <param name="Program"></param>
        /// <param name="Level"></param>
        private void ListAdditionalInfo(EmMediaItem Program, byte Level)
        {
            EmMediaItem CurrentProgram = Program.CurrentProgram ?? Program;
            if (Program.CurrentProgram != null && !string.IsNullOrEmpty(Program.CurrentProgram.Name))
                Talker.Queue(string.Format(Program.CurrentProgram.Name));
            if(CurrentProgram.StartDate > DateTime.Now.AddDays(-2))
            {
                Talker.Queue(string.Format("Started at {0} {1}", CurrentProgram.StartDate.Hour > 12 ? CurrentProgram.StartDate.Hour - 12 : CurrentProgram.StartDate.Hour, CurrentProgram.StartDate.Minute == 0 ? "o'clock" : CurrentProgram.StartDate.Minute.ToString()));
            }
            if (CurrentProgram.EndDate > DateTime.Now)
            {
                TimeSpan EndingDiff = CurrentProgram.EndDate - DateTime.Now;
                if (EndingDiff.Minutes > 0 && EndingDiff.Hours < 4)
                    Talker.Queue(string.Format("Ending in {0} minute{1}", EndingDiff.Minutes, EndingDiff.Minutes == 1 ? "" : "s"));
            }
            if (Level == 2 && !string.IsNullOrEmpty(CurrentProgram.Overview))
                Talker.Queue(CurrentProgram.Overview);
        }
        /// <summary>
        /// Start up
        /// </summary>
        public void Start ()
        {
            RefreshServerList();
        }
        /// <summary>
        /// Refreshes the list of servers and then connects to the last server used or the most available.
        /// </summary>
        private void RefreshServerList(bool ConnectOnReload = true)
        {
            EmbyServer UseServer = null;
            // First up connect to emby, get a list of servers.
            Logger.Log("Emby", "Retrieving list of avilable servers");
            GetServerList();
            // Check available servers.
            if (ConnectedServer.Servers == null || ConnectedServer.Servers.Count == 0)
            {
                Logger.Log("Emby", "No available Emby servers, try setting a service using EnableEmbyConnect or EnableEmbyBasic");
                Talker.Speak("No media servers currently available, check your setup then say Refresh Servers", true);
            }
            else
            {
                Logger.Log("Emby", string.Format("Connected to the emby service {0} server{1} available", ConnectedServer.Servers.Count, ConnectedServer.Servers.Count == 1 ? "" : "s"));
                Talker.Speak("Connected to the emby service", true);
                Talker.Queue(string.Format("{0} media server{1} available", ConnectedServer.Servers.Count, ConnectedServer.Servers.Count == 1 ? "" : "s"));
                foreach (EmbyServer Server in ConnectedServer.Servers)
                    Logger.Log("Emby", string.Format("{1} server {0}", Server.Conn.Name, Server.Conn.IsLocal ? "Local" : "Remote"));
            }
            if (ConnectOnReload)
            {
                // Connect to the most appropiate server.
                if (!string.IsNullOrEmpty(Options.Instance.ConnectedId))
                    foreach(EmbyServer Server in ConnectedServer.Servers)
                        if(Server.Conn.Id == Options.Instance.ConnectedId)
                        {
                            UseServer = Server;
                            break;
                        }
                // get a default server, as long as the force Previous server isn't initialised
                if (UseServer == null && !(Options.Instance.ForcePrevServer && string.IsNullOrEmpty(Options.Instance.ConnectedId)))
                    foreach (EmbyServer Server in ConnectedServer.Servers)
                        if (Server.Conn.IsLocal || (UseServer == null && !Server.ConnectionAttempted))
                            UseServer = Server;

                // We should have a server, now attempt a connection.
                if (UseServer == null)
                    Talker.Queue("No default server set, try saying List Servers then Connect to server");
                else
                    ConnectToServer(UseServer, true);
            }
        }
        /// <summary>
        /// Connects to a given emby server then pulls a server list
        /// </summary>
        /// <param name="Server"></param>
        /// <returns></returns>
        public RestResult ConnectToServer(EmbyServer Server, bool Speak = false)
        {
            Store.Clear();
            if (Server == null)
            {
                Logger.Log("Ignoring connection attempt to invalid server");
                return new RestResult() { Success = false };
            }
            Logger.Log("Emby", string.Format("Attempting connection to the server {0}", Server.ServerName));
            if(Speak)
                Talker.Queue(string.Format("Connecting to the server {0}", Server.ServerName));

            RestResult Result = Server.Connect();
            if (Result.Success)
            {
                Logger.Log("Emby", string.Format("Connected to the server {0} ", Server.Conn.Name));
                if(Speak)
                    Talker.Queue(string.Format("Connected to the server {0} ", Server.Conn.Name));
                // Save the current connection as the last connected server
                SelectedServer = Server;
                Options.Instance.ConnectedId = Server.Conn.Id;
                Options.Instance.SaveOptions();
                // Get the catlog information for the server, do this from the refresh command
                if (Speak)
                    Talker.Queue("Refreshing media");
                Server.RefreshCatalog();
                if (Speak)
                {
                    TalkCatalog();
                }
            }
            else
            {
                Logger.Log("Emby", string.Format("Unable to connect to the server {0}", Server.Conn.Name));
                Logger.Log("Emby", Result.Error);
                if(Speak)
                    Talker.Queue(string.Format("Unable to connect to the server {0}, try connecting to another server", Server.Conn.Name));
            }
            // Seeing as we have no information we need to clear down the command set
            SetCommands();

            return Result;
        }
        /// <summary>
        /// States what's available on the selected server
        /// </summary>
        private void TalkCatalog(bool StopTalking = false)
        {
            if (SelectedServer == null)
                return;
            if (StopTalking)
                Talker.Stop();
            int Count = (SelectedServer.Movies.Count > 0 ? 1 : 0) + (SelectedServer.TVShows.Count > 0 ? 1 : 0) + (SelectedServer.TVChannels.Count > 0 ? 1 : 0);
            int UsedCount = 0;
            if (Count == 0)
            {
                Talker.Queue("You currently have no media available");
                return;
            }
            string Sentence = "There are ";
            if(SelectedServer.Movies.Count > 0)
            {
                Sentence += UsedCount == 0 ? "" : (UsedCount == Count - 1 ? " and " : ", ");
                Sentence += string.Format("{0} movie{1}", SelectedServer.Movies.Count, SelectedServer.Movies.Count == 1 ? "" : "s");
                UsedCount++;
            }
            if (SelectedServer.TVShows.Count > 0)
            {
                Sentence += UsedCount == 0 ? "" : (UsedCount == Count - 1 ? " and " : ", ");
                Sentence += string.Format("{0} tv show{1}", SelectedServer.TVShows.Count, SelectedServer.TVShows.Count == 1 ? "" : "s");
                UsedCount++;
            }
            if (SelectedServer.TVChannels.Count > 0)
            {
                Sentence += UsedCount == 0 ? "" : (UsedCount == Count - 1 ? " and " : ", ");
                Sentence += string.Format("{0} tv channel{1}", SelectedServer.TVChannels.Count, SelectedServer.TVChannels.Count == 1 ? "" : "s");
                UsedCount++;
            }

            Sentence += " available";

            Talker.Queue(Sentence);
        }
        /// <summary>
        /// Gets a list of servers from the emby class.
        /// </summary>
        /// <returns></returns>
        public RestResult<List<EmbyServer>> GetServerList()
        {
            return ConnectedServer.GetServerList();
        }

        /// <summary>
        /// Clean up
        /// </summary>
        public void Dispose()
        {
            if(ConnectedServer != null)
                ConnectedServer.Dispose();
            ConnectedServer = null;
            Talker = null;
            if(Listener != null)
                Listener.SpeechRecognised -= Listener_SpeechRecognised;
            Listener = null;
            Store = null;
        }
    }
}
