using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Recognition;

namespace EmbyVision.Speech
{
    public delegate void SpeechRecognisedHandler(object sender, string Assembly, string Context, string SpokenCommand, int CommandIndex, Dictionary<string, object> SelectList);

    public class Listener : IDisposable
    {
        string _EmulatedString;
        Dictionary<string, List<BaseSpeechItem.ContextData>> _SemStore;
        SpeechRecognitionEngine _RecEngine;
        public bool Listening
        {
            get
            {
                if (_RecEngine == null)
                    return false;
                return _RecEngine.AudioState != AudioState.Stopped;
            }
        }
        public event SpeechRecognisedHandler SpeechRecognised;


        /// <summary>
        /// Start up.
        /// </summary>
        public Listener()
        {
            _SemStore = new Dictionary<string, List<BaseSpeechItem.ContextData>>();
            _RecEngine = new SpeechRecognitionEngine(new System.Globalization.CultureInfo("en-US"));
            _RecEngine.InitialSilenceTimeout = TimeSpan.FromSeconds(5);
            _RecEngine.EndSilenceTimeout = TimeSpan.FromSeconds(1);
            _RecEngine.BabbleTimeout = TimeSpan.FromSeconds(5);
            _RecEngine.SpeechRecognized += RecEngine_SpeechRecognized;
            _RecEngine.RecognizeCompleted += RecEngine_RecognizeCompleted;
            _RecEngine.EmulateRecognizeCompleted += RecEngine_EmulateRecognizeCompleted;
            _RecEngine.SetInputToDefaultAudioDevice();
        }
        /// <summary>
        /// Once emulation is complete we carry on as normal.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RecEngine_EmulateRecognizeCompleted(object sender, EmulateRecognizeCompletedEventArgs e)
        {
            StartRecognition();
        }

        /// <summary>
        /// Emulates the command
        /// </summary>
        /// <param name="Command"></param>
        public void EmulateCommand(string Command)
        {
            _RecEngine.RecognizeAsyncStop();
            _EmulatedString = Command;
        }
        /// <summary>
        /// If we've completed, then just carry on.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RecEngine_RecognizeCompleted(object sender, RecognizeCompletedEventArgs e)
        {
            if (_EmulatedString != null && _EmulatedString != "")
            {
                _RecEngine.EmulateRecognizeAsync(_EmulatedString);
                _EmulatedString = "";
            }
            else
                StartRecognition();
        }
        /// <summary>
        /// We have a match, with this we can report back to the server.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RecEngine_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            // Some recognised code here, there's probably a better way of doing this but the name contains the assembly and the context of the command, extract these and send them to the server
            // from there it will pass it back to the plugin.
            if (e.Result.Confidence < 0.4)
                return;
            string Source = e.Result.Grammar.Name;
            string[] Details = Source.Split('\n');
            string Assembly = Details[0];
            string Context = Details[1];
            int CommandIndex = int.Parse(Details[2]);

