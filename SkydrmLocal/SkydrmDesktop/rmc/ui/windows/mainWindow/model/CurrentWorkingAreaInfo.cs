using SkydrmDesktop.rmc.fileSystem.utils;
using SkydrmLocal.rmc.ui.windows.mainWindow.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace SkydrmDesktop.rmc.ui.windows.mainWindow.model
{
    /// <summary>
    /// Use for display repository info
    /// </summary>
    public class CurrentWorkingAreaInfo
    {
        public CurrentWorkingAreaInfo(EnumCurrentWorkingArea workingArea, string repoName, string describe="", 
            bool isOwner=true, long usedStorage=0, long totalStorage=0)
        {
            switch (workingArea)
            {
                case EnumCurrentWorkingArea.MYVAULT:
                    Image = @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/myvault.png";
                    break;
                case EnumCurrentWorkingArea.MYDRIVE:
                    Image = @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/mydrive.png";
                    break;
                case EnumCurrentWorkingArea.SHARED_WITH_ME:
                    Image = @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/sharedWithMe.png";
                    break;
                case EnumCurrentWorkingArea.PROJECT_ROOT:
                    if (isOwner)
                    {
                        Image = @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/projectByMe.png";
                    }
                    else
                    {
                        Image = @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/projectByOthers.png";
                    }
                    break;
                case EnumCurrentWorkingArea.WORKSPACE:
                    Image = @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/workspace.png";
                    break;
                default:
                    // other repo will not display 
                    Image = @"/rmc/resources/icons/Icon_red_warning.png";
                    break;
            }
            RepoName = repoName;
            RepoProviderClass = RepositoryProviderClass.UNKNOWN;
            Describe = describe;
            IsOwner = isOwner;
            UsedStorage = usedStorage;
            TotalStorage = totalStorage;
        }

        /// <summary>
        /// Use for external Repo type
        /// </summary>
        /// <param name="repoType"></param>
        /// <param name="repoName"></param>
        /// <param name="describe"></param>
        /// <param name="isOwner"></param>
        /// <param name="usedStorage"></param>
        /// <param name="totalStorage"></param>
        public CurrentWorkingAreaInfo(string repoType, string repoName, RepositoryProviderClass providerClass, string describe = "",
            bool isOwner = true, long usedStorage = 0, long totalStorage = 0)
        {
            if (repoType.Equals(FileSysConstant.DROPBOX))
            {
                Image = @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/dropbox.png";
            }
            else if (repoType.Equals(FileSysConstant.ONEDRIVE))
            {
                Image = @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/oneDrive.png";
            }
            else if (repoType.Equals(FileSysConstant.GOOGLE_DRIVE))
            {
                Image = @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/googleDrive.png";
            }
            else if (repoType.Equals(FileSysConstant.BOX))
            {
                Image = @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/box.png";
            }
            else if (repoType.Equals(FileSysConstant.SHAREPOINT)
                || repoType.Equals(FileSysConstant.SHAREPOINT_ONLINE)
                || repoType.Equals(FileSysConstant.SHAREPOINT_ONPREMISE))
            {
                Image = @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/sharepoint.png";
            }
            else
            {
                Image = @"/rmc/resources/icons/Icon_red_warning.png";
            }
            
            RepoName = repoName;
            RepoProviderClass = providerClass;
            Describe = describe;
            IsOwner = isOwner;
            UsedStorage = usedStorage;
            TotalStorage = totalStorage;
        }

        public string Image { get; }
        public string RepoName { get; }
        /// <summary>
        /// Used to distinguish external repo account type(personal, business, application)
        /// </summary>
        public RepositoryProviderClass RepoProviderClass { get; }
        public string Describe { get; }
        public bool IsOwner { get; }
        public long UsedStorage { get; }
        public long TotalStorage { get; }
    }
}
