using System.Collections.Generic;

namespace EmbyVision.Speech
{
    public partial class BaseSpeechItem
    {
        public class ContextData
        {
            public string SelectName { get; set; }
            public string Name { get; set; }
            public object Item { get; set; }
        }
        internal List<object> Items;

        public BaseSpeechItem()
        {
            Init();
        }
        internal void Init()
        {
            Items = new List<object>();
        }
        public List<object> GetItems()
        {
            return Items;
        }
        public List<string> GetStringItems()
        {
            List<string> Return = new List<string>();
            foreach (object Item in Items)
                if (Item is ContextData)
                    Return.Add((Item as ContextData).Name);
                else if (Item is SpeechContextItem)
                    Return.Add((Item as SpeechContextItem).Item);
                else
                    Return.Add(Item.ToString());
            return Return;
        }
        public void AddString(IEnumerable<object> Commands)
        {
            if (Commands != null)
                foreach (object Item in Commands)
                    Items.Add(Item);
        }
        public void AddString(params string[] Commands)
        {
            if (Commands != null && Commands.Length > 0)
                foreach (object Item in Commands)
                    Items.Add(Item);
        }
        public void AddString(string Command)
        {
            if (Command != null && Command != "")
                Items.Add(Command);
        }
        public object Clone()
        {
            BaseSpeechItem Return = new BaseSpeechItem();
            Return.AddString(this.Items);
            return Return;
        }
    }
}
