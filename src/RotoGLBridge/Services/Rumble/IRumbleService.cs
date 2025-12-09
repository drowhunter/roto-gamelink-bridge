namespace RotoGLBridge.Services
{
    public interface IRumbleService
    {
        event Action<(int power, int durationMs)> RumbleEvent;

        void Rumble(int amplitude, int frequencyMs);
        Task Start();
        void Stop();
    }
}
