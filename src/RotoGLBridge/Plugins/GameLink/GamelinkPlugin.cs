using Microsoft.Extensions.Logging;

using RotoGLBridge.Models;
using RotoGLBridge.Services;

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
        ILogger<GamelinkGlobal> logger
        ) : UpdateablePlugin, IConfigurablePlugin<GamelinkSettings>
    {

        #region Private Fields

        CancellationTokenSource _cancellationTokenSource;


        UdpTelemetry<StringData> udp;

        TcpTelemetry<DeviceParams> tcp;

        public UdpTelemetryConfig Config;

        YawGLData _data;

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

            _ = WaitForConnectionAsync();

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
        private async Task WaitForConnectionAsync()
        {


            Config = new UdpTelemetryConfig(
                sendAddress: new IPEndPoint(IPAddress.Parse(Settings.SendAddress), 50050), 
                receiveAddress: new IPEndPoint(IPAddress.Parse(Settings.ReceiveAddress), 50010));

            udp = new UdpTelemetry<StringData>(Config) { Convert = new StringByteConverter(Encoding.ASCII) };

            var converter = new YawGLByteConverter();

            while (!_cancellationTokenSource!.IsCancellationRequested)
            {
                udp.OnReceiveAsync += async (result, data) =>
                {

                    udp.Config.SendAddress.Address = result.RemoteEndPoint.Address;

                    if (data.Value == "YAW_CALLING")
                    {

                        udp.Send(new StringData
                        {
                            Value = new GameLinkResponse
                            {
                                DeviceType = "UNKNOWN",
                                DeviceName = "RotoVR",
                                InGame = false
                            }.ToString()
                        });

                        IsConnected = true;
                    }
                    else if (result.Buffer.Length < 5)
                    {
                        //ping
                        logger.LogDebug("Buffer < 5 {0:x}", data.Value);
                        //udp.Send(new StringData { Value = new GameLinkResponse { DeviceType = "", DeviceName = "RotoVR", InGame = false }.ToString() });
                    }
                    else
                    {
                        Data = converter.FromBytes(result.Buffer);
                        
                        OnUpdate();
                    }


                };


                try
                {
                    _ = await udp.ReceiveAsync(_cancellationTokenSource.Token);
                }
                catch (Exception ex)
                {
                    // Handle exceptions
                    logger.LogError(ex.Message);
                }

                await Task.Delay(1800, _cancellationTokenSource.Token);
            }

            //tcpServer.Stop();
            IsConnected = false;
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
