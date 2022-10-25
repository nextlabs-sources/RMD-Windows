using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Viewer.viewer
{
    interface IOverlay
    {
        /// <summary>
        /// Used to decide whether attach overlay or not.
        /// </summary>
        /// <returns>true[means attach overlay you created.]|false[means do nothing.]</returns>
        Boolean IsAttach();

        /// <summary>
        /// Create Overlay element if you want to attach it.
        /// </summary>
        /// <param name="width">The width of the element that you want to attach overlay on it.[or the definition area's width]</param>
        /// <param name="height">The height of the element that you want to attach overlay on it.[or the definition area's height]</param>
        /// <returns>Concreate overlay.</returns>
        UIElement Attach(double width,double height); 
    }
}
