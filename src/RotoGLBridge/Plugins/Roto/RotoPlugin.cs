using com.rotovr.sdk;

using Microsoft.Extensions.Logging;

using Sharpie.Engine.Contracts.Plugins;
using Sharpie.Helpers;

namespace RotoGLBridge.Plugins
{

    [GlobalType(Type = typeof(RotoPluginGlobal))]
    public class RotoPlugin(
        ILogger<RotoPluginGlobal> logger
        ) : UpdateablePlugin
    {

        public RotoBehaviour Roto;

        public RotoDataModel rotoDataModel;

        public ConnectionStatus connectionStatus = ConnectionStatus.Unknown;

        public ModeType Mode = default;

        private int? zeroAngle = null;

        CancellationTokenSource _cts;

        private int? ZeroAngle
        {
            get => zeroAngle;
            set
            {
                if (zeroAngle == null)
                    zeroAngle = value;
            }
        }

        public int Angle
        {
            get
            {
                if (rotoDataModel == null)
                    return 0;
                var a = (rotoDataModel.Angle + (ZeroAngle ?? 0));
                if (a < 0)
                    a += 360;
                return a;
            }
        }

        


        public override async Task Start()
        {
            _cts = new();
            Roto = new RotoBehaviour();
            //Roto.ConnectionType = ConnectionType.Simulation;
            

            Roto.OnConnectionStatusChanged += _roto_OnConnectionStatusChanged;

            await ConnectAsync(_cts.Token);

            Roto.OnModeChanged += _roto_OnModeChanged;
            
            Roto.OnDataChanged += _roto_OnDataChanged;
            //SwitchMode(RotoModeType.IdleMode);//, 0, 1, RotoMovementMode.Jerky);
            //SetPower(1);



            //return Task.CompletedTask;

        }
        public override async Task Stop()
        {
            _cts.Cancel();
            Roto.SwitchMode(ModeType.IdleMode);

            Roto.OnModeChanged -= _roto_OnModeChanged;
            Roto.OnConnectionStatusChanged -= _roto_OnConnectionStatusChanged;
            Roto.OnDataChanged -= _roto_OnDataChanged;

            // Cleanup the plugin
            await DisconnectAsync();
        }

        public override void Execute()
        {
            
        }

        private Task DisconnectAsync()
        {
            if(connectionStatus != ConnectionStatus.Disconnected)
                return Task.CompletedTask;

            Roto.Disconnect();

            var tcs = new TaskCompletionSource();

            void Handler(ConnectionStatus status)
            {
                if (status == ConnectionStatus.Disconnected)
                {
                    Roto.OnConnectionStatusChanged -= Handler;
                    tcs.TrySetResult();
                }
            }

            Roto.OnConnectionStatusChanged += Handler;

            return tcs.Task;
        }


        private Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            if (connectionStatus == ConnectionStatus.Connected)
                return Task.CompletedTask;

            Roto.Connect();

            var tcs = new TaskCompletionSource();

            void Handler(ConnectionStatus status)
            {
                if (status == ConnectionStatus.Connected)
                {
                    Roto.OnConnectionStatusChanged -= Handler;
                    tcs.TrySetResult();
                }
            }

            Roto.OnConnectionStatusChanged += Handler;

            if (cancellationToken != default)
            {
                cancellationToken.Register(() =>
                {
                    Roto.OnConnectionStatusChanged -= Handler;
                    tcs.TrySetCanceled(cancellationToken);
                });
            }

            return tcs.Task;
        }

        private void _roto_OnDataChanged(RotoDataModel obj)
        {
            if (Mode == ModeType.FollowObject && ZeroAngle == null)
            {
                ZeroAngle = -obj.Angle;
            }

            rotoDataModel = obj;
            
            if (Enum.TryParse(obj.Mode, out ModeType mode))
            {
                Mode = mode;
            }

            OnUpdate();
        }

        private void _roto_OnConnectionStatusChanged(ConnectionStatus obj)
        {
            connectionStatus = obj;

            if (obj == ConnectionStatus.Connected)
            {
                
            }
            else if (obj == ConnectionStatus.Disconnected)
            {
                ZeroAngle = null;
                rotoDataModel = null;
            }
            OnUpdate();
        }

