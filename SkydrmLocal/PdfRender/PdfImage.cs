using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace PdfRender
{
    internal class PdfImage
    {
        public ImageSource ImageSource { get; set; }
        // we use only the "Right"-property of "Thickness", but we choose the "Thickness" structure instead of a simple double, because it makes data binding easier.
        public Thickness Margin { get; set; } 
    }
}
