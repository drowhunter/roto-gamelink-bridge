using Microsoft.Extensions.Logging;

using RotoGLBridge.Plugins;

using Sharpie.Helpers;

namespace RotoGLBridge.Scripts
{
    public class Main(ILogger<Main> logger, GamelinkGlobal gamelink, RotoPluginGlobal roto) : SharpieScript
    {
        float yaw;
        int i = 0;
        float roll;
        int mode = 1;



        public override Task Start()
        {
            logger.LogInformation($"Main script started.");
            gamelink.OnUpdate += OnGameLinkUpdate;

            roto.switchMode(RotoModeType.FollowObject, () => yaw);

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
                logger.LogInformation($"Main script update: yaw={yaw}");
            }
            
        }
    }
}