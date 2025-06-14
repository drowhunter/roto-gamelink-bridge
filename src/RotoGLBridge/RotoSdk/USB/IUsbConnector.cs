
namespace com.rotovr.sdk
{
    public interface IUsbConnector
    {
        internal event Action<ConnectionStatus> OnConnectionStatus;
        internal event Action<RotoDataModel> OnDataChange;

        internal Task<bool> ConnectAsync();
        internal Task DisconnectAsync();
        internal void PlayRumble(RumbleModel model);
        internal Task SetModeAsync(ModeModel model);
        internal void TurnToAngle(RotateToAngleModel model);
    }
}