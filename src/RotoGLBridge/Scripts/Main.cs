using Microsoft.Extensions.Logging;

using RotoGLBridge.Plugins;
using RotoGLBridge.Plugins.GameLink;
using RotoGLBridge.Services;

using Sharpie.Helpers;

using System.Text.Json;
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



        public override Task Start()
        {
            logger.LogInformation($"Main script started.");
            gamelink.OnUpdate += OnGameLinkUpdate;

            var options = new JsonSerializerOptions { WriteIndented = false };
            options.Converters.Add(new JsonStringEnumConverter());

            roto.switchMode(RotoModeType.FollowObject, () => {

                return yaw;
            });

            yawDevice.OnUpdate += () =>
            {
                cons.Write(0, 12, $"cmd: {yawDevice.Command}");
            };

            return Task.CompletedTask;
        }

        private void OnGameLinkUpdate()
        {
            yaw = gamelink.yaw;

            var r = Filters.EnsureMapRange(gamelink.roll, -40, 40, -1, 1);
            roll = r > 180 ? r - 360 : r;
        }

        

        public override void Update()
        {
            i++;
            
            cons.Write(0, 10, $"Yaw: {yaw}");
            cons.Write(20,10,$"ConnectionStatus: {roto.connectionStatus}");

            cons.Write(0, 11, roto.ToString() );

            

        }

        
    }
}