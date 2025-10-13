using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RotoGLBridge.UI
{
    public class ConnectionStatusViewModel : ObservableObject
    {
        
        public bool IsConnected { get; set; }

        public string Label { get; set; }
    }
}
