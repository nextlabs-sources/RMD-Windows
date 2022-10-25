using SkydrmLocal.rmc.common.component;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.helper;
using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.search;
using SkydrmLocal.rmc.ui;
using SkydrmLocal.rmc.ui.components.ValiditySpecify.model;
using SkydrmLocal.rmc.ui.pages.model;
using SkydrmLocal.rmc.ui.utils;
using SkydrmLocal.rmc.ui.windows.mainWindow.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.modifyRights
{
    public interface IModifyRights
    {
        bool IsFromLocalDrive();

        bool GetRights(out ProtectAndShareConfig config);

        void UploadToRms();
        // for Online file
        void DeleteNxlFile();
    }

    public class ModifyRightsFeature: IModifyRights
    {
        private SkydrmLocalApp App = SkydrmLocalApp.Singleton;

        private string nxlPath;
        private INxlFile nxlFile;
        private bool isDeleteNxlFile;
        private NxlFileFingerPrint FingerPrint { get; set; }

        public ModifyRightsFeature(string nxlFilePath, INxlFile nxlFile = null, bool isDeleteNxlFile = false)
        {
            this.nxlPath = nxlFilePath;
            this.nxlFile = nxlFile;
            this.isDeleteNxlFile = isDeleteNxlFile;
        }

        public bool IsFromLocalDrive()
        {
            bool result = true;
            // Rms file search
            ISearchFileInProject searchFileInProject = new SearchProjectFileByLocalPath();
            IProjectFile projectFile = searchFileInProject.SearchInRmsFiles(nxlPath);
            if (projectFile != null)
            {
                result = false;
            }
            return result;
        }

        public bool GetRights(out ProtectAndShareConfig config)
        {
            bool result = true;

            config = new ProtectAndShareConfig();
            // Todo: Get Rights, water & expiry info by invoking SDK api.

            try
            {
                FingerPrint = App.Rmsdk.User.GetNxlFileFingerPrint(nxlPath);

                if (!FingerPrint.hasAdminRights)
                {
                    App.ShowBalloonTip(CultureStringInfo.Common_Not_Authorized);
                    return false;
                }

                // The file is from project but why the "IsFromProjcet" return false???  -------- Need to check.File://Jewelry/header---06-39-40.txt.nxl
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
            filePathArray[0] = nxlPath;
            fileNameArray[0] = nxlFileName;
            // file operation
            FileOperation fileOperation = new FileOperation(filePathArray, FileOperation.ActionType.ModifyRights, fileNameArray);

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
            config.sProject = SkydrmLocalApp.Singleton.SystemProject;
            // Get project
            try
            {
                foreach (var item in SkydrmLocalApp.Singleton.MainWin.viewModel.projectRepo.GetProjects())
                {
                    if (item.ProjectId == config.ProjectId)
                    {
                        config.myProject = item.Raw;
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                App.Log.Error("ModifyRightsFeature: projectRepro GetAllData error ", e);
                result = false;
            }
            return result;
        }
        
        public void UploadToRms()
        {
            if ( !IsFromLocalDrive() && nxlFile != null)
            {
                //SetEditedStatus(nxlFile);

                // Don't upload to RMS，PM required
                //// add to uploading queue
                //UploadManagerEx.GetInstance().AddToWaitingQueue(nxlFile);

                //if (NetworkStatus.IsAvailable && SkydrmLocalApp.Singleton.User.StartUpload)
                //{
                //    UploadManagerEx.GetInstance().TryToUpload();
                //}

                // Db file will update "LastModified time" by this flag after refresh.
                nxlFile.IsModifiedRights = true;

                App.MainWin.viewModel.DoRefresh();   
            }
        }

        public void DeleteNxlFile()
        {
            if (isDeleteNxlFile)
            {
                FileHelper.Delete_NoThrow(nxlPath);
            }
        }
    }
}
