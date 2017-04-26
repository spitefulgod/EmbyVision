using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EmbyVision.Base
{
    public class Options
    {
        private string FileName = "settings.json";
        public string ConnectUsername { get; set; }
        public string ConnectPassword { get; set; }
        public string BasicUsername { get; set; }
        public string BasicPassword { get; set; }
        public string DefaultClient { get; set; }
        public string ConnectedId { get; set; }
        public string DeviceId { get; set; }
        [JsonIgnore]
        public string ExternalIPAddr { get; set; }
        [JsonIgnore]
        public string Client { get
            {
                return Assembly.GetEntryAssembly().GetName().Name;
            }
        }
        [JsonIgnore]
        public string Version
        {
            get
            {
                return Assembly.GetEntryAssembly().GetName().Version.ToString();
            }
        }
        [JsonIgnore]
        public string ClientVersion
        {
            get
            {
                return string.Format("{0}/{1}", Client, Version);
            }
        }
        /// <summary>
        /// Gets a unique ID for this client, if it doesn't exist, we'll make it.
        /// </summary>
        /// <returns></returns>
        public string GetDeviceId()
        {
            if (string.IsNullOrEmpty(this.DeviceId))
            {
                this.DeviceId = Guid.NewGuid().ToString();
                this.SaveOptions();
            }
            return DeviceId;
        }
        // Handles static instance of the options object
        private static Options instance;
        public static Options Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Options();
                    instance.LoadOptions();
                }
                return instance;
            }
        }
        /// <summary>
        /// Saves the current options to the settings file.
        /// </summary>
        public void SaveOptions()
        {
            try
            {
                using (IsolatedStorageFile Storage = IsolatedStorageFile.GetUserStoreForAssembly())
                {
                    if(Storage.FileExists(FileName))
                        Storage.DeleteFile(FileName);
                    using (IsolatedStorageFileStream Stream = new IsolatedStorageFileStream(FileName, FileMode.Create, Storage))
                    using (StreamWriter Writer = new StreamWriter(Stream))
                        Writer.Write(JsonConvert.SerializeObject(this));
                }
            }
            catch (Exception)
            {

            }

        }
        /// <summary>
        /// Loads options stored in the settings file.
        /// </summary>
        public void LoadOptions()
        {
            try
            {
                using (IsolatedStorageFile Storage = IsolatedStorageFile.GetUserStoreForAssembly())
                using (IsolatedStorageFileStream Stream = new IsolatedStorageFileStream(FileName, FileMode.Open, Storage))
                using (StreamReader Reader = new StreamReader(Stream))
                {
                    string Json = Reader.ReadToEnd();
                    Options Options = JsonConvert.DeserializeObject<Options>(Json);
                    // Copy the properties.
                    FieldInfo[] myObjectFields = this.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    foreach (FieldInfo fi in myObjectFields)
                        fi.SetValue(this, fi.GetValue(Options));
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
