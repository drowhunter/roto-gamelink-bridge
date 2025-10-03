using RotoGLBridge.Services;

using Sharpie.Plugins.UsbWatcher;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RotoGLBridge.Scripts
{
    internal class UsbWatcherTest(UsbWatcherGlobal usb, IConsoleWatcher cons) : SharpieScript
    {
        override public async Task Start()
        {
            usb.OnDeviceChange += Usb_OnDeviceChange;

            usb.Watch(0x045e, 0x0b13); // Xbox Controller
        }

       
        private void Usb_OnDeviceChange(VidPid pid, bool plugged)
        {
            //cons.Watch("Device", pid.ToString() + (plugged ? " PLUGGED": " UNPLUGGED"));
            cons.Watch(usb.WatchedDevices.ToDictionary(kv => kv.Key.ToString(), kv => (object)kv.Value));
            cons.Publish();
        }
    }
}
