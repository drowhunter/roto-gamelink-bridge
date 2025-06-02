using System.Collections;

namespace Sharpie.Engine.Contracts.Tools
{
    public class Indexer<T> : Indexer<T, int>  where T : notnull
    {
        public Indexer(Func<int, T> initializer) : base(initializer)
        {
        }
    }

    public class Indexer2D<T> : Indexer2D<T, int, string> where T : notnull
    {
        public Indexer2D(Func<int, T> initializerOne, Func<string, int, T> initializerTwo) : base(initializerOne, initializerTwo)
        {
        }
    }


    public abstract class Indexer<T, TIndex>(Func<TIndex, T> initializer) : IEnumerable<T>
           where TIndex : notnull
    {
        private Dictionary<TIndex, T> items = new();

        public T this[TIndex index]
        {
            get
            {
                if (!items.TryGetValue(index, out var value))
                {
                    value = initializer(index);
                    items[index] = value;
                }
                return value;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return items.Values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public abstract class Indexer2D<T, TIndexOne, TIndexTwo> : IEnumerable<KeyValuePair<TIndexTwo, KeyValuePair<TIndexOne, T>>>
       where T : notnull
       where TIndexOne : notnull
       where TIndexTwo : notnull
    {
        private readonly Func<TIndexTwo, TIndexOne, T> _2DInitializer;
        private readonly Dictionary<TIndexTwo, Dictionary<TIndexOne, T>> items2;

        public Indexer2D(
            Func<TIndexOne, T> initializerOne,
            Func<TIndexTwo, TIndexOne, T> initializerTwo
        )
        {
            _2DInitializer = initializerTwo;
            items2 = new Dictionary<TIndexTwo, Dictionary<TIndexOne, T>>();
        }

        public T this[TIndexTwo key, TIndexOne index = default!]
        {
            get
            {
                if (!items2.TryGetValue(key, out var theKey))
                {
                    theKey = new Dictionary<TIndexOne, T>();
                    items2[key] = theKey;
                }

                if (!theKey.TryGetValue(index, out var item))
                {
                    item = _2DInitializer(key, index);
                    theKey[index] = item;
                }

                return item;
            }
        }

        public IEnumerator<KeyValuePair<TIndexTwo, KeyValuePair<TIndexOne, T>>> GetEnumerator()
        {
            foreach (var outerPair in items2)
            {
                foreach (var innerPair in outerPair.Value)
                {
                    yield return new KeyValuePair<TIndexTwo, KeyValuePair<TIndexOne, T>>(
                        outerPair.Key,
                        new KeyValuePair<TIndexOne, T>(innerPair.Key, innerPair.Value)
                    );
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

}