            if (SpeechRecognised != null)
            {
                Dictionary<string, object> Semantics = null;
                if (e.Result.Semantics != null && e.Result.Semantics.Count > 0)
                {
                    Semantics = new Dictionary<string, object>();
                    for (var Counter = 0; Counter < e.Result.Semantics.Count; Counter++)
                    {
                        BaseSpeechItem.ContextData Data = _SemStore[Assembly][(int)e.Result.Semantics[e.Result.Semantics.ToList()[Counter].Key].Value];
                        Semantics.Add(Data.SelectName, Data.Item);
                    }
                }

                // The speech system may have some "select" properties in here, if so we need to decide which items these are and then highlight them for the user.
                SpeechRecognised(this, Assembly, Context, e.Result.Text, CommandIndex, Semantics);
            }
        }
        /// <summary>
        /// Returns the number of commands for a given context
        /// </summary>
        /// <param name="Context"></param>
        /// <returns></returns>
        public bool HasCommands(string Context)
        {
            int Counter = 0;
            while (Counter < _RecEngine.Grammars.Count)
            {
                Grammar LoadedGrammar = _RecEngine.Grammars[Counter++];
                if (LoadedGrammar.Name.Contains(Context + '\n'))
                    return true;
            }
            return false;
        }
        /// <summary>
        /// Creates a grammar object based on the given voice commands.
        /// </summary>
        /// <param name="Commands"></param>
        public void CreateGrammarList(string Context, List<VoiceCommand> Commands)
        {
            _RecEngine.RecognizeAsyncStop();
            // Go through our loaded grammar and remove anyFixed dictation that we've already got.
            int counter = 0;
            while (counter < _RecEngine.Grammars.Count)
            {
                Grammar LoadedGrammar = _RecEngine.Grammars[counter];
                if (LoadedGrammar.Name.Contains(Context + '\n'))
                    _RecEngine.UnloadGrammar(LoadedGrammar);
                else
                    counter++;
            }

            // Make sure we have commands.
            if (Commands == null || Commands.Count == 0)
                return;

            // Add each item we have stored into a grammer builder with choices, the ItemGrammarList will collect a full list of grammar items connected to each command.
            List<BaseSpeechItem.ContextData> ItemGrammarList = new List<BaseSpeechItem.ContextData>();
            foreach (VoiceCommand vc in Commands)
            {
                int OCounter = 0;
                int CCounter = 0;
                // Make sure the command is valid.
                foreach (SpeechItem Command in vc.Commands)
                {
                    if (Command != null && Command._Items != null && Command._Items.Count > 0)
                    {
                        // Get a new Grammar Builder item and add each choice / optional item to it.
                        GrammarBuilder gb = new GrammarBuilder();
                        foreach (SpeechItem.SpeechType sp in Command._Items)
                        {
                            bool AllowIndex = sp.AllowIndexSelect;
                            if (sp.Commands != null && sp.Commands.GetItems().Count > 0)
                            {
                                if (sp.Type == 2)
                                {
                                    int ItemCount = 0;
                                    int OptionCounter = 0;
                                    GrammarBuilder[] Options = new GrammarBuilder[sp.Commands.GetItems().Count * (AllowIndex ? 2 : 1)];
                                    foreach (object spi in sp.Commands.GetItems())
                                    {
                                        ItemCount++;
                                        if (spi is BaseSpeechItem.ContextData)
                                        {
                                            ItemGrammarList.Add((BaseSpeechItem.ContextData)spi);
                                            Options[OptionCounter++] = new SemanticResultValue((spi as BaseSpeechItem.ContextData).Name, ItemGrammarList.Count - 1);
                                            if (AllowIndex)
                                                Options[OptionCounter++] = new SemanticResultValue(ItemCount.ToString(), ItemGrammarList.Count - 1);
                                        }
                                        else
                                        {
                                            Options[OptionCounter++] = new SemanticResultValue((string)spi, spi);
                                            if (AllowIndex)
                                                Options[OptionCounter++] = new SemanticResultValue(ItemCount.ToString(), spi);
                                        }

                                    }

                                    // Slightly different here, our select items are using semantics so we can get a result as to what was chosen.
                                    gb.Append(new SemanticResultKey((OCounter++).ToString(), new Choices(new Choices(Options))), 1, 1);
                                }
                                else
                                {
                                    Choices choices = new Choices();
                                    foreach (object spi in sp.Commands.GetItems())
                                        if (spi is SpeechContextItem)
                                            choices.Add((spi as SpeechContextItem).Item);
                                        else
                                            choices.Add(spi.ToString());
                                    if (sp.Type == 0)
                                        gb.Append(new GrammarBuilder(choices));
                                    if (sp.Type == 1)
                                        gb.Append(new GrammarBuilder(choices), 0, 1);
                                }
                            }
                        }
                        // Add this new command to the list we're holding, making sure we add a context we can use to back track.
                        Grammar CommandGrammer = new Grammar(gb);
                        CommandGrammer.Name = Context + '\n' + vc.Name + '\n' + (CCounter++).ToString();
                        _RecEngine.LoadGrammar(CommandGrammer);
                    }
                }
            }
            // Again don't bother if we have nothing
            if (ItemGrammarList.Count != 0)
            {
                // Save the new grammar list.
                if (_SemStore.ContainsKey(Context))
                    _SemStore.Remove(Context);
                _SemStore.Add(Context, ItemGrammarList);

            }
            //   _RecEngine.RequestRecognizerUpdate();
            StartRecognition();
        }
        /// <summary>
        /// Start recognition
        /// </summary>
        internal void StartRecognition()
        {
            if (_RecEngine != null && _RecEngine.AudioState == AudioState.Stopped && _RecEngine.Grammars.Count > 0)
            {
                try
                {
                    _RecEngine.RecognizeAsync();
                }
                catch (Exception)
                {
                    // Failed to start, means we're already probably running.
                    _RecEngine.RecognizeAsyncCancel();
                }
            }
        }
        /// <summary>
        /// Clean up
        /// </summary>
        public void Dispose()
        {
            // Clean the grammer List.
            if (_SemStore != null)
                _SemStore.Clear();
            _SemStore = null;
            // Clean up the recognition.
            if (_RecEngine != null)
            {
                _RecEngine.UnloadAllGrammars();
                _RecEngine.RecognizeAsyncCancel();
                _RecEngine.RecognizeAsyncStop();
                _RecEngine.SpeechRecognized -= RecEngine_SpeechRecognized;
                _RecEngine.RecognizeCompleted -= RecEngine_RecognizeCompleted;
                _RecEngine.EmulateRecognizeCompleted -= RecEngine_EmulateRecognizeCompleted;
            }
            _RecEngine = null;
        }
    }
}
