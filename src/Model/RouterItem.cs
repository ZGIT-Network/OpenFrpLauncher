using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace OpenFrp.Launcher.Model
{
    public class RouterItem
    {
        public Geometry? Icon { get; set; }

        public string? Title { get; set; }

        public Type? Page { get; set; }

        public Awe.UI.Helper.TwiceBindingHelper? IsEnableBinding { get; set; }
    }
}
