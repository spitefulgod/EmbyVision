using System.Collections.Generic;

namespace EmbyVision.Speech
{
    public class VoiceCommand
    {
        public string Name { get; set; }
        public List<SpeechItem> Commands { get; set; }
        public SpeechItem Command
        {
            set
            {
                if (Commands != null)
                    Commands.Clear();
                else
                    Commands = new List<SpeechItem>();
                Commands.Add((SpeechItem)value.Clone());
            }
        }
        public VoiceCommand Clone()
        {
            VoiceCommand Item = new VoiceCommand();
            Item.Name = this.Name;
            Item.Commands = new List<SpeechItem>();
            foreach (SpeechItem SpeechItem in Commands)
                Item.Commands.Add((SpeechItem)SpeechItem.Clone());
            return Item;
        }
    }
}
