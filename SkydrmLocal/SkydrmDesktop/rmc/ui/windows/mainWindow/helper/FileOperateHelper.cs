using SkydrmLocal.rmc.fileSystem;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.ui.windows.mainWindow.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.ui.windows.mainWindow.helper
{
    class FileOperateHelper
    {
        // Check the file if is conflict when view offline file.
        public static void CheckOfflineFileVersion(IFileRepo currentWorkRepo, EnumCurrentWorkingArea area, INxlFile nxlFile, Action<bool> callback)
        {
            if (currentWorkRepo == null || nxlFile == null)
            {
                return;
            }

            SkydrmDesktop.SkydrmApp.Singleton.Log.Info("Check Offline File Version -->");
            if (area == EnumCurrentWorkingArea.FILTERS_OFFLINE)
            {
                currentWorkRepo.SyncDestFile(nxlFile, (bool bSuccess, INxlFile updatedFile) =>
                {
                    NotifyModified(bSuccess, updatedFile, callback);
                }, true);
            }
            else
            {
                currentWorkRepo.SyncDestFile(nxlFile, (bool bSuccess, INxlFile updatedFile) =>
                {
                    NotifyModified(bSuccess, updatedFile, callback);
                }, false);
            }

        }

        private static void NotifyModified(bool bSuccess, INxlFile updatedFile, Action<bool> callback)
        {
            if (bSuccess && updatedFile != null)
            {
                Console.WriteLine("$$$$$$$$$$$ NotifyModified, file: " + updatedFile.Name);
                // Judge file if is modified by field 'IsMarkedFileRemoteModified'.
                callback?.Invoke(updatedFile.IsMarkedFileRemoteModified);
            }
            else
            {
                callback?.Invoke(false);
            }
        }

        public static INxlFile GetFileFromListByLocalPath(string localPath, IList<INxlFile> list)
        {
            INxlFile ret = null;
            foreach (var one in list)
            {
                if (one.LocalPath == localPath)
                {
                    ret = one;
                    break;
                }
            }

            return ret;
        }

        public static INxlFile GetFileFromListByLocalPath(string localPath, IList<INxlFile> nxlFileList, IList<INxlFile> viewList)
        {
            INxlFile ret = null;

            // first find in nxlFileList
            foreach (var one in nxlFileList)
            {
                if (string.Equals(one.LocalPath, localPath,StringComparison.CurrentCultureIgnoreCase))
                {
                    ret = one;
                    break;
                }
            }



            // second find in ViewOfflineList
            if (ret == null)
            {
                foreach (var one in viewList)
                {
                    if (string.Equals(one.LocalPath, localPath, StringComparison.CurrentCultureIgnoreCase))
                    {
                        ret = one;
                        break;
                    }
                }
            }



            // if find it, remove item in ViewOfflineList
            if (ret != null)
            {
                INxlFile removeItem = null;
                foreach (var one in viewList)
                {
                    if (string.Equals(one.LocalPath , localPath , StringComparison.CurrentCultureIgnoreCase))
                    {
                        removeItem = one;
                        break;
                    }
                }
                // if find it, remove 
                if (removeItem != null)
                {
                    viewList.Remove(ret);
                }
            }


            return ret;
        }
    }
}
