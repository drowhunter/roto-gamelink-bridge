using Microsoft.Extensions.Logging;

using System.Diagnostics;



namespace com.rotovr.sdk
{
    public class UsbConnector(ILogger<UsbConnector> logger) : IUsbConnector
    {
        const ushort k_vid = 0x04D9;
        const ushort k_pid = 0xB564;

        byte[] m_usbMessage = new byte[19];
        byte[] m_writeBuffer = new byte[33];
        byte[] m_readMessage = new byte[19];

        RotoDataModel m_runtimeModel;



        nint m_device;
        Thread m_connectionThread;
        int m_messageSize;
        bool m_initPacket;

        bool m_reaDevice;


        public event Action<ConnectionStatus> OnConnectionStatus;
        public event Action<RotoDataModel> OnDataChange;

        byte[] ConfigureFeature() => [0x00, 0x01, 0x00, 0xC2, 0x01, 0x00, 0x01, 0x00, 0x08];

        bool IsConnectedAndOpen => m_device != nint.Zero;


        public async Task<bool> ConnectAsync()
        {
            m_device = await Native.OpenFirstHIDDeviceAsync(k_vid, k_pid);

            if (!IsConnectedAndOpen)
                return false;

            byte[] feature = ConfigureFeature();
            var success = await Native.SetFeatureAsync(m_device, ConfigureFeature());
            success = await Native.GetFeatureAsync(m_device, feature);

            logger.LogDebug($"Set Feature success: {success}");

            success = await SendConnectAsync();

            if (success)
            {
                m_reaDevice = true;

                m_connectionThread = new Thread(ReadDeviceThread) { Name = "ReadDeviceThread", IsBackground = true };
                m_connectionThread.Start();
            }
            else
            {
                logger.LogError("SendConnectAsync success was false");
            }
            return success;
        }

        async Task<bool> SendConnectAsync()
        {
            byte[] message = new byte[33];
            for (int i = 0; i < message.Length; i++)
            {
                switch (i)
                {
                    case 0:
                        message[i] = 0x00;
                        break;
                    case 1:
                        message[i] = 19;
                        break;
                    case 2:
                        message[i] = 0xF1;
                        break;
                    case 3:
                        message[i] = 0x41;
                        break;
                    case 20:
                        message[i] = 0x32;
                        break;
                    default:
                        message[i] = 0x00;
                        break;
                }
            }

            logger.LogDebug($"Send connect message: {LogBuffer(message)}");

            var success = await Native.WriteFileAsync(m_device, message);

            logger.LogDebug($"Write file success: {success}");                

            if (success)
                OnConnectionStatus?.Invoke(ConnectionStatus.Connected);

            return success;
        }

        void ReadDeviceThread()
        {
            try
            {
                while (m_reaDevice)
                {
                    var result = Native.ReadFile(m_device, out var buffer, 33);

                    if (!result)
                        return;

                    if (buffer[2] == 0xF1)
                    {
                        m_initPacket = true;
                        for (int i = 0; i < m_readMessage.Length; i++)
                        {
                            m_readMessage[i] = 0x00;
                        }

                        m_messageSize = 0;
                        m_messageSize = buffer[1];
                        for (int i = 0; i < m_messageSize; i++)
                        {
                            m_readMessage[i] = buffer[i + 2];
                        }
                    }
                    else
                    {
                        if (!m_initPacket)
                            return;

                        int startIndex = m_messageSize;
                        m_messageSize += buffer[1];

                        for (int i = 0; i < buffer[1]; i++)
                        {
                            var index = startIndex + i;
                            if (index < m_readMessage.Length)
                                m_readMessage[index] = buffer[i + 2];
                        }

                        if (m_messageSize >= 19)
                        {
                            m_initPacket = false;
                            m_runtimeModel = GetModel(m_readMessage);
                            OnDataChange?.Invoke(m_runtimeModel);
                        }
                    }
                }

            }            
            catch (TaskCanceledException tex)
            {
                logger.LogError(tex, "ReadDeviceThread was cancelled.");
            }
            catch (OperationCanceledException cex)
            {
                logger.LogError(cex, "ReadDeviceThread was cancelled.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to connect to the chair.");
            }

        }

        
        string LogBuffer(byte[] data)
        {
            if (data.Length == 0)
                return "Buffer is Empty";

            string message = $"";
            message = $"{message} {BitConverter.ToString(data).Replace("-", "  ")}";

            return message;
        }

        

