
namespace rotoUSB
{
    public interface IRotoChair
    {
        event Action<string> OnUsbError;

        //static abstract RotoChair Instance { get; }

        //int Clamp(int value, int min, int max);

        bool Connect(bool reConnect);
        void Disconnect();
        void Dispose();
        void EnableConsoleDebug(bool isEnabled = true);
        RotoStatus GetRotoStatus();
        string GetUSBError();
        void LoadUSBLibrary();
        void MoveChair(int speed);
        void MoveChairByAngle(int speed, int angle);
        bool SetCockpitMode(int cockpitLimit);

        bool SetRunMode(RunMode mode);

        bool SetFreeMode();
        bool SetIdleMode();
        void SetObjectFollowDegree(int degree);
        bool SetObjectFollowMode();
        void SetRumble(int power, ushort milliSeconds);
        void SetZeroBaseCommand();
        void StopRumble();
    }
}