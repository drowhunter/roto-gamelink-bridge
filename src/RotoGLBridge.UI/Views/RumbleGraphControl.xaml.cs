using RotoGLBridge.UI.ViewModels;

using ScottPlot.Plottables;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace RotoGLBridge.UI.Views
{
    /// <summary>
    /// Interaction logic for RumbleGraph.xaml
    /// </summary>
    public partial class RumbleGraphControl : UserControl
    {
        
        private DispatcherTimer dataTimer = new();
        Dictionary<string, DataStreamer> streamers = new();

        public RumbleGraphControl()
        {
            InitializeComponent();
            //this.DataContext = App.ServiceProvider.GetService<RumbleGraphViewModel>();
        }

        


        private void RumblePlot_Loaded(object sender, RoutedEventArgs e)
        {
            dataTimer.Start();
        }

        // Token: 0x0600056D RID: 1389 RVA: 0x00013289 File Offset: 0x00011489
        private void RumblePlot_Unloaded(object sender, RoutedEventArgs e)
        {
            dataTimer.Stop();
        }

        

        

        private void RefreshPlotAxes()
        {
            RumblePlot.Plot.Clear();
            streamers.Clear();

            CreateStreamer("Amplitude");
            CreateStreamer("Frequency");
        }

        private void CreateStreamer(string name)
        {
            DataStreamer streamer = RumblePlot.Plot.Add.DataStreamer(500, 1.0);
            streamer.LegendText = name;
            streamer.ViewScrollLeft();
            
            streamers.Add(streamer.LegendText, streamer);            
        }


        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            dataTimer.Interval = TimeSpan.FromSeconds(0.01);
            
            //RumblePlot.Plot.FigureBackground.Color = ScottPlot.Color.FromHex("#1E1E1E");
            RumblePlot.Plot.Axes.AutoScale(null, null);
            RumblePlot.Plot.ShowLegend();

            dataTimer.Tick += (object sender, EventArgs e) =>
            {
                int count = 5;
                var vm = (RumbleGraphViewModel)DataContext;

                foreach (var (name, strmr) in streamers)
                {
                    if (name == "Amplitude")
                    {
                        strmr.Add(vm.Amplitude);
                    }
                    else if (name == "Frequency")
                    {
                        strmr.Add(vm.Frequency);
                    }
                }

                RumblePlot.Plot.GetPlottables<Marker>().ToList().ForEach(m => m.X -= (double)count);
                
                RumblePlot.Plot.GetPlottables<Marker>()
                    .Where(m => m.X < 0.0)
                    .ToList()
                    .ForEach(m => RumblePlot.Plot.Remove(m));

                

                RumblePlot.Refresh();
            };

            RefreshPlotAxes();
        }

        

        //// DependencyProperty for Amplitude
        //public static readonly DependencyProperty AmplitudeProperty =
        //    DependencyProperty.Register(
        //        nameof(Amplitude),
        //        typeof(int),
        //        typeof(RumbleGraphControl),
        //        new PropertyMetadata(0));

        //public int Amplitude
        //{
        //    get => (int)GetValue(AmplitudeProperty);
        //    set => SetValue(AmplitudeProperty, value);
        //}

        //// DependencyProperty for Frequency
        //public static readonly DependencyProperty FrequencyProperty =
        //    DependencyProperty.Register(
        //        nameof(Frequency),
        //        typeof(int),
        //        typeof(RumbleGraphControl),
        //        new PropertyMetadata(0));

        //public int Frequency
        //{
        //    get => (int)GetValue(FrequencyProperty);
        //    set => SetValue(FrequencyProperty, value);
        //}
    }
}
