namespace rotoUSB
{
    public interface IUSBNative
    {
        string LastErrorMessage { get; }

        void CloseUSBDevice(nint device);
        bool ConfigUSBDevice(nint usbDevice);
        bool LoadLibrary();
        nint OpenUSBDevice();
        bool ReadHIDPacket(nint handle, byte[] data, int length);
        string ToHexString(byte[] b, long offset, long size);
        //void USBSpeedTest();
        bool WritePacket(nint handle, byte[] data);
    }
}
