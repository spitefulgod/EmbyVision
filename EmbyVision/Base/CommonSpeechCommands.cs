using EmbyVision.Speech;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmbyVision.Base
{
    public class CommonSpeechCommands
    {
        private Talker Talker { get; set; }
        private Listener Listener { get; set; }

        public CommonSpeechCommands(Talker Talker, Listener Listener)
        {
            this.Listener = Listener;
            this.Talker = Talker;
            Listener.SpeechRecognised += Listener_SpeechRecognised;
        }
        /// <summary>
        /// Detected speech
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="Assembly"></param>
        /// <param name="Context"></param>
        /// <param name="SpokenCommand"></param>
        /// <param name="CommandIndex"></param>
        /// <param name="SelectList"></param>
        private void Listener_SpeechRecognised(object sender, string Assembly, string Context, string SpokenCommand, int CommandIndex, Dictionary<string, object> SelectList)
        {
            if (Assembly != "common")
                return;
            if (Context == "DateTime")
                switch (CommandIndex)
                {
                    case 0:
                        Talker.Speak(string.Format("The time is {0:HH} {0:mm}", DateTime.Now));
                        break;
                    case 1:
                        Talker.Speak(string.Format("It is the {0} or {1:MMMM}", Common.ToOrdinal(DateTime.Now.Day), DateTime.Now));
                        break;
                    default:
                        Talker.Speak(string.Format("The year is {0:yyyy}", DateTime.Now));
                        break;
                }
            if (Context == "StopTalking")
                Talker.Stop();
        }

        public void Start()
        {
            SetCommands();
        }

        private void SetCommands()
        {
            // Create the list of commands based on the information we have
            List<VoiceCommand> Commands = new List<VoiceCommand>();
            Commands.Add(new VoiceCommand()
            {
                Name = "DateTime",
                Commands = new List<SpeechItem>()
                {
                    new SpeechItem(
                        new CommandList("What time is it", "What's the time", "Tell me the time")
                    ),
                    new SpeechItem(
                        new CommandList("What is the date", "What's the date", "What date is it", "What date is it today", "What's today's date", "What is today", "Tell me the date", "Tell me what date it is")
                    ),
                    new SpeechItem(
                        new CommandList("What year is it", "What's the year", "What is the year", "What year is it today", "What's today's year", "Tell me the year", "Tell me what year it is")
                        )
                }
            });
            Commands.Add(new VoiceCommand()
            {
                Name = "StopTalking",
                Commands = new List<SpeechItem>()
                {
                    new SpeechItem(
                            new CommandList("Stop Talking", "Be Quiet", "Shut Up")
                        )
                }

            });

            Listener.CreateGrammarList("common", Commands);
        }

        public void Dispose()
        {
        }
    }
}
