
using RotoGLBridge.Configuration;
using RotoGLBridge.Models;
using RotoGLBridge.Plugins;
using RotoGLBridge.Plugins.GameLink;
using RotoGLBridge.Scripts;
using RotoGLBridge.Services;

using Sharpie.Helpers.Core.Lerping;
using Sharpie.Helpers.Telemetry;
using Sharpie.Plugins.SharpDX;
using Sharpie.Plugins.Speech;
using Sharpie.Plugins.UsbWatcher;

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
            //.AddPluginsFrom<GamelinkPlugin>()
            .AddPlugin<GamelinkPlugin>()
            .AddPlugin<YawDevicePlugin>()
            .AddPlugin<Roto2Plugin>()
            .AddPlugin<OxrmcPlugin>()
            //.AddScriptsFrom<RotoMCSender>()
            //.AddScript<Main>()
            .AddScript<RotoScript>()
            //.AddScript<JoystickTest>()
            //.AddScript<UsbWatcherTest>()
            .AddPlugin<SpeechPlugin>()
            .AddPlugin<Xbox360Plugin>()
            .AddPlugin<UsbWatcherPlugin>()
            .Build();


            builder.Services.AddRotoUsb();

            //builder.Services.AddSingleton<TcpCommandFactory>();
            builder.Services.AddTransient<IByteConverter<YawGLData>, YawGLByteConverter>();
            builder.Services.AddTransient<IConsoleWatcher, ConsoleWatcher>();
            
            //builder.Services.
            builder.Services.AddRotoServices();
           
            return builder;
        }

        private static void AddRotoServices(this IServiceCollection services) 
        {
            //services.AddSingleton<IUsbConnector, UsbConnector>();

            //services.AddTransient<IUsbWatcher, UsbWatcher>();
            services.AddTransient<ILerper, Lerper>();
            services.AddTransient<Stopwatch>();
            services.AddTransient<IRumbleService, RumbleService>();
            //services.AddSingleton<Roto>();
            services.AddTransient<MathService>();
            services.AddSingleton<IFollowTargetCalculator, FollowTargetCalculator>();

            //services.AddTransient<IMmfSender, RotoMCSender>();
            services.AddTransient<IMmfSender, FlyPtSender>();
        }
    }
}
