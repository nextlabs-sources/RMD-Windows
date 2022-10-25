using SkydrmLocal.rmc.fileSystem.basemodel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.ui.windows.mainWindow.treeViewComponent.helper
{
    public class TreeViewHelper
    {
        // Judge one node if has sub folder
        public static bool HasFolderChildren(NxlFolder parent)
        {
            bool ret = false;

            foreach (INxlFile one in parent.Children)
            {
                if (one.IsFolder)
                {
                    ret = true;
                    break;
                }
            }

            return ret;
        }


        public static bool HasFolderChildren(IList<INxlFile> parent)
        {
            bool ret = false;

            foreach (INxlFile one in parent)
            {
                if (one.IsFolder)
                {
                    ret = true;
                    break;
                }
            }

            return ret;
        }

    }
}
