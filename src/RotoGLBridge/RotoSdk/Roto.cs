
#define OXRMC_ROTO
#define OXRMC_FLYPT
#define DEBUG_MMF

using Sharpie.Helpers.Core;
using Sharpie.Helpers.Core.Lerping;
using Sharpie.Helpers.Telemetry;

using System.Diagnostics;

namespace com.rotovr.sdk
{
    /// <summary>
    /// The Roto class provides an API to interact with the RotoVR chair, allowing for communication with the device, 
    /// mode setting, rotation control, calibration, and connection management.
    /// </summary>
    public partial class Roto(
        ILogger<Roto> logger,
        ILerper m_yawInterpolator,
        Stopwatch _angleUpdateStopwatch,
        IUsbConnector usbConnector
        )
    {
        private const int ReadHz = 144; // 90 Hz for the interpolator
        int WriteHz = 50;


        RotoDataModel m_RotoData = new();


        Func<float?> m_ObservableTarget;

        CancellationTokenSource m_CancelSource;
        

        bool m_IsInit;
        float? m_StartTargetAngle = null;
        int m_prevTargetAngle = 0;
        int m_StartRotoAngle;

        long m_AntiJump = 0;
        int maxPower = 50;
        bool enableDeltaCapping = false;


        float? m_homeAngle = null;

        ConnectionType m_ConnectionType;

        //const float TOP_SPEED_DEG_PER_MS = 120 / 1000f;

        /// <summary>
        /// The current connection status of the RotoVR chair.
        /// Possible statuses: Disconnected, Connecting, or Connected.
        /// </summary>
        public ConnectionStatus ConnectionStatus { get; private set; } = ConnectionStatus.Disconnected;

        /// <summary>
        /// Event triggered when the system mode changes.
        /// </summary>
        public event Action<ModeType> OnRotoMode;

        /// <summary>
        /// Event triggered when chair data changes.
        /// </summary>
        public event Action<RotoDataModel> OnDataChanged;

        /// <summary>
        /// Event triggered when the system connection status changes.
        /// </summary>
        public event Action<ConnectionStatus> OnConnectionStatus;

        
        public bool IsPluggedIn => usbConnector.IsPluggedIn;

        /// <summary>
        /// Initializes the Roto manager with a specified connection type.
        /// </summary>
        /// <param name="connectionType">The connection type (e.g., Simulation or Chair) used for communication.</param>
        public void Initialize(ConnectionType connectionType)
        {
            if (m_IsInit)
                return;

            m_IsInit = true;
            m_ConnectionType = connectionType;           

        }

        void OnConnectionStatusChange(ConnectionStatus status)
        {
            ConnectionStatus = status;
            OnConnectionStatus?.Invoke(status);
        }

        

        /// <summary>
        /// This method is about 5-10fps
        /// </summary>
        /// <param name="model"></param>
        void OnUsbDataChanged(RotoDataModel model)
        {
            telemetry.ActualAngle = model.Angle;

           
            //telemetry.AngularVelocity = m_AngularVelocity;            

            if (model.Mode != m_RotoData.Mode)
            {
                OnRotoMode?.Invoke(model.ModeType);
            }

            //OnDataChanged?.Invoke(model);
            m_RotoData = model;

            telemetry.KalmanAngle = m_yawInterpolator.UpdateValue(model.Angle);
        }


        float previousAngle = 0;
        public int turns = 0;
        private void M_yawInterpolator_OnAngleUpdate(float angle)
        {

            var es = _angleUpdateStopwatch.ElapsedMilliseconds == 0 ? 1 : _angleUpdateStopwatch.ElapsedMilliseconds;
            
            _angleUpdateStopwatch.Restart();

            //telemetry.AngularVelocity = CalculateAngularVelocity();
            telemetry.PreciseAngle = angle;
            telemetry.RecieveHz = m_yawInterpolator.OriginalFramerate;
            telemetry.LerpedHz = es <= 1 ? 0 : 1000 / es;

            var delta = MathF.Abs(angle - previousAngle);
            if (delta > 180)
                delta = 360 - delta;

            telemetry.AngularVelocity = NormalizeAngle(delta) / (es / 1000f); // angle per second

            if(Math.Abs(angle - previousAngle) > 300)
            {
                // this is a jump, so we reset the home angle
                turns = turns + (angle > previousAngle ? -1 : 1);
            }

            previousAngle = angle;
#if DEBUG_MMF
            tel.Send(telemetry);
#endif
            m_RotoData.LerpedAngle = angle;
            m_RotoData.CalibratedAngle = NormalizeAngle(angle - m_homeAngle.GetValueOrDefault(angle));

            OnDataChanged?.Invoke(m_RotoData);


        }


        /// <summary>
        /// Connects to a specified device by name.
        /// </summary>
        /// <param name="deviceName">The name of the device to connect to.</param>
        internal async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
        {
            bool success = false;
            if (m_ConnectionType == ConnectionType.Chair)
            {
                usbConnector.OnConnectionStatus += OnConnectionStatusChange;
                usbConnector.OnDataChange += OnUsbDataChanged;
                await usbConnector.ConnectAsync();
                success = true;
            }
            else
            {
                OnConnectionStatusChange(ConnectionStatus.Connected);
                success = true;
            }

            return success;
        }

        /// <summary>
        /// Disconnects from the currently connected device.
        /// </summary>
        /// <param name="deviceName">The name of the device to disconnect from.</param>
        internal async Task DisconnectAsync()
        {
            if (m_ObservableTarget != null)
                m_ObservableTarget = null;
            
            GC.Collect();
            if (m_ConnectionType == ConnectionType.Chair)
            {
                usbConnector.OnConnectionStatus -= OnConnectionStatusChange;
                usbConnector.OnDataChange -= OnUsbDataChanged;
                await usbConnector.DisconnectAsync();
            }
            else
            {
                OnConnectionStatusChange(ConnectionStatus.Disconnected);
            }
        }

        

        /// <summary>
        /// Sets the mode for the RotoVR chair with specific mode parameters.
        /// </summary>
        /// <param name="mode">The mode to set for the chair (e.g., HeadTrack, FreeMode).</param>
        /// <param name="modeParams">The mode parameters (e.g., power, deltaTargetAngle limits) to configure the chair in the specified moFollowde.</param>
        public async Task SetModeAsync(ModeType mode, ModeParams modeParams)
        {
            var parametersModel = new ModeParametersModel(modeParams);

            if (m_RotoData.ModeType == mode)
                return;

            if (m_ConnectionType == ConnectionType.Chair)
            {
                TaskCompletionSource modeChangeCS = new();

                void OnModeChanged(ModeType m)
                {
                    if (m == mode)
                    {
                        m_RotoData.Mode = mode.ToString();
                        modeChangeCS.TrySetResult();
                    }
                }

                this.OnRotoMode += OnModeChanged;

                await usbConnector.SetModeAsync(new ModeModel(mode.ToString(), parametersModel));

                await modeChangeCS.Task;

                this.OnRotoMode -= OnModeChanged;
            }
        }

        /// <summary>
        /// Sets the power of the RotoVR chair when in FreeMode. The power value must be between 30 and 100.
        /// </summary>
        /// <param name="power">The rotation power to set for the chair (valid range is 30-100).</param>
        public Task SetPowerAsync(int power)
        {
            maxPower = power;

            //var modeParams = new ModeParams
            //{
            //    CockpitAngleLimit = m_RotoData.TargetCockpit,
            //    MaxPower = power
            //};

            //return SetModeAsync(m_RotoData.ModeType, modeParams);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Calibrates the RotoVR chair, resetting the deltaTargetAngle based on the specified calibration mode.
        /// </summary>
        /// <param name="calibrationMode">The calibration mode (e.g., set to zero, set to last position).</param>
        public void Calibration(CalibrationMode calibrationMode)
        {
            switch (calibrationMode)
            {
                case CalibrationMode.SetToZero:
                    RotateToAngle(GetDirection(0, (int) m_RotoData.Angle), 0, 30);
                    break;
                case CalibrationMode.SetCurrent:
                    m_homeAngle = m_RotoData.Angle;
                    break;
            }
        }

        /// <summary>
        /// Rotates the RotoVR chair to the specified deltaTargetAngle.
        /// This is applicable only in the Calibration or CockpitMode.
        /// </summary>
        /// <param name="direction">The direction in which to rotate (e.g., left or right).</param>
        /// <param name="angle">The deltaTargetAngle to rotate to.</param>
        /// <param name="power">The power of rotation (valid range is 0-100).</param>
        public void RotateToAngle(Direction direction, int angle, int power)
        {
            if (angle == m_RotoData.Angle)
                return;

            if (m_ConnectionType == ConnectionType.Chair)
            {
                usbConnector.TurnToAngle(new RotateToAngleModel(angle, power, direction));
            }
            else
            {
                m_RotoData = new RotoDataModel()
                {
                    Angle = angle
                };
                OnDataChanged?.Invoke(m_RotoData);
            }
        }


        /// <summary>
        /// Follow rotation of a target object
        /// </summary>
        /// <param name="behaviour">Target that will be used as the rotation preference.</param>
        /// <param name="targetFunc">Target function which returns a rotation to follow</param>
        public void FollowTarget(Func<float?> targetFunc)
        {
            

            if (m_CancelSource != null && !m_CancelSource.IsCancellationRequested)
            {
                m_CancelSource.Cancel();                 
            }
            
            m_CancelSource = new CancellationTokenSource();

            m_ObservableTarget = targetFunc;

            m_yawInterpolator.OnValueUpdate += M_yawInterpolator_OnAngleUpdate;
            m_yawInterpolator.Start(ReadHz, m_CancelSource.Token);
            _angleUpdateStopwatch = Stopwatch.StartNew();

            var t = new Thread(async () =>
            {
                try
                {
                    await FollowTargetRoutine(m_CancelSource.Token);
                }
                catch (Exception ex)
                {
                    //xxx Log or handle the exception
                    Console.WriteLine($"Error in FollowTargetRoutine: {ex.Message}");
                }
            })
            {
                Name = "FollowTargetRoutine",
                IsBackground = true
            };

            t.Start();

        }

        


            /// <summary>
            /// Plays a rumble effect on the chair with a specified duration and power.
            /// </summary>
            /// <param name="duration">The duration of the rumble in seconds.</param>
            /// <param name="power">The power of the rumble (valid range is 0-100).</param>
        public void Rumble(float duration, int power)
        {
            if (m_ConnectionType == ConnectionType.Chair)
            {
                usbConnector.PlayRumble(new RumbleModel(duration, power));
            }
        }

        

        
        
        private float? GetTargetAngle()
        {
            
            if (m_ObservableTarget != null)
            {
                var targetAngle = m_ObservableTarget();
                //float nTargetAngle = 0;

                //if(targetAngle != null)
                //{
                //    nTargetAngle = targetAngle.Value;
                //}

                if (targetAngle == null)
                {
                    m_StartTargetAngle = null;
                }
                else if (m_StartTargetAngle == null) // enable follow mode
                {

                    m_StartTargetAngle = targetAngle;
                    m_AntiJump = 0;
                    m_StartRotoAngle = m_RotoData.Angle;

                }
                else if (m_AntiJump > 100)
                {
                    m_StartTargetAngle = null;
                    m_AntiJump = 0; 
                }

                //if (targetAngle != null)
                //    return nTargetAngle;
                
                return targetAngle;
            }

            
            return null;
        }

        //object testObj = new object();
        public Telemetry telemetry = new ();




#if DEBUG_MMF
        

        MmfTelemetry<Telemetry> tel = new (new MmfTelemetryConfig()
        {
            Name = "RotoVR",
            Create = true
        }, new MarshalByteConverter<Telemetry>());
#endif


        
        
        async Task FollowTargetRoutine(CancellationToken cancellationToken)
        {
            if (m_ObservableTarget == null)
                logger.LogError("For FollowObject Mode you need to set target func");
            else
            {
                await Task.Delay(500);                
                
                int targetMs = 1000 / WriteHz;

                telemetry.MinPower = 30;
                m_homeAngle = m_RotoData.Angle;

                var sendWatch = Stopwatch.StartNew();

                while (!cancellationToken.IsCancellationRequested)
                {
                    var currentTargetAngle = GetTargetAngle();

                    telemetry.Delta = 0;
                    telemetry.Direction = 0;

                    if (currentTargetAngle != null && m_StartTargetAngle != null)
                    {

                        var deltaTargetAngle = NormalizeAngle(currentTargetAngle.Value - m_StartTargetAngle.Value);

                        //if ((int)deltaTargetAngle != 0 )
                        //{
                            

                        var targetRotoAngle = NormalizeAngle(m_StartRotoAngle + deltaTargetAngle);

                        telemetry.CappedTargetAngle = telemetry.TargetAngle = (int)targetRotoAngle;

                        var delta = Math.Abs(telemetry.TargetAngle - m_RotoData.Angle);
                        if (delta > 180)
                            delta = 360 - delta;
                            
                            
                        telemetry.Delta = delta;

                        if (delta >= 1)
                        {
                            m_AntiJump = 0;


                            //var brakePoint = 10;// MaxPower <= 80 ? 10 : 60;
                            //var pmin = 20;
                            //var pmax = 30;
                            int power = maxPower;

                            power = (int)Filters.EnsureMapRange(delta, 0, 50, 30, maxPower);

                            telemetry.MaxPower = Math.Max(telemetry.MaxPower, power);
                            telemetry.Power = power;

                            telemetry.Direction = GetDirection(telemetry.TargetAngle, m_RotoData.Angle) == Direction.Left ? -1 : 1;

                            //telemetry.AvgTargetAngle = avgTargetRotoAngle;
                            //telemetry.Delta = fDelta;
                            telemetry.AntiJump = (int)m_AntiJump;
                            if (enableDeltaCapping) 
                            { 
                                var cap = 50;
                                if (delta > cap)
                                {
                                    var over = delta - cap;
                                    telemetry.CappedTargetAngle = (int)NormalizeAngle(targetRotoAngle + (over * -telemetry.Direction));
                                }
                            }
                                    RotateToAngle(Direction.Right, telemetry.CappedTargetAngle, power);
                        }
                        else // less than one degree of delta means you are aligned, so increment the anti jump
                        {
                            m_AntiJump = sendWatch.ElapsedMilliseconds;
                        }
                        //}

                        m_prevTargetAngle = (int)currentTargetAngle;
                    }
                    
                    var elapsedTimeLeft = targetMs - sendWatch.ElapsedMilliseconds;
                    
                    if ((int)elapsedTimeLeft > 0)
                    {
                        SleepAccurate(elapsedTimeLeft);
                    }

                    telemetry.SendHz = 1000f / Math.Max(1, sendWatch.ElapsedMilliseconds);
                    sendWatch.Restart();
                    
                }
                                
                m_ObservableTarget = null;
            }
        }

        
        private void SleepAccurate(float ms)
        {
            if (ms <= float.Epsilon)
                return;

            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed.TotalMilliseconds < ms)
            {
                Thread.SpinWait(1);
            }
            stopwatch.Stop();
        }

        


        Direction GetDirection(int targetAngle, int sourceAngle)
        {
            if (targetAngle > sourceAngle)
            {
                if (Math.Abs(targetAngle - sourceAngle) > 180)
                {
                    return Direction.Left;
                }
                else
                {
                    return Direction.Right;
                }
            }
            else
            {
                if (Math.Abs(targetAngle - sourceAngle) > 180)
                {
                    return Direction.Right;
                }
                else
                {
                    return Direction.Left;
                }
            }
        }

        float NormalizeAngle(float angle)
        {
            if (angle < 0)
                angle += 360;
            else if (angle > 360)
                angle -= 360;

            return angle;
        }

        int NormalizeAngle(int angle)
        {
            if (angle < 0)
                angle += 360;
            else if (angle > 360)
                angle -= 360;

            return angle;
        }
        
    }
}