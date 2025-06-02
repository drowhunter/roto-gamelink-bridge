using Sharpie.Engine.Contracts.Plugins;

namespace Sharpie.Engine.Contracts
{
    public interface ISharpieEngine
    {
        bool IsRunning { get; }

        void Start(CancellationToken cancellationToken);

        void Stop();

        event EventHandler? OnStarted;
        event EventHandler? OnStopped;
        event EventHandler? OnUpdate;

        event Action<ISharpiePlugin, PluginStateChangedEventArgs>? OnPluginStateChanged;
    }

    public class PluginStateChangedEventArgs
    {
        public PluginState State { get; init; }
    }

}
