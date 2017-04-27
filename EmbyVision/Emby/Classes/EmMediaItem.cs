using EmbyVision.Rest;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using static EmbyVision.Rest.RestClient;

namespace EmbyVision.Emby.Classes
{
    public class EmMediaItem : IComparable<EmMediaItem>
    {
        public string Name { get; set; }
        public string ServerId { get; set; }
        public string Id { get; set; }
        public string Etag { get; set; }
        public DateTime DateCreated { get; set; }
        public bool CanDelete { get; set; }
        public bool CanDownload { get; set; }
        public string SortName { get; set; }
        public string Container { get; set; }
        public string ServiceName { get; set; }
        public string Number { get; set; }
        public string ChannelType { get; set; }
        public string ChannelNumber { get; set; }
        public string ParentId { get; set; }
        public string ChannelId { get; set; }
        public bool LockData { get; set; }
        public string DisplayPreferencesId { get; set; }
        public string LocationType { get; set; }
        public DateTime PremiereDate { get; set; }
        public string OfficialRating { get; set; }
        public float CommunityRating { get; set; }
        public long RunTimeTicks { get; set; }
        public string PlayAccess { get; set; }
        public int IndexNumber { get; set; }
        [JsonProperty("Genres")]
        public List<string> Genres { get; set; }
        [JsonProperty("CurrentProgram")]
        public EmMediaItem CurrentProgram { get; set; }
        public string EpisodeName { get { return "Episode " + IndexNumber.ToString(); } }
        public string EpisodeTitle { get; set; }
        public bool IsSeries { get; set; }
        public int ParentIndexNumber { get; set; }
        public int ProductionYear { get; set; }
        public bool IsPlaceHolder { get; set; }
        public bool IsHD { get; set; }
        public bool IsFolder { get; set; }
        public string Type { get; set; }
        public int LocalTrailerCount { get; set; }
        public EmUserData UserData { get; set; }
        public string SeriesName { get; set; }
        public string SeriesId { get; set; }
        public string SeasonId { get; set; }
        public bool IsThemeMedia { get; set; }
        public List<EmMediaSource> MediaSources { get; set; }
        public List<EmMediaStream> MediaStreams { get; set; }
        public string VideoType { get; set; }
        public EmImageTag ImageTags { get; set; }
        [JsonProperty("BackdropImageTags")]
        public List<string> BackdropImageTags { get; set; }
        public string PrimaryImageTag { get; set; }
        public string PrimaryImageItemId { get; set; }
        public string LogoImageTag { get; set; }
        public string LogoItemId { get; set; }
        public string ThumbImageTag { get; set; }
        public string ThumbItemId { get; set; }
        public string BackdropImageTag { get; set; }
        public string BackdropItemId { get; set; }
        public string MediaType { get; set; }
        public string ChapterImagesItemId { get; set; }
        public string ChannelName { get; set; }
        public string Overview { get; set; }
        public List<EmChapter> Chapters { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // Refreshes all our details, we need a server object to get the client.
        public RestResult<EmMediaItem> Refresh(EmbyServer Server)
        {
            if (Server == null || Server.SelectedUser == null)
                return new RestResult<EmMediaItem>() { Success = false, Error = "Server object not initialised" };

            if (this.Id == null)
                return new RestResult<EmMediaItem>() { Success = false, Error = "No item to update" };

            using (RestClient Client = Server.Conn.GetClient(Server.SelectedUser))
            {
                RestResult<EmMediaItem> Result = Client.Execute<EmMediaItem>(string.Format("Users/{0}/Items/{1}", Server.SelectedUser.Id, this.Id), RestClient.PostType.GET);
                if (Result.Success && Result.Response != null)
                {
                    FieldInfo[] myObjectFields = this.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    foreach (FieldInfo fi in myObjectFields)
                        fi.SetValue(this, fi.GetValue(Result.Response));
                }
                return Result;
            }
        }
        public int CompareTo(EmMediaItem other)
        {
            switch (this.Type)
            {
                case "Episode":
                    return this.IndexNumber.CompareTo(other.IndexNumber);
                case "TvChannel":
                    return this.ChannelNumber.CompareTo(other.ChannelNumber);
                default:
                    return this.Name.CompareTo(other.Name);

            }
        }
    }
}
