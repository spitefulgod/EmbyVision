using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;


namespace EmbyVision.Rest
{
    public class RestClient : IDisposable
    {
        public string CurrentUrl(string function)
        {
            return ConstructUri(function);
        }
        public enum PostType
        {
            GET,
            POST,
            UPDATE,
            DELETE,
            PATCH
        }
        public enum ParameterType
        {
            Query,
            Header,
            Form
        }
        private class Params
        {
            public string Parameter { get; set; }
            public string Value { get; set; }
            public ParameterType Type { get; set; }
        }
        public class RestResultBase
        {
            public bool Success { get; set; }
            public string Error { get; set; }
            public List<RestSharp.RestResponseCookie> Cookies { get; set; }
        }
        public class RestResult : RestResultBase
        {
            public string Response { get; set; }
        }
        public class RestResult<T> : RestResultBase
        {
            public RestResult() { }
            public RestResult(RestResultBase Source)
            {
                this.Success = Source.Success;
                this.Error = Source.Error;
                if (Source.Cookies != null)
                {
                    this.Cookies = new List<RestSharp.RestResponseCookie>();
                    foreach (RestSharp.RestResponseCookie Cookie in Source.Cookies)
                        this.Cookies.Add(new RestSharp.RestResponseCookie() { Comment = Cookie.Comment, CommentUri = Cookie.CommentUri, Discard = Cookie.Discard, Domain = Cookie.Domain, Expired = Cookie.Expired, Expires = Cookie.Expires, HttpOnly = Cookie.HttpOnly, Name = Cookie.Name, Path = Cookie.Path, Port = Cookie.Port, Secure = Cookie.Secure, Value = Cookie.Value, Version = Cookie.Version });
                }
            }
            public T Response { get; set; }
        }

        private object Content { get; set; }

