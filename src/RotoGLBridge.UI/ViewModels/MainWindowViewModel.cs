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
       


        private int _fps = 60;
        private float _frameTime { get => 1000f / _fps; }
        private float _timePerTurn = 2000f;
        //private float _dps { get => 360f / _timePerTurn * _frameTime; }
        public bool IsEngineRunning => sharpieEngine?.IsRunning ?? false;
        private CancellationTokenSource cts;

        // Added field inside MainViewModel class (with other private fields)

        //private readonly Dispatcher _uiDispatcher;
        public event Action ZoomExtentsRequested;
        public event Action ResetViewRequested;


        [ObservableProperty]
        private double yaw;

        [ObservableProperty]
        private float rumblePower;

        [ObservableProperty]
        private float rumbleSpeed;

        [ObservableProperty]
        private double hertz;

        partial void OnYawChanged(double value)
        {
            yawRotation?.Angle = value;
        }


        [ObservableProperty]
        RumbleGraphViewModel rumbleview = new RumbleGraphViewModel();

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
        private ConnectionStatusViewModel _rotoConnected = new() { Label = "Roto" };
       

        [ObservableProperty]
        private ConnectionStatusViewModel _gamelinkConnected = new() { Label = "Telemetry" };

        [ObservableProperty]
        private ConnectionStatusViewModel _tcpConnected = new() { Label = "Gamelink" };

        [ObservableProperty]
        private ConnectionStatusViewModel _oxrmcConnected = new() { Label = "OXRMC" };

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

            
            

            // UI thread rendering event to update properties
            CompositionTarget.Rendering += (s, e) =>
            {
                
                RotoConnected.IsConnected = _rotoScript.RotoIsConnected;
                OxrmcConnected.IsConnected = _rotoScript.OxrmcIsConnected;
                GamelinkConnected.IsConnected = _rotoScript.GamelinkIsConnected;
                TcpConnected.IsConnected = _rotoScript.TcpIsConnected;

                RumblePower = _rotoScript.RumblePower;
                RumbleSpeed = _rotoScript.RumbleSpeed;
                Hertz = _rotoScript.Hertz;

                

                if (RotoConnected.IsConnected || GamelinkConnected.IsConnected || TcpConnected.IsConnected)
                {
                   Yaw = -_rotoScript.Yaw;
                   SliderEnabled = false;
                } 
                else
                {
                    SliderEnabled = true;
                }

                rumbleview.RumblePower = RumblePower;
                rumbleview.RumbleSpeed = RumbleSpeed;
                

            };

            

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
        //private bool _renderSubscribed;
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
        //bool animated = false;
        private bool sliderEnabled = false; 
       

        [RelayCommand]
        private void ResetYaw()
        {
            //StopYawAnimation();
            
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



        private Model3DGroup LoadModelFromPath(string path)
        {
            Model3DGroup model = null;

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


        

    }
}
