using com.rotovr.sdk;
using RotoGLBridge.Services;
using Sharpie.Engine.Contracts.Plugins;
using Sharpie.Helpers;

namespace RotoGLBridge.Plugins
{

    [GlobalType(Type = typeof(RotoPluginGlobal))]
    public class RotoPlugin(
        ILogger<RotoPlugin> logger,
        IEnumerable<IMmfSender> mmfSenders,
        Roto roto
        ) : UpdateablePlugin
    {

        public RotoDataModel RotoDataModel = new();

        public ConnectionStatus ConnectionStatus = ConnectionStatus.Unknown;

        public ModeType Mode = default;
        
        CancellationTokenSource _cts;

        public bool IsPluggedIn => roto.IsPluggedIn;

        public float Angle
        {
            get
            {
                if (RotoDataModel == null)
                    return 0;

                return RotoDataModel.CalibratedAngle;
            }
        }

        internal Roto.Telemetry Telemetry => roto.telemetry;


        public override async Task Start()
        {
            _cts = new();

            roto.Initialize(ConnectionType.Chair);

            roto.OnConnectionStatus += _roto_OnConnectionStatusChanged;
            roto.OnRotoMode += _roto_OnModeChanged;
            roto.OnDataChanged += _roto_OnDataChanged;

            

            if (ConnectionStatus != ConnectionStatus.Connected)
            {
                bool connected = await roto.ConnectAsync(_cts.Token);

            }
            else
            {
                logger.LogWarning("Already Connected ?");
            }

        }


        public override async Task Stop()
        {
            _cts.Cancel();

            await roto.SetModeAsync(ModeType.IdleMode, new ModeParams { CockpitAngleLimit = 0, MaxPower = 30});

            roto.OnRotoMode -= _roto_OnModeChanged;
            roto.OnConnectionStatus -= _roto_OnConnectionStatusChanged;
            roto.OnDataChanged -= _roto_OnDataChanged;

            if (ConnectionStatus != ConnectionStatus.Disconnected)
                await roto.DisconnectAsync();
        }

        public override void Execute()
        {
            
        }

        private void _roto_OnDataChanged(RotoDataModel obj)
        {

            RotoDataModel = obj;
            
            if (Enum.TryParse(obj.Mode, out ModeType mode))
            {
                Mode = mode;
            }

            foreach(var mmf in mmfSenders)
            {
                mmf.Send(-obj.CalibratedAngle);
            }

            OnUpdate();
        }

        private void _roto_OnConnectionStatusChanged(ConnectionStatus obj)
        {
            ConnectionStatus = obj;

            if (obj == ConnectionStatus.Disconnected)
            {
                RotoDataModel = new();
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
            roto.SetPowerAsync(RoundDouble(p));
        }


        /// <summary>
        /// Rumble the chair
        /// </summary>
        /// <param name="seconds"></param>
        /// <param name="power">value 0 - 1 </param>
        public void Rumble(float seconds, float power = 1)
        {
            var p = Filters.EnsureMapRange(power, 0, 1, 0, 100);
            roto.Rumble((float)seconds, RoundDouble(p));
        }


        public void Rotate(float degrees, float power = 1)
        {
            var p = Filters.EnsureMapRange(power, 0, 1, 0, 100);
            var (d, a) = GetAngleDirection(degrees);

            roto.RotateToAngle(d, a, RoundDouble(p));
        }

        public void RotateTo(Direction direction, float degrees, float power = 1)
        {
            var p = Filters.EnsureMapRange(power, 0, 1, 0, 100);

            roto.RotateToAngle(direction, RoundDouble(Ensure360(degrees)), RoundDouble(p));
        }

        //public void RotateClosest(float degrees, float power = 1)
        //{
        //    var p = Filters.EnsureMapRange(power, 0, 1, 0, 100);

        //    roto.RotateToClosestAngleDirection(RoundDouble(Ensure360(degrees)), RoundDouble(p));
        //}

        public async Task SwitchModeAsync(ModeType mode, Func<float?> targetFunc = null)//, float limit, float power, RotoMovementMode movementMode)
        {
            if(roto == null)
            {
                return;
            }

            var m = (ModeType)(byte)mode;

            if (mode == ModeType.FollowObject)
            {
                await roto.SetModeAsync(ModeType.HeadTrack, new ModeParams { CockpitAngleLimit = 0, MaxPower = 100 });
                if(targetFunc != null)
                    roto.FollowTarget(targetFunc);
            }
            else //if(m == ModeType.HeadTrack)
            {
                await roto.SetModeAsync(m, new ModeParams { MaxPower = 100 });
                //SetPower(1);
            }
        }

        private float Ensure360(float degrees)
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
            roto.Calibration(CalibrationMode.SetCurrent);
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

        private float NormalizeAngle(float angle)
        {
            if (angle < 0)
                angle += 360;
            else if (angle > 360)
                angle -= 360;

            return angle;
        }
    }

    public class RotoPluginGlobal : UpdateablePluginGlobal<RotoPlugin>
    {
        public bool IsPluggedIn => plugin.IsPluggedIn;

        public RotoDataModel Data => plugin.RotoDataModel;

        public Roto.Telemetry Telemetry => plugin.Telemetry;        

        public string Status => plugin.ConnectionStatus.ToString();

        public bool IsConnected => plugin.ConnectionStatus == ConnectionStatus.Connected;

        public void Rumble(float seconds, float power = 1) => plugin.Rumble(seconds, power);

        public void Rotate(float degrees, float power = 1) => plugin.Rotate(degrees, power);

        public void RotateTo(Direction direction, float degrees, float power = 1) => plugin.RotateTo(direction, degrees, power);

        //public void rotateClosest(float degrees, float power = 1) => plugin.RotateClosest(degrees, power);

        public Task SwitchModeAsync(ModeType mode, Func<float?> targetFunc = null) => plugin.SwitchModeAsync(mode, targetFunc);
        

        public void Calibrate() => plugin.SetToZero();

        /// <summary>
        /// set the power
        /// </summary>
        /// <param name="power">between 0.3 - 1.0</param>
        public void SetPower(float power = .5f) => plugin.SetPower(power);


        public override string ToString()
        {
            return plugin.RotoDataModel?.ToString() ?? "";// ToJson() ?? "";
        }
        
    }
}
