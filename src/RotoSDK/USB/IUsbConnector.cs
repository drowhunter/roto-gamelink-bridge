
using System;
using System.Threading.Tasks;

namespace com.rotovr.sdk
{
    public interface IUsbConnector
    {
        public event Action<ConnectionStatus> OnConnectionStatus;
        public event Action<RotoDataModel> OnDataChange;
        public bool IsPluggedIn { get; }

        public Task ConnectAsync();
        public Task DisconnectAsync();
        public void PlayRumble(RumbleModel model);
        public Task SetModeAsync(ModeModel model);
        public void TurnToAngle(RotateToAngleModel model);
    }
}