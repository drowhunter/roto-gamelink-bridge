namespace Sharpie.Engine.Contracts.Configuration
{
    public record SharpieEngineSettings
    {
        /// <summary>
        /// Delay in milliseconds between updates.
        /// </summary>
        public int PollRate { get; set; } = (int) TimeSpan.FromMilliseconds(16).TotalMilliseconds;
    }
}
