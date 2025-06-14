using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Logging.Console;

using RotoGLBridge.ConsoleApp;
using RotoGLBridge.Plugins;
using RotoGLBridge.Scripts;
using RotoGLBridge.Services;


var runargs = ArgumentParser<RunArgs>.Parse(args);

if (runargs == null)
    return;


CancellationTokenSource cts = new();



//handle unhandled exceptions
AppDomain.CurrentDomain.UnhandledException += (s, e) =>
{
    //LogLine("Unhandled Exception");
    Console.WriteLine(((Exception)e.ExceptionObject).Message);
    cts.Cancel();
};


var serviceProvider = CreateServices();

 serviceProvider.GetRequiredService<App>().Run(cts.Token);

await WaitForCtrlQ(cts);


//Console.ReadLine();
Environment.Exit(0);

ServiceProvider CreateServices()
{
    var services = new ServiceCollection()
        .AddLogging(b =>
        {
            b.AddFilter("Microsoft", LogLevel.Warning)
             .AddFilter("System", LogLevel.Warning)
             //.AddFilter("Sharpie", LogLevel.Debug)
             .AddFilter("RotoGLBridge", LogLevel.Debug);
        })

        //.AddLogging(builder =>
        //                builder.AddConsole()
        //                        .AddFilter(level => level >= LogLevel.Information)
        //                       // .AddFilter<ConsoleLoggerProvider>(level => level >= LogLevel.Debug)
        //                        .AddFilter("Sharpie.Engine", l => l >= LogLevel.Warning)
        //                        )
        .AddSingleton<App>()
        
        .AddSingleton(sp => runargs);

    

    services.AddRotoGLBridge();

    return services.BuildServiceProvider();
}

Task WaitForCtrlQ(CancellationTokenSource cts)
{
    Console.WriteLine("Press Ctrl+Q to exit...");
    Console.CancelKeyPress += async (s, e) =>
    {
        e.Cancel = true;
        await cts.CancelAsync();
    };

    return Task.Run(async () =>
    {     
        while (!cts.IsCancellationRequested)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(intercept: true);
                if (key.Modifiers.HasFlag(ConsoleModifiers.Control) && key.Key == ConsoleKey.Q)
                {
                    cts.Cancel();
                    break;
                }
            }
            Thread.Sleep(1000);
        }

        await cts.CancelAsync();
       
    });
}