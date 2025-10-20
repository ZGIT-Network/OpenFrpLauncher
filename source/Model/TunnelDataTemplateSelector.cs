using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace OpenFrp.Launcher.Model
{
    internal class TunnelDataTemplateSelector : DataTemplateSelector
    {
        public TunnelDataTemplateSelector()
        {
            
        }

        public DataTemplate CardDataTemplate { get; set; } = new DataTemplate { };

        public DataTemplate ListDataTemplate { get; set; } = new DataTemplate { };

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            return App.Settings.UseGridViewTunnelFeature ? CardDataTemplate : ListDataTemplate;
        }
    }
}
