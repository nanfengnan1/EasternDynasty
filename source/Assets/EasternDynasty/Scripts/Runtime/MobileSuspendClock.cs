using System;

namespace EasternDynasty
{
    internal sealed class MobileSuspendClock
    {
        private DateTime pausedAt;
        internal bool IsPaused { get; private set; }
        internal bool Suspend(DateTime now)
        { if(IsPaused)return false; pausedAt=now;IsPaused=true;return true; }
        internal double Resume(DateTime now,double maximumSeconds)
        {
            if(!IsPaused)return 0;
            IsPaused=false;
            return Math.Max(0,Math.Min(maximumSeconds,(now-pausedAt).TotalSeconds));
        }
    }
}
