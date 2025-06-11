using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace RotoGLBridge.ConsoleApp;

internal class RunArgs
{
    //[Required]
    [Description("the port to bind (listen) on for incoming data (54321)")]
    public int ListenPort { get; set; } = 54321;

    //[Required]
    [Description("The Ip Address of Gamelink (127.0.0.1)")]
    public string IPAddress { get; set; } = "127.0.0.1";


    public bool DebugMode { get; set; }
}
