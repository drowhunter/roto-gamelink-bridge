using RotoGLBridge.Services;

using rotoUSB;

using Sharpie.Engine.Contracts.Plugins;
using Sharpie.Helpers.Core;

using System.Data;

namespace RotoGLBridge.Plugins
{
    [GlobalType(Type = typeof(Roto2PluginGlobal))]
    public class Roto2Plugin(
        ILogger<RotoPlugin> logger,
        IEnumerable<IMmfSender> mmfSenders,
        RotoChair roto
        ) : UpdateablePlugin
    {
    
    }

    public class Roto2PluginGlobal : UpdateablePluginGlobal<Roto2Plugin>
    {
        

    }
}
