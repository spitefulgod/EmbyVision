using EmbyVision.Base;
using Newtonsoft.Json;
using System;

namespace EmbyVision.Emby.Classes
{
    public class EmUser
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string DisplayName { get; set; }
        public string ServerId { get; set; }
        public string ConnectUserName { get; set; }
        public string ConnectUserId { get; set; }
        public string ConnectLinkType { get; set; }
        public string Id { get; set; }
        public string ImageUrl { get; set; }
        public bool? IsSupporter { get; set; }
        public bool IsActive { get; set; }
        public bool HasPassword { get; set; }
        public bool HasConfiguredPassword { get; set; }
        public bool HasConfiguredEasyPassword { get; set; }
        public bool EnableAutoLogin { get; set; }
        [JsonIgnore]
        public bool LinkedUser { get
            {
                return ConnectUserName.ToLower() == (Options.Instance.ConnectUsername ?? "").ToLower() && ConnectLinkType == "LinkedUser";
            }
        }
        [JsonIgnore]
        public string UsableId
        {
            get
            {
                if (LinkedUser && ExchangeToken != null && !string.IsNullOrEmpty(ExchangeToken.LocalUserId))
                    return ExchangeToken.LocalUserId;
                return Id;
            }
        }
        [JsonIgnore]
        public string UsableUsername
        {
            get
            {
                if (LinkedUser && !string.IsNullOrEmpty(ConnectUserName))
                    return ConnectUserName;
                return Name;
            }
        }
        [JsonIgnore]
        public string UsablePassword
        {
            get
            {
                if (LinkedUser && !string.IsNullOrEmpty(Options.Instance.ConnectPassword))
                    return Options.Instance.ConnectPassword;
                return Options.Instance.BasicPassword ?? "";
            }
        }
        public DateTime? ExpDate { get; set; }
        public DateTime LastLoginDate { get; set; }
        public DateTime LastActivityDate { get; set; }
        public EmConfiguration Configuration { get; set; }
        public EmPolicy Policy { get; set; }
        public EmExchToken ExchangeToken { get; set; }

        public string Authorisation
        {
            get
            {
                return string.Format("MediaBrowser UserId=\"{0}\", Client=\"{1}\", Device=\"{2}\", DeviceId=\"{3}\", Version=\"{4}\"", UsableId, Options.Instance.Client, "Windows", Options.Instance.GetDeviceId(), Options.Instance.Version);
            }
        }
    }
}
