using Sharpie.Engine.Contracts.Plugins;
using Sharpie.Extras.Telemetry;

using System.Net;

namespace RotoGLBridge.Plugins.YawEmu
{
    [GlobalType(Type = typeof(YawVRGlobal))]
    public class YawVRPlugin : UpdateablePlugin
    {
        CancellationTokenSource _cancellationTokenSource;

        TcpTelemetry<YawData> tcp;

        public YawData Data { get; private set; } = new();

        public override void Execute()
        {
            //throw new NotImplementedException();
        }

        public override Task Start()
        {
            TcpTelemetryConfig config = new()
            {

                IpAddress = new IPEndPoint(IPAddress.Loopback, 50020)
            };
            
            tcp = new TcpTelemetry<YawData>(config) { Convert = new YawTcpCommandConvertor() };
            tcp.OnReceiveAsync += OnReceiveAsync;


            _cancellationTokenSource = new CancellationTokenSource();

            Task.Factory.StartNew(() =>
            {
                while(!_cancellationTokenSource.IsCancellationRequested)
                {
                    try
                    {
                        if (tcp.IsConnected)
                        {
                            tcp.Receive();
                        }
                    }
                    catch (Exception ex)
                    {
                        //Log($"Error receiving data: {ex.Message}");
                    }
                    //Task.Delay(100, _cancellationTokenSource.Token).Wait(); // Wait for 100ms before next receive attempt
                }
            }, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        
            return Task.CompletedTask;
        }

        

        public override void Stop()
        {
            _cancellationTokenSource?.Cancel();
            tcp?.Dispose();
        }
        
        private void OnReceiveAsync(YawData data)
        {
            this.Data = data;
            switch (data.Command)
            {

                default:
                    // Handle other commands if necessary
                    break;
            }

            OnUpdate();
        }
    }

    public class YawVRGlobal : UpdateablePluginGlobal<YawVRPlugin>
    {
        public YawData Data => plugin.Data;
    }
}
