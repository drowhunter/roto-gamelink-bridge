using System.Diagnostics;

namespace RotoGLBridge.Services
{
    public class RumbleService2 : IRumbleService
    {    
        AmpTime? _lastAmpTime;
        PowDur? _lastPowDur;

        public int ThresholdMs { get; set; } = 100;

        public event Action<(int power, int durationMs)> RumbleEvent;

        public void Rumble(int amplitude, int frequencyMs = 1)
        {
            if (_lastAmpTime == null)
            {
                var now = DateTime.Now;
                var durationMs = (int)(now - _lastAmpTime.Value.time).TotalMilliseconds;
                var powDur = new PowDur
                {
                    power = (int)_lastAmpTime.Value.amplitude,
                    durationMs = durationMs
                };
                
                if (_lastPowDur != null)
                {
                    var newDur = new PowDur
                    {
                        power = _lastPowDur.Value.power,
                        durationMs = _lastPowDur.Value.durationMs + (powDur.durationMs > 1000 ? 100 : powDur.durationMs)
                    };
                    

                    if (newDur.durationMs < ThresholdMs)
                    {
                        // not enough time accumulated yet
                        _lastPowDur = newDur;
                        return;
                    }
                    else
                    {
                        // emit
                        RumbleEvent?.Invoke((newDur.power, newDur.durationMs));
                        _lastPowDur = new PowDur { durationMs = 0, power = newDur.power };
                    }
                }
                else
                {
                    _lastPowDur = powDur;
                }

                _lastAmpTime = new AmpTime(amplitude);
            }
        }

        public async Task Start()
        {
            //throw new NotImplementedException();
        }

        public void Stop()
        {
            //throw new NotImplementedException();
        }
    }

}
