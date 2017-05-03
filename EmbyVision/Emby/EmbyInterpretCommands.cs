using EmbyVision.Base;
using EmbyVision.Emby.Classes;
using EmbyVision.Speech;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EmbyVision.Rest.RestClient;

namespace EmbyVision.Emby
{
    /// <summary>
    /// Handles the speech commands from the emby connector, purely put here to keep the code
    /// cleaner in that class.
    /// </summary>
    public class EmbyInterpretCommands: IDisposable
    {
        private EmbyCore Store { get; set; }
        /// <summary>
        /// Start up, hook the listener.
        /// </summary>
        /// <param name="Talker"></param>
        /// <param name="Listener"></param>
        public EmbyInterpretCommands(EmbyCore Store)
        {
            this.Store = Store;
            Store.Listener.SpeechRecognised += Listener_SpeechRecognised;
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
                        Store.Talker.Speak(string.Format("You currently have {0} server{1} available", Store.Servers.Count, Store.Servers.Count == 1 ? "" : "s"));
                        break;
                    }
                    else
                    {
                        if (Store.SelectedServer == null)
                        {
                            Store.Talker.Speak("You are currently not connected to a server, try saying List Servers and then connect to a server");
                            break;
                        }
                        switch (SelectList["Type"])
                        {
                            case "Movie":
                                Store.Talker.Speak(string.Format("You currently have {0} Movie{1} available", Store.SelectedServer.Movies.Count, Store.SelectedServer.Movies.Count == 1 ? "" : "s"));
                                break;
                            case "TV":
                                Store.Talker.Speak(string.Format("You currently have {0} TV Series{1} available", Store.SelectedServer.TVShows.Count, Store.SelectedServer.TVShows.Count == 1 ? "" : "s"));
                                break;
                            case "Channel":
                                Store.Talker.Speak(string.Format("You currently have {0} Channel{1} available", Store.SelectedServer.TVChannels.Count, Store.SelectedServer.TVChannels.Count == 1 ? "" : "s"));
                                break;
                        }
                    }
                    break;
                case "ChangeClient":
                    EmClient SelectedClient = (EmClient)SelectList["Client"];
                    if (Store.SelectedServer.SetClient(SelectedClient))
                        Store.Talker.Speak(string.Format("Connected to the client {0}", SelectedClient.Client));
                    else
                        Store.Talker.Speak("Unable to find the select client, try saying List Clients to refresh");

