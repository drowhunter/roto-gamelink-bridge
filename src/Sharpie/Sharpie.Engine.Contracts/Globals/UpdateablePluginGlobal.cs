using Sharpie.Engine.Contracts.Plugins;

namespace Sharpie.Engine.Contracts
{
    public interface IUpdateableGlobal<TPlugin> : ISharpieGlobal<TPlugin> 
        where TPlugin : ISharpiePlugin
    {
        /// <summary>
        /// event for external code to sub to
        /// </summary>
        Action OnUpdate { get; set; }
    }

    public abstract class UpdateablePluginGlobal<TPlugin> : SharpieGlobal<TPlugin>, IUpdateableGlobal<TPlugin>
        where TPlugin : ISharpiePlugin
    {
        public Action OnUpdate { get; set; } = () => { };
    }


}
