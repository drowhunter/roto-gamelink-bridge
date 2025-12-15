using System.Diagnostics;

namespace RotoGLBridge.Services
{
    [DebuggerDisplay("{ToString()}")]
    internal struct AmpTime
    {
        public float amplitude;
        public DateTime time;

        public AmpTime(float amp)
        {
            amplitude = amp;
            time = DateTime.Now;
        }

        public override string ToString()
        {
            return $"Amplitude: {amplitude}, Time: {time:mm.ss.fff}";
        }
    }

    [DebuggerDisplay("{ToString()}")]
    internal struct PowDur
    {
        public int durationMs;
        public int power;

        public PowDur(int p, int d)
        {
            power = p;
            durationMs = d;
        }

        public override string ToString()
        {
            return $"Power: {power}, DurationMs: {durationMs}";
        }
    }
}
