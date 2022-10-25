using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Viewer.upgrade.database;
using Viewer.upgrade.application;
using Viewer.upgrade.utils;
using Viewer.upgrade.communication.message;

namespace Viewer.upgrade.file.component.export
{
    public class FileExport
    {
        private ViewerApp mApplication;
        private static FileExport mFileExport = new FileExport();
        private ProjectFileExport mProjectFileExport;
        private MyVaultFileExport mMyVaultFileExport;
        private ShareWithMeFileExport mShareWithMeFileExport;
        private WorkSpaceFileExport mWorkSpaceFileExport;
        private ShareWorkSpaceFileExport mShareWorkSpaceFileExport;

        public static FileExport GetInstance()
        {
            return mFileExport;
        }

        private FileExport()
        {
            mApplication = (ViewerApp)ViewerApp.Current;
            mProjectFileExport = new ProjectFileExport();
            mMyVaultFileExport = new MyVaultFileExport();
            mShareWithMeFileExport = new ShareWithMeFileExport();
            mWorkSpaceFileExport = new WorkSpaceFileExport();
            mShareWorkSpaceFileExport =new ShareWorkSpaceFileExport();
        }

        public void Export(string repoId, string displayPath, NxlFileFingerPrint nxlFileFingerPrint, Window owner)
        {
            string destPath = string.Empty;
            try
            {
                destPath = InputDestinationPath(nxlFileFingerPrint, owner);

                if (string.IsNullOrEmpty(destPath))
                {
                    return;
                }

                //mShareWorkSpaceFileExport.Export(mApplication.SdkSession, repoId, displayPath, destPath);
                Export(nxlFileFingerPrint.name, displayPath, NxlFileSpaceType.sharepoint_online, repoId, destPath);
                NotifyMsg(mApplication.SdkSession, true, nxlFileFingerPrint.name, destPath);
            }
            catch (Exception ex)
            {
                NotifyMsg(mApplication.SdkSession, false, nxlFileFingerPrint.name, destPath);
                throw ex;
            }
        }

