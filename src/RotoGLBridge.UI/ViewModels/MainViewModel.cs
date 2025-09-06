using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HelixToolkit.Wpf;
using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace RotoGLBridge.UI.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private double yaw;

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

        // 3D transformation objects
        private AxisAngleRotation3D? yawRotation;
        private RotateTransform3D? yawTransform;
        private Transform3DGroup? combinedTransform;
        private TranslateTransform3D? centeringTransform;

        // Animation timer
        private DispatcherTimer? yawTimer;
        private double currentYaw = 0;

        private string version = "rotovr";

        private readonly ISharpieEngine sharpieEngine;
        
       
        
    // Pseudocode plan:
    // - Replace the private getter-only property with a public computed property for binding.
    // - Raise PropertyChanged for this computed property whenever engine state may change.
    // - Update StartEngine to notify after starting.
    // - Update StopEngine to be async, await engine.Stop(), and notify after stopping.
    // - Ensure Task is available via using System.Threading.Tasks.

   

    // Replace this private property with a public computed property
    public bool IsEngineRunning => sharpieEngine?.IsRunning ?? false;

    // Replace StartEngine to notify bindings that IsEngineRunning may have changed
    [RelayCommand]
    private void StartEngine()
    {
        sharpieEngine?.Start(cts.Token);
        OnPropertyChanged(nameof(IsEngineRunning));
    }

    // Replace StopEngine to await engine stop and notify bindings
    [RelayCommand]
    private async Task StopEngine()
    {
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

        CancellationTokenSource cts;

        public MainViewModel( ISharpieEngine sharpieEngine) 
        {
            SetupModels();

            cts = new CancellationTokenSource();
            this.sharpieEngine = sharpieEngine;
        }

        public event Action ZoomExtentsRequested;

        public event Action ResetViewRequested;

        private void SetupModels()
        {
            // Setup for rotatable model (model2)
            yawRotation = new AxisAngleRotation3D(new Vector3D(0, 0, 1), 0);
            yawTransform = new RotateTransform3D(yawRotation);
            centeringTransform = new TranslateTransform3D();

            // Combine transforms: first translate to center, then rotate
            combinedTransform = new Transform3DGroup();
            combinedTransform.Children.Add(centeringTransform);
            combinedTransform.Children.Add(yawTransform);

            RotoChairGroup.Transform = combinedTransform;

            LoadModels();
            
        }


        

        public event Action<double>? CameraPitchRequested;

        partial void OnYawChanged(double value)
        {
            UpdateYaw(value);
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

        

        [RelayCommand]
        private void StartYawAnimation()
        {
            if (yawTimer?.IsEnabled == true)
                return;

            var fps = 60;
            var frameTime = 1000f / fps;
            var timePerTurn = 2000f; // milliseconds for a full 360-degree turn

            yawTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(frameTime) };

            var dps = 360f / timePerTurn * frameTime;

            yawTimer.Tick += (s, e) =>
            {
                currentYaw += dps;
                if (currentYaw > 360) 
                    currentYaw -= 360;
                Yaw = currentYaw;
            };
            yawTimer.Start();
        }

        [RelayCommand]
        private void StopYawAnimation()
        {
            yawTimer?.Stop();
        }

        [RelayCommand]
        private void ResetYaw()
        {
            StopYawAnimation();
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
            StopYawAnimation();
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

        Dictionary<int, string?> modelFileNames = new()
        {
            { 1, null },
            { 2, null }
        };
        

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

        private void UpdateYaw(double yawDegrees)
        {
            if (yawRotation != null)
            {
                yawRotation.Angle = yawDegrees;
            }
        }
    }
}
