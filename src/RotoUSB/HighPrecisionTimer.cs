using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace rotoUSB
{
    // High Precision Timer  from  Window Multimedia timer
    // precision can achieve 1ms 
    public class HighPrecisionTimer
    {
        private delegate void TimerCallback(uint uTimerID, uint uMsg, UIntPtr dwUser, UIntPtr dw1, UIntPtr dw2);

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern uint timeSetEvent(uint uDelay, uint uResolution, TimerCallback lpTimeProc, UIntPtr dwUser, uint fuEvent);

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern void timeKillEvent(uint uTimerID);

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern uint timeBeginPeriod(uint uPeriod);

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern uint timeEndPeriod(uint uPeriod);

        private TimerCallback _callback;
        private uint _timerId;

        // Start a default timer with 10ms interval
        public void Start(Action action, int intervalMs = 10)
        {
            timeBeginPeriod(1);
            // Request 1ms resolution

            _callback = (uTimerID, uMsg, dwUser, dw1, dw2) => action();

            _timerId = timeSetEvent((uint)intervalMs, 0, _callback, UIntPtr.Zero, 1);

        }

        public void Stop()
        {
            timeKillEvent(_timerId);
            timeEndPeriod(1);
        }
    }
}
