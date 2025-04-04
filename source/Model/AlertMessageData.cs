using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iNKORE.UI.WPF.Modern.Controls;

namespace OpenFrp.Launcher.Model
{
    class AlertMessageData
    {
        public string? Title { get; set; }

        public string? Message { get; set; }

        public InfoBarSeverity Severity { get; set; } = InfoBarSeverity.Informational;
    }
}
