namespace Sharpie.Engine.Contracts.Plugins
{
    public interface IUpdateablePlugin : ISharpiePlugin
    {
        Action OnUpdated { get; set; }
    }

    public abstract class UpdateablePlugin : SharpiePlugin, IUpdateablePlugin
    {
        Action IUpdateablePlugin.OnUpdated { get; set; } = () => { };

        protected void OnUpdate()
        {
            if (this is IUpdateablePlugin self)
            {
                self.OnUpdated();
            }
        }
    }
}