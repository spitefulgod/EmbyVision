using EmbyVision.Base;
using System;
using System.Collections.Generic;
using System.Speech.Synthesis;
using System.Threading.Tasks;

namespace EmbyVision.Speech
{
    public delegate void TalkingHandler(object sender, bool Talking);

    public class Talker : IDisposable
    {
        private Queue<string> StoredSpeech;
        private bool Processing = false;
        private SpeechSynthesizer Synthesizer;
        public event TalkingHandler Speaking;
        private VmTimer StopTimer;
        private bool IsSpeaking { get; set; }

        public Talker()
        {
            Synthesizer = new SpeechSynthesizer();
            Synthesizer.SpeakCompleted += Synthesizer_SpeakCompleted;
            StopTimer = new VmTimer();
            StopTimer.TimerReached += StopTimer_TimerReached;
        }
        /// <summary>
        /// The system has stopped talking at least a second ago.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="ID"></param>
        /// <param name="Context"></param>
        private void StopTimer_TimerReached(object sender, string ID, object Context)
        {
            if (Speaking != null)
                Speaking(this, false);
            IsSpeaking = false;
        }

        /// <summary>
        /// Stopped speaking??? move to the next entry.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// 
        private void Synthesizer_SpeakCompleted(object sender, SpeakCompletedEventArgs e)
        {
            Processing = false;
            if (!e.Cancelled && StoredSpeech != null && StoredSpeech.Count > 0)
                SpeakNext();
            else
                Stop();
        }

        /// <summary>
        /// Stop speaking
        /// </summary>
        public void Stop()
        {
            try
            {
                // Clear the list of items.
                if (StoredSpeech != null)
                    StoredSpeech.Clear();
                StoredSpeech = null;
                // Stop current open stream
                if (Synthesizer != null)
                    Synthesizer.SpeakAsyncCancelAll();
                Processing = false;
                StopTimer.StartTimer("StopTalking", 1000, this);
            }
            catch (Exception)
            {

            }
        }
        /// <summary>
        /// If we have been given a list of items to say we need to state them one at once.
        /// </summary>
        private async Task SpeakNext()
        {
            if (StoredSpeech == null || StoredSpeech.Count == 0)
            {
                Stop();
                return;
            }

            string Speech = StoredSpeech.Dequeue();
            if (Speech == null || Speech.Trim() == "")
            {
                await SpeakNext();
                return;
            }
            // Synchronous
            if (!Processing && !IsSpeaking)
            {
                IsSpeaking = true;
                if (Speaking != null)
                    Speaking(this, true);
            }
            Processing = true;
            StopTimer.StopTimer("StopTalking");
            Prompt Talk = Synthesizer.SpeakAsync(Speech);
            Logger.Log("Talker", Speech);
        }

        /// <summary>
        /// Speaks the given speech entry
        /// </summary>
        /// <param name="Speech"></param>
        public async Task SpeakWait(IEnumerable<string> Speech)
        {
            Stop();
            // Clone speech to avoid modification outside of class
            Queue(Speech);
            // Clone speech to avoid modification outside of class
            foreach (string item in Speech)
            {
                if (StoredSpeech == null)
                    StoredSpeech = new Queue<string>();
                StoredSpeech.Enqueue(item);
            }
            try
            {
                if (!Processing)
                    await SpeakNext();
            }
            catch (Exception)
            {
                await SpeakNext();
            }
        }
        /// <summary>
        /// Speak a single string
        /// </summary>
        /// <param name="Speech"></param>
        public void Speak(string Speech)
        {
            Speak(new List<string>() { Speech });
        }
        public void Speak(string Speech, bool WaitForLastStatement)
        {
            if (WaitForLastStatement && (StoredSpeech == null || StoredSpeech.Count == 0))
            {
                if (StoredSpeech != null)
                    StoredSpeech.Clear();
                Queue(Speech);
            }
            else
                Speak(new List<string>() { Speech });
        }
        /// <summary>
        /// Speaks the given speech entry
        /// </summary>
        /// <param name="Speech"></param>
        public void Speak(IEnumerable<string> Speech)
        {
            Stop();
            // Clone speech to avoid modification outside of class
            Queue(Speech);
        }
        public void Speak(IEnumerable<string> Items, string Label, bool AddNumbers)
        {
            Stop();
            Queue(Items, Label, AddNumbers);
        }
        public void Speak(IEnumerable<object> Items, string Property, string Label, bool AddNumbers)
        {
            Stop();
            Queue(Items, Property, Label, AddNumbers);
        }
        public void Queue(string Speech)
        {
            Queue(new List<string>() { Speech });
        }
        public void Speak(params object[] Speech)
        {
            Stop();
            Queue(Speech);
        }
        public void Queue(params object[] Speech)
        {
            List<string> items = new List<string>();
            foreach (object item in Speech)
            {
                if (item is string)
                    items.Add((string)item);
                else if (item is List<string>)
                    foreach (string s in (List<string>)item)
                        items.Add(s);
                else if (item is string[])
                    foreach (string s in (string[])item)
                        items.Add(s);
                else if (item != null)
                    items.Add(item.ToString());
            }
            Queue(items);
        }
        public void Queue(IEnumerable<object> Items, string DisplayPath, string Label, bool AddNumbers)
        {
            List<string> Commands = new List<string>();
            foreach (object Item in Items)
                Commands.Add(Item.GetType().GetProperty(DisplayPath).GetValue(Item, null).ToString());
            Queue(Commands, Label, AddNumbers);
        }

        public void Queue(IEnumerable<string> Items, string Label, bool AddNumbers)
        {
            string itemType = Label;
            itemType = itemType == null || itemType == "" ? "" : char.ToUpper(itemType[0]) + itemType.Substring(1);

            int counter = 0;
            List<string> speechList = new List<string>();
            if (Items != null)
            {
                foreach (string ItemLabel in Items)
                {
                    counter++;
                    speechList.Add(string.Format("{0}{1}", AddNumbers ? itemType + " " + "[" + counter.ToString() + "] " : "", ItemLabel));
                }
            }
            if (counter == 0)
                Queue(string.Format("There are currently no {0} available", AddS(itemType)));
            else
                Queue(string.Format("There {2} {0} {1}.", counter, counter == 1 ? Label : AddS(Label), counter == 1 ? "is" : "are"), speechList);
        }
        /// <summary>
        /// Adds an s to the first item of the string
        /// </summary>
        /// <param name="Item"></param>
        /// <returns></returns>
        private string AddS(string Item)
        {
            if (Item == null || Item == "")
                return "";

            int first = Item.IndexOf(" ");
            if (first < 0)
            {
                return Item.Substring(Item.Length - 1, 1) == "s" ? Item : Item + "s";
            }

            string fullItem = Item.Substring(0, first);
            if (fullItem.Substring(fullItem.Length - 1, 1) == "s")
                return Item;
            return fullItem + "s" + Item.Substring(first);
        }
        /// <summary>
        /// Speaks the given speech entry
        /// </summary>
        /// <param name="Speech"></param>
        public void Queue(IEnumerable<string> Speech)
        {
            bool Found = false;
            foreach (string SpeechItem in Speech)
                if (!string.IsNullOrEmpty(SpeechItem))
                {
                    Found = true;
                    break;
                }
                if (!Found)
                    return;

                // Clone speech to avoid modification outside of class
                foreach (string item in Speech)
                {
                    if (StoredSpeech == null)
                        StoredSpeech = new Queue<string>();
                    StoredSpeech.Enqueue(item);
                }
                try
                {
                    if (!Processing)
                        SpeakNext();
                }
                catch (Exception)
                {
                    SpeakNext();
                }
            }
        /// <summary>
        /// Clean up
        /// </summary>
        public void Dispose()
        {
            Stop();
            if (Synthesizer != null)
            {
                Synthesizer.SpeakCompleted -= Synthesizer_SpeakCompleted;
                Synthesizer.Dispose();
            }
            if (StopTimer != null)
            {
                StopTimer.TimerReached -= StopTimer_TimerReached;
                StopTimer.Dispose();
            }
            StopTimer = null;
            Synthesizer = null;
        }
    }
}
