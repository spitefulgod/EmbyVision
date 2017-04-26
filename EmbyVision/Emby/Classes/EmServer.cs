using System.Collections.Generic;

namespace EmbyVision.Emby.Classes
{
    public class EmServer
    {
        public EmServer() { }
        public EmServer(string Server, int Port)
        {
            this.Server = Server;
        }
        public List<EmMediaItem> Movies { get; set; }
        public List<EmMediaItem> TVChannels { get; set; }
        public List<EmSeries> TVShows { get; set; }
        public EmClient SelectedClient { get; private set; }
        public List<EmClient> Clients { get; private set; }
        public EmAuthResult LoginDetails { get; private set; }
        public List<EmUser> Users;
        public EmUser SelectedUser;
        public string Server { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
