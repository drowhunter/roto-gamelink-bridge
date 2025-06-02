namespace RotoGLBridge.ConsoleApp
{
    internal class App(RunArgs args, ILogger<App> logger, ISharpieEngine engine)
    {
        public void Run(CancellationToken cancellationToken)
        {
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(args));

            engine.OnStarted += (s, e) => logger.LogInformation("Engine started successfully.");
            engine.OnStopped += (s, e) => logger.LogInformation("Engine stopped successfully.");

            engine.Start(cancellationToken);


            if (!engine.IsRunning)
            {
                logger.LogError("Error starting engine");
            }
            // return Task.CompletedTask;
        }
    }
}
