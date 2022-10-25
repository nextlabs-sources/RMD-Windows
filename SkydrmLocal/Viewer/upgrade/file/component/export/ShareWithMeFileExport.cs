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
    public class ShareWithMeFileExport
    {
        public void Export(Session session,string fileName, string transactionId, string transactionCode, string destPath)
        {
            string currentUserTempPathOrDownloadFilePath = System.IO.Path.GetTempPath();
            try
            {
                // session.User.DownLoadSharedWithMeFile(transactionId, transactionCode, ref currentUserTempPathOrDownloadFilePath, false);
                session.User.CopyNxlFile(fileName, "/" + fileName, NxlFileSpaceType.shared_with_me, "",
                Path.GetFileName(destPath), currentUserTempPathOrDownloadFilePath, NxlFileSpaceType.local_drive, "",
                true, transactionCode, transactionId);

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
