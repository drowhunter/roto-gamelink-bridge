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
        Roto2PluginGlobal roto,
        YawDeviceGlobal tcpDevice,
        SpeechGlobal speech,
        OxrmcGlobal oxrmc,
        UsbWatcherGlobal usbWatcher,
        
        IFollowCalculator followCalculator
        ) : SharpieScript
    {

        private bool? chair = null;
        private float _yaw;

        public float Yaw
        {
            get => _yaw;
            private set
            {
                if (_yaw != value)
                {
                    _yaw = value;
                    oxrmc.SendMotionComp(value);
                }
            }
        }

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

            
            followCalculator.Reset();

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
            
            if (roto.IsConnected)
            {
                var result = followCalculator.Update(gamelink.yaw, roto.Yaw);
                
                roto.Yaw = result.NewFollowAngle;
                
            }
            else
            {
                Yaw = gamelink.yaw;               
            }

            RumblePower = gamelink.rumblePower;
            RumbleSpeed = gamelink.rumbleSpeed;
            Hertz = gamelink.hz;// != 0 ? Math.Clamp(gamelink.hz, 20, 100) : 0;
            Runmode = roto.RunMode.ToString();

            
            if (RumblePower > 0 || RumbleSpeed > 0)
                roto.Vibrate((int)RumblePower, (int)RumbleSpeed);

        }

        
        
        
        public override void Execute()
        {
            IsConnected.OnNext(roto.IsConnected); 
            
            if (roto.IsConnected)
            {
                Yaw = roto.Yaw;
            }

            EnableVoiceControl();
        }

        

        private void EnableVoiceControl()
        {
            oxrmc.Activate = speech.Said(["toggle motion comp"], .70f) || oxrmc.Activate;

            oxrmc.CrosshairToggle = speech.Said(["crosshair"], .70f);

            oxrmc.StabilizerToggle = speech.Said(["stabilize"], .70f);
        }

        

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
