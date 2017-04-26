using System.Collections.Generic;

namespace EmbyVision.Speech
{
    public class OptionalCommandList : BaseSpeechItem
    {
        public OptionalCommandList()
        {
            Init();
        }
        public OptionalCommandList(params string[] Commands)
        {
            Init();
            AddString(Commands);
        }
        public OptionalCommandList(string Command)
        {
            Init();
            AddString(Command);
        }
        public OptionalCommandList(List<string> Commands)
        {
            Init();
            AddString(Commands);
        }
        public OptionalCommandList(IEnumerable<object> Commands)
        {
            Init();
            List<string> CommandList = new List<string>();
            foreach (object Command in Commands)
            {
                if (Command is string)
                    CommandList.Add(Command.ToString());
                if (Command is ContextData)
                    CommandList.Add((Command as ContextData).Name);
                if (Command is SpeechContextItem)
                    CommandList.Add((Command as SpeechContextItem).Item);
            }
            AddString(CommandList);
        }
    }
}
