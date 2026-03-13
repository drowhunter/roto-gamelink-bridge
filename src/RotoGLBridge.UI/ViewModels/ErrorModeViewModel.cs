namespace RotoGLBridge.UI.ViewModels;

public partial class ErrorModeViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ErrorModeLabel))]
    int errorMode;


    public string ErrorModeLabel => errorMode switch
    {
        0 => "None",
        0x10 => "Emergency Stop (HT)",
        0x20 => "Base Stop",
        0x40 => "Motor Stalled",
        0x80 => "Emergency Stop",
        _ => "Unknown"
    };
}
