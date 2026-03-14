namespace rotoUSB
{
    public interface IRotoChair
    {
        
        event RotoChair.ErrorModeHandler ErrorModeChanged;
        event RotoChair.RunModeChangeHandler RunModeChanged;

        int Clamp(int value, int min, int max);
        bool Connect(bool reConnect);
        void Disconnect();
        void Dispose();
        void EnableConsoleDebug(bool isEnabled = true);
        void EnableModeResume(bool isEnabled);
        void EnableModeSound(bool isEnabled);
        RotoStatus GetRotoStatus();
        string GetUSBError();
        void KeepRumbling(int power);
        void LoadUSBLibrary();
        void MoveChair(int speed);
        void MoveChairByAngle(int speed, int angle);
        bool SetCockpitMode(int cockpitLimit);
        bool SetFreeMode();
        bool SetIdleMode();
        void SetObjectFollowDegree(int degree, int speed);
        bool SetObjectFollowMode();
        void SetRumble(int power, ushort milliSeconds);
        void SetZeroBaseCommand();
        void StopRumble();
        bool UpdateChairAction();
    }
}