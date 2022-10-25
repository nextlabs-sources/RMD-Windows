using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SkydrmDesktop.Resources.languages;
using SkydrmDesktop.rmc.featureProvider.MessageNotify;
using SkydrmDesktop.rmc.fileSystem.sharedWorkspace;
using SkydrmDesktop.rmc.fileSystem.workspace;
using SkydrmDesktop.rmc.ui.windows.fileOperationWindow.model;
using SkydrmDesktop.rmc.ui.windows.mainWindow.model;
using SkydrmLocal.rmc.drive;
using SkydrmLocal.rmc.exception;
using SkydrmLocal.rmc.fileSystem;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.fileSystem.project;
using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.ui.utils;

namespace SkydrmDesktop.rmc.ui.windows.fileOperationWindow.operation
{
    class AddNxlFile : IAddNxlFile
    {
        private readonly SkydrmApp app = SkydrmApp.Singleton;

        private NxlFileFingerPrint fingerPrint;
        private string decryptNxlPath = string.Empty;

        private OperateFileInfo fileInfo;
        private IList<IFileRepo> repos;
        private INxlFile nxl;
        private CurrentSelectedSavePath currentSelectedSavePath;

        /// <summary>
        /// Used to main window file listview or desktop plug-in cmd
        /// </summary>
        /// <param name="fingerP"></param>
        /// <param name="fileInfo"></param>
        /// <param name="repos"></param>
        public AddNxlFile(NxlFileFingerPrint fingerP, OperateFileInfo fileInfo, IList<IFileRepo> repos, INxlFile file)
        {
            this.fingerPrint = fingerP;
            this.fileInfo = fileInfo;
            this.repos = repos;
            this.nxl = file;
            this.currentSelectedSavePath = null;
        }

        /// <summary>
        /// Used to main window button cmd
        /// </summary>
        /// <param name="fingerP"></param>
        /// <param name="fileInfo"></param>
        /// <param name="selectedSavePath"></param>
        public AddNxlFile(OperateFileInfo fileInfo, IList<IFileRepo> repos, CurrentSelectedSavePath selectedSavePath)
        {
            this.fileInfo = fileInfo;
            this.currentSelectedSavePath = selectedSavePath;
            this.repos = repos;
        }

        /// <summary>
        /// Used to FileSelectPage re-init this class
        /// </summary>
        /// <param name="fingerP"></param>
        /// <param name="fileInfo"></param>
        /// <param name="repos"></param>
        /// <param name="selectedSavePath"></param>
        public AddNxlFile(NxlFileFingerPrint fingerP, OperateFileInfo fileInfo, IList<IFileRepo> repos, CurrentSelectedSavePath selectedSavePath, INxlFile file)
        {
            this.fingerPrint = fingerP;
            this.fileInfo = fileInfo;
            this.currentSelectedSavePath = selectedSavePath;
            this.repos = repos;
            this.nxl = file;
        }

        public FileAction FileAction => FileAction.AddFileTo;

        public OperateFileInfo FileInfo { get => fileInfo; set => fileInfo = value; }

        public IList<IFileRepo> TreeList => repos;

        public CurrentSelectedSavePath CurrentSelectedSavePath { get => currentSelectedSavePath; set => currentSelectedSavePath = value; }

        public EnumFileRepo OriginalFileRepo
        {
            get
            {
                if (nxl != null)
                {
                    return nxl.FileRepo;
                }
                return EnumFileRepo.UNKNOWN;
            }
        }

        public string RepoId
        {
            get
            {
                if (nxl != null)
                {
                    return nxl.RepoId;
                }
                return null;
            }
        }

        public int ProjectId => fingerPrint.projectId;

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

        public string FileName
        {
            get
            {
                if (nxl == null)
                {
                    if (FileInfo != null)
                    {
                        if (FileInfo.FromSource == FileFromSource.SkyDRM_PlugIn ||
                            FileInfo.FromSource == FileFromSource.SkyDRM_Window_Button)
                        {
                            return FileInfo.FileName[0];
                        }
                    }
                    return "";
                }
                return nxl.Name;
            }
        }

        public string AvailableDestFileName
        {
            get
            {
                return GenerateNewName(FileName, string.Format("({0})", GetCountIndex()));
            }
        }

