using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenFrp.Launcher.Model
{
    public class ProtocolItem
    {
        public string? Title { get; set; }

        public bool IsEnabled { get; set; } = true;

        public override string ToString() => Title ?? "";
    }
}
