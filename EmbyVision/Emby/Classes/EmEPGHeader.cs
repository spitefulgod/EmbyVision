using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmbyVision.Emby.Classes
{
    class EmEPGHeader : IDisposable
    {
        public string ChannelId { get
            {
                return Channel == null || string.IsNullOrEmpty(Channel.Id) ? null : Channel.Id;
            }
        }
        public EmMediaItem Channel { get; set; }
        public List<EmMediaItem> Items { get; set; }

        /// <summary>
        /// Clean up
        /// </summary>
        public void Dispose()
        {
            Channel = null;
            if(Items != null)
                Items.Clear();
            Items = null;
        }
    }
}
