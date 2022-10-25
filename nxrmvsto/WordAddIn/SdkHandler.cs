using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

using WordAddIn.featureProvider;
using WordAddIn.sdk;
using WordAddIn.sdk.helper;

namespace WordAddIn
{
    public class SdkHandler
    {
        private string appName { get; set; }

        private Form callerForm { get; set; }

        /// <summary>
        /// When protect successful, delete plain file flag
        /// </summary>
        public bool isDeletePlainFile { get; set; }

        /// <summary>
        /// Flag that for fix open nxl file carsh bug in word do SaveAs
        /// </summary>
        public bool IsAddSpecificSymbol { get; set; }

        /// <summary>
        /// Flag that if remove the timestamp when protect, default is true.
        /// </summary>
        private bool isRemoveTimeStamp = true;
        public bool IsRemoveTimeStamp { get => isRemoveTimeStamp; set => isRemoveTimeStamp = value; }

        #region SDK
        private Session session;
        public Session Rmsdk { get => session; }
        public bool SysBucketIsEnableAdhoc { get; private set; }
        public ProjectClassification[] SysBucketClassifications { get; private set; }
        public WaterMarkInfo RmsWaterMarkInfo { get; private set; }
        public sdk.Expiration RmsExpiration { get; private set; }
        public bool bRecoverSucceed { get; set; }

