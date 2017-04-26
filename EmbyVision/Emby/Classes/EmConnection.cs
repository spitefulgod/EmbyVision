using EmbyVision.Base;
using EmbyVision.Rest;
using static EmbyVision.Rest.RestClient;

namespace EmbyVision.Emby.Classes
{
    public class EmConnection
    {
        public string Id { get; set; }
        public string Url { get; set; }
        public string Name { get; set; }
        public string SystemId { get; set; }
        public string AccessKey { get; set; }
        public string LocalAddress { get; set; }
        public string UserType { get; set; }
        public string SupporterKey { get; set; }
        public bool IsLocal { get
            {
                return Url.Contains(string.Format("://{0}", Options.Instance.ExternalIPAddr)) || Url == LocalAddress;
            }
        }
        public RestClient GetClient()
        {
            RestClient Client = new RestClient(IsLocal ? LocalAddress : Url);
            Client.AddQueryParameter("X-MediaBrowser-Token", AccessKey, ParameterType.Header);
            return Client;
        }
        public RestClient GetClient(EmUser User)
        {
            if(User == null)
                return GetClient();
            RestClient Client = GetClient();
            Client.AddQueryParameter("Authorization", User.Authorisation, ParameterType.Header);
            return Client;
        }
    }
}
