namespace Sharpie.Extras.Telemetry
{
    public interface IByteConvertor<T> where T : struct
    {
        T FromBytes(byte[] data);
        byte[] ToBytes(T data);
    }

}