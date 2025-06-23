using com.rotovr.sdk;

using RotoGLBridge.Configuration;
using RotoGLBridge.Models;
using RotoGLBridge.Plugins;
using RotoGLBridge.Plugins.GameLink;
using RotoGLBridge.Scripts;
using RotoGLBridge.Services;

using Sharpie.Helpers.Core.Lerping;
using Sharpie.Helpers.Telemetry;
using Sharpie.Plugins.Speech;

using System.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class RegistrationExtensions
    {
        public static RotoGLBuilder AddRotoGLBridge(this IServiceCollection services, Action<RotoGLBridgeSettings> setup = null)
        {
            var builder = new RotoGLBuilder(services);
            //var config = new SharpieEngineConfiguration();

            var settings = new RotoGLBridgeSettings();
            setup?.Invoke(settings);

            builder.Services.AddSingleton(settings);

            return builder.AddServices();
        }

        private static RotoGLBuilder AddServices(this RotoGLBuilder builder)
        {
            //builder.Services.AddSingleton<Warehouse>();
            // Register the engine
            //builder.Services.AddSingleton<ISharpieEngine, SharpieEngine>();
            builder.Services.AddSharpieEngine(setup =>
            {
                setup.EnginePollInterval = (1000 / 90); // 90 FPS
            })
            .AddPluginsFrom<GamelinkPlugin>()
            .AddScriptsFrom<Main>()
            .AddPlugin<SpeechPlugin>()
            .Build();


            //builder.Services.AddSingleton<TcpCommandFactory>();
            builder.Services.AddTransient<IByteConverter<YawGLData>, YawGLByteConverter>();
            builder.Services.AddSingleton<IConsoleWatcher, ConsoleWatcher>();
            //builder.Services.
            builder.Services.AddRotoServices();
           
            return builder;
        }

        private static void AddRotoServices(this IServiceCollection services) 
        {
            services.AddSingleton<IUsbConnector, UsbConnector>();

            services.AddTransient<IUsbWatcher, UsbWatcher>();
            services.AddTransient<ILerper, Lerper>();
            services.AddTransient<Stopwatch>();
            services.AddSingleton<Roto>();
            //services.AddTransient<IMmfSender, RotoMCSender>();
            services.AddTransient<IMmfSender, FlyPtSender>();
        }
    }
}
