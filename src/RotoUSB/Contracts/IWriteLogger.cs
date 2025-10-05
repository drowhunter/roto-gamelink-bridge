namespace rotoUSB
{
    /// <summary>
    ///  Logger for writing debug logs to console and file
    /// </summary>
    public interface IWriteLogger<T> where T : class
    {
        void EnableConsoleDebug(bool isEnabled = true);

        void WriteLog(string message);
    }
}
