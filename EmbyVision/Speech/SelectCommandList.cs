using System.Collections.Generic;

namespace EmbyVision.Speech
{
    public class SelectCommandList : BaseSpeechItem
    {
        public string Name { get; protected set; }
        public bool AllowIndexSelect { get; private set; }
        public SelectCommandList()
        {
            Init();
        }
        public SelectCommandList(string Name, bool AllowIndexSelect, params string[] Commands)
        {
            Init();
            List<ContextData> ContextList = new List<ContextData>();
            foreach (string Command in Commands)
            {
                ContextData Data = new ContextData()
                {
                    Item = Command,
                    Name = Command,
                    SelectName = Name
                };
                ContextList.Add(Data);
            }
            AddString(ContextList);
            this.Name = Name;
            this.AllowIndexSelect = AllowIndexSelect;
        }
        public SelectCommandList(string Name, bool AllowIndexSelect, string Command)
        {
            Init();
            List<ContextData> ContextList = new List<ContextData>()
            {
                new ContextData() {
                    Item = Command,
                    Name = Command,
                    SelectName = Name
                }
            };
            AddString(ContextList);
            this.Name = Name;
            this.AllowIndexSelect = AllowIndexSelect;
        }
        public SelectCommandList(string Name, bool AllowIndexSelect, IEnumerable<string> Commands)
        {
            Init();
            List<ContextData> ContextList = new List<ContextData>();
            foreach (string Command in Commands)
            {
                ContextData Data = new ContextData()
                {
                    Item = Command,
                    Name = Command,
                    SelectName = Name
                };
                ContextList.Add(Data);
            }
            AddString(ContextList);
            this.Name = Name;
            this.AllowIndexSelect = AllowIndexSelect;
        }
        public SelectCommandList(string Name, bool AllowIndexSelect, IEnumerable<SpeechContextItem> Commands)
        {
            Init();
            List<ContextData> ContextList = new List<ContextData>();
            foreach (SpeechContextItem Command in Commands)
            {
                ContextData Data = new ContextData()
                {
                    SelectName = Name,
                    Item = Command.Context,
                    Name = Command.Item,
                };
                ContextList.Add(Data);
            }
            AddString(ContextList);
            this.Name = Name;
            this.AllowIndexSelect = AllowIndexSelect;
        }
        public SelectCommandList(string Name, bool AllowIndexSelect, IEnumerable<object> Commands, string DisplayPath)
        {
            Init();
            List<ContextData> ContextList = new List<ContextData>();
            foreach (object Command in Commands)
            {
                ContextData Data = new ContextData()
                {
                    Item = Command,
                    Name = Command.GetType().GetProperty(DisplayPath).GetValue(Command, null).ToString(),
                    SelectName = Name
                };
                ContextList.Add(Data);
            }
            AddString(ContextList);
            this.Name = Name;
            this.AllowIndexSelect = AllowIndexSelect;
        }
    }
}
