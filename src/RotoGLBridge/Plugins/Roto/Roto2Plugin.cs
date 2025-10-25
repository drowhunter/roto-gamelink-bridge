using RotoGLBridge.Services;

using rotoUSB;

using Sharpie.Engine.Contracts.Plugins;

using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace RotoGLBridge.Plugins
{
    [GlobalType(Type = typeof(Roto2PluginGlobal))]
    public class Roto2Plugin(
        ILogger<Roto2Plugin> logger,
        IEnumerable<IMmfSender> mmfSenders,
        IRotoChair rotoChair,
        IRumbleService rumbleService
        ) : SharpiePlugin //UpdateablePlugin
    {
    
        private BehaviorSubject<bool> _rumbleEnabled = new BehaviorSubject<bool>(false);

        public IObservable<bool> RumbleEnabled => _rumbleEnabled.AsObservable();


        public bool UsbConnected { get => State.USBConnected;  }

        public RotoStatus State { get; private set; } = new RotoStatus();

        public IObservable<string> Error { get; private set; }

        public override Task Start()
        {
            logger.LogInformation("Roto2Plugin started.");

            rotoChair.LoadUSBLibrary();

            Error = Observable.FromEvent<string>(
                h => rotoChair.OnUsbError += h,
                h => rotoChair.OnUsbError -= h
            );

            rotoChair.OnUsbError += RotoChair_OnUsbError;

            rumbleService.RumbleEvent += (power, duration) =>
            {
                rotoChair.SetRumble(power, (byte)duration);
            };

            rumbleService.Start();

            return Task.CompletedTask;
        }

        
        public void RotoChair_OnUsbError(string errorMessage)
        {
            logger.LogError("Roto Chair USB Error: {0}", errorMessage);
        }

        public override void Execute()
        {
            var state = rotoChair.GetRotoStatus();
            
            
            if(!UsbConnected && state.USBConnected)
            {
                logger.LogInformation("Roto Chair connected.");
            }
            else if (UsbConnected && !state.USBConnected)
            {
                logger.LogInformation("Roto Chair disconnected.");
            }

            State = state;
           // OnUpdate();
        }

        public override Task Stop()
        {
            rumbleService.Stop();
            rotoChair.Disconnect();
            logger.LogInformation("Roto2Plugin stopped.");
            return Task.CompletedTask;
        }

        
        
        public void SetRunMode(RunMode mode)
        {
            rotoChair.SetRunMode(mode);
        }

        //public void SetPower(float amplitude)
        //{
        //    rotoChair.(amplitude);
        //}

        public void Connect()
        {
            rotoChair.Connect(true);
            rotoChair.SetObjectFollowMode();            
        }

        internal void Disconnect()
        {
            
            rotoChair.Disconnect();
        }

        internal void SetFollowDegree(int degree) => rotoChair.SetObjectFollowDegree(degree);

        /// <summary>
        /// Tell the chair to rumble
        /// </summary>
        /// <param name="power">a value between 0 and 100</param>
        /// <param name="speed">a value in ms between 0 - 100</param>
        public void Vibrate(int power, int speed)
        {
            rumbleService.Rumble(power, speed);
        }

    }

    public class Roto2PluginGlobal : SharpieGlobal //UpdateablePluginGlobal
                                                   <Roto2Plugin>
    {
        public IObservable<string> OnError => plugin.Error.DistinctUntilChanged();

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


        internal void Vibrate(int power, int speed) => plugin.Vibrate(power, speed);


    }
}
