using System.Collections.Concurrent;
using System.Diagnostics;




namespace rotoUSB
{
    public class RotoChair : IRotoChair
    {

        // =============== Constant ===============
        // Constant for  Roto VR Chair Run Mode status 
        public const int MODE_IDLE = 0x00;
        private const int MODE_CAL = 0x01;           // HT is calibrating

        public const int MODE_OBJECT_FOLLOW = 0x02;  // Object follow mode
        public const int MODE_FREE = 0x03;           // Free mode
        public const int MODE_COCKPIT = 0x04;        // Cockpit mode

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






        // =============== Threading ===============
        // Read USB thread
        private CancellationTokenSource _ctsRead = null;
        private bool _isReadingLoop = false;

        private byte[] _usbReadPacket = new byte[ROTO_PACKET_LEN];
        private bool _isPacketInit = false;
        private int _curReadPacketSize = 0;


        // Write USB precision timer  
        private IHighPrecisionTimer _writeTimer; //= new HighPrecisionTimer();
        private ConcurrentQueue<byte[]> _sendQueue = new ConcurrentQueue<byte[]>();
        private IRotoActionStruct _rotoAction = null;
        private readonly IUSBNative _usbNative;
        private readonly IWriteLogger<RotoChair> _logger;
        private bool isSettingZero = false;

        

        // =============== RotoChair Singleton ===========
        // Private static field to hold the single instance
        //private static readonly Lazy<RotoChair> instance = new Lazy<RotoChair>(() => new RotoChair());


        // Public static property to access the instance
        //public static RotoChair Instance => instance.Value;


        // Private constructor for singleton pattern
        public RotoChair(
            IHighPrecisionTimer highPrecisionTimer, 
            IRotoActionStruct rotoAction, 
            IUSBNative usbNative, 
            IWriteLogger<RotoChair> logger)
        {
            _writeTimer = highPrecisionTimer;
            _rotoAction = rotoAction;
            _usbNative = usbNative;
            _logger = logger;
        }


        // Loads the USB library for communication
        public void LoadUSBLibrary()
        {
            // load the USB dll 
            if (!_usbNative.LoadLibrary())
                _logger.WriteLog(GetUSBError());
        }



