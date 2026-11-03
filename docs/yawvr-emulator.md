# YawVR device emulation

`RotoYawEmulator` derives from `YawVR.Link.Emulator.YawEmulator`. The base class
owns all sockets, discovery, sessions, protocol replies and outgoing telemetry.
`RotoScript.Start()` calls `StartAsync()`; `RotoScript.Stop()` awaits `StopAsync()`
before disconnecting the chair. The old TCP and UDP plugins are removed.

The default identity is a Yaw2 named **Roto VR**, ID `ROTOVR`. It listens on all
IPv4 interfaces (`0.0.0.0`), UDP 50010 and TCP 50020. Replies and telemetry go to
the requesting client's address; there is no configured sender IP restriction.
The base class retains the normal one-game reservation while allowing app sessions.

Configure identity, bind address and ports with `AddRotoGLBridge`:

```csharp
services.AddRotoGLBridge(settings =>
{
    settings.YawDevice = new(YawVR.Link.YawModel.Yaw2, "ROTOVR", "My Roto");
    settings.YawEmulator.TcpPort = 50020;
    settings.YawEmulator.UdpPort = 50010;
});
```

Motion keeps the existing relative yaw-follow calculation and vibration scaling
(0–254 to 0–100%). UDP-only motion without check-in or START remains supported.
START enables follow mode; STOP and a running owner's disconnect idle the motor
and stop vibration. YawVR power (0–99) maps to Roto motor power (0–100), including
reissuing the current target when power changes. Outgoing telemetry uses the
chair's measured yaw. State queries report USB availability and chair errors.
Calibration maps to Roto's zero-base command. Motion compensation, voice control
and USB detection remain in RotoScript.

The Roto has no pitch/roll axes, so tilt limits and actual pitch/roll report zero.
Its USB API has no parking operation; STOP with park idles the chair. Other
protocol settings and replies use YawVR.Link's behavior; LEDs, temperatures,
reboot, persistent settings and Yaw-specific yaw limits have no physical Roto
implementation. Real-device parity still depends on the protocol verification
items documented in YawVR.Link and testing against GameLink and the mobile app.

The project reference defaults to the current `D:\_srcyawvr\YawVR.Link` checkout
relative to this repository. Override it on another machine:

```powershell
dotnet build -p:YawVRLinkProject=C:\src\YawVR.Link\src\YawVR.Link\YawVR.Link.csproj
```

The library adds `OnStarting()` and `OnStopped()` hooks for adapter state and
parameter initialization. Roto initializes its parameters before any listener
starts and resets follow state on every start; connection handling stays in the
base class.
