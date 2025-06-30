using System.Collections;
using System.Collections.Concurrent;

namespace RotoGLBridge.Services
{
    public interface IConsoleWatcher
    {
        //void Write(int left, int top, string text, ConsoleColor? color = null);
        int Columns { get; set; }

        void Watch(string key, object value);

        void Watch(Dictionary<string, object> watch);

        void Publish();
    }
    public class ConsoleWatcher : IConsoleWatcher
    {
        private static object _lock = new object();

        public int Columns { get; set; } = 4;

        ConcurrentDictionary<string, object> watch = new();

        
        private void Write(int left, int top, string text, ConsoleColor? color = null)
        {
            lock (_lock)
            {
                var originalColor = Console.ForegroundColor;
                var originalBackgroundColor = Console.BackgroundColor;

                if (color.HasValue)
                {
                    Console.ForegroundColor = color.Value;
                }

                
                var (l, t) = Console.GetCursorPosition();

                Console.SetCursorPosition(left, top);
                Console.Write(text);
                Console.SetCursorPosition(l, t);

                Console.ForegroundColor = originalColor;
            }

        }

        public void Watch(Dictionary<string, object> watch)
        {
            foreach (var (key, value) in watch)
            {
                Watch(key, value);
            }
        }

        public void Watch(string key, object value)
        {
            //watch.Clear();

            if (!watch.ContainsKey(key))
            {
                watch.TryAdd(key, value);
            }
            else
            {
                watch[key] = value; // Update existing key
            }
        }

        public void Publish()
        {
            int maxKeyLen = watch.Keys.Max(k => k.Length) + 10;

            int i = 0;
            int j = 0;



            foreach (var (k, v) in watch)
            {
                var c = j % (maxKeyLen * Columns);
                if (c == 0)
                    i += 2;

                string paddedKey = (k + ": " + v).PadRight(maxKeyLen+1);
                Write(c, i + 4, paddedKey);

                j += maxKeyLen;
            }
        }
    }
}
