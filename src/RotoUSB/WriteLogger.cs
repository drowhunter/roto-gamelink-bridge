using Microsoft.Extensions.Logging;

namespace rotoUSB
{

    public class WriteLogger<T> : IWriteLogger<T>, IDisposable
        where T : class
    {
#if !DEBUG
        public bool _isConsoleDebug = false;
#else
        public bool _isConsoleDebug = false;
#endif
        public WriteLogger(ILogger<T> logger)
        {
            _logger = logger;
        }

        //private StreamWriter logger;
        private readonly ILogger<T> _logger;

        public void Dispose()
        {
            //((IDisposable)logger).Dispose();
            _isConsoleDebug = false;
        }

        public void EnableConsoleDebug(bool isEnabled = true)
        {
            _isConsoleDebug = isEnabled;

        }

        public void WriteLog(string message)
        {
            if (_isConsoleDebug)
            {
                //Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + message);


               // if (logger == null)
                 //   logger = new StreamWriter(DateTime.Now.ToString("yyyy-MM-dd ") + "log.txt", append: true);
                _logger.LogDebug(message);
                //logger.Flush();


            }
        }
    }
}
