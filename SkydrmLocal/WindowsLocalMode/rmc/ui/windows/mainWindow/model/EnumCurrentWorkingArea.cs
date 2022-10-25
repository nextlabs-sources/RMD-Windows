using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.ui.windows.mainWindow.model
{
    // Curren user working area:
    //  --- myVault, project, offliine, outbox
    public enum EnumCurrentWorkingArea
    {
        MYVAULT = 0,
        PROJECT = 1,
        PROJECT_ROOT = 2,
        PROJECT_FOLDER = 3,
        FILTERS_OFFLINE = 4,
        FILTERS_OUTBOX = 5,
        SHARED_WITH_ME = 6
    }
}
