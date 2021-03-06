﻿using EmbyVision.Base;
using EmbyVision.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EmbyVision.Rest.RestClient;

namespace EmbyVision.Emby.Classes
{
    public class EmbyServer : IDisposable
    {
        public List<EmSeries> TVShows { get; private set; }
        public List<EmMediaItem> TVChannels { get; private set; }
        public List<EmMediaItem> Movies { get; private set; }
        public EmClient SelectedClient { get; private set; }
        public List<EmClient> Clients { get; private set; }
        private List<EmUser> Users { get; set; }
        public EmUser SelectedUser { get; private set; }
        public EmConnection Conn { get; set; }
        private EmAuthResult LoginDetails { get; set; }
        public bool ConnectionAttempted { get; set; }
        public string ServerName
        {
            get
            {
                return Conn == null ? "" : Conn.Name;
            }
        }

        public EmbyServer()
        {
            ConnectionAttempted = false;
            Movies = new List<EmMediaItem>();
            TVChannels = new List<EmMediaItem>();
            TVShows = new List<EmSeries>();
        }
        /// <summary>
        /// Connects to the given server, then extracts a list of items on that server.
        /// </summary>
        /// <returns></returns>
        public async Task<RestResult> Connect()
        {
            ConnectionAttempted = true;

            // Clean up anything we currently have
            Clean();

            // Get user list from the server
            Logger.Log("Emby Server", string.Format("Attempting connection to the server {0} ({1})", Conn.Name, Conn.IsLocal ? Conn.LocalAddress : Conn.Url));
            using (RestClient Client = new RestClient(Conn.IsLocal ? Conn.LocalAddress : Conn.Url))
            {
                RestResult<List<EmUser>> Data = await Client.ExecuteAsync<List<EmUser>>("users/public", PostType.GET);
                if (!Data.Success || Data.Response == null || Data.Response.Count == 0)
                    return new RestResult() { Success = false, Error = string.Format("There was an error retrieving the server user list, {0}", Data.Success ?  "no users available" : Data.Error) };
                Users = Data.Response;
                Logger.Log("Emby Server", string.Format("User list retrieved from server {0} available, selecting user", Users.Count));
                // Make sure we have a user to log in as, if not find one.
                foreach (EmUser User in Users)
                {
                    if (User.Name.ToLower() == (Options.Instance.BasicUsername ?? "").ToLower() || User.ConnectUserName.ToLower() == (Options.Instance.ConnectUsername ?? "").ToLower())
                    {
                        SelectedUser = User;
                        break;
                    }
                    // Find a default
                    if (!User.HasPassword && (User.EnableAutoLogin || User.Policy.IsAdministrator))
                        SelectedUser = User;
                }

                if (SelectedUser == null)
                    return new RestResult() { Success = false, Error = "No default user could be selected on the system" };
                Logger.Log("Emby Server", string.Format("Selected user {0}, attempting Login",  SelectedUser.UsableUsername));
                Client.ClearParams();
                Client.AddQueryParameter("Username", SelectedUser.UsableUsername, RestClient.ParameterType.Form);
                Client.AddQueryParameter("Password", Common.HashCodeSha1(SelectedUser.UsablePassword), RestClient.ParameterType.Form);
                Client.AddQueryParameter("PasswordMd5", Common.HashCodeMd5(SelectedUser.UsablePassword), RestClient.ParameterType.Form);
                Client.AddQueryParameter("Authorization", SelectedUser.Authorisation, RestClient.ParameterType.Header);
                RestResult<EmAuthResult> AuthResult = await Client.ExecuteAsync<EmAuthResult>("/Users/AuthenticateByName", RestClient.PostType.POST);
                if (!AuthResult.Success)
                    return new RestResult() { Success = false, Error = string.Format("Unable to authenticate with the media server, {0}", AuthResult.Error) };

                Logger.Log("Emby Server", string.Format("Authentication successful for {0}", SelectedUser.UsableUsername));
                LoginDetails = AuthResult.Response;

                // If this is a Emby Connect connection then we need to sort out our access token.
                if(SelectedUser.LinkedUser)
                {
                    Logger.Log("Emby Server", "Exchanging access token");
                    Client.ClearParams();
                    Client.AddQueryParameter("X-MediaBrowser-Token", Conn.AccessKey, ParameterType.Header);
                    Client.AddQueryParameter("X-Emby-Authorization", SelectedUser.Authorisation, ParameterType.Header);
                    RestResult<EmExchToken> ExchangeResult = await Client.ExecuteAsync<EmExchToken>(string.Format("/Connect/Exchange?format=json&ConnectUserId={0}", SelectedUser.ConnectUserId), RestClient.PostType.GET);
                    if(!ExchangeResult.Success)
                        return new RestResult() { Success = false, Error = string.Format("Unable to retrieve exchange token, {0}", ExchangeResult.Error) };
                    SelectedUser.ExchangeToken = ExchangeResult.Response;
                    Logger.Log("Emby Server", "Exchange token received");
                }
                else
                {
                    // Access token for local connections needs to be copied
                    Conn.AccessKey = AuthResult.Response.AccessToken;
                }

                // Now connect to a client, ATM this is purely down to which is the best client there is no checking against locaility or storing of a client preference, this probably needs work.
                Logger.Log("Emby Server", "Retrieving client list");
                Client.ClearParams();
                Client.AddQueryParameter("X-MediaBrowser-Token", Conn.AccessKey, ParameterType.Header);
                Client.AddQueryParameter("ControllableByUserId", SelectedUser.UsableId, RestClient.ParameterType.Query);
                RestResult<List<EmClient>> ClientResult = await Client.ExecuteAsync<List<EmClient>>("Sessions", RestClient.PostType.GET);
                if (!ClientResult.Success || ClientResult.Response == null || ClientResult.Response.Count == 0)
                    return new RestResult() { Success = false, Error = string.Format("There was an error retrieving the a list of available client, {0}", ClientResult.Success ? "no clients available" : ClientResult.Error) };
                Logger.Log("Emby Server", string.Format("{0} connected clients available", ClientResult.Response.Count));
                Clients = ClientResult.Response;

                // Select a default client
                foreach (EmClient EmClient in Clients)
                {
                    // Add the server to these classes so they can perform ops
                    EmClient.Server = this;
                    // Check for an appropriate client to use (if we're not forceing client locks)
                    if(SelectedClient == null && !(Options.Instance.ForcePrevClient && !string.IsNullOrEmpty(Options.Instance.ConnectedClientId)))
                        if (EmClient.SupportsRemoteControl && (SelectedClient == null || EmClient.SupportedCommands.Count > SelectedClient.SupportedCommands.Count || (EmClient.PlayableMediaTypes.Contains("Video") && !SelectedClient.PlayableMediaTypes.Contains("Video"))))
                            SelectedClient = EmClient;
                    // Attach to previous client?
                    if (EmClient.DeviceId == Options.Instance.ConnectedClientId)
                        SelectedClient = EmClient;
                }
                // Save it.
                Options.Instance.ConnectedClientId = SelectedClient == null ? (Options.Instance.ForcePrevClient ? Options.Instance.ConnectedClientId : null) : SelectedClient.DeviceId;
                Options.Instance.SaveOptions();

                // Exit if we can't find a usable entry.
                if (SelectedClient == null)
                    return new RestResult() { Success = false, Error = "Unable to find a viable client running on the machine" };
                else
                    Logger.Log("Emby Server", string.Format("Selected client {0}", SelectedClient.Client));
            }
            // We are ready, now we can run a refresh on the media available on this server.
            return new RestResult() { Success = true };
        }
        /// <summary>
        /// Refresh the clients currently connected to the given server.
        /// </summary>
        /// <returns></returns>
        public RestResult<List<EmClient>> RefreshClients()
        {
            using (RestClient Client = Conn.GetClient(SelectedUser))
            {
                Logger.Log("Emby Server", "Retrieving client list");
                Client.AddQueryParameter("ControllableByUserId", SelectedUser.UsableId, RestClient.ParameterType.Query);
                RestResult<List<EmClient>> ClientResult = Client.Execute<List<EmClient>>("Sessions", RestClient.PostType.GET);
                if (!ClientResult.Success || ClientResult.Response == null || ClientResult.Response.Count == 0)
                    return new RestResult<List<EmClient>>() { Success = false, Error = string.Format("There was an error retrieving the a list of available client, {0}", ClientResult.Success ? "no clients available" : ClientResult.Error) };
                foreach (EmClient EmClient in ClientResult.Response)
                    EmClient.Server = this;
                Logger.Log("Emby Server", string.Format("{0} connected clients available", ClientResult.Response.Count));
                Clients = ClientResult.Response;
                return ClientResult;
            }
        }
        /// <summary>
        /// Sets the current Client to the 
        /// </summary>
        /// <param name="Client"></param>
        public bool SetClient(EmClient NewClient)
        {
            if (Clients != null)
                foreach (EmClient Client in Clients)
                    if (Client.Id == NewClient.Id)
                    {
                        SelectedClient = NewClient;
                        Options.Instance.ConnectedClientId = SelectedClient.DeviceId;
                        Options.Instance.SaveOptions();
                        return true;
                    }
            return false;
        }
        /// <summary>
        /// If we're connected we can go and collect the media info from the server and store it
        /// </summary>
        /// <returns></returns>
        public async Task<RestResult> RefreshCatalog()
        {
            Reset();
            Logger.Log("Emby Server", "Retrieving catalog listings");
            using (RestClient Client = Conn.GetClient(SelectedUser))
            {
                // Retieve movies
                RestResult<EmCatalogList> MovieResult = await Client.ExecuteAsync<EmCatalogList>(string.Format("Users/{0}/Items?Recursive=true&IncludeItemTypes=Movie", SelectedUser.Id), RestClient.PostType.GET);
                if (MovieResult.Success)
                {
                    MovieResult.Response.Items.Sort();
                    Movies = MovieResult.Response.Items;
                    Logger.Log("Emby Server", string.Format("{0} movies retrieved", Movies.Count));
                }
                else
                {
                    Logger.Log("Emby Server", "Unable to retrieve movie catalog");
                    Logger.Log("Emby Server", MovieResult.Error);
                }

                // now get the infromation for the tv series.
                RestResult<EmCatalogList> TVSeries = await Client.ExecuteAsync<EmCatalogList>(string.Format("Users/{0}/Items?Recursive=true&IncludeItemTypes=Episode", SelectedUser.Id), RestClient.PostType.GET);
                if (TVSeries.Success)
                {
                    TVShows = CreateSeries(TVSeries.Response.Items);
                    Logger.Log("Emby Server", string.Format("{0} TV Episodes in {1} series retrieved", TVSeries.Response.TotalRecordCount, TVShows.Count));
                }
                else
                {
                    Logger.Log("Emby Server", "Unable to retrieve TV show catalog");
                    Logger.Log("Emby Server", TVSeries.Error);
                }

                // Retieve TV Channels, first check if it's enabled
                RestResult<EmTVInfo> TVSettings = await Client.ExecuteAsync<EmTVInfo>("LiveTv/Info", RestClient.PostType.GET);
                if (TVSettings.Success && TVSettings.Response.IsEnabled)
                {
                    RestClient.RestResult<EmCatalogList> Channels = await Client.ExecuteAsync<EmCatalogList>("LiveTv/Channels", RestClient.PostType.GET);
                    if (Channels.Success && Channels.Response.TotalRecordCount > 0)
                    {
                        TVChannels = Channels.Response.Items;
                        TVChannels.Sort();
                        Logger.Log("Emby Server", string.Format("{0} TV Channels Retrieved", TVChannels.Count));
                    }
                }
                else
                {
                    Logger.Log("Emby Server", "Unable to retrieve TV channel catalog");
                    Logger.Log("Emby Server", TVSettings.Error);
                }
                // Fix anomolies
                TVChannels = TVChannels ?? new List<EmMediaItem>();
                TVShows = TVShows ?? new List<EmSeries>();
                Movies = Movies ?? new List<EmMediaItem>();
                return new RestResult() { Success = true };
            }
        }
        /// <summary>
        /// Creates a series / season / episode structure from the tv show list items
        /// </summary>
        /// <param name="Items"></param>
        /// <returns></returns>
        private List<EmSeries> CreateSeries(List<EmMediaItem> Items)
        {
            List<EmSeries> SeriesList = new List<EmSeries>();
            foreach (EmMediaItem Item in Items)
            {
                EmSeason FoundSeason = null;
                EmSeries FoundSeries = null;
                foreach (EmSeries Series in SeriesList)
                    if (Series.Id == Item.SeriesId)
                    {
                        FoundSeries = Series;
                        break;
                    }
                if (FoundSeries == null)
                {
                    FoundSeries = new EmSeries() { Id = Item.SeriesId, Name = Item.SeriesName, Seasons = new List<EmSeason>() };
                    SeriesList.Add(FoundSeries);
                }
                // Find the season in the season list.
                foreach (EmSeason Season in FoundSeries.Seasons)
                    if (Season.Id == Item.SeasonId)
                    {
                        FoundSeason = Season;
                        break;
                    }
                if (FoundSeason == null)
                {
                    FoundSeason = new EmSeason() { Id = Item.SeasonId, Episodes = new List<EmMediaItem>(), Index = Item.ParentIndexNumber };
                    FoundSeries.Seasons.Add(FoundSeason);
                }
                FoundSeason.Episodes.Add(Item);
            }
            foreach (EmSeries Series in SeriesList)
            {
                foreach (EmSeason Season in Series.Seasons)
                    Season.Episodes.Sort();
                Series.Seasons.Sort();
            }
            SeriesList.Sort();
            return SeriesList;
        }
        /// <summary>
        /// Plays the specified movie file.
        /// </summary>
        /// <param name="Item"></param>
        /// <returns></returns>
        public async Task<RestResult> PlayFile(EmMediaItem Item, bool Resume)
        {
            await Item.Refresh(this);
            // Get information regardin this item, we can then figure if we need to resume or not
            /* using (RestClient Client = GetClient())
             {
                 Client.AddQueryParameter("ControllableByUserId", SelectedUser.Id, RestClient.ParameterType.Query);
                 Results = Client.Execute<List<EmClient>>("Sessions", RestClient.PostType.GET);
                 if (!Results.Success || Results.Response == null || Results.Response.Count == 0)
                     return Results;
                 Logger.Log(string.Format("{0} connected clients available", Results.Response.Count));
             }*/
             // there's not header item here.
            Logger.Log(string.Format("Sending play command for media item {0} ({1})", Item.Id, Item.Name));
            using (RestClient Client = Conn.GetClient(SelectedUser))
            {
                Client.AddQueryParameter("ItemIds", Item.Id, RestClient.ParameterType.Query);
                Client.AddQueryParameter("StartPositionTicks", Resume && Item.UserData != null && Item.UserData.PlaybackPositionTicks > 0 && Item.UserData.PlaybackPositionTicks < Item.RunTimeTicks ? Item.UserData.PlaybackPositionTicks.ToString() : "0", RestClient.ParameterType.Query);
                Client.AddQueryParameter("PlayCommand", "PlayNow", RestClient.ParameterType.Query);
                return await Client.ExecuteAsync(string.Format("Sessions/{0}/Playing", SelectedClient.Id), RestClient.PostType.POST);
            }
        }
        /// <summary>
        /// Clears out media content
        /// </summary>
        public void Reset()
        {
            if (TVShows != null)
                TVShows.Clear();
            TVShows = null;
            if (Movies != null)
                Movies.Clear();
            Movies = null;
            if (TVChannels != null)
                TVChannels.Clear();
            TVChannels = null;
        }
        /// <summary>
        /// Cleans up the currently set properties
        /// </summary>
        private void Clean()
        {
            Reset();
            SelectedClient = null;
            if (Clients != null)
                Clients.Clear();
            Clients = null;
            if (Users != null)
                Users.Clear();
            Users = null;
            LoginDetails = null;
            SelectedUser = null;
        }
        /// <summary>
        /// Clean up and exit
        /// </summary>
        public void Dispose()
        {
            Clean();
            Conn = null;
        }
    }
}
