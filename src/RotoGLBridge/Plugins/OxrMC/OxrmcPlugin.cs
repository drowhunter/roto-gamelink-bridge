using Sharpie.Engine.Contracts.Plugins;
using Sharpie.Helpers.Telemetry;


namespace RotoGLBridge.Plugins
{

    [GlobalType(Type= typeof(OxrmcGlobal))]
    public class OxrmcPlugin(ILogger<OxrmcPlugin> logger) : SharpiePlugin
    {

        public ActivityBit HotKeysPreseed;

        private MmfTelemetry<ActivityFlags> mmf;

        CancellationTokenSource cts = new();

        public ActivityFlags activityFlags = new();

        public override async Task Start()
        {
            mmf = new MmfTelemetry<ActivityFlags>(new() { Create = false, Name = "OXRMC_ActivityInput" }, new MarshalByteConverter<ActivityFlags>());

            //var res = await mmf.TryOpenAsync(0, cts.Token);

            //logger.LogInformation("OXRMC Activity Input MmfTelemetry opened: {result}", res);
        }

        public override Task Stop()
        {

            if (mmf != null)
            {

                mmf.Dispose();
                mmf = null;
            }

            return Task.CompletedTask;
        }


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
                



            }
            else
            {
                HotKeysPreseed = 0;
            }
        }        
    }

    public class OxrmcGlobal : SharpieGlobal<OxrmcPlugin>
    {
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

        public bool Activate
        {
            get => ((plugin?.HotKeysPreseed ?? 0) & ActivityBit.Activate) != 0;
            set => SetActivityBit(ActivityBit.Activate, value);
        }

        // Example for another property:
        public bool Calibrate
        {
            get => ((plugin?.HotKeysPreseed ?? 0) & ActivityBit.Calibrate) != 0;
            set => SetActivityBit(ActivityBit.Calibrate, value);
        }

        // Add more properties for other ActivityBit values as needed, using SetActivityBit
        public bool SaveConfig
        {
            get => ((plugin?.HotKeysPreseed ?? 0) & ActivityBit.SaveConfig) != 0;
            set => SetActivityBit(ActivityBit.SaveConfig, value);
        }

        public bool SaveConfigPerApp
        {
            get => ((plugin?.HotKeysPreseed ?? 0) & ActivityBit.SaveConfigPerApp) != 0;
            set => SetActivityBit(ActivityBit.SaveConfigPerApp, value);
        }
    }
}
