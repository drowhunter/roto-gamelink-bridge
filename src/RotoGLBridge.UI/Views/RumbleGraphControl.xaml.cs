using RotoGLBridge.UI.ViewModels;

using ScottPlot.Plottables;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace RotoGLBridge.UI.Views
{
    public partial class RumbleGraphControl : UserControl
    {
        
        private DispatcherTimer dataTimer = new();
        Dictionary<string, DataStreamer> streamers = new();

        public RumbleGraphControl()
        {
            InitializeComponent();
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

            CreateStreamer("Power %");
            CreateStreamer("Hz %");
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
                    if (name == "Power %")
                    {
                        strmr.Add(vm.RumblePower);
                    }
                    else if (name == "Hz %")
                    {
                        strmr.Add(vm.RumbleSpeed);
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

        

        
    }
}
