using RotoGLBridge.Configuration;
using RotoGLBridge.Models;
using RotoGLBridge.Plugins;
using RotoGLBridge.Plugins.YawEmu;
using RotoGLBridge.Scripts;
using RotoGLBridge.Services;

using Sharpie.Extras.Telemetry;

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
            .Build();
            builder.Services.AddSingleton<TcpCommandFactory>();
            builder.Services.AddTransient<IByteConvertor<YawGLData>, YawGLByteConverter>();
            builder.Services.AddSingleton<IConsoleWatcher, ConsoleWatcher>();

            return builder;
        }
    }
}
