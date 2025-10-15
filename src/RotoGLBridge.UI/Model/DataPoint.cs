using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RotoGLBridge.UI.Model
{
    public partial class DataPoint : ObservableObject
    {
        [ObservableProperty]
        private string key;

        [ObservableProperty]
        private float value;

    }
}
