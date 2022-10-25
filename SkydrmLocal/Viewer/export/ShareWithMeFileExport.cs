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
    public class ShareWithMeFileExport
    {
        public void Export(Session session, string transactionId, string transactionCode, string destPath)
        {
            string currentUserTempPathOrDownloadFilePath = System.IO.Path.GetTempPath();
            try
            {
                session.User .DownLoadSharedWithMeFile(transactionId, transactionCode, ref currentUserTempPathOrDownloadFilePath, false);
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
