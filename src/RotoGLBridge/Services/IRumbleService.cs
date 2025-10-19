
namespace RotoGLBridge.Services
{
    public interface IRumbleService
    {
        event RumbleService.OnRumbleDelagate RumbleEvent;

        void Rumble(int amplitude, int frequencyMs);
        Task Start();
        void Stop();
    }
}