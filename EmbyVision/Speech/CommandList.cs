using System.Collections.Generic;

namespace EmbyVision.Speech
{
    public class CommandList : BaseSpeechItem
    {
        public CommandList()
        {
            Init();
        }
        public CommandList(params string[] Commands)
        {
            Init();
            AddString(Commands);
        }
        public CommandList(string Command)
        {
            Init();
            AddString(Command);
        }
        public CommandList(List<string> Commands)
        {
            Init();
            AddString(Commands);
        }
        public CommandList(List<SpeechContextItem> Commands)
        {
            Init();
            AddString(Commands);
        }
    }
}
