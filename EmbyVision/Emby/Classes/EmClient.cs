using EmbyVision.Base;
using EmbyVision.Rest;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static EmbyVision.Rest.RestClient;

namespace EmbyVision.Emby.Classes
{
    public class EmClient
    {
        public EmbyServer Server { get; set; }
        [JsonProperty("SupportedCommands")]
        public List<string> SupportedCommands { get; set; }
        [JsonProperty("QueueableMediaTypes")]
        public List<string> QueueableMediaTypes { get; set; }
        [JsonProperty("PlayableMediaTypes")]
        public List<string> PlayableMediaTypes { get; set; }
        public EmMediaItem NowPlayingItem { get; set; }
        public string Id { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string ApplicationVersion { get; set; }
        public string Client { get; set; }
        public DateTime LastActivityDate { get; set; }
        public string DeviceName { get; set; }
        public string DeviceId { get; set; }
        public bool SupportsRemoteControl { get; set; }
        public EmPlayState PlayState { get; set; }

        /// <summary>
        /// Refresh the current client, mainly for the playing state, this could probably be simplified on the copying of the fields.
        /// </summary>
        /// <returns></returns>
        public async Task<RestResult<EmClient>> Refresh()
        {
            using (RestClient Client = Server.Conn.GetClient(Server.SelectedUser))
            {
                Logger.Log("Emby Client", "Refreshing client information");
                Client.AddQueryParameter("DeviceId", this.DeviceId, RestClient.ParameterType.Query);
                RestResult<List<EmClient>> Result = await Client.ExecuteAsync<List<EmClient>>("Sessions", PostType.GET);
                if (!Result.Success || Result.Response == null || Result.Response.Count == 0)
                {
                    Logger.Log("Emby Client", "Unable to retrieve client information");
                    Logger.Log("Emby Client", Result.Error);
                    return new RestClient.RestResult<EmClient>(Result);
                }
                EmClient EmClient = Result.Response[0];
                if (EmClient.Id == this.Id)
                {
                    EmClient.Server = Server;
                    Common.CopyObject(EmClient, this, new string[] {"Server"});
                    return new RestResult<EmClient>(Result) { Response = EmClient };
                }
                return new RestResult<EmClient>() { Success = false, Error = "Unable to find matching client to refresh" };
            }
        }
        /// <summary>
        /// Switches to another audio track on the currently playing item.
        /// </summary>
        /// <param name="TrackNumber"></param>
        /// <param name="RefreshClient"></param>
        /// <returns></returns>
        public async Task<RestClient.RestResult<EmMediaStream>> SwitchAudioChannel(int TrackNumber, bool RefreshClient = true)
        {
            // check the client supports these channel switching
            if (!Server.SelectedClient.SupportedCommands.Contains("SetAudioStreamIndex"))
                return new RestClient.RestResult<EmMediaStream>() { Error = "Current client does not support audio switching", Success = false };

            // refesh the now playing item
            if (RefreshClient)
            {
                RestClient.RestResult<EmClient> CurrentClient = await Refresh();
                if (!CurrentClient.Success || CurrentClient.Response == null)
                    return new RestClient.RestResult<EmMediaStream>(CurrentClient);
            }
            if (this.NowPlayingItem == null)
                return new RestClient.RestResult<EmMediaStream>() { Error = "No item currently playing", Success = false };
            // Check of the track number exists
            int TrackCount = 0;
            EmMediaStream FoundStream = null;
            foreach (EmMediaStream Stream in NowPlayingItem.MediaStreams)
                if (Stream.Type == "Audio")
                {
                    TrackCount++;
                    if (TrackNumber == TrackCount)
                    {
                        FoundStream = Stream;
                        break;
                    }

                }
            if (FoundStream == null)
                return new RestClient.RestResult<EmMediaStream>() { Error = string.Format("Track number {0} does not exist", TrackNumber), Success = false };
            using (RestClient Client = Server.Conn.GetClient(Server.SelectedUser))
            {
                Client.AddQueryParameter("Index", FoundStream.Index.ToString(), RestClient.ParameterType.Query);
                RestClient.RestResult Result = await Client.ExecuteAsync(string.Format("Sessions/{0}/Command/SetAudioStreamIndex", this.Id), RestClient.PostType.POST);
                return new RestClient.RestResult<EmMediaStream>(Result) { Response = FoundStream };
            }
        }
        /// <summary>
        /// Stop the currently playing item.
        /// </summary>
        /// <param name="RefreshClient"></param>
        /// <returns></returns>
        public async Task<RestResultBase> Stop(bool RefreshClient = true)
        {
            if (RefreshClient)
            {
                RestResult<EmClient> CurrentClient = await Refresh();
                if (!CurrentClient.Success || CurrentClient.Response == null)
                    return CurrentClient;
            }
            if (this.NowPlayingItem == null)
                return new RestResultBase() { Error = "No item currently playing", Success = false };
            using (RestClient Client = Server.Conn.GetClient(Server.SelectedUser))
            {
                RestResult Result = await Client.ExecuteAsync(string.Format("Sessions/{0}/Playing/Stop", this.Id), PostType.POST);
                if (Result.Success)
                    this.NowPlayingItem = null;
                return Result;
            }
        }
        /// <summary>
        /// Pause the currently playing item
        /// </summary>
        /// <param name="RefreshClient"></param>
        /// <returns></returns>
        public async Task<RestResultBase> Pause(bool RefreshClient = true)
        {
            if (RefreshClient)
            {
                RestResult<EmClient> CurrentClient = await Refresh();
                if (!CurrentClient.Success || CurrentClient.Response == null)
                    return CurrentClient;
            }
            if (this.NowPlayingItem == null)
                return new RestResultBase() { Error = "No item currently playing", Success = false };
            using (RestClient Client = Server.Conn.GetClient(Server.SelectedUser))
            {
                RestResult Result = await Client.ExecuteAsync(string.Format("Sessions/{0}/Playing/Pause", this.Id), PostType.POST);
                return Result;
            }
        }
        /// <summary>
        /// Resume the playing of the current item
        /// </summary>
        /// <param name="RefreshClient"></param>
        /// <returns></returns>
        public async Task<RestResultBase> Resume(bool RefreshClient = true)
        {
            if (RefreshClient)
            {
                RestResult<EmClient> CurrentClient = await Refresh();
                if (!CurrentClient.Success || CurrentClient.Response == null)
                    return CurrentClient;
            }
            if (this.NowPlayingItem == null)
                return new RestResultBase() { Error = "No item currently playing", Success = false };
            using (RestClient Client = Server.Conn.GetClient(Server.SelectedUser))
            {
                RestResult Result = await Client.ExecuteAsync(string.Format("Sessions/{0}/Playing/Unpause", this.Id), PostType.POST);
                return Result;
            }
        }
        /// <summary>
        /// Sets the position within a file.
        /// </summary>
        /// <param name="Position"></param>
        /// <param name="RefreshClient"></param>
        /// <returns></returns>
        public async Task<RestResultBase> SetPosition(long Position, bool RefreshClient = true)
        {
            if (RefreshClient)
            {
                RestResult<EmClient> CurrentClient = await Refresh();
                if (!CurrentClient.Success || CurrentClient.Response == null)
                    return CurrentClient;
            }
            if (this.NowPlayingItem == null)
                return new RestResultBase() { Error = "No item currently playing", Success = false };
            using (RestClient Client = Server.Conn.GetClient(Server.SelectedUser))
            {
                RestResult Result = await Client.ExecuteAsync(string.Format("Sessions/{0}/Playing/Seek?SeekPositionTicks={1}", this.Id, Position), PostType.POST);
                return Result;
            }
        }
        /// <summary>
        /// Show content on the client (if supported)
        /// </summary>
        /// <param name="Item"></param>
        /// <returns></returns>
        public RestResultBase ShowContent(EmSeries Item)
        {
            if (!Server.SelectedClient.SupportedCommands.Contains("DisplayContent"))
                return new RestResult() { Error = "Current client does not support content display", Success = false };
            return new RestResultBase() { Success = true };
            /*   using (RestClient Client = Server.Conn.GetClient(Server.SelectedUser))
               {
                   Client.SetContent(new EmCommandArgs() { Arguments = new EmCommandArgs.DisplayMessage() { Header = "YEAH", Text = "This is test", TimeoutMs = 10000 } });
                   RestClient.RestResult Result = await Client.ExecuteAsync(string.Format("Sessions/{0}/Command/DisplayMessage", this.Id), RestClient.PostType.POST);
                   return Result;
               }*/
        }
        public RestResultBase ShowContent(EmSeason Item)
        {
            if (!Server.SelectedClient.SupportedCommands.Contains("DisplayContent"))
                return new RestClient.RestResult() { Error = "Current client does not support content display", Success = false };
            return new RestResultBase() { Success = true };
            /*  using (RestClient Client = Server.Conn.GetClient(Server.SelectedUser))
              {
                  Client.AddQueryParameter("ItemId", Item.Episodes[0].Id, RestClient.ParameterType.Query);
                  RestClient.RestResult Result =await Client.ExecuteAsync(string.Format("Sessions/{0}/Command/DisplayContent", this.Id), RestClient.PostType.POST);
                  return Result;
              }*/
        }
        public RestResultBase ShowContent(EmMediaItem Item)
        {
            if (!Server.SelectedClient.SupportedCommands.Contains("DisplayContent"))
                return new RestClient.RestResult() { Error = "Current client does not support content display", Success = false };
            return new RestResultBase() { Success = true };
            /* using (RestClient Client = Server.Conn.GetClient(Server.SelectedUser))
             {
                 Client.AddQueryParameter("ItemId", Item.Id.ToString(), RestClient.ParameterType.Query);
                 RestClient.RestResult Result = await Client.ExecuteAsync(string.Format("Sessions/{0}/Command/DisplayContent", this.Id), RestClient.PostType.POST);
                 return Result;
             }*/
        }
    }
}
