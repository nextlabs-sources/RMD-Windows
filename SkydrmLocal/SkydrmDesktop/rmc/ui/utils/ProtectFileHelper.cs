using Alphaleonis.Win32.Filesystem;
using SkydrmDesktop;
using SkydrmDesktop.Resources.languages;
using SkydrmDesktop.rmc.featureProvider.MessageNotify;
using SkydrmDesktop.rmc.fileSystem.externalDrive;
using SkydrmDesktop.rmc.fileSystem.sharedWorkspace;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.ui.utils
{
    static class ProtectFileHelper
    {
        private static SkydrmApp App = (SkydrmApp)SkydrmApp.Current;

        private static readonly string[] notSupptGoogleFile = new string[] {".gdoc", ".gsheet", ".gslides", ".gdraw" };
        /// <summary>
        /// Check select file path
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="tag"></param>
        /// <param name="rightFilePath"></param>
        /// <returns></returns>
        public static bool CheckFilePathDoProtect(string[] filePath, out string tag, out List<string> rightFilePath)
        {
            bool isOK = false;
            tag = "";

            rightFilePath = new List<string>();
            List<string> emptyFileName = new List<string>();
            List<string> nxlFileName = new List<string>();
            List<string> notSuptGoogleFileName = new List<string>();
            try
            {
                for (int i = 0; i < filePath.Length; i++)
                {
                    // Sanity check
                    if (filePath[i] == null || filePath[i].Length == 0)
                    {
                        return false;
                    }

                    // new feature request, deny ops in rmp folder
                    //var dirPath = Path.GetDirectoryName(filePath[i]);
                    if (App.Rmsdk.SDWL_RPM_GetFileStatus(filePath[i], out int dirstatus, out bool filestatus))
                    {
                        if (filestatus)
                        {
                            nxlFileName.Add(new FileInfo(filePath[i]).Name);
                            continue;
                        }
                    }

                    // Required to FILTER OUT 0-SIZED FILE
                    if (common.helper.Win32Common.FileSizeOnDisk.GetFileSizeOnDisk(filePath[i]) == 0)
                    {
                        emptyFileName.Add(new FileInfo(filePath[i]).Name);
                        continue;
                    }

                    string ext = Path.GetExtension(filePath[i]);
                    if (notSupptGoogleFile.Contains(ext.ToLower()))
                    {
                        notSuptGoogleFileName.Add(new FileInfo(filePath[i]).Name);
                        continue;
                    }
                    else
                    {
                        rightFilePath.Add(filePath[i]);
                        tag += filePath[i];
                    }
                }

                // File size is 0
                if (emptyFileName.Count > 0)
                {
                    foreach (var item in emptyFileName)
                    {
                        App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Common_Notify_EmptyFile_Not_Operate"), false, item, MsgNotifyOperation.PROTECT);
                    }
                }

                // File is nxl
                if (nxlFileName.Count > 0)
                {
                    foreach (var item in nxlFileName)
                    {
                        App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Common_Notify_NxlFile_Not_Protect"), false, item, MsgNotifyOperation.PROTECT);
                    }
                }

                // Not support google file.
                if (notSuptGoogleFileName.Count > 0)
                {
                    foreach (var item in notSuptGoogleFileName)
                    {
                        App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Common_Notify_GoogleFileType_Not_Protect"), false, item, MsgNotifyOperation.PROTECT);
                    }
                }

                if (rightFilePath.Count > 0)
                {
                    isOK = true;
                }
            }
            catch (Exception e)
            {
                App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Common_System_Internal_Error"), false);
                App.Log.Error("Error in CheckFilePathDoProtect:", e);
                isOK = false;
            }
            return isOK;
        }

        public static INxlFile MyVaultAddLocalFile(string filePath, List<FileRights> fileRights, WaterMarkInfo waterMark, Expiration expiration, out string msg)
        {
            msg = string.Empty;
            try
            {
                UserSelectTags userSelectTags = new UserSelectTags();
                var localFile = App.MyVault.AddLocalAdded(filePath, fileRights,
                                                waterMark, expiration, userSelectTags, null, null);

                PendingUploadFile doc = new PendingUploadFile(localFile);

                return doc;
            }
            catch (Exception e)
            {
                msg = e.Message;
                // Notify serviceManger
                App.MessageNotify.NotifyMsg(Alphaleonis.Win32.Filesystem.Path.GetFileName(filePath),
                    e.Message,
                    EnumMsgNotifyType.LogMsg,
                    MsgNotifyOperation.PROTECT,
                    EnumMsgNotifyResult.Failed,
                    EnumMsgNotifyIcon.Unknown);
                return null;
            }
        }

        public static INxlFile SystemBucketAddLocalFile(bool isCentralPolicy, string filePath, string destFolder, 
            List<FileRights> fileRights, WaterMarkInfo waterMark, Expiration expiration, UserSelectTags userSelectTags,
            out string msg)
        {
            msg = string.Empty;
            try
            {
                if (!Alphaleonis.Win32.Filesystem.Directory.Exists(destFolder))
                {
                    // The local drive path does not exist
                    var message = CultureStringInfo.ApplicationFindResource("ProtectOperation_LocalDrive_NotExist");
                    throw new Exception(message);
                }

                string outPath = string.Empty;
                if (isCentralPolicy)
                {
                    outPath = App.SystemProject.ProtectFileCentrolPolicy(filePath, destFolder, userSelectTags);
                }
                else
                {
                    outPath = App.SystemProject.ProtectFileAdhoc(filePath, destFolder, fileRights, waterMark, expiration);
                }

                NxlDoc doc = new NxlDoc();
                doc.Name = Alphaleonis.Win32.Filesystem.Path.GetFileName(outPath);

                App.MessageNotify.NotifyMsg(doc.Name, CultureStringInfo.ApplicationFindResource("Notify_RecordLog_Protect_Succeed"), EnumMsgNotifyType.LogMsg,
                    MsgNotifyOperation.PROTECT, EnumMsgNotifyResult.Succeed, EnumMsgNotifyIcon.Online);

                return doc;
            }
            catch (Exception e)
            {
                msg = e.Message;
                // Notify serviceManger
                App.MessageNotify.NotifyMsg(Alphaleonis.Win32.Filesystem.Path.GetFileName(filePath),
                    e.Message,
                    EnumMsgNotifyType.LogMsg,
                    MsgNotifyOperation.PROTECT,
                    EnumMsgNotifyResult.Failed,
                    EnumMsgNotifyIcon.Unknown);
                return null;
            }
        }

        public static INxlFile WorkSpaceAddLocalFile(bool isCentralPolicy, string filePath, string parentFolder, 
            List<FileRights> fileRights, WaterMarkInfo waterMark, Expiration expiration, UserSelectTags userSelectTags,
            out string msg)
        {
            msg = string.Empty;
            try
            {
                if (isCentralPolicy)
                {
                    fileRights.Clear();
                }
                else
                {
                    userSelectTags = new UserSelectTags();
                }
                var localfile = App.WorkSpace.AddLocalAdded(parentFolder,
                                                     filePath, fileRights, waterMark, expiration, userSelectTags);

                PendingUploadFile doc = new PendingUploadFile(localfile);

                return doc;
            }
            catch (Exception e)
            {
                msg = e.Message;
                // Notify serviceManger
                App.MessageNotify.NotifyMsg(Alphaleonis.Win32.Filesystem.Path.GetFileName(filePath),
                    e.Message,
                    EnumMsgNotifyType.LogMsg,
                    MsgNotifyOperation.PROTECT,
                    EnumMsgNotifyResult.Failed,
                    EnumMsgNotifyIcon.Unknown);
                return null;
            }
        }

        public static INxlFile ProjectAddLocalFile(bool isCentralPolicy, string filePath, string parentFolder, List<FileRights> fileRights, WaterMarkInfo waterMark, Expiration expiration, UserSelectTags userSelectTags,
            SkydrmLocal.rmc.featureProvider.IMyProject selectedProject, out string msg)
        {
            msg = string.Empty;
            if (selectedProject == null)
            {
                msg = "Selected project is null";
                return null;
            }
            try
            {
                if (isCentralPolicy)
                {
                    fileRights.Clear();
                }
                else
                {
                    userSelectTags = new UserSelectTags();
                }
                var localfile = selectedProject.AddLocalFile(parentFolder,
                                                     filePath, fileRights, waterMark, expiration, userSelectTags);

                PendingUploadFile doc = new PendingUploadFile(localfile, selectedProject.Id, selectedProject.DisplayName);

                return doc;
            }
            catch (Exception e)
            {
                msg = e.Message;
                // Notify serviceManger
                App.MessageNotify.NotifyMsg(Alphaleonis.Win32.Filesystem.Path.GetFileName(filePath),
                    e.Message,
                    EnumMsgNotifyType.LogMsg,
                    MsgNotifyOperation.PROTECT,
                    EnumMsgNotifyResult.Failed,
                    EnumMsgNotifyIcon.Unknown);
                return null;
            }
        }

        public static INxlFile ExternalRepoAddLocalFile(bool isCentralPolicy, string filePath, string parentFolder, 
            List<FileRights> fileRights, WaterMarkInfo waterMark, Expiration expiration, UserSelectTags userSelectTags,
            ExternalRepo selectedRepo, out string msg)
        {
            msg = string.Empty;
            if (selectedRepo == null)
            {
                msg = "Selected repository is null";
                return null;
            }
            try
            {
                if (isCentralPolicy)
                {
                    fileRights.Clear();
                }
                else
                {
                    userSelectTags = new UserSelectTags();
                }

                return selectedRepo.AddLocalFile(parentFolder,
                                                 filePath, 
                                                 fileRights, 
                                                 waterMark, 
                                                 expiration, 
                                                 userSelectTags);
            }
            catch (Exception e)
            {
                msg = e.Message;
                // Notify serviceManger
                App.MessageNotify.NotifyMsg(Alphaleonis.Win32.Filesystem.Path.GetFileName(filePath),
                    e.Message,
                    EnumMsgNotifyType.LogMsg,
                    MsgNotifyOperation.PROTECT,
                    EnumMsgNotifyResult.Failed,
                    EnumMsgNotifyIcon.Unknown);
                return null;
            }
        }

        public static INxlFile SharedWorkSpaceAddLocalFile(bool isCentralPolicy, string filePath, string parentPathId, string parentDisplayPath,
            List<FileRights> fileRights, WaterMarkInfo waterMark, Expiration expiration, UserSelectTags userSelectTags,
            SharedWorkspaceRepo selectedRepo, out string msg)
        {
            msg = string.Empty;
            if (selectedRepo == null)
            {
                msg = "Selected repository is null";
                return null;
            }
            try
            {
                if (isCentralPolicy)
                {
                    fileRights.Clear();
                }
                else
                {
                    userSelectTags = new UserSelectTags();
                }

                return selectedRepo.AddLocalFile(parentPathId, parentDisplayPath,
                                                 filePath,
                                                 fileRights,
                                                 waterMark,
                                                 expiration,
                                                 userSelectTags);
            }
            catch (Exception e)
            {
                msg = e.Message;
                // Notify serviceManger
                App.MessageNotify.NotifyMsg(Alphaleonis.Win32.Filesystem.Path.GetFileName(filePath),
                    e.Message,
                    EnumMsgNotifyType.LogMsg,
                    MsgNotifyOperation.PROTECT,
                    EnumMsgNotifyResult.Failed,
                    EnumMsgNotifyIcon.Unknown);
                return null;
            }
        }

    }
}
