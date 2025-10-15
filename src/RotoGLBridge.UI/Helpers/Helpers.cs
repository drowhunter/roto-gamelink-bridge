using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RotoGLBridge.UI.Helpers
{
    internal static class Helpers
    {
        public static IServiceCollection AddView<T1,T2>(this IServiceCollection services)
            where T1 : class
            where T2 : class
        {
            services.AddTransient<T1>();
            services.AddTransient<T2>();
            return services;
        }
    }
}
