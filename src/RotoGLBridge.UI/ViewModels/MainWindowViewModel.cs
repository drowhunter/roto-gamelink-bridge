using CommunityToolkit.Mvvm.Input;

using HelixToolkit.Wpf;

using Microsoft.Win32;
using RotoGLBridge.Scripts;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace RotoGLBridge.UI.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private AxisAngleRotation3D yawRotation;
        private RotateTransform3D yawTransform;
        private Transform3DGroup combinedTransform;
        private TranslateTransform3D _centeringTransform;

        private double currentYaw = 0;


        private string version = "rotovr";

        private readonly ISharpieEngine sharpieEngine;
        private readonly RotoScript _rotoScript;
        //private readonly Roto2PluginGlobal _rotoGlobal;

        //private readonly RotoPluginGlobal _roto;
        [ObservableProperty]
        RumbleGraphViewModel rumbleview = new RumbleGraphViewModel();


        private int _fps = 60;
        private float _frameTime { get => 1000f / _fps; }
        private float _timePerTurn = 2000f;
        private float _dps { get => 360f / _timePerTurn * _frameTime; }
        public bool IsEngineRunning => sharpieEngine?.IsRunning ?? false;
        private CancellationTokenSource cts;

        // Added field inside MainViewModel class (with other private fields)
        //private readonly Dispatcher _uiDispatcher;
        public event Action ZoomExtentsRequested;
        public event Action ResetViewRequested;

        //IObservable<EventPattern<EventArgs>> _renderObservable;


        [ObservableProperty]
        private double yaw;

        [ObservableProperty]
        private double amp;

        [ObservableProperty]
        private double hz;

        [ObservableProperty]
        private double hertz;

        partial void OnYawChanged(double value)
        {
            yawRotation?.Angle = value;
        }

        

        [ObservableProperty]
        private Model3DGroup rotoBaseGroup = new();

        [ObservableProperty]
        private Model3DGroup rotoChairGroup = new();

        [ObservableProperty]
        private bool isRotoChairModelLoaded;

        [ObservableProperty]
        private bool isRotoBaseModelLoaded;

        [ObservableProperty]
        private string rotoChairFileName = "No model loaded";

        [ObservableProperty]
        private string rotoBaseFileName = "No model loaded";


        [ObservableProperty]
        private string _programVersion;

        [ObservableProperty]
        private ConnectionStatusViewModel _rotoConnected = new() { Label = "Chair Connected" };
       

        [ObservableProperty]
        private ConnectionStatusViewModel _gamelinkConnected = new() { Label = "Receiving Telemetry" };

        [ObservableProperty]
        private ConnectionStatusViewModel _tcpConnected = new() { Label = "Gamelink Connected" };

        [ObservableProperty]
        private ConnectionStatusViewModel _oxrmcConnected = new() { Label = "OXRMC Installed" };

        [RelayCommand]
        public void OpenSettings()
        {
            //var settingsWindow = new SettingsWindow();
            //settingsWindow.Owner = Application.Current.MainWindow;
            //settingsWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            //settingsWindow.ShowDialog();
        }



        // Updated constructor to safely update Yaw from background thread via dispatcher
        public MainWindowViewModel(ISharpieEngine sharpieEngine, RotoScript rotoScript)
        {
            SetupModels();
            

            ProgramVersion = GetProgramVersion();

            cts = new CancellationTokenSource();
            this.sharpieEngine = sharpieEngine;
            _rotoScript = rotoScript;
            
            //_renderObservable = Observable.FromEventPattern<EventHandler, EventArgs>(
            //    h => CompositionTarget.Rendering += h,
            //    h => CompositionTarget.Rendering -= h);




            /*
            rotoScript.OnYawUpdate += (newYaw) =>
            {

                // Use dispatcher to update Yaw on UI thread
                Application.Current.Dispatcher.Invoke(() =>
                {
                    currentYaw = -newYaw; // Invert if necessary
                    Yaw = currentYaw;
                });
            };
            */

            //_roto = roto;

            ////_uiDispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

            //_roto.OnUpdate += () =>
            //{
            //    // Extract latest angle (prefer lerped if non-zero)
            //    var data = _roto?.Data;
            //    if (data == null) return;

            //    currentYaw = -data.LerpedAngle;


            //};

            // UI thread rendering event to update properties
            CompositionTarget.Rendering += (s, e) =>
            {
                //_renderObservable.Subscribe(_ =>
                //{
                
                RotoConnected.IsConnected = _rotoScript.RotoIsConnected;
                Yaw = currentYaw = -_rotoScript.Yaw;
                Amp = _rotoScript.Amplitude;
                Hz = _rotoScript.Frequency;
                Hertz = _rotoScript.Hertz;

                OxrmcConnected.IsConnected = _rotoScript.OxrmcIsConnected;
                GamelinkConnected.IsConnected = _rotoScript.GamelinkIsConnected;
                TcpConnected.IsConnected = _rotoScript.TcpIsConnected;

                rumbleview.Amplitude = (int) Amp;
                rumbleview.Frequency = (int) Hz;
                //Animated = GamelinkConnected || TcpConnected || RotoConnected;

            };//);

            

        }

        

        void OnRender(object s, EventArgs e)
        {
            //if (Animated)
            //{
            //    currentYaw += _dps;
            //    if (currentYaw >= 360) currentYaw -= 360;
            //}

            Yaw = currentYaw;



        }

        private string GetProgramVersion()
        {
            var assembly = typeof(MainWindowViewModel).Assembly;
            var fileVersion = FileVersionInfo.GetVersionInfo(assembly.Location).FileVersion;
            return fileVersion ?? assembly.GetName().Version?.ToString() ?? "Unknown";
        }

        


        Dictionary<int, string> modelFileNames = new()
        {
            { 1, null },
            { 2, null }
        };
        private bool _renderSubscribed;
        public event Action<double> CameraPitchRequested;

        // Replace StartEngine to notify bindings that IsEngineRunning may have changed
        [RelayCommand]
        private void StartEngine()
        {
            sharpieEngine?.Start(cts.Token);
            OnPropertyChanged(nameof(IsEngineRunning));
           // StartSmoothYaw();
        }

        // Replace StopEngine to await engine stop and notify bindings
        [RelayCommand]
        private async Task StopEngine()
        {
            //StopSmoothYaw();
            cts?.Cancel();
            if (sharpieEngine != null)
            {
                try
                {
                    await sharpieEngine.Stop();
                }
                catch
                {
                    // swallow or handle as needed
                }
            }
            cts = new CancellationTokenSource();
            OnPropertyChanged(nameof(IsEngineRunning));
        }

        
        private void SetupModels()
        {
            // Setup for rotatable model (model2)
            yawRotation = new AxisAngleRotation3D(new Vector3D(0, 0, 1), 0);
            yawTransform = new RotateTransform3D(yawRotation);
            _centeringTransform = new TranslateTransform3D();

            // Combine transforms: first translate to center, then rotate
            combinedTransform = new Transform3DGroup();
            combinedTransform.Children.Add(_centeringTransform);
            combinedTransform.Children.Add(yawTransform);

            RotoChairGroup.Transform = combinedTransform;

            LoadModels();
            
        }
        
        [RelayCommand]
        private void LoadModels()
        {
            try
            {
                var path = @$"assets\{version}\base.obj"; // BrowseModel(1, );

                var baseModel = LoadModelFromPath(path);


                var path2 = @$"assets\{version}\chair.obj"; // BrowseModel(1, );

                var chairModel = LoadModelFromPath(path2);


                if (baseModel != null && chairModel != null)
                {
                    modelFileNames[0] = path;
                    modelFileNames[1] = path2;


                    RotoBaseGroup.Children.Clear();
                    RotoBaseGroup.Children.Add(baseModel);
                    IsRotoChairModelLoaded = true;
                    RotoChairFileName = path;

                    RotoChairGroup.Children.Clear();
                    RotoChairGroup.Children.Add(chairModel);
                    IsRotoBaseModelLoaded = true;
                    RotoBaseFileName = path2;


                }


            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading models: {ex.Message}");
            }

            

        }

        [ObservableProperty]
        bool animated = false;

        /*partial void OnAnimatedChanging(bool value)
        {
            
            if (value)
            {
                StartYawAnimation();
            }
            else
            {
                StopYawAnimation();
            }
        }*/

