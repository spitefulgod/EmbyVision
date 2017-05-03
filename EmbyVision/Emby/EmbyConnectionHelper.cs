using EmbyVision.Base;
using EmbyVision.Emby.Classes;
using EmbyVision.Rest;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static EmbyVision.Rest.RestClient;

namespace EmbyVision.Emby
{
    public class EmbyConnectionHelper
    {
        private EmConnectResult UserDetails { get; set; }
        private EmbyCore Store { get; set; }

        /// <summary>
        /// Start up
        /// </summary>
        public EmbyConnectionHelper(EmbyCore Store)
        {
            this.Store = Store;
        }
        /// <summary>
        /// Gets a list of usable servers via Emby connect or basic network enumeration.
        /// </summary>
        /// <returns></returns>
        public async Task<RestResult<List<EmbyServer>>>GetServerList()
        {
            bool IsConnected = false;
            string LastError = null;
            // Clear the current list of servers.
            Store.Servers.Clear();
            // Connect to Emby Connect?
            if (!string.IsNullOrEmpty(Options.Instance.ConnectUsername) && !string.IsNullOrEmpty(Options.Instance.ConnectPassword))
            {
                Logger.Log("Emby Server", "Attempting connection to the emby connect service");
                // Attempt Authentication with the remote Emby Server.
                using (RestClient Client = new RestClient("https://connect.emby.media"))
                {
                    Client.SetContent(new EmAuth() { nameOrEmail = Options.Instance.ConnectUsername, rawpw = Options.Instance.ConnectPassword });
                    Client.AddQueryParameter("X-Application", Options.Instance.ClientVersion, RestClient.ParameterType.Header);
                    RestClient.RestResult<EmConnectResult> Result = await Client.ExecuteAsync<EmConnectResult>("service/user/authenticate", RestClient.PostType.POST);
                    // If we've connected, good, if not then we'll attempt to look on the network.
                    if (!Result.Success)
                        LastError =  Result.Error;
                    else 
                        UserDetails = Result.Response;
                }
                // Got the user information, lets get a list of servers.
                if(LastError == null)
                    using (RestClient Client = new RestClient("https://connect.emby.media"))
                    {
                        Client.AddQueryParameter("X-Application", Options.Instance.Client, RestClient.ParameterType.Header);
                        Client.AddQueryParameter("X-Connect-UserToken", UserDetails.AccessToken, RestClient.ParameterType.Header);
                        RestClient.RestResult<List<EmConnection>> Result = await Client.ExecuteAsync<List<EmConnection>>(string.Format("service/servers?userId={0}", UserDetails.User.Id));
                        if (!Result.Success)
                            LastError = Result.Error;
                        else
                        {
                            // Exit if we don't have any details
                            if (Result.Response == null || Result.Response.Count == 0)
                                LastError = "Unable to find any valid servers on emby connect";
                            else
                            {
                                // Store the server information
                                foreach (EmConnection Server in Result.Response)
                                    Store.Servers.Add(new EmbyServer() { Conn = Server });
                                IsConnected = true;
                            }
                        }
                    }
            }
            // If we didn't connect to the emby service then we'll need to check the local
            // network.
            if(!IsConnected)
            {
                Logger.Log("Emby Server", "Attempting network discovery");
                // Send out a request for servers.
                using (UdpClient Client = new UdpClient())
                {
                    Client.Client.ReceiveTimeout = 5000;
                    var RequestData = Encoding.ASCII.GetBytes("who is EmbyServer?");
                    var ServerEp = new IPEndPoint(IPAddress.Any, 0);

                    Client.EnableBroadcast = true;
                    Client.Send(RequestData, RequestData.Length, new IPEndPoint(IPAddress.Broadcast, 7359));
                    // I assume multiple servers will return multiple batch items
                    try
                    {
                        while (1 == 1)
                        {
                            byte[] ServerResponseData = Client.Receive(ref ServerEp);
                            if (ServerResponseData == null)
                                break;
                            string ServerResponse = Encoding.ASCII.GetString(ServerResponseData);
                            EmUdpClient Return = JsonConvert.DeserializeObject<EmUdpClient>(ServerResponse, new JsonSerializerSettings { DateTimeZoneHandling = DateTimeZoneHandling.Local });
                            Store.Servers.Add(new EmbyServer() { Conn = new EmConnection() { Id = Return.Id, LocalAddress = Return.Address, Name = Return.Name, Url = Return.Address } });
                        }
                    }
                    catch(Exception)
                    {
                        // Timed out probably
                    }
                    if (Store.Servers.Count > 0)
                        IsConnected = true;
                }
            }
            // Is the default server in the list, if not then check it and add it.

            // Exit with the information
            return new RestResult<List<EmbyServer>>() { Success = IsConnected, Response = Store.Servers, Error = LastError };
        }
        /// <summary>
        /// Connects to a given server.
        /// </summary>
        /// <param name="Server"></param>
        /// <param name="Talker"></param>
        /// <returns></returns>
        public async Task<RestResult> ConnectToServer(EmbyServer Server, bool Speak)
        {
            Store.ClearMedia();
            if (Server == null)
            {
                Logger.Log("Ignoring connection attempt to invalid server");
                return new RestResult() { Success = false };
            }
            Logger.Log("Emby", string.Format("Attempting connection to the server {0}", Server.ServerName));
            if (Speak)
                Store.Talker.Queue(string.Format("Connecting to the server {0}", Server.ServerName));

            RestResult Result = await Server.Connect();
            if (!Result.Success)
            {
                Logger.Log("Emby", string.Format("Unable to connect to the server {0}", Server.Conn.Name));
                Logger.Log("Emby", Result.Error);
                if (Speak)
                    Store.Talker.Queue(string.Format("Unable to connect to the server {0}, try connecting to another server", Server.Conn.Name));
                return new RestResult() { Success = false };
            }
            Logger.Log("Emby", string.Format("Connected to the server {0} ", Server.Conn.Name));
            if (Speak)
                Store.Talker.Queue(string.Format("Connected to the server {0} ", Server.Conn.Name));
            // Save the current connection as the last connected server
            Options.Instance.ConnectedId = Server.Conn.Id;
            Options.Instance.SaveOptions();

            Store.SelectedServer = Server;
            // Get the catlog information for the server, do this from the refresh command
            if (Speak)
                Store.Talker.Queue("Refreshing media");
            await Server.RefreshCatalog();
            if (Speak)
                Store.InterpretCommands.TalkCatalog();

            await Store.SetCommands.SetCommands(Server);
            // Collect EPG information
            Store.EPG.Server = Server;
            return Result;
        }

