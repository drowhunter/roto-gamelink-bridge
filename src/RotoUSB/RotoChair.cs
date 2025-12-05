/*
 * 
 *  Edward Chan
 * 
 *  Revision history:
 * 
 *          V1.1 -   Add  Event delegate to support  Safety Sensor / Emergency Button triggered event
 *                   Support resume to previous mode or  default IDLE mode
 *               -   Add SetV2BaseMode() function with an option to disable the BASE running mode sound
 *               -   Clarify the rumble duration in term of 100ms per unit 
 *               -   update SetObjectFollowDegree() with speed option
 *               
 * 
 */



using System.Collections.Concurrent;
using System.Diagnostics;



namespace rotoUSB
{
    

    public class RotoChair : IDisposable, IRotoChair
    {
        private IUSBNative _usbNative;

        public bool _isConsoleDebug = false;

        // =============== Constant ===============
        // Constant for  Roto VR Chair Run Mode status 
        public const int MODE_IDLE = 0x00;
        private const int MODE_CAL = 0x01;           // HT is calibrating

        public const int MODE_OBJECT_FOLLOW = 0x02;  // Object follow mode
        public const int MODE_FREE = 0x03;           // Free mode
        public const int MODE_COCKPIT = 0x04;        // Cockpit mode



        public const int ERROR_EMERGENCY_STOP = 0x80;   //  v1.1:   Wireless Emergency stop button triggerd
        public const int ERROR_NONE = 0x00;


        private const int MODE_APP = 0x05;           // Android/Quest APP control mode 




        // Constant for   Roto VR Chair connection status 
        private const int STATE_HT_CONNECT = 0x01;       //   head tracker connected
        private const int STATE_ANDROID_CONNECT = 0x02;  //   android connected  
        private const int STATE_HT_IR_DETECT = 0x04;     //   head tracker detected   
        private const int STATE_HT_CAL_DONE = 0x08;      //   head tracker calibrated


        private const int STATE_PC_CONNECT = 0x10;       //   PC connected - Not use for PC 

        // FOR USB BASE ERROR MODE - Internal Use only
        private const int MODE_EMERGENCY_STOP = 0x10;           //   Emergency stop from HT 
        private const int MODE_BASE_ROTATION_STOP = 0x20;       //   Base self-rotation stop (Base Move)
        private const int MODE_MOTOR_STALL_STOP = 0x40;         //   Motor Stall             (Hold)






        private const int DEAFAULT_COCKPIT_DEGREE = 30;
        private const int DEAFAULT_HT_SENSITIVTY = 30;


        // USB packet constant
        private const int HID_REPORT_LEN = USBNative.USB_REPORT_LEN;
        private const int ROTO_PACKET_LEN = USBNative.ROTO_PACKET_LEN;
        private const int CHECKSUM_INDEX = (ROTO_PACKET_LEN - 1);
        private const byte RESERVED_BYTE = 0x00;


        // ======== roto default power up configuration =====
        private int POWER_LIMIT = 100;
        private int cockpitAngleLimit = 60;

        private bool ENABLE_HT = false;

        // =============== roto Chair Version ==========
        private IntPtr _usbDeviceR;
        private IntPtr _usbDeviceW;


        private RotoStatus _rotoStatus = new RotoStatus();
        private readonly object _statusLock = new object();

        private Stopwatch stopwatch;



        // ============= event delegate ===========

        // run mode event
        public delegate void RunModeChangeHandler(int newRunMode);
        public event RunModeChangeHandler RunModeChanged;


        // error mode event
        public delegate void ErrorModeHandler(int errorMode);
        public event ErrorModeHandler ErrorModeChanged;




        // =============== Threading ===============
        // Read USB thread
        private CancellationTokenSource _ctsRead = null;
        private bool _isReadingLoop = false;

        private byte[] _usbReadPacket = new byte[ROTO_PACKET_LEN];
        private bool _isPacketInit = false;
        private int _curReadPacketSize = 0;


