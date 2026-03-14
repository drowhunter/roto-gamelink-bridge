using RotoGLBridge.Services;

using rotoUSB;

using Sharpie.Engine.Contracts.Plugins;

using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace RotoGLBridge.Plugins
{
    [GlobalType(Type = typeof(RotoPluginGlobal))]
    public class RotoPlugin(
        ILogger<RotoPlugin> logger,        
        IRotoChair rotoChair,
        IRumbleService rumbleService
        ) : SharpiePlugin //UpdateablePlugin
    {
    
        private BehaviorSubject<bool> _rumbleEnabled = new BehaviorSubject<bool>(false);

        public IObservable<bool> RumbleEnabled => _rumbleEnabled.AsObservable();


        public bool UsbConnected { get => Status.USBConnected;  }

        public RotoStatus Status { get; private set; } = new RotoStatus();

        public IObservable<int> Error { get; private set; }

        public override Task Start()
        {
            logger.LogInformation("RotoPlugin started.");

            rotoChair.LoadUSBLibrary();

            Error = Observable.FromEvent<RotoChair.ErrorModeHandler, int>(
                handler => (errorMode) => handler(errorMode),
                h => rotoChair.ErrorModeChanged += h,
                h => rotoChair.ErrorModeChanged -= h
            );

           

            rumbleService.RumbleEvent += (rumble) =>
            {
                rotoChair.SetRumble(rumble.power, (byte)rumble.durationMs);
            };

            rumbleService.Start();

            return Task.CompletedTask;
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

            Status = state;
           // OnUpdate();
        }

        public override Task Stop()
        {
            rumbleService.Stop();
            rotoChair.Disconnect();
            logger.LogInformation("RotoPlugin stopped.");
            return Task.CompletedTask;
        }

        
        
        public void SetRunMode(RunMode mode)
        {
            switch (mode)
            {
                case RunMode.Idle:
                    rotoChair.SetIdleMode();
                    break;
                case RunMode.Follow:
                    rotoChair.SetObjectFollowMode();
                    break;
                case RunMode.Free:
                    rotoChair.SetFreeMode();
                    break;
                case RunMode.Cockpit:
                    rotoChair.SetCockpitMode(60);
                    break;
                
                
            }
            //rotoChair.SetRunMode((byte)mode);
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

        internal void SetFollowDegree(int degree, int speed) => rotoChair.SetObjectFollowDegree(degree, speed);

        /// <summary>
        /// Tell the chair to rumble
        /// </summary>
        /// <param name="ampPercent">a value between 0 and 100</param>
        /// <param name="hzPercent">a value in ms between 0 - 100</param>
        public void Vibrate(int ampPercent, int hzPercent)
        {
            rumbleService.Rumble(ampPercent, hzPercent);
        }

    }

    public class RotoPluginGlobal : SharpieGlobal //UpdateablePluginGlobal
                                                   <RotoPlugin>
    {
        public IObservable<int> OnError => plugin.Error.DistinctUntilChanged();

        #region  Exposed Properties

        public bool IsConnected => plugin?.UsbConnected ?? false;

        public RunMode RunMode
        {
            get => (RunMode)plugin.Status.RunMode;
            set => plugin.SetRunMode(value);
        }

        public RotoStatus Status => plugin.Status;
         

        //public new RotoStatus State => plugin.State;
        public int Power { get; set; } = 100;

        public float Yaw
        {
            get => (float)plugin.Status.BaseDegree;
            set
            {
                plugin.SetFollowDegree((int)value, this.Power);
            }
        }


        #endregion
        internal void Connect() => plugin.Connect();

        internal void Disconnect() => plugin.Disconnect();


        internal void Vibrate(int ampPercent, int hzPercent) => plugin.Vibrate(ampPercent, hzPercent);


    }
}
