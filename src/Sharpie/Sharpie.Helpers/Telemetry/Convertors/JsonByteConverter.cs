using System.Text;
using System.Text.Json;

namespace Sharpie.Extras.Telemetry
{
    /// <summary>
    /// Convert json to byte array and vice versa.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class JsonByteConverter<T> : IByteConvertor<T> where T : struct
    {
        public byte[] ToBytes(T data)
        {
            string json = JsonSerializer.Serialize(data);
            return Encoding.UTF8.GetBytes(json);
        }
        public T FromBytes(byte[] data)
        {
            string json = Encoding.UTF8.GetString(data);
            return JsonSerializer.Deserialize<T>(json);
        }
    }

}