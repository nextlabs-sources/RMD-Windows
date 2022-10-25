using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.upgrade.utils;

namespace Viewer.upgrade.file.component.export
{
    public class MyVaultFileExport
    {
        public void Export(Session session, string fileName, string rmsPathId, string destPath)
        {
            string currentUserTempPathOrDownloadFilePath = System.IO.Path.GetTempPath();
            try
            {
                // by commend, sdk will help us to record log: DownloadForOffline
                //App.User.AddNxlFileLog(raw.LocalPath, NxlOpLog.Download, true);
                // download 
                //session.User.DownloadMyVaultFile(rmsPathId, ref currentUserTempPathOrDownloadFilePath, DownlaodMyVaultFileType.Normal);

                session.User.CopyNxlFile(fileName, rmsPathId, NxlFileSpaceType.my_vault, "",
                     Path.GetFileName(destPath), currentUserTempPathOrDownloadFilePath, NxlFileSpaceType.local_drive, "",
                     true);

                string downloadFilePath = currentUserTempPathOrDownloadFilePath + Path.GetFileName(destPath);

                File.Copy(downloadFilePath, destPath, true);
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                //if (!StringHelper.Equals(currentUserTempPathOrDownloadFilePath, Path.GetTempPath()))
                //{
                    // del 
                    FileUtils.DelFileNoThrow(currentUserTempPathOrDownloadFilePath + Path.GetFileName(destPath));
               // }
            }
        }
    }
}
