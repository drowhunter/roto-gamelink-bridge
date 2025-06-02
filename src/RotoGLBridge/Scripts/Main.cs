using Microsoft.Extensions.Logging;

namespace RotoGLBridge.Scripts
{
    public class Main (ILogger<Main> logger) : SharpieScript
    {
        public override Task Start()
        {
            logger.LogInformation($"Main script started.");
            return Task.CompletedTask;
        }
        public override Task Stop()
        {
            logger.LogInformation($"Main script stopped.");
            return Task.CompletedTask;
        }
    }    
}
