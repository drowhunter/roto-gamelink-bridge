using Microsoft.Extensions.Logging;

using RotoGLBridge.Plugins;
using RotoGLBridge.Plugins.YawEmu;
using RotoGLBridge.Services;

using Sharpie.Helpers;

using System.Security;

namespace RotoGLBridge.Scripts
{
    public class Main(
        ILogger<Main> logger, 
        GamelinkGlobal gamelink, 
        //RotoPluginGlobal roto,
        YawVRGlobal yawVr,
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
            yawVr.OnUpdate += () =>
            {
                cons.Write(0, 12, $"tcp: {yawVr.Data.Command} [{string.Join(':',yawVr.Data.Buffer)}]");
            };
            //roto.switchMode(RotoModeType.FollowObject, () => yaw);

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
            
            if (i % 10 == 0)
            {
                //logger.LogInformation($"Main script update: yaw={yaw}");
                cons.Write(0, 10, $"Yaw: {yaw}");
            }
            
        }

        
    }
}