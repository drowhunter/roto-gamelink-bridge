namespace Sharpie.Engine.Contracts.Plugins
{
    public abstract class IndexedPlugin<TGlobal, TPlugin> : SharpiePlugin, IIndexedPlugin<TGlobal, TPlugin>, IUpdateablePlugin
        where TGlobal : ISharpieGlobal<TPlugin>
        where TPlugin : ISharpiePlugin
    {
        public Action OnUpdated { get; set; } = () => { };

        public abstract void OnGlobalAdded(TGlobal gbl, int index, TPlugin plugin);
    }


    public interface IIndexedPlugin<TGlobal, TPlugin> : IHasIndex<TGlobal, int, TPlugin>, ISharpiePlugin
        where TGlobal : ISharpieGlobal<TPlugin>
        where TPlugin : ISharpiePlugin
    {
    }

    public interface IIndexedPlugin<TGlobal, TStr, TPlugin> : IHasIndex<TGlobal, int, TStr, TPlugin>, ISharpiePlugin
        where TGlobal : ISharpieGlobal<TPlugin>
        where TPlugin : ISharpiePlugin
    {
    }

    public interface IHasIndex<T, TIndex, TPlugin> where T : ISharpieGlobal<TPlugin> where TPlugin : ISharpiePlugin
    {
        void OnGlobalAdded(T gbl, TIndex index, TPlugin plugin);
    }

    public interface IHasIndex<T, TIndex, TStr, TPlugin> where T : ISharpieGlobal<TPlugin> where TPlugin : ISharpiePlugin
    {
        void OnGlobalAdded(T gbl, TIndex index, TStr str, TPlugin plugin);
    }

}