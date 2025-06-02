using Sharpie.Engine.Contracts.Plugins;

namespace Sharpie.Engine.Contracts
{

    // Update the interface to allow nullable TPlugin
    public interface ISharpieGlobal<TPlugin>  where TPlugin : notnull, ISharpiePlugin
    {
        /// <summary>
        /// The plugin that this global belongs to.
        /// </summary>
        TPlugin plugin { get; set; }

        string Name { get; }
    }
   

    public abstract class SharpieGlobal<TPlugin> : ISharpieGlobal<TPlugin> 
        where TPlugin : notnull, ISharpiePlugin

    {
        public required TPlugin plugin { get; set; }


        public string Name => GetType().Name;

        
    }

}
