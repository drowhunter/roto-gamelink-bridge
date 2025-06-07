using Sharpie.Extras.Telemetry;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RotoGLBridge.Plugins.YawEmu
{
    public struct YawData
    {
        public byte Command;

        public byte[] Buffer;
    }

    internal class YawTcpCommandConvertor : IByteConvertor<YawData>
    {
        public YawData FromBytes(byte[] data)
        { 

            if (data == null) // || data.Length < 2)
            {
                return default;
            }
            YawData yawData = new()
            {
                Command = data[0],
                Buffer =  data[1..]
            };
            
            return yawData;

        }

        public byte[] ToBytes(ITcpCommand data)
        {
            throw new NotImplementedException();
        }

        public byte[] ToBytes(YawData data)
        {
            throw new NotImplementedException();
        }

        YawData IByteConvertor<YawData>.FromBytes(byte[] data)
        {
            throw new NotImplementedException();
        }
    }
}
