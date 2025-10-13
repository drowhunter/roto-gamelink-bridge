using RotoGLBridge.Plugins;
using RotoGLBridge.Plugins.GameLink;
using RotoGLBridge.Services;

using Sharpie.Plugins.Speech;
using Sharpie.Plugins.UsbWatcher;

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
        IConsoleWatcher cons) : SharpieScript
    {

        //public event Action<float> OnYawUpdate;
        public float Yaw { get; set; }

        public bool OxrmcIsConnected => oxrmc.IsConnected;

        public bool RotoIsConnected => roto.IsConnected;

        public bool GamelinkIsConnected => gamelink.IsConnected;

        public bool TcpIsConnected => tcpDevice.IsConnected;

        public override Task Start()
        {
            logger.LogInformation($"Main script started.");

            speech.Say("Roto Chair Initialized");

            gamelink.OnUpdate += OnGameLinkUpdate;

            usbWatcher.OnDeviceChange += OnUsbChange;

            usbWatcher.Watch(0x04D9, 0xB564);


            

            //var options = new JsonSerializerOptions { WriteIndented = false };
            //options.Converters.Add(new JsonStringEnumConverter());

            /*
            await roto.SwitchModeAsync(ModeType.FollowObject, () => {

                return yaw;
            });

            roto.SetPower(.8f);
            */


            //yawDevice.OnUpdate += () =>
            //{
            //cons.Write(0, 12, $"tcp: {yawDevice.Command}");
            //};

            return Task.CompletedTask;
        }

        private void OnGameLinkUpdate()
        {
            //yaw = gamelink.yaw;
            if (roto.IsConnected)
            {
                roto.Yaw = gamelink.yaw;
            }
            else
            {
                Yaw = gamelink.yaw;
                //OnYawUpdate?.Invoke(gamelink.yaw);
            }

            //var r = Filters.EnsureMapRange(gamelink.roll, -40, 40, -1, 1);
            //roll = r > 180 ? r - 360 : r;
        }

        
        public override void Execute()
        {
            if (roto.IsConnected)
            {
                //OnYawUpdate?.Invoke(roto.Yaw);
                Yaw = gamelink.yaw;
            }


            Watch();

            EnableVoiceControl();
        }

        private void Watch()
        {
            //cons.Watch(nameof(RotoPlugin.IsPluggedIn), roto.IsPluggedIn);
            //cons.Watch(nameof(RotoPluginGlobal.Status), roto.Status);
            //cons.Watch(nameof(yaw), yaw.ToString("F1").PadLeft(5));
            //cons.Watch(nameof(Roto.Telemetry.Power), roto.Telemetry.Power.ToString().PadLeft(3));
            //cons.Watch(nameof(RotoDataModel.Mode), roto.Data?.Mode.ToString());
            //cons.Watch(nameof(RotoDataModel.LerpedAngle), roto.Data?.LerpedAngle.ToString("F1").PadLeft(5));
            //cons.Watch(nameof(RotoDataModel.CalibratedAngle), roto.Data?.CalibratedAngle.ToString("F1").PadLeft(5));
            //cons.Watch(nameof(Roto.Telemetry.Delta), roto.Telemetry.Delta.ToString("F1").PadLeft(5));
            //cons.Watch(nameof(Roto.Telemetry.TargetAngle), roto.Telemetry.TargetAngle.ToString().PadLeft(3));
            //cons.Watch(nameof(Roto.Telemetry.CappedTargetAngle), roto.Telemetry.CappedTargetAngle.ToString().PadLeft(3));
            //cons.Watch(nameof(Roto.Telemetry.AngularVelocity), $"{roto.Telemetry.AngularVelocity,8:F1} °/s");
            cons.Watch("hotkeys", $"{oxrmc.plugin.HotKeysPreseed}");
            cons.Watch("trigger", $"{(ActivityBit)oxrmc.plugin.activityFlags.trigger}");
            cons.Watch("confirm", $"{(ActivityBit)oxrmc.plugin.activityFlags.confirm}");
            //cons.Watch("turns", roto.Turns);

            cons.Publish();
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
                logger.LogInformation("Roto detected.");
                speech.Say("Roto Chair Detected, connecting..");
                roto.Connect();
                
            }
            else
            {
                logger.LogInformation("Roto disconnected.");
                speech.Say("Roto Chair Disconnected");
                roto.Disconnect();
            }
        }
    }
}