        private string GenerateNewName(string fileName, string count)
        {
            if (fileName == null || fileName.Length == 0)
            {
                return null;
            }
            string orgFileNameNoExt = "";
            if (fileName.EndsWith(".nxl"))
            {
                // Remove .nxl extention first then try get name without ext.
                var nameWithoutNXL = fileName.Substring(0, fileName.LastIndexOf(".nxl"));
                var idx = nameWithoutNXL.LastIndexOf(".");
                if (idx != -1)
                {
                    orgFileNameNoExt = nameWithoutNXL.Substring(0, idx);
                }
            }
            else
            {
                var idx = fileName.LastIndexOf(".");
                if (idx != -1)
                {
                    orgFileNameNoExt = fileName.Substring(0, idx);
                }
            }
            // Get first extension
            string firstExt = Path.GetExtension(fileName);
            string secExt = string.Empty;
            int lastIndex = fileName.LastIndexOf('.');
            if (lastIndex != -1)
            {
                // Remove first extension
                fileName = fileName.Substring(0, lastIndex);
                // Get second extension .txt ... or null, empty
                secExt = Path.GetExtension(fileName);
            }
            return string.Format("{0}{1}{2}{3}", orgFileNameNoExt, count, secExt, firstExt);
        }

        private int GetCountIndex()
        {
            int count = 1;
            if(CurrentSelectedSavePath == null)
            {
                return count;
            }
            var repo = GetDestRepo(CurrentSelectedSavePath);
            if(repo == null)
            {
                return count;
            }
            var destPathId = GetDestPathId(CurrentSelectedSavePath);
            var name_escaping = FileName.Replace("(", "\\(").Replace(")", "\\)");
            string reg = GenerateNewName(name_escaping, "([(][0-9]+[)])");

            bool found = false;
            IList<INxlFile> results = SyncChildren(repo, destPathId);
            if(results != null && results.Count != 0)
            {
                foreach (var f in results)
                {
                    if (f == null || f.IsFolder)
                    {
                        continue;
                    }
                    if (Regex.IsMatch(f.Name, reg))
                    {
                        var num = GetCount(f.Name);
                        if (count <= num)
                        {
                            found = true;
                            count = num;
                        }
                    }
                }
            }
            return found ? ++count : count;
        }

        private IList<INxlFile> SyncChildren(IFileRepo repo, string pathId)
        {
            if (repo is ProjectRepo || repo is SharedWorkspaceRepo || repo is WorkSpaceRepo)
            {
                var destFolder = GetTargetFolder(repo, pathId);
                if (destFolder != null)
                {
                    var children = (destFolder as NxlFolder).Children;
                    if(children == null || children.Count == 0)
                    {
                        return null;
                    }
                    IList<INxlFile> data = new List<INxlFile>();
                    repo.SyncParentNodeFile(children[0], ref data);
                    return data;
                }
                else
                {
                    if (pathId.Equals("/"))
                    {
                        var fp = repo.GetFilePool();
                        if (fp == null || fp.Count == 0)
                        {
                            return null;
                        }
                        IList<INxlFile> data = new List<INxlFile>();
                        repo.SyncParentNodeFile(fp[0], ref data);
                        return data;
                    }
                }
                return repo.GetFilePool();
            }
            IList<INxlFile> rt = new List<INxlFile>();
            repo.SyncFilesRecursively("/", rt);
            return rt;
        }

        private INxlFile GetTargetFolder(IFileRepo repo, string pathId)
        {
            INxlFile rt = null;
            if (repo == null)
            {
                return rt;
            }
            if (pathId == null || pathId.Length == 0)
            {
                return rt;
            }
            var fp = repo.GetFilePool();
            return GetTargetFolder(fp, pathId);
        }

        private INxlFile GetTargetFolder(IList<INxlFile> fp, string pathId)
        {
            INxlFile rt = null;
            if (fp == null)
            {
                return rt;
            }
            foreach (var f in fp)
            {
                if (f == null || !f.IsFolder)
                {
                    continue;
                }
                if (f.IsFolder)
                {
                    rt =  GetTargetFolder((f as NxlFolder).Children, pathId);
                    if(rt != null)
                    {
                        break;
                    }
                }
                if (f.PathId.Equals(pathId)
                    || f.DisplayPath.Equals(pathId))
                {
                    rt = f;
                    break;
                }
            }
            return rt;
        }

