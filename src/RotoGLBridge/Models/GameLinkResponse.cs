using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RotoGLBridge.Models
{
    internal record GameLinkResponse
    {
        public string DeviceName;
        public string DeviceType;

        public int TcpPort;
        public bool InGame;

        public override string ToString()
        {
            return $"{DeviceType};EMULATOR01;{DeviceName};{TcpPort};{(InGame ? "RESERVED" : "AVAILABLE")}";
        }
    }
}
