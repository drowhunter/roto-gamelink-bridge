namespace RotoGLBridge.Services
{
    public interface IRumbleService
    {
        event Action<(int power, int durationMs)> RumbleEvent;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="amplitudePercent">0-100</param>
        /// <param name="hzPercent">0-100</param>
        void Rumble(int amplitudePercent, int hzPercent);
        Task Start();
        void Stop();
    }
}