        public SdkHandler(string appname)
        {
            appName = appname;

            try
            {
                SdkLibInit();

                bRecoverSucceed = RecoverSession();
                if (bRecoverSucceed)
                {
                    Debug.WriteLine("Vsto sdkSession Recover Successful.");
                    InitSystemBucket();
                    GetDocumentPreference();
                }
            }
            catch (SkydrmException skyEx)
            {
                NotifyMsg("",
                    skyEx.Message,
                    SdkHandler.EnumMsgNotifyType.PopupBubble, "",
                    SdkHandler.EnumMsgNotifyResult.Failed,
                    SdkHandler.EnumMsgNotifyIcon.Unknown);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        private void SdkLibInit()
        {
            Apis.SdkLibInit();
        }

        public void SdkLibCleanup()
        {
            Apis.SdkLibCleanup();
        }

        // Recover session and user, and init rmSdk(Session & User).
        private bool RecoverSession()
        {
            if (Apis.GetCurrentLoggedInUser(out session))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void InitSystemBucket()
        {
            string sbTenant = Rmsdk.User.GetSystemProjectTenantId();
            SysBucketClassifications = Rmsdk.User.GetProjectClassification(sbTenant);
            SysBucketIsEnableAdhoc = Rmsdk.User.IsEnabledAdhocForSystemBucket();
        }

        public string LocalPath(string fullPath)
        {
            if (fullPath.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase))
            {
                // So Documents/ location works below
                fullPath = fullPath.Replace("\\", "/");

                var userAccounts = Microsoft.Win32.Registry.CurrentUser
                    .OpenSubKey(@"Software\Microsoft\OneDrive\Accounts\");

                if (userAccounts != null)
                {
                    foreach (var accountName in userAccounts.GetSubKeyNames())
                    {
                        var account = userAccounts.OpenSubKey(accountName);
                        var endPoint = account.GetValue("ServiceEndPointUri") as string;
                        var userFolder = account.GetValue("UserFolder") as string;

                        if (!string.IsNullOrEmpty(endPoint) && !string.IsNullOrEmpty(userFolder))
                        {
                            if (endPoint.EndsWith("/_api"))
                            {
                                endPoint = endPoint.Substring(0, endPoint.Length - 4) + "documents/";
                            }

                            if (fullPath.StartsWith(endPoint, StringComparison.InvariantCultureIgnoreCase))
                            {
                                return Path.Combine(userFolder, fullPath.Substring(endPoint.Length));
                            }
                        }
                    }
                }
            }

            return fullPath;
        }

        private void GetDocumentPreference()
        {
            try
            {
                //invoke SDWLResult GetUserPreference
                sdk.Expiration eprn;
                string watermark;

                Rmsdk.User.GetPreference(out eprn,
                    out watermark);

                if (watermark != null)
                {
                    WaterMarkInfo waterMarkInfo = new WaterMarkInfo();
                    waterMarkInfo.text = watermark;

                    //set Watermark
                    RmsWaterMarkInfo = waterMarkInfo;
                }

                //set expiration
                RmsExpiration = eprn;
            }
            catch (Exception msg)
            {
                Debug.WriteLine(msg);
            }
        }

        public bool IsNxlFile(string plainFilePath)
        {
            bool result = false;
            if (string.IsNullOrEmpty(plainFilePath))
            {
                return result;
            }

            bool invoke;
            invoke = Rmsdk.SDWL_RPM_GetFileStatus(plainFilePath, out int dirstatus, out bool filestatus);
            if (invoke)
            {
                result = filestatus;
            }

            return result;
        }

        public bool IsRPMFolder(string folderPath)
        {
            bool result = false;
            if (string.IsNullOrEmpty(folderPath))
            {
                return result;
            }
            result = Rmsdk.RMP_IsSafeFolder(folderPath);
            return result;
        }

        #region SystemBucket Protect
        public string ProtectFileAdhoc(Form caller, string PlainFilePath, List<FileRights> rights, WaterMarkInfo waterMark, sdk.Expiration expiration)
        {
            callerForm = caller;

            int Id = Rmsdk.User.GetSystemProjectId();

            string outpath = Rmsdk.User.ProtectFileToSystemProject(
                Id, PlainFilePath,
                rights, waterMark, expiration, new UserSelectTags());

            return DoAfterProtect(PlainFilePath, outpath);
        }

        public string ProtectFileAdhoc(string PlainFilePath, string DestPath, List<FileRights> rights, WaterMarkInfo waterMark, sdk.Expiration expiration)
        {
            int Id = Rmsdk.User.GetSystemProjectId();

            string outpath = Rmsdk.User.ProtectFileToSystemProject(
                Id, PlainFilePath,
                rights, waterMark, expiration, new UserSelectTags());

            return DoAfterProtect(PlainFilePath, outpath, DestPath, false);
        }

        /// <summary>
        /// In main thread do protect, and show overwrite dialog
        /// </summary>
        /// <param name="PlainFilePath"></param>
        /// <param name="tags"></param>
        /// <returns></returns>
        public string ProtectFileCentrolPolicy(string PlainFilePath, UserSelectTags tags)
        {
            List<FileRights> defaultRights;
            WaterMarkInfo defaultWatermark;
            sdk.Expiration defaultExpiration;

            GenerateDefaultValue(out defaultRights, out defaultWatermark, out defaultExpiration);

            int Id = Rmsdk.User.GetSystemProjectId();

            string outpath = Rmsdk.User.ProtectFileToSystemProject(
                Id, PlainFilePath,
                defaultRights, defaultWatermark, defaultExpiration,
                tags);

            return HandleFile(PlainFilePath, outpath);
        }

        public string ProtectFileCentrolPolicy(Form caller, string PlainFilePath, UserSelectTags tags)
        {
            callerForm = caller;

            List<FileRights> defaultRights;
            WaterMarkInfo defaultWatermark;
            sdk.Expiration defaultExpiration;

            GenerateDefaultValue(out defaultRights, out defaultWatermark, out defaultExpiration);

            int Id = Rmsdk.User.GetSystemProjectId();

            string outpath = Rmsdk.User.ProtectFileToSystemProject(
                Id, PlainFilePath,
                defaultRights, defaultWatermark, defaultExpiration,
                tags);

            return DoAfterProtect(PlainFilePath, outpath);
        }

        public string ProtectFileCentrolPolicy(string PlainFilePath, string DestPath, UserSelectTags tags)
        {
            List<FileRights> defaultRights;
            WaterMarkInfo defaultWatermark;
            sdk.Expiration defaultExpiration;

            GenerateDefaultValue(out defaultRights, out defaultWatermark, out defaultExpiration);

            int Id = Rmsdk.User.GetSystemProjectId();

            string outpath = Rmsdk.User.ProtectFileToSystemProject(
                Id, PlainFilePath,
                defaultRights, defaultWatermark, defaultExpiration,
                tags);

            return DoAfterProtect(PlainFilePath, outpath, DestPath, false);
        }

        private void GenerateDefaultValue(out List<FileRights> defaultRights,
            out WaterMarkInfo defaultWatermark,
            out sdk.Expiration defaultExpiration)
        {
            defaultRights = new List<FileRights>();

            defaultWatermark = new WaterMarkInfo()
            {
                fontColor = "",
                fontName = "",
                text = "",
                fontSize = 0,
                repeat = 0,
                rotation = 0,
                transparency = 0
            };
            defaultExpiration = new sdk.Expiration()
            {
                type = sdk.ExpiryType.NEVER_EXPIRE,
                Start = 0,
                End = 0
            };
        }

        private bool isMessageBoxHasShow = false;

        private string HandleFile(string plainFilePath, string nxlProtectedFilePath)
        {
            string newNxl = Path.GetFileName(nxlProtectedFilePath);

            // If the file comes from the RPM folder, regardless of whether it has a corresponding nxl file, try to delete the nxl and plain file for later copying.
            bool fromRPM = IsRPMFolder(Path.GetDirectoryName(plainFilePath));
            //if (fromRPM)
            //{
            //    //NotifyMsg("",
            //    //  "The file can not be automatically encrypted in RPM folder",
            //    //  SdkHandler.EnumMsgNotifyType.PopupBubble, "",
            //    //  SdkHandler.EnumMsgNotifyResult.Failed,
            //    //  SdkHandler.EnumMsgNotifyIcon.Unknown);
            //    //Rmsdk.RPM_DeleteFile(plainFilePath + ".nxl");
            //   // Rmsdk.RPM_DeleteFile(plainFilePath);
            //}
            //else
            //{
            //    //if (isDeletePlainFile)
            //    //{
            //    //    Rmsdk.RPM_DeleteFile(plainFilePath);
            //    //}
            //}

            // path combination    
            string destPath = Path.Combine(Path.GetDirectoryName(plainFilePath), newNxl);

            if (IsRemoveTimeStamp)
            {
                Utils.Replace(destPath, out destPath, Utils.TIMESTAMP_PATTERN, RegexOptions.IgnoreCase);
            }

            if (IsAddSpecificSymbol)
            {
                string tempPath = destPath;
                string tempDirec = Path.GetDirectoryName(tempPath);
                string tempNameExt = Path.GetFileNameWithoutExtension(tempPath);// return document.docx
                string tempName = Path.GetFileNameWithoutExtension(tempNameExt);// return document
                string tempExten = Path.GetExtension(tempNameExt);// return .docx
                string tempExten2 = Path.GetExtension(tempPath);// return .nxl
                destPath = tempDirec + Path.DirectorySeparatorChar + tempName + "-" + tempExten + tempExten2;
                IsAddSpecificSymbol = false;
            }


            if (File.Exists(destPath))
            {
                bool isCancel = true;

                string message = "The destination folder already has a file named \"" + destPath + "\". "
            + "Are you sure you want to replace this file in the destination folder?";
                string caption = "NextLabs Rights Management";
                MessageBoxButtons buttons = MessageBoxButtons.YesNo;
                DialogResult result;

                // Displays the MessageBox.
                if (!isMessageBoxHasShow)
                {
                    result = MessageBox.Show(message, caption, buttons,
                        MessageBoxIcon.Warning);
                    if (result == DialogResult.Yes)
                    {
                        isCancel = false;
                    }
                    else
                    {
                        isCancel = true;
                    }
                }

                if (!isCancel)
                {

                    try
                    {
                        if (fromRPM)
                        {
                            Rmsdk.RPM_DeleteFile(plainFilePath + ".nxl");
                            Rmsdk.RPM_DeleteFile(plainFilePath);

                            Rmsdk.RPM_copyFile(nxlProtectedFilePath, destPath, false);
                        }
                        else
                        {
                            if (isDeletePlainFile)
                            {
                                Rmsdk.RPM_DeleteFile(plainFilePath);
                            }

                            File.Copy(nxlProtectedFilePath, destPath, true);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e);
                    }
                }
                else
                {
                    isMessageBoxHasShow = true;
                    throw new Exception("Cancel");
                }
            }
            else
            {
                try
                {
                    if (fromRPM)
                    {
                        Rmsdk.RPM_DeleteFile(plainFilePath + ".nxl");
                        Rmsdk.RPM_DeleteFile(plainFilePath);

                        Rmsdk.RPM_copyFile(nxlProtectedFilePath, destPath, false);
                    }
                    else
                    {
                        if (isDeletePlainFile)
                        {
                            Rmsdk.RPM_DeleteFile(plainFilePath);
                        }

                        File.Copy(nxlProtectedFilePath, destPath, true);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            }


            // add workaround feature: delete nxlProtectedFilePath in sdk folder 
            try
            {
                Rmsdk.User.RemoveLocalGeneratedFiles(nxlProtectedFilePath);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

            return destPath;
        }

        private string DoAfterProtect(string plainFilePath, string nxlProtectedFilePath)
        {
            string newNxl = Path.GetFileName(nxlProtectedFilePath);

            // path combination    
            string destPath = Path.Combine(Path.GetDirectoryName(plainFilePath), newNxl);

            if (IsRemoveTimeStamp)
            {
                Utils.Replace(destPath, out destPath, Utils.TIMESTAMP_PATTERN, RegexOptions.IgnoreCase);
            }

            if (IsAddSpecificSymbol)
            {
                string tempPath = destPath;
                string tempDirec = Path.GetDirectoryName(tempPath);
                string tempNameExt = Path.GetFileNameWithoutExtension(tempPath);// return document.docx
                string tempName = Path.GetFileNameWithoutExtension(tempNameExt);// return document
                string tempExten = Path.GetExtension(tempNameExt);// return .docx
                string tempExten2 = Path.GetExtension(tempPath);// return .nxl
                destPath = tempDirec + Path.DirectorySeparatorChar + tempName + "-" + tempExten + tempExten2;
                IsAddSpecificSymbol = false;
            }

            if (File.Exists(destPath))
            {
                bool isCancel = true;
                if (callerForm != null && callerForm.InvokeRequired)
                {
                    callerForm.Invoke(new Action(() =>
                    {
                        string message = "The destination folder already has a file named \"" + destPath + "\". "
                    + "Are you sure you want to replace this file in the destination folder?";
                        string caption = "NextLabs Rights Management";
                        MessageBoxButtons buttons = MessageBoxButtons.YesNo;
                        DialogResult result;

                        // Displays the MessageBox.

                        result = MessageBox.Show(callerForm, message, caption, buttons,
                            MessageBoxIcon.Warning);
                        if (result == DialogResult.Yes)
                        {
                            isCancel = false;
                        }
                        else
                        {
                            isCancel = true;
                        }
                    }));
                }

                if (!isCancel)
                {
                    try
                    {
                        File.Copy(nxlProtectedFilePath, destPath, true);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e);
                        throw e;
                    }
                }
                else
                {
                    throw new Exception("Cancel");
                }
            }
            else
            {
                try
                {
                    File.Copy(nxlProtectedFilePath, destPath, false);
                }
                catch (Exception)
                {
                    throw;
                }
            }

            // add workaround feature: delete nxlProtectedFilePath in sdk folder 
            try
            {
                Rmsdk.User.RemoveLocalGeneratedFiles(nxlProtectedFilePath);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

            try
            {
                if (isDeletePlainFile)
                {
                    //File.Delete(plainFilePath);
                    Rmsdk.RPM_DeleteFile(plainFilePath);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }


            return destPath;
        }

        private string DoAfterProtect(string plainFilePath, string nxlProtectedFilePath, string destPath, bool isNeedDeleteSourceFile)
        {
            try
            {
                File.Copy(nxlProtectedFilePath, destPath, false);
            }
            catch (Exception e) // The file 'C:\Users\aning\Desktop\project.jpg.nxl' already exists.
            {
                Debug.WriteLine(e);
                throw e;
            }

            // add workaround feature: delete nxlProtectedFilePath in sdk folder 
            try
            {
                Rmsdk.User.RemoveLocalGeneratedFiles(nxlProtectedFilePath);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }


            try
            {
                if (isNeedDeleteSourceFile)
                {
                    File.Delete(plainFilePath);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

            return destPath;
        }
        #endregion

        #region Get RPMFileRights
        public void GetRPMFileRights(string plainFilePath, out List<FileRights> rights, out WaterMarkInfo watermark)
        {
            Rmsdk.RPMGetFileRights(plainFilePath, out rights, out watermark);
        }

        public Dictionary<string, List<string>> ReadFileTags(string plainFilePath)
        {
            string tags = Rmsdk.RPMReadFileTags(plainFilePath);
            return Utils.ParseClassificationTag(tags);
        }
        #endregion

        #region Notify Message
        public void NotifyMsg(string target, string message, EnumMsgNotifyType msgtype, string operation, EnumMsgNotifyResult result, EnumMsgNotifyIcon fileStatus)
        {
            bool ret = Rmsdk.SDWL_RPM_NotifyMessage(appName,
                target,
                message,
                Convert.ToUInt32(msgtype),
                operation,
                Convert.ToUInt32(result),
                Convert.ToUInt32(fileStatus));

            if (!ret)
            {
                Debug.WriteLine("Notify message failed.");
            }
        }

        public enum EnumMsgNotifyType
        {
            LogMsg = 0,
            PopupBubble = 1
        }

        public enum EnumMsgNotifyResult
        {
            Failed = 0,
            Succeed = 1
        }

        public enum EnumMsgNotifyIcon
        {
            Unknown = 0,
            Online = 1,
            Offline = 2,
            WaitingUpload = 3
        }
        #endregion

        #endregion


    }
}
