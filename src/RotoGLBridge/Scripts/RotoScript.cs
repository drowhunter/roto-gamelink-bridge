using RotoGLBridge.Plugins;
using RotoGLBridge.Services;
using rotoUSB;
using Sharpie.Plugins.Speech;
using Sharpie.Plugins.UsbWatcher;

using System.Reactive.Subjects;

namespace RotoGLBridge.Scripts
{
    public class RotoScript(
        ILogger<RotoScript> logger,
        RotoPluginGlobal roto,
        RotoYawEmulator yawEmulator,
        SpeechGlobal speech,
        OxrmcGlobal oxrmc,
        UsbWatcherGlobal usbWatcher
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

        public float AmpPercent => yawEmulator.AmpPercent;

        public float HzPercent => yawEmulator.HzPercent;

        public float Hertz => yawEmulator.Hertz;

        public string Runmode => roto.RunMode.ToString();

        public BehaviorSubject<bool?> IsConnected = new(null);

        public bool OxrmcIsConnected => oxrmc.IsConnected;

        public bool RotoIsConnected => roto.IsConnected;

        public bool GamelinkIsConnected => yawEmulator.TelemetryIsConnected;

        public bool TcpIsConnected => yawEmulator.TcpIsConnected;

        public int Power => yawEmulator.Power;

        List<IDisposable> disposables = new();

        //bool? _lastConnected = null;

        public RotoStatus State => roto.Status;

        public override async Task Start()
        {
            logger.LogInformation($"Main script started.");

            usbWatcher.OnDeviceChange += OnUsbChange;

            usbWatcher.Watch(0x04D9, 0xB564);

            roto.OnWriteError += Roto_OnWriteError;

            disposables.Add(roto.OnError.Subscribe(errorMessage =>
            {
                logger.LogError("Roto Chair USB Error: {0}", errorMessage);

                if(errorMessage == 0x80)
                    speech.Say("Roto Chair Emergency Stop Activated");
            }));
            await yawEmulator.StartAsync();
        }

        public override async Task Stop()
        {
            await yawEmulator.StopAsync();
            usbWatcher.OnDeviceChange -= OnUsbChange;
            roto.OnWriteError -= Roto_OnWriteError;
            foreach (var disposable in disposables)
                disposable.Dispose();
            disposables.Clear();
            roto.Disconnect();
            logger.LogInformation("Main script stopped.");
        }

        public override void Execute()
        {
            IsConnected.OnNext(roto.IsConnected);
            Yaw = yawEmulator.Yaw;
            EnableVoiceControl();
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
