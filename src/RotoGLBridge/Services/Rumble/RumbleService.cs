using RotoGLBridge.Extensions;

using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace RotoGLBridge.Services
{

    internal struct Rumble
    {
        public readonly int Power;

        public readonly int Speed;

        public readonly DateTime Time;


        public Rumble(int amp, int freq)
        {
            Power = amp;
            Speed = freq;
            Time = DateTime.Now;
        }
    }

    public class RumbleService : IRumbleService, IDisposable
    {
        public event Action<(int power, int durationMs)> RumbleEvent;


        public int RUMBLE_DURATION_MS = 100; // milliseconds

        private Subject<int> _amplitudeSubject = new();


        private IObservable<AmpTime> O_AmplitudeTime => _amplitudeSubject.Select(_ => new AmpTime { amplitude = _, time = DateTime.Now });

        private IObservable<PowDur> O_PowerDuration => O_AmplitudeTime
            .Pairwise()
            .Select(_ => new PowDur((int)_.previous.amplitude, (int)(_.current.time - _.previous.time).TotalMilliseconds));            



        /// <summary>
        /// Emits <c>PowDur</c> items where consecutive power durations are accumulated until
        /// the configured <see cref="RUMBLE_DURATION_MS"/> threshold is met or exceeded.
        /// Once the threshold is reached, the accumulated duration is emitted with the latest power value,
        /// and the accumulator is reset.
        /// </summary>
        private IObservable<PowDur> O_StackedPowerDuration => O_PowerDuration
            .Scan((sum:0, emit: (PowDur?)null), (acc,curr) =>
            {
                var newSum = acc.sum + curr.durationMs;

                if (newSum >= RUMBLE_DURATION_MS)
                {
                    var emit = new PowDur(curr.power, newSum);
                    return (0, emit);
                }
                
                return (newSum, null);
                
            })
            .Where(_ => _.emit != null).Select(_ => _.emit!.Value);



        public void Rumble(int amplitudePercent, int hzPercent)
            =>  _amplitudeSubject.OnNext(amplitudePercent);

        public Task Start()
        {
            O_StackedPowerDuration.Subscribe(pd => RumbleEvent?.Invoke((pd.power, pd.durationMs)));

            return Task.CompletedTask;
        }

        

        public void Stop()
        {
            _amplitudeSubject.OnCompleted();
        }

        public void Dispose()
        {
            ((IDisposable)_amplitudeSubject).Dispose();
        }
    }
}
