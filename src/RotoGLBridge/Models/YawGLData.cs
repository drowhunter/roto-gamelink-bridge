using System.Globalization;
using System.Runtime.InteropServices;

namespace RotoGLBridge.Models
{
    public struct YawGLData
    {
        public float yaw;
        public float pitch;//-180 to 180
        public float roll; // -180 to 180
        public float amp; //0-254
        public float hz; // if hz == 0 rumblePower = 0
        public float fan;

        public override string ToString()
        {
            var c = CultureInfo.InvariantCulture;

            string retval = string.Format("Y[{0}]P[{1}]R[{2}]",
                yaw.ToString("000.000", c),
                pitch.ToString("000.000", c),
                roll.ToString("000.000", c)
            );

            string vibes = string.Format("V[{0},{1},{2},{3}]", new object[]
            {
                amp.ToString(c),
                amp.ToString(c),
                amp.ToString(c),
                hz.ToString(c)
            });
            retval += vibes;


            string f = string.Format("F[{0},{0}]", fan.ToString(c));
            retval += f;

            return retval;

        }

        /// <summary>
        /// Gets the amplitude as a percentage of its maximum value.
        /// </summary>
        public float rumblePower => (float) (amp / 254f * 100);


        /// <summary>
        /// 0 hz = 0% rumbleSpeed, 254 hz = 100% rumbleSpeed
        /// </summary>
        public float rumbleSpeed => hz != 0 ? (float)(hz / 254f * 100) : 0;

        /// <summary>
        /// Gets the rumblePeriod, in hertz, calculated as the reciprocal of the period (in milliseconds).
        /// </summary>
        public int rumblePeriod => hz != 0 ? (int)(1000f / hz) : 0;

        /// <summary>
        /// Gets the fanSpeed rumbleSpeed as a percentage of its maximum value.
        /// </summary>
        public int fanSpeed => (int) (fan / 254f * 100);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct DeviceParams
    {
        public int packetType;
        public int Power;//4
        public byte Unknown;//8
        public int PitchLimitB;//9
        public int PitchLimitF;//13
        public short RollLimit;//17
        public int YawLimit;//19
        public byte Unknown3;//23
        public short HasYawLimit;//24
        public float YawReturn;//26        
        public byte Unknown5;//30
        public byte HasVibration;//31
        public byte VibrationLimit;//32
        public byte Unknown6;//33
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 37)]
        public byte[] leds;//34-67
                           //public int LedNum;//69

    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct DeviceParams2
    {
        public int PacketType;
        public string DeviceEdition;

        public DeviceParams2(string edition)
        {
            PacketType = 49;
            DeviceEdition = $";;PRO;";
        }

    }
}
