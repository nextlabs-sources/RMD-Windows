using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkydrmDesktop.Resources.languages;
using SkydrmDesktop.rmc.ui.windows.fileOperationWindow.model;
using SkydrmDesktop.rmc.ui.windows.mainWindow.model;
using SkydrmLocal.rmc.exception;
using SkydrmLocal.rmc.fileSystem;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.search;
using SkydrmLocal.rmc.ui.utils;

namespace SkydrmDesktop.rmc.ui.windows.fileOperationWindow.operation
{
    class ModifyNxlFileRight : IModifyNxlFileRight
    {
        private readonly SkydrmApp app = SkydrmApp.Singleton;

        private NxlFileFingerPrint fingerPrint;

        private OperateFileInfo fileInfo;
        private IList<IFileRepo> repos;
        private CurrentSelectedSavePath currentSelectedSavePath;

        /// <summary>
        /// Use for desktop plug-in cmd
        /// </summary>
        /// <param name="fingerP"></param>
        /// <param name="fileInfo"></param>
        /// <param name="repos"></param>
        public ModifyNxlFileRight(NxlFileFingerPrint fingerP, OperateFileInfo fileInfo, IList<IFileRepo> repos)
        {
            this.fingerPrint = fingerP;
            this.fileInfo = fileInfo;
            this.repos = repos;

            string path = Alphaleonis.Win32.Filesystem.Path.GetDirectoryName(fileInfo.FilePath[0]);
            this.currentSelectedSavePath = new CurrentSelectedSavePath(DataTypeConvertHelper.SYSTEMBUCKET, path, path, app.SystemProject.Id.ToString());
        }
        /// <summary>
        /// Use for main window file list cmd
        /// </summary>
        /// <param name="fingerP"></param>
        /// <param name="fileInfo"></param>
        /// <param name="repos"></param>
        /// <param name="selectedSavePath"></param>
        public ModifyNxlFileRight(NxlFileFingerPrint fingerP, OperateFileInfo fileInfo, IList<IFileRepo> repos, CurrentSelectedSavePath selectedSavePath)
        {
            this.fingerPrint = fingerP;
            this.fileInfo = fileInfo;
            this.repos = repos;

            this.currentSelectedSavePath = selectedSavePath;
        }

        public FileAction FileAction => FileAction.ModifyRights;

        public OperateFileInfo FileInfo { get => fileInfo; set => fileInfo = value; }

        public IList<IFileRepo> RepoList => repos;

        public int NxlRepoId => fingerPrint.projectId;

        public CurrentSelectedSavePath CurrentSelectedSavePath { get => currentSelectedSavePath; set => currentSelectedSavePath = value; }

        public NxlFileType NxlType => fingerPrint.isByAdHoc ? NxlFileType.Adhoc : NxlFileType.CentralPolicy;

        public List<FileRights> NxlRights
        {
            get
            {
                List<FileRights> fileRights = fingerPrint.rights.ToList();
                if (!string.IsNullOrWhiteSpace(fingerPrint.adhocWatermark) && !fileRights.Contains(FileRights.RIGHT_WATERMARK))
                {
                    fileRights.Add(FileRights.RIGHT_WATERMARK);
                }
                return fileRights;
            }
        }

        public WaterMarkInfo NxlAdhocWaterMark => new WaterMarkInfo() { text = fingerPrint.adhocWatermark, fontName = "", fontColor = "" };

        public Expiration NxlExpiration => fingerPrint.expiration;

        public Dictionary<string, List<string>> NxlTags => fingerPrint.tags;

        public List<FileRights> PreviewRightsByCentralPolicy(Dictionary<string, List<string>> selectedTags, out WaterMarkInfo warterMark)
        {
            List<FileRights> fileRights = new List<FileRights>();
            warterMark = new WaterMarkInfo();
            try
            {
                UserSelectTags tags = new UserSelectTags();
                foreach (var item in selectedTags)
                {
                    tags.AddTag(item.Key, item.Value);
                }
                // Inoke sdk api, get rights
                Dictionary<FileRights, List<WaterMarkInfo>> rightsAndWatermarks;
              
                app.Rmsdk.User.GetFileRightsFromCentalPolicyByProjectId(NxlRepoId, tags,
                    out rightsAndWatermarks);

                foreach (var item in rightsAndWatermarks.Keys)
                {
                    fileRights.Add(item);
                }

                foreach (var v in rightsAndWatermarks)
                {
                    List<WaterMarkInfo> waterMarkInfoList = v.Value;
                    if (waterMarkInfoList == null)
                    {
                        continue;
                    }
                    foreach (var w in waterMarkInfoList)
                    {
                        warterMark = w;
                        if (!string.IsNullOrEmpty(warterMark.text))
                        {
                            break;
                        }
                    }
                    if (!string.IsNullOrEmpty(warterMark.text))
                    {
                        break;
                    }
                }
                return fileRights;
            }
            catch (SkydrmException e)
            {
                GeneralHandler.Handle(e, true);
                return fileRights;
            }
        }

