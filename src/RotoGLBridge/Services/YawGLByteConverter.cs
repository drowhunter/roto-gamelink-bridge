using RotoGLBridge.Models;
using Sharpie.Extras.Telemetry;
using System.Text;
using System.Text.RegularExpressions;

namespace RotoGLBridge.Services
{
    

    public class YawGLByteConverter : IByteConvertor<YawGLData>
    {
        static Regex rot = new Regex($@"Y\[(?<yaw>-?[\d.]+)\]P\[(?<pitch>-?[\d.]+)\]R\[(?<roll>-?[\d.]+)\]");

        static Regex vibes = new Regex($@"V\[(?<amp>\d+?),\d*?,\d*?,(?<hz>\d*?)\]");

        static Regex fan = new Regex($@"F\[(?<fan>\d+?)");

        //static CultureInfo c = CultureInfo.InvariantCulture;

        

        YawGLData previousData;

        public YawGLData FromBytes(byte[] data)
        {
            var dataString = Encoding.ASCII.GetString(data);


            var yawGLData = new YawGLData();

            float fullCircle(float degrees) => (degrees + 360) % 360;

            if (data.Length > 4)
            {
                try
                {
                    var r = rot.Match(dataString);
                    if (r.Success)
                    {

                        yawGLData.yaw = fullCircle(float.Parse(r.Groups["yaw"].Value));      //-180-180
                        yawGLData.pitch = fullCircle(float.Parse(r.Groups["pitch"].Value));  //-180-180
                        yawGLData.roll = fullCircle(float.Parse(r.Groups["roll"].Value));    //-180-180

                    }

                    var v = vibes.Match(dataString);
                    if (v.Success)
                    {
                        yawGLData.amp = byte.Parse(v.Groups["amp"].Value) / byte.MaxValue;
                        yawGLData.hz = byte.Parse(v.Groups["hz"].Value) / byte.MaxValue;
                    }

                    var f = fan.Match(dataString);
                    if (f.Success)
                    {
                        yawGLData.fan = byte.Parse(f.Groups["fan"].Value) / byte.MaxValue;
                    }

                    previousData = yawGLData;
                }
                catch
                {
                    // Handle parsing errors if necessary  
                }
            }
            else
            {
                yawGLData = previousData;
            }

            return yawGLData;


        }

        public byte[] ToBytes(YawGLData data)
        {
            byte[] array = Encoding.ASCII.GetBytes(data.ToString());
            return array;
        }

        
    }
}
