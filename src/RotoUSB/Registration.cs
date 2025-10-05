using rotoUSB;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class RegistrationExtensions
    {
        public static IServiceCollection AddRotoUsb(this IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            services.AddSingleton<IUSBNative, USBNative>();
            services.AddTransient<IHighPrecisionTimer, HighPrecisionTimer>();

            services.AddSingleton<IRotoChair, RotoChair>();
            services.AddTransient(typeof(IWriteLogger<>), typeof(WriteLogger<>));
            services.AddTransient<IRotoActionStruct, RotoActionStruct>();
            return services;
        }
    }
}
