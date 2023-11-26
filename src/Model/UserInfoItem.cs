using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace OpenFrp.Launcher.Model
{
    internal class UserInfoItem
    {
        public string? Title { get; set; }

        public Awe.UI.Helper.TwiceBindingHelper? Binding { get; set; } 
    }
}