        // Write USB precision timer  
        private HighPrecisionTimer _writeTimer = new HighPrecisionTimer();
        private ConcurrentQueue<byte[]> _sendQueue = new ConcurrentQueue<byte[]>();
        private IRotoActionStruct chairAction = null;
        
        private bool isSettingZero = false;


        // v1.1 new features
        private bool enableModeResume = false;
        private bool skipBASESound = false;
        private int lastRunMode = -1;
        private int lastErrorMode = 0;


       
        private readonly IWriteLogger<USBNative> logger;

        // Private constructor for singleton pattern
        public RotoChair(IUSBNative usbNative, IRotoActionStruct chairAction, IWriteLogger<USBNative> logger)
        {
            this.chairAction = chairAction;
            this.logger = logger;
            _usbNative = usbNative;
        }


        // Loads the USB library for communication
        public void LoadUSBLibrary()
        {
            // load the USB dll 
            if (!_usbNative.LoadLibrary())
                WriteLog(GetUSBError());
        }



        // Enables/disables console debug output
        public void EnableConsoleDebug(bool isEnabled = true)
        {
            _isConsoleDebug = isEnabled;

        }


        // Retrieves the last USB error message
        public string GetUSBError()
        {
            return _usbNative.LastErrorMessage;
        }


        // ============== Helper functions =============

        // Computes checksum for a message buffer
        private static byte ComputeCheckSum(byte[] message)
        {
            int sum = 0;
            for (int i = 0; i < Math.Min(CHECKSUM_INDEX, message.Length); i++)
                sum += message[i];

            return (byte)(sum % 256);
        }





        // write debug logs to console and file
        private void WriteLog(string log)
        {            
            logger.WriteLog(log);
        }



        // ===============  USB command  ===============
        public int Clamp(int value, int min, int max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }



