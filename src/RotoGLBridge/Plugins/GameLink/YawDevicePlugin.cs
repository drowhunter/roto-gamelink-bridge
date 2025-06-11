using Sharpie.Engine.Contracts.Plugins;
using Sharpie.Extras.Telemetry;
using Sharpie.Helpers.Telemetry.Convertors;

namespace RotoGLBridge.Plugins.GameLink
{
    [GlobalType(Type = typeof(YawDeviceGlobal))]
    public class YawDevicePlugin() : UpdateablePlugin
    {
        private TcpTelemetry<byte[]> tcp;

        private CancellationTokenSource _cancellationTokenSource;

        internal bool IsConnected => tcp?.IsConnected ?? false;

        public byte[] Data { get; set; }

        public override void Execute()
        {
           
        }

        public override Task Start()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            tcp = new TcpTelemetry<byte[]>(new ()
            {
                IpAddress = new System.Net.IPEndPoint(System.Net.IPAddress.Any, 50020)
            }, new RawBytesConverter());

            _ = tcp.BeginAsync(_cancellationTokenSource.Token);

            tcp.OnReceiveAsync += (sender,data) =>
            {
                Data = data;
                OnUpdate(); 
            };

            return Task.CompletedTask;
        }



        public override Task Stop()
        {
            _cancellationTokenSource.Cancel();

            try
            {
                tcp?.Dispose();
            }
            catch
            { /* ignore */

            }
            return Task.CompletedTask;
        }
    }

    public class YawDeviceGlobal : UpdateablePluginGlobal<YawDevicePlugin>
    {
        public byte Command => plugin.Data.FirstOrDefault();

        public bool IsConnected => plugin.IsConnected;
    }
}
