using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Sharpie.Engine.Configuration
{
    public partial class SharpieEngineBuilder 
    {
    

        public IServiceCollection Services { get; init; }
        public IConfiguration Configuration { get; init; }

        public SharpieEngineBuilder(IServiceCollection services, IConfiguration configuration = null)
        {
            Services = services;
            Configuration = configuration;
        }


        
    }
}
