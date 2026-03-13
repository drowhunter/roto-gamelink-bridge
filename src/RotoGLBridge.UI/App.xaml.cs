using Microsoft.Extensions.DependencyInjection;
using RotoGLBridge.UI.Views;
using Microsoft.Extensions.Logging;
using RotoGLBridge.UI.ViewModels;
using System.Globalization;
using System.Windows;
using System.Windows.Threading;
using NLog;
using NLog.Extensions.Logging;
using System.IO;

namespace RotoGLBridge.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        //private readonly ServiceProvider _serviceProvider;
        public static ServiceProvider ServiceProvider;
        private Microsoft.Extensions.Logging.ILogger<App> _logger;
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

            _logger = ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<App>>();
            _engine = ServiceProvider.GetRequiredService<ISharpieEngine>();

            _logger.LogInformation("Application started");
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


            var view = ServiceProvider.GetRequiredService<MainWindow>();
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
            //delete logs/all.log on startup
            //var logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "all.log");
            //if (File.Exists(logFilePath))
            //{
            //    try
            //    {
            //        File.Delete(logFilePath);
            //        File.Create(logFilePath);
            //    }
            //    catch (Exception ex)
            //    {
            //        // If deletion fails, log the error and continue
            //        Console.WriteLine($"Failed to delete log file: {ex.Message}");
            //    }
            //}

            services.AddLogging(b =>
              {
                  b.AddFilter("Microsoft", Microsoft.Extensions.Logging.LogLevel.Warning)
                   .AddFilter("System", Microsoft.Extensions.Logging.LogLevel.Warning)
                   //.AddFilter("Sharpie", Microsoft.Extensions.Logging.LogLevel.Debug)
                   .AddFilter("RotoGLBridge", Microsoft.Extensions.Logging.LogLevel.Debug)
                   .AddNLog(); // Configure NLog
              });

            services.AddSingleton<MainWindow>();
            services.AddSingleton<MainWindowViewModel>();
            //services.AddTransient<ConnectionStatusViewModel>();

            services.AddRotoGLBridge();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _logger?.LogInformation("Application exiting");
            LogManager.Shutdown(); // Flush and close NLog
            ServiceProvider?.Dispose();
            base.OnExit(e);
        }
    }
}
