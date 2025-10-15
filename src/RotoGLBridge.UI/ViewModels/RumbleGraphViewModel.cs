using System.Windows.Threading;

namespace RotoGLBridge.UI.ViewModels
{
    public partial class RumbleGraphViewModel : ObservableObject
    {


        private DispatcherTimer updateTimer;

        [ObservableProperty]
        int amplitude;

        [ObservableProperty]
        int frequency;


        public RumbleGraphViewModel()
        {
            updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(10)
            };
            updateTimer.Tick += UpdateTimer_Tick;
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            // Logic to update the graph data
        }
    }
}
