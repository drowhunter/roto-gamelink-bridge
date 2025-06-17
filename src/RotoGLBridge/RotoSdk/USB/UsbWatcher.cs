using System.Management;

namespace com.rotovr.sdk
{
    public interface IUsbWatcher : IDisposable
    {
        event Action<PluggedStatus> OnPluggedStatus;

        Task WatchAsync(ushort vid, ushort pid);
    }

    internal class UsbWatcher(ILogger<UsbWatcher> logger) : IUsbWatcher
    {
        public event Action<PluggedStatus> OnPluggedStatus;

        private ManagementEventWatcher _watcher;

        public async Task WatchAsync(ushort vid, ushort pid)
        {
            InvokeIfUsbDeviceIsPlugged(vid, pid);

            if (_watcher != null)
            {
                logger.LogWarning("WMI watcher is already running. Dispose it before starting a new one.");
                return;
            }

            _watcher = StartWatching(vid, pid);
            
        }

        private ManagementEventWatcher StartWatching(ushort vid, ushort pid)
        {
            var query = $"SELECT * FROM __InstanceOperationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBControllerDevice'";
            
            var watcher = new ManagementEventWatcher(query);

            watcher.EventArrived += (sender, args) =>
            {
                try
                {
                    var instance = args.NewEvent["TargetInstance"] as ManagementBaseObject;
                    if (instance == null) return;

                    var dependent = instance["Dependent"]?.ToString();
                    if (dependent == null) return;

                    if (dependent.Contains($"VID_{vid:X4}") && dependent.Contains($"PID_{pid:X4}"))
                    {
                        if (args.NewEvent.ClassPath.ClassName == "__InstanceCreationEvent")
                        {
                            OnPluggedStatus?.Invoke(PluggedStatus.Plugged);
                        }
                        else if (args.NewEvent.ClassPath.ClassName == "__InstanceDeletionEvent")
                        {
                            OnPluggedStatus?.Invoke(PluggedStatus.Unplugged);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in WMI USB device monitoring.");
                }
            };

            try
            {
                watcher.Start();




                //await AwaitCancellationAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to start WMI watcher.");
                return null;
            }

            return watcher;
        }

        private async Task AwaitCancellationAsync(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Run(() =>
                {
                    try
                    {
                        // Wait until cancellation is requested
                        cancellationToken.WaitHandle.WaitOne();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error while waiting for cancellation.");
                    }
                }, cancellationToken);
            }
            finally
            {
                // Stop and dispose watcher if cancellation is requested
                Dispose();
            }
        }

        private void InvokeIfUsbDeviceIsPlugged(ushort vid, ushort pid)
        {
            var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_USBControllerDevice");

            foreach (var obj in searcher.Get())
            {
                var dependent = obj["Dependent"]?.ToString();
                if (dependent != null &&
                    dependent.Contains($"VID_{vid:X4}") &&
                    dependent.Contains($"PID_{pid:X4}"))
                {
                    OnPluggedStatus?.Invoke(PluggedStatus.Plugged);
                    break;
                }
            }
        }

        public void Dispose()
        {
            // Dispose of the ManagementEventWatcher if it is not null
            if (_watcher != null)
            {
                _watcher.Stop(); // Stop the watcher before disposing
                _watcher.Dispose(); // Dispose of the watcher
                _watcher = null; // Set to null to avoid dangling references
            }
        }
    }
}
