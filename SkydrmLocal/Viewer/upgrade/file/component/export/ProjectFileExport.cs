using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.upgrade.session;
using Viewer.upgrade.utils;

namespace Viewer.upgrade.file.component.export
{
    public class ProjectFileExport
    {
        public void Export(SkydrmLocal.rmc.sdk.Session session, int projectId, string pathId, string destPath)
        {
            string currentUserTempPathOrDownloadFilePath = Path.GetTempPath();
            try
            {
                // by commend, sdk will help us to record log: DownloadForOffline
                //app.User.AddNxlFileLog(raw.Local_path, NxlOpLog.Download, true);
                // set isViewOnly as false, by design requried
                session.User.DownlaodProjectFile(projectId, pathId, ref currentUserTempPathOrDownloadFilePath, ProjectFileDownloadType.Normal);

                File.Copy(currentUserTempPathOrDownloadFilePath, destPath, true);
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                if (!StringHelper.Equals(currentUserTempPathOrDownloadFilePath, Path.GetTempPath()))
                {
                    // del 
                    FileUtils.DelFileNoThrow(currentUserTempPathOrDownloadFilePath);
                }
            }
        }

    }
}
