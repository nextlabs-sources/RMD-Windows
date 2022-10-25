using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfRender.MouseKeyBoardActivityMonitor.Controls
{
    /// <summary>
    /// Indicates which hooks to listen to application or global.
    /// </summary>
    public enum HookType
    {
        /// <summary>
        /// Only events inside the application are monitored and fired.
        /// </summary>
        Application,

        /// <summary>
        /// All events system wide are monitored and fired.
        /// </summary>
        Global
    }
}