                    break;
                case "ListClients":
                    // Refresh the server client list
                    RestResult<List<EmClient>> ClientResult = Store.SelectedServer.RefreshClients();
                    if (ClientResult.Success)
                    {
                        Store.Talker.Speak(Store.SelectedServer.Clients, "Client", "Client", true);
                        await Store.SetCommands.SetCommands(Store.SelectedServer);
                    }
                    else
                        Store.Talker.Speak("Unable to refresh the servers client list");
                    break;
                case "ListItems":
                    if (SelectList["Type"].ToString() == "Server")
                    {
                        Store.Talker.Speak(Store.Servers, "ServerName", "Server", true);
                        if (Store.SelectedServer != null)
                            Store.Talker.Queue(string.Format("You are currently connected to the server {0}", Store.SelectedServer.ServerName));
                        break;
                    }
                    else
                    {
                        if (Store.SelectedServer == null)
                        {
                            Store.Talker.Speak("You are currently not connected to a server, try saying List Servers and then connect to a server");
                            break;
                        }
                        switch (SelectList["Type"].ToString())
                        {
                            case "Movie":
                                Store.Talker.Speak(Store.SelectedServer.Movies, "Name", "Movie", true);
                                break;
                            case "Channel":
                                Store.Talker.Speak(Store.SelectedServer.TVChannels, "Name", "Channel", true);
                                break;
                            case "TV":
                                Store.Talker.Speak(Store.SelectedServer.TVShows, "Name", "TV Series", true);
                                break;
                        }
                    }
                    Store.ClearMedia();
                    break;
                case "ChangeServer":
                    EmbyServer NewServer = (EmbyServer)SelectList["Server"];
                    if (Store.SelectedServer != null && NewServer.Conn.Id == Store.SelectedServer.Conn.Id)
                    {
                        Store.Talker.Speak(string.Format("You are already connected to the server {0}", Store.SelectedServer.ServerName));
                        break;
                    }
                    Store.Talker.Stop();
                    EmbyServer CurrentServer = Store.SelectedServer;
                    Options.Instance.ConnectedClientId = null;
                    Options.Instance.SaveOptions();
                    if (!(await Store.ConnectionHelper.ConnectToServer(NewServer, true)).Success)
                    {
                        if (CurrentServer == null)
                            Store.Talker.Speak(string.Format("Unable to connect to the server {0}", NewServer.ServerName));
                        else
                        {
                            Store.Talker.Speak(string.Format("Unable to connect to the server {0}, will attempt reconnection to {1}", NewServer.ServerName, CurrentServer.ServerName));
                            await Store.ConnectionHelper.ConnectToServer(CurrentServer, true);
                        }
                    }
                    break;
                case "PlayItem":
                    // Play a movie or TV channel
                    EmMediaItem PlayItem = (EmMediaItem)SelectList["PlayItem"];
                    switch (PlayItem.Type)
                    {
                        case "Movie":
                            Store.Talker.Speak(string.Format("Playing the movie {0}", PlayItem.Name));
                            break;
                        case "TvChannel":
                            Store.Talker.Speak(string.Format("Switching to channel {0}", PlayItem.Name));
                            break;
                    }
                    RestResult PlayResult = await Store.SelectedServer.PlayFile(PlayItem, true);
                    // Additional information on what is left
                    Store.Talker.Queue(GetStartEnd(PlayItem));
                    Store.ClearMedia();
                    break;
                case "Pause":
                    // Pause or play an item if the client is currently playing.
                    await Store.SelectedServer.SelectedClient.Refresh();
                    if (Store.SelectedServer.SelectedClient.NowPlayingItem != null)
                        if (Store.SelectedServer.SelectedClient.PlayState.IsPaused)
                        {
                            // Send a resume command
                            RestResultBase PauseResult = await Store.SelectedServer.SelectedClient.Resume(false);
                            if (PauseResult.Success)
                                Store.Talker.Speak(string.Format("Resuming {0}", Store.SelectedServer.SelectedClient.NowPlayingItem.Name));
                        }
                        else
                        {
                            // Send a pause command
                            RestResultBase PauseResult = await Store.SelectedServer.SelectedClient.Pause(false);
                            if (PauseResult.Success)
                                Store.Talker.Speak(string.Format("Pausing {0}", Store.SelectedServer.SelectedClient.NowPlayingItem.Name));
                        }
                    break;
                case "Stop":
                    await Store.SelectedServer.SelectedClient.Refresh();
                    if (Store.SelectedServer.SelectedClient.NowPlayingItem != null)
                    {
                        string Name = Store.SelectedServer.SelectedClient.NowPlayingItem.Name;
                        RestResultBase StopResult = await Store.SelectedServer.SelectedClient.Stop(false);
                        if (StopResult.Success)
                            Store.Talker.Speak(string.Format("Stopped {0}", Name));
                    }
                    break;
                case "CheckAudioTrack":
                    if (Store.SelectedServer == null)
                    {
                        Store.Talker.Speak("You are currently not connected to a server, try saying List Servers and then connect to a server");
                        break;
                    }
                    await Store.SelectedServer.SelectedClient.Refresh();
                    if (Store.SelectedServer.SelectedClient.NowPlayingItem == null)
                    {
                        Store.Talker.Speak("There is no media currently playing");
                        return;
                    }
                    List<string> AudioTracks = new List<string>();
                    if (Store.SelectedServer.SelectedClient.NowPlayingItem.MediaStreams != null)
                        foreach (EmMediaStream Stream in Store.SelectedServer.SelectedClient.NowPlayingItem.MediaStreams)
                            if (Stream.Type == "Audio")
                                AudioTracks.Add(Stream.DisplayTitle);

