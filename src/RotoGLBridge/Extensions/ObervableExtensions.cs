using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RotoGLBridge.Extensions
{
    internal static class ObervableExtensions
    {
        public static IObservable<T> DelayBetween<T>(this IObservable<T> source, TimeSpan delay)
            => source.Zip(Observable.Interval(delay), (item, _) => item);

        
        public static IObservable<(T previous, T current)> Pairwise<T>(this IObservable<T> source)
        {
            return source.Scan(
                (previous: default(T), current: default(T)),
                (acc, current) => (previous: acc.current, current: current)
            )
            .Skip(1);// Skip the first emission which has default previous value
            
             
        }

        

    }
}
