using RotoGLBridge.Services;

using rotoUSB;

using Sharpie.Engine.Contracts.Plugins;

namespace RotoGLBridge.Plugins
{
    [GlobalType(Type = typeof(Roto2PluginGlobal))]
    public class Roto2Plugin(
        ILogger<Roto2Plugin> logger,
        IEnumerable<IMmfSender> mmfSenders,
        IRotoChair rotoChair
        ) : SharpiePlugin //UpdateablePlugin
    {
    
        public bool UsbConnected { get => State.USBConnected;  }

        public RotoStatus State { get; private set; } = new RotoStatus();

        public override Task Start()
        {
            logger.LogInformation("Roto2Plugin started.");

            rotoChair.LoadUSBLibrary();

            return Task.CompletedTask;
        }

        public override void Execute()
        {
            var state = rotoChair.GetRotoStatus();
            
            
            if(!UsbConnected && state.USBConnected)
            {
                logger.LogInformation("RotoChair connected.");
            }
            else if (UsbConnected && !state.USBConnected)
            {
                logger.LogInformation("RotoChair disconnected.");
            }

            State = state;
           // OnUpdate();
        }

        public override Task Stop()
        {
            rotoChair.Disconnect();
            logger.LogInformation("Roto2Plugin stopped.");
            return Task.CompletedTask;
        }

        
        
        public void SetRunMode(RunMode mode)
        {
            rotoChair.SetRunMode(mode);
        }

        //public void SetPower(float power)
        //{
        //    rotoChair.(power);
        //}

        public void Connect()
        {
            rotoChair.Connect();
            rotoChair.SetObjectFollowMode();            
        }

        internal void Disconnect()
        {
            
            rotoChair.Disconnect();
        }

        internal void SetFollowDegree(int degree) => rotoChair.SetObjectFollowDegree(degree);

    }

    public class Roto2PluginGlobal : SharpieGlobal //UpdateablePluginGlobal
                                                   <Roto2Plugin>
    {
        
        #region  Exposed Properties

        public bool IsConnected => plugin?.UsbConnected ?? false;

        public RunMode RunMode
        {
            get => plugin.State.RunMode;
            set => plugin.SetRunMode(value);
        }

        //public new RotoStatus State => plugin.State;

        public float Yaw
        {
            get => (float)plugin.State.BaseDegree;
            set
            {
                plugin.SetFollowDegree((int)value);
            }
        }


        #endregion
        internal void Connect() => plugin.Connect();

        internal void Disconnect() => plugin.Disconnect();


    }
}
