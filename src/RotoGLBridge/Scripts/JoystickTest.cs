using com.rotovr.sdk;

using RotoGLBridge.Plugins;
using RotoGLBridge.Services;

using Sharpie.Helpers.Core;
using Sharpie.Plugins.SharpDX;

namespace RotoGLBridge.Scripts
{
    internal class JoystickTest(
        Xbox360GlobalIndexer xbox360,
        IConsoleWatcher cons,
        RotoPluginGlobal roto
        ) : SharpieScript
    {

        float? yaw = 0f;

        public override async Task Start()
        {
            roto.SetPower(1);
            await roto.SwitchModeAsync(ModeType.FollowObject, () => yaw);

            
        }

        public override void Update()
        {
            var x = xbox360[0].LeftStickX;
            var y = xbox360[0].LeftStickY;

            var (ang, mag) = Maths.RectToPolar(x, y);

            yaw = mag > 0.8 ? ang : null;
            Watch(4);
        }

        private void Watch(int cols = 4)
        {
            cons.Watch("Yaw", yaw);
            cons.Watch("LeftStickX",  xbox360[0].LeftStickX);           
            cons.Watch("LeftStickY",  xbox360[0].LeftStickY);
            cons.Watch("RightStickX", xbox360[0].RightStickX);
            cons.Watch("RightStickY", xbox360[0].RightStickY);
            cons.Watch("ButtonA",     xbox360[0].A);
            cons.Watch("ButtonB",     xbox360[0].B);
            cons.Watch("ButtonX",     xbox360[0].X);
            cons.Watch("ButtonY",     xbox360[0].Y);

            cons.Publish();
        }
    }
}