                    Store.Talker.Speak(AudioTracks, "Audio Track", true);
                    break;
                case "SwitchAudioTrack":
                    if (Store.SelectedServer == null)
                    {
                        Store.Talker.Speak("You are currently not connected to a server, try saying List Servers and then connect to a server");
                        break;
                    }
                    await Store.SelectedServer.SelectedClient.Refresh();
                    if (Store.SelectedServer.SelectedClient.NowPlayingItem == null)
                    {
                        Store.Talker.Speak("There is no media currently playing");
                        return;
                    }
                    int Track = int.Parse(SelectList["Track"].ToString());
                    // Send the command to switch to the next audio track.
                    RestResultBase Result = await Store.SelectedServer.SelectedClient.SwitchAudioChannel(Track, false);
                    if (!Result.Success)
                        Store.Talker.Speak("Unable to switch audio channel", Result.Error);
                    else
                        Store.Talker.Speak(string.Format("Audio switched to track {0}", Track));
                    break;
                case "RefreshMedia":
                    if (Store.SelectedServer == null)
                    {
                        Store.Talker.Speak("You are currently not connected to a server, try saying List Servers and then connect to a server");
                        break;
                    }
                    await Store.SelectedServer.RefreshCatalog();
                    TalkCatalog(true);
                    // Say what we have.
                    break;
                case "ProgramInfo":
                    if (Store.SelectedServer == null)
                    {
                        Store.Talker.Speak("You are currently not connected to a server, try saying List Servers and then connect to a server");
                        break;
                    }
                    RestResult<EmClient> PIClient = await Store.SelectedServer.SelectedClient.Refresh();
                    if (!PIClient.Success)
                    {
                        Store.Talker.Speak(string.Format("Unable to retrieve client information : {0}", PIClient.Error));
                        break;
                    }
                    if (PIClient.Response.NowPlayingItem == null)
                    {
                        Store.Talker.Speak("You are currently not watching anything");
                        break;
                    }
                    EmMediaItem PIWatchingItem = Store.SelectedServer.SelectedClient.NowPlayingItem;
                    await PIWatchingItem.Refresh(Store.SelectedServer);
                    // Overview of current playing things.
                    Store.Talker.Stop();
                    Store.TalkHelper.ListAdditionalInfo(PIWatchingItem, 2);
                    break;
                case "WhatAmIWatching":
                    if (Store.SelectedServer == null)
                    {
                        Store.Talker.Speak("You are currently not connected to a server, try saying List Servers and then connect to a server");
                        break;
                    }
                    RestResult<EmClient> WIAClient = await Store.SelectedServer.SelectedClient.Refresh();
                    if (!WIAClient.Success)
                    {
                        Store.Talker.Speak(string.Format("Unable to retrieve client information : {0}", WIAClient.Error));
                        break;
                    }
                    if (WIAClient.Response.NowPlayingItem == null)
                    {
                        Store.Talker.Speak("You are currently not watching anything");
                        break;
                    }
                    EmMediaItem NWWatchingItem = Store.SelectedServer.SelectedClient.NowPlayingItem;
                    await NWWatchingItem.Refresh(Store.SelectedServer);
                    bool StateEnd = false;
                    switch (NWWatchingItem.Type)
                    {
                        case "TvChannel":
                            Store.Talker.Speak(string.Format("You are currently watching the TV Channel {0}", NWWatchingItem.Name));
                            StateEnd = true;
                            break;
                        case "Movie":
                            Store.Talker.Speak(string.Format("You are currently watching the Movie {0}", NWWatchingItem.Name));
                            StateEnd = true;
                            break;
                        case "Episode":
                            Store.Talker.Speak(string.Format("You are currently watching season {0} episode {1} of the TV Series {2}", NWWatchingItem.ParentIndexNumber, NWWatchingItem.IndexNumber, NWWatchingItem.SeriesName));
                            if (!string.IsNullOrEmpty(NWWatchingItem.Name) && NWWatchingItem.Name.ToLower().IndexOf("episode")>=0)
                                Store.Talker.Queue(NWWatchingItem.Name);
                            StateEnd = true;
                            break;
                        default:
                            Store.Talker.Speak(string.Format("You are currently watching {0}", NWWatchingItem.Name));
                            StateEnd = true;
                            break;
                    }
                    // Tell the user when the program is ending.
                    if (StateEnd)
                        Store.TalkHelper.ListAdditionalInfo(NWWatchingItem, 1);
                    break;
                case "RefreshServerList":
                    await Store.ConnectionHelper.RefreshServerList(false);
                    break;
                case "GotoTVShow":
                    Store.SelectedSeries = (EmSeries)SelectList["TVShow"];
                    Store.SelectedServer.SelectedClient.ShowContent(Store.SelectedSeries);
                    Store.Talker.Speak(Store.SelectedSeries.Name);
                    Store.Talker.Queue(string.Format("There {0} {1} season{2}", Store.SelectedSeries.Seasons.Count == 1 ? "is" : "are", Store.SelectedSeries.Seasons.Count, Store.SelectedSeries.Seasons.Count == 1 ? "" : "s"));
                    //Talker.Queue(SelectedSeries.Seasons, "Name", "Season", false);
                    Store.SelectedSeason = null;
                    break;
                case "ListTVSeasons":
                    if (SelectList != null && SelectList.ContainsKey("TVShow"))
                        Store.SelectedSeries = (EmSeries)SelectList["TVShow"];
                    if (Store.SelectedSeries == null)
                    {
                        Store.Talker.Speak("You have not selected any TV show, try saying List TV Shows");
                        break;
                    }
                    Store.Talker.Speak(Store.SelectedSeries.Seasons, "Name", "Season", false);
                    Store.SelectedSeason = null;
                    break;
                case "GoToSeason":
                    if (SelectList.ContainsKey("TVShow"))
                        Store.SelectedSeries = (EmSeries)SelectList["TVShow"];
                    if (Store.SelectedSeries == null)
                    {
                        Store.Talker.Speak("You have not selected any TV show, try saying List TV Shows");
                        break;
                    }
                    if (SelectList != null && SelectList.ContainsKey("Season"))
                    {
                        int EpSeason = int.Parse(SelectList["Season"].ToString());
                        int SeasonIndex = Store.SelectedSeries.SeasonIndex(EpSeason);
                        if (SeasonIndex < 0)
                        {
                            Store.Talker.Speak(string.Format("You do not have season {0} of {1}", EpSeason, Store.SelectedSeries.Name));
                            break;
                        }
                        Store.SelectedSeason = Store.SelectedSeries.Seasons[SeasonIndex];
                    }
                    Store.SelectedServer.SelectedClient.ShowContent(Store.SelectedSeason);
                    Store.Talker.Speak(Store.SelectedSeries.Name);
                    Store.Talker.Queue(Store.SelectedSeason.Name);
                    Store.Talker.Queue(string.Format("There {0} {1} episode{2}", Store.SelectedSeason.Episodes.Count == 1 ? "is" : "are", Store.SelectedSeason.Episodes.Count, Store.SelectedSeason.Episodes.Count == 1 ? "" : "s"));
                    break;
                case "ListTVEpisodes":
                    if (SelectList != null && SelectList.ContainsKey("TVShow"))
                        Store.SelectedSeries = (EmSeries)SelectList["TVShow"];
                    if (Store.SelectedSeries == null)
                    {
                        Store.Talker.Speak("You have not selected any TV Show, try saying List TV Shows");
                        break;
                    }
                    if (SelectList != null && SelectList.ContainsKey("Season"))
                    {
                        int EpSeason = int.Parse(SelectList["Season"].ToString());
                        int SeasonIndex = Store.SelectedSeries.SeasonIndex(EpSeason);
                        if (SeasonIndex < 0)
                        {
                            Store.Talker.Speak(string.Format("You do not have season {0} of {1}", EpSeason, Store.SelectedSeries.Name));
                            break;
                        }
                        Store.SelectedSeason = Store.SelectedSeries.Seasons[SeasonIndex];
                    }
                    if (Store.SelectedSeason == null)
                    {
                        Store.Talker.Speak("You have not selected any season, try saying List Seasons");
                        break;
                    }
                    Store.Talker.Speak(Store.SelectedSeason.Episodes, "EpisodeName", "Episode", false);
                    break;
                case "PlayTVEpisode":
                    if (SelectList != null && SelectList.ContainsKey("TVShow"))
                        Store.SelectedSeries = (EmSeries)SelectList["TVShow"];
                    if (Store.SelectedSeries == null)
                    {
                        Store.Talker.Speak("You have not selected any TV Show, try saying List TV Shows");
                        break;
                    }
                    if (SelectList != null && SelectList.ContainsKey("Season"))
                    {
                        int EpSeason = int.Parse(SelectList["Season"].ToString());
                        int SeasonIndex = Store.SelectedSeries.SeasonIndex(EpSeason);
                        if (SeasonIndex < 0)
                        {
                            Store.Talker.Speak(string.Format("You do not have season {0} of {1}", EpSeason, Store.SelectedSeries.Name));
                            break;
                        }
                        Store.SelectedSeason = Store.SelectedSeries.Seasons[SeasonIndex];
                    }
                    if (Store.SelectedSeason == null)
                    {
                        Store.Talker.Speak("You have not selected any season, try saying List Seasons");
                        break;
                    }
                    int EpEpisode = 0;
                    if (SelectList != null && SelectList.ContainsKey("Episode"))
                        EpEpisode = int.Parse(SelectList["Episode"].ToString());
                    if (EpEpisode == 0)
                    {
                        Store.Talker.Speak("You have not selected an episode, try saying List Episodes");
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
                        Store.Talker.Speak(string.Format("Episode {0} for {1} Season {2} does not exist", EpEpisode, Store.SelectedSeason.Index, Store.SelectedSeries.Name));
                        break;
                    }
                    await PlayEpisode();
                    break;
                case "ContinueTVShow":
                    if (SelectList != null && !SelectList.ContainsKey("TVShow"))
                        break;
                    // We have a series, we should go through it and see the last watchde episode then start from there, unfortunately this means updating all the shows so we can get the last watched data.
                    Store.SelectedSeries = (EmSeries)SelectList["TVShow"];
                    // Update these items.
                    await Common.UpdateMediaItems(Store.SelectedServer, Store.SelectedSeries);
                    // Now we need to get the last moved item, then play from there.
                    Store.SelectedEpisode = Store.SelectedSeries.Seasons[0].Episodes[0];
                    Store.SelectedSeason = Store.SelectedSeries.Seasons[0];
                    DateTime LastEpDate = DateTime.MinValue;
                    foreach (EmSeason Season in Store.SelectedSeries.Seasons)
                        foreach (EmMediaItem Episode in Season.Episodes)
                            if (Episode.UserData != null && Episode.UserData.LastPlayedDate != null && Episode.UserData.LastPlayedDate > LastEpDate)
                            {
                                LastEpDate = (DateTime)Episode.UserData.LastPlayedDate;
                                Store.SelectedEpisode = Episode;
                                Store.SelectedSeason = Season;
                                await PlayEpisode();
                            }     
                    break;
                case "Restart":
                    // Restarts the current media item
                    await Store.SelectedServer.SelectedClient.Refresh();
                    if (Store.SelectedServer.SelectedClient.NowPlayingItem == null)
                    {
                        Store.Talker.Speak("You are not currently watching any media");
                        break;
                    }
                    if (Store.SelectedServer.SelectedClient.NowPlayingItem.Type == "TvChannel")
                    {
                        Store.Talker.Speak("You cannot restart Live TV programs");
                        break;
                    }
                    // Send the command to restart the current media
                    RestResultBase RestartResult = await Store.SelectedServer.SelectedClient.SetPosition(0, false);
                    if (RestartResult.Success)
                        Store.Talker.Speak("Restart");
                    else
                        Store.Talker.Speak(string.Format("Unable to restart the current item {0}", RestartResult.Error));

                    break;
                case "NextPrevEpisode":
                    await Store.SelectedServer.SelectedClient.Refresh();
                    if (Store.SelectedServer.SelectedClient.NowPlayingItem == null || Store.SelectedServer.SelectedClient.NowPlayingItem.Type != "Episode")
                    {
                        Store.Talker.Speak("You are not currently watching any episode");
                        break;
                    }
                    EmMediaItem NewEpisode;
                    // Client refresh now automatically refreshes the nowplaying item
                    // await Store.SelectedServer.SelectedClient.NowPlayingItem.Refresh(Store.SelectedServer);
                    SetCurrentEpisode(Store.SelectedServer.SelectedClient.NowPlayingItem);

                    if (CommandIndex == 0)
                        NewEpisode = Store.SelectedSeries.FindPrev(Store.SelectedServer.SelectedClient.NowPlayingItem);
                    else
                        NewEpisode = Store.SelectedSeries.FindNext(Store.SelectedServer.SelectedClient.NowPlayingItem);
                    
                    // if we have an episode then play it.
                    if (NewEpisode == null)
                        Store.Talker.Speak(string.Format("You are already watching the {0} episode", CommandIndex == 0 ? "first" : "last"));
                    else
                    {
                        SetCurrentEpisode(NewEpisode);
                        await PlayEpisode();
                    }
                    break;
                case "MovePosition":
                case "SkipPosition":
                    int Direction = -1;
                    string SkipString = "";
                    TimeSpan SkipAmount = new TimeSpan(0);
                    if (SelectList == null)
                        break;
                    if (SelectList.ContainsKey("Direction"))
                        Direction = SelectList["Direction"].ToString() == "forward" ? 1 : -1;
                    for (int i = 1; i <= 3; i++)
                    {
                        if (SelectList.ContainsKey("TimeType" + i.ToString()) && SelectList.ContainsKey("Time" + i.ToString()))
                        {
                            long SkipValue = long.Parse(SelectList["Time" + i.ToString()].ToString());
                            switch (SelectList["TimeType" + i.ToString()].ToString())
                            {
                                case "Hours":
                                case "Hour":
                                    SkipString += string.Format("{0} hour{1}, ", SkipValue, SkipValue == 1 ? "" : "s");
                                    SkipAmount += TimeSpan.FromHours(SkipValue);
                                    break;
                                case "Minutes":
                                case "Minute":
                                    SkipString += string.Format("{0} minute{1}, ", SkipValue, SkipValue == 1 ? "" : "s");
                                    SkipAmount += TimeSpan.FromMinutes(SkipValue);
                                    break;
                                default:
                                    SkipString += string.Format("{0} second{1}, ", SkipValue, SkipValue == 1 ? "" : "s");
                                    SkipAmount += TimeSpan.FromSeconds(SkipValue);
                                    break;
                            }
                        }
                    }
                    // Skip Forward / backwards.
                    if (SkipAmount.Ticks > 0)
                    {
                        // SOrt the formating.
                        SkipString = SkipString.Substring(0, SkipString.Length - 2);
                        if(SkipString.IndexOf(",")>=0)
                            SkipString = SkipString.Substring(0, SkipString.LastIndexOf(",")) + " and" + SkipString.Substring(SkipString.LastIndexOf(",") + 1);
                        // Skip or move?

                        if (Context == "SkipPosition")
                        {
                            RestResultBase SkipResult = await Store.SelectedServer.SelectedClient.MovePosition(SkipAmount.Ticks * Direction, true);
                            if (SkipResult.Success)
                                Store.Talker.Speak(string.Format("Skipping {0} {1}", Direction == 1 ? "forward" : "backwards", SkipString));
                            else
                                Store.Talker.Speak(string.Format("Unable to restart the current item {0}", SkipResult.Error));
                        }
                        else
                        {
                            RestResultBase SkipResult = await Store.SelectedServer.SelectedClient.SetPosition(SkipAmount.Ticks, true);
                            if (SkipResult.Success)
                                Store.Talker.Speak(string.Format("Starting at {0}", SkipString));
                            else
                                Store.Talker.Speak(string.Format("Unable to restart the current item {0}", SkipResult.Error));
                        }
                    }
                    break;
            }
        }
        /// <summary>
        /// If we're given an episode we'll get the current series and season, in case
        /// the app was started mid episode and we don't already have it
        /// </summary>
        /// <param name="Episode"></param>
        private void SetCurrentEpisode(EmMediaItem Episode)
        {
            if (Episode == null || Episode.Type != "Episode")
                return;
            // Make sure we have the correct season selected
            foreach(EmSeries Series in Store.SelectedServer.TVShows)
                foreach (EmSeason Season in Series.Seasons)
                    foreach (EmMediaItem Ep in Season.Episodes)
                        if (Ep.Id == Episode.Id)
                        {
                            Store.SelectedSeries = Series;
                            Store.SelectedSeason = Season;
                            Store.SelectedEpisode = Ep;
                        }
        }
        /// <summary>
        /// Plays the episode.
        /// </summary>
        /// <param name="Episode"></param>
        public async Task PlayEpisode()
        {
            RestResult TVPlayResult = await Store.SelectedServer.PlayFile(Store.SelectedEpisode, true);
            if (!TVPlayResult.Success)
                Store.Talker.Queue(string.Format("Unable to play the specified item, {0}", TVPlayResult.Error));
            else
                Store.Talker.Speak(string.Format("Playing season {0} episode {1} of {2} {3}", Store.SelectedSeason.Index, Store.SelectedEpisode.IndexNumber, Store.SelectedSeries.Name, GetStartEnd(Store.SelectedEpisode)));
        }
        /// <summary>
        /// Gets the start / end position of a media item
        /// </summary>
        /// <param name="Item"></param>
        /// <returns></returns>
        private string GetStartEnd(EmMediaItem Item)
        {
            string EpPlayState = "";
            if (Item.UserData != null && Item.UserData.PlaybackPositionTicks > 0)
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
            if (Length.Hours > 0)
            {
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
        /// States what's available on the selected server
        /// </summary>
        public void TalkCatalog(bool StopTalking = false)
        {
            if (Store.SelectedServer == null)
                return;
            if (StopTalking)
                Store.Talker.Stop();

            Store.TalkHelper.TalkCatalog(Store.SelectedServer);
        }

        public void Dispose()
        {
            if(Store.Listener != null)
                Store.Listener.SpeechRecognised -= Listener_SpeechRecognised;
        }
    }
}
