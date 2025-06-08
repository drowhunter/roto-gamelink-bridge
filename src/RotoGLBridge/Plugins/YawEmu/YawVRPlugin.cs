using RotoGLBridge.Plugins.YawEmu.Commands;

using Sharpie.Engine.Contracts.Plugins;
using Sharpie.Extras.Telemetry;

using System.Net;
using System.Net.Sockets;

namespace RotoGLBridge.Plugins.YawEmu
{
    [GlobalType(Type = typeof(YawVRGlobal))]
    public class YawVRPlugin : UpdateablePlugin
    {
        CancellationTokenSource _cancellationTokenSource;

        TcpTelemetry<ITcpCommand> tcp;

        public ITcpCommand Data { get; private set; }

        public override void Execute()
        {
            //throw new NotImplementedException();
        }

        public override Task Start()
        {
            _cancellationTokenSource = new CancellationTokenSource();

            _ = WaitForConnectionAsync(_cancellationTokenSource.Token);


            return Task.CompletedTask;
        }

        private Task WaitForConnectionAsync(CancellationToken cancellationToken)
        {
            TcpTelemetryConfig config = new()
            {
                IpAddress = new IPEndPoint(IPAddress.Any, 50020)
            };

            tcp = new TcpTelemetry<ITcpCommand>(config, new YawTcpCommandConverter());
            tcp.OnReceiveAsync += OnReceiveAsync;

            return tcp.BeginAsync(cancellationToken);            
            
        }

        public override Task Stop()
        {
            _cancellationTokenSource?.Cancel();
            tcp?.Dispose();

            tcp = null;
            Data = null;

            return Task.CompletedTask;

        }
        
        private void OnReceiveAsync(object sender,  ITcpCommand data)
        {
            if (sender is TcpClient client)
            {
                switch (data)
                {
                    case CheckInCommand cmd:
                        break;
                    case SetPowerCommand cmd:
                        tcp.Send(data);

                        break;

                }

                this.Data = data;

                //todo mediate

                OnUpdate();
            }
        }
    }

    public class YawVRGlobal : UpdateablePluginGlobal<YawVRPlugin>
    {
        public ITcpCommand Data => plugin.Data;
    }
}
