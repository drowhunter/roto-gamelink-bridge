using Microsoft.Extensions.DependencyInjection;
using RotoGLBridge.UI.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace RotoGLBridge.UI.Views
{
    public partial class ConnectionStatusControl : UserControl
    {
        //public static readonly DependencyProperty StatusProperty =
        //    DependencyProperty.Register(nameof(Status), typeof(bool), typeof(ConnectionStatusControl), new PropertyMetadata(false));

        //public bool Status
        //{
        //    get => (bool)GetValue(StatusProperty);
        //    set => SetValue(StatusProperty, value);
        //}

        //public static readonly DependencyProperty LabelProperty =
        //    DependencyProperty.Register(nameof(Label), typeof(string), typeof(ConnectionStatusControl), new PropertyMetadata(string.Empty));

        //public string Label
        //{
        //    get => (string)GetValue(LabelProperty);
        //    set => SetValue(LabelProperty, value);
        //}

        public ConnectionStatusControl()
        {
            InitializeComponent();
            //DataContext = App.ServiceProvider.GetService<ConnectionStatusViewModel>();
        }
    }
}