        // Initiates connection to the Roto VR Chair and checks version
        private void ConnectRoto()
        {
            WriteLog("Connect Roto and Checkversion !");
            byte[] data = new byte[ROTO_PACKET_LEN] { 0xF1, (byte)'A', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            data[CHECKSUM_INDEX] = ComputeCheckSum(data);
            //EnqueueBaseCommand(data);
            SendBaseCommand(data);
        }




        // Sets the current chair position as the zero position
        public void SetZeroBaseCommand()
        {
            byte[] data = new byte[ROTO_PACKET_LEN] { 0xF1, (byte)'B', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            data[CHECKSUM_INDEX] = ComputeCheckSum(data);

            //WriteLog("set zero ...");
            isSettingZero = true;

            // reset the object follow degree
            chairAction.UpdateObjectFollowDegree(_rotoStatus.MaxPowerLimit, 0);
            EnqueueBaseCommand(data);

            //WriteLog("set zero 2...");
        }

        // Disconnects the Roto VR Chair
        private void DisconnectRoto()
        {
            WriteLog("Disconnect Roto !");
            byte[] data = new byte[ROTO_PACKET_LEN] { 0xF1, (byte)'Z', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            data[CHECKSUM_INDEX] = ComputeCheckSum(data);
            // EnqueueBaseCommand(data);
            SendBaseCommand(data);
        }


        // Enqueues a USB command packet for sending
        private void EnqueueBaseCommand(byte[] message)
        {
            _sendQueue.Enqueue(message);
        }




        // Disposes resources and disconnects the chair
        public void Dispose()
        {
            _isConsoleDebug = false;
            Disconnect();
        }


        // Closes the USB write task
        private void CloseWriteTask()
        {
            Console.WriteLine("Close USB Write Task");
            if (_usbDeviceW != IntPtr.Zero)
            {
                _writeTimer.Stop();

                // Send disconnect USB command to stop write Timer
                DisconnectRoto();

                // close the USB connection
                _usbNative.CloseUSBDevice(_usbDeviceW);
            }
            _usbDeviceW = IntPtr.Zero;
        }


        // Closes the USB read task
        private void CloseReadTask()
        {
            Console.WriteLine("Close USB Read Task");
            if (_usbDeviceR != IntPtr.Zero)
            {
                _ctsRead.Cancel();
                _ctsRead.Dispose(); // Clean up
            }
        }



        // Disconnects the chair and cleans up resources
        public void Disconnect()
        {

            CloseReadTask();
            CloseWriteTask();

            lock (_statusLock)
            {
                _rotoStatus.USBConnected = false;
            }
            Console.WriteLine("RotoChair disconnect - done");

        }



        // Connects to the Roto VR Chair
        public bool Connect(bool reConnect = false)
        {

            bool result = false;

            if (_usbDeviceR != IntPtr.Zero && reConnect)
            {
                WriteLog("RotoChair already connected.");
                // clsoe the device first

                CloseReadTask();
            }

            
            if (_usbDeviceR == IntPtr.Zero)
                _usbDeviceR = _usbNative.OpenUSBDevice();

            if (_usbDeviceW == IntPtr.Zero) 
                _usbDeviceW = _usbNative.OpenUSBDevice();

            if (_usbDeviceR != IntPtr.Zero && _usbDeviceW != IntPtr.Zero)
            {
                // Enable USB-HID to 115200 baud rate
                bool success = _usbNative.ConfigUSBDevice(_usbDeviceW);
                WriteLog($"Set Feature success: {success}");

                stopwatch = Stopwatch.StartNew();

                //_readThread = new Thread(baseReadLoop);
                // _readThread.Start();

                // Create new reading task
                _ctsRead = new CancellationTokenSource();
                // Start the reading loop in a background task
                Task.Run(() => { 
                    Thread.CurrentThread.IsBackground = true;
                    Thread.CurrentThread.Name = "RotoChair USB Read Thread";
                    ReadLoop(_ctsRead.Token);
                });


                Thread.Sleep(10);

                // connect the USB device and check the chair hardware version
                ConnectRoto();

                _sendQueue.Clear();
                _writeTimer.Start(WriteTimerTick, 10);

                result = true;
            }

            if (!result)
                WriteLog(GetUSBError());
            
            return result;
        }


        // Resets chair movement parameters
        private void ResetChairMovement()
        {
            chairAction.Reset();
        }


        // Moves the chair at a specified speed (+/-100) for at most 1 seconds
        public void MoveChair(int speed)
        {
            MoveChairByAngle(speed, 360);
        }

        // Moves the chair by a specified angle and speed
        public void MoveChairByAngle(int speed, int angle)
        {
            if (_rotoStatus.RunMode == MODE_COCKPIT || _rotoStatus.RunMode == MODE_FREE)
            {
                chairAction.UpdateChairSpeed(speed, angle);
            }
        }

        // Sets the object follow degree for tracking
        public void SetObjectFollowDegree(int degree, int speed)
        {
            if (_rotoStatus.RunMode == MODE_OBJECT_FOLLOW)
            {
                chairAction.UpdateObjectFollowDegree(speed, degree);
            }
        }


        // Activates chair rumble effect
        public void SetRumble(int power, ushort milliSeconds)
        {
            if (_rotoStatus.RunMode != MODE_IDLE)
            {
                chairAction.UpdateRumble(power, Clamp(milliSeconds, 0, 25400));
            }
        }



        // Keep chair rumbling 
        public void KeepRumbling(int power)
        {
            if (_rotoStatus.RunMode != MODE_IDLE)
            {
                chairAction.UpdateRumble(power, 25500);
            }
        }



        // Stops the chair rumble effect
        public void StopRumble()
        {
            if (_rotoStatus.RunMode != MODE_IDLE)
            {
                chairAction.UpdateRumble(0, 0);
            }
        }


        // Updates chair actions based on current status
        public bool UpdateChairAction()
        {
            bool success = true;

            bool enableMotor = false;
            bool enableRumble = false;
            bool isTurnLeft = false;

            bool motorChanged = false;
            bool rumbleChanged = false;


            int chairSpeed;
            int objectAngle;
            int rumblePower;
            int rumbleDurationMS;

            int chairAngle;



            bool isValueChange = chairAction.GetRotoAction(out motorChanged, out chairSpeed, out objectAngle, out chairAngle, out rumbleChanged, out rumblePower, out rumbleDurationMS);


            Console.WriteLine("Chair degree: " + objectAngle);

            // Only object following provides realtime chair angle synchronization
            if (_rotoStatus.RunMode == MODE_OBJECT_FOLLOW)
            {

                if (isSettingZero == false)
                {
                    enableRumble = rumbleChanged;
                    enableMotor = true; // for object tracking, always send command to chair even no value change
                    success = MoveChairTracking(enableMotor, objectAngle, chairSpeed, enableRumble, rumblePower, rumbleDurationMS);

                }

            }
            else if (_rotoStatus.RunMode == MODE_FREE || _rotoStatus.RunMode == MODE_COCKPIT)
            {
                enableRumble = rumbleChanged;
                enableMotor = motorChanged;
                isTurnLeft = (chairSpeed < 0 ? true : false);

                // why keep movement??
                if (isValueChange)
                {

                    //Console.WriteLine("Move left?"+ isTurnLeft + " chair angle to " + chairAngle);
                    success = MoveChairByAngle(enableMotor, isTurnLeft, chairAngle, Math.Abs(chairSpeed), enableRumble, rumblePower, rumbleDurationMS);
                }
            }

            return success;

        }




        // Timer tick handler for writing USB commands
        private void WriteTimerTick()
        {
            bool success = false;

            try
            {
                if (_usbDeviceW != IntPtr.Zero)
                {
                    // top priority is running the chair command, then update chair movement
                    if (_sendQueue.TryDequeue(out byte[] sendControlPacket))
                    {

                        success = SendBaseCommand(sendControlPacket);
                    }
                    else
                        success = UpdateChairAction();
                }
            }
            catch
            {
                success = false;

            }

        }




        // Sends a base command to the chair
        private bool SendBaseCommand(byte[] sendPacket)
        {
            bool success = false;


            if (_usbDeviceW != IntPtr.Zero && sendPacket != null)
            {

                string timeStr = stopwatch.ElapsedMilliseconds.ToString("D8");
                stopwatch.Reset();
                stopwatch.Start();

                WriteLog(">> " + timeStr + "ms  Send Packet: [" + _usbNative.ToHexString(sendPacket, 0, sendPacket.Length) + "]");


                success = _usbNative.WritePacket(_usbDeviceW, sendPacket);



                if (sendPacket[1] == (byte)'B')
                {
                    WriteLog("Setting zero complete!");
                    isSettingZero = false;
                }

            }


            if (!success)
            {
                WriteLog("Error: " + GetUSBError());

                // if write error, stop read/write thread
                _writeTimer.Stop();
                _usbDeviceW = IntPtr.Zero;
                _isReadingLoop = false;

                lock (_statusLock)
                {
                    _rotoStatus.USBConnected = false;
                }

            }

            return success;
        }






        // Reads USB packets in a loop
        private void ReadLoop(CancellationToken token)
        {
            byte[] buffer = new byte[HID_REPORT_LEN];

            _isReadingLoop = true;
            WriteLog($"USB baseReadLoop start");
            while (_isReadingLoop && !token.IsCancellationRequested)
            {
                ReadPacket(buffer, HID_REPORT_LEN);
            }
            WriteLog($"USB baseReadLoop closed ");

            // Disconnect USB device if possible
            _usbNative.CloseUSBDevice(_usbDeviceR);

            _usbDeviceR = IntPtr.Zero;
            _isReadingLoop = false;
        }






        // Retrieves the current chair status
        public RotoStatus GetRotoStatus()
        {
            lock (_statusLock)
            {
                return (RotoStatus)_rotoStatus.Clone();

            }

        }






        // Sends USB command to move chair in object follow mode with rumbling
        private bool MoveChairTracking(bool enableMotor, int objectAngle, int power, bool enableRumble, int rumblePower, int duration)
        {

            bool result = false;
            byte[] data = new byte[ROTO_PACKET_LEN] { 0xF1, (byte)'M', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            int rumbleDuration100ms = Clamp(duration / 100, 0, 255);


            if (enableMotor)
            {
                data[2] = 0x01;  // enable motor                
                data[3] = 0x00;  // 0x00 
                data[4] = (byte)((objectAngle % 0xFF00) >> 8);
                data[5] = (byte)(objectAngle & 0xFF);
                data[6] = (byte)Clamp(power, 0, 100);
            }

            if (enableRumble)
            {
                data[7] = (byte)0x01; // enable rumble
                data[8] = (byte)Clamp(rumblePower, 0, 100);
                data[9] = (byte)rumbleDuration100ms;  // duration in 100ms unit
            }
            data[CHECKSUM_INDEX] = ComputeCheckSum(data);

            result = SendBaseCommand(data);

            return result;
        }


        // Sends USB command to move chair with rumbling
        private bool MoveChairByAngle(bool enableMotor, bool isTurnLeft, int chairAngle, int power, bool enableRumble, int rumblePower, int duration)
        {
            bool result = false;

            byte[] data = new byte[ROTO_PACKET_LEN] { 0xF1, (byte)'M', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            int rumbleDuration100ms = Clamp(duration / 100, 0, 255);


            if (enableMotor)
            {
                data[2] = 0x01; // enable motor
                if (isTurnLeft)
                    data[3] = 0x4C; // anti-clockwise (Turn left)
                else
                    data[3] = 0x52; // clockwise (Turn right)


                data[4] = (byte)((chairAngle % 0xFF00) >> 8);
                data[5] = (byte)(chairAngle & 0xFF);

                data[6] = (byte)Clamp(power, 0, 100);
            }

            if (enableRumble)
            {
                data[7] = (byte)0x01; // enable rumble
                data[8] = (byte)Clamp(rumblePower, 0, 100);
                data[9] = (byte)rumbleDuration100ms;  // duration in 100ms unit
            }

            data[CHECKSUM_INDEX] = ComputeCheckSum(data);

            result = SendBaseCommand(data);

            return result;
        }

        // Set the BASE mode sound indication
        // isEnabled = true, BASE will play the corresponding beep beep sound  when mode changed by Window device
        //           = false, BASE will keep silence when mode changed by  Window device
        public void EnableModeSound(bool isEnabled)
        {
            skipBASESound = !isEnabled;
        }

        // Set the BASE behavior when emergency stop / safety button resume to normal condition
        // isEnabled = true,  BASE will resume back to its previous mode
        //           = false, BASE will return to IDLE mode
        public void EnableModeResume(bool isEnabled)
        {
            enableModeResume = isEnabled;
        }






        // Sets the chair's operating mode
        private bool SetV2BaseMode(int newMode, bool motorHardStop, bool enableTracker)
        {
            bool success = false;
            // for debugging
            // _rotoStatus.RunMode = newMode;

            if (_usbDeviceW != IntPtr.Zero)
            {
                byte baseConfig = 0;
                byte[] runModePacket = new byte[ROTO_PACKET_LEN] { 0xF1, (byte)'S', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };


                runModePacket[2] = (byte)newMode;
                runModePacket[3] = (byte)(motorHardStop ? 1 : 0);

                runModePacket[9] = (byte)((cockpitAngleLimit & 0xFF00) >> 8);
                runModePacket[10] = (byte)((cockpitAngleLimit & 0xFF));

                runModePacket[11] = (byte)RESERVED_BYTE;
                runModePacket[12] = (byte)POWER_LIMIT;
                runModePacket[14] = (byte)(enableTracker ? 1 : 0);

                // [15]  reserved for [report interval]
                if (enableModeResume)
                {
                    //  0x00 - emergency stop resume to IDLE,   0x01 - emergency stop resume to previous mode without any sound
                    baseConfig = (byte)(baseConfig | (1 << 7));
                }

                if (skipBASESound)
                {
                    // runModePacket[16] = (byte) (skipBASESound    ? 1 : 0);    //  0x00 - default change mode has sound,  default change mode has no sound
                    baseConfig = (byte)(baseConfig | (1 << 6));
                }

                runModePacket[16] = baseConfig;
                runModePacket[CHECKSUM_INDEX] = ComputeCheckSum(runModePacket);


                EnqueueBaseCommand(runModePacket);


                success = true;
            }
            return success;
        }


       


        // Sets the chair to object follow mode
        public bool SetObjectFollowMode()
        {
            SetZeroBaseCommand();
            return SetV2BaseMode(MODE_OBJECT_FOLLOW, false, ENABLE_HT);
        }

        // Sets the chair to idle mode
        public bool SetIdleMode()
        {
            return SetV2BaseMode(MODE_IDLE, false, ENABLE_HT);
        }

        // Sets the chair to free mode
        public bool SetFreeMode()
        {
            return SetV2BaseMode(MODE_FREE, false, ENABLE_HT);
        }


        // Sets the chair to cockpit mode with a specified angle limit
        public bool SetCockpitMode(int cockpitLimit)
        {

            cockpitAngleLimit = Clamp(cockpitLimit, 60, 140);
            return SetV2BaseMode(MODE_COCKPIT, false, ENABLE_HT);
        }






        // Parses chair settings from received USB packet
        private void ParseChairSetting(byte[] data)
        {

            int runMode = (data[2]) & 0x0F;
            int errorMode = (data[2]) & 0xF0;

            bool htConnected = ((data[3] & STATE_HT_CONNECT) == STATE_HT_CONNECT) ? true : false;
            bool androidConnected = ((data[3] & STATE_ANDROID_CONNECT) == STATE_ANDROID_CONNECT) ? true : false;

            int baseDegree = (int)((data[5] << 8) + data[6]);
            int compassDegree = (int)((data[7] << 8) + data[8]);

            int cockpitDegreeLimit = (int)((data[9] << 8) + data[10]);
            int htSensitivityDegree = data[11];

            int maxPower = data[12];
            int firmwareVersion = data[13];

            bool htEnabled = ((data[14] & 0x01) == 1) ? true : false;


            // error mode delegate
            if (errorMode != lastErrorMode)
            {
                if (ErrorModeChanged != null)
                    ErrorModeChanged(errorMode);
                lastErrorMode = errorMode;
            }


            // run mode delegate
            if (runMode != lastRunMode)
            {
                if (RunModeChanged != null)
                {
                    if (runMode == RotoChair.MODE_IDLE)
                    {
                        ResetChairMovement();
                    }
                    RunModeChanged(runMode);
                }
                lastRunMode = runMode;
            }





            lock (_statusLock)
            {
                _rotoStatus.USBConnected = true;
                _rotoStatus.HTConnected = htConnected;
                _rotoStatus.AndroidConnected = androidConnected;
                _rotoStatus.HTDegree = compassDegree;
                _rotoStatus.HTEnabled = htEnabled;
                _rotoStatus.HTSensitivityDegree = htSensitivityDegree;

                // _rotoStatus.BaseDegree = baseDegree;
                _rotoStatus.FirmwareVersion = firmwareVersion;
                _rotoStatus.ErrorMode = errorMode;
                _rotoStatus.RunMode = runMode;
                _rotoStatus.MaxPowerLimit = maxPower;
                _rotoStatus.CockpitDegreeLimit = cockpitDegreeLimit;

                // for latest firmware, we upgrade the base degree sensor accuracy to 12-bit resolution
                if (firmwareVersion > 0x20)
                {
                    int resolution = (int)((data[15] << 8) + data[16]);
                    _rotoStatus.BaseDegree = 360.0 - (resolution * 360.0 / 4096.0);
                    if (_rotoStatus.BaseDegree >= 360.0)
                        _rotoStatus.BaseDegree = 0.0;
                }
                else  // backward compatiable to old firmware  
                {
                    _rotoStatus.BaseDegree = (int)((data[5] << 8) + data[6]);
                }
            }
        }




        // Parses incoming USB packets
        private void ParseUSBPacket(byte[] data)
        {
            byte checkSum = ComputeCheckSum(data);
            if (data[0] == 0xF1 && data[ROTO_PACKET_LEN - 1] == checkSum)
            {
                byte header = data[1];
                switch (header)
                {
                    case (byte)'A': // rotoVR base device will reply "rotoVR"
                        if (data[2] == 'r' && data[3] == 'o' && data[4] == 't' && data[5] == 'o')
                        {
                            // get chair revision number
                            int chairVersion = data[6] - (int)'0';

                            WriteLog("rotoVR Chair version: " + chairVersion.ToString() + " detected");

                            if (chairVersion != 2)
                            {
                                WriteLog("Error: This library only support roto VR explorer! ");
                            }

                            lock (_rotoStatus)
                            {
                                _rotoStatus.ChairVersion = chairVersion;
                            }


                        }
                        break;
                    case (byte)'L':
                        //WriteLog("Move Left received");
                        break;
                    case (byte)'R':
                        //WriteLog("Move Right received");
                        break;
                    case (byte)'P':  // Parse the current setting of the rotoVR base

                        try
                        {
                            WriteLog("Receive Packet: " + _usbNative.ToHexString(data, 0, data.Length));
                            ParseChairSetting(data);
                        }
                        catch (Exception ex)
                        {
                            WriteLog(ex.ToString());
                        }
                        // printChairStatus();
                        break;
                }
            }
            else
            {
                WriteLog("USB Packet Checksum Error!!!!");
            }
        } // parse USB Device



        // Reads and processes USB packets
        private bool ReadPacket(byte[] buffer, int reportLen)
        {
            bool completePacketRead = false;

            // read out a USB-HID packet 
            if (!_usbNative.ReadHIDPacket(_usbDeviceR, buffer, reportLen))
            {
                //WriteLog("...");
            }
            else
            {
                // create Start of Packet
                if (buffer[2] == 0xF1)
                {
                    _isPacketInit = true;
                    Array.Clear(_usbReadPacket, 0, _usbReadPacket.Length);
                    _curReadPacketSize = buffer[1];
                    Array.Copy(buffer, 2, _usbReadPacket, 0, _curReadPacketSize);
                }
                else if (_isPacketInit)  // concate pending packet 
                {
                    int startIndex = _curReadPacketSize;
                    _curReadPacketSize += buffer[1];
                    int copyLength = Math.Min(buffer[1], _usbReadPacket.Length - startIndex);
                    Array.Copy(buffer, 2, _usbReadPacket, startIndex, copyLength);

                    // if a full packet complete, parse the value
                    if (_curReadPacketSize >= 19)
                    {
                        _isPacketInit = false;

                        // Console.WriteLine("RX Buffer: " + buffer[0]);
                        ParseUSBPacket(_usbReadPacket);
                        completePacketRead = true;
                    }
                }
                // WriteLog("==========>  RX Buffer: " + USBNative.ToHexString(buffer,0, reportLen)) ;
            }

            return completePacketRead;
        } // Read Device
    }

}
