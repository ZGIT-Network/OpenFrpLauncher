using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenFrp.Launcher.Model
{
    public class FrpcFeatrue
    {
        public bool AllowDisableConsoleColor = false;

        public bool UseForceTls
        {
            get => App.Settings.UseForceTls;
        }
        public bool UseDebug
        {
            get => App.Settings.UseDebug;
        }
    }
}
