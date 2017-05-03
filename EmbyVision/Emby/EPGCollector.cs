using EmbyVision.Base;
using EmbyVision.Emby.Classes;
using EmbyVision.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static EmbyVision.Rest.RestClient;

namespace EmbyVision.Emby
{
    public class EPGCollector : IDisposable
    {
        public EmbyServer IntServer { get; set; }
        public EmbyServer Server
        {
            get
            {
                return IntServer;
            }
            set
            {
                Cancel();
                IntServer = value;
                ClearTimer(true);
                
                if (EPGHeaders != null)
                    EPGHeaders.Clear();
                EPGHeaders = null;
                Task.Run(async() => { await Collect(); });
            }
        }
        private int IntInterval { get; set; }
        public int Interval
        {
            get
            {
                return IntInterval;
            }
            set
            {
                IntInterval = value;
                ClearTimer(true);
            }
        }
        private List<EmEPGHeader> EPGHeaders { get; set; }
        private VmTimer UpdateTimer { get; set; }
        private CancellationTokenSource TokenSource { get; set; }

        /// <summary>
        /// Set up the collector
        /// </summary>
        public EPGCollector()
        {
            Interval = 60;
        }
        public EPGCollector(EmbyServer Server)
        {
            Interval = 60;
            this.Server = Server;
        }
        /// <summary>
        /// Go to the emby server, collect the new EPG data.
        /// </summary>
        /// <returns></returns>
        public async Task<RestResult> Collect()
        {
            int Items = 0;
            try
            {
                if (Server == null || Server.Conn == null)
                    throw new Exception("Specified server not connected");

                // IF the server has no channels then don't bother
                if (Server.TVChannels == null || Server.TVChannels.Count == 0)
                {
                    if(EPGHeaders != null)
                        EPGHeaders.Clear();
                    throw new Exception("Specified server not connected");
                }

                // Cancel if we're already running and then create a cancellation object.
                Cancel();
                TokenSource = new CancellationTokenSource();
                CancellationToken CancelEPG = TokenSource.Token;

                await Task.Run(async () =>
                {
                    Logger.Log("EPG", "Collecting EPG");
                    using (RestClient Client = Server.Conn.GetClient(Server.SelectedUser))
                    {
                        RestResult<EmCatalogList> TVData = await Client.ExecuteAsync<EmCatalogList>("/LiveTv/Programs", PostType.GET);
                        if (!TVData.Success)
                            throw new Exception(TVData.Error);

                        if (TVData.Response == null || TVData.Response.Items == null)
                            throw new Exception("No data returned");

                        // We may have lost the server while waiting, if so then exit
                        if (Server == null || Server.TVChannels == null || Server.TVChannels.Count == 0)
                            throw new Exception("Server disconnected");

                        // Construct the new list of EPG items.
                        List<EmEPGHeader> Headers = new List<EmEPGHeader>();
                        foreach (EmMediaItem Item in TVData.Response.Items)
                        {
                            // Find the correct header
                            EmEPGHeader Header = Headers.Find(x => x.ChannelId == Item.ChannelId);
                            if (Header == null)
                            {
                                EmMediaItem Channel = Server.TVChannels.Find(x => x.Id == Item.ChannelId);
                                if (Channel != null)
                                {
                                    Header = new EmEPGHeader() { Channel = Channel, Items = new List<EmMediaItem>() };
                                    Headers.Add(Header);
                                }
                            }
                            // If we have a header add the item.
                            if (Header != null)
                            {
                                Header.Items.Add(Item);
                                Items++;
                            }
                        }
                        // Save these new channels
                        if(EPGHeaders != null)
                            EPGHeaders.Clear();
                        EPGHeaders = Headers;
                    }
                }, CancelEPG);
            }
            catch(Exception ex)
            {
                TokenSource = null;
                Logger.Log("EPG", ex.Message);
                return new RestResult() { Success = false, Error = ex.Message };
            }
            Logger.Log("EPG", string.Format("EPG Updated, {0} channels with {1} entries", EPGHeaders.Count, Items));
            return new RestResult() { Success = true };
        }
        /// <summary>
        /// Recollect on interval.
        /// </summary>
        private async void UpdateTimer_TimerReached(object sender, string ID, object Context)
        {
            await Collect();
        }
        /// <summary>
        /// Kill the session if running.
        /// </summary>
        private void Cancel()
        {
            if (TokenSource != null)
                TokenSource.Cancel();
            TokenSource = null;
        }
        /// <summary>
        /// Clears the timer
        /// </summary>
        private void ClearTimer(bool Reset = false)
        {
            if (UpdateTimer != null)
            {
                UpdateTimer.TimerReached -= UpdateTimer_TimerReached;
                UpdateTimer.Dispose();
            }
            UpdateTimer = null;
            if(Reset)
            {
                UpdateTimer = new VmTimer();
                UpdateTimer.TimerReached += UpdateTimer_TimerReached;
                UpdateTimer.StartInterval("CollectEPG", (long)IntInterval * 60 * 1000);
            }
        }
        /// <summary>
        /// Clean up
        /// </summary>
        public void Dispose()
        {
            Cancel();
            ClearTimer();
            Server = null;
            if (EPGHeaders != null)
                EPGHeaders.Clear();
            EPGHeaders = null;
        }
    }
}
