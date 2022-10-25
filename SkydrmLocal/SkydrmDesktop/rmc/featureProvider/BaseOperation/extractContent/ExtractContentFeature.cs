using SkydrmDesktop;
using SkydrmDesktop.Resources.languages;
using SkydrmDesktop.rmc.featureProvider.MessageNotify;
using SkydrmLocal.rmc.common.helper;
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
using SkydrmLocal.rmc.drive;

namespace SkydrmLocal.rmc.removeProtection
{
    public interface IExtractContent
    {
        bool IsCentrolPolicyFile(SkydrmApp SkydrmApp, string nXlFileLocalPath);

        bool CheckRights(SkydrmApp SkydrmApp, string nXlFileLocalPath);

        bool ExtractContent(SkydrmApp SkydrmApp, Window owner, string nXlFileLocalPath, out bool isCancled, bool isOnline = false);

        void DecryptFile(SkydrmApp SkydrmApp, string nXlFileLocalPath, out string decryptedFilePath);

        bool MoveFile(SkydrmApp SkydrmApp, string sourceFilePath, string destFilePath);

        bool ShowSaveFileDialog(Window owner, out string destinationPath, string nXlFileLocalPath);

    }

    public class ExtractContentHelper
    {
        private static RealExtractContent RealExtractContent = new RealExtractContent();

        public static bool CheckRights(SkydrmApp SkydrmApp,string nXlFileLocalPath)
        {
            return RealExtractContent.CheckRights(SkydrmApp, nXlFileLocalPath);
        }

        public static bool IsCentrolPolicyFile(SkydrmApp SkydrmApp, string nXlFileLocalPath)
        {
            return RealExtractContent.IsCentrolPolicyFile(SkydrmApp, nXlFileLocalPath);
        }

        public static bool ExtractContent(SkydrmApp SkydrmApp, Window owner, string nXlFileLocalPath, out bool cancled, bool isOnline = false)
        {
           return RealExtractContent.ExtractContent(SkydrmApp, owner, nXlFileLocalPath, out cancled, isOnline);
        }

        public static void DecryptFile(SkydrmApp SkydrmApp, string nXlFileLocalPath, out string decryptedFilePath)
        {
             RealExtractContent.DecryptFile(SkydrmApp, nXlFileLocalPath , out decryptedFilePath);
        }

        public static bool MoveFile(SkydrmApp SkydrmApp, string sourceFilePath, string destFilePath)
        {
            return RealExtractContent.MoveFile(SkydrmApp, sourceFilePath, destFilePath);
        }

        public static bool ShowSaveFileDialog(Window owner, out string destinationPath, string nXlFileLocalPath)
        {
            return RealExtractContent.ShowSaveFileDialog(owner, out destinationPath, nXlFileLocalPath);
        }

        public static void SendLog(string nxlFilePath, NxlOpLog operation, bool isAllow)
        {
            var app = SkydrmApp.Singleton;
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

        public bool CheckRights(SkydrmApp SkydrmApp, string nXlFileLocalPath)
        {
            bool result = false;
            try
            {
                NxlFileFingerPrint NxlFileFingerPrint = SkydrmApp.Rmsdk.User.GetNxlFileFingerPrint(nXlFileLocalPath);
                result = NxlFileFingerPrint.HasRight(FileRights.RIGHT_DECRYPT);
            }
            catch (Exception ex)
            {
                SkydrmApp.Log.Error(ex);
            }
      
            return result;
        }

        public bool IsCentrolPolicyFile(SkydrmApp SkydrmApp, string nXlFileLocalPath)
        {
            bool result = false;
            try
            {
                NxlFileFingerPrint NxlFileFingerPrint = SkydrmApp.Rmsdk.User.GetNxlFileFingerPrint(nXlFileLocalPath);
                result = NxlFileFingerPrint.isByCentrolPolicy;      
            }
            catch (Exception ex)
            {
                SkydrmApp.Log.Error(ex);
            }
            return result;
        }

        public bool ExtractContent(SkydrmApp app, Window owner, string nXlFileLocalPath, out bool isCancled, bool isOnline = true)
        {
            bool result = false;
            isCancled = false;

            try
            {
                string destinationPath = string.Empty;
                if (ShowSaveFileDialog(owner, out destinationPath, nXlFileLocalPath))
                {
                    string decryptedFilePath = string.Empty;

                    DecryptFile(app, nXlFileLocalPath, out decryptedFilePath);

                    result = MoveFile(app, decryptedFilePath, destinationPath);

                    if (result)
                    {
                        var icon = isOnline ? EnumMsgNotifyIcon.Online : EnumMsgNotifyIcon.Offline;
                        app.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Exception_ExportFeature_Succeeded") + destinationPath + ".",
                            true,
                            Path.GetFileName(nXlFileLocalPath),
                            MsgNotifyOperation.EXTRACT,
                            icon
                            );
                    }
                }
                else
                {
                    isCancled = true;
                }            
            }
            catch (System.IO.IOException ex)
            {
                app.Log.Error(ex);
                app.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Notify_PopBubble_Extract_Failed_DestFile_Exist"), 
                    false, 
                    Path.GetFileName(nXlFileLocalPath),
                    MsgNotifyOperation.EXTRACT
                    );
                
            }
            catch (Exception ex)
            {
                app.Log.Error(ex);
            }

            if (!isCancled)
            {
                if (!result)
                {
                    app.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Notify_PopBubble_ExtractContent_Failed"), 
                        false, 
                        Path.GetFileName(nXlFileLocalPath),
                        MsgNotifyOperation.EXTRACT
                        );
                }
            }
            return result;
        }

        public void DecryptFile(SkydrmApp SkydrmApp, string nXlFileLocalPath, out string decryptedFilePath)
        {
            decryptedFilePath = RightsManagementService.GenerateDecryptFilePath(SkydrmApp.User.RPMFolder, nXlFileLocalPath, DecryptIntent.ExtractContent);
            RightsManagementService.DecryptNXLFile(SkydrmApp, nXlFileLocalPath, decryptedFilePath);
        }

        public bool MoveFile(SkydrmApp SkydrmApp, string sourceFilePath, string destFilePath)
        {
            bool result = false;
            try
            {
                if (File.Exists(sourceFilePath))
                {
                    File.Copy(sourceFilePath, destFilePath, true);
                    RightsManagementService.RPMDeleteDirectory(SkydrmApp, Path.GetDirectoryName(sourceFilePath));
                    result = true;
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
