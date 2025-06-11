namespace RotoGLBridge.Services
{
    public interface IConsoleWatcher
    {
        void Write(int left, int top, string text, ConsoleColor? color = null);
    }
    public class ConsoleWatcher : IConsoleWatcher
    {
        private static object _lock = new object();

        public void Write(int left, int top, string text, ConsoleColor? color = null)
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
    }
}
