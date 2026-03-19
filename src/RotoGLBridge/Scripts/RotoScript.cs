using RotoGLBridge.Plugins;
using RotoGLBridge.Plugins.GameLink;
using RotoGLBridge.Services;
using rotoUSB;
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

        public BehaviorSubject<bool?> IsConnected = new(null);

        public bool OxrmcIsConnected => oxrmc.IsConnected;

        public bool RotoIsConnected => roto.IsConnected;

        public bool GamelinkIsConnected => gamelink.IsConnected;

        public bool TcpIsConnected => tcpDevice.IsConnected;

        public int Power { get; private set; } = 100;

        List<IDisposable> disposables = new();

        //bool? _lastConnected = null;

        public RotoStatus State => roto.Status;

        public override Task Start()
        {
            logger.LogInformation($"Main script started.");

            gamelink.sampleRateHz = 30;

            followCalculator.Reset();

            
            disposables.Add(followCalculator.OnAngleChangedObservable.Select(_ => MathF.Round(_.NewFollowAngle)).DistinctUntilChanged().Subscribe(angle =>
            {
                // update roto chair yaw when angle changes
                roto.Yaw = angle;
            }));

            gamelink.OnUpdate += OnUdpUpdate;

            usbWatcher.OnDeviceChange += OnUsbChange;

            usbWatcher.Watch(0x04D9, 0xB564);

            roto.OnWriteError += Roto_OnWriteError;

            roto.ErrorModeChangedObservable.Subscribe(code =>
            {
                logger.LogError("Roto Chair USB Error: {0}", code);

                switch (code)
                {
                    case 0x80:
                        speech.Say("Roto Wireless Emergency Stop Activated");
                        break;
                    case 0x40:
                        speech.Say("Roto Motor Stalled");
                        break;
                    case 0x20:
                        speech.Say("Roto Stopped");
                        break;
                    case 0x10:
                        speech.Say("Roto Head Tracker Emergency Stop");
                        break;
                    case 0x00:
                        roto.SetRunMode(RunMode.Follow);
                        speech.Say("Roto Resuming");
                        break;
                }


            });
            /*
            var s = IsConnected.DistinctUntilChanged().Buffer(2, 1).Subscribe(buffer =>
            {
                _lastConnected = buffer[0];
                var connected = buffer[1];

                if (connected.Value)
                {
                    speech.Say("Roto Connected");
                }
                else if(_lastConnected != null)
                {
                    speech.Say("Roto Disconnected");
                }

                _lastConnected = connected;
            });

            disposables.Add(s);*/



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
            try
            {
                IsConnected.OnNext(roto.IsConnected);

                if (roto.IsConnected)
                {
                    roto.Power = this.Power;
                    Yaw = roto.Yaw;

                }

                EnableVoiceControl();
            }
            catch (Exception ex)
            {
                //logger.LogError($"Error in Execute: {ex.Message} {ex.StackTrace}");
                throw;
            }
        }

        

        private void EnableVoiceControl()
        {
            oxrmc.Activate = speech.Said(["toggle motion comp"], .70f) || oxrmc.Activate;

            oxrmc.CrosshairToggle = speech.Said(["crosshair"], .70f);

            oxrmc.StabilizerToggle = speech.Said(["stabilize"], .70f);
        }

        private void Roto_OnWriteError()
        {
            logger.LogError("Roto Chair Write Error: Watching for Device");
            usbWatcher.Watch(0x04D9, 0xB564);
        }

        private void OnUsbChange(VidPid vidpid, bool isConnected)
        {
            if (isConnected)
            {
                usbWatcher.UnWatch(0x04D9, 0xB564);


                chair = true;
                logger.LogInformation("USB Roto detected.");
                speech.Say("Roto Chair Detected, connecting..");
                roto.Connect();
                
            }
            else 
            {
                logger.LogInformation("USB Roto disconnected.");
                if (chair == true)
                {
                    chair = false;
                    speech.Say("Roto Chair Disconnected");

                    //roto.Disconnect();                    
                }
            }
        }
    }
}
