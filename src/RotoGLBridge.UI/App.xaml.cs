using Microsoft.Extensions.DependencyInjection;

using System.Windows;
using System.Windows.Threading;

namespace RotoGLBridge.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly ServiceProvider _serviceProvider;
        private ILogger<App> _logger;
        private ISharpieEngine _engine;

        CancellationTokenSource _cts = new();
        /// <summary>
        /// Application Entry for WpfApp3D
        /// </summary>
        public App()
        {
            var services = new ServiceCollection();
           

            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
            _logger = _serviceProvider.GetRequiredService<ILogger<App>>();
            _engine = _serviceProvider.GetRequiredService<ISharpieEngine>();
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


            var view = _serviceProvider.GetRequiredService<MainView>();
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

            services.AddTransient<MainView>();
            services.AddTransient<MainViewModel>();

            services.AddRotoGLBridge();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _serviceProvider?.Dispose();
            base.OnExit(e);
        }
    }
}
