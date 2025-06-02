namespace Sharpie.Engine.Contracts.Plugins
{
    public enum PluginState
    {
        NotStarted,
        Started,
        Stopped,
        Error
    }

    public interface ISharpiePlugin
    {
        PluginState State { get; set; }

        Task Start();

        void Execute();

        void Stop();
    }

    

    public abstract class SharpiePlugin : ISharpiePlugin
    {
        PluginState ISharpiePlugin.State { get; set; }

        //public PluginState State

        public abstract void Execute();
        public abstract Task Start();
        public abstract void Stop();

    }

    
}