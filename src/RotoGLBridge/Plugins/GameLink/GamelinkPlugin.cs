using Microsoft.Extensions.Logging;

using RotoGLBridge.Models;
using RotoGLBridge.Plugins.GameLink;

using Sharpie.Engine.Contracts.Plugins;
using Sharpie.Extras.Telemetry;

using System.Net;
using System.Text;

namespace RotoGLBridge.Plugins
{

    public record GamelinkSettings : IPluginSettings
    {
        /// <summary>
        /// Address to send udp responses to defaults to 127.0.0.1 (localhost).
        /// </summary>
        public string SendAddress { get; set; } = IPAddress.Loopback.ToString();

        /// <summary>
        /// Address to receive udp packets from, defaults to 0.0.0.0 (any address).
        /// </summary>
        public string ReceiveAddress { get; set; } = IPAddress.Any.ToString();

    }

    [GlobalType(Type = typeof(GamelinkGlobal))]
    public class GamelinkPlugin(
        ILogger<GamelinkGlobal> logger,
        GamelinkSettings settings
        ) : UpdateablePlugin, IConfigurablePlugin<GamelinkSettings>
    {

        #region Private Fields

        CancellationTokenSource _cancellationTokenSource;
        UdpTelemetry<string> udp;
        public UdpTelemetryConfig Config;

        YawGLData _data;

        YawGLByteConverter converter = new ();

        object lockObj = new object();

        #endregion

        #region Properties


        public bool IsConnected { get; private set; }

        public YawGLData Data
        {
            get { return _data; }
            set
            {
                lock (lockObj)                
                    _data = value;                
            }
        }

       

        #endregion



        #region IUpdateablePlugin Implementation

        public override Task Start()
        {
            _cancellationTokenSource = new CancellationTokenSource();

            _ = StartListeningAsync(_cancellationTokenSource.Token);

            return Task.CompletedTask;
        }


        public override void Execute()
        {
            // do nothing
        }

        

        public override void Stop()
        {
            IsConnected = false;
            _cancellationTokenSource?.Cancel();
            udp?.Dispose();
        }

        #endregion


        /// <summary>
        /// Configure the UDP plugin with send and receive addresses and ports.
        /// </summary>
        /// <param name="receiveAddress">ipaddress:port</param>       
        private async Task StartListeningAsync(CancellationToken cancellationToken)
        {
            Config = new UdpTelemetryConfig(
                sendAddress: new IPEndPoint(IPAddress.Parse(settings.SendAddress), 50050),
                receiveAddress: new IPEndPoint(IPAddress.Parse(settings.ReceiveAddress), 50010))
            {
                ReceiveTimeout = 1000
            };
            
            
            
            udp = new UdpTelemetry<string>(Config, new StringByteConverter(Encoding.ASCII)) ;
            udp.OnReceiveAsync += OnUdpReceiveAsync;

            await udp.BeginAsync(cancellationToken);

            //tcpServer.Stop();
            
            IsConnected = false;

            logger.LogDebug($"GamelinkPluginIsConnected {IsConnected}");
        }

        private void OnUdpReceiveAsync(System.Net.Sockets.UdpReceiveResult result, string data)        
        {

            //udp.Config.SendAddress.Address = result.RemoteEndPoint.Address;

            if (data == "YAW_CALLING")
            {
                if (result.RemoteEndPoint.Address.Equals(udp.Config.SendAddress.Address))
                {
                    udp.Send(new GameLinkResponse
                    {
                        DeviceType = "UNKNOWN", //"YAWDEVICE",
                        DeviceName = "Roto VR", //"YAW_EMULATOR",
                        TcpPort = 50020,
                        InGame = false
                    }.ToString()
                    );

                    IsConnected = true;
                }
            }
            else if (result.Buffer.Length < 5)
            {
                //ping
                logger.LogDebug("Buffer < 5 {0:x}", data);
                //udp.Send(new StringData { Value = new GameLinkResponse { DeviceType = "", DeviceName = "RotoVR", InGame = false }.ToString() });
            }
            else
            {
                Data = converter.FromBytes(result.Buffer);

                OnUpdate();
            }


        }
    }


    public class GamelinkGlobal : UpdateablePluginGlobal<GamelinkPlugin>
    {
       

        public float yaw => plugin.Data.yaw;

        public float pitch => plugin.Data.pitch;

        public float roll => plugin.Data.roll;

        public float amp => plugin.Data.amp;

        public float hz => plugin.Data.hz;

        public float fan => plugin.Data.fan;

    }

}
