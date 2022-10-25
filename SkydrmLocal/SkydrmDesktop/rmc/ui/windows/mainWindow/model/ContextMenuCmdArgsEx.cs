using SkydrmLocal.rmc.fileSystem.basemodel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace SkydrmLocal.rmc.ui.windows.mainWindow.model
{
    // Extend for new feature.
    class ContextMenuCmdArgs
    {
        public INxlFile SelectedFile { get; set; }
        public string CmdName { get; set; }

        public MenuItem MenuItem { get; set; }

        public ContextMenuCmdArgs(INxlFile selectedFile, string cmdName)
        {
            SelectedFile = selectedFile;
            CmdName = cmdName;
        }

        public ContextMenuCmdArgs(MenuItem menuItem, INxlFile selectedFile, string cmdName)
        {
            this.MenuItem = menuItem;
            SelectedFile = selectedFile;
            CmdName = cmdName;
        }

    }
}
