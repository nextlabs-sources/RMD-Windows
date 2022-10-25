using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.utils;

namespace Viewer.export
{
    public class MyVaultFileExport
    {
        public void Export(Session session, string rmsPathId, string destPath)
        {
            string currentUserTempPathOrDownloadFilePath = System.IO.Path.GetTempPath();

            try
            {
                // by commend, sdk will help us to record log: DownloadForOffline
                //App.User.AddNxlFileLog(raw.LocalPath, NxlOpLog.Download, true);
                // download 
                session.User.DownloadMyVaultFile(rmsPathId, ref currentUserTempPathOrDownloadFilePath, DownlaodMyVaultFileType.Normal);
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