        public void Export(string fileRepo, NxlFileFingerPrint nxlFileFingerPrint, Window owner)
        {
            string destPath = string.Empty;
   
            try
            {
                destPath = InputDestinationPath(nxlFileFingerPrint, owner);

                if (string.IsNullOrEmpty(destPath))
                {
                    return;
                }

                FunctionProvider functionProvider = mApplication.FunctionProvider;

                if (nxlFileFingerPrint.isFromSystemBucket)
                {
                    WorkSpaceFile workSpaceFile = functionProvider.QueryWorkSpacetFileByDuid(nxlFileFingerPrint.duid);
                    if (null != workSpaceFile)
                    {
                        //mWorkSpaceFileExport.Export(mApplication.SdkSession, workSpaceFile.RmsPathId, destPath);
                        Export(nxlFileFingerPrint.name, workSpaceFile.RmsPathId, NxlFileSpaceType.enterprise_workspace, "", destPath);
                        NotifyMsg(mApplication.SdkSession ,true, nxlFileFingerPrint.name, destPath);
                    }
                }
                else if (nxlFileFingerPrint.isFromPorject)
                {
                    // means from Porject
                    int projectId;
                    ProjectFile projectFile = functionProvider.QueryProjectFileByDuid(nxlFileFingerPrint.duid, out projectId);
                    if (null != projectFile)
                    {
                        //mProjectFileExport.Export(mApplication.SdkSession, projectId, projectFile.Rms_path_id, destPath);
                        Export(nxlFileFingerPrint.name, projectFile.Rms_path_id, NxlFileSpaceType.project, projectId.ToString(), destPath);
                        NotifyMsg(mApplication.SdkSession,true, nxlFileFingerPrint.name, destPath);
                    }
                }
                else if (nxlFileFingerPrint.isFromMyVault)
                {
                    //Filtered file shared by me.
                    if (nxlFileFingerPrint.isOwner && !string.Equals(fileRepo, "REPO_SHARED_WITH_ME", StringComparison.CurrentCultureIgnoreCase))
                    {
                        // meansfrom MyVault
                        MyVaultFile myVaultFile = functionProvider.QueryMyVaultFileByDuid(nxlFileFingerPrint.duid);
                        if (null != myVaultFile)
                        {
                            //mMyVaultFileExport.Export(mApplication.SdkSession, nxlFileFingerPrint.name, myVaultFile.RmsPathId, destPath);
                            Export(nxlFileFingerPrint.name, myVaultFile.RmsPathId, NxlFileSpaceType.my_vault, "", destPath);
                            NotifyMsg(mApplication.SdkSession, true, nxlFileFingerPrint.name, destPath);
                        }
                    }
                    else
                    {
                        // means from ShareWithMe
                        SharedWithMeFile sharedWithMeFile = functionProvider.QuerySharedWithMeFileByDuid(nxlFileFingerPrint.duid);
                        if (null != sharedWithMeFile)
                        {
                            //mShareWithMeFileExport.Export(mApplication.SdkSession, nxlFileFingerPrint.name, sharedWithMeFile.Transaction_id, sharedWithMeFile.Transaction_code, destPath);
                            var rmsPathId = "/" + sharedWithMeFile.Name;
                            Export(nxlFileFingerPrint.name, rmsPathId, NxlFileSpaceType.shared_with_me, "", destPath, sharedWithMeFile.Transaction_code, sharedWithMeFile.Transaction_id);
                            NotifyMsg(mApplication.SdkSession, true, nxlFileFingerPrint.name, destPath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NotifyMsg(mApplication.SdkSession, false, nxlFileFingerPrint.name, destPath);
                throw ex;
            }
        }

        public void Export(string srcFileName, string srcPathId, NxlFileSpaceType srcRepo, string srcRepoId,
            string destPath, string transactionCode = "", string transactionId = "")
        {
            var app = mApplication;

            app.Log.Info(string.Format("{0} try to export file, path {1}", srcRepo.ToString(), destPath));
            string currentUserTempPathOrDownloadFilePath = Path.GetTempPath();
            try
            {
                app.SdkSession.User.CopyNxlFile(srcFileName, srcPathId, srcRepo, srcRepoId,
                       Path.GetFileName(destPath), currentUserTempPathOrDownloadFilePath, NxlFileSpaceType.local_drive, "",
                       true, transactionCode, transactionId);

                string downloadFilePath = currentUserTempPathOrDownloadFilePath + Path.GetFileName(destPath);
                File.Copy(downloadFilePath, destPath, true);
            }
            catch (Exception e)
            {
                app.Log.Error(string.Format("{0} failed to export file {1}.", srcRepo.ToString(), destPath), e);
                throw e;
            }
            finally
            {
                FileUtils.DelFileNoThrow(currentUserTempPathOrDownloadFilePath);
            }
        }

        private void NotifyMsg(SkydrmLocal.rmc.sdk.Session session, bool succeeded, string nxlFileName, string destinationPath)
        {
            if (succeeded)
            {
                MessageNotify.NotifyMsg(session, nxlFileName, mApplication.FindResource("Exception_ExportFeature_Succeeded").ToString() + destinationPath + ".", EnumMsgNotifyType.PopupBubble, MsgNotifyOperation.SAVE_AS, EnumMsgNotifyResult.Succeed, EnumMsgNotifyIcon.Online);
            }
            else
            {
                MessageNotify.NotifyMsg(session, nxlFileName, mApplication.FindResource("Exception_ExportFeature_Failed").ToString() + destinationPath + ".", EnumMsgNotifyType.PopupBubble, MsgNotifyOperation.SAVE_AS, EnumMsgNotifyResult.Failed, EnumMsgNotifyIcon.Online);
            }
        }

        public string InputDestinationPath(NxlFileFingerPrint nxlFileFingerPrint, Window owner)
        {
            string destinationPath = string.Empty;
            try
            {
                //fix bug 53134 add new feature, 
                // extract timestamp in target.Name and replaced it as local lastest one
                // ShowSaveFileDialog(out destinationPath, ModifyExportedFileNameReplacedWithLatestTimestamp(nxlFileFingerPrint.name), owner);
                ShowSaveFileDialog(out destinationPath, nxlFileFingerPrint.name, owner);
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message.ToString());
            }

            return destinationPath;
        }

        private bool ShowSaveFileDialog(out string destinationPath, string fileName, Window owner)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.CheckFileExists = false;
            dlg.FileName = fileName; // Default file name
            dlg.DefaultExt = Path.GetExtension(fileName); // .nxl Default file extension
            dlg.Filter = "NextLabs Protected Files (*.nxl)|*.nxl"; // Filter files by extension

            // Show save file dialog box
            Nullable<bool> result = dlg.ShowDialog(owner);
            // Process save file dialog box results
            if (result == true)
            {
                destinationPath = dlg.FileName;

                if (Path.HasExtension(destinationPath))
                {
                    if (!string.Equals(Path.GetExtension(destinationPath), ".nxl", StringComparison.CurrentCultureIgnoreCase))
                    {
                        // destinationPath = destinationPath.Substring(0, destinationPath.LastIndexOf(".")) + ".nxl";
                        destinationPath += ".nxl";
                    }
                }

            }
            else
            {
                destinationPath = string.Empty;
            }

            return result.Value;
        }

        private string ModifyExportedFileNameReplacedWithLatestTimestamp(string fname)
        {
            // like log-2019-01-24-07-04-28.txt
            // pattern-match "-2019-01-24-07-04-28" replaced with latest lcoal timestamp
            string pattern = @"-\d{4}-\d{2}-\d{2}-\d{2}-\d{2}-\d{2}";
            // new stime string
            string newTimeStamp = DateTime.Now.ToLocalTime().ToString("-yyyy-MM-dd-HH-mm-ss");
            Regex r = new Regex(pattern);
            string newName = fname;
            if (r.IsMatch(fname))
            {
                newName = r.Replace(fname, newTimeStamp);
            }
            return newName;
        }

    }
}