        private int GetCount(string fileName)
        {
            int count = -1;
            if (fileName == null || fileName.Length == 0)
            {
                return count;
            }
            var regx = @"([(][0-9]+[)][.])";
            var matches = Regex.Split(fileName, regx);
            if (matches == null || matches.Length == 0)
            {
                return count;
            }
            for (int i = matches.Length - 1; i != 0; i--)
            {
                var s = matches[i];
                if (s == null || s.Length == 0)
                {
                    continue;
                }
                if (!Regex.IsMatch(s, regx))
                {
                    continue;
                }
                // Find the last string matches pattern.
                count = int.Parse(Regex.Match(s, @"\d+").Value);
                break;
            }
            return count;
        }

        //public bool DecryptNxlFile(out string decryptPath)
        //{
        //    bool result = false;
        //    decryptPath = "";

        //    try
        //    {
        //        decryptPath = RightsManagementService.GenerateDecryptFilePath(app.User.RPMFolder, fingerPrint.localPath, DecryptIntent.AddNxl);
        //        RightsManagementService.DecryptNXLFile(app, fingerPrint.localPath, decryptPath);

        //        if (Alphaleonis.Win32.Filesystem.File.Exists(decryptPath))
        //        {
        //            decryptNxlPath = decryptPath;
        //            result = true;
        //        }
        //        else
        //        {
        //            if (!string.IsNullOrEmpty(decryptPath))
        //            {
        //                RightsManagementService.RPMDeleteDirectory(app, Alphaleonis.Win32.Filesystem.Path.GetDirectoryName(decryptPath));
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        result = false;
        //        app.Log.Info(ex.ToString());
        //        if (!string.IsNullOrEmpty(decryptPath))
        //        {
        //            RightsManagementService.RPMDeleteDirectory(app, Alphaleonis.Win32.Filesystem.Path.GetDirectoryName(decryptPath));
        //        }
        //    }

        //    return result;
        //}

        //public void DeleteDecryptNxlFile()
        //{
        //    if (decryptNxlPath.Contains(app.User.RPMFolder))
        //    {
        //        RightsManagementService.RPMDeleteDirectory(app, Alphaleonis.Win32.Filesystem.Path.GetDirectoryName(decryptNxlPath));
        //    }
        //}

        public List<FileRights> PreviewRightsByCentralPolicy(int id, Dictionary<string, List<string>> selectedTags, out WaterMarkInfo warterMark)
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

        //public List<INxlFile> ProtectFile(string[] filePath, List<FileRights> rights, WaterMarkInfo waterMark, Expiration expiration, Dictionary<string, List<string>> selectedTags)
        //{
        //    // Reset user-selected actions
        //    app.User.ApplyAllSelectedOption = false;
        //    app.User.SelectedOption = 0;

        //    List<INxlFile> createdNxlFiles = new List<INxlFile>();
        //    var selectedSavePath = CurrentSelectedSavePath;
        //    if (selectedSavePath == null)
        //    {
        //        return createdNxlFiles;
        //    }

        //    if (!rights.Contains(FileRights.RIGHT_WATERMARK))
        //    {
        //        waterMark.text = "";
        //    }

        //    // init UserSelectTags, if protect to central policy file, Rights should clear.
        //    UserSelectTags userSelectTags = new UserSelectTags();
        //    bool isCentralPolicy = NxlType == NxlFileType.CentralPolicy;
        //    if (isCentralPolicy)
        //    {
        //        rights.Clear();
        //        // whether reset WarterMarkInfo and Expiration ??

        //        foreach (var item in selectedTags)
        //        {
        //            userSelectTags.AddTag(item.Key, item.Value);
        //        }
        //    }

        //    // if protect to project, need find selected project
        //    SkydrmLocal.rmc.featureProvider.IMyProject selectedProject = null;
        //    if (selectedSavePath.RepoName.Equals(DataTypeConvertHelper.PROJECT))
        //    {
        //        foreach (var item in TreeList)
        //        {
        //            if (item is SkydrmLocal.rmc.fileSystem.project.ProjectRepo)
        //            {
        //                IList<SkydrmLocal.rmc.fileSystem.project.ProjectData> projects = (item as SkydrmLocal.rmc.fileSystem.project.ProjectRepo).FilePool;
        //                foreach (var project in projects)
        //                {
        //                    if (selectedSavePath.OwnerId.Equals(project.ProjectInfo.ProjectId.ToString()))
        //                    {
        //                        selectedProject = project.ProjectInfo.Raw;
        //                        break;
        //                    }
        //                }
        //                break;
        //            }
        //        }
        //    }