        public bool ModifyRights(List<FileRights> rights, WaterMarkInfo waterMark, Expiration expiration, Dictionary<string, List<string>> selectedTags)
        {
            bool result = false;

            // init UserSelectTags, if protect to central policy file, Rights should clear.
            UserSelectTags userSelectTags = new UserSelectTags();
            if (NxlType == NxlFileType.CentralPolicy)
            {
                rights.Clear();
                // whether reset WarterMarkInfo and Expiration ??

                foreach (var item in selectedTags)
                {
                    userSelectTags.AddTag(item.Key, item.Value);
                }
            }

            string filePath = fingerPrint.localPath;

            try
            {
                // SystemBucket or workSpace
                if (fingerPrint.isFromSystemBucket)
                {
                    // System Bucket
                    if (currentSelectedSavePath.RepoName == DataTypeConvertHelper.SYSTEMBUCKET)
                    {
                        result = app.Rmsdk.User.UpdateNxlFileRights(filePath, rights,
                            waterMark, expiration, userSelectTags);
                    }
                    // WorkSpace 
                    else
                    {
                        ISearchFileInWorkSpace searchFileInWorkSpace = new SearchWorkSpaceFileByLocalPath();
                        var workSpaceFile = searchFileInWorkSpace.SearchInRmsFiles(filePath);

                        if (workSpaceFile != null)
                        {
                            result = workSpaceFile.ModifyRights(rights, waterMark, expiration, userSelectTags);
                        }
                        else
                        {
                            // The file that exported(save as) from workspace repo.
                            result = app.Rmsdk.User.UpdateNxlFileRights(filePath, rights,
                                waterMark, expiration, userSelectTags);
                        }
                    }
                }
                // Project
                else if (fingerPrint.isFromPorject)
                {
                    ISearchFileInProject SearchFileInMyVault = new SearchProjectFileByLocalPath();
                    var projectFile = SearchFileInMyVault.SearchInRmsFiles(filePath);
                    if (projectFile != null)
                    {
                        result = projectFile.ModifyRights(rights, waterMark, expiration, userSelectTags);
                    }
                    else
                    {
                        // In project saveAs file that is new nxlFile.
                        result = app.Rmsdk.User.UpdateNxlFileRights(filePath, rights,
                       waterMark, expiration, userSelectTags);
                    }
                }
            }
            catch (Exception e)
            {
                result = false;
                app.Log.Error(e.Message, e);
                app.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("ModifyRightOperation_ModifyFailed"),
                    false, fingerPrint.name, "Modify rights");
            }
            
            return result;
        }

        public void AddLog()
        {
            // Send classify log for system bucket file(fix bug 54446)
            // We won't send log for project because RMS will keep both success & failed classify log for us.
            if (fingerPrint.isFromSystemBucket)
            {
                app.User.AddNxlFileLog(fingerPrint.localPath, NxlOpLog.Classify, true);
            }
        }

        public void UpdateNxlFile()
        {
            INxlFile nxlFileP = GlobalSearchEx.GetInstance().SearchByLocalPath(fingerPrint.localPath, SearchFileRepo.Project, SearchFileTable.Rms);
            INxlFile nxlFileW = GlobalSearchEx.GetInstance().SearchByLocalPath(fingerPrint.localPath, SearchFileRepo.WorkSpace, SearchFileTable.Rms);

            if (nxlFileP != null)
            {
                UpdateNxl(nxlFileP);
            }
            if (nxlFileW != null)
            {
                UpdateNxl(nxlFileW);
            }
        }
        private void UpdateNxl(INxlFile nxl)
        {
            if (nxl == null)
            {
                return;
            }
            // For offline file, after modify rights successfully, try to download partial again,
            // in order to avoid can't get new rights by right click context menu when network is offline.
            // Fix bug 58038
            if (nxl.Location == EnumFileLocation.Local)
            {
                app.MainWin.viewModel.PartialDownloadEx();
            }

            // Db file will update "LastModified time" by this flag after refresh.
            nxl.IsModifiedRights = true;
            app.MainWin.viewModel.DoRefresh();
        }

    }
}
