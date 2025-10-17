namespace RotoGLBridge.UI.ViewModels;

public partial class ConnectionStatusViewModel : ObservableObject
{
    [ObservableProperty]
    bool isConnected;


    [ObservableProperty]
    string label;
    

}

