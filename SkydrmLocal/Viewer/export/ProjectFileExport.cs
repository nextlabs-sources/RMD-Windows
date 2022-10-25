using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Viewer.utils;

namespace Viewer.export
{
    public class ProjectFileExport
    {
        public void Export(Session session, int projectId, string pathId, string destPath)
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
                if (!StringHelper.Equals(currentUserTempPathOrDownloadFilePath, System.IO.Path.GetTempPath()))
                {
                    // del 
                    CommonUtils.DelFileNoThrow(currentUserTempPathOrDownloadFilePath);
                }
            }
        }
    }
}
