using Sharpie.Engine.Contracts.Plugins;

namespace Sharpie.Engine
{
    /// <summary>  
    /// Applicate State  
    /// </summary>  
    public class Warehouse
    {
        public static readonly SemaphoreSlim _lock = new(1, 1);

        private readonly HashSet<ISharpiePlugin> _activePlugins = new();

        public IReadOnlyCollection<ISharpiePlugin> ActivePlugins => _activePlugins;


        //public Warehouse(IEnumerable<ISharpieScript> scripts)
        //{
            
        //}

        public void AddPlugin(ISharpiePlugin plugin)
        {
            _lock.Wait();
            try
            {
                _activePlugins.Add(plugin);
            }
            finally
            {
                _lock.Release();
            }
        }

        public void RemovePlugin(ISharpiePlugin plugin)
        {
            _lock.Wait();
            try
            {
                _activePlugins.Remove(plugin);
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}
