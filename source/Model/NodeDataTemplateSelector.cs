using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace OpenFrp.Launcher.Model
{
    internal class NodeDataTemplateSelector : DataTemplateSelector
    {
        public NodeDataTemplateSelector()
        {
            TitleTemplate = NormalTemplate = new DataTemplate();
        }

        public DataTemplate NormalTemplate { get; set; }

        public DataTemplate TitleTemplate { get; set; }


        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is Node node)
            {
                return node.IsDiplayLabel ? TitleTemplate : NormalTemplate;
            }
            throw new NotSupportedException();
        }
    }
}