        private List<RestSharp.RestResponseCookie> Cookies { get; set; }
        private List<Params> Parameters;
        private Uri BaseUri { get; set; }
        /// <summary>
        /// Initiator
        /// </summary>
        public RestClient()
        {
            Parameters = new List<Params>();
        }
        public RestClient(Uri BaseUri)
        {
            this.BaseUri = BaseUri;
            Parameters = new List<Params>();
        }
        public RestClient(string BaseUri)
        {
            this.BaseUri = new Uri(BaseUri);
            Parameters = new List<Params>();
        }
        public void ClearParams()
        {
            Parameters.Clear();
        }
        public void SetContent(object Content)
        {
            this.Content = Content;
        }
        public void SetCookies(List<RestSharp.RestResponseCookie> values)
        {
            Cookies = null;
            if (values != null)
                Cookies = values;
        }
        private string Strip(string source, string first, string last)
        {
            int start = source.IndexOf(first);
            int end = source.IndexOf(last, start + first.Length);

            if (start == -1 || end == -1)
                return null;

            return source.Substring(start + first.Length, end - start - first.Length);
        }
        /// <summary>
        /// Adds a parameter that we'll use as a query parameter.
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Value"></param>
        public void AddQueryParameter(string Name, string Value, ParameterType Type)
        {
            Parameters.Add(new Params() { Parameter = Name, Value = Value, Type = Type });
        }
        public RestResult Execute(string Function, PostType Type = PostType.GET)
        {
            RestResult Result = null;
            Task.Run(async () =>
            {
                Result = await ExecuteAsync(Function, Type);
            }).Wait();
            return Result;
        }
        /// <summary>
        /// Loads up the information from a standard Execute and then loads that into an object via serialisation.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Function"></param>
        /// <param name="Type"></param>
        /// <returns></returns>
        public async Task<RestResult<T>> ExecuteAsync<T>(string Function, PostType Type = PostType.GET)
        {
            RestResult<T> resultObject = new RestResult<T>();
            try
            {
                RestResult result = await ExecuteAsync(Function, Type);
                if (!result.Success)
                    throw new Exception(result.Error == null ? result.Response : result.Error);

                resultObject = new RestResult<T>(result);

                if (result.Response.Trim().IndexOf("{") == 0 || result.Response.Trim().IndexOf("[") == 0)
                {
                    resultObject.Response = (T)JsonConvert.DeserializeObject(result.Response, typeof(T), new JsonSerializerSettings { DateTimeZoneHandling = DateTimeZoneHandling.Local });
                }
                else
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(T));
                    using (MemoryStream memXml = new MemoryStream(Encoding.UTF8.GetBytes(result.Response.Replace("\n", "").Replace("\r", ""))))
                    {
                        resultObject.Response = (T)serializer.Deserialize(memXml);
                    }
                    serializer = null;
                }
            }
            catch (Exception ex)
            {
                resultObject.Success = false;
                resultObject.Error = ex.Message;
            }
            return resultObject;
        }
        public RestResult<T> Execute<T>(string Function, PostType Type = PostType.GET)
        {
            RestResult<T> resultObject = null;
            Task.Run(async () =>
            {
                resultObject = await ExecuteAsync<T>(Function, Type);
            }).Wait();
            return resultObject;
        }

        public T Deserialise<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings { DateTimeZoneHandling = DateTimeZoneHandling.Local });
        }
        /// <summary>
        /// Executes the given Uri with parameters provided
        /// </summary>
        /// <returns></returns>
        public async Task<RestResult> ExecuteAsync(string Function, PostType Type = PostType.GET)
        {
            RestSharp.RestRequest Request = new RestSharp.RestRequest();
            RestSharp.IRestResponse Response = null;

            RestResult result = null;

            try
            {
                await Task.Run(() =>
                {
                    if (this.Cookies != null)
                        foreach (RestSharp.RestResponseCookie Cookie in this.Cookies)
                            Request.AddCookie(Cookie.Name, Cookie.Value);
                    if (Parameters != null)
                        foreach (Params Param in Parameters)
                        {
                            if (Param.Type == ParameterType.Query)
                                Request.Parameters.Add(new RestSharp.Parameter() { Name = Param.Parameter, Value = Param.Value, Type = RestSharp.ParameterType.UrlSegment });
                            if (Param.Type == ParameterType.Form)
                                Request.Parameters.Add(new RestSharp.Parameter() { Name = Param.Parameter, Value = Param.Value, Type = RestSharp.ParameterType.GetOrPost });
                        }
                    // Add Content
                    if (this.Content != null && this.Content.ToString() != "")
                    {
                        //  Request.AddHeader("Accept", "application/json");
                        //Request.AddParameter("application/json", JsonConvert.SerializeObject(this.Content), RestSharp.ParameterType.RequestBody);
                        //Request.RequestFormat = RestSharp.DataFormat.Json;
                        Request.AddJsonBody(this.Content);
                        //Request.AddParameter("application/json", this.Content.ToString(), RestSharp.ParameterType.RequestBody);
                    }
                    RestSharp.RestClient Client = new RestSharp.RestClient(ConstructUri(Function));
                    // Add the default headers
                    AddHeaders(Client);
                    switch (Type)
                    {
                        case PostType.PATCH:
                            Request.Method = RestSharp.Method.PATCH;
                            Response = Client.Execute(Request);
                            break;
                        case PostType.GET:
                            Request.Method = RestSharp.Method.GET;
                            Response = Client.Execute(Request);
                            break;
                        case PostType.DELETE:
                            Request.Method = RestSharp.Method.DELETE;
                            Response = Client.Execute(Request);
                            break;
                        default:
                            Request.Method = RestSharp.Method.POST;
                            Response = Client.Execute(Request);
                            break;
                    }
                });
                if (Response.StatusCode == HttpStatusCode.OK || (Response.ResponseStatus == RestSharp.ResponseStatus.Completed && Response.StatusCode == HttpStatusCode.NoContent))
                {
                    result = new RestResult() { Success = true, Response = Response.Content, Cookies = (List<RestSharp.RestResponseCookie>)Response.Cookies };
                }
                else
                {
                    result = new RestResult() { Success = false, Error = Response.ErrorMessage, Response = Response.Content };
                }
            }
            catch (Exception ex)
            {
                result = new RestResult() { Success = true, Error = ex.Message.ToString() };
            }
            return result;
        }
        private byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }
        /// <summary>
        /// Adds headers to a request
        /// </summary>
        /// <param name="Request"></param>
        private void AddHeaders(HttpRequestMessage Request)
        {
            Request.Headers.Clear();
            // Add new items
            foreach (Params Param in Parameters)
                if (Param.Type == ParameterType.Header)
                    Request.Headers.Add(Param.Parameter, Param.Value);
        }
        /// <summary>
        /// Adds any headers we have onto the request
        /// </summary>
        /// <param name="Request"></param>
        private void AddHeaders(RestSharp.RestClient Client)
        {
            Client.DefaultParameters.Clear();
            // Add new items
            foreach (Params Param in Parameters)
                if (Param.Type == ParameterType.Header)
                    Client.DefaultParameters.Add(new RestSharp.Parameter() { Name = Param.Parameter, Value = Param.Value, Type = RestSharp.ParameterType.HttpHeader });
        }
        private Dictionary<string, string> GetFormItems()
        {
            Dictionary<string, string> requestData = new Dictionary<string, string>();
            // Add new items
            foreach (Params Param in Parameters)
                if (Param.Type == ParameterType.Form)
                    requestData[Param.Parameter] = Param.Value;
            return requestData;
        }
        /// <summary>
        /// onstructs a URL woth the information we have.
        /// </summary>
        /// <param name="Function"></param>
        /// <returns></returns>
        private string ConstructUri(string Function)
        {
            bool first = true;
            string uri = BaseUri.ToString() + (Function.IndexOf("/") == 0 ? Function.Substring(1) : Function);
            foreach (Params Param in Parameters)
                if (Param.Type == ParameterType.Query)
                {
                    uri += first ? '?' : '&';
                    uri += Param.Parameter + "=" + Uri.EscapeDataString(Param.Value);
                    first = false;
                }
            return uri;
        }
        /// <summary>
        /// Creates a base64 string
        /// </summary>
        /// <param name="Value"></param>
        public string CreateBase64(string Value)
        {
            return System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(Value));
        }

        public void Dispose()
        {
            Parameters = null;
        }
    }
}
