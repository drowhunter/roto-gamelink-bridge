using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace com.rotovr.sdk
{
    static class Native
    {
        #region Native Methods

        [DllImport("HIDApi.dll")]
        internal static extern IntPtr OpenFirstHIDDevice(ushort vid, ushort pid, ushort usagePage = 0, ushort usage = 0,
            bool sync = true);

        [DllImport("HIDApi.dll")]
        internal static extern void CloseHIDDevice(IntPtr device);

        [DllImport("HIDApi.dll")]
        internal static extern bool SetFeature(IntPtr device, byte[] pData, ushort length);

        [DllImport("HIDApi.dll")]
        internal static extern bool GetFeature(IntPtr device, byte[] pData, ushort length);

        [DllImport("kernel32.dll")]
        internal static extern bool ReadFile(IntPtr hFile, IntPtr lpBuffer, uint nNumberOfBytesToRead,
            out uint lpNumberOfBytesRead, [In] ref NativeOverlapped lpOverlapped);

        [DllImport("kernel32.dll")]
        internal static extern bool WriteFile(IntPtr hFile, byte[] lpBuffer, uint nNumberOfBytesToWrite,
            out uint lpNumberOfBytesWritten, [In] ref NativeOverlapped lpOverlapped);

        #endregion



        internal static bool ReadFile(IntPtr handle, out byte[] data, int length)
        {
            data = new byte[length];
            bool success = false;
            IntPtr nonManagedBuffer = Marshal.AllocHGlobal(data.Length);
            uint bytesRead;
            try
            {
                var overlapped = new NativeOverlapped();
                
                var result = ReadFile(handle, nonManagedBuffer, (ushort)data.Length, out bytesRead, ref overlapped);

                if (result)
                {
                    Marshal.Copy(nonManagedBuffer, data, 0, (int)bytesRead);
                    success = true;
                }
            }
            catch
            {
                success = false;
            }
            finally
            {
                Marshal.FreeHGlobal(nonManagedBuffer);
            }

            return success;
        }

        internal static bool WriteFile(IntPtr handle, byte[] data)
        {
            uint bytesWritten;
            var overlapped = new NativeOverlapped();
            var result = WriteFile(handle, data, (uint)data.Length, out bytesWritten, ref overlapped);
            return result;
        }

        #region Async Methods

        internal static Task<IntPtr> OpenFirstHIDDeviceAsync(ushort vid, ushort pid, ushort usagePage = 0, ushort usage = 0, bool sync = true)
        {
            return Task.Run(() =>
            {
                return OpenFirstHIDDevice(vid, pid, usagePage, usage, sync);
            });
        }

        internal static Task CloseHIDDeviceAsync(IntPtr device)
        {
            return Task.Run(() =>
            {
                CloseHIDDevice(device);
            });
        }

        internal static Task<bool> GetFeatureAsync(IntPtr device, byte[] data)
        {
            return Task.Run(() =>
            {
                return GetFeature(device, data, (ushort)data.Length);
            });
        }

        internal static Task<bool> SetFeatureAsync(IntPtr device, byte[] data)
        {
            return Task.Run(() =>
            {
                return SetFeature(device, data, (ushort)data.Length);
            });
        }

        internal static Task<bool> ReadFileAsync(IntPtr handle, byte[] data, int length)
        {
            return Task.Run(() =>
            {
                return ReadFile(handle, out data, length);
            });
        }

        internal static Task<bool> WriteFileAsync(IntPtr handle, byte[] data)
        {
            return Task.Run(() =>
            {
                return WriteFile(handle, data);
            });
        }

        #endregion
    }
}