using System.Windows;
using System.Windows.Media.Media3D;



namespace RotoGLBridge.UI
{
    public partial class MainView : Window
    {
        private MainViewModel ViewModel => (MainViewModel)DataContext;

        public MainView(MainViewModel viewModel)
        {
            InitializeComponent();
           
            DataContext = viewModel;
            
            // Subscribe to zoom extents event
            viewModel.ZoomExtentsRequested += () => viewPort.ZoomExtents();

            viewModel.ResetViewRequested += MoveCamera;

            // Setup the viewport with models from ViewModel
            SetupViewport();

            //MoveCamera();
        }

        private void SetupViewport()
        {
            // Create visual objects using models from ViewModel
            var model1Visual = new ModelVisual3D { Content = ViewModel.RotoChairGroup };
            var model2Visual = new ModelVisual3D { Content = ViewModel.RotoBaseGroup };

            // Add both models to the viewport
            viewPort.Children.Add(model1Visual);
            viewPort.Children.Add(model2Visual);
        }


        public void MoveCamera()
        {
            viewPort.Camera = new PerspectiveCamera(
                new Point3D(-122.326, - 222.055, 195.867),  // Position" LookDirection="" UpDirection="-0.158 -0.284 0.946
                new Vector3D(122.325, 219.549, -157.8),     // LookDirection
                new Vector3D(-0.158, -0.284, 0.946),         // UpDirection
                45                                          // Field of View (for PerspectiveCamera)
            );
        }
        

       

        
    }
}