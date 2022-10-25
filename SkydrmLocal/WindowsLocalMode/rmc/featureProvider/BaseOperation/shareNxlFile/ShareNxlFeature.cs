using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.drive;
using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.fileSystem;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.search;
using SkydrmLocal.rmc.ui;
using SkydrmLocal.rmc.ui.components.ValiditySpecify.model;
using SkydrmLocal.rmc.ui.pages.model;
using SkydrmLocal.rmc.ui.utils;
using SkydrmLocal.rmc.ui.windows.mainWindow.model;
using System;
using System.Collections.Generic;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SkydrmLocal.rmc.shareNxlFeature.ShareNxlFeature;

namespace SkydrmLocal.rmc.shareNxlFeature
{
    public interface IShareNxlFeature
    {
        /// <summary>
        /// .nxl file do share (not contain updateRecipient) or add file to project, need decrypt 
        /// </summary>
        /// <param name="decryptPath"></param>
        /// <returns></returns>
        bool IsDecrypt(out string decryptPath);

        /// <summary>
        /// Build config for window operation
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        bool BuildConfig(out ProtectAndShareConfig config);

        /// <summary>
        /// Delete original .nxl file
        /// </summary>
        void DeleteNxlFile();

        /// <summary>
        /// Delete decrypt file in RPM
        /// </summary>
        void DeleteRPM_File();

        /// <summary>
        /// Get operation in project do share
        /// </summary>
        /// <returns></returns>
        ShareNxlAction GetProjectShareAction();

        /// <summary>
        /// Get original .nxl file loaclPath
        /// </summary>
        /// <returns></returns>
        string GetSourceNxlLocalPath();
    }

    public class ShareNxlFeature: IShareNxlFeature
    {
        private SkydrmLocalApp App = SkydrmLocalApp.Singleton;

        private log4net.ILog Log = SkydrmLocalApp.Singleton.Log;

        private NxlFileFingerPrint FingerPrint { get; set; }

        private string nxlLocalPath;
        private ShareNxlAction action;

        private bool isDeleteNxlFile;
        private string RPM_FilePath = "";

        public ShareNxlFeature(ShareNxlAction action, string nxlLocalPath, bool deleteNxlFile = false)
        {
            this.nxlLocalPath = nxlLocalPath;
            this.action = action;
            this.isDeleteNxlFile = deleteNxlFile;
        }

        public bool IsDecrypt(out string decryptPath)
        {
            bool result = false;
            decryptPath = "";
          
            try
            {

                 decryptPath = RightsManagementService.GenerateDecryptFilePath(App.User.RPMFolder, nxlLocalPath, DecryptIntent.Share);
                 RightsManagementService.DecryptNXLFile(App, nxlLocalPath, decryptPath);

                if (File.Exists(decryptPath))
                {
                    RPM_FilePath = decryptPath;
                    result = true;
                }       
            }
            catch (InsufficientRightsException e)
            {
                result = false;
                App.Log.Info(e.ToString());
                if (!string.IsNullOrEmpty(decryptPath))
                {
                    RightsManagementService.RPMDeleteDirectory(App, Path.GetDirectoryName(decryptPath));
                }
                App.ShowBalloonTip(CultureStringInfo.Common_System_Internal_Error);
            }
            catch (Exception ex)
            {
                result = false;
                App.Log.Info(ex.ToString());
                if (!string.IsNullOrEmpty(decryptPath))
                {
                    RightsManagementService.RPMDeleteDirectory(App, Path.GetDirectoryName(decryptPath));
                }
                App.ShowBalloonTip(CultureStringInfo.Common_System_Internal_Error);
            }
           
            return result;
        }

