using Microsoft.Extensions.DependencyInjection;

namespace RotoGLBridge.Configuration
{
    public class RotoGLBuilder
    {
        public IServiceCollection Services { get; }

        public RotoGLBuilder(IServiceCollection services)
        {
            Services = services;
        }



    }
}
