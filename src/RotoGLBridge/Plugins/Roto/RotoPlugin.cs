using RotoGLBridge.Services;

using rotoUSB;

using Sharpie.Engine.Contracts.Plugins;

using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace RotoGLBridge.Plugins
{
    [GlobalType(Type = typeof(RotoPluginGlobal))]
    public class RotoPlugin : SharpiePlugin //UpdateablePlugin
    {
        private readonly ILogger<RotoPlugin> _logger;
        private readonly IRotoChair _rotoChair;
        private readonly IRumbleService _rumbleService;


        private BehaviorSubject<bool> _rumbleEnabled = new BehaviorSubject<bool>(false);

        public IObservable<bool> RumbleEnabled => _rumbleEnabled.AsObservable();

        public event Action OnWriteError;

        public bool UsbConnected { get => Status.USBConnected;  }

        public RotoStatus Status { get; private set; } = new RotoStatus();

        public IObservable<int> ErrorMode { get; private set; }


        public RotoPlugin(
            ILogger<RotoPlugin> logger,
            IRotoChair rotoChair,
            IRumbleService rumbleService
        )
        {
            this._logger = logger;
            this._rotoChair = rotoChair;
            this._rumbleService = rumbleService;

            _rotoChair.OnWriteError += () =>
            {
                OnWriteError?.Invoke();
            };

            rotoChair.EnableModeSound(false);
        }

        public override Task Start()
        {
            _logger.LogInformation("RotoPlugin started.");

            _rotoChair.LoadUSBLibrary();

            ErrorMode = Observable.FromEvent<RotoChair.ErrorModeHandler, int>(
                handler => (errorMode) => handler(errorMode),
                h => _rotoChair.ErrorModeChanged += h,
                h => _rotoChair.ErrorModeChanged -= h
            );

           

            _rumbleService.RumbleEvent += (rumble) =>
            {
                _rotoChair.SetRumble(rumble.power, (byte)rumble.durationMs);
            };

            _rumbleService.Start();

            return Task.CompletedTask;
        }

      
        public override void Execute()
        {
            Status = _rotoChair.GetRotoStatus();            
        }

        public override Task Stop()
        {
            _rumbleService.Stop();
            _rotoChair.Disconnect();
            _logger.LogInformation("RotoPlugin stopped.");
            return Task.CompletedTask;
        }

        
        
        public void SetRunMode(RunMode mode)
        {
            switch (mode)
            {
                case RunMode.Idle:
                    _rotoChair.SetIdleMode();
                    break;
                case RunMode.Follow:
                    _rotoChair.SetObjectFollowMode();
                    break;
                case RunMode.Free:
                    _rotoChair.SetFreeMode();
                    break;
                case RunMode.Cockpit:
                    _rotoChair.SetCockpitMode(60);
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
            _rotoChair.Connect(true);
            _rotoChair.SetObjectFollowMode();            
        }

        internal void Disconnect()
        {            
            _rotoChair.Disconnect();
        }

        internal void SetFollowDegree(int degree, int speed) => _rotoChair.SetObjectFollowDegree(degree, speed);

        /// <summary>
        /// Tell the chair to rumble
        /// </summary>
        /// <param name="ampPercent">a value between 0 and 100</param>
        /// <param name="hzPercent">a value in ms between 0 - 100</param>
        public void Vibrate(int ampPercent, int hzPercent)
        {
            _rumbleService.Rumble(ampPercent, hzPercent);
        }

    }

    /// <summary>
    /// Provides a global scripting interface for the RotoPlugin, exposing properties and methods
    /// for controlling the Roto Chair hardware from scripts.
    /// </summary>
    public class RotoPluginGlobal : SharpieGlobal //UpdateablePluginGlobal
                                                   <RotoPlugin>
    {
        
       

        /// <summary>
        /// Occurs when a write error is encountered during communication with the Roto Chair.
        /// </summary>
        public event Action OnWriteError
        {
            add => plugin.OnWriteError += value;
            remove => plugin.OnWriteError -= value;
        }

        /// <summary>
        /// Gets an observable sequence of error mode changes, emitting only distinct consecutive values.
        /// </summary>
        public IObservable<int> ErrorModeChangedObservable => plugin.ErrorMode.DistinctUntilChanged();

        #region  Exposed Properties

        /// <summary>
        /// Gets a value indicating whether the Roto Chair is currently connected via USB.
        /// </summary>
        public bool IsConnected => plugin?.UsbConnected ?? false;

        /// <summary>
        /// Gets or sets the current run mode of the Roto Chair.
        /// </summary>
        public RunMode RunMode
        {
            get => (RunMode)plugin.Status.RunMode;
            set => plugin.SetRunMode(value);
        }

        /// <summary>
        /// Gets the current status information of the Roto Chair.
        /// </summary>
        public RotoStatus Status => plugin.Status;
         

        //public new RotoStatus State => plugin.State;
        /// <summary>
        /// Gets or sets the power level for chair movements. Default is 100.
        /// </summary>
        public int Power { get; set; } = 100;

        /// <summary>
        /// Gets or sets the yaw (rotation) angle of the chair in degrees.
        /// Setting this value instructs the chair to rotate to the specified degree using the current power level.
        /// </summary>
        public float Yaw
        {
            get => (float)plugin.Status.BaseDegree;
            set
            {
                plugin.SetFollowDegree((int)value, this.Power);
            }
        }


        #endregion
        /// <summary>
        /// Connects to the Roto Chair hardware.
        /// </summary>
        internal void Connect() => plugin.Connect();


        public void SetRunMode(RunMode mode) => plugin.SetRunMode(mode);

        /// <summary>
        /// Disconnects from the Roto Chair hardware.
        /// </summary>
        internal void Disconnect() => plugin.Disconnect();

        /// <summary>
        /// Triggers a vibration effect on the Roto Chair.
        /// </summary>
        /// <param name="ampPercent">The amplitude percentage (0-100).</param>
        /// <param name="hzPercent">The duration in milliseconds (0-100).</param>
        internal void Vibrate(int ampPercent, int hzPercent) => plugin.Vibrate(ampPercent, hzPercent);


    }
}