        //    // if protect to external repo, need find selectedExternalRepo
        //    IFileRepo selectedExternalRepo = null;
        //    if (selectedSavePath.RepoName.Equals(DataTypeConvertHelper.REPOSITORIES))
        //    {
        //        foreach (var item in TreeList)
        //        {
        //            if (item is fileSystem.externalDrive.ExternalRepo || item is fileSystem.sharedWorkspace.SharedWorkspaceRepo)
        //            {
        //                if (item.RepoId.Equals(selectedSavePath.OwnerId))
        //                {
        //                    selectedExternalRepo = item;
        //                    break;
        //                }
        //            }
        //        }
        //    }

        //    // protect files
        //    List<string> nxlFileName = new List<string>();
        //    Dictionary<string, string> failedFileName = new Dictionary<string, string>();
        //    INxlFile doc = null;

        //    for (int i = 0; i < filePath.Length; i++)
        //    {
        //        doc = InnerProtectFile(selectedSavePath, isCentralPolicy, filePath[i], rights, waterMark,
        //            expiration, userSelectTags, selectedProject, selectedExternalRepo, out string msg);
        //        if (doc != null)
        //        {
        //            nxlFileName.Add(doc.Name);
        //            createdNxlFiles.Add(doc);
        //        }
        //        else
        //        {
        //            if (app.User.SelectedOption != 3)
        //            {
        //                failedFileName.Add(FileInfo.FileName[i], msg);
        //            }
        //        }
        //    }

        //    // update fileName to NxlFileName
        //    if (nxlFileName.Count > 0)
        //    {
        //        FileInfo.FileName = nxlFileName.ToArray();
        //    }

        //    // update failed fileName
        //    FileInfo.FailedFileName = failedFileName;

        //    return createdNxlFiles;
        //}
        //private INxlFile InnerProtectFile(CurrentSelectedSavePath selectedSavePath, bool isCentralPolicy, string filePath, List<FileRights> fileRights,
        //    WaterMarkInfo waterMarkInfo, Expiration expiration,
        //    UserSelectTags userSelectTags,
        //    SkydrmLocal.rmc.featureProvider.IMyProject selectedProject,
        //    IFileRepo selectedExternalRepo, out string msg)
        //{
        //    msg = string.Empty;
        //    if (selectedSavePath.OwnerId.Equals("0") || selectedSavePath.OwnerId.Equals(app.SystemProject.Id.ToString())) // MyVault, systemBucket, workSpace, myDrive
        //    {
        //        if (selectedSavePath.RepoName.Equals(DataTypeConvertHelper.MY_VAULT))
        //        {
        //            return ProtectFileHelper.MyVaultAddLocalFile(filePath, fileRights, waterMarkInfo, expiration, out msg);
        //        }
        //        else if (selectedSavePath.RepoName.Equals(DataTypeConvertHelper.SYSTEMBUCKET))
        //        {
        //            return ProtectFileHelper.SystemBucketAddLocalFile(isCentralPolicy, filePath, selectedSavePath.DestPathId,
        //                fileRights, waterMarkInfo, expiration, userSelectTags, out msg);
        //        }
        //        else if (selectedSavePath.RepoName.Equals(DataTypeConvertHelper.WORKSPACE))
        //        {
        //            return ProtectFileHelper.WorkSpaceAddLocalFile(isCentralPolicy, filePath, selectedSavePath.DestPathId,
        //                fileRights, waterMarkInfo, expiration, userSelectTags, out msg);
        //        }
        //    }
        //    else // project, or external repo
        //    {
        //        if (selectedSavePath.RepoName.Equals(DataTypeConvertHelper.PROJECT))
        //        {
        //            return ProtectFileHelper.ProjectAddLocalFile(isCentralPolicy, filePath, selectedSavePath.DestPathId,
        //                fileRights, waterMarkInfo, expiration, userSelectTags, selectedProject, out msg);
        //        }
        //        else if (selectedSavePath.RepoName.Equals(DataTypeConvertHelper.REPOSITORIES))
        //        {
        //            if (selectedExternalRepo is fileSystem.externalDrive.ExternalRepo)
        //            {
        //                return ProtectFileHelper.ExternalRepoAddLocalFile(isCentralPolicy, filePath, selectedSavePath.DestPathId,
        //                    fileRights, waterMarkInfo, expiration, userSelectTags,
        //                    (fileSystem.externalDrive.ExternalRepo)selectedExternalRepo, out msg);
        //            }
        //            if (selectedExternalRepo is fileSystem.sharedWorkspace.SharedWorkspaceRepo)
        //            {
        //                //  dest display path SkyDRM://Repositories/SPOL1/folder1
        //                string path = selectedSavePath.DestDisplayPath.Substring(9);
        //                // path = Repositories/SPOL1/folder1
        //                string firstIndexPath = path.Substring(path.IndexOf('/') + 1);
        //                // firstIndexPath = SPOL1/folder1
        //                string displayPath = firstIndexPath.Substring(firstIndexPath.IndexOf('/'));
        //                // displayPath = /folder1
        //                return ProtectFileHelper.SharedWorkSpaceAddLocalFile(isCentralPolicy, filePath, selectedSavePath.DestPathId,
        //                    displayPath, fileRights, waterMarkInfo, expiration, userSelectTags,
        //                    (fileSystem.sharedWorkspace.SharedWorkspaceRepo)selectedExternalRepo, out msg);
        //            }
        //        }
        //    }
        //    return null;
        //}

