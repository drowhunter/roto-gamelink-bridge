using RotoGLBridge.Services;

using rotoUSB;

using Sharpie.Engine.Contracts.Plugins;

namespace RotoGLBridge.Plugins
{
    [GlobalType(Type = typeof(Roto2PluginGlobal))]
    public class Roto2Plugin(
        ILogger<RotoPlugin> logger,
        IEnumerable<IMmfSender> mmfSenders,
        IRotoChair roto
        ) : UpdateablePlugin
    {
    
        public bool IsConnected { get; set; }

        public override Task Start()
        {
            logger.LogInformation("Roto2Plugin started.");

            roto.LoadUSBLibrary();

            return Task.CompletedTask;


        }

        public override Task Stop()
        {
            logger.LogInformation("Roto2Plugin stopped.");
            return Task.CompletedTask;
        }

        
        
        public async Task SwitchModeAsync(int mode)
        {
            //RotoChair.MODE_OBJECT_FOLLOW = 2;
            switch(mode)
            {
                case 0:
                    roto.SetFreeMode();
                    break;
                case 1:
                    roto.SetCockpitMode(90);
                    break;
                default:
                    roto.SetObjectFollowMode();
                    break;
                
            }
            roto.SetObjectFollowMode();

            //return roto.SwitchModeAsync(mode, getYaw);
        }
        //public void SetPower(float power)
        //{
        //    roto.(power);
        //}

        public Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    public class Roto2PluginGlobal : UpdateablePluginGlobal<Roto2Plugin>
    {
        public bool IsConnected => plugin?.IsConnected ?? false;

        internal void Connect()
        {
            throw new NotImplementedException();
        }

        internal void Disconnect()
        {
            throw new NotImplementedException();
        }
    }
}
