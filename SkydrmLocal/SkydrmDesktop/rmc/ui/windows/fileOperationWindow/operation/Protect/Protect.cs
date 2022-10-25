using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkydrmDesktop.Resources.languages;
using SkydrmDesktop.rmc.featureProvider.MessageNotify;
using SkydrmDesktop.rmc.ui.windows.fileOperationWindow.model;
using SkydrmDesktop.rmc.ui.windows.mainWindow.model;
using SkydrmLocal.rmc.exception;
using SkydrmLocal.rmc.fileSystem;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.ui.utils;

namespace SkydrmDesktop.rmc.ui.windows.fileOperationWindow.operation
{
    class Protect : IProtect
    {
        private readonly SkydrmApp app = SkydrmApp.Singleton;

        private OperateFileInfo fileInfo;
        private IList<IFileRepo> repos;
        private CurrentSelectedSavePath currentSelectedSavePath;
        public Protect(OperateFileInfo fileInfo, IList<IFileRepo> repos, CurrentSelectedSavePath selectedSavePath)
        {
            this.fileInfo = fileInfo;
            this.repos = repos;
            this.currentSelectedSavePath = selectedSavePath;
        }

        public FileAction FileAction => FileAction.Protect;

        public OperateFileInfo FileInfo { get => fileInfo; set => fileInfo = value; }

        public IList<IFileRepo> TreeList => repos;

        public CurrentSelectedSavePath CurrentSelectedSavePath { get => currentSelectedSavePath; set => currentSelectedSavePath = value; }

        public List<FileRights> PreviewRightsByCentralPolicy(int id, Dictionary<string, List<string>> selectedTags, out string mWatermarkStr)
        {
            List<FileRights> fileRights = new List<FileRights>();
            mWatermarkStr = string.Empty;
            try
            {
                UserSelectTags tags = new UserSelectTags();
                foreach (var item in selectedTags)
                {
                    tags.AddTag(item.Key, item.Value);
                }
                // Inoke sdk api, get rights
                Dictionary<FileRights, List<WaterMarkInfo>> rightsAndWatermarks;

                app.Rmsdk.User.GetFileRightsFromCentalPolicyByProjectId(id, tags,
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
                        mWatermarkStr = w.text;
                        if (!string.IsNullOrEmpty(mWatermarkStr))
                        {
                            break;
                        }
                    }
                    if (!string.IsNullOrEmpty(mWatermarkStr))
                    {
                        break;
                    }
                }
                return fileRights;
            }
            catch (SkydrmLocal.rmc.sdk.SkydrmException e)
            {
                GeneralHandler.Handle(e, true);
                return fileRights;
            }
        }

        public List<INxlFile> ProtectFile(List<FileRights> rights,
            string waterMarkText, Expiration expiration, Dictionary<string, List<string>> selectedTags)
        {
            // Reset user-selected actions
            app.User.ApplyAllSelectedOption = false;
            app.User.SelectedOption = 0;

            List<INxlFile> createdNxlFiles = new List<INxlFile>();

            // init WarterMarkInfo
            WaterMarkInfo waterMarkInfo = new WaterMarkInfo()
            {
                fontColor = "",
                fontName = "",
                text = "",
                fontSize = 0,
                repeat = 0,
                rotation = 0,
                transparency = 0
            };

            if (rights.Contains(FileRights.RIGHT_WATERMARK))
            {
                waterMarkInfo.text = waterMarkText;
            }

            // init UserSelectTags, if protect to central policy file, Rights should clear.
            UserSelectTags userSelectTags = new UserSelectTags();
            bool isCentralPolicy = app.User.IsCentralPlcRadio;
            if (isCentralPolicy)
            {
                rights.Clear();
                // whether reset WarterMarkInfo and Expiration ??

                foreach (var item in selectedTags)
                {
                    userSelectTags.AddTag(item.Key, item.Value);
                }
            }

            var selectedSavePath = CurrentSelectedSavePath;

            // if protect to project, need find selected project
            SkydrmLocal.rmc.featureProvider.IMyProject selectedProject = null;
            if (selectedSavePath.RepoName.Equals(DataTypeConvertHelper.PROJECT))
            {
                foreach (var item in TreeList)
                {
                    if (item is SkydrmLocal.rmc.fileSystem.project.ProjectRepo)
                    {
                        IList<SkydrmLocal.rmc.fileSystem.project.ProjectData> projects = (item as SkydrmLocal.rmc.fileSystem.project.ProjectRepo).FilePool;
                        foreach (var project in projects)
                        {
                            if (selectedSavePath.OwnerId.Equals(project.ProjectInfo.ProjectId.ToString()))
                            {
                                selectedProject = project.ProjectInfo.Raw;
                                break;
                            }
                        }
                        break;
                    }
                }
            }

            // if protect to external repo, need find selectedExternalRepo
            IFileRepo selectedExternalRepo = null;
            if (selectedSavePath.RepoName.Equals(DataTypeConvertHelper.REPOSITORIES))
            {
                foreach (var item in TreeList)
                {
                    if (item is fileSystem.externalDrive.ExternalRepo || item is fileSystem.sharedWorkspace.SharedWorkspaceRepo)
                    {
                        if (item.RepoId.Equals(selectedSavePath.OwnerId))
                        {
                            selectedExternalRepo = item;
                            break;
                        }
                    }
                }
            }

            // protect files
            List<string> nxlFileName = new List<string>();
            Dictionary<string, string> failedFileName = new Dictionary<string, string>();
            INxlFile doc = null;
            
            for (int i = 0; i < FileInfo.FilePath.Length; i++)
            {
                doc = InnerProtectFile(selectedSavePath, isCentralPolicy, FileInfo.FilePath[i], rights, waterMarkInfo, 
                    expiration, userSelectTags, selectedProject, selectedExternalRepo, out string msg);
                if (doc != null)
                {
                    nxlFileName.Add(doc.Name);
                    createdNxlFiles.Add(doc);
                }
                else
                {
                    if (app.User.SelectedOption != 3)
                    {
                        failedFileName.Add(FileInfo.FileName[i], msg);
                    }
                }
            }

            // update fileName to NxlFileName
            if (nxlFileName.Count > 0)
            {
                FileInfo.FileName = nxlFileName.ToArray();
            }

            // update failed fileName
            FileInfo.FailedFileName= failedFileName;

            return createdNxlFiles;
        }
        private INxlFile InnerProtectFile(CurrentSelectedSavePath selectedSavePath, bool isCentralPolicy, string filePath, List<FileRights> fileRights, 
            WaterMarkInfo waterMarkInfo, Expiration expiration,
            UserSelectTags userSelectTags, 
            SkydrmLocal.rmc.featureProvider.IMyProject selectedProject,
            IFileRepo selectedExternalRepo, out string msg)
        {
            msg = string.Empty;
            if (selectedSavePath.OwnerId.Equals("0") || selectedSavePath.OwnerId.Equals(app.SystemProject.Id.ToString())) // MyVault, systemBucket, workSpace, myDrive
            {
                if (selectedSavePath.RepoName.Equals(DataTypeConvertHelper.MY_VAULT))
                {
                    return ProtectFileHelper.MyVaultAddLocalFile(filePath, fileRights, waterMarkInfo, expiration, out msg);
                }
                else if (selectedSavePath.RepoName.Equals(DataTypeConvertHelper.SYSTEMBUCKET))
                {
                    return ProtectFileHelper.SystemBucketAddLocalFile(isCentralPolicy, filePath, selectedSavePath.DestPathId, 
                        fileRights, waterMarkInfo, expiration,userSelectTags, out msg);
                }
                else if (selectedSavePath.RepoName.Equals(DataTypeConvertHelper.WORKSPACE))
                {
                    return ProtectFileHelper.WorkSpaceAddLocalFile(isCentralPolicy,filePath, selectedSavePath.DestPathId, 
                        fileRights, waterMarkInfo, expiration, userSelectTags, out msg);
                }
            }
            else // project, or external repo
            {
                if (selectedSavePath.RepoName.Equals(DataTypeConvertHelper.PROJECT))
                {
                    return ProtectFileHelper.ProjectAddLocalFile(isCentralPolicy, filePath, selectedSavePath.DestPathId, 
                        fileRights, waterMarkInfo, expiration, userSelectTags, selectedProject, out msg);
                }
                else if (selectedSavePath.RepoName.Equals(DataTypeConvertHelper.REPOSITORIES))
                {
                    if (selectedExternalRepo is fileSystem.externalDrive.ExternalRepo)
                    {
                        return ProtectFileHelper.ExternalRepoAddLocalFile(isCentralPolicy, filePath, selectedSavePath.DestPathId, 
                            fileRights, waterMarkInfo, expiration, userSelectTags, 
                            (fileSystem.externalDrive.ExternalRepo)selectedExternalRepo, out msg);
                    }
                    if (selectedExternalRepo is fileSystem.sharedWorkspace.SharedWorkspaceRepo)
                    {
                        //  dest display path SkyDRM://Repositories/SPOL1/folder1
                        string path = selectedSavePath.DestDisplayPath.Substring(9);
                        // path = Repositories/SPOL1/folder1
                        string firstIndexPath = path.Substring(path.IndexOf('/')+1);
                        // firstIndexPath = SPOL1/folder1
                        string displayPath = firstIndexPath.Substring(firstIndexPath.IndexOf('/'));
                        // displayPath = /folder1
                        return ProtectFileHelper.SharedWorkSpaceAddLocalFile(isCentralPolicy, filePath, selectedSavePath.DestPathId, 
                            displayPath, fileRights, waterMarkInfo, expiration, userSelectTags, 
                            (fileSystem.sharedWorkspace.SharedWorkspaceRepo)selectedExternalRepo, out msg);
                    }
                }
            }
            return null;
        }

    }
}
