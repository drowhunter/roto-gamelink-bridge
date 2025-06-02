namespace Sharpie.Engine.Contracts
{
    public interface ISharpieScript
    {
        Task Start();

        void Update();

        Task Stop();
    }

    public abstract class SharpieScript : ISharpieScript
    {
        public virtual Task Start() => Task.CompletedTask;
        public virtual void Update()
        {
            // Default implementation can be overridden by derived classes
        }
        public virtual Task Stop() => Task.CompletedTask;
    }
}
