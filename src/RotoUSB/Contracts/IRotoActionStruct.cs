namespace rotoUSB
{
    public interface IRotoActionStruct
    {
        bool GetRotoAction(out bool motorChanged, out int chairSpeed, out int objectDegree, out int chairAngle, out bool rumbleChanged, out int rumblePower, out int rumbleDurationMS);
        void Reset();
        void UpdateChairSpeed(int speed, int degree);
        void UpdateObjectFollowDegree(int speed, int degree);
        void UpdateRumble(int power, int milliSeconds);
    }
}