using RotoGLBridge.Services;

using rotoUSB;

using Sharpie.Engine.Contracts.Plugins;

using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace RotoGLBridge.Plugins
{
    [GlobalType(Type = typeof(Roto2PluginGlobal))]
    public class Roto2Plugin(
        ILogger<RotoPlugin> logger,
        IEnumerable<IMmfSender> mmfSenders,
        IRotoChair rotoChair
        ) : UpdateablePlugin
    {
    
        

        internal BehaviorSubject<RotoStatus> State { get; set; }

        public override Task Start()
        {
            logger.LogInformation("Roto2Plugin started.");

            rotoChair.LoadUSBLibrary();

            return Task.CompletedTask;


        }

        public override void Execute()
        {
            var state = rotoChair.GetRotoStatus();

            //if (state != null)
            //{
            //    UsbConnected.OnNext(state.USBConnected);
            //}

            State.OnNext(state);
        }

        public override Task Stop()
        {
            rotoChair.Disconnect();
            logger.LogInformation("Roto2Plugin stopped.");
            return Task.CompletedTask;
        }

        
        
        public async Task SwitchModeAsync(int mode)
        {
            //RotoChair.MODE_OBJECT_FOLLOW = 2;
            switch(mode)
            {
                case 0:
                    rotoChair.SetFreeMode();
                    break;
                case 1:
                    rotoChair.SetCockpitMode(90);
                    break;
                default:
                    rotoChair.SetObjectFollowMode();
                    break;
                
            }
            rotoChair.SetObjectFollowMode();

            //return rotoChair.SwitchModeAsync(mode, getYaw);
        }
        //public void SetPower(float power)
        //{
        //    rotoChair.(power);
        //}

        public Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            rotoChair.Connect();
            rotoChair.SetObjectFollowMode();
            return Task.CompletedTask;
        }

        public Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    public class Roto2PluginGlobal : UpdateablePluginGlobal<Roto2Plugin>
    {
        public IObservable<RotoStatus> State => plugin.State.AsObservable();

        internal void Connect()
        {
            throw new NotImplementedException();
        }

        internal void Disconnect()
        {
            throw new NotImplementedException();
        }

        internal void SetTargetAngle(float yaw)
        {
            plugin
        }
    }
}
