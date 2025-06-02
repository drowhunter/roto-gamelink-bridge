using System.Text;

namespace Sharpie.Extras.Telemetry
{
    public struct StringData
    {
        public string Value;

        public override string ToString()
        {
            return Value;
        }
    }

    public class StringByteConverter : IByteConvertor<StringData>
    {
        private readonly Encoding encoding;

        public StringByteConverter(Encoding encoding)
        {
            this.encoding = encoding;
        }

        public StringData FromBytes(byte[] data)
        {
            return new StringData { Value = encoding.GetString(data) };
        }

        public byte[] ToBytes(StringData data)
        {
            return encoding.GetBytes(data.ToString());
        }
    }

}