        // Enables/disables console debug output
        public void EnableConsoleDebug(bool isEnabled = true)
        {
            _logger.EnableConsoleDebug(isEnabled);

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



        // ===============  USB command  ===============

        private int Clamp(int value, int min, int max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }



        // Initiates connection to the Roto VR Chair and checks version
        private void ConnectRoto()
        {
            _logger.WriteLog("Connect Roto and Checkversion !");
            byte[] data = [0xF1, (byte)'A', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
            data[CHECKSUM_INDEX] = ComputeCheckSum(data);
            //EnqueueBaseCommand(data);
            sendBaseCommand(data);
        }




        // Sets the current chair position as the zero position
        public void SetZeroBaseCommand()
        {
            byte[] data = [0xF1, (byte)'B', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
            data[CHECKSUM_INDEX] = ComputeCheckSum(data);


            //_logger.WriteLog("set zero ...");
            isSettingZero = true;

            // reset the object follow degree
            _rotoAction.UpdateObjectFollowDegree(_rotoStatus.MaxPowerLimit, 0);
            EnqueueBaseCommand(data);

            //_logger.WriteLog("set zero 2...");
        }

        // Disconnects the Roto VR Chair
        private void DisconnectRoto()
        {
            _logger.WriteLog("Disconnect Roto !");
            byte[] data = [0xF1, (byte)'Z', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
            data[CHECKSUM_INDEX] = ComputeCheckSum(data);
            // EnqueueBaseCommand(data);
            sendBaseCommand(data);
        }


        // Enqueues a USB command packet for sending
        private void EnqueueBaseCommand(byte[] message)
        {

            _sendQueue.Enqueue(message);

        }




        // Disposes resources and disconnects the chair
        public void Dispose()
        {
           
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
            if (_usbDeviceR != IntPtr.Zero && !_ctsRead.IsCancellationRequested)
            {
                // check if _ctsRead is already disposed



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
        public bool Connect()
        {



            bool result = false;

            if (_usbDeviceR == IntPtr.Zero || _usbDeviceW == IntPtr.Zero)
            {
                _usbDeviceR = _usbNative.OpenUSBDevice();
                _usbDeviceW = _usbNative.OpenUSBDevice();
                if (_usbDeviceR != IntPtr.Zero && _usbDeviceW != IntPtr.Zero)
                {
                    // Enable USB-HID to 115200 baud rate
                    bool success = _usbNative.ConfigUSBDevice(_usbDeviceW);
                    _logger.WriteLog($"Set Feature success: {success}");

                    stopwatch = Stopwatch.StartNew();

                    //_readThread = new Thread(baseReadLoop);
                    // _readThread.Start();

                    // Create new reading task
                    _ctsRead = new CancellationTokenSource();
                    // Start the reading loop in a background task

                    Task.Factory.StartNew(() => {

                        ReadLoop(_ctsRead.Token);
                    }, TaskCreationOptions.LongRunning);
                    //Task.Run(() => ReadLoop(_ctsRead.Token));



                    Thread.Sleep(10);

                    // connect the USB device and check the chair hardware version
                    ConnectRoto();

                    _sendQueue.Clear();
                    _writeTimer.Start(WriteTimerTick, 10);



                    result = true;

                }

                if (!result)
                    _logger.WriteLog(GetUSBError());
            }
            return result;
        }


        // Resets chair movement parameters
        private void ResetChairMovement()
        {
            _rotoAction.Reset();
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
                _rotoAction.UpdateChairSpeed(speed, angle);
            }
        }

        // Sets the object follow degree for tracking
        public void SetObjectFollowDegree(int degree)
        {
            if (_rotoStatus.RunMode == MODE_OBJECT_FOLLOW)
            {
                _rotoAction.UpdateObjectFollowDegree(_rotoStatus.MaxPowerLimit, degree);
            }
        }

        /// <summary>
        /// Activates chair rumble effect.
        /// </summary>
        /// <param name="power">The power.</param>
        /// <param name="milliSeconds">The milli seconds.</param>
        public void SetRumble(int power, ushort milliSeconds)
        {
            if (_rotoStatus.RunMode != MODE_IDLE)
            {
                _rotoAction.UpdateRumble(power, milliSeconds);
            }
        }

        /// <summary>
        /// Stops the chair rumble effect by setting the rumble power and duration to zero.
        /// </summary>
        public void StopRumble()
        {
            if (_rotoStatus.RunMode != MODE_IDLE)
            {
                _rotoAction.UpdateRumble(0, 0);
            }
        }


        
        /// <summary>
        /// Updates chair actions based on current status
        /// </summary>
        /// <returns></returns>
        private bool UpdateChairAction()
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



            bool isValueChange = _rotoAction.GetRotoAction(out motorChanged, out chairSpeed, out objectAngle, out chairAngle, out rumbleChanged, out rumblePower, out rumbleDurationMS);

            //Console.WriteLine("Chair degree: " + objectAngle);

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
                    success = moveChairByAngle(enableMotor, isTurnLeft, chairAngle, Math.Abs(chairSpeed), enableRumble, rumblePower, rumbleDurationMS);
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

                        success = sendBaseCommand(sendControlPacket);
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
        private bool sendBaseCommand(byte[] sendPacket)
        {
            bool success = false;


            if (_usbDeviceW != IntPtr.Zero && sendPacket != null)
            {
#if DEBUG
                string timeStr = stopwatch.ElapsedMilliseconds.ToString("D8");
                stopwatch.Reset();
                stopwatch.Start();

                _logger.WriteLog(">> " + timeStr + "ms  Send Packet: [" + _usbNative.ToHexString(sendPacket, 0, sendPacket.Length) + "]");
#endif

                success = _usbNative.WritePacket(_usbDeviceW, sendPacket);



                if (sendPacket[1] == (byte)'B')
                {
                    _logger.WriteLog("Setting zero complete!");
                    isSettingZero = false;
                }

            }


            if (!success)
            {
                _logger.WriteLog("Error: " + GetUSBError());

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
            _logger.WriteLog($"USB baseReadLoop start");
            while (_isReadingLoop && !token.IsCancellationRequested)
            {
                ReadPacket(buffer, HID_REPORT_LEN);
            }
            _logger.WriteLog($"USB baseReadLoop closed ");

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
            byte[] data = [0xF1, (byte)'M', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];

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
                data[9] = (byte)duration;
            }
            data[CHECKSUM_INDEX] = ComputeCheckSum(data);


            result = sendBaseCommand(data);

            return result;
        }


        // Sends USB command to move chair with rumbling
        private bool moveChairByAngle(bool enableMotor, bool isTurnLeft, int chairAngle, int power, bool enableRumble, int rumblePower, int duration)
        {
            bool result = false;

            byte[] data = [0xF1, (byte)'M', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];


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
                data[9] = (byte)duration;
            }

            data[CHECKSUM_INDEX] = ComputeCheckSum(data);

            result = sendBaseCommand(data);

            return result;
        }





        // Sets the chair's operating mode
        private bool SetV2BaseMode(int newMode, bool motorHardStop, bool enableTracker)
        {
            bool success = false;

            // for debugging
            //_rotoStatus.RunMode = newMode;

            if (_usbDeviceW != IntPtr.Zero)
            {

                byte[] runModePacket = [0xF1, (byte)'S', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];

                runModePacket[2] = (byte)newMode;
                runModePacket[3] = (byte)(motorHardStop ? 1 : 0);


                runModePacket[9] = (byte)((cockpitAngleLimit & 0xFF00) >> 8);
                runModePacket[10] = (byte)((cockpitAngleLimit & 0xFF));


                runModePacket[11] = (byte)RESERVED_BYTE;
                runModePacket[12] = (byte)POWER_LIMIT;

                runModePacket[14] = (byte)(enableTracker ? 1 : 0);

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
        private void parseUSBPacket(byte[] data)
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

                            _logger.WriteLog("rotoVR Chair version: " + chairVersion.ToString() + " detected");

                            if (chairVersion != 2)
                            {
                                _logger.WriteLog("Error: This library only support roto VR explorer! ");
                            }

                            lock (_rotoStatus)
                            {
                                _rotoStatus.ChairVersion = chairVersion;
                            }


                        }
                        break;
                    case (byte)'L':
                        //_logger.WriteLog("Move Left received");
                        break;
                    case (byte)'R':
                        //_logger.WriteLog("Move Right received");
                        break;
                    case (byte)'P':  // Parse the current setting of the rotoVR base

                        try
                        {
                            _logger.WriteLog("Receive Packet: " + _usbNative.ToHexString(data, 0, data.Length));
                            ParseChairSetting(data);
                        }
                        catch (Exception ex)
                        {
                            _logger.WriteLog(ex.ToString());
                        }
                        // printChairStatus();
                        break;
                }
            }
            else
            {
                _logger.WriteLog("USB Packet Checksum Error!!!!");
            }
        } // parse USB Device



        // Reads and processes USB packets
        private bool ReadPacket(byte[] buffer, int reportLen)
        {
            bool completePacketRead = false;
            try
            {
                // read out a USB-HID packet 
                if (!_usbNative.ReadHIDPacket(_usbDeviceR, buffer, reportLen))
                {
                    //_logger.WriteLog("...");
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
                            parseUSBPacket(_usbReadPacket);
                            completePacketRead = true;
                        }
                    }

                    // _logger.WriteLog("==========>  RX Buffer: " + _usbNative.ToHexString(buffer,0, reportLen)) ;

                }
            }
            catch (Exception ex)
            {
                _logger.WriteLog("Read USB Exception: " + ex.ToString());
            }

            return completePacketRead;
        } // Read Device
    }

}
