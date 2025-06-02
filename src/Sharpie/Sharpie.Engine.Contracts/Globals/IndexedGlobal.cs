using Sharpie.Engine.Contracts.Plugins;
using Sharpie.Engine.Contracts.Tools;

namespace Sharpie.Engine.Contracts
{
    public interface IIndexedGlobal<TPlugin, TGlobal> : ISharpieGlobal<TPlugin>
        where TPlugin : ISharpiePlugin
        where TGlobal : ISharpieGlobal<TPlugin>
    {
        Indexer<TGlobal>? Index { get; set; }
    }

    public abstract class IndexedGlobal<TPlugin, TGlobal> : IIndexedGlobal<TPlugin,TGlobal >, IUpdateableGlobal<TPlugin>
        where TGlobal : ISharpieGlobal<TPlugin>
        where TPlugin : ISharpiePlugin
    {
        

        public string Name => GetType().Name;

        TPlugin? ISharpieGlobal<TPlugin>.plugin { get; set; }

        
        Indexer<TGlobal>? IIndexedGlobal<TPlugin, TGlobal>.Index { get; set; }
        
        public Action OnUpdate { get; set; } = () => { };


        protected IndexedGlobal()
        {
            if(this is IIndexedGlobal<TPlugin, TGlobal> indexedGlobal)
            {
                indexedGlobal.Index = new Indexer<TGlobal>(Initialize);
            }           
        }

        

        protected TGlobal Initialize(int index)
        {
            var g = (TGlobal) Activator.CreateInstance(typeof(TGlobal))!;

            if (this is ISharpieGlobal<TPlugin> self)
            {
                g.plugin = self.plugin;

                if (g is IUpdateableGlobal<TPlugin> ug)
                {
                    OnUpdate += ug.OnUpdate;
                }
            }
            return g;
        }


        public TGlobal? this[int index]
        {
            get
            {
                if (this is IIndexedGlobal<TPlugin, TGlobal> indexedGlobal && indexedGlobal.Index != null)
                    return indexedGlobal.Index[index];

                return default;
            }
        }
    }

    /// <summary>
    /// Represents a globally indexed collection of items, accessible via a combination of a string key and an integer
    /// index.
    /// </summary>
    /// <remarks>This class provides a two-dimensional indexer for accessing global items using a string key
    /// and an optional integer index. The default index value is 0 if not specified.</remarks>
    /// <typeparam name="TPlugin">The type of the plugin associated with the global item.</typeparam>
    /// <typeparam name="TGlobal">The type of the global item, which must implement <see cref="ISharpieGlobal{TPlugin}"/>.</typeparam>
    /// <typeparam name="TStr">The type of the string key, which must be non-null.</typeparam>
    public abstract class IndexedGlobal<TPlugin, TGlobal, TStr> : IndexedGlobal<TPlugin,TGlobal>
       where TGlobal : ISharpieGlobal<TPlugin>
         where TStr : notnull
         where TPlugin : ISharpiePlugin
    {
        protected readonly Indexer2D<TGlobal> Index;

        protected IndexedGlobal() 
        {
            Index = new Indexer2D<TGlobal>(Initialize, InitializeWithString);
        }

        private TGlobal InitializeWithString(string arg1, int arg2)
        {
            return base.Initialize(arg2);
        }

        public TGlobal this[string key, int index = 0]
        {
            get
            {
                return Index[key, index];
            }
        }
    }

    
}