using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.drive;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.ui;
using System;
using System.Collections.Generic;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace SkydrmLocal.rmc.removeProtection
{
    public interface IExtractContent
    {
        bool IsCentrolPolicyFile(SkydrmLocalApp SkydrmLocalApp, string nXlFileLocalPath);

        bool CheckRights(SkydrmLocalApp SkydrmLocalApp, string nXlFileLocalPath);

        bool ExtractContent(SkydrmLocalApp SkydrmLocalApp, Window owner, string nXlFileLocalPath, out bool isCancled);

        void DecryptFile(SkydrmLocalApp SkydrmLocalApp, string nXlFileLocalPath, out string decryptedFilePath);

        bool MoveFile(SkydrmLocalApp SkydrmLocalApp, string sourceFilePath, string destFilePath);

        bool ShowSaveFileDialog(Window owner, out string destinationPath, string nXlFileLocalPath);

    }

    public class ExtractContentHelper
    {
        private static RealExtractContent RealExtractContent = new RealExtractContent();

        public static bool CheckRights(SkydrmLocalApp skydrmLocalApp,string nXlFileLocalPath)
        {
            return RealExtractContent.CheckRights(skydrmLocalApp, nXlFileLocalPath);
        }

        public static bool IsCentrolPolicyFile(SkydrmLocalApp skydrmLocalApp, string nXlFileLocalPath)
        {
            return RealExtractContent.IsCentrolPolicyFile(skydrmLocalApp, nXlFileLocalPath);
        }

        public static bool ExtractContent(SkydrmLocalApp skydrmLocalApp, Window owner,string nXlFileLocalPath,out bool cancled)
        {
           return RealExtractContent.ExtractContent(skydrmLocalApp, owner, nXlFileLocalPath,out cancled);
        }

        public static void DecryptFile(SkydrmLocalApp skydrmLocalApp, string nXlFileLocalPath, out string decryptedFilePath)
        {
             RealExtractContent.DecryptFile(skydrmLocalApp, nXlFileLocalPath , out decryptedFilePath);
        }

        public static bool MoveFile(SkydrmLocalApp SkydrmLocalApp, string sourceFilePath, string destFilePath)
        {
            return RealExtractContent.MoveFile(SkydrmLocalApp, sourceFilePath, destFilePath);
        }

        public static bool ShowSaveFileDialog(Window owner, out string destinationPath, string nXlFileLocalPath)
        {
            return RealExtractContent.ShowSaveFileDialog(owner, out destinationPath, nXlFileLocalPath);
        }

        public static void SendLog(string nxlFilePath, NxlOpLog operation, bool isAllow)
        {
            var app = SkydrmLocalApp.Singleton;
            try
            {
                app.Rmsdk.User.AddLog(nxlFilePath, operation, isAllow);
                app.User.UploadNxlFileLog_Async();
            }
            catch (Exception e)
            {
                app.Log.Warn("error when sending nxl log to rms,e=" + e.Message, e);
            }
        }

    }


    public class RealExtractContent : IExtractContent
    {

        public RealExtractContent()
        { }

        public bool CheckRights(SkydrmLocalApp skydrmLocalApp, string nXlFileLocalPath)
        {
            bool result = false;
            try
            {
                NxlFileFingerPrint NxlFileFingerPrint = skydrmLocalApp.Rmsdk.User.GetNxlFileFingerPrint(nXlFileLocalPath);
                result = NxlFileFingerPrint.HasRight(FileRights.RIGHT_DECRYPT);
            }
            catch (Exception ex)
            {
                skydrmLocalApp.Log.Error(ex);
            }
      
            return result;
        }

        public bool IsCentrolPolicyFile(SkydrmLocalApp skydrmLocalApp, string nXlFileLocalPath)
        {
            bool result = false;
            try
            {
                NxlFileFingerPrint NxlFileFingerPrint = skydrmLocalApp.Rmsdk.User.GetNxlFileFingerPrint(nXlFileLocalPath);
                result = NxlFileFingerPrint.isByCentrolPolicy;      
            }
            catch (Exception ex)
            {
                skydrmLocalApp.Log.Error(ex);
            }
            return result;
        }

        public bool ExtractContent(SkydrmLocalApp skydrmLocalApp, Window owner, string nXlFileLocalPath, out bool isCancled)
        {
            bool result = false;
            isCancled = false;
            try
            {
                string destinationPath = string.Empty;
                if (ShowSaveFileDialog(owner, out destinationPath, nXlFileLocalPath))
                {
                    string decryptedFilePath = string.Empty;

                    DecryptFile(skydrmLocalApp, nXlFileLocalPath, out decryptedFilePath);

                    result = MoveFile(skydrmLocalApp, decryptedFilePath, destinationPath);
                }
                else
                {
                    isCancled = true;
                }            
            }
            catch (System.IO.IOException ex)
            {
                skydrmLocalApp.Log.Error(ex);
                skydrmLocalApp.ShowBalloonTip("Extract Contents Failed, DestFileName Exists.");
                
            }
            catch (Exception ex)
            {
                skydrmLocalApp.Log.Error(ex);
            }

            if (!isCancled)
            {
                if (!result)
                {
                    skydrmLocalApp.ShowBalloonTip("Extract Contents Failed.");
                }
            }
            return result;
        }

        public void DecryptFile(SkydrmLocalApp SkydrmLocalApp, string nXlFileLocalPath, out string decryptedFilePath)
        {
            decryptedFilePath = RightsManagementService.GenerateDecryptFilePath(SkydrmLocalApp.User.RPMFolder, nXlFileLocalPath, DecryptIntent.ExtractContent);
            RightsManagementService.DecryptNXLFile(SkydrmLocalApp, nXlFileLocalPath, decryptedFilePath);
        }

        public bool MoveFile(SkydrmLocalApp skydrmLocalApp, string sourceFilePath, string destFilePath)
        {
            bool result = false;
            try
            {
                if (File.Exists(sourceFilePath))
                {
                    File.Copy(sourceFilePath, destFilePath, true);
                    RightsManagementService.RPMDeleteDirectory(skydrmLocalApp, Path.GetDirectoryName(sourceFilePath));
                    result = true;

                    skydrmLocalApp.ShowBalloonTip(CultureStringInfo.Exception_ExportFeature_Succeeded + destFilePath + ".");
                }
            }
            catch (Exception ex)
            {
                result = false;
            }    
            return result;
        }

        public bool ShowSaveFileDialog(Window owner, out string destinationPath,  string nXlFileLocalPath)
        {
            string originalFileName;
            string originalExtension;
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            bool b = StringHelper.Replace(Path.GetFileNameWithoutExtension(nXlFileLocalPath),
                                          out originalFileName,
                                          StringHelper.TIMESTAMP_PATTERN,
                                          RegexOptions.IgnoreCase);
            if (b)
            {
                originalExtension = Path.GetExtension(originalFileName);
            }
            else
            {
                originalExtension = Path.GetExtension(Path.GetFileNameWithoutExtension(nXlFileLocalPath));
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
}
