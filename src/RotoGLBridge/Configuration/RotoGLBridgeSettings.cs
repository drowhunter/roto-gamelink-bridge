namespace RotoGLBridge.Configuration
{
    public class RotoGLBridgeSettings
    {
        public YawVR.Link.DeviceInfo YawDevice { get; set; } = new(YawVR.Link.YawModel.Yaw2, "ROTOVR", "Roto VR");

        // Listen on all IPv4 interfaces using the standard YawVR ports.
        public YawVR.Link.Emulator.EmulatorOptions YawEmulator { get; } = new();
    }
}
