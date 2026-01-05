using RotoGLBridge.Plugins;
using RotoGLBridge.Plugins.GameLink;
using RotoGLBridge.Services;

using Sharpie.Plugins.Speech;
using Sharpie.Plugins.UsbWatcher;

using System.ComponentModel.DataAnnotations;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace RotoGLBridge.Scripts
{
    public class RotoScript(
        ILogger<RotoScript> logger,
        GamelinkGlobal gamelink,
        RotoPluginGlobal roto,
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

        public float AmpPercent { get; private set; }

        public float HzPercent { get; private set; }

        public float Hertz { get; private set; }

        public string Runmode { get; private set; }

        public BehaviorSubject<bool> IsConnected = new(false);

        public bool OxrmcIsConnected => oxrmc.IsConnected;

        public bool RotoIsConnected => roto.IsConnected;

        public bool GamelinkIsConnected => gamelink.IsConnected;

        public bool TcpIsConnected => tcpDevice.IsConnected;

        public int Power { get; private set; } = 100;

        List<IDisposable> disposables = new();

        public override Task Start()
        {
            logger.LogInformation($"Main script started.");

            
            followCalculator.Reset();

            
            disposables.Add(followCalculator.OnAngleChangedObservable.Select(_ => MathF.Round(_.NewFollowAngle)).DistinctUntilChanged().Subscribe(angle =>
            {
                // update roto chair yaw when angle changes
                roto.Yaw = angle;
            }));

            gamelink.OnUpdate += OnUdpUpdate;

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
            gamelink.OnUpdate -= OnUdpUpdate;
            
            usbWatcher.OnDeviceChange -= OnUsbChange;

            foreach (var disposable in disposables)
            {
                disposable.Dispose();
            }

           

            roto.Disconnect();
            logger.LogInformation($"Main script stopped.");
            return Task.CompletedTask;
        }

        

        private void OnUdpUpdate()
        {
            
            if (roto.IsConnected)
            {
                followCalculator.Update(gamelink.Yaw, roto.Yaw);                
            }
            else
            {
                Yaw = gamelink.Yaw;               
            }

            AmpPercent = gamelink.AmpPercent;
            HzPercent = gamelink.HzPercent;
            Hertz = gamelink.Hz;// != 0 ? Math.Clamp(gamelink.Hz, 20, 100) : 0;
            Runmode = roto.RunMode.ToString();

            
            if (AmpPercent > 0 || HzPercent > 0)
                roto.Vibrate((int)AmpPercent, (int)HzPercent);

        }

        
        
        
        public override void Execute()
        {
            IsConnected.OnNext(roto.IsConnected); 
            
            if (roto.IsConnected)
            {
                roto.Power = this.Power;
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