        private void _roto_OnModeChanged(ModeType obj)
        {
            Mode = obj;
            OnUpdate();
        }

        public void SetPower(float power = 1)
        {
            var p = Filters.EnsureMapRange(power, 0, 1, 30, 100);
            Roto.SetPower(RoundDouble(p));
        }


        /// <summary>
        /// Rumble the chair
        /// </summary>
        /// <param name="seconds"></param>
        /// <param name="power">value 0 - 1 </param>
        public void Rumble(float seconds, float power = 1)
        {
            var p = Filters.EnsureMapRange(power, 0, 1, 0, 100);
            Roto.Rumble((float)seconds, RoundDouble(p));
        }


        public void Rotate(float degrees, float power = 1)
        {
            var p = Filters.EnsureMapRange(power, 0, 1, 0, 100);
            var (d, a) = GetAngleDirection(degrees);

            Roto.Rotate(d, a, RoundDouble(p));
        }

        public void RotateTo(RotoDirection direction, float degrees, float power = 1)
        {
            var p = Filters.EnsureMapRange(power, 0, 1, 0, 100);

            Roto.RotateToAngle(direction == RotoDirection.Left ? Direction.Left : Direction.Right, RoundDouble(Ensure360(degrees)), RoundDouble(p));
        }

        public void RotateClosest(float degrees, float power = 1)
        {
            var p = Filters.EnsureMapRange(power, 0, 1, 0, 100);

            Roto.RotateToClosestAngleDirection(RoundDouble(Ensure360(degrees)), RoundDouble(p));
        }

        public void SwitchMode(RotoModeType mode, Func<float?> targetFunc = null)//, float limit, float power, RotoMovementMode movementMode)
        {
            if(Roto == null)
            {
                return;
            }

            var m = (ModeType)(byte)mode;

            if (new[] { RotoModeType.FollowObject, RotoModeType.JoystickMode }.Contains(mode))
            {
                Roto.SwitchMode(m, new ModeParams { CockpitAngleLimit = 0, MaxPower = 100 }, targetFunc);
            }
            else //if(m == ModeType.HeadTrack)
            {
                Roto.roto.SetMode(m, new ModeParams { MaxPower = 100 });
                //SetPower(1);
            }
        }

        public float Ensure360(float degrees)
        {
            var a = Math.Sign(degrees) * (degrees % 360);
            if (a < 0)
            {
                a += 360;
            }
            return a;
        }

        public void SetToZero()
        {
            Roto.Calibration(CalibrationMode.SetToZero);
        }


        private int RoundDouble(float value)
        {
            return (int)Math.Round(value, 0);
        }

        private (Direction direction, int angle) GetAngleDirection(float degrees)
        {
            var d = degrees < 0 ? Direction.Left : Direction.Right;

            var ang = RoundDouble(Ensure360(degrees));

            return (d, ang);
        }        
    }

    public class RotoPluginGlobal : UpdateablePluginGlobal<RotoPlugin>
    {
        public float rawAngle => plugin.rotoDataModel?.Angle ?? 0;
        public float angle => plugin.Angle;


        public string mode => plugin.Mode.ToString();

        public int angleLimit => plugin.rotoDataModel?.TargetCockpit ?? 0;

        public int maxPower => plugin.rotoDataModel?.MaxPower ?? 0;

        public string connectionStatus => plugin.connectionStatus.ToString();


        public void rumble(float seconds, float power = 1) => plugin.Rumble(seconds, power);

        public void rotate(float degrees, float power = 1) => plugin.Rotate(degrees, power);

        public void rotateTo(RotoDirection direction, float degrees, float power = 1) => plugin.RotateTo(direction, degrees, power);

        public void rotateClosest(float degrees, float power = 1) => plugin.RotateClosest(degrees, power);

        public void switchMode(RotoModeType mode, Func<float?> targetFunc = null) => plugin.SwitchMode(mode, targetFunc);

        public void setPower(float power = .5f) => plugin.SetPower(power);


        public override string ToString()
        {
            return plugin.rotoDataModel.ToJson();
        }
        
    }
}
