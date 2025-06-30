using com.rotovr.sdk;

using RotoGLBridge.Plugins;
using RotoGLBridge.Plugins.GameLink;
using RotoGLBridge.Services;

using Sharpie.Helpers.Core;
using Sharpie.Plugins.Speech;

using System.Text.Json.Serialization;

namespace RotoGLBridge.Scripts
{
    public class Main(
        ILogger<Main> logger, 
        GamelinkGlobal gamelink, 
        RotoPluginGlobal roto,
        YawDeviceGlobal yawDevice,
        SpeechGlobal speech,
        OxrmcGlobal oxrmc,
        IConsoleWatcher cons ) : SharpieScript
    {
        float yaw;
        //int i = 0;
        float roll;
        //int mode = 1;



        public override async Task Start()
        {
            logger.LogInformation($"Main script started.");

            speech.Say("Roto Chair Initialized");

            gamelink.OnUpdate += OnGameLinkUpdate;

            var options = new JsonSerializerOptions { WriteIndented = false };
            options.Converters.Add(new JsonStringEnumConverter());

            await roto.SwitchModeAsync(ModeType.FollowObject, () => {

                return yaw;
            });

            roto.SetPower(.8f);

            

            yawDevice.OnUpdate += () =>
            {
                //cons.Write(0, 12, $"tcp: {yawDevice.Command}");
            };
            roto.OnUpdate += () =>
            {
               gamelink.IsConnected = roto.IsConnected;
            };

            
        }

        private void OnGameLinkUpdate()
        {
            yaw = gamelink.yaw;

            var r = Filters.EnsureMapRange(gamelink.roll, -40, 40, -1, 1);
            roll = r > 180 ? r - 360 : r;
        }

        

        public override void Update()
        {

            Watch();

            EnableVoiceControl();
            //oxrmc.Activate = speech.Said(["toggle motion compensation"], .8f);
        }

        private void Watch()
        {
            cons.Watch(nameof(RotoPlugin.IsPluggedIn), roto.IsPluggedIn);
            cons.Watch(nameof(RotoPluginGlobal.Status), roto.Status);
            cons.Watch(nameof(yaw), yaw.ToString("F1").PadLeft(5));
            cons.Watch(nameof(Roto.Telemetry.Power), roto.Telemetry.Power.ToString().PadLeft(3));
            cons.Watch(nameof(RotoDataModel.Mode), roto.Data?.Mode.ToString());
            cons.Watch(nameof(RotoDataModel.LerpedAngle), roto.Data?.LerpedAngle.ToString("F1").PadLeft(5));
            cons.Watch(nameof(RotoDataModel.CalibratedAngle), roto.Data?.CalibratedAngle.ToString("F1").PadLeft(5));
            cons.Watch(nameof(Roto.Telemetry.Delta), roto.Telemetry.Delta.ToString("F1").PadLeft(5));
            cons.Watch(nameof(Roto.Telemetry.TargetAngle), roto.Telemetry.TargetAngle.ToString().PadLeft(3));
            cons.Watch(nameof(Roto.Telemetry.CappedTargetAngle), roto.Telemetry.CappedTargetAngle.ToString().PadLeft(3));
            cons.Watch(nameof(Roto.Telemetry.AngularVelocity), $"{roto.Telemetry.AngularVelocity,8:F1} °/s");
            cons.Watch("hotkeys", $"{oxrmc.plugin.HotKeysPreseed}");
            cons.Watch("trigger", $"{(ActivityBit)oxrmc.plugin.activityFlags.trigger}");
            cons.Watch("confirm", $"{(ActivityBit)oxrmc.plugin.activityFlags.confirm}");
            cons.Watch("turns", roto.Turns);

            cons.Publish();
        }

        private void EnableVoiceControl()
        {
            oxrmc.Activate = speech.Said(["toggle motion comp"], .70f) || oxrmc.Activate;

            oxrmc.CrosshairToggle = speech.Said(["crosshair"], .70f);

            oxrmc.StabilizerToggle = speech.Said(["stabilize"], .70f);
        }
        
    }
}