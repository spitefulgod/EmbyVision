using EmbyVision.Base;
using EmbyVision.Emby.Classes;
using EmbyVision.Rest;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static EmbyVision.Rest.RestClient;

namespace EmbyVision.Emby
{
    public class EmbyServerHelper : IDisposable
    {
        private EmConnectResult UserDetails { get; set; }
        public List<EmbyServer> Servers { get; set; }

        /// <summary>
        /// Start up
        /// </summary>
        public EmbyServerHelper()
        {
            Servers = new List<EmbyServer>();
        }
        /// <summary>
        /// Gets a list of usable servers via Emby connect or basic network enumeration.
        /// </summary>
        /// <returns></returns>
        public RestResult<List<EmbyServer>>GetServerList()
        {
            bool IsConnected = false;
            string LastError = null;
            // Clear the current list of servers.
            Servers.Clear();
            // Connect to Emby Connect?
            if (!string.IsNullOrEmpty(Options.Instance.ConnectUsername) && !string.IsNullOrEmpty(Options.Instance.ConnectPassword))
            {
                Logger.Log("Emby Server", "Attempting connection to the emby connect service");
                // Attempt Authentication with the remote Emby Server.
                using (RestClient Client = new RestClient("https://connect.emby.media"))
                {
                    Client.SetContent(new EmAuth() { nameOrEmail = Options.Instance.ConnectUsername, rawpw = Options.Instance.ConnectPassword });
                    Client.AddQueryParameter("X-Application", Options.Instance.ClientVersion, RestClient.ParameterType.Header);
                    RestClient.RestResult<EmConnectResult> Result = Client.Execute<EmConnectResult>("service/user/authenticate", RestClient.PostType.POST);
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
                        RestClient.RestResult<List<EmConnection>> Result = Client.Execute<List<EmConnection>>(string.Format("service/servers?userId={0}", UserDetails.User.Id));
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
                                    Servers.Add(new EmbyServer() { Conn = Server });
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
                            EmUdpClient Return = JsonConvert.DeserializeObject<EmUdpClient>(ServerResponse);
                            Servers.Add(new EmbyServer() { Conn = new EmConnection() { Id = Return.Id, LocalAddress = Return.Address, Name = Return.Name, Url = Return.Address } });
                        }
                    }
                    catch(Exception)
                    {
                        // Timed out probably
                    }
                    if (Servers.Count > 0)
                        IsConnected = true;
                }
            }
            // Is the default server in the list, if not then check it and add it.

            // Exit with the information
            return new RestResult<List<EmbyServer>>() { Success = IsConnected, Response = Servers, Error = LastError };
        }
        /// <summary>
        /// Clean up
        /// </summary>
        public void Dispose()
        {
            if (Servers != null)
                Servers.Clear();
            Servers = null;
            UserDetails = null;
        }
    }
}
