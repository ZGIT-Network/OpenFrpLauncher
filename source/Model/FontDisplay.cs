using System;
using System.Collections.Generic;

using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace OpenFrp.Launcher.Model
{
    internal class FontDisplay
    {
        public FontDisplay(FontFamily font, string name)
        {
            Font = font;
            Name = name;
        }

        public FontFamily Font { get; private set; }

        public string Name { get; private set; }
    }
}
