using Microsoft.Extensions.DependencyInjection;
using RotoGLBridge.UI.Controls;
using RotoGLBridge.UI.Helpers;
using RotoGLBridge.UI.ViewModels;
using System.Globalization;
using System.Windows;
using System.Windows.Threading;

namespace RotoGLBridge.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        //private readonly ServiceProvider _serviceProvider;
        public static ServiceProvider ServiceProvider;
        private ILogger<App> _logger;
        private ISharpieEngine _engine;

        CancellationTokenSource _cts = new();
        /// <summary>
        /// Application Entry for WpfApp3D
        /// </summary>
        public App()
        {
            var services = new ServiceCollection();

            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();
            _logger = ServiceProvider.GetRequiredService<ILogger<App>>();
            _engine = ServiceProvider.GetRequiredService<ISharpieEngine>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Handle UI thread exceptions
            this.DispatcherUnhandledException += OnDispatcherUnhandledException;
            
            // Handle background thread exceptions
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            _engine.OnStarted += (s, e) => _logger.LogInformation("Engine started successfully.");
            _engine.OnStopped += (s, e) => _logger.LogInformation("Engine stopped successfully.");

            _cts = new();
           
            _engine.Start(_cts.Token);


            if (!_engine.IsRunning)
            {
                _logger.LogError("Error starting engine");
            }


            var view = ServiceProvider.GetRequiredService<MainView>();
            view.Show();
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            _cts.Cancel();

            _logger?.LogError(e.Exception, "Unhandled UI thread exception occurred");
            
            MessageBox.Show($"An unexpected error occurred: {e.Exception.Message}", 
                          "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            
            e.Handled = true;
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            _cts.Cancel();

            var exception = e.ExceptionObject as Exception;
            _logger?.LogCritical(exception, "Unhandled background thread exception occurred. Terminating: {IsTerminating}", e.IsTerminating);
            
            if (exception != null)
            {
                MessageBox.Show($"A critical error occurred: {exception.Message}", 
                              "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(b =>
              {
                  b.AddFilter("Microsoft", LogLevel.Warning)
                   .AddFilter("System", LogLevel.Warning)
                   //.AddFilter("Sharpie", LogLevel.Debug)
                   .AddFilter("RotoGLBridge", LogLevel.Debug);
              });

            services
                .AddView<MainView, MainViewModel>()
                .AddView<RumbleGraphControl, RumbleGraphViewModel>()
                .AddView<ConnectionStatusControl, ConnectionStatusViewModel>();

            

            services.AddRotoGLBridge();
        }

        
        protected override void OnExit(ExitEventArgs e)
        {
            ServiceProvider?.Dispose();
            base.OnExit(e);
        }
    }
}
