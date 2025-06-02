using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RotoGLBridge.Models
{
    internal class GameLinkResponse
    {
        public string DeviceName { get; set; }
        public string DeviceType { get; set; }

        public bool InGame { get; set; }

        public override string ToString()
        {
            return $"{DeviceType};;{DeviceName};;{(InGame ? "RESERVED" : "")}";
        }
    }
}
