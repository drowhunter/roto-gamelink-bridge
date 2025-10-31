using RotoGLBridge.Plugins;
using RotoGLBridge.Plugins.GameLink;
using RotoGLBridge.Services;

using Sharpie.Plugins.Speech;
using Sharpie.Plugins.UsbWatcher;

using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace RotoGLBridge.Scripts
{
    public class RotoScript(
        ILogger<RotoScript> logger,
        GamelinkGlobal gamelink,
        //RotoPluginGlobal roto,
        Roto2PluginGlobal roto,
        YawDeviceGlobal tcpDevice,
        SpeechGlobal speech,
        OxrmcGlobal oxrmc,
        UsbWatcherGlobal usbWatcher,
        //MathService mathService
        IFollowTargetCalculator followTargetCalculator
        //IConsoleWatcher cons
        ) : SharpieScript
    {

        private float? _initialYaw = null;

        private float? _initialRotoYaw = null;

        //public event Action<float> OnYawUpdate;
        public float Yaw { get; private set; }

        public float RumblePower { get; private set; }

        public float RumbleSpeed { get; private set; }

        public float Hertz { get; private set; }

        public string Runmode { get; private set; }

        public BehaviorSubject<bool> IsConnected = new(false);

        public bool OxrmcIsConnected => oxrmc.IsConnected;

        public bool RotoIsConnected => roto.IsConnected;

        public bool GamelinkIsConnected => gamelink.IsConnected;

        public bool TcpIsConnected => tcpDevice.IsConnected;

        List<IDisposable> disposables = new();

        public override Task Start()
        {
            logger.LogInformation($"Main script started.");

            //speech.Say("Roto Chair Initialized");
            followTargetCalculator.Reset();

            gamelink.OnUpdate += OnGameLinkUpdate;

            usbWatcher.OnDeviceChange += OnUsbChange;

            usbWatcher.Watch(0x04D9, 0xB564);

            roto.OnError.Subscribe(errorMessage =>
            {
                logger.LogError("Roto Chair USB Error: {0}", errorMessage);
                speech.Say("Roto Chair USB Error");
            });

            var s = IsConnected.DistinctUntilChanged().Subscribe(connected =>
            {
                if (connected)
                {
                    speech.Say("Roto Chair Connected");
                }
                else
                {
                    speech.Say("Roto Chair Disconnected");
                }
            });

            disposables.Add(s);



            return Task.CompletedTask;
        }

        override public Task Stop()
        {
            gamelink.OnUpdate -= OnGameLinkUpdate;
            
            usbWatcher.OnDeviceChange -= OnUsbChange;

            foreach (var disposable in disposables)
            {
                disposable.Dispose();
            }

           

            roto.Disconnect();
            logger.LogInformation($"Main script stopped.");
            return Task.CompletedTask;
        }

        private void OnGameLinkUpdate()
        {
            //yaw = gamelink.yaw;
            if (roto.IsConnected)
            {
                followTargetCalculator.Update(gamelink.yaw, roto.Yaw);
                
                roto.Yaw = followTargetCalculator.NewFollowAngle;
                
            }
            else
            {
                Yaw = gamelink.yaw;               
            }

            RumblePower = gamelink.rumblePower;
            RumbleSpeed = gamelink.rumbleSpeed;
            Hertz = gamelink.hz;// != 0 ? Math.Clamp(gamelink.hz, 20, 100) : 0;
            Runmode = roto.RunMode.ToString();

            //if (roto.IsConnected)
            if (RumblePower > 0 || RumbleSpeed > 0)
                roto.Vibrate((int)RumblePower, (int)RumbleSpeed);

        }

        private float CalculateDeltaYaw(float target)
        {
            float retval = 0;

            if (_initialYaw == null)
            {
                _initialYaw = target;
                logger.LogDebug($"Initial Yaw set to {_initialYaw}");
                _initialRotoYaw = roto.Yaw;
                logger.LogDebug($"Initial Roto Yaw set to {_initialRotoYaw}");
                return 0;
            }

           // var deltaTarget = mathService.CalculateDeltaAngle(_initialYaw.Value, target);




            return retval;
        }
        
        
        public override void Execute()
        {
            IsConnected.OnNext(roto.IsConnected); 
            
            if (roto.IsConnected)
            {
                Yaw = roto.Yaw;
            }

            //if (!gamelink.IsConnected)
            //{
            //    IsConnected.OnNext(false);
            //}
            

            //Watch();

            EnableVoiceControl();
        }

        private void Watch()
        {
            //cons.Watch(nameof(RotoPlugin.IsPluggedIn), roto.IsPluggedIn);
            //cons.Watch(nameof(RotoPluginGlobal.Status), roto.Status);
            //cons.Watch(nameof(yaw), yaw.ToString("F1").PadLeft(5));
            //cons.Watch(nameof(Roto.Telemetry.RumblePower), roto.Telemetry.RumblePower.ToString().PadLeft(3));
            //cons.Watch(nameof(RotoDataModel.Mode), roto.Data?.Mode.ToString());
            //cons.Watch(nameof(RotoDataModel.LerpedAngle), roto.Data?.LerpedAngle.ToString("F1").PadLeft(5));
            //cons.Watch(nameof(RotoDataModel.CalibratedAngle), roto.Data?.CalibratedAngle.ToString("F1").PadLeft(5));
            //cons.Watch(nameof(Roto.Telemetry.Delta), roto.Telemetry.Delta.ToString("F1").PadLeft(5));
            //cons.Watch(nameof(Roto.Telemetry.TargetAngle), roto.Telemetry.TargetAngle.ToString().PadLeft(3));
            //cons.Watch(nameof(Roto.Telemetry.CappedTargetAngle), roto.Telemetry.CappedTargetAngle.ToString().PadLeft(3));
            //cons.Watch(nameof(Roto.Telemetry.AngularVelocity), $"{roto.Telemetry.AngularVelocity,8:F1} °/s");
            //cons.Watch("hotkeys", $"{oxrmc.plugin.HotKeysPreseed}");
            //cons.Watch("trigger", $"{(ActivityBit)oxrmc.plugin.activityFlags.trigger}");
            //cons.Watch("confirm", $"{(ActivityBit)oxrmc.plugin.activityFlags.confirm}");
            //cons.Watch("turns", roto.Turns);

            //cons.Publish();
        }

        private void EnableVoiceControl()
        {
            oxrmc.Activate = speech.Said(["toggle motion comp"], .70f) || oxrmc.Activate;

            oxrmc.CrosshairToggle = speech.Said(["crosshair"], .70f);

            oxrmc.StabilizerToggle = speech.Said(["stabilize"], .70f);
        }

        bool? chair = null;

        private void OnUsbChange(VidPid vidpid, bool isConnected)
        {
            if (isConnected)
            {
                chair = true;
                logger.LogInformation("Roto detected.");
                speech.Say("Roto Chair Detected, connecting..");
                roto.Connect();
                
            }
            else 
            {
                logger.LogInformation("Roto disconnected.");
                if (chair == true)
                {
                    chair = false;
                    speech.Say("Roto Chair Disconnected");

                    roto.Disconnect();

                    
                }
            }
        }
    }
}
