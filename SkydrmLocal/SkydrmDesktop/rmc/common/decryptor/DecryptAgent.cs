using Newtonsoft.Json;
using SkydrmLocal.rmc.app.process;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.drive;
using SkydrmLocal.rmc.exception;
using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.featureProvider.common;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.fileSystem.external;
using SkydrmLocal.rmc.process;
using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.search;
using SkydrmLocal.rmc.ui;
using SkydrmLocal.rmc.ui.utils;
using SkydrmLocal.rmc.ui.windows.mainWindow.model;
using System;
using Alphaleonis.Win32.Filesystem;
using System.Text.RegularExpressions;
using SkydrmDesktop;
using SkydrmDesktop.Resources.languages;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace SkydrmLocal.rmc.common.decryptor
{
    public class DecryptAgent
    {
        #region logic
        private readonly SkydrmApp skydrmLocalApp = SkydrmApp.Singleton;
  
        #endregion

        public DecryptAgent(){ }

        public void Decrypt(INxlFile nxlFile, NxlConvertCompleteDelegate callback)
        {
            NxlConverterResult.Builder builder = new NxlConverterResult.Builder();          
            builder = BuildNxlConverterResult(builder, nxlFile);
            callback?.Invoke(builder.Build());
        }

        public void Decrypt(string nxlFilePath, NxlConvertCompleteDelegate callback)
        {
            NxlConverterResult.Builder builder = new NxlConverterResult.Builder();
            builder = BuildNxlConverterResult(builder, nxlFilePath);
            callback?.Invoke(builder.Build());          
        }

        private NxlConverterResult.Builder BuildNxlConverterResult(
            NxlConverterResult.Builder builder,
            string nxlFilePath)
        {        
            builder.IsConverterSucceed(false);

            try
            {
                RightsManagementService.CheckRPMDriverExist(skydrmLocalApp.Rmsdk);
                NxlFileFingerPrint fp = skydrmLocalApp.Rmsdk.User.GetNxlFileFingerPrint(nxlFilePath);
                // for every thing is ok, user try to view this file, send log to rms
                skydrmLocalApp.User.AddNxlFileLog(nxlFilePath, NxlOpLog.View, fp.HasRight(FileRights.RIGHT_VIEW));

                Int64 modifiedtime = fp.modified;
                string strModified = "UnKnown";
                if (modifiedtime > 0)
                {
                    strModified = DateTimeHelper.TimestampToDateTime2(modifiedtime);
                }

                builder.SetAdhocWaterMark(fp.adhocWatermark)
                       .SetDateModified(strModified)
                       .SetExpiration(fp.expiration)
                       .SetFileName(fp.name)
                       .SetTags(JsonConvert.SerializeObject(fp.tags))
                       .SetEnumFileRepo(EnumFileRepo.EXTERN)
                       .SetRights(fp.rights)
                       .SetSharedWith(new string[0])
                       .SetSize(fp.size)
                       .SetLocalDiskPath(fp.localPath)
                       .SetUserEmail(skydrmLocalApp.User.Email)
                       .SetOwner(fp.isOwner)
                       .SetIsAdHoc(fp.isByAdHoc)
                       .SetIsCentrolPolicy(fp.isByCentrolPolicy);

                bool displayPrintButton = IsDisplayPrintButton(fp);

                builder.IsDecryptFromRPM(true)
                        .IsDisplayPrintButton(displayPrintButton)
                        //as PM required, disable share icon in viewer temprorily
                        //.IsDisplayShareButton(IsDisplayShareButton(fp.rights, EnumFileRepo.EXTERN, EnumNxlFileStatus.UnknownError))
                        .IsDisplayShareButton(IsDisplayShareButton(fp,EnumNxlFileStatus.UnknownError,EnumFileRepo.EXTERN))
                        .IsDisplayEditButton(SkydrmApp.Singleton.ExternalMgr.IsNxlFileCanEdit(fp))
                        .IsDisplaySaveAsButton(false)
                        .IsDisplayExtractButton(IsDisplayExtractButton(fp,EnumNxlFileStatus.UnknownError, EnumFileRepo.EXTERN));

                if (displayPrintButton)
                {
                    string forPrintFilePath = RightsManagementService.GenerateDecryptFilePath(skydrmLocalApp.User.RPMFolder, fp.localPath , DecryptIntent.Print);
                    builder.SetForPrintFilePath(forPrintFilePath);
                    RightsManagementService.AsynDecryptNXLFile(skydrmLocalApp, fp.localPath, forPrintFilePath);
                    PrintProcess.Start();
                }

                string decryptedFilePath = RightsManagementService.GenerateDecryptFilePath(skydrmLocalApp.User.RPMFolder, fp.localPath, DecryptIntent.View);
                RightsManagementService.DecryptNXLFile(skydrmLocalApp, fp.localPath, decryptedFilePath);
                builder.SetTmpPath(decryptedFilePath)
                       .IsConverterSucceed(true);
            }            
            catch (RmSdkException e)
            {
                skydrmLocalApp.Log.Info(e.ToString());
                // Fix bug 54938,The error message inconsistent
                //skydrmLocalApp.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Common_Not_Authorized"));

                // send deny log to rms
                skydrmLocalApp.User.AddNxlFileLog(nxlFilePath, NxlOpLog.View, false);

                string name = Path.GetFileName(nxlFilePath);
                builder.SetUserEmail(skydrmLocalApp.Rmsdk.User.Email)
                       .SetFileName(name)
                       .SetErrorMsg(e.Message)
                       .SetLocalDiskPath(nxlFilePath)
                       .IsConverterSucceed(false);
            }
            catch (Exception e)
            {
                skydrmLocalApp.Log.Info(e.ToString());
                // Fix bug 54938,The error message inconsistent
                //skydrmLocalApp.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Common_Not_Authorized"));

                string name = Path.GetFileName(nxlFilePath);
                builder.SetUserEmail(skydrmLocalApp.Rmsdk.User.Email)
                       .SetFileName(name)
                       .SetErrorMsg(CultureStringInfo.ApplicationFindResource("Common_System_Internal_Error"))
                       .SetLocalDiskPath(nxlFilePath)
                       .IsConverterSucceed(false);
            }
            finally
            {
                // work around sdk bug, maually close  nxlFilePath                
                skydrmLocalApp.Rmsdk.User.ForceCloseFile_NoThrow(nxlFilePath);
            }

            return builder;
        }

    

        private NxlConverterResult.Builder BuildNxlConverterResult(NxlConverterResult.Builder builder, INxlFile localNxlFile)
        {
            try
            {
                RightsManagementService.CheckRPMDriverExist(skydrmLocalApp.Rmsdk);
                SkydrmApp App = SkydrmApp.Singleton;
                // check file rights and prepare most fields of convert result 
                NxlFileFingerPrint fp = skydrmLocalApp.Rmsdk.User.GetNxlFileFingerPrint(localNxlFile.LocalPath);
                // for every thing is ok, user try to view this file, send log to rms
                skydrmLocalApp.User.AddNxlFileLog(localNxlFile.LocalPath, NxlOpLog.View, fp.HasRight(FileRights.RIGHT_VIEW));
                
                builder.IsConverterSucceed(false);

                // common info
                builder.SetUserEmail(App.Rmsdk.User.Email)
                       .SetFileName(localNxlFile.Name)
                       .SetSize(localNxlFile.Size)
                       .SetEnumFileRepo(localNxlFile.FileRepo)
                       .SetLocalDiskPath(localNxlFile.LocalPath);

                builder.SetTags(JsonConvert.SerializeObject(fp.tags))
                       .SetExpiration(fp.expiration)
                       .SetRights(fp.rights)
                       .SetAdhocWaterMark(fp.adhocWatermark)
                       .SetRmsRemotePath(localNxlFile.DisplayPath)
                       .IsCreatedLocal(localNxlFile.IsCreatedLocal)
                       .SetSharedWith(localNxlFile.SharedWith.ToArray())
                       .SetDateModified(localNxlFile.RawDateModified.ToLocalTime().ToString())
                       .SetProjectId(GetProjectId(localNxlFile))
                       .IsDecryptFromRPM(true)
                       .SetOwner(fp.isOwner)
                       .SetIsAdHoc(fp.isByAdHoc)
                       .SetIsCentrolPolicy(fp.isByCentrolPolicy);

                bool displayPrintButton = IsDisplayPrintButton(fp);

                // as PM required, file come MainWindow, disable edit
                builder.IsDisplayEditButton(IsDisplayEditButton(localNxlFile, fp))
                       .IsDisplayPrintButton(displayPrintButton)
                       .IsDisplayShareButton(IsDisplayShareButton(fp, localNxlFile.FileStatus, localNxlFile.FileRepo))
                       .IsDisplaySaveAsButton(IsDisplayExportButton(fp, localNxlFile.FileRepo, localNxlFile.FileStatus))
                       .IsDisplayExtractButton(IsDisplayExtractButton(fp, localNxlFile.FileStatus, localNxlFile.FileRepo));

                if (displayPrintButton)
                {
                    string forPrintFilePath = RightsManagementService.GenerateDecryptFilePath(skydrmLocalApp.User.RPMFolder, fp.localPath, DecryptIntent.Print);
                    builder.SetForPrintFilePath(forPrintFilePath);
                    RightsManagementService.AsynDecryptNXLFile(skydrmLocalApp, fp.localPath, forPrintFilePath);
                    PrintProcess.Start();
                }

                string decryptedFilePath = RightsManagementService.GenerateDecryptFilePath(skydrmLocalApp.User.RPMFolder, fp.localPath, DecryptIntent.View);
                RightsManagementService.DecryptNXLFile(App, localNxlFile.LocalPath, decryptedFilePath);
                builder.SetTmpPath(decryptedFilePath)
                       .IsConverterSucceed(true);
            }
            catch (RmSdkException e)
            {
                skydrmLocalApp.Log.Info(e.ToString());
                // Fix bug 54938,The error message inconsistent
                //skydrmLocalApp.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Common_Not_Authorized"));

                // send deny log to rms
                skydrmLocalApp.User.AddNxlFileLog(localNxlFile.LocalPath, NxlOpLog.View, false);

                // fix bug 53434 - No .nxl extension when open nxl file protected in system bucket
                string name = Path.GetFileName(localNxlFile.Name);
                builder.SetUserEmail(skydrmLocalApp.Rmsdk.User.Email)
                       .SetFileName(name)
                       .SetErrorMsg(e.Message)
                       .IsConverterSucceed(false);
            }
            catch (Exception e)
            {
                string name = Path.GetFileName(localNxlFile.Name);
                builder.SetUserEmail(skydrmLocalApp.Rmsdk.User.Email)
                       .SetFileName(name)
                       .SetErrorMsg(CultureStringInfo.ApplicationFindResource("Common_System_Internal_Error"))
                       .IsConverterSucceed(false);

                skydrmLocalApp.Log.Info(e.ToString());

                // Fix bug 54938,The error message inconsistent
                //skydrmLocalApp.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Common_Not_Authorized"));
            }
            finally
            {
                // work around sdk bug, maually close  nxlFilePath                
                skydrmLocalApp.Rmsdk.User.ForceCloseFile_NoThrow(localNxlFile.Name);
            }
            return builder;
        }

        private bool IsDisplayShareButton(NxlFileFingerPrint fp , EnumNxlFileStatus FileStatus, EnumFileRepo EnumFileRepo)
        {
            bool result = false;

            bool isNetwork = skydrmLocalApp.MainWin.viewModel.IsNetworkAvailable;
            // Fix bug 53840
            if (fp.isFromMyVault && !isNetwork)
            {
                result = false;
                return result;
            }
            if (!fp.isByCentrolPolicy && fp.HasRight(FileRights.RIGHT_SHARE))
            {
                if (EnumFileRepo == EnumFileRepo.EXTERN)
                {
                    result = true;
                }
                else
                {
                    switch (FileStatus)
                    {
                        case EnumNxlFileStatus.AvailableOffline:
                        case EnumNxlFileStatus.CachedFile:
                        case EnumNxlFileStatus.Online:
                        case EnumNxlFileStatus.DownLoadedSucceed:
                            result = true;
                            break;
                    }
                }
            }

            skydrmLocalApp.Log.InfoFormat("IsDisplayShareButton:{0}", result);
            return result;
        }

        public bool IsDisplayEditButton(INxlFile localNxlFile, NxlFileFingerPrint fp)
        {
            bool result = false;

            switch(localNxlFile.FileRepo)
            {
                case EnumFileRepo.REPO_PROJECT:

                    if (localNxlFile.FileStatus == EnumNxlFileStatus.AvailableOffline
                        || localNxlFile.FileStatus == EnumNxlFileStatus.CachedFile
                        || localNxlFile.IsEdit)
                    {
                        result = SkydrmApp.Singleton.ExternalMgr.IsNxlFileCanEdit(fp);
                    }
                    break;

                default:
                    break;
            }

            return result;
        }

        private bool IsDisplayExtractButton(NxlFileFingerPrint fp, EnumNxlFileStatus FileStatus, EnumFileRepo EnumFileRepo)
        {
            bool result = false;
            if(fp.HasRight(FileRights.RIGHT_DECRYPT) && !fp.isFromMyVault)
            {
                if (EnumFileRepo == EnumFileRepo.EXTERN)
                {
                    result = true;
                }
                else
                {
                    switch (FileStatus)
                    {
                        case EnumNxlFileStatus.AvailableOffline:
                        case EnumNxlFileStatus.CachedFile:
                        case EnumNxlFileStatus.Online:
                        case EnumNxlFileStatus.WaitingUpload:
                        case EnumNxlFileStatus.DownLoadedSucceed:

                            result = true;
                            break;
                    }
                }
            }
            return result;
         }

        private bool IsDisplayPrintButton(NxlFileFingerPrint fp)
        {
            bool result = false;
            if (fp.HasRight(FileRights.RIGHT_PRINT))
            {
                string oriFileName = Path.GetFileNameWithoutExtension(fp.name);
                if (!string.Equals(Path.GetExtension(oriFileName), ".gif", StringComparison.CurrentCultureIgnoreCase) &&
                    !string.Equals(Path.GetExtension(oriFileName), ".vds", StringComparison.CurrentCultureIgnoreCase))
                {
                    result = true;
                }
            }
            return result;
        }

        private bool IsDisplayExportButton(NxlFileFingerPrint fp , EnumFileRepo FileRepo, EnumNxlFileStatus enumNxlFileStatus)
        {
            bool result = false;

            bool isNetwork = skydrmLocalApp.MainWin.viewModel.IsNetworkAvailable;

            if (isNetwork)
            {
                switch (FileRepo)
                {
                    case EnumFileRepo.REPO_PROJECT:
                    case EnumFileRepo.REPO_MYVAULT:
                    case EnumFileRepo.REPO_SHARED_WITH_ME:
                        if (fp.HasRight(FileRights.RIGHT_DOWNLOAD) || fp.HasRight(FileRights.RIGHT_SAVEAS))
                        {
                                switch (enumNxlFileStatus)
                                {
                                    case EnumNxlFileStatus.AvailableOffline:
                                    case EnumNxlFileStatus.CachedFile:
                                    case EnumNxlFileStatus.Online:
                                    case EnumNxlFileStatus.DownLoadedSucceed:

                                        result = true;
                                        break;

                                    default:
                                        break;
                                }
                        }
                        break;
                }
            }
            skydrmLocalApp.Log.InfoFormat("IsDisplayExportButton:{0}", result);
            return result;
        }

        private Int32 GetProjectId(INxlFile localNxlFile)
        {
            Int32 result = -1;
            switch (localNxlFile.FileRepo)
            {
                case EnumFileRepo.REPO_PROJECT:
                    result = int.Parse(localNxlFile.RepoId);
                    break;
            }
            return result;
        }

        struct FileInfoExt
        {
            public string createdBy;
            public long dateCreated;
            public long dateModifed;
            public string extension;
            public string filename;
            public string modifiedBy;
        }

        private static FileInfoExt ParseFileInfoExt(string strJson)
        {
            FileInfoExt ret = new FileInfoExt()
            {
                createdBy = "",
                dateCreated = 0,
                dateModifed = 0,
                extension = "",
                filename = "",
                modifiedBy = ""
            };
            try
            {
                JObject jo = (JObject)JsonConvert.DeserializeObject(strJson);
                if (jo.ContainsKey("createdBy"))
                {
                    ret.createdBy = jo.GetValue("createdBy").ToString();
                }
                if (jo.ContainsKey("dateModified"))
                {
                    ret.dateModifed = (long)jo.GetValue("dateModified");
                }
                if (jo.ContainsKey("dateCreated"))
                {
                    ret.dateCreated = (long)jo.GetValue("dateCreated");
                }
                if (jo.ContainsKey("fileExtension"))
                {
                    ret.extension = jo.GetValue("fileExtension").ToString();
                }
                if (jo.ContainsKey("fileName"))
                {
                    ret.filename = jo.GetValue("fileName").ToString();
                }
                if (jo.ContainsKey("modifiedBy"))
                {
                    ret.modifiedBy = jo.GetValue("modifiedBy").ToString();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }

            return ret;
        }

        public static string ConvertNxlFileToJson(string nxlFilePath)
        {
            NxlConverterResult.Builder builder = new NxlConverterResult.Builder();
            builder.IsConverterSucceed(true);
            var skydrmLocalApp = SkydrmApp.Singleton;

            try
            {
                RightsManagementService.CheckRPMDriverExist(skydrmLocalApp.Rmsdk);

                // Will throw 403 error exception if have no any permission for a central policy nxl file.
                NxlFileFingerPrint fp = skydrmLocalApp.Rmsdk.User.GetNxlFileFingerPrint(nxlFilePath,true);

                Int64 modifiedtime = fp.modified;
                string strModified = "UnKnown";
                if (modifiedtime > 0)
                {
                    strModified = DateTimeHelper.TimestampToDateTime2(modifiedtime);
                }

                string watermarkStr = string.Empty;
                if (fp.isByCentrolPolicy)
                {
                    Dictionary<FileRights, List<WaterMarkInfo>> rightsAndWatermarks;

                    try
                    {
                        SkydrmApp.Singleton.Rmsdk.User.EvaulateNxlFileRights(nxlFilePath, out rightsAndWatermarks,true);
                        foreach (var v in rightsAndWatermarks)
                        {
                            List<WaterMarkInfo> waterMarkInfoList = v.Value;
                            if (waterMarkInfoList == null)
                            {
                                continue;
                            }
                            foreach (var w in waterMarkInfoList)
                            {
                                watermarkStr = w.text;
                                if (!string.IsNullOrEmpty(watermarkStr))
                                {
                                    break;
                                }
                            }
                            if (!string.IsNullOrEmpty(watermarkStr))
                            {
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }

                builder.SetAdhocWaterMark(fp.isByCentrolPolicy ? watermarkStr : fp.adhocWatermark)
                       .SetDateModified(strModified)
                       .SetExpiration(fp.expiration)
                       .SetFileName(fp.name)
                       .SetTags(JsonConvert.SerializeObject(fp.tags))
                       .SetEnumFileRepo(EnumFileRepo.EXTERN)
                       .SetRights(fp.rights)
                       .SetSharedWith(new string[0])
                       .SetSize(fp.size)
                       .SetLocalDiskPath(fp.localPath)
                       .SetUserEmail(skydrmLocalApp.User.Email)
                       .SetOwner(fp.isOwner)
                       .SetIsAdHoc(fp.isByAdHoc)
                       .SetIsCentrolPolicy(fp.isByCentrolPolicy);

                return JsonConvert.SerializeObject(builder.Build());

            }
            // error code 403: one case is that current user has no any permission for a central policy nxl file.
            // or error code 5: one case is that current user does not belong to the token group of the adhoc nxl file.
            catch (AccessForbiddenException e)
            {
                NxlFileInfo info = skydrmLocalApp.Rmsdk.RPMGetFileInfo(nxlFilePath);
                FileInfoExt fileInfoExt = ParseFileInfoExt(info.infoext);
                string strModified = DateTimeHelper.TimestampToDateTime2(fileInfoExt.dateModifed);
                FileInfo finfo = new FileInfo(nxlFilePath);

                // This is temporary fix bug 69814, actually should return if is central policy info by 'RPMGetFileInfo' interface,
                // which can distinguish by 'policy' field.
                bool isCentralPolicy = false;
                if (!string.Equals("{}", info.tags))
                {
                    isCentralPolicy = true;
                }

                builder.SetAdhocWaterMark(info.watermark.text)
                   .SetDateModified(strModified)
                   .SetExpiration(info.expiration)
                   .SetFileName(finfo.Name)
                   .SetTags(info.tags)
                   .SetEnumFileRepo(EnumFileRepo.EXTERN)
                   .SetRights(info.rights.ToArray())
                   .SetSharedWith(new string[0])
                   .SetSize(finfo.Length)
                   .SetLocalDiskPath("")
                   .SetUserEmail(skydrmLocalApp.User.Email)
                   .SetOwner(false)
                   .SetIsAdHoc(!isCentralPolicy)
                   .SetIsCentrolPolicy(isCentralPolicy);

                return JsonConvert.SerializeObject(builder.Build());
            }
            // Bug 70234 - Fail to view file info when RMD without DAP plugin configured
            catch (InsufficientRightsException e)
            {
                NxlFileInfo info = skydrmLocalApp.Rmsdk.RPMGetFileInfo(nxlFilePath);
                FileInfoExt fileInfoExt = ParseFileInfoExt(info.infoext);
                string strModified = DateTimeHelper.TimestampToDateTime2(fileInfoExt.dateModifed);
                FileInfo finfo = new FileInfo(nxlFilePath);

                // This is temporary fix bug 69814, actually should return if is central policy info by 'RPMGetFileInfo' interface,
                // which can distinguish by 'policy' field.
                bool isCentralPolicy = false;
                if (!string.Equals("{}", info.tags))
                {
                    isCentralPolicy = true;
                }

                builder.SetAdhocWaterMark(info.watermark.text)
                   .SetDateModified(strModified)
                   .SetExpiration(info.expiration)
                   .SetFileName(finfo.Name)
                   .SetTags(info.tags)
                   .SetEnumFileRepo(EnumFileRepo.EXTERN)
                   .SetRights(info.rights.ToArray())
                   .SetSharedWith(new string[0])
                   .SetSize(finfo.Length)
                   .SetLocalDiskPath("")
                   .SetUserEmail(skydrmLocalApp.User.Email)
                   .SetOwner(false)
                   .SetIsAdHoc(!isCentralPolicy)
                   .SetIsCentrolPolicy(isCentralPolicy);

                return JsonConvert.SerializeObject(builder.Build());
            }
           
        }
    }
}