        //public void AddLog()
        //{
        //    app.User.AddNxlFileLog(fingerPrint.localPath, NxlOpLog.Protect, true);
        //}

        #region COPY NXL API 
        public void CopyNxlFile(string destName, bool overwrite)
        {
            CopyTo(destName, overwrite);
        }

        public void CopyTo(string destName, bool overwrite)
        {
            //Source params.
            // If nxl is null then need to check whether file from local.
            var src = nxl;
            var srcFileName = FileName;
            // If overwrite, then take src file name as dest name.
            if (!overwrite)
            {
                if (destName == null || destName.Length == 0)
                {
                    destName = srcFileName;
                }
            }
            else
            {
                destName = srcFileName;
            }
            var srcFilePathId = GetSourcePathId(src);
            Space srcSpaceType = GetSourceSpace(src);
            if (srcFileName == null || srcFilePathId == null || srcSpaceType == null)
            {
                throw new Exception("Invalid source params.");
            }
            //Dest params.
            var dest = currentSelectedSavePath;
            if (dest == null)
            {
                throw new Exception("Invalid destination path.");
            }

            var destFileName = destName;
            var destParentPathId = GetDestPathId(dest);
            var destSpaceType = GetDestSpace(dest);
            if (destParentPathId == null || destSpaceType == null)
            {
                throw new Exception("Invalid dest params.");
            }
            if (src is SkydrmLocal.rmc.fileSystem.myvault.SharedWithDoc)
            {
                //Mandatory params: transactionId, spaceType.
                var transactionId = (src as SkydrmLocal.rmc.fileSystem.myvault.SharedWithDoc).TransactionId;
                var transactionCode = "";

                CopyInternal(srcFileName, srcFilePathId, srcSpaceType.Type, srcSpaceType.Id,
                    destFileName, destParentPathId, destSpaceType.Type, destSpaceType.Id, overwrite,
                    transactionCode, transactionId);
                return;
            }

            CopyInternal(srcFileName, srcFilePathId, srcSpaceType.Type, srcSpaceType.Id,
                destFileName, destParentPathId, destSpaceType.Type, destSpaceType.Id, overwrite,
                "", "");
        }

        public void CopyInternal(string srcFileName, string srcFilePathId, NxlFileSpaceType srcSpaceType, string srcSpaceId,
            string destFileName, string destParentPathId, NxlFileSpaceType destSpaceType, string destSpaceId,
            bool overwrite,
            string transactionCode, string transactionId)
        {
            try
            {
                app.Rmsdk.User.CopyNxlFile(srcFileName, srcFilePathId, srcSpaceType, srcSpaceId,
                    destFileName, destParentPathId, destSpaceType, destSpaceId,
                    overwrite,
                    transactionCode, transactionId);
            }
            catch(Exception e)
            {
                app.MessageNotify.NotifyMsg(srcFileName, e.Message, 
                    EnumMsgNotifyType.LogMsg, 
                    MsgNotifyOperation.ADD_NXL_FILE, 
                    EnumMsgNotifyResult.Failed, 
                    EnumMsgNotifyIcon.Unknown);

                throw e;
            }
        }

        private string GetSourcePathId(INxlFile file)
        {
            if (file == null)
            {
                if (FileInfo != null)
                {
                    if (FileInfo.FromSource == FileFromSource.SkyDRM_PlugIn ||
                        FileInfo.FromSource == FileFromSource.SkyDRM_Window_Button)
                    {
                        return FileInfo.FilePath[0];
                    }
                }
                return null;
            }
            if (file.PathId.Contains("/"))
            {
                return file.PathId;
            }
            else
            {
                return file.FileInfo.RmsRemotePath;
            }
        }

        private Space GetSourceSpace(INxlFile file)
        {
            if (file == null)
            {
                if (FileInfo != null)
                {
                    if (FileInfo.FromSource == FileFromSource.SkyDRM_PlugIn ||
                        FileInfo.FromSource == FileFromSource.SkyDRM_Window_Button)
                    {
                        return new Space("", NxlFileSpaceType.local_drive);
                    }
                }
                return null;
            }
            var repo = file.FileRepo;
            var repoId = file.RepoId == null ? "" : file.RepoId;

            if (repo == EnumFileRepo.REPO_MYVAULT)
            {
                return new Space(repoId, NxlFileSpaceType.my_vault);
            }
            if (repo == EnumFileRepo.REPO_SHARED_WITH_ME)
            {
                return new Space(repoId, NxlFileSpaceType.shared_with_me);
            }
            if (repo == EnumFileRepo.REPO_PROJECT)
            {
                return new Space(repoId, NxlFileSpaceType.project);
            }
            if (repo == EnumFileRepo.REPO_WORKSPACE)
            {
                return new Space(repoId, NxlFileSpaceType.enterprise_workspace);
            }
            if (repo == EnumFileRepo.LOCAL_DRIVE)
            {
                return new Space(repoId, NxlFileSpaceType.local_drive);
            }
            if (repo == EnumFileRepo.REPO_EXTERNAL_DRIVE)
            {
                if (file is fileSystem.sharedWorkspace.SharedWorkspaceDoc)
                {
                    return new Space(repoId, NxlFileSpaceType.sharepoint_online);
                }
            }
            return null;
        }

        private string GetDestPathId(CurrentSelectedSavePath path)
        {
            if (path == null)
            {
                return null;
            }
            var rt = path.DestPathId;
            if (rt.Contains("/"))
            {
                return path.DestPathId;
            }
            var repo = GetDestRepo(path);
            if (repo == null)
            {
                return null;
            }
            var repoName = repo.RepoDisplayName;
            var displayPath = path.DestDisplayPath;
            if (displayPath.Contains(repoName))
            {
                rt = displayPath.Substring(displayPath.LastIndexOf(repoName) + repoName.Length);
            }
            return rt;
        }

        private Space GetDestSpace(CurrentSelectedSavePath path)
        {
            if (path == null)
            {
                throw new Exception("Invalid selected path.");
            }
            //MyVault.
            if (path.RepoName.Equals(DataTypeConvertHelper.MY_SPACE)
                || path.RepoName.Equals(DataTypeConvertHelper.MY_VAULT))
            {
                return new Space("", NxlFileSpaceType.my_vault);
            }
            //Enterprise workspace.
            if (path.RepoName.Equals(DataTypeConvertHelper.WORKSPACE))
            {
                return new Space("", NxlFileSpaceType.enterprise_workspace);
            }
            //Project
            if (path.RepoName.Equals(DataTypeConvertHelper.PROJECT))
            {
                foreach (var item in TreeList)
                {
                    if (item is ProjectRepo)
                    {
                        IList<ProjectData> projects = (item as ProjectRepo).FilePool;
                        foreach (var project in projects)
                        {
                            if (path.OwnerId.Equals(project.ProjectInfo.ProjectId.ToString()))
                            {
                                return new Space(project.ProjectInfo.Raw.Id.ToString(), NxlFileSpaceType.project);
                            }
                        }
                    }
                }
            }
            //Shared workspace&externl repo.
            if (path.RepoName.Equals(DataTypeConvertHelper.REPOSITORIES))
            {
                foreach (var item in TreeList)
                {
                    if (item.RepoId.Equals(path.OwnerId))
                    {
                        if (item is fileSystem.sharedWorkspace.SharedWorkspaceRepo
                            || item is fileSystem.externalDrive.ExternalRepo)
                        {
                            string type = item.RepoType;
                            var provider = item.RepoProviderClass;
                            //Shared workspace.
                            if (provider == fileSystem.utils.RepositoryProviderClass.APPLICATION)
                            {
                                if (type.Equals(fileSystem.utils.FileSysConstant.ExternalRepoName[5]))
                                {
                                    return new Space(item.RepoId, NxlFileSpaceType.sharepoint_online);
                                }
                            }
                            //External repo.
                            if (provider == fileSystem.utils.RepositoryProviderClass.PERSONAL)
                            {
                                //DROPBOX
                                if (type.Equals(fileSystem.utils.FileSysConstant.ExternalRepoName[0]))
                                {
                                    return new Space(item.RepoId, NxlFileSpaceType.dropbox);
                                }
                                //ONEDRIVE
                                if (type.Equals(fileSystem.utils.FileSysConstant.ExternalRepoName[1]))
                                {
                                    return new Space(item.RepoId, NxlFileSpaceType.one_drive);
                                }
                                //GOOGLEDRIVE
                                if (type.Equals(fileSystem.utils.FileSysConstant.ExternalRepoName[2]))
                                {
                                    return new Space(item.RepoId, NxlFileSpaceType.google_drive);
                                }
                                //BOX
                                if (type.Equals(fileSystem.utils.FileSysConstant.ExternalRepoName[3]))
                                {
                                    return new Space(item.RepoId, NxlFileSpaceType.box);
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

        public bool CheckNxlFileExists()
        {
            var destPath = CurrentSelectedSavePath;
            if (destPath == null)
            {
                throw new Exception("Invalid selected path.");
            }
            IFileRepo destRepo = GetDestRepo(destPath);
            if (destRepo == null)
            {
                throw new Exception("Invalid selected path, failed to get dest file repo.");
            }
            var fileName = FileName;
            var destPathId = GetDestPathId(destPath);
            if (fileName == null || destPathId == null)
            {
                throw new Exception("Invalid checking params.");
            }
            var pathId = "";
            if (destPathId.EndsWith("/"))
            {
                pathId = destPathId + fileName;
            }
            else
            {
                pathId = destPathId + "/" + fileName;
            }
            return destRepo.CheckFileExists(pathId);
        }

        private IFileRepo GetDestRepo(CurrentSelectedSavePath path)
        {
            IFileRepo destRepo = null;
            foreach (var item in TreeList)
            {
                if (item == null)
                {
                    continue;
                }
                if (item is ProjectRepo)
                {
                    var fp = (item as ProjectRepo).FilePool;
                    if (fp == null)
                    {
                        continue;
                    }
                    foreach (var p in fp)
                    {
                        if (p == null)
                        {
                            continue;
                        }
                        if (p.ProjectInfo == null)
                        {
                            continue;
                        }
                        if (p.ProjectInfo.ProjectId.ToString().Equals(path.OwnerId))
                        {
                            (item as ProjectRepo).CurrentWorkingProject = p;
                            break;
                        }
                    }
                }
                if (item is WorkSpaceRepo)
                {
                    if (item.RepoDisplayName.Equals(path.RepoName))
                    {
                        destRepo = item;
                        break;
                    }
                }
                if (item.RepoId.Equals(path.OwnerId))
                {
                    destRepo = item;
                    break;
                }
            }
            return destRepo;
        }

        class Space
        {
            string id;
            NxlFileSpaceType type;

            public Space(string id, NxlFileSpaceType type)
            {
                this.id = id;
                this.type = type;
            }

            public string Id { get => id; }

            public NxlFileSpaceType Type { get => type; }
        }
        #endregion
    }
}
