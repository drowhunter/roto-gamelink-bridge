
using System.Runtime.InteropServices;

namespace com.rotovr.sdk
{
    public partial class Roto
    {
        [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
        public struct Telemetry
        {
            public int ActualAngle;

            public int TargetAngle;

            public int CappedTargetAngle;

            public int Power;

            public float Delta;

            public int AntiJump;

            public float AngularVelocity;
        
            //public float AvgTargetAngle;

            public float PreciseAngle;

            public float RecieveFPS;

            public float LerpedFPS;

            public float MaxPower;

            public float SendFPS;

            public int Direction;

            public int StopAngle;

            public int MinPower;
        }
        
    }
}