        public async Task DisconnectAsync()
        {
            logger.LogDebug("DisconnectAsync");
            m_reaDevice = false;

            await SendDisconnectAsync();

            if (IsConnectedAndOpen)
            {
                await Native.CloseHIDDeviceAsync(m_device);
                OnConnectionStatus?.Invoke(ConnectionStatus.Disconnected);
            }
        }

        

        

        private async Task SendDisconnectAsync()
        {
            byte[] message = new byte[33];
            for (int i = 0; i < message.Length; i++)
            {
                switch (i)
                {
                    case 0:
                        message[i] = 0x00;
                        break;
                    case 1:
                        message[i] = 19;
                        break;
                    case 2:
                        message[i] = 0xF1;
                        break;
                    case 3:
                        message[i] = 0x5A;
                        break;
                    case 20:
                        message[i] = 0x4B;
                        break;
                    default:
                        message[i] = 0x00;
                        break;
                }
            }

            
            var success = await Native.WriteFileAsync(m_device, message);
            logger.LogDebug($"DisconnectAsync success: {success}");
            
        }

        

        public Task SetModeAsync(ModeModel model)
        {
            if (!IsConnectedAndOpen)
                return Task.CompletedTask;

            ResetMessage();

            m_usbMessage[0] = 0xF1;
            m_usbMessage[1] = (byte)'S';

            switch (model.Mode)
            {
                case "IdleMode":
                    m_usbMessage[2] = 0x00;
                    break;
                case "Calibration":
                    m_usbMessage[2] = 0x01;
                    break;
                case "HeadTrack":
                    m_usbMessage[2] = 0x02;
                    break;
                case "FreeMode":
                    m_usbMessage[2] = 0x03;
                    break;
                case "CockpitMode":
                    m_usbMessage[2] = 0x04;
                    break;
            }

            switch (model.ModeParametersModel.MovementMode)
            {
                case "Smooth":
                    m_usbMessage[3] = 0x00;
                    break;
                case "Jerky":
                    m_usbMessage[3] = 0x01;
                    break;
            }

            logger.LogDebug($"Set Mode: {model.Mode}");

            m_usbMessage[9] = (byte)model.ModeParametersModel.TargetCockpit;
            m_usbMessage[11] = 40;
            m_usbMessage[12] = (byte)model.ModeParametersModel.MaxPower;
            m_usbMessage[14] = 0x01;

            byte sum = ByteSum(m_usbMessage);
            m_usbMessage[18] = sum;

            return Native.WriteFileAsync(m_device, PrepareWriteBuffer(m_usbMessage));
        }

        byte[] PrepareWriteBuffer(byte[] message)
        {
            for (int i = 0; i < m_writeBuffer.Length; i++)
            {
                m_writeBuffer[i] = 0x00;
            }

            m_writeBuffer[0] = 0x00;
            m_writeBuffer[1] = 19;
            for (int i = 0; i < message.Length; i++)
            {
                m_writeBuffer[i + 2] = message[i];
            }

            return m_writeBuffer;
        }

