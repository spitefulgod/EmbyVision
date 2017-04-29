using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmbyVision.Emby.Classes
{
    public class EmCommandArgs
    {
        public class DisplayMedia
        {
            public string ItemId { get; set; }
            public string ItemName { get; set; }
            public string ItemType { get; set; }
        }
        public class DisplayMessage
        {
            public string Header { get; set; }
            public string Text { get; set; }
            public long TimeoutMs { get; set; }
        }
        public object Arguments { get; set; }
    }

}
