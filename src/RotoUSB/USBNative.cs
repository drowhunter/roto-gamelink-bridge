using System;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;


namespace rotoUSB
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct OVERLAPPED
    {
        public IntPtr Internal;
        public IntPtr InternalHigh;
        public uint Offset;
        public uint OffsetHigh;
        public IntPtr hEvent;
    }

    // Static class for interacting with USB-HID devices using native Windows API calls
    public class USBNative : IUSBNative
    {
        // USB-HID device Vendor ID (VID) and Product ID (PID) for the Roto device
        public const int VID = 0x04D9;
        public const int PID = 0xB564;

        // Length of a USB HID report (33 bytes)
        public const int USB_REPORT_LEN = 33;

        // Effective data length for a USB packet (19 bytes, aligned for 20-byte BLE packet)
        public const int ROTO_PACKET_LEN = 19;

        


        // Stores the latest USB-related error message
        public string LastErrorMessage { get; private set; }


        // =============== DLL import  ===============
        [DllImport("HIDApi.dll")]
        static extern IntPtr OpenFirstHIDDevice(UInt32 dwVID, UInt32 dwPID, UInt32 dwUsagePage, UInt32 dwUsage, bool bSync);


        [DllImport("HIDApi.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        static extern IntPtr OpenNextHIDDevice(UInt32 dwVID, UInt32 dwPID, UInt32 dwUsagePage, UInt32 dwUsage, bool bSync);


        [DllImport("HIDApi.dll")]
        private static extern void CloseHIDDevice(IntPtr device);

        [DllImport("HIDApi.dll")]
        private static extern bool SetFeature(IntPtr device, byte[] pData, ushort length);

        [DllImport("HIDApi.dll")]
        private static extern bool GetFeature(IntPtr device, byte[] pData, ushort length);



        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadFile(IntPtr hFile, IntPtr lpBuffer, uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead, ref OVERLAPPED lpOverlapped);


        internal static bool ReadFileInternal(IntPtr hFile, IntPtr lpBuffer, uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead, ref OVERLAPPED lpOverlapped)
        {
            return ReadFile(hFile, lpBuffer, nNumberOfBytesToRead, out lpNumberOfBytesRead, ref lpOverlapped);
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool ReadFile(IntPtr hFile, [Out] byte[] lpBuffer, uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead, IntPtr overlapped);


        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool WriteFile(IntPtr hFile, byte[] buffer, uint nNumberOfBytesToWrite, out uint lpNumberOfBytesWritten, IntPtr overlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr CreateEvent(IntPtr lpEventAttributes, bool bManualReset, bool bInitialState, string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool GetOverlappedResult(IntPtr hFile, ref OVERLAPPED lpOverlapped, out uint lpNumberOfBytesTransferred, bool bWait);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool CancelIoEx(IntPtr hFile, ref OVERLAPPED lpOverlapped);




        [DllImport("kernel32")]
        private static extern uint GetLastError();



        [DllImport("kernel32.dll")]
        static extern bool FormatMessage(uint dwFlags, IntPtr lpSource, uint dwMessageId, uint dwLanguageId, StringBuilder lpBuffer, int nSize, IntPtr Arguments);


        // Constant for formatting system error messages
        const uint FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;

        // =====================================================


        // Tracks whether the HIDApi.dll library has been loaded
        private static bool _isLibaryLoaded = false;

        // Lock objects to synchronize USB read and write operations
        private static readonly object _lockUSBR = new object();
        private static readonly object _lockUSBW = new object();



        // for internal use 
        // Performs a speed test for USB communication at 115200 baud rate
        public void USBSpeedTest()
        {
            bool success = false;
            //                             0       1      2   3      4    5(stop)  6(Parity)     7     8
            byte[] FeatureReportData = { 0x00, 0x01, 0x00, 0xC2, 0x01, 0x00, 0x00, 0x00, 0x08 };
            // USB HID output report buffer
            byte[] sendPacket = new byte[USB_REPORT_LEN];
            uint bytesWritten = 0;


            Byte[] szData = new Byte[USB_REPORT_LEN];


            Console.WriteLine("\n\n----------- Native USB Test for 115200 baud rate ----------");
            // Open USB Device
            IntPtr usbDevice = OpenFirstHIDDevice(0x04D9, 0xB564, 0x00, 0x00, true);
            if (usbDevice == IntPtr.Zero)
            {
                Console.WriteLine("USB-HID Device not found!");
                return;
            }
            else
            {
                // Set USB-HID to 115200 maximium baud rate
                uint baudrate = 115200; // 9600;


                FeatureReportData[0] = 0;
                FeatureReportData[1] = 0x01;    // command code
                FeatureReportData[2] = (byte)(baudrate & 0x00ff);
                FeatureReportData[3] = (byte)((baudrate >> 8) & 0xff);
                FeatureReportData[4] = (byte)((baudrate >> 16) & 0xff);
                FeatureReportData[5] = (byte)((baudrate >> 24) & 0xff);
                FeatureReportData[6] = 0;     // 1 stop bit
                FeatureReportData[7] = 0;     // parity none
                FeatureReportData[8] = 0x08;

                success = SetFeature(usbDevice, FeatureReportData, (ushort)FeatureReportData.Length);

                Console.WriteLine("Native USB: " + ToHexString(FeatureReportData, 0, FeatureReportData.Length));
                if (!success)
                {
                    Console.WriteLine("Cannot set feature report");
                    return;
                }

                uint dw = 0;


                int numPacket = 400;

                // Send  Connect commands
                szData[0] = 0;
                szData[1] = ROTO_PACKET_LEN; // 19 bytes data
                szData[2] = 0xF1;
                szData[3] = 0x41;
                szData[20] = 0x32;


                DateTime curTime = DateTime.Now;
                for (int i = 0; i < numPacket; i++)
                {

                    bool result = WriteFile(usbDevice, szData, USB_REPORT_LEN, out dw, IntPtr.Zero);
                    if (!result)
                        Console.WriteLine("Write Error");
                }

                TimeSpan duration = DateTime.Now.Subtract(curTime);
                curTime = DateTime.Now;
                Console.WriteLine(" " + duration.TotalSeconds + "s  - write " + numPacket + " x 32  byte packet");

                CloseHIDDevice(usbDevice);
            }

        }


        // Retrieves the last I/O error message from the system
        private static string GetIOError()
        {
            uint errorCode = GetLastError();
            StringBuilder messageBuffer = new StringBuilder(512);

            FormatMessage(
                FORMAT_MESSAGE_FROM_SYSTEM,
                IntPtr.Zero,
                errorCode,
                0, // Default language
                messageBuffer,
                messageBuffer.Capacity,
                IntPtr.Zero);

            return messageBuffer.ToString();
        }


        // constructor to initialize the LastErrorMessage
        public USBNative()
        {
            LastErrorMessage = "";
        }


        // Loads the HIDApi.dll library based on the process architecture (x86 or x64)
        public bool LoadLibrary()
        {

            bool success = false;


            if (!_isLibaryLoaded)
            {

                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string platformFolder = Environment.Is64BitProcess ? "Libs\\x64" : "Libs\\x86";
                string dllFullPath = Path.Combine(basePath, platformFolder, "HIDApi.dll");
                //  Console.WriteLine(dllFullPath);

                if (File.Exists(dllFullPath))
                {
                    try
                    {

                        success = true;
                        // Update DLL search path for unmanaged calls
                        NativeLibrary.SetDllImportResolver(typeof(USBNative).Assembly, (libraryName, assembly, searchPath) =>
                        {
                            IntPtr handle = IntPtr.Zero;
                            if (libraryName == "HIDApi.dll")
                            {
                              NativeLibrary.TryLoad(dllFullPath, out handle);
                                _isLibaryLoaded = true;
                            }
                            return handle;
                        });
                    }
                    catch (Exception ex)
                    {
                        success = false;
                        LastErrorMessage = "HIDApi.dll import error: " + ex.Message;
                    }
                }
                else
                {
                    LastErrorMessage = "HIDApi.dll is not found: " + dllFullPath;
                }


            }
            else
            {
                success = true;
            }

            return success;
        }


        // Opens the first available USB-HID device with the specified VID and PID
        public IntPtr OpenUSBDevice()
        {
            IntPtr usbDevice = IntPtr.Zero;

            // synchronous mode
            usbDevice = OpenFirstHIDDevice(VID, PID, 0x00, 0x00, true);

            if (usbDevice == IntPtr.Zero)
            {
                LastErrorMessage = "Roto USB Device not found!";
            }

            return usbDevice;
        }


        // Configures the USB-HID device to operate at 115200 baud rate 
        public bool ConfigUSBDevice(IntPtr usbDevice)
        {
            bool success = false;
            byte[] FeatureReportData = { 0x00, 0x01, 0x00, 0xC2, 0x01, 0x00, 0x01, 0x00, 0x08 };

            if (usbDevice != IntPtr.Zero)
            {
                // Set USB-HID to 115200 maximium baud rate
                success = SetFeature(usbDevice, FeatureReportData, (ushort)FeatureReportData.Length);
            }

            if (success == false)
            {
                LastErrorMessage = "Configure USB device feature report error!";
            }
            return success;
        }






        // Writes a data packet to the USB-HID device, ensuring proper packet formatting
        public bool WritePacket(IntPtr handle, byte[] data)
        {
            bool success = false;

            if (data.Length != ROTO_PACKET_LEN)
            {
                LastErrorMessage = "Error: WritePacket length must equal to :" + ROTO_PACKET_LEN;
            }
            else
            {
                // USB HID output report buffer
                byte[] sendPacket = new byte[USB_REPORT_LEN];

                // convert roto packet comamnd into USB-HID write report packet
                Array.Clear(sendPacket, 0, USB_REPORT_LEN);
                sendPacket[0] = 0x00; // HID report ID
                sendPacket[1] = (byte)ROTO_PACKET_LEN; // Packet length
                Array.Copy(data, 0, sendPacket, 2, ROTO_PACKET_LEN);

                success = WriteHIDPacket(handle, sendPacket);
            }
            return success;
        }


        // Converts a byte array to a hexadecimal string for debugging or logging
        public string ToHexString(byte[] b, long offset, long size)
        {
            string hexStr = string.Empty;
            for (long i = offset; i < offset + size; i++)
                hexStr += " " + b[i].ToString("X2");
            return hexStr;
        }


        // Low-level method to write a USB-HID report packet to the device
        private bool WriteHIDPacket(IntPtr handle, byte[] data)
        {
            bool success = false;
            uint bytesWritten = 0;

            if (handle != IntPtr.Zero && data != null && data.Length >= USB_REPORT_LEN)
            {
                lock (_lockUSBW)
                {
                    success = WriteFile(handle, data, (uint)USB_REPORT_LEN, out bytesWritten, IntPtr.Zero);
                }

                if (!success)
                {
                    LastErrorMessage = GetIOError();
                }
            }
            else
            {
                LastErrorMessage = "Write USB-HID packet error! Either handle is null or data packet length mismatch  ";
            }

            return success;
        }




        // Reads a USB-HID input report from the device
        //public bool ReadHIDPacket(IntPtr handle, byte[] data, int length)
        //{
        //    bool success = false;
        //    uint bytesRead = 0;

        //    if (handle != IntPtr.Zero && data != null && data.Length >= USB_REPORT_LEN)
        //    {
        //        lock (_lockUSBR)
        //        {
        //            try
        //            {
        //                success = ReadFile(handle, data, (uint)USB_REPORT_LEN, out bytesRead, IntPtr.Zero);
        //            }
        //            catch (Exception ex)
        //            {
        //                success = false;
        //                LastErrorMessage = "ReadFile exception: " + ex.Message;
        //            }
        //        }
        //        if (!success)
        //            LastErrorMessage = GetIOError();
        //    }
        //    else
        //    {
        //        LastErrorMessage = "Read USB packet error! Either handle is null or  data buffer is not allocated ";
        //    }


        //    return success;
        //}

        
        public bool ReadHIDPacket(IntPtr handle, byte[] data, int length)
        {
            bool success = false;
            uint bytesRead = 0;

            //GCHandle pinnedBuffer = GCHandle.Alloc(data, GCHandleType.Pinned);
            //IntPtr bufferPtr = pinnedBuffer.AddrOfPinnedObject();

            //OVERLAPPED ov = new OVERLAPPED
            //{
            //    hEvent = USBNative.CreateEvent(IntPtr.Zero, true, false, null)
            //};


            if (handle == IntPtr.Zero)
            {
                LastErrorMessage = "Read USB packet error! Handle is null.";
                return false;
            }
            if (data == null || data.Length < USB_REPORT_LEN)
            {
                LastErrorMessage = "Read USB packet error! Data buffer is not allocated or too small.";
                return false;
            }

            lock (_lockUSBR)
            {
                try
                {

                    success = ReadFile(handle, data, (uint)USB_REPORT_LEN, out bytesRead, IntPtr.Zero);
                    //success = ReadFileInternal(handle, bufferPtr, (uint)USB_REPORT_LEN, out bytesRead, ref ov);
                    if (!success)
                    {
                        LastErrorMessage = GetIOError();
                    }
                    else if (bytesRead == 0)
                    {
                        LastErrorMessage = "No data read from USB device.";
                        success = false;
                    }
                }
                catch (IOException ioEx)
                {
                    LastErrorMessage = $"ReadFile IOException: {ioEx.Message}";
                    success = false;
                }
                catch (Exception ex)
                {
                    LastErrorMessage = $"ReadFile exception: {ex.Message}";
                    success = false;
                }
                catch
                {
                    LastErrorMessage = "ReadFile encountered a corrupted state exception.";
                    var e = Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
                    if (e != null)
                    {
                        LastErrorMessage += $" Exception: {e.Message}";
                    }
                    success = false;
                }
            }

            return success;
        }


        // Closes the USB-HID device connection
        public void CloseUSBDevice(IntPtr device)
        {
            if (device != IntPtr.Zero)
            {
                CloseHIDDevice(device);
            }
        }


    }
}
