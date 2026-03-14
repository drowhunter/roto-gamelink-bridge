using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using rotoUSB;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class Registration
    {
        public static IServiceCollection AddRotoUSB(this IServiceCollection services)
        {
            services.AddSingleton<IRotoChair, RotoChair>();
            services.AddSingleton<IRotoActionStruct, RotoActionStruct>();
            services.AddTransient<IUSBNative, USBNative>(); 
            services.AddTransient(typeof(IWriteLogger<>), typeof(WriteLogger<>));
            return services;
        }
    }
}
