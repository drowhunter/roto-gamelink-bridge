using com.rotovr.sdk;

using RotoGLBridge.Plugins;
using RotoGLBridge.Plugins.GameLink;
using RotoGLBridge.Services;

using Sharpie.Helpers;
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
        int i = 0;
        float roll;
        int mode = 1;



        public override async Task Start()
        {
            logger.LogInformation($"Main script started.");
            speech.Say("Main script started.");
            gamelink.OnUpdate += OnGameLinkUpdate;

            var options = new JsonSerializerOptions { WriteIndented = false };
            options.Converters.Add(new JsonStringEnumConverter());

            await roto.SwitchModeAsync(ModeType.FollowObject, () => {

                return yaw;
            });

            roto.SetPower(.8f);

            

            yawDevice.OnUpdate += () =>
            {
                cons.Write(0, 12, $"tcp: {yawDevice.Command}");
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
            


            Dictionary<string, object> stats = new()
            {
                { nameof(RotoPlugin.IsPluggedIn), roto.IsPluggedIn },
                { nameof(RotoPluginGlobal.Status),  roto.Status},
                { nameof(yaw) , yaw.ToString("F1").PadLeft(5) },
                { nameof(Roto.Telemetry.Power), roto.Telemetry.Power.ToString().PadLeft(3) },
                { nameof(RotoDataModel.Mode), roto.Data?.Mode.ToString() },
                { nameof(RotoDataModel.LerpedAngle), roto.Data?.LerpedAngle.ToString("F1").PadLeft(5) },
                { nameof(RotoDataModel.CalibratedAngle), roto.Data?.CalibratedAngle.ToString("F1").PadLeft(5) },
                { nameof(Roto.Telemetry.Delta), roto.Telemetry.Delta.ToString("F1").PadLeft(5) },
                { nameof(Roto.Telemetry.TargetAngle), roto.Telemetry.TargetAngle.ToString().PadLeft(3) },
                { nameof(Roto.Telemetry.CappedTargetAngle), roto.Telemetry.CappedTargetAngle.ToString().PadLeft(3) },
                { nameof(Roto.Telemetry.AngularVelocity), $"{roto.Telemetry.AngularVelocity,8:F1} °/s" },
                { "hotkeys", $"{oxrmc.plugin.HotKeysPreseed}" },
                { "trigger", $"{(ActivityBit)oxrmc.plugin.activityFlags.trigger}" },
                { "confirm", $"{(ActivityBit)oxrmc.plugin.activityFlags.confirm}" }
            };

            if (speech.Said(["toggle motion compensation"], .75f))
            {
                //speech.Say("Toggling motion compensation.");
                stats.Add("motion compensation", oxrmc.Activate ? "off" : "on");
                oxrmc.Activate = true;
            }
            else
            {
                oxrmc.Activate = false;
            }

                //oxrmc.Activate = speech.Said(["toggle motion compensation"], .8f);

                int maxKeyLen = stats.Keys.Max(k => k.Length) + 10;

            int i = 0;
            int j = 0;

            int cols = 4;

            foreach (var (k, v) in stats)
            {
                var c = j % (maxKeyLen * cols);
                if (c == 0)
                    i += 2;

                string paddedKey = k + ":";
                cons.Write(c, i + 4, $"{paddedKey} {v}");

                j += maxKeyLen;
            }
        }

        
    }
}