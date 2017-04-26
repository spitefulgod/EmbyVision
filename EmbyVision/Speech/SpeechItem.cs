using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmbyVision.Speech
{
    public class SpeechItem
    {
        internal class SpeechType
        {
            public int Type { get; set; }
            public string Index { get; set; }
            public bool AllowIndexSelect { get; set; }
            public BaseSpeechItem Commands { get; set; }

            public object Clone()
            {
                return new SpeechType() { Type = this.Type, Index = this.Index, AllowIndexSelect = this.AllowIndexSelect, Commands = (BaseSpeechItem)this.Commands.Clone() };
            }
        }
        internal List<SpeechType> _Items;
        public SpeechItem()
        {
            Init();
        }
        public SpeechItem(params object[] Items)
        {
            Init();
            if (Items != null && Items.Length > 0)
                foreach (object Item in Items)
                {
                    bool AllowIndexSelect = false;
                    string Index = null;
                    int CommandType = 0;
                    if (Item is DictationCommand)
                        CommandType = 3;
                    if (Item is OptionalCommandList)
                        CommandType = 1;
                    if (Item is SelectCommandList)
                    {
                        Index = (Item as SelectCommandList).Name;
                        CommandType = 2;
                        AllowIndexSelect = (Item as SelectCommandList).AllowIndexSelect;
                    }
                    if (Item is BabbleCommand)
                        CommandType = 4;
                    _Items.Add(new SpeechType() { Commands = (BaseSpeechItem)Item, Type = CommandType, Index = Index, AllowIndexSelect = AllowIndexSelect });
                }
        }

        private void Init()
        {
            _Items = new List<SpeechType>();
        }

        public object Clone()
        {
            SpeechItem Item = new SpeechItem();
            foreach (SpeechType item in this._Items)
                Item._Items.Add((SpeechType)item.Clone());
            return Item;
        }
    }
}
