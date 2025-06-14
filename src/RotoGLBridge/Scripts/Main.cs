using com.rotovr.sdk;

using Microsoft.Extensions.Logging;

using RotoGLBridge.Plugins;
using RotoGLBridge.Plugins.GameLink;
using RotoGLBridge.Services;

using Sharpie.Helpers;

using System.Text.Json.Serialization;

namespace RotoGLBridge.Scripts
{
    public class Main(
        ILogger<Main> logger, 
        GamelinkGlobal gamelink, 
        RotoPluginGlobal roto,
        YawDeviceGlobal yawDevice,
        //YawVRGlobal yawVr,
        IConsoleWatcher cons ) : SharpieScript
    {
        float yaw;
        int i = 0;
        float roll;
        int mode = 1;



        public override async Task Start()
        {
            logger.LogInformation($"Main script started.");
            gamelink.OnUpdate += OnGameLinkUpdate;

            var options = new JsonSerializerOptions { WriteIndented = false };
            options.Converters.Add(new JsonStringEnumConverter());

            await roto.SwitchModeAsync(ModeType.FollowObject, () => {

                return yaw;
            });

            roto.SetPower(.6f);

            

            yawDevice.OnUpdate += () =>
            {
                cons.Write(0, 12, $"tcp: {yawDevice.Command}");
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
            Dictionary<string, string> stats = new()
            {
                { "Status",  roto.Status},
                { "Yaw" , yaw.ToString("F1")},
                { "Mode", roto.Data.Mode.ToString() },
                { "Angle", roto.Data.LerpedAngle.ToString("F2") },
                { "CalibratedAngle", roto.Data.CalibratedAngle.ToString("F2") },
                { "Delta", roto.Telemetry.Delta.ToString("F1") },
                { "Target", roto.Telemetry.TargetAngle.ToString("F1") }
            };

            int maxKeyLen = stats.Keys.Max(k => k.Length)+ 10;

            int i = 0;
            int j = 0;

            foreach (var (k, v) in stats)
            {
                var c = j % (maxKeyLen * 3);
                if (c == 0)
                    i += 2;

                string paddedKey = k + ":";
                cons.Write(c, i + 4, $"{paddedKey} {v}");

                j += maxKeyLen;
            }
        }

        
    }
}