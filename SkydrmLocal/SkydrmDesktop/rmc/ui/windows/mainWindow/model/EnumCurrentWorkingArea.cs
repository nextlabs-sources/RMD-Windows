using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.ui.windows.mainWindow.model
{
    public enum EnumCurrentWorkingArea
    {
        MYSPACE = 0,
        MYVAULT = 1,
        MYDRIVE = 2,

        PROJECT = 3,
        PROJECT_ROOT = 4,

        WORKSPACE = 5,

        /// <summary>
        /// All repositories
        /// </summary>
        EXTERNAL_REPO = 6,
        /// <summary>
        /// each external repo, like google drive, dropbox
        /// </summary>
        EXTERNAL_REPO_ROOT=7,

        /// <summary>
        /// Contain MySpace, WorkSpace and external repo
        /// </summary>
        HOME = 8,

        // Filters area
        FILTERS_OFFLINE = 20,
        FILTERS_OUTBOX = 21,

        SHARED_WITH_ME
    }
}
