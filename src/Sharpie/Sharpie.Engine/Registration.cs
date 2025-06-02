using Microsoft.Extensions.Configuration;
using Sharpie.Engine.Configuration;
using Sharpie.Engine.Contracts.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class RegistrationExtensions
    {
        public static SharpieEngineBuilder AddSharpieEngine(this IServiceCollection services, Action<SharpieEngineSettings> setup = null)
        {
            var config = AddConfiguration(out var settings);
            setup?.Invoke(settings);

            
            var builder = new SharpieEngineBuilder(services, config);

            builder.Services.AddSingleton(settings);



            return builder.AddSharpieEngineCore();
        }

        private static SharpieEngineBuilder AddSharpieEngineCore(this SharpieEngineBuilder builder)
        {
            builder.Services.AddSingleton<Warehouse>();
            // Register the engine
            builder.Services.AddSingleton<ISharpieEngine, SharpieEngine>();        
        

            return builder;
        }


        private static IConfiguration AddConfiguration(out SharpieEngineSettings settings )
        {
            var config = new ConfigurationBuilder()
                     .AddJsonFile("sharpie.json", optional: false, reloadOnChange: true)
                     .Build();

            var section = config.GetSection("Sharpie.Engine");
            settings = new SharpieEngineSettings();

            section.Bind(settings);

            return config;
        }
    }
}