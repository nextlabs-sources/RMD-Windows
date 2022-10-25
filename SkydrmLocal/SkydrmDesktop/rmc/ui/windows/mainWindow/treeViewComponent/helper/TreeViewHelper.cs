using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.ui.windows.mainWindow.treeViewComponent.viewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        public static void AddFolderNodeChild(ObservableCollection<TreeViewItemViewModel> children, FolderViewModel folderNode)
        {
            if(IsFolderViewModelExisted(children, folderNode.NxlFolder))
            {
                return;
            }
            children.Add(folderNode);
        }

        public static void AddFolderNodeChildInFirst(ObservableCollection<TreeViewItemViewModel> children, FolderViewModel folderNode)
        {
            if (IsFolderViewModelExisted(children, folderNode.NxlFolder))
            {
                return;
            }
            children.Insert(0, folderNode);
        }

        // Judge the folder treeview item node if has existed.
        private static bool IsFolderViewModelExisted(ObservableCollection<TreeViewItemViewModel> children, NxlFolder folderNode)
        {
            bool ret = false;
            foreach(var one in children)
            {
                if(one is FolderViewModel)
                {
                    var fvm = one as FolderViewModel;
                    if(fvm.NxlFolder.PathId == folderNode.PathId)
                    {
                        ret = true;
                        break;
                    }
                }
            }
            return ret;
        }

    }
}
