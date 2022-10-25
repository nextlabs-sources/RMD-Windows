using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.ui;
using SkydrmLocal.rmc.ui.windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SkydrmDesktop;
using SkydrmDesktop.Resources.languages;
using SkydrmLocal.rmc.search;
using System.Security.Cryptography;
using SkydrmLocal.rmc.drive;

namespace SkydrmLocal.rmc.common.helper
{
    public class FileHelper
    {
        private static readonly SkydrmApp App = SkydrmApp.Singleton;

        public static readonly Func<string, string, bool> IsDirectChild = (f, Parent) =>
        {
            // find direclt child, i.e path= /a/b
            // return /a/b/c.txt  /a/b/d/aaa/
            if (f.Length == 0)
            {
                return false;
            }
            if (f.Length <= Parent.Length)
            {
                return false;
            }
            if (!f.StartsWith(Parent, StringComparison.CurrentCultureIgnoreCase))
            {
                return false;
            }
            var idx = f.IndexOf('/', Parent.Length);

            if (idx == -1)
            {
                // a direct doc found
                return true;
            }
            if (idx == f.Length - 1)
            {
                // a direct foldr found
                return true;
            }
            return false;
        };

        public static bool Exist(string FullPath)
        {
            if (FullPath == null || FullPath.Length == 0)
            {
                return false;
            }
            return File.Exists(FullPath);
        }

        public static void Delete_NoThrow(string FullPath,bool autoDelNxlIfEpt=true)
        {
            // sanity check
            if (FullPath == null || FullPath.Length==0)
            {
                return;
            }
            if (!File.Exists(FullPath))
            {
                App.Log.Warn("File want to delete ,but not exist, " + FullPath);
                return;
            }
            try
            {
                File.Delete(FullPath);
            }
            catch(Exception e)
            {
                App.Log.Warn("File can not be deleted, " + FullPath + "\t Unexception: " + e,e);
                if (autoDelNxlIfEpt)
                {
                    DelNxlByOpenAndClose_NoThrow(FullPath);
                }
            }
            // should never reach here
        }

        // this is a workaround for RMSDK lock the file, when it locked, 
        // we can not delete it, but we open it and then close
        private static bool DelNxlByOpenAndClose_NoThrow(string FullPath)
        {
            if (FullPath == null || FullPath.Length == 0)
            {
                return false;
            }
            if (!FullPath.EndsWith(".nxl", StringComparison.CurrentCultureIgnoreCase))
            {
                return false;
            }
            try
            {
                App.Rmsdk.User.ForceCloseFile_NoThrow(FullPath);
                File.Delete(FullPath);
                return true;
            }
            catch (Exception e)
            {
                App.Log.Warn("Nxl File can not be deleted, " + FullPath + "\t Unexception: " + e, e);
            }
            return false;
        }


        public static bool RenameAsGarbage_NoThrow(string FullPath)
        {
            if (FullPath == null || FullPath.Length == 0)
            {
                return false;
            }
            try
            {
                // generate random garbage name
                var garbagepath = Path.Combine(GetParentPathWithoutTrailSlash_WorkAround(FullPath), 
                    "garbage_"+Guid.NewGuid().ToString());
                File.Move(FullPath, garbagepath);
                return true;                
            }
            catch (Exception e)
            {
                App.Log.Warn("File can not be renamed, " + FullPath+ "\t Unexception: " + e,e);
            }
            return false;
        }