        public void TurnToAngle(RotateToAngleModel model)
        {
            if (!IsConnectedAndOpen)
                return;

            ResetMessage();

            m_usbMessage[0] = 0xF1;
            m_usbMessage[1] = (byte)'M';
            m_usbMessage[2] = 0x01;

            if (model.Direction.Equals("Right"))
            {
                m_usbMessage[3] = 0x52;
            }
            else
            {
                m_usbMessage[3] = 0x4C;
            }

            var angle = model.Angle;

            if (angle == 360)
                angle -= 1;

            if (angle >= 256)
            {
                m_usbMessage[4] = 0x01;
                m_usbMessage[5] = (byte)(angle - 256);
            }
            else
            {
                m_usbMessage[4] = 0x00;
                m_usbMessage[5] = (byte)angle;
            }

            m_usbMessage[6] = (byte)model.Power;
            m_usbMessage[7] = 0x00;

            byte sum = ByteSum(m_usbMessage);
            m_usbMessage[18] = sum;

            
            var s = Stopwatch.StartNew();
            //xxx should this be awaited?
            var r = Native.WriteFileAsync(m_device, PrepareWriteBuffer(m_usbMessage))
                .ContinueWith(r =>
                {
                    s.Stop();
                    string message = $"[{s.ElapsedMilliseconds} ms] - TTA({angle}) = " + r.Result;


                    logger.LogDebug(message);
                }, TaskContinuationOptions.NotOnRanToCompletion);
            
        }

        public void PlayRumble(RumbleModel model)
        {
            if (!IsConnectedAndOpen)
                return;

            ResetMessage();

            m_usbMessage[0] = 0xF1;
            m_usbMessage[1] = (byte)'M';
            m_usbMessage[2] = 0x00;
            m_usbMessage[3] = 0x00;
            m_usbMessage[4] = 0x00;
            m_usbMessage[5] = 0x00;
            m_usbMessage[6] = 0x00;
            m_usbMessage[7] = 0x01;
            m_usbMessage[8] = (byte)model.Power;
            m_usbMessage[9] = (byte)(int)(model.Duration * 10);

            byte sum = ByteSum(m_usbMessage);
            m_usbMessage[18] = sum;

            
            var result = Native.WriteFileAsync(m_device, PrepareWriteBuffer(m_usbMessage))
            .ContinueWith(r => {
                if (r.IsFaulted)
                {
                    logger.LogError(r.Exception, "Failed to play rumble.");
                }
                else
                {
                    logger.LogDebug($"Play Rumble success: {r.Result}");
                    if(r.Result)
                        logger.LogDebug($"Rumble played successfully: {model.Power} for {model.Duration} seconds.");
                    else
                        logger.LogWarning("Rumble play failed.");
                }
            }, TaskContinuationOptions.NotOnRanToCompletion);
            
            
        }

        void ResetMessage()
        {
            for (int i = 0; i < m_usbMessage.Length; i++)
            {
                m_usbMessage[i] = 0x00;
            }
        }

        byte ByteSum(byte[] blk)
        {
            byte sum = 0;

            for (int i = 0; i <= 17; i++)
            {
                sum = (byte)(sum + blk[i] & 0xff);
            }

            return sum;
        }

        private RotoDataModel GetModel(byte[] rawData)
        {
            RotoDataModel model = new ();
            var modeType = (ModeType)rawData[2];
            model.Mode = modeType.ToString();
            switch (rawData[2])
            {
                case 0:
                    model.Mode = ModeType.IdleMode.ToString();
                    break;
                case 1:
                    model.Mode = ModeType.Calibration.ToString();
                    break;
                case 2:
                    model.Mode = ModeType.HeadTrack.ToString();
                    break;
                case 3:
                    model.Mode = ModeType.FreeMode.ToString();
                    break;
                case 4:
                    model.Mode = ModeType.CockpitMode.ToString();
                    break;
                case 5:
                    model.Mode = ModeType.Error.ToString();
                    break;
            }

            switch (rawData[5])
            {
                case 0:
                    model.Angle = rawData[6];
                    break;
                case 1:
                    int angle = rawData[6];
                    model.Angle = angle + 256;
                    break;
            }

            return model;
        }
    }
}