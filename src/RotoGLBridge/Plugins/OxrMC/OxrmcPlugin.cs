using RotoGLBridge.Models;
using RotoGLBridge.Services;

using Sharpie.Engine.Contracts.Plugins;
using Sharpie.Helpers.Telemetry;


namespace RotoGLBridge.Plugins
{

    /// <summary>
    /// OxrmcPlugin integrates with the OXRMC shared-memory activity input (MmfTelemetry)
    /// and merges local hotkey state into the incoming activity bits before sending them back.
    /// </summary>
    /// <param name="logger">The logger to write informational and debug messages to.</param>
    [GlobalType(Type= typeof(OxrmcGlobal))]
    public class OxrmcPlugin(
        IEnumerable<IMmfSender> mmfSenders,
        ILogger<OxrmcPlugin> logger) : SharpiePlugin
    {
       
        /// <summary>
        /// Bits representing hotkeys that are currently pre-seeded by this process.
        /// These bits are OR'ed into the incoming activity trigger bits before they are sent back.
        /// </summary>
        public ActivityBit HotKeysPreseed;

        /// <summary>
        /// The memory-mapped-file telemetry object used to receive and send ActivityFlags structures.
        /// When not null it indicates an attempt to communicate with the OXRMC shared memory.
        /// </summary>
        private MmfTelemetry<ActivityFlags> mmf;

        /// <summary>
        /// Cancellation token source reserved for potential asynchronous operations or shutdown.
        /// Currently reserved for future use.
        /// </summary>
        CancellationTokenSource cts = new();

        /// <summary>
        /// Local copy of the activity flags structure used for temporary processing or debugging.
        /// </summary>
        public ActivityFlags activityFlags = new();

        /// <summary>
        /// Gets a value indicating whether the underlying memory-mapped telemetry is connected.
        /// </summary>
        /// <value>True when <see cref="mmf"/> is non-null and reports connected; otherwise false.</value>
        public bool IsConnected => true;

        /// <summary>
        /// Initializes and opens the shared-memory telemetry for reading activity bits.
        /// </summary>
        /// <remarks>
        /// This method attempts to construct and open a <see cref="MmfTelemetry{ActivityFlags}"/>
        /// instance pointing to the "OXRMC_ActivityInput" memory-mapped file. It logs the result
        /// but deliberately does not throw; callers rely on <see cref="IsConnected"/> to check status.
        /// </remarks>
        /// <returns>A completed <see cref="Task"/> when the initialization attempt is done.</returns>
        public override Task Start()
        {
            mmf = new MmfTelemetry<ActivityFlags>(new() { 
                Create = true, 
                Name = "OXRMC_ActivityInput" 
            }, new MarshalByteConverter<ActivityFlags>());

            
            if (mmf.IsConnected)
            {
                logger.LogInformation("OXRMC Activity Input MmfTelemetry opened successfully.");
                
                //return Task.CompletedTask;
            }
            else
            {
                logger.LogWarning("OXRMC Activity Input MmfTelemetry failed to open.");
                //return Task.FromException(new Exception("OXRMC Activity Input MmfTelemetry failed to open."));
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Stops the plugin and releases any resources used for shared-memory communication.
        /// </summary>
        /// <remarks>
        /// This will dispose the <see cref="mmf"/> telemetry instance and set the reference to null.
        /// The operation is synchronous and returns a completed task when finished.
        /// </remarks>
        /// <returns>A completed <see cref="Task"/> once shutdown is complete.</returns>
        public override Task Stop()
        {

            if (mmf != null)
            {

                mmf.Dispose();
                mmf = null;
            }

            return Task.CompletedTask;
        }


        public void SendMotionComp(SixDofTracker tracker)
        {
            mmfSenders.ToList().ForEach(sender => sender.Send(tracker));
        }

        /// <summary>
        /// Periodic execution entry invoked by the hosting framework.
        /// </summary>
        /// <remarks>
        /// When the memory-mapped telemetry is available this method:
        /// 1. Receives the current <see cref="ActivityFlags"/> from shared memory.
        /// 2. Copies the trigger bits into a local <see cref="ActivityBit"/> variable.
        /// 3. Clears bits that were confirmed by the external producer (confirm bits).
        /// 4. Applies the locally pre-seeded hotkey bits (<see cref="HotKeysPreseed"/>) by OR'ing them into the trigger.
        /// 5. Writes the modified trigger back into the activity structure and sends it back to shared memory.
        /// If the telemetry is not available, the local hotkey state is reset to zero.
        /// </remarks>
        public override void Execute()
        {
            
            if (mmf != null)
            {
                
                var activity = mmf.Receive();

                // Step 1 : Read the current activity bits from the shared memory into a local variable
                var temp = (ActivityBit)activity.trigger;
                var confirmed = (ActivityBit)activity.confirm;

                logger.LogDebug("OXRMC Activity Input: {trigger}", temp);

                // Step 2 : Apply Confirmed bits to the local variable
                temp &= ~confirmed;

                logger.LogDebug("OXRMC Confirm Bits: {confirmed}", confirmed);
                logger.LogDebug("trigger &= ~ confirmed = {temp}", temp);

                //Step 3: Apply the HotKeysPresseed bits to the local variable
                temp |= HotKeysPreseed;
                logger.LogDebug("HotKeysPreseed: {activity}", HotKeysPreseed);
                logger.LogDebug("temp |= HotKeysPreseed = {temp}", temp);

                // Step 4: Write the modified bits back to the shared memory
                activity.trigger = (ulong)temp;
                mmf.Send(activity);



            }
            else
            {
                HotKeysPreseed = 0;
            }
        }        
    }

    /// <summary>
    /// Global facade exposing a simplified set of boolean properties that map to individual <see cref="ActivityBit"/> flags.
    /// </summary>
    /// <remarks>
    /// Instances of this global type are registered via <see cref="GlobalTypeAttribute"/> on <see cref="OxrmcPlugin"/>.
    /// Properties read and update the plugin's <see cref="OxrmcPlugin.HotKeysPreseed"/> bitmask using the helper <see cref="SetActivityBit"/>.
    /// </remarks>
    public class OxrmcGlobal : SharpieGlobal<OxrmcPlugin>
    {
        /// <summary>
        /// Gets whether the underlying plugin has an active connection to the OXRMC telemetry.
        /// </summary>
        public bool IsConnected => plugin.IsConnected;

        /// <summary>
        /// Helper that sets or clears a specific <see cref="ActivityBit"/> in the plugin's <see cref="OxrmcPlugin.HotKeysPreseed"/>.
        /// </summary>
        /// <param name="bit">The <see cref="ActivityBit"/> to set or clear.</param>
        /// <param name="value">True to set the bit; false to clear it.</param>
        private void SetActivityBit(ActivityBit bit, bool value)
        {
            if (plugin != null)
            {
                if (value)
                    plugin.HotKeysPreseed |= bit;
                else
                    plugin.HotKeysPreseed &= ~bit;
            }
        }

        /// <summary>
        /// Gets or sets the Activate bit in the plugin hotkey pre-seed mask.
        /// </summary>
        public bool Activate
        {
            get => ((plugin?.HotKeysPreseed ?? 0) & ActivityBit.Activate) != 0;
            set => SetActivityBit(ActivityBit.Activate, value);
        }

        /// <summary>
        /// Gets or sets the Calibrate bit in the plugin hotkey pre-seed mask.
        /// </summary>
        /// <remarks>
        /// Toggling this property will set or clear the <see cref="ActivityBit.Calibrate"/> flag within <see cref="OxrmcPlugin.HotKeysPreseed"/>.
        /// </remarks>
        public bool Calibrate
        {
            get => ((plugin?.HotKeysPreseed ?? 0) & ActivityBit.Calibrate) != 0;
            set => SetActivityBit(ActivityBit.Calibrate, value);
        }

        /// <summary>
        /// Gets or sets the SaveConfig bit in the plugin hotkey pre-seed mask.
        /// </summary>
        public bool SaveConfig
        {
            get => ((plugin?.HotKeysPreseed ?? 0) & ActivityBit.SaveConfig) != 0;
            set => SetActivityBit(ActivityBit.SaveConfig, value);
        }

        /// <summary>
        /// Gets or sets the SaveConfigPerApp bit in the plugin hotkey pre-seed mask.
        /// </summary>
        public bool SaveConfigPerApp
        {
            get => ((plugin?.HotKeysPreseed ?? 0) & ActivityBit.SaveConfigPerApp) != 0;
            set => SetActivityBit(ActivityBit.SaveConfigPerApp, value);
        }

        /// <summary>
        /// Gets or sets the CrosshairToggle bit in the plugin hotkey pre-seed mask.
        /// </summary>
        public bool CrosshairToggle
        {
            get => ((plugin?.HotKeysPreseed ?? 0) & ActivityBit.CrosshairToggle) != 0;
            set => SetActivityBit(ActivityBit.CrosshairToggle, value);
        }

        /// <summary>
        /// Gets or sets the StabilizerToggle bit in the plugin hotkey pre-seed mask.
        /// </summary>
        public bool StabilizerToggle
        {
            get => ((plugin?.HotKeysPreseed ?? 0) & ActivityBit.StabilizerToggle) != 0;
            set => SetActivityBit(ActivityBit.StabilizerToggle, value);
        }

        public void SendMotionComp(float yaw, float pitch = 0, float roll = 0, float x = 0, float y = 0, float z = 0) 
        {
            // i might be wrong about sway surge and heave mappings
            plugin.SendMotionComp(new SixDofTracker { yaw = yaw, pitch = pitch, roll = roll, sway = x, surge = y, heave = z });
        }
    }
}
