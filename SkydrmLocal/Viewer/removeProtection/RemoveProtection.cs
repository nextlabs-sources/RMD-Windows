using log4net;
using SkydrmLocal.rmc.drive;
using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Viewer.utils;

namespace Viewer.removeProtection
{
    public interface IExtractContent
    {
        bool CheckRights(log4net.ILog log, User user, string nXlFilePath);
        bool IsCentrolPolicyFile(log4net.ILog log, User user, string nXlFilePath);

      //  void DecryptFile(log4net.ILog log, User user, string nxlfilePath, string decryptedFilePath);

        bool CopyFile(ILog log, Session session, string RPMFolder, string sourceFilePath, string destFilePath);

        bool ShowSaveFileDialog(log4net.ILog log, Window owner, out string destinationPath, string nXlFilePath);
    }

    public class RealExtractContent : IExtractContent
    {
        public bool CheckRights(ILog log, User user, string nXlFilePath)
        {
            bool result = false;
            try
            {
                NxlFileFingerPrint nxlFileFingerPrint = user.GetNxlFileFingerPrint(nXlFilePath);
                result = nxlFileFingerPrint.HasRight(FileRights.RIGHT_DECRYPT);
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }

            return result;
        }

        public bool IsCentrolPolicyFile(ILog log, User user, string nXlFilePath)
        {
            bool result = false;
            try
            {
                NxlFileFingerPrint NxlFileFingerPrint = user.GetNxlFileFingerPrint(nXlFilePath);
                result = NxlFileFingerPrint.isByCentrolPolicy;
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
            return result;
        }

        public bool CopyFile(ILog log, Session session, string RPMFolder, string sourceFilePath, string destFilePath)
        {
            bool result = false;
            try
            {
                if (File.Exists(sourceFilePath))
                {
                    File.Copy(sourceFilePath, destFilePath, true);   
                    result = true;
                   // CommonUtils.ShowBalloonTip(CultureStringInfo.Exception_ExportFeature_Succeeded + destFilePath + ".",true);
                }
            }
            catch (Exception ex)
            {
                result = false;
            }
            return result;
        }

        public bool ShowSaveFileDialog(ILog log, Window owner, out string destinationPath, string nXlFilePath)
        {
            string originalFileName;
            string originalExtension;
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            bool b = StringHelper.Replace(Path.GetFileNameWithoutExtension(nXlFilePath),
                                          out originalFileName,
                                          StringHelper.TIMESTAMP_PATTERN,
                                          RegexOptions.IgnoreCase);
            if (b)
            {
                originalExtension = Path.GetExtension(originalFileName);
            }
            else
            {
                originalExtension = Path.GetExtension(Path.GetFileNameWithoutExtension(nXlFilePath));
            }

            dlg.CheckFileExists = false;
            dlg.FileName = originalFileName; // Default file name
            dlg.DefaultExt = originalExtension; // .nxl Default file extension
            dlg.Filter = "Documents (*" + originalExtension + ")|*" + originalExtension;
            // Show save file dialog box
            Nullable<bool> result = dlg.ShowDialog(owner);
            // Process save file dialog box results
            if (result == true)
            {
                destinationPath = dlg.FileName;
            }
            else
            {
                destinationPath = string.Empty;
            }
            return result.Value;
        }
    }

    public class ExtractContentHelper
    {
        private static RealExtractContent RealExtractContent = new RealExtractContent();

        public static bool CheckRights(ILog log, User user, string nXlFilePath)
        {
            return RealExtractContent.CheckRights(log, user, nXlFilePath);
        }

        public static bool IsCentrolPolicyFile(ILog log, User user, string nXlFilePath)
        {
            return RealExtractContent.IsCentrolPolicyFile(log, user, nXlFilePath);
        }

        public static bool CopyFile(ILog log, Session session, string RPMFolder, string sourceFilePath, string destFilePath)
        {
            return RealExtractContent.CopyFile(log, session, RPMFolder, sourceFilePath, destFilePath);
        }

        public static bool ShowSaveFileDialog(ILog log, Window owner, out string destinationPath, string nXlFilePath)
        {
            return RealExtractContent.ShowSaveFileDialog(log, owner, out destinationPath, nXlFilePath);
        }
    }
}