        public bool BuildConfig(out ProtectAndShareConfig config)
        {
            config = new ProtectAndShareConfig();
            // Todo: Get Rights, water & expiry info by invoking SDK api.

            try
            {
                FingerPrint = App.Rmsdk.User.GetNxlFileFingerPrint(nxlLocalPath);

                if (action == ShareNxlAction.Share)
                {
                    if (!FingerPrint.HasRight(FileRights.RIGHT_SHARE))
                    {
                        App.ShowBalloonTip(CultureStringInfo.Common_Not_Authorized);
                        return false;
                    }
                }
                if (action == ShareNxlAction.AddFileToProject)
                {
                    if (!FingerPrint.HasRight(FileRights.RIGHT_DECRYPT))
                    {
                        App.ShowBalloonTip(CultureStringInfo.Common_Not_Authorized);
                        return false;
                    }
                }
                // only from systemBucket or from project can do share. If is from MyVault should do updateRecipients.
                if (!FingerPrint.isFromMyVault && !FingerPrint.isFromPorject && !FingerPrint.isFromSystemBucket)
                {
                    App.ShowBalloonTip(CultureStringInfo.Common_Not_Authorized);
                    return false;
                }
            }
            catch (Exception e)
            {
                App.Log.Error(e.Message, e);
                App.ShowBalloonTip(e.Message);
                return false;
            }

            var nxlFileName = FingerPrint.name;

            FileRights[] rights = FingerPrint.rights;
            Expiration expiration = FingerPrint.expiration;
            string waterMarkInfo = FingerPrint.adhocWatermark;

            string[] filePathArray = new string[1];
            string[] fileNameArray = new string[1];
            
            filePathArray[0] = "";// file path is decrypt file Path
            fileNameArray[0] = nxlFileName;
            // file operation
            FileOperation fileOperation;
            if (action == ShareNxlAction.Share)
            {
                if (FingerPrint.isFromMyVault)
                {
                    // If from MyVault should do updateRecipients. the file path is .nxl file path
                    filePathArray[0] = nxlLocalPath;
                    fileOperation = new FileOperation(filePathArray, FileOperation.ActionType.UpdateRecipients, fileNameArray);
                }
                else
                {
                    // When share nxlFile to person, is not UpdateRecipients. In fact, protect a file and share
                    fileOperation = new FileOperation(filePathArray, FileOperation.ActionType.Share, fileNameArray);
                }
            }
            else
            {
                // When share nxlFile to project. In fact, protect a file
                fileOperation = new FileOperation(filePathArray, FileOperation.ActionType.Protect, fileNameArray);
            }
            
            //get right list
            IList<string> list = FingerPrint.Helper_GetRightsStr();

            //get ExpireDateValue
            IExpiry Expiry;
            string expireDateValue = "";
            CommonUtils.SdkExpiration2ValiditySpecifyModel(expiration, out Expiry, out expireDateValue, false);

            // Now get config
            RightsSelectConfig rightsSelectConfig = new RightsSelectConfig();
            rightsSelectConfig.Expiry = Expiry;
            rightsSelectConfig.ExpireDateValue = expireDateValue;
            rightsSelectConfig.Watermarkvalue = waterMarkInfo;
            StringBuilder sb = new StringBuilder();
            CommonUtils.ConvertWatermark2DisplayStyle(waterMarkInfo, ref sb);
            rightsSelectConfig.DispalyWatermark = sb.ToString();
            rightsSelectConfig.Rights = list;

            config.FileOperation = fileOperation;
            config.RightsSelectConfig = rightsSelectConfig;
            config.Tags = FingerPrint.tags;
            config.IsAdHoc = FingerPrint.isByAdHoc;
            config.ProjectId = FingerPrint.projectId;

            return true;
        }

        public void DeleteNxlFile()
        {
            if (isDeleteNxlFile)
            {
                FileHelper.Delete_NoThrow(nxlLocalPath);
            }
        }

        public void DeleteRPM_File()
        {
            if (RPM_FilePath.Contains(App.User.RPMFolder))
            {
                RightsManagementService.RPMDeleteDirectory(App, Path.GetDirectoryName(RPM_FilePath));
            }
        }

        public ShareNxlAction GetProjectShareAction()
        {
            return action;
        }

        public string GetSourceNxlLocalPath()
        {
            return nxlLocalPath;
        }
        
        public enum ShareNxlAction
        {
            /// <summary>
            /// Share nxl file to person
            /// </summary>
            Share,
            /// <summary>
            /// Share nxl file to project
            /// </summary>
            AddFileToProject
        }

    }
}
