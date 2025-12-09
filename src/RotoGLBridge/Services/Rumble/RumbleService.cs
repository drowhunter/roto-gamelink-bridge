using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Sharpie.Helpers.Core;
using Sharpie.Helpers.Core.Extensions;

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

    public class RumbleService : IRumbleService
    {

        public int RUMBLE_DURATION_MS = 100; // milliseconds

        CancellationTokenSource _cts;

        static object _lock = new object();

        ConcurrentQueue<Rumble> RumbleQueue = new ConcurrentQueue<Rumble>();
        
        public event Action<(int power, int durationMs)> RumbleEvent;


        public RumbleService()
        {
           
        }

        public void Rumble(int amplitude, int frequencyMs)
        {
            var rumble = new Rumble(amplitude, frequencyMs);

            lock (_lock)
                RumbleQueue.Enqueue(rumble);

        }

        public Task Start()
        {
            _cts = new CancellationTokenSource();

            return Task.Run(async () =>
            {
                Thread.CurrentThread.IsBackground = true;
                Thread.CurrentThread.Name = "Rumble Service Thread";

                while (!_cts.Token.IsCancellationRequested)
                {
                    var toProcess = new List<Rumble>();
                    lock (_lock)
                    {

                        while (RumbleQueue.TryDequeue(out var rumble))
                        {
                            if(rumble.Power > 0)
                                toProcess.Add(rumble);
                        }
                    }
                    if (toProcess.Count > 0)
                    {
                        var (r, delay) = ProcessRumble(toProcess, RUMBLE_DURATION_MS);

                        RumbleEvent?.Invoke((r.Power, r.Speed));

                        await Task.Delay(delay);
                    }
                }
            }, _cts.Token);
        }

        private (Rumble rumble, int delay) ProcessRumble(List<Rumble> rumbles, int duration)
        {
            var p = rumbles.Select(r => r.Power).RootMeanSquared();

            var s = (float)rumbles.Select(r => r.Speed).RootMeanSquared();



            //  convert rumblePeriod into duration to pass to a pwm rumble motor

            // _logger.LogDebug("Rumble Processed: Power={0}, Speed={1}", rumblePower, s);

            int delay = (int) ( duration * ((100 - s) / 100));  // (int)Filters.EnsureMapRange(s, 0, 254, 0, duration);


            return (new Rumble((int)p, duration), duration + 0);


        }

        private double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public void Stop()
        {
            _cts.Cancel();
        }

    }
}
