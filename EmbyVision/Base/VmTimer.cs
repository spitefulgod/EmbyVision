using System;
using System.Collections.Generic;
using System.Timers;

namespace EmbyVision.Base
{
    public delegate void TimerTickHandler(object sender, string ID, object Context);
    public class VmTimer : IDisposable
    {
        private class TimerDetails
        {
            public object Context { get; set; }
            public TimerDetails(VmTimer Parent)
            {
                this.Parent = Parent;
                this.IsInterval = false;
                Timer = new Timer();
            }
            public TimerDetails(VmTimer Parent, string ID, long Duration, bool IsInterval)
            {
                this.ID = ID;
                this.Parent = Parent;
                this.IsInterval = IsInterval;
                Timer = new Timer();
                Timer.Elapsed += Timer_Elapsed;
                Timer.Interval = Duration;
                Timer.AutoReset = IsInterval;
                Timer.Enabled = true;
            }
            public bool Enabled
            {
                get
                {
                    if (Timer == null)
                        return false;
                    return Timer.Enabled;
                }
            }
            private void Timer_Elapsed(object sender, ElapsedEventArgs e)
            {
                Parent.Timer_Tick(this, e);
            }

            private VmTimer Parent { get; set; }
            public string ID { get; set; }
            public bool IsInterval { get; set; }
            private Timer Timer { get; set; }
            public void Stop()
            {
                if (Timer != null)
                {
                    Timer.Elapsed -= this.Timer_Elapsed;
                    Timer.Enabled = false;
                    Timer.Dispose();
                }
                Timer = null;
            }
        }
        private Dictionary<string, TimerDetails> Timers;
        public event TimerTickHandler TimerReached;
        public VmTimer()
        {
            Timers = new Dictionary<string, TimerDetails>();
        }
        /// <summary>
        /// Timer has started.
        /// </summary>
        /// <param name="Duration"></param>
        public void StartTimer(string ID, long Duration)
        {
            StopTimer(ID);
            TimerDetails Timer = new TimerDetails(this, ID, Duration, false);
            Timers[ID] = Timer;
        }
        public bool Started(string ID)
        {
            if (!Timers.ContainsKey(ID) || Timers[ID] == null)
                return false;
            return Timers[ID].Enabled;
        }
        public void StartTimer(string ID, long Duration, params object[] Context)
        {
            StartTimer(ID, Duration);
            Timers[ID].Context = Context;
        }
        /// <summary>
        /// Starts an interval timer.
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="Duration"></param>
        public void StartInterval(string ID, long Duration)
        {
            StopTimer(ID);
            TimerDetails Timer = new TimerDetails(this, ID, Duration, true);
            Timers[ID] = Timer;
        }
        public void StartInterval(string ID, long Duration, params object[] Context)
        {
            StartTimer(ID, Duration);
            Timers[ID].Context = Context;
        }
        /// <summary>
        /// Timer has been fired, call the calling op and then close.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer_Tick(object sender, object e)
        {
            TimerDetails Timer = (sender as TimerDetails);
            this.TimerReached(this, Timer.ID, Timer.Context);
            if (!Timer.IsInterval)
                StopTimer(Timer);
        }
        private void StopTimer(TimerDetails Timer)
        {
            if (Timer == null)
                return;
            Timer.Stop();
            if (Timer.ID != null && Timers != null)
                Timers[Timer.ID] = null;
            Timer = null;
        }
        /// <summary>
        /// Stop the timer and close everything up.
        /// </summary>
        public void StopTimer(string ID)
        {
            if (!Timers.ContainsKey(ID))
                return;
            StopTimer(Timers[ID]);
        }
        /// <summary>
        /// Clean up
        /// </summary>
        public void Dispose()
        {
            if (Timers != null)
            {
                foreach (KeyValuePair<string, TimerDetails> TimerItem in Timers)
                    StopTimer(TimerItem.Value);
                Timers.Clear();
            }
            Timers = null;
        }
    }
}
