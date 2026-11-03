using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RotoGLBridge.Configuration;
using RotoGLBridge.Plugins;
using RotoGLBridge.Services;
using rotoUSB;
using YawVR.Link;
using YawVR.Link.Sender;

namespace RotoGLBridge.Tests.Services;

public class RotoYawEmulatorTests
{
    [Fact]
    public async Task Discovery_and_control_work_from_another_loopback_address()
    {
        await using var rig = await Rig.Create();
        using var udp = new UdpClient(new IPEndPoint(IPAddress.Parse("127.0.0.2"), 0));
        await udp.SendAsync(Encoding.ASCII.GetBytes("YAW_CALLING").AsMemory(), rig.Endpoint, TestContext.Current.CancellationToken);
        var reply = await udp.ReceiveAsync(TestContext.Current.CancellationToken).AsTask().WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
        Assert.Contains("Roto VR", Encoding.ASCII.GetString(reply.Buffer));

        using var tcp = new TcpClient(new IPEndPoint(IPAddress.Parse("127.0.0.2"), 0));
        await tcp.ConnectAsync(IPAddress.Loopback, rig.Emulator.TcpPort, TestContext.Current.CancellationToken);
        await Eventually(() => rig.Emulator.TcpIsConnected);
        Assert.Equal(IPAddress.Parse("127.0.0.2"), rig.Emulator.Sessions.Single().Remote.Address);
    }

    [Fact]
    public async Task Udp_only_motion_routes_yaw_and_vibration_without_check_in()
    {
        await using var rig = await Rig.Create();
        await rig.Send("Y[-10]P[2]R[3]V[127,127,127,127]");
        await Eventually(() => rig.Follow.Invocations.Any(i => i.Method.Name == "Update"));
        rig.Follow.Verify(f => f.Update(350, 25), Times.Once);
        await Eventually(() => rig.Chair.Invocations.Any(i => i.Method.Name == "SetObjectFollowDegree"));
        rig.Chair.Verify(c => c.SetObjectFollowDegree(40, 100), Times.Once);
        await Eventually(() => rig.Emulator.AmpPercent == 50);
        Assert.Equal(50, rig.Emulator.HzPercent);
        rig.Rumble.Verify(r => r.Rumble(50, 50), Times.Once);
        Assert.True(rig.Emulator.TelemetryIsConnected);
    }

    [Fact]
    public async Task Power_status_and_actual_pose_round_trip_and_stop_blocks_motion()
    {
        await using var rig = await Rig.Create();
        await using var sender = rig.NewSender();
        await sender.StartAsync(IPAddress.Loopback, YawModel.Yaw2, rig.Emulator.TcpPort, TestContext.Current.CancellationToken);
        Assert.Equal(99, (await sender.GetParametersAsync(TestContext.Current.CancellationToken)).Power);
        await sender.SetPowerAsync(49, TestContext.Current.CancellationToken);
        await Eventually(() => rig.Emulator.Power == 49);
        await sender.StartMotionAsync(TestContext.Current.CancellationToken);
        await Eventually(() => rig.Chair.Invocations.Any(i => i.Method.Name == "SetObjectFollowMode"));
        rig.Status.RunMode = (int)RunMode.Follow;
        Assert.Equal(DeviceStateKind.SimulationMode, (await sender.GetStatusAsync(TestContext.Current.CancellationToken)).State);
        await Eventually(() => sender.LastTelemetry?.Actual == new Orientation(25, 0, 0));
        rig.Status.ErrorMode = 0x80;
        Assert.Equal(DeviceStateKind.EmergencySwitch, (await sender.GetStatusAsync(TestContext.Current.CancellationToken)).State);
        rig.Status.ErrorMode = 0;

        await sender.StopMotionAsync(park: true, ct: TestContext.Current.CancellationToken);
        await Eventually(() => rig.Chair.Invocations.Any(i => i.Method.Name == "SetIdleMode"));
        rig.Chair.Verify(c => c.StopRumble(), Times.AtLeastOnce);
        rig.Follow.Invocations.Clear();
        await rig.Send("Y[90]P[0]R[0]");
        await Eventually(() => rig.Emulator.LastCommandedPose.Yaw == 90);
        rig.Follow.Verify(f => f.Update(It.IsAny<float>(), It.IsAny<float>()), Times.Never);
    }

    [Fact]
    public async Task Running_owner_disconnect_idles_chair_and_service_can_restart()
    {
        await using var rig = await Rig.Create();
        var sender = rig.NewSender();
        await sender.StartAsync(IPAddress.Loopback, YawModel.Yaw2, rig.Emulator.TcpPort, TestContext.Current.CancellationToken);
        await sender.StartMotionAsync(TestContext.Current.CancellationToken);
        await Eventually(() => rig.Emulator.State == YawVR.Link.Emulator.EmulatorState.Running);
        await sender.DisposeAsync();
        await Eventually(() => rig.Chair.Invocations.Any(i => i.Method.Name == "SetIdleMode"));
        await rig.Emulator.StopAsync();
        Assert.False(rig.Emulator.IsRunning);
        Assert.False(rig.Emulator.TelemetryIsConnected);
        await rig.Emulator.StartAsync(TestContext.Current.CancellationToken);
        rig.Follow.Invocations.Clear();
        await rig.Send("Y[20]P[0]R[0]");
        await Eventually(() => rig.Emulator.TelemetryIsConnected);
        await Eventually(() => rig.Follow.Invocations.Any(i => i.Method.Name == "Update"));
        rig.Follow.Verify(f => f.Update(20, 25), Times.Once);
    }

    private static async Task Eventually(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        while (!condition()) await Task.Delay(10, timeout.Token);
    }

    private sealed class Rig : IAsyncDisposable
    {
        public Mock<IRotoChair> Chair { get; } = new();
        public Mock<IRumbleService> Rumble { get; } = new();
        public Mock<IFollowCalculator> Follow { get; } = new();
        public RotoStatus Status { get; } = new() { USBConnected = true, BaseDegree = 25 };
        public RotoYawEmulator Emulator { get; private set; } = null!;
        public IPEndPoint Endpoint => new(IPAddress.Loopback, Emulator.UdpPort);

        public static async Task<Rig> Create()
        {
            var rig = new Rig();
            rig.Chair.Setup(c => c.GetRotoStatus()).Returns(rig.Status);
            rig.Follow.Setup(f => f.Update(It.IsAny<float>(), It.IsAny<float>()))
                .Returns(new FollowResult { NewFollowAngle = 40.4f, OffsetDifference = 15.4f });
            var plugin = new RotoPlugin(NullLogger<RotoPlugin>.Instance, rig.Chair.Object, rig.Rumble.Object);
            plugin.Execute();
            var settings = new RotoGLBridgeSettings();
            settings.YawEmulator.TcpPort = settings.YawEmulator.UdpPort = 0;
            rig.Emulator = new(new RotoPluginGlobal { plugin = plugin }, rig.Follow.Object,
                settings, NullLoggerFactory.Instance);
            await rig.Emulator.StartAsync(TestContext.Current.CancellationToken);
            return rig;
        }

        public YawSender NewSender() => new(new SenderOptions
        {
            TelemetryPort = 0, DiscoveryReplyPort = 0, DeviceUdpPort = Emulator.UdpPort,
            RequestTimeout = TimeSpan.FromSeconds(3)
        });

        public async Task Send(string text)
        {
            using var udp = new UdpClient();
            await udp.SendAsync(Encoding.ASCII.GetBytes(text).AsMemory(), Endpoint, TestContext.Current.CancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            await Emulator.StopAsync();
            await Emulator.DisposeAsync();
        }
    }
}
