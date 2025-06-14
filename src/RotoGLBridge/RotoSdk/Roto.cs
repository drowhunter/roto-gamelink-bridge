
#define OXRMC_ROTO
#define OXRMC_FLYPT

using Microsoft.Extensions.Logging;

using Sharpie.Helpers;

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
        private const int ReadFPS = 90; // 90 FPS for the interpolator
        int WriteFPS = 50;


        RotoDataModel m_RotoData = new();


        Func<float?> m_ObservableTarget;

        CancellationTokenSource m_CancelSource;
        

        bool m_IsInit;
        float? m_StartTargetAngle = null;
        int m_prevTargetAngle = 0;
        int m_StartRotoAngle;

        long m_AntiJump = 0;
        

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

            m_yawInterpolator.UpdateValue(model.Angle);
        }

        private void M_yawInterpolator_OnAngleUpdate(float angle)
        {

            var es = _angleUpdateStopwatch.ElapsedMilliseconds == 0 ? 1 : _angleUpdateStopwatch.ElapsedMilliseconds;
            _angleUpdateStopwatch.Restart();

            //telemetry.AngularVelocity = CalculateAngularVelocity();
            telemetry.PreciseAngle = angle;
            telemetry.RecieveFPS = m_yawInterpolator.OriginalFramerate;
            telemetry.LerpedFPS = es <= 1 ? 0 : 1000 / es;

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
                success = await usbConnector.ConnectAsync();
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

        int _followPower;
        /// <summary>
        /// Sets the mode for the RotoVR chair with specific mode parameters.
        /// </summary>
        /// <param name="mode">The mode to set for the chair (e.g., HeadTrack, FreeMode).</param>
        /// <param name="modeParams">The mode parameters (e.g., power, deltaTargetAngle limits) to configure the chair in the specified moFollowde.</param>
        public async Task SetModeAsync(ModeType mode, ModeParams modeParams)
        {
            var parametersModel = new ModeParametersModel(modeParams);

            _followPower = (mode == ModeType.FollowObject) ? modeParams.MaxPower : 100;

            if (m_ConnectionType == ConnectionType.Chair)
            {
                await usbConnector.SetModeAsync(new ModeModel(mode.ToString(), parametersModel));
            }

        }

        /// <summary>
        /// Sets the power of the RotoVR chair when in FreeMode. The power value must be between 30 and 100.
        /// </summary>
        /// <param name="power">The rotation power to set for the chair (valid range is 30-100).</param>
        public Task SetPowerAsync(int power)
        {
            var modeParams = new ModeParams
            {
                CockpitAngleLimit = m_RotoData.TargetCockpit,
                MaxPower = power
            };

            return SetModeAsync(m_RotoData.ModeType, modeParams);
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
            m_yawInterpolator.Start(ReadFPS, m_CancelSource.Token);


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
        /// Stop routine
        /// </summary>
        internal void StopRoutine()
        {
            if (m_CancelSource != null && !m_CancelSource.IsCancellationRequested)
            {
                m_CancelSource.Cancel();
            }
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
                if (targetAngle == null)
                {
                    m_StartTargetAngle = null;
                }
                else if (m_StartTargetAngle == null) // enable follow mode
                {
                    m_StartTargetAngle = targetAngle;
                    m_AntiJump = 0;
                    m_StartRotoAngle = (int)m_RotoData.Angle;

                }
                else if (m_AntiJump > 100)
                {
                    m_StartTargetAngle = null;
                    m_AntiJump = 0; 
                }
                return targetAngle;
            }

            
            return null;
        }

        //object testObj = new object();
        public Telemetry telemetry = new ();




#if DEBUG_MMF
        MmfTelemetry<Telemetry> tel = new (config =>
        {
            config.Name = "RotoVR";
            config.Create = true; 
        });
#endif


        
        
        async Task FollowTargetRoutine(CancellationToken cancellationToken)
        {
            if (m_ObservableTarget == null)
                logger.LogError("For FollowObject Mode you need to set target func");
            else
            {
                await Task.Delay(500);                
                
                int targetMs = 1000 / WriteFPS;

                telemetry.MinPower = 30;
                m_homeAngle = m_RotoData.Angle;

                var sendWatch = Stopwatch.StartNew();

                while (!cancellationToken.IsCancellationRequested)
                {
                    var currentTargetAngle = GetTargetAngle();

                    if (currentTargetAngle != null && m_StartTargetAngle != null)
                    {

                        var deltaTargetAngle = currentTargetAngle.Value - m_StartTargetAngle.Value;

                        if ((int)deltaTargetAngle != 0 )
                        {
                            deltaTargetAngle = NormalizeAngle(deltaTargetAngle);

                            var targetRotoAngle = NormalizeAngle(m_StartRotoAngle + deltaTargetAngle);

                            telemetry.TargetAngle = (int)targetRotoAngle;

                            var fDelta = Math.Abs(targetRotoAngle - m_RotoData.Angle);
                            if (fDelta > 180)                            
                                fDelta = 360 - fDelta;
                            
                            

                            if (fDelta >= 1)
                            {
                                m_AntiJump = 0;
                                
                                var maxPower = 80;
                                //var brakePoint = 10;// MaxPower <= 80 ? 10 : 60;
                                //var pmin = 20;
                                //var pmax = 30;
                                int power = maxPower;

                                //power = (int)Filters.EnsureMapRange(fDelta, 0, 45, 20, maxPower);

                                telemetry.MaxPower = Math.Max(telemetry.MaxPower, power);
                                telemetry.Power = power;

                                var dir = (int)GetDirection((int)targetRotoAngle, 0);                                
                                telemetry.Direction =  dir * 10;
                                //telemetry.AvgTargetAngle = avgTargetRotoAngle;
                                telemetry.Delta = fDelta;
                                telemetry.AntiJump = (int)m_AntiJump;

                                RotateToAngle(Direction.Right, (int)targetRotoAngle, power);
                            }
                            else // less than one degree of delta means you are aligned, so increment the anti jump
                            {                                
                                m_AntiJump = sendWatch.ElapsedMilliseconds;
                            }
                        }

                        m_prevTargetAngle = (int)currentTargetAngle;
                    }
                    
                    var elapsedTimeLeft = targetMs - sendWatch.ElapsedMilliseconds;
                    
                    if ((int)elapsedTimeLeft > 0)
                    {
                        SleepAccurate(elapsedTimeLeft);
                    }

                    telemetry.SendFPS = 1000f / Math.Max(1, sendWatch.ElapsedMilliseconds);
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