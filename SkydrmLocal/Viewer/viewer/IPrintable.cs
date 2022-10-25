using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.viewer
{
    public interface IPrintable
    {
        /// <summary>
        /// Used to printable document.
        /// </summary>
        void Print();
    }
}