/*
        [RelayCommand]
        private void StartYawAnimation()
        {           
            if (!Animated)
            {
                StartSmoothYaw();
                return;
            }

        }

        [RelayCommand]
        private void StopYawAnimation()
        {
            if (Animated)
            {
                StopSmoothYaw();
                return;
            }

        }
*/
        [RelayCommand]
        private void ResetYaw()
        {
            //StopYawAnimation();
            currentYaw = 0;
            Yaw = 0;
        }

        [RelayCommand]
        private void ZoomExtents()
        {
            // This will be handled in the view via event or messaging
            ZoomExtentsRequested?.Invoke();
        }

        [RelayCommand]
        private void ResetView()
        {
            //StopYawAnimation();
            currentYaw = 0;
            Yaw = 0;
            ResetViewRequested?.Invoke();
            ZoomExtentsRequested?.Invoke();

        }


        


        //[RelayCommand]
        //private void StartEngine()
        //{
        //    sharpieEngine?.Start(cts.Token);
        //}
        
        //[RelayCommand]
        //private void StopEngine()
        //{
        //    cts?.Cancel();
        //    cts = new CancellationTokenSource();
        //}

        

        private string BrowseModel()
        {
            string path = string.Empty;

            var openFileDialog = new OpenFileDialog
            {
                Filter = "3D Model files (*.stl;*.dae;*.obj;*.fbx)|*.stl;*.dae;*.obj;*.fbx|All files (*.*)|*.*",
                InitialDirectory = @"D:\source\mine\Roto\assets\"
            };


            if (openFileDialog.ShowDialog() == true)
            {
                path = openFileDialog.FileName;
            }
            

            return path;
        }



        private Model3DGroup? LoadModelFromPath(string path)
        {
            Model3DGroup? model = null;

            if (path == null)
                return null;


            try
            {
                var ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
                var fileName = System.IO.Path.GetFileName(path);

                model = ext switch
                {
                    ".stl" => new StLReader().Read(path),
                    ".dae" or ".obj" => new ModelImporter().Load(path),
                    ".fbx" => null, // Add FBX support if needed
                    _ => null
                };

                if (model == null)
                {
                    MessageBox.Show($"Unsupported file format: {ext}");
                    
                }
                
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading model: {ex.Message}");
            }

            return model;
        }

        

        
    

        public void StartSmoothYaw()
        {
            if (_renderSubscribed) return;
            CompositionTarget.Rendering += OnRender;

            

               


            _renderSubscribed = true;
        }

        public void StopSmoothYaw()
        {
            if (!_renderSubscribed) return;
            CompositionTarget.Rendering -= OnRender;
            _renderSubscribed = false;
        }

        

        

    }
}
