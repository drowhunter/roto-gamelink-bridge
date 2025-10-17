#define FULLYINTEGRATED_ON
using Microsoft.Extensions.Logging;

using RotoGLBridge.Models;
using RotoGLBridge.Plugins.GameLink;

using Sharpie.Engine.Contracts.Plugins;
using Sharpie.Helpers.Telemetry;

using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;


namespace RotoGLBridge.Plugins
{

    /// <summary>
    /// Configuration settings for the GameLink plugin.
    /// </summary>
    public record GamelinkSettings : IPluginSettings
    {
        /// <summary>
        /// Address to send UDP responses to. Defaults to 127.0.0.1 (localhost).
        /// </summary>
        public string SendAddress { get; set; } = IPAddress.Loopback.ToString();

        /// <summary>
        /// Address to receive UDP packets from. Defaults to 0.0.0.0 (any address).
        /// </summary>
        public string ReceiveAddress { get; set; } = IPAddress.Any.ToString();
    }

    /// <summary>
    /// GameLink plugin that handles UDP communication for YAW GameLink protocol.
    /// Provides motion data communication between games and the RotoVR device.
    /// </summary>
    [GlobalType(Type = typeof(GamelinkGlobal))]
    public class GamelinkPlugin(
        ILogger<GamelinkGlobal> logger,
        GamelinkSettings settings
        ) : UpdateablePlugin, IConfigurablePlugin<GamelinkSettings>
    {
        #region Private Fields

        private int _connectionState = 0;

        
        /// <summary>
        /// Cancellation token source for stopping UDP operations.
        /// </summary>
        CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// UDP telemetry instance for network communication.
        /// </summary>
        UdpTelemetry<string> udp;

        /// <summary>
        /// UDP telemetry configuration.
        /// </summary>
        public UdpTelemetryConfig Config;

        /// <summary>
        /// Current YAW GameLink data received from UDP packets.
        /// </summary>
        YawGLData _data;

        /// <summary>
        /// Converter for transforming between bytes and YawGLData.
        /// </summary>
        YawGLByteConverter converter = new();

        /// <summary>
        /// Lock object for thread-safe access to data.
        /// </summary>
        object lockObj = new object();

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets whether the plugin is currently connected to a GameLink client.
        /// </summary>
        public bool IsConnected { get; private set; }

        /// <summary>
        /// Gets or sets the current YAW GameLink data in a thread-safe manner.
        /// </summary>
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

        IDisposable updateSubscription;
        /// <summary>
        /// Starts the plugin by initializing UDP listening.
        /// </summary>
        /// <returns>A completed task.</returns>
        public override Task Start()
        {
            _cancellationTokenSource = new CancellationTokenSource();

            _ = StartListeningAsync(_cancellationTokenSource.Token);



            return Task.CompletedTask;
        }

        /// <summary>
        /// Executes the plugin's main logic. Currently does nothing.
        /// </summary>
        public override void Execute()
        {
            // do nothing
            //IsConnected = false;
            
            //if(IsConnected && ((DateTime.Now - _lastConnectedSubject).TotalSeconds >= 5))
            //{
            //    IsConnected = false;
            //}
            
        }

        /// <summary>
        /// Stops the plugin by cancelling UDP operations and disconnecting.
        /// </summary>
        /// <returns>A task that completes after a short delay.</returns>
        public override Task Stop()
        {
            //UsbConnected = false;
            updateSubscription?.Dispose();

            _cancellationTokenSource?.Cancel();

            return Task.Delay(100);
        }

        #endregion

        

        
        /// <summary>
        /// Configures and starts UDP listening for GameLink protocol messages.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the listening operation.</param>
        /// <returns>A completed task.</returns>
        private Task StartListeningAsync(CancellationToken cancellationToken)
        {
            Config = new UdpTelemetryConfig(
                sendAddress: new IPEndPoint(IPAddress.Parse(settings.SendAddress), 50050),
                receiveAddress: new IPEndPoint(IPAddress.Parse(settings.ReceiveAddress), 50010))
            {
                ReceiveTimeout = 1000
            };

            udp = new UdpTelemetry<string>(Config, new StringByteConverter(Encoding.ASCII));
            udp.OnReceiveAsync += OnUdpReceiveAsync;

            _ = udp.BeginAsync(cancellationToken);

            logger.LogDebug($"GamelinkPluginIsConnected {IsConnected}");

            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles incoming UDP messages, processing GameLink protocol data.
        /// </summary>
        /// <param name="result">The UDP receive result containing the message.</param>
        /// <param name="data">The received string data.</param>
        private void OnUdpReceiveAsync(UdpReceiveResult result, string data)
        {
            //if (!UsbConnected)
            //    return;

            if (data == "YAW_CALLING")
            {
#if FULLYINTEGRATED_ON
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

                    
                    // UsbConnected = true;
                }
                _connectionState = 1;
                
                IsConnected = false;
#endif
            }
            else if (result.Buffer.Length < 5)
            {
                //ping
                logger.LogDebug("Buffer < 5 {0:x}", data);
                //udp.Send(new StringData { Value = new GameLinkResponse { DeviceType = "", DeviceName = "RotoVR", InGame = false }.ToString() });
            }
            else
            {
                _connectionState = 2;
                //IsConnected = true;
                Data = converter.FromBytes(result.Buffer);
               
                
                IsConnected = true;
                OnUpdate();
            }
        }

        
    }


    /// <summary>
    /// Global interface for the GameLink plugin, exposing motion data properties.
    /// </summary>
    public class GamelinkGlobal : UpdateablePluginGlobal<GamelinkPlugin>
    {
        

        /// <summary>
        /// Gets or sets whether the GameLink connection is active.
        /// </summary>
        public bool IsConnected { get => plugin.IsConnected; }

        

        /// <summary>
        /// Gets the yaw rotation value from the current motion data.
        /// </summary>
        public float yaw => plugin.Data.yaw;

        /// <summary>
        /// Gets the pitch rotation value from the current motion data.
        /// </summary>
        public float pitch => plugin.Data.pitch;

        /// <summary>
        /// Gets the roll rotation value from the current motion data.
        /// </summary>
        public float roll => plugin.Data.roll;

        /// <summary>
        /// Gets the vibration amplitude value from the current motion data.
        /// </summary>
        public int amp => plugin.Data.ampPct;

        /// <summary>
        /// Gets the vibration frequency (Hz) value from the current motion data.
        /// </summary>
        public float hz => plugin.Data.hz;

        /// <summary>
        /// Gets the fan speed value from the current motion data.
        /// </summary>
        public float fan => plugin.Data.fan;

        /// <summary>
        /// Milliseconds between waves
        /// </summary>
        public int frequency => plugin.Data.frequency;
    }

}
