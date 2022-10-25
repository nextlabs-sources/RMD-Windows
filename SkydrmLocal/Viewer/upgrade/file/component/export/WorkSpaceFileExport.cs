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
    public class WorkSpaceFileExport
    {
        public void Export(Session session, string pathId, string destinationFolder)
        {
            string currentUserTempPathOrDownLoadFilePath = Path.GetTempPath();
            try
            {
                session.User.DownloadWorkSpaceFile(pathId, ref currentUserTempPathOrDownLoadFilePath, DownlaodWorkSpaceFileType.Normal);
                File.Copy(currentUserTempPathOrDownLoadFilePath, destinationFolder, true);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (!StringHelper.Equals(currentUserTempPathOrDownLoadFilePath, Path.GetTempPath()))
                {
                    FileUtils.DelFileNoThrow(currentUserTempPathOrDownLoadFilePath);
                }
            }
        }
    }
}
