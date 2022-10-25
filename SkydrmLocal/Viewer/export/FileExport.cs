using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Viewer.database;
using Viewer.utils;

namespace Viewer.export
{
    public class FileExport
    {
        private static FileExport mFileExport = new FileExport();
        private ProjectFileExport mProjectFileExport;
        private MyVaultFileExport mMyVaultFileExport;
        private ShareWithMeFileExport mShareWithMeFileExport;
        private WorkSpaceFileExport mWorkSpaceFileExport;

        public static FileExport GetInstance()
        {
            return mFileExport;
        }

        private FileExport()
        {
            mProjectFileExport = new ProjectFileExport();
            mMyVaultFileExport = new MyVaultFileExport();
            mShareWithMeFileExport = new ShareWithMeFileExport();
            mWorkSpaceFileExport = new WorkSpaceFileExport();
        }

        public void Export(NxlFileFingerPrint nxlFileFingerPrint, Window owner)
        {
            string destPath = string.Empty;
            ViewerApp viewerApp = (ViewerApp)Application.Current;
            try
            {
                destPath = InputDestinationPath(nxlFileFingerPrint, owner);

                if (string.IsNullOrEmpty(destPath))
                {
                    return;
                }

                FunctionProvider functionProvider = viewerApp.FunctionProvider;

                if (nxlFileFingerPrint.isFromSystemBucket)
                {
                    WorkSpaceFile workSpaceFile = functionProvider.QueryWorkSpacetFileByDuid(nxlFileFingerPrint.duid);
                    if (null != workSpaceFile)
                    {
                        mWorkSpaceFileExport.Export(viewerApp.Session, workSpaceFile.RmsPathId, destPath);
                        NotifyMsg(true, nxlFileFingerPrint.name, destPath);
                    }
                }
                else if (nxlFileFingerPrint.isFromPorject)
                {
                    // means from Porject
                    int projectId;
                    ProjectFile projectFile = functionProvider.QueryProjectFileByDuid(nxlFileFingerPrint.duid, out projectId);
                    if (null != projectFile)
                    {
                        mProjectFileExport.Export(viewerApp.Session, projectId, projectFile.Rms_path_id, destPath);
                        NotifyMsg(true, nxlFileFingerPrint.name, destPath);
                    }
                }
                else if(nxlFileFingerPrint.isFromMyVault)
                {
                    if (nxlFileFingerPrint.isOwner)
                    {
                        // meansfrom MyVault
                        MyVaultFile myVaultFile = functionProvider.QueryMyVaultFileByDuid(nxlFileFingerPrint.duid);
                        if (null != myVaultFile)
                        {
                            mMyVaultFileExport.Export(viewerApp.Session, myVaultFile.RmsPathId, destPath);
                            NotifyMsg(true, nxlFileFingerPrint.name, destPath);
                        }
                    }
                    else
                    {
                        // means from ShareWithMe
                        SharedWithMeFile sharedWithMeFile = functionProvider.QuerySharedWithMeFileByDuid(nxlFileFingerPrint.duid);
                        if (null != sharedWithMeFile)
                        {
                            mShareWithMeFileExport.Export(viewerApp.Session, sharedWithMeFile.Transaction_id, sharedWithMeFile.Transaction_code, destPath);
                            NotifyMsg(true, nxlFileFingerPrint.name, destPath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NotifyMsg(false, nxlFileFingerPrint.name, destPath);
                throw ex;
            }
        }



        private void NotifyMsg(bool succeeded, string nxlFileName ,string destinationPath)
        {
            if (succeeded)
            {
                
                MessageNotify.NotifyMsg(nxlFileName, CultureStringInfo.Exception_ExportFeature_Succeeded + destinationPath+".", EnumMsgNotifyType.PopupBubble, MsgNotifyOperation.SAVE_AS, EnumMsgNotifyResult.Succeed, EnumMsgNotifyIcon.Online);
            }
            else
            {
                
                MessageNotify.NotifyMsg(nxlFileName, CultureStringInfo.Exception_ExportFeature_Failed + destinationPath+".", EnumMsgNotifyType.PopupBubble, MsgNotifyOperation.SAVE_AS, EnumMsgNotifyResult.Failed, EnumMsgNotifyIcon.Online);
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
            dlg.Filter = "NextLabs Protected Documents (*.nxl)|*.nxl"; // Filter files by extension

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