        /// <summary>
        /// Refreshes the list of servers and then connects to the last server used or the most available.
        /// </summary>
        public async Task RefreshServerList(bool ConnectOnReload = true)
        {
            EmbyServer UseServer = null;
            // First up connect to emby, get a list of servers.
            Logger.Log("Emby", "Retrieving list of avilable servers");
            await GetServerList();
            // Check available servers.
            if (Store.Servers == null || Store.Servers.Count == 0)
            {
                Logger.Log("Emby", "No available Emby servers, try setting a service using EnableEmbyConnect or EnableEmbyBasic");
                Store.Talker.Speak("No media servers currently available, check your setup then say Refresh Servers", true);
            }
            else
            {
                Logger.Log("Emby", string.Format("Connected to the emby service {0} server{1} available", Store.Servers.Count, Store.Servers.Count == 1 ? "" : "s"));
                Store.Talker.Speak("Connected to the emby service", true);
                Store.Talker.Queue(string.Format("{0} media server{1} available", Store.Servers.Count, Store.Servers.Count == 1 ? "" : "s"));
                foreach (EmbyServer Server in Store.Servers)
                    Logger.Log("Emby", string.Format("{1} server {0}", Server.Conn.Name, Server.Conn.IsLocal ? "Local" : "Remote"));
            }
            if (ConnectOnReload)
            {
                // Connect to the most appropiate server.
                if (!string.IsNullOrEmpty(Options.Instance.ConnectedId))
                    foreach (EmbyServer Server in Store.Servers)
                        if (Server.Conn.Id == Options.Instance.ConnectedId)
                        {
                            UseServer = Server;
                            break;
                        }
                // get a default server, as long as the force Previous server isn't initialised
                if (UseServer == null && !(Options.Instance.ForcePrevServer && string.IsNullOrEmpty(Options.Instance.ConnectedId)))
                    foreach (EmbyServer Server in Store.Servers)
                        if (Server.Conn.IsLocal || (UseServer == null && !Server.ConnectionAttempted))
                            UseServer = Server;

                // We should have a server, now attempt a connection.
                if (UseServer == null)
                    Store.Talker.Queue("No default server set, try saying List Servers then Connect to server");
                else
                    await ConnectToServer(UseServer, true);
            }
        }
    }
}
