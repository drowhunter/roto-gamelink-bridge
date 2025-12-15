using RotoGLBridge.Models;

using Sharpie.Helpers.Telemetry;

using System.Text;
using System.Text.RegularExpressions;

namespace RotoGLBridge.Plugins.GameLink
{
    

    public class YawGLByteConverter : IByteConverter<YawGLData>
    {
        static Regex rot = new Regex($@"Y\[(?<Yaw>-?[\d.]+)\]P\[(?<Pitch>-?[\d.]+)\]R\[(?<Roll>-?[\d.]+)\]");

        static Regex vibes = new Regex($@"V\[(?<ampPercent>\d+?),\d*?,\d*?,(?<Hz>\d*?)\]");

        static Regex fan = new Regex($@"F\[(?<fanPcercent>\d+?)");

        //static CultureInfo c = CultureInfo.InvariantCulture;

        

        YawGLData previousData;

        public YawGLData FromBytes(byte[] data)
        {
            var dataString = Encoding.ASCII.GetString(data);


            var yawGLData = new YawGLData();

            float fullCircle(float degrees) => (degrees + 360) % 360;

            if (data.Length > 4 && dataString.StartsWith("Y["))
            {
                try
                {
                    var r = rot.Match(dataString);
                    if (r.Success)
                    {

                        yawGLData.yaw = fullCircle(float.Parse(r.Groups["Yaw"].Value));      //-180-180
                        yawGLData.pitch = fullCircle(float.Parse(r.Groups["Pitch"].Value));  //-180-180
                        yawGLData.roll = fullCircle(float.Parse(r.Groups["Roll"].Value));    //-180-180

                    }

                    var v = vibes.Match(dataString);
                    if (v.Success)
                    {
                        yawGLData.amp = byte.Parse(v.Groups["ampPercent"].Value);
                        yawGLData.hz = byte.Parse(v.Groups["Hz"].Value);/// byte.MaxValue;
                    }

                    var f = fan.Match(dataString);
                    if (f.Success)
                    {
                        yawGLData.fan = byte.Parse(f.Groups["fanPcercent"].Value);
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
