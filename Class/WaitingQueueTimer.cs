using System;
using System.Timers;

namespace FreeswitchListenerServer.Class
{
    public class WaitingQueueTimer
    {
        public static Timer SetTimer(double intervalSec,Action timeoutFinishedAction)
        {
            var newTimer = new Timer();
            newTimer.Elapsed += (sender, e) => OnTimedEvent(sender, e, timeoutFinishedAction);
            newTimer.Interval = intervalSec * 1000;
            newTimer.Enabled = true;
            return newTimer;
        }

        private static void OnTimedEvent(object source, ElapsedEventArgs e,Action action)
        {
            Timer timer = (Timer)source; 
            timer.Stop();
            action();
            timer.Dispose();
        }
    }
}
