using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RotoGLBridge.Extensions
{
    internal static class ObervableExtensions
    {
        /// <summary>
        /// Introduces a minimum delay between consecutive emissions from the source observable sequence.
        /// </summary>
        /// <typeparam name="T">The type of the values in the source sequence.</typeparam>
        /// <param name="source">The source observable sequence to pace.</param>
        /// <param name="delay">The minimum time interval to enforce between consecutive emissions.</param>
        /// <returns>
        /// An observable sequence that emits the same values as the source, but with at least the specified
        /// delay between each emission.
        /// </returns>
        /// <remarks>
        /// This method uses <see cref="Observable.Zip"/> combined with <see cref="Observable.Interval"/> to pace
        /// the emissions from the source observable. Each value from the source is paired with a tick from an
        /// interval timer, ensuring that emissions occur no faster than the specified delay interval.
        /// <para>
        /// The first emission occurs after the initial delay period. Subsequent emissions are released as they arrive
        /// from the source, but never sooner than the specified delay after the previous emission.
        /// </para>
        /// <para>
        /// This operator is useful for rate-limiting, throttling data streams, or spacing out operations
        /// that should not occur too rapidly (e.g., API calls, UI updates, or hardware commands).
        /// </para>
        /// <para>
        /// Note: If the source emits faster than the delay period, values will be buffered. If the source
        /// emits slower than the delay, values pass through with their natural timing plus the delay offset.
        /// </para>
        /// </remarks>
        public static IObservable<T> DelayBetween<T>(this IObservable<T> source, TimeSpan delay)
            => source.Zip(Observable.Interval(delay), (item, _) => item);


        /// <summary>
        /// Emits tuples containing consecutive pairs of values from the source observable sequence.
        /// </summary>
        /// <typeparam name="T">The type of the values in the source sequence.</typeparam>
        /// <param name="source">The source observable sequence to pair.</param>
        /// <returns>
        /// An observable sequence that emits tuples of (previous, current) values, where each tuple contains
        /// the previous value and the current value from consecutive emissions of the source sequence.
        /// </returns>
        /// <remarks>
        /// This method uses <see cref="Observable.Scan"/> to maintain state of consecutive values as they arrive.
        /// Each emission contains both the previous and current value, allowing subscribers to compare or
        /// compute differences between consecutive values.
        /// <para>
        /// The first value from the source is skipped because it has no previous value to pair with.
        /// Emissions begin with the second value from the source, where the first value becomes the "previous"
        /// and the second becomes the "current".
        /// </para>
        /// <para>
        /// This operator is useful for detecting changes, computing deltas between consecutive values,
        /// or implementing logic that depends on both the current and previous state.
        /// </para>
        /// </remarks>
        public static IObservable<(T previous, T current)> Pairwise<T>(this IObservable<T> source)
        {
            return source.Scan(
                (previous: default(T), current: default(T)),
                (acc, current) => (previous: acc.current, current: current)
            )
            .Skip(1);// Skip the first emission which has default previous value


        }


        /// <summary>
        /// Applies a first-order low-pass filter (RC filter) to the source observable sequence to smooth noisy signals.
        /// </summary>
        /// <typeparam name="T">The numeric type of the values. Must implement <see cref="INumber{T}"/>.</typeparam>
        /// <param name="source">The source observable sequence to filter.</param>
        /// <param name="cutoffHz">The cutoff frequency in Hertz. Frequencies below this value pass through with minimal attenuation, while higher frequencies are attenuated.</param>
        /// <param name="sampleRateHz">The sample rate in Hertz (samples per second) of the incoming signal.</param>
        /// <returns>
        /// An observable sequence that emits filtered values smoothed using a first-order RC low-pass filter.
        /// </returns>
        /// <remarks>
        /// This method implements a discrete-time approximation of an analog RC (resistor-capacitor) low-pass filter.
        /// The filter uses the recursive formula: <c>output[n] = output[n-1] + alpha * (input[n] - output[n-1])</c>,
        /// where <c>alpha = dt / (rc + dt)</c>, <c>dt = 1 / sampleRate</c>, and <c>rc = 1 / (2 * π * cutoffFrequency)</c>.
        /// <para>
        /// The alpha coefficient determines the smoothing strength: values closer to 0 produce more smoothing (slower response),
        /// while values closer to 1 produce less smoothing (faster response to changes).
        /// </para>
        /// <para>
        /// Low-pass filters are commonly used to reduce noise and smooth sensor data, motion tracking values,
        /// or other signals where high-frequency variations should be attenuated.
        /// </para>
        /// <para>
        /// The first emitted value will be equal to the first input value, as there is no previous state to smooth with.
        /// </para>
        /// </remarks>
        public static IObservable<T> LowPassFilter<T>(this IObservable<T> source, T cutoffHz, T sampleRateHz) where T : INumber<T>
        {
            double dt = 1.0 / double.CreateChecked(sampleRateHz);
            double rc = 1.0 / (2 * Math.PI * double.CreateChecked(cutoffHz));
            double alpha = dt / (rc + dt);

            return source.Scan(
                (prev, current) =>
                {
                    var alphaTerm = T.CreateChecked(alpha) * (current - prev);
                    return prev + alphaTerm;
                }
            );
        }

       
        /// <summary>
        /// Applies a Finite Impulse Response (FIR) filter to the source observable sequence using the specified filter coefficients (taps).
        /// </summary>
        /// <typeparam name="T">The numeric type of the values. Must implement <see cref="INumber{T}"/>.</typeparam>
        /// <param name="source">The source observable sequence to filter.</param>
        /// <param name="taps">The FIR filter coefficients. The number of taps determines the filter order.</param>
        /// <returns>
        /// An observable sequence that emits filtered values computed as the weighted sum (convolution) of
        /// the current and past samples with the corresponding filter taps.
        /// </returns>
        /// <remarks>
        /// This method implements a standard FIR (Finite Impulse Response) filter by maintaining a sliding window
        /// of the most recent N samples, where N is the number of taps. For each emission, it computes the output as:
        /// <c>output = sample[0] * taps[0] + sample[1] * taps[1] + ... + sample[N-1] * taps[N-1]</c>
        /// <para>
        /// No values are emitted until the window is filled with N samples from the source.
        /// Each subsequent emission applies the filter coefficients to the current window of samples.
        /// </para>
        /// <para>
        /// FIR filters are commonly used for smoothing, low-pass, high-pass, band-pass filtering,
        /// and other signal processing operations on data streams.
        /// </para>
        /// </remarks>
        public static IObservable<T> FirFilter<T>(this IObservable<T> source, IReadOnlyList<T> taps) where T : INumber<T>
        {
            int n = taps.Count;

            return source
                .Scan(new Queue<T>(n), (queue, sample) =>
                {
                    if (queue.Count == n)
                        queue.Dequeue();

                    queue.Enqueue(sample);
                    return queue;
                })
                .Where(q => q.Count == n)
                .Select(q =>
                {
                    T acc = T.Zero;
                    int i = 0;
                    foreach (var sample in q)
                        acc += sample * taps[i++];

                    return acc;
                });
        }

        /// <summary>
        /// Interpolates values from the source observable to emit at a fixed target rate using linear interpolation (LERP).
        /// </summary>
        /// <typeparam name="T">The numeric type of the values. Must implement <see cref="INumber{T}"/>.</typeparam>
        /// <param name="source">The source observable sequence to interpolate.</param>
        /// <param name="targetRateHz">The target emission rate in Hertz (samples per second).</param>
        /// <returns>
        /// An observable sequence that emits interpolated values at the specified target rate,
        /// calculated by linearly interpolating between the two most recent values from the source.
        /// </returns>
        /// <remarks>
        /// This method subscribes to the source observable and tracks the two most recent values along with their timestamps.
        /// A timer runs at the target rate and emits interpolated values based on the current time position between
        /// the previous and last received samples. The interpolation factor is clamped between 0.0 and 1.0.
        /// <para>
        /// No values are emitted until at least two samples have been received from the source.
        /// If the source emits slower than the target rate, values will be extrapolated up to the last sample.
        /// </para>
        /// </remarks>
        public static IObservable<T> LerpToRate<T>(this IObservable<T> source, double targetRateHz)
        where T : struct, INumber<T>
        {
            var period = TimeSpan.FromSeconds(1.0 / targetRateHz);

            return Observable.Create<T>(observer =>
            {
                T? prev = default;
                T? last = default;
                DateTimeOffset prevTime = default;
                DateTimeOffset lastTime = default;

                // Track incoming samples with timestamps
                var sourceSub = source.Timestamp().Subscribe(sample =>
                {
                    prev = last;
                    prevTime = lastTime;

                    last = sample.Value;
                    lastTime = sample.Timestamp;
                });

                // High-frequency timer that performs interpolation
                var timerSub = Observable.Interval(period).Subscribe(_ =>
                {
                    if (prev is null || last is null)
                        return;

                    // Unwrap nullable values after null check
                    T prevValue = prev.Value;
                    T lastValue = last.Value;

                    var now = DateTimeOffset.UtcNow;

                    double total = (lastTime - prevTime).TotalSeconds;
                    if (total <= 0)
                        return;

                    double t = (now - prevTime).TotalSeconds / total;
                    t = Math.Clamp(t, 0.0, 1.0);

                    // Convert t to T
                    T tt = T.CreateChecked(t);

                    // LERP: prev + (last - prev) * t
                    T value = prevValue + (lastValue - prevValue) * tt;

                    observer.OnNext(value);
                });

                return new CompositeDisposable(sourceSub, timerSub);
            });
        }

    }
}