        public static string GetParentPathWithoutTrailSlash_WorkAround(string path)
        {
            if(path==null)
            {
                path = "";
            }
            path=path.Replace(@"/", @"\");
            if (path.EndsWith(@"\"))
            {
                path = path.Substring(0, path.Length - 1);
            }
            try
            {
                var idx = path.LastIndexOf(@"\");
                if(idx != -1)
                {
                    return path.Substring(0, idx);
                }else
                {
                    //not found
                    throw new NotFoundException("can not find this path's parent");
                }
            }
            catch (Exception e)
            {
                App.Log.Warn("can not get the parent path" + path + "\t Unexception: " + e, e);
                throw;
            }
        }

        public static string RemovePathTrailSlash(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return "";
            }
            path = path.Replace(@"/", @"\");
            if (path.EndsWith(@"\"))
            {
                path = path.Substring(0, path.Length - 1);
            }
            return path;
        }

        public static void CreateDir_NoThrow(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            catch (Exception e)
            {
                App.Log.Warn("Exception in CreateDirectory,path=" + path, e);
            }
        }

        public static void CreateDir_NoThrow_For_LeaveCopy(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            catch (Exception e)
            {
                App.Log.Warn("Exception in CreateDirectory,path=" + path, e);
            }
        }

        // As folder owner in NTFS, we can remove the folder's list permission
        // isSet: true - protect, false - unprotect
        public static bool ProtectFolder(string folder,bool isSet)
        {
            bool rt = false;
            try
            {

                NTAccount curUser = (NTAccount)WindowsIdentity.GetCurrent().User.Translate(typeof(NTAccount));

                FileSystemAccessRule DenyListDir = new FileSystemAccessRule(curUser, 
                    FileSystemRights.ListDirectory, AccessControlType.Deny);

                DirectorySecurity dirSecureity = System.IO.Directory.GetAccessControl(folder);              

                if (isSet)
                {
                    dirSecureity.ResetAccessRule(DenyListDir);
                }
                else
                {
                    dirSecureity.RemoveAccessRule(DenyListDir);
                }

                System.IO.Directory.SetAccessControl(folder, dirSecureity);

                return true;
            }
            catch (Exception e)
            {
                App.Log.Error(e.Message, e);
            }
            return rt;
        }

        public static bool IsFileInUse(string filePath)
        {
            bool inUse = true;

            System.IO.FileStream fs = null;
            try
            {

                fs = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.None);

                inUse = false;
            }
            catch
            {
            }
            finally
            {
                if (fs != null)
                    fs.Close();
            }
            return inUse;
        }

        private static string GenerateSHA1(string txt)
        {
            using (SHA1 sha= SHA1.Create())
            {
                byte[] buffer = Encoding.Default.GetBytes(txt);
                byte[] newBuffer = sha.ComputeHash(buffer);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < newBuffer.Length; i++)
                {
                    sb.Append(newBuffer[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }
		
		// 
        // Get "leave copy" the temp folder by localPath.
        //
        // Like: "C:\Users\john.tyler @qapf1.qalab01.nextlabs.com\MyVault\nxltemp_d479fa10dc5dda35c133f29c3cc247e789932ab1\7776668888.txt.nxl" 
        public static string GetLeaveCopyTempFolder(string localPath)
        {
            if (string.IsNullOrEmpty(localPath))
            {
                return "";
            }

            // Get waiting uploading file tmp folder.
            // Like: "C:\Users\john.tyler @qapf1.qalab01.nextlabs.com\MyVault\nxltemp_d479fa10dc5dda35c133f29c3cc247e789932ab1"
            string tmpPath = Path.GetDirectoryName(localPath);

            // Get its the working folder.
            // Like: "C:\Users\john.tyler @qapf1.qalab01.nextlabs.com\MyVault"
            tmpPath = Path.GetDirectoryName(tmpPath);

            string folderName = "LeaveCopyTemp_" + GenerateSHA1(tmpPath.ToLower());
            string tmpFolder = tmpPath + @"\" + folderName;

            CreateDir_NoThrow_For_LeaveCopy(tmpFolder);

            return tmpFolder;
        }

        // Get "leave copy" the temp folder by cache folder.
        public static string GetLeaveCopyTempFolderEx(string cacheFolder)
        {
            cacheFolder = RemovePathTrailSlash(cacheFolder);
            if (string.IsNullOrEmpty(cacheFolder))
            {
                return "";
            }

            // Note: Will convert 'cacheFolder' string to lower in order to keep the same results(SHA1 value) with one that created by localPath. 
            string folderName = "LeaveCopyTemp_" + GenerateSHA1(cacheFolder.ToLower());
            string tmpFolder = cacheFolder + @"\" + folderName;

            CreateDir_NoThrow_For_LeaveCopy(tmpFolder);
            return tmpFolder;
        }

        /// <summary>
        /// Create nxl temp folder, return temp folder path
        /// </summary>
        /// <param name="workingPath"></param>
        /// <param name="parentFolder"></param>
        /// <returns></returns>
        private static string CreateNxlTempFolder(string workingPath, string parentFolder)
        {
            parentFolder = parentFolder.Replace(@"/", @"\");
            if (!parentFolder.StartsWith(@"\"))
            {
                parentFolder = "\\" + parentFolder;
            }
            if (!parentFolder.EndsWith(@"\"))
            {
                parentFolder = parentFolder+ "\\";
            }
            string destPath = FileHelper.GetParentPathWithoutTrailSlash_WorkAround(workingPath + parentFolder + "temp.txt");

            string tempFolder = "nxltemp_" + GenerateSHA1(destPath);
            string tempPath = Path.Combine(destPath, tempFolder);
            CreateDir_NoThrow(tempPath);

            return tempPath;
        }

        /// <summary>
        /// Create nxl temp folder and return remove timestamp nxl file temp path
        /// </summary>
        /// <param name="workingPath"></param>
        /// <param name="parentFolder"></param>
        /// <param name="rmsdkNxlFilePath"></param>
        /// <returns></returns>
        public static string CreateNxlTempPath(string workingPath, string parentFolder,
            string rmsdkNxlFilePath, bool removeTimeStamp= true)
        {
            string destFilePath = string.Empty;

            string tempPath = CreateNxlTempFolder(workingPath, parentFolder);
            string name = Path.GetFileName(rmsdkNxlFilePath);
            string tempFilePath = Path.Combine(tempPath, name);

            if (removeTimeStamp)
            {
                StringHelper.MatchFirstStrReplace(tempFilePath, out destFilePath, StringHelper.TIMESTAMP_PATTERN);
            }
            else
            {
                destFilePath = tempFilePath;
            }
            
            return destFilePath;
        }

        /// <summary>
        /// Move file to dest path and show dialog  
        /// </summary>
        /// <param name="destFilePath"></param>
        /// <param name="sourceFilePath"></param>
        /// <param name="overWriteUpload"></param>
        /// <param name="fileExistAction"></param>
        /// <param name="isDeleteSourceFile"></param>
        /// <returns></returns>
        public static string HandleAddedFile(string destFilePath,
            string sourceFilePath, out bool overWriteUpload, Func<string, bool> fileExistAction, Func<string, bool> fileCanOverWriteAction,
            bool isDeleteSourceFile=true, bool isRenameProtect = true)
        {
            string destFileName = Path.GetFileName(destFilePath);
            // set defult value
            string finalFilePath = sourceFilePath;
            overWriteUpload = false;

            // file not exist
            if (!fileExistAction(destFileName)) 
            {
                File.Copy(sourceFilePath, destFilePath, true);
                if (isDeleteSourceFile)
                {
                    FileHelper.Delete_NoThrow(sourceFilePath);
                }
                finalFilePath = destFilePath;
                return finalFilePath;
            }

            // file exist, create a unique name
            string directory = Path.GetDirectoryName(destFilePath); // nxltemp folder path
            string firstExt = Path.GetExtension(destFileName); // return first extension

            string orgFileName = destFileName;
            string secExt = string.Empty;
            int lastIndex = destFileName.LastIndexOf('.');
            if (lastIndex != -1)
            {
                orgFileName = destFileName.Substring(0, lastIndex); // remove first extension
                secExt = Path.GetExtension(orgFileName); // get second extension .txt ... or null, empty
            }
            string orgFileNameNoExt = Path.GetFileNameWithoutExtension(orgFileName);

            int count = 1;
            string newFileName = string.Format("{0}({1}){2}{3}", orgFileNameNoExt, count, secExt, firstExt);
            string newtempNxlPath = Path.Combine(directory, newFileName);

            while (fileExistAction(newFileName))
            {
                count++;
                newFileName = string.Format("{0}({1}){2}{3}", orgFileNameNoExt, count, secExt, firstExt);
                newtempNxlPath = Path.Combine(directory, newFileName);
            }

            if (App.User.ApplyAllSelectedOption && App.User.SelectedOption > 0) // Apply All items
            {
                if (App.User.SelectedOption == 1) // overwrite
                {
                    if (fileCanOverWriteAction(destFileName))
                    {
                        File.Copy(sourceFilePath, destFilePath, true);
                        if (isDeleteSourceFile)
                        {
                            FileHelper.Delete_NoThrow(sourceFilePath);
                        }
                        finalFilePath = destFilePath;
                    }
                    else
                    {
                        if (isDeleteSourceFile)
                        {
                            FileHelper.Delete_NoThrow(sourceFilePath);
                        }

                        throw new Exception(CultureStringInfo.ApplicationFindResource("Notify_RecordLog_Protect_Overwrite"));
                    }
                }
                else if (App.User.SelectedOption == 2) // rename
                {
                    if (isRenameProtect)
                    {
                        string sdkFolder = Path.GetDirectoryName(sourceFilePath);
                        string nxlNameWithouTime = Path.GetFileName(newtempNxlPath);
                        string sdkNxlFilePath = Path.Combine(sdkFolder, newFileName);
                        // rename to withoutTimeStamp nxl file
                        File.Move(sourceFilePath, sdkNxlFilePath, MoveOptions.ReplaceExisting);
                        // decrypt
                        string decryptPath = RightsManagementService.GenerateDecryptFilePath(App.User.RPMFolder, sdkNxlFilePath, DecryptIntent.ExtractContent);
                        RightsManagementService.DecryptNXLFile(App, sdkNxlFilePath, decryptPath);

                        if (File.Exists(decryptPath))
                        {
                            string tempPath = Path.Combine(Path.GetTempPath(), Path.GetFileName(decryptPath));

                            File.Copy(decryptPath, tempPath, true);
                            RightsManagementService.RPMDeleteDirectory(App, Path.GetDirectoryName(decryptPath));

                            App.Rmsdk.User.ProtectFileFrom(tempPath, sdkNxlFilePath, out string outPutNxlPath);

                            //fix bug rename failed on APP Stream environment
                            //File.Move(outPutNxlPath, newtempNxlPath, MoveOptions.ReplaceExisting);
                            File.Copy(outPutNxlPath, newtempNxlPath,true);
                            FileHelper.Delete_NoThrow(outPutNxlPath);

                            FileHelper.Delete_NoThrow(tempPath);
                        }
                        else
                        {
                            if (isDeleteSourceFile)
                            {
                                FileHelper.Delete_NoThrow(sdkNxlFilePath);
                            }
                            throw new Exception(CultureStringInfo.ApplicationFindResource("Notify_RecordLog_Protect_Rename"));
                        }

                        if (isDeleteSourceFile)
                        {
                            FileHelper.Delete_NoThrow(sdkNxlFilePath);
                        }
                        finalFilePath = newtempNxlPath;
                    }
                    else// use for myDrive rename
                    {
                        File.Copy(sourceFilePath, newtempNxlPath, true);
                        if (isDeleteSourceFile)
                        {
                            FileHelper.Delete_NoThrow(sourceFilePath);
                        }
                        finalFilePath = newtempNxlPath;
                    }

                }
                else if (App.User.SelectedOption == 3) // cancel
                {
                    if (isDeleteSourceFile)
                    {
                        FileHelper.Delete_NoThrow(sourceFilePath);
                    }

                    throw new Exception(CultureStringInfo.ApplicationFindResource("Notify_RecordLog_Protect_Cancel"));
                }

            }
            else
            {
                // show rename, overwrite dialog
                App.Dispatcher.Invoke(() =>
                {
                    var result = FileHelper.ShowReplaceDlg(destFileName, newFileName, out bool applyAllitem);
                    if (result == ui.windows.CustomMessageBoxWindow.CustomMessageBoxResult.Positive) // overwrite
                    {
                        // update db
                        App.User.ApplyAllSelectedOption = applyAllitem;
                        App.User.SelectedOption = 1;

                        if (fileCanOverWriteAction(destFileName))
                        {
                            File.Copy(sourceFilePath, destFilePath, true);
                            if (isDeleteSourceFile)
                            {
                                FileHelper.Delete_NoThrow(sourceFilePath);
                            }
                            finalFilePath = destFilePath;
                        }
                        else
                        {
                            if (isDeleteSourceFile)
                            {
                                FileHelper.Delete_NoThrow(sourceFilePath);
                            }

                            throw new Exception(CultureStringInfo.ApplicationFindResource("Notify_RecordLog_Protect_Overwrite"));
                        }
                        
                    }
                    else if (result == ui.windows.CustomMessageBoxWindow.CustomMessageBoxResult.Neutral) // rename
                    {
                        // update db
                        App.User.ApplyAllSelectedOption = applyAllitem;
                        App.User.SelectedOption = 2;

                        if (isRenameProtect)
                        {
                            string sdkFolder = Path.GetDirectoryName(sourceFilePath);
                            string nxlNameWithouTime = Path.GetFileName(newtempNxlPath);
                            string sdkNxlFilePath = Path.Combine(sdkFolder, newFileName);

                            App.Log.Info($"Move {sourceFilePath} to {sdkNxlFilePath}");
                            // rename to withoutTimeStamp nxl file
                            File.Move(sourceFilePath, sdkNxlFilePath, MoveOptions.ReplaceExisting);
                            App.Log.Info("Move successfully");

                            // decrypt
                            App.Log.Info("RPM Create decrypt path");
                            string decryptPath = RightsManagementService.GenerateDecryptFilePath(App.User.RPMFolder, sdkNxlFilePath, DecryptIntent.ExtractContent);

                            App.Log.Info($"RPM Decrypt nxl file to {decryptPath}");
                            RightsManagementService.DecryptNXLFile(App, sdkNxlFilePath, decryptPath);
                            App.Log.Info("RPM Decrypt nxl file successfuly");

                            if (File.Exists(decryptPath))
                            {
                                string tempPath = Path.Combine(Path.GetTempPath(), Path.GetFileName(decryptPath));

                                App.Log.Info($"Copy decrypt path {decryptPath} to temp path {tempPath}");
                                File.Copy(decryptPath, tempPath, true);

                                App.Log.Info($"RPM delete directory {Path.GetDirectoryName(decryptPath)}");
                                RightsManagementService.RPMDeleteDirectory(App, Path.GetDirectoryName(decryptPath));

                                App.Log.Info($"Protect file {tempPath} from original nxl file {sdkNxlFilePath}");
                                App.Rmsdk.User.ProtectFileFrom(tempPath, sdkNxlFilePath, out string outPutNxlPath);
                                App.Log.Info($"Protect file successfully {outPutNxlPath}");

                                App.Log.Info($"Copy nxl file {outPutNxlPath} to new temp nxl path {newtempNxlPath}");
                                //File.Move(outPutNxlPath, newtempNxlPath, MoveOptions.ReplaceExisting);
                                File.Copy(outPutNxlPath, newtempNxlPath, true);
                                App.Log.Info("Copy successfully.");
                                App.Log.Info($"Delete nxl file {outPutNxlPath}");
                                FileHelper.Delete_NoThrow(outPutNxlPath);

                                App.Log.Info($"Delete temp path {tempPath}");
                                FileHelper.Delete_NoThrow(tempPath);
                            }
                            else
                            {
                                if (isDeleteSourceFile)
                                {
                                    FileHelper.Delete_NoThrow(sdkNxlFilePath);
                                }
                                throw new Exception(CultureStringInfo.ApplicationFindResource("Notify_RecordLog_Protect_Rename"));
                            }

                            if (isDeleteSourceFile)
                            {
                                App.Log.Info($"Delete sdk nxl file {sdkNxlFilePath}");
                                FileHelper.Delete_NoThrow(sdkNxlFilePath);
                            }
                            finalFilePath = newtempNxlPath;
                        }
                        else // use for myDrive rename
                        {
                            File.Copy(sourceFilePath, newtempNxlPath, true);
                            if (isDeleteSourceFile)
                            {
                                FileHelper.Delete_NoThrow(sourceFilePath);
                            }
                            finalFilePath = newtempNxlPath;
                        }
                    }
                    else // cancel
                    {
                        // update db
                        App.User.ApplyAllSelectedOption = applyAllitem;
                        App.User.SelectedOption = 3;
                        if (isDeleteSourceFile)
                        {
                            FileHelper.Delete_NoThrow(sourceFilePath);
                        }

                        throw new Exception(CultureStringInfo.ApplicationFindResource("Notify_RecordLog_Protect_Cancel"));
                    }
                });
            }

            // update db
            if (App.User.SelectedOption == 1)
            {
                overWriteUpload = true;
            }
            return finalFilePath;
        }


        public static CustomMessageBoxWindow.CustomMessageBoxResult ShowReplaceDlg(string fileName, string suggestName,
            out bool isApplyAll)
        {
            string subject = "The destination already has a file named \"" + fileName + "\". "+ "You can choose replace or keep both files.";
            string detial = "Replace: " + "Replace the file in the destination folder." + "\n"
                + "Keep both: " + "Keep both files and the new protected file will be named to \"" + suggestName + "\". " + "\n";

            CustomMessageBoxWindow.CustomMessageBoxResult ret = CustomMessageBoxWindow.Show(out isApplyAll,
                CultureStringInfo.ReplaceFile_DlgBox_Title,
                subject,
                detial,
                CustomMessageBoxWindow.CustomMessageBoxIcon.Warning,
                CustomMessageBoxWindow.CustomMessageBoxButton.BTN_OVERWRITE,
                CustomMessageBoxWindow.CustomMessageBoxButton.BTN_CANCEL,
                "Keep both"
            );

            return ret;
        }

        public static CustomMessageBoxWindow.CustomMessageBoxResult ShowReplaceDlg(string fileName, string suggestName)
        {
            string subject = "The destination already has a file named \"" + fileName + "\". " + "You can choose replace or keep both files.";
            string detial = "Replace: " + "Replace the file in the destination folder." + "\n"
                + "Keep both: " + "Keep both files and the new protected file will be named to \"" + suggestName + "\". " + "\n";

            CustomMessageBoxWindow.CustomMessageBoxResult ret = CustomMessageBoxWindow.Show(
                CultureStringInfo.ReplaceFile_DlgBox_Title,
                subject,
                detial,
                CustomMessageBoxWindow.CustomMessageBoxIcon.Warning,
                CustomMessageBoxWindow.CustomMessageBoxButton.BTN_OVERWRITE,
                CustomMessageBoxWindow.CustomMessageBoxButton.BTN_CANCEL,
                "Keep both"
            );

            return ret;
        }
    }
}
