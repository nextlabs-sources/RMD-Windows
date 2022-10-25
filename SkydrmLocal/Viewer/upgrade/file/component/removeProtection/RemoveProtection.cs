using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Viewer.upgrade.application;
using Viewer.upgrade.utils;

namespace Viewer.upgrade.file.component.removeProtection
{
    public interface IExtractContent
    {
        bool CheckRights(User user, string nXlFilePath);

        bool IsCentrolPolicyFile(User user, string nXlFilePath);

        bool CopyFile(Session session, string RPMFolder, string sourceFilePath, string destFilePath);

        bool ShowSaveFileDialog(System.Windows.Window owner, out string destinationPath, string nXlFilePath);
    }

    public class RealExtractContent : IExtractContent
    {
        private ViewerApp mApplication;

        public RealExtractContent()
        {
            mApplication = (ViewerApp)ViewerApp.Current;
        }

        public bool CheckRights(User user, string nXlFilePath)
        {
            mApplication.Log.Info("CheckRights");
            bool result = false;
            try
            {
                NxlFileFingerPrint nxlFileFingerPrint = user.GetNxlFileFingerPrint(nXlFilePath);
                result = nxlFileFingerPrint.HasRight(FileRights.RIGHT_DECRYPT);
            }
            catch (Exception ex)
            {
                mApplication.Log.Error(ex);
            }

            return result;
        }

        public bool IsCentrolPolicyFile(User user, string nXlFilePath)
        {
            mApplication.Log.Info("IsCentrolPolicyFile");
            bool result = false;
            try
            {
                NxlFileFingerPrint NxlFileFingerPrint = user.GetNxlFileFingerPrint(nXlFilePath);
                result = NxlFileFingerPrint.isByCentrolPolicy;
            }
            catch (Exception ex)
            {
                mApplication.Log.Error(ex);
            }
            return result;
        }

        public bool CopyFile(Session session, string RPMFolder, string sourceFilePath, string destFilePath)
        {
            mApplication.Log.Info("CopyFile");
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
                mApplication.Log.Error(ex);
            }
            return result;
        }

        public bool ShowSaveFileDialog(System.Windows.Window owner, out string destinationPath, string nXlFilePath)
        {
            mApplication.Log.Info("ShowSaveFileDialog");
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

        public static bool CheckRights(User user, string nXlFilePath)
        {
            return RealExtractContent.CheckRights(user, nXlFilePath);
        }

        public static bool IsCentrolPolicyFile(User user, string nXlFilePath)
        {
            return RealExtractContent.IsCentrolPolicyFile(user, nXlFilePath);
        }

        public static bool CopyFile(Session session, string RPMFolder, string sourceFilePath, string destFilePath)
        {
            return RealExtractContent.CopyFile(session, RPMFolder, sourceFilePath, destFilePath);
        }

        public static bool ShowSaveFileDialog(System.Windows.Window owner, out string destinationPath, string nXlFilePath)
        {
            return RealExtractContent.ShowSaveFileDialog(owner, out destinationPath, nXlFilePath);
        }
    }
}
