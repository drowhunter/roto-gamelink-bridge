
namespace rotoUSB
{
    public interface IHighPrecisionTimer
    {
        void Start(Action action, int intervalMs = 10);
        void Stop();
    }
}