using RotoGLBridge.Configuration;
using RotoGLBridge.Plugins;
using YawVR.Link;
using YawVR.Link.Emulator;

namespace RotoGLBridge.Services;

/// <summary>Adapts the YawVR device protocol to the Roto chair.</summary>
public sealed class RotoYawEmulator : YawEmulator
{
    private readonly RotoPluginGlobal roto;
    private readonly IFollowCalculator follow;
    private readonly object motionGate = new();
    private bool acceptMotion = true;
    private float? followAngle;
    private long lastMotionTick;

    public RotoYawEmulator(RotoPluginGlobal roto, IFollowCalculator follow,
        RotoGLBridgeSettings settings, ILoggerFactory loggerFactory)
        : base(CreateOptions(settings, loggerFactory))
    {
        this.roto = roto;
        this.follow = follow;
        DeviceInfo = settings.YawDevice;
    }

    private static EmulatorOptions CreateOptions(RotoGLBridgeSettings settings, ILoggerFactory loggerFactory)
    {
        var options = settings.YawEmulator;
        options.LoggerFactory ??= loggerFactory;
        return options;
    }

    public override DeviceInfo DeviceInfo { get; }
    public bool TcpIsConnected => Sessions.Count > 0;
    public bool TelemetryIsConnected => IsRunning && Volatile.Read(ref lastMotionTick) != 0
        && Environment.TickCount64 - Volatile.Read(ref lastMotionTick) < 5000;
    public float Yaw => roto.IsConnected ? roto.Yaw : Normalize(LastCommandedPose.Yaw);
    public float AmpPercent { get; private set; }
    public float HzPercent { get; private set; }
    public float Hertz { get; private set; }
    public int Power => roto.Power;

    protected override void OnStarting()
    {
        lock (motionGate)
        {
            follow.Reset();
            followAngle = null;
            acceptMotion = true; // Preserve senders that stream UDP without START.
            lastMotionTick = 0;
            AmpPercent = HzPercent = Hertz = 0;
            Parameters.Power = (int)Math.Round(Math.Clamp(roto.Power, 0, 100) * 99d / 100);
            Parameters.TiltLimits = new(0, 0, 0);
            Parameters.HasVibration = true;
            Parameters.VibrationLimit = 100;
        }
    }

    protected override void OnStopped()
    {
        OnStop(false);
        Volatile.Write(ref lastMotionTick, 0);
    }

    protected override void OnStart()
    {
        lock (motionGate)
        {
            follow.Reset();
            followAngle = null;
            acceptMotion = true;
            if (roto.IsConnected) roto.RunMode = RunMode.Follow;
        }
    }

    protected override void OnStop(bool park)
    {
        lock (motionGate)
        {
            acceptMotion = false;
            follow.Reset();
            followAngle = null;
            AmpPercent = HzPercent = Hertz = 0;
            if (roto.IsConnected)
            {
                // The Roto API has no parking operation; idle stops its motor.
                roto.RunMode = RunMode.Idle;
                roto.Vibrate(0, 0);
                roto.StopVibration();
            }
        }
    }

    protected override int OnSetPower(int percent)
    {
        lock (motionGate)
        {
            var accepted = Math.Clamp(percent, 0, 99);
            // YawVR uses 0..99; the Roto motor uses 0..100.
            roto.Power = (int)Math.Round(accepted * 100d / 99);
            if (acceptMotion && roto.IsConnected && followAngle.HasValue)
                roto.Yaw = followAngle.Value;
            return accepted;
        }
    }

    protected override TiltLimits OnSetTiltLimits(TiltLimits limits) => new(0, 0, 0);

    protected override void OnCalibrate(bool allAxes)
    {
        lock (motionGate)
        {
            follow.Reset();
            followAngle = null;
            if (roto.IsConnected) roto.Calibrate();
        }
    }

    protected override void OnMotion(MotionFrame frame)
    {
        lock (motionGate)
        {
            Volatile.Write(ref lastMotionTick, Environment.TickCount64);
            if (!acceptMotion) return;

            if (frame.HasPose && roto.IsConnected)
            {
                var result = follow.Update(Normalize(frame.Pose.Yaw), roto.Yaw);
                if (result is not null && MathF.Abs(result.OffsetDifference) >= 1)
                {
                    var angle = MathF.Round(result.NewFollowAngle);
                    if (followAngle != angle)
                    {
                        followAngle = angle;
                        roto.Yaw = angle;
                    }
                }
            }

            var vibration = frame.Vibration;
            Hertz = vibration?.FrequencyHz ?? 0;
            AmpPercent = Parameters.HasVibration
                ? Math.Min(Parameters.VibrationLimit, (vibration?.Amplitude0 ?? 0) / 254f * 100) : 0;
            HzPercent = Hertz / 254f * 100;
            if (roto.IsConnected) roto.Vibrate((int)AmpPercent, (int)HzPercent);
        }
    }

    protected override Orientation GetActualPose() => new(roto.Yaw, 0, 0);

    protected override DeviceStatus OnGetStatus()
    {
        var status = roto.Status;
        var kind = !status.USBConnected ? DeviceStateKind.Disabled
            : status.ErrorMode == 0x80 ? DeviceStateKind.EmergencySwitch
            : status.ErrorMode != 0 ? DeviceStateKind.EmergencyMode
            : (RunMode)status.RunMode == RunMode.Idle ? DeviceStateKind.Disabled
            : DeviceStateKind.SimulationMode;
        return new(kind, Calibrating: (RunMode)status.RunMode == RunMode.Calibrating);
    }

    private static float Normalize(float yaw) => (yaw % 360 + 360) % 360;
}
