using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SkydrmDesktop.Resources.languages;
using SkydrmDesktop.rmc.database.table.externalrepo.sharepoint;
using SkydrmDesktop.rmc.featureProvider.externalDrive.RmsRepoMgr;
using SkydrmDesktop.rmc.featureProvider.externalDrive.sharepoint;
using SkydrmDesktop.rmc.featureProvider.MessageNotify;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.sdk;

namespace SkydrmDesktop.rmc.featureProvider.externalDrive
{
    public abstract class NxSharePointBase : IExternalDrive
    {
        protected List<IExternalDriveLocalFile> mLocalData = new List<IExternalDriveLocalFile>();

        private readonly Dictionary<string, string> mCloudPathIdContainer = new Dictionary<string, string>();
        private readonly Dictionary<string, bool> mSiteContainer = new Dictionary<string, bool>();

        private readonly ReaderWriterLockSlim mLocalDataLock = new ReaderWriterLockSlim();

        private readonly ReaderWriterLockSlim mCloudPathIdContainerLock = new ReaderWriterLockSlim();
        private readonly ReaderWriterLockSlim mSiteContainerLock = new ReaderWriterLockSlim();

        private readonly IRmsRepo mRmsRepo;

        protected string mServerSite;

        public string WorkingPath { get; set; }

        public NxSharePointBase(IRmsRepo repo)
        {
            mRmsRepo = repo;
            var serversite = repo.AccountName;
            this.mServerSite = string.IsNullOrEmpty(serversite) ? GetDefaultServerSite() : serversite;
        }

        public abstract string GetDefaultServerSite();

        public abstract IExternalDriveFile[] SyncInternal(string pathId, bool site);

        protected abstract IExternalDriveFile NewByDBItem(SharePointDriveFile file);

        protected abstract IExternalDriveLocalFile NewByDBLocalItem(SharePointDriveLocalFile file);

        protected abstract string DownloadFile(string pathId, string localPath, bool overwrite, int start, long totalLength,
            bool bPartialDownload,
            IProgress<HttpDownloadProgress> callback,
            CancellationToken cancellation);

        protected abstract string UploadFile(string pathId, string fileName, string localpath, bool overwrite);

        public ExternalRepoType Type => mRmsRepo.Type;

        public string DisplayName { get => mRmsRepo.DisplayName; set => mRmsRepo.DisplayName = value; }

        public string AccessToken { get => mRmsRepo.Token; set => mRmsRepo.Token = value; }

        public string RepoId => mRmsRepo.RepoId;

        protected bool SetCloudPathId(string pathId, string cloudPathId)
        {
            if (string.IsNullOrEmpty(pathId))
            {
                return false;
            }
            try
            {
                mCloudPathIdContainerLock.EnterWriteLock();

                if (mCloudPathIdContainer.ContainsKey(pathId))
                {
                    mCloudPathIdContainer[pathId] = cloudPathId;
                }
                else
                {
                    mCloudPathIdContainer.Add(pathId, cloudPathId);
                }

                return true;
            }
            finally
            {
                mCloudPathIdContainerLock.ExitWriteLock();
            }
        }

        protected string GetCloudPathIdByPathId(string pathId)
        {
            var rt = "/";
            if (string.IsNullOrEmpty(pathId))
            {
                return rt;
            }
            if (pathId.Equals("/"))
            {
                return rt;
            }
            return SkydrmApp.Singleton.DBFunctionProvider.QuerySharePointDriveFilePathId(RepoId, pathId);
        }

        protected bool SetSite(string pathId, bool site)
        {
            if (string.IsNullOrEmpty(pathId))
            {
                return false;
            }
            try
            {
                mSiteContainerLock.EnterWriteLock();

                if (mSiteContainer.ContainsKey(pathId))
                {
                    mSiteContainer[pathId] = site;
                }
                else
                {
                    mSiteContainer.Add(pathId, site);
                }
                return true;
            }
            finally
            {
                mSiteContainerLock.ExitWriteLock();
            }
        }

        protected bool IsSite(string pathId)
        {
            if (string.IsNullOrEmpty(pathId))
            {
                return false;
            }
            if (pathId.Equals("/"))
            {
                return false;
            }
            try
            {
                mSiteContainerLock.EnterReadLock();

                mSiteContainer.TryGetValue(pathId, out bool site);

                return site;
            }
            finally
            {
                mSiteContainerLock.ExitReadLock();
            }
        }

        public IExternalDriveFile[] SyncFiles(string folderId)
        {
            var remotes = SyncInternal(folderId, IsSite(folderId));
            var locals = ListFiles(folderId);

            // routine: delete file that had been del on remote but still in local
            var diffset = from i in locals
                          let rIds = from j in remotes select j.FileId
                          where !rIds.Contains(i.FileId)
                          select i;

            foreach (var i in diffset)
            {
                DeleteLocal(i.CloudPathId, i.IsFolder);
            }

            SetLocal(FilterOutNotModified(locals, remotes));

            return ListFiles(folderId);
        }

        public IExternalDriveFile[] ListAllFiles()
        {
            return ListInternal("/", false);
        }

        public IExternalDriveFile[] ListFiles(string folderId)
        {
            return ListInternal(folderId, false);
        }

        public IExternalDriveLocalFile[] ListAllLocalFiles()
        {
            var locals = SkydrmApp.Singleton
                .DBFunctionProvider
                .QuerySharePointDriveAllLocalFile(RepoId);
            if (locals == null || locals.Length == 0)
            {
                return new IExternalDriveLocalFile[0];
            }
            List<IExternalDriveLocalFile> ret = new List<IExternalDriveLocalFile>();
            foreach (var item in locals)
            {
                if (item == null)
                {
                    continue;
                }
                ret.Add(NewByDBLocalItem(item as SharePointDriveLocalFile));
            }
            return ret.ToArray();
        }

        public IExternalDriveLocalFile[] ListLocalFiles(string folderId)
        {
            var locals = SkydrmApp.Singleton
                .DBFunctionProvider
                .QuerySharePointDriveLocalFile(RepoId, folderId);
            if (locals == null || locals.Length == 0)
            {
                return new IExternalDriveLocalFile[0];
            }
            List<IExternalDriveLocalFile> ret = new List<IExternalDriveLocalFile>();
            foreach (var item in locals)
            {
                if (item == null)
                {
                    continue;
                }
                ret.Add(NewByDBLocalItem(item as SharePointDriveLocalFile));
            }
            return ret.ToArray();
        }

        public IOfflineFile[] GetOfflines()
        {
            var local = SkydrmApp.Singleton
                .DBFunctionProvider
                .QuerySharePointOfflineFile(RepoId);
            if (local == null || local.Length == 0)
            {
                return new IOfflineFile[0];
            }
            var rt = new List<IOfflineFile>();
            foreach (var i in local)
            {
                if (i == null)
                {
                    continue;
                }
                if (!Alphaleonis.Win32.Filesystem.File.Exists(i.LocalPath))
                {
                    continue;
                }
                rt.Add(NewByDBItem(i) as ExternalDriveFile);
            }
            return rt.ToArray();
        }

        public IPendingUploadFile[] GetPendingUploads()
        {
            var locals = SkydrmApp.Singleton
                .DBFunctionProvider
                .QuerySharePointDriveAllLocalFile(RepoId);
            if (locals == null || locals.Length == 0)
            {
                return new IPendingUploadFile[0];
            }
            IList<IPendingUploadFile> rt = new List<IPendingUploadFile>();
            foreach (var i in locals)
            {
                if (i == null)
                {
                    continue;
                }
                // auto fix
                if (!FileHelper.Exist(i.LocalPath))
                {
                    SkydrmApp.Singleton
                        .DBFunctionProvider
                        .DeleteSharePointDriveLocalFile(i.Id);
                    continue;
                }
                if (IsMatchPendingUpload((EnumNxlFileStatus)i.OperationStatus))
                {
                    rt.Add(NewByDBLocalItem(i as SharePointDriveLocalFile));
                }
            }
            return rt.ToArray();
        }

        public IExternalDriveLocalFile AddLocalFile(string ParentFolder, string filePath,
            List<FileRights> rights, WaterMarkInfo waterMark, Expiration expiration,
            UserSelectTags tags)
        {
            string newAddedName = "";

            string outPath = string.Empty;
            if (SkydrmApp.Singleton.IsPersonRouter)
            {
                // using myVault defult token.
                outPath = SkydrmApp.Singleton.Rmsdk.User.ProtectFile(filePath, rights,
                    waterMark, expiration, new UserSelectTags());
            }
            else
            {
                // using system bucket token group.
                outPath = SkydrmApp.Singleton.Rmsdk.User.ProtectFileToSystemProject(SkydrmApp.Singleton.SystemProject.Id, filePath,
                rights, waterMark, expiration, tags);
            }


            newAddedName = Alphaleonis.Win32.Filesystem.Path.GetFileName(outPath);
            var newAddedFileSize = new Alphaleonis.Win32.Filesystem.FileInfo(outPath).Length;

            // store this file into db;
            SkydrmApp.Singleton.DBFunctionProvider.InsertSharePointDriveLocalFile(RepoId, ParentFolder,
                    newAddedName, outPath, (int)newAddedFileSize, Alphaleonis.Win32.Filesystem.File.GetLastAccessTime(outPath));

            // tell service mgr
            SkydrmApp.Singleton.MessageNotify.NotifyMsg(newAddedName, CultureStringInfo.ApplicationFindResource("Notify_RecordLog_Protect_Succeed"),
                EnumMsgNotifyType.LogMsg, MsgNotifyOperation.PROTECT, EnumMsgNotifyResult.Succeed, EnumMsgNotifyIcon.WaitingUpload);

            // return this local file to caller
            return ListLocalFiles(ParentFolder).First((i) =>
            {
                return i.LocalDiskPath.Equals(outPath);
            });
        }

        public void Upload(string localPath, string name, string cloudPathId,
            bool isOverwrite = false, IUploadProgressCallback callback = null)
        {
            UploadFile(cloudPathId, name, localPath, isOverwrite);
        }

        public void Download(string localPath, string pathId, long length, int start = 0, bool bPartialDownload = false)
        {
            var cts = new CancellationTokenSource();
            cts.Token.Register(() =>
            {
                Console.WriteLine("Download file has been canceled.");
            });
            DownloadFile(GetCloudPathIdByPathId(pathId), localPath, false, start, length, bPartialDownload, null, cts.Token);
        }

        public void OnHeartBeat()
        {

        }

        private bool IsMatchPendingUpload(EnumNxlFileStatus status)
        {
            if (status == EnumNxlFileStatus.WaitingUpload ||
                status == EnumNxlFileStatus.Uploading ||
                status == EnumNxlFileStatus.UploadFailed ||
                status == EnumNxlFileStatus.UploadSucceed
                )
            {
                return true;
            }
            return false;
        }

        private IExternalDriveFile[] FilterOutNotModified(IExternalDriveFile[] locals, IExternalDriveFile[] remotes)
        {
            if (locals.Length == 0)
            {
                return remotes;
            }

            var rt = new List<IExternalDriveFile>();
            foreach (var i in remotes)
            {
                try
                {
                    // If use Enumerable.First(), will throw exception when no matching element. 
                    // It will cause blocking UI when switch treeView item during uploading many files.
                    var l = locals.FirstOrDefault((j) =>
                    {
                        if (i.FileId != j.FileId)
                        {
                            return false;
                        }
                        return true;
                    });

                    // If no matching element, will return null.
                    if (l == null)
                    {
                        // remote added node, should add into local
                        rt.Add(i);
                        continue;
                    }

                    // Modified in remote, local node should also update.
                    if (i.Name != l.Name ||
                        i.Size != l.Size ||
                        i.ModifiedTme != l.ModifiedTme)
                    {
                        rt.Add(i);
                    }

                }
                catch (Exception e)
                {
                    // local find error
                    rt.Add(i);
                }
            }

            return rt.ToArray();
        }

        private IExternalDriveFile[] ListInternal(string pathId, bool recursively)
        {
            return QueryLocal(pathId);
        }

        private IExternalDriveFile[] QueryLocal(string pathId)
        {
            var local = SkydrmApp.Singleton
                   .DBFunctionProvider
                   .QuerySharePointFile(RepoId, pathId);
            if (local == null || local.Length == 0)
            {
                return new IExternalDriveFile[0];
            }
            List<IExternalDriveFile> rt = new List<IExternalDriveFile>();
            foreach (var i in local)
            {
                if (i == null)
                {
                    continue;
                }
                rt.Add(NewByDBItem(i));
            }
            return rt.ToArray();
        }

        private void SetLocal(IExternalDriveFile[] data)
        {
            if (data == null)
            {
                return;
            }
            List<SharePointDriveFile> upserts = new List<SharePointDriveFile>();
            foreach (var item in data)
            {
                if (item == null)
                {
                    continue;
                }
                if (item is SharePointBaseFile file)
                {
                    upserts.Add(SharePointDriveFile.NewByRemote(file.FileId, file.IsFolder, file.IsSite,
                        file.Name, file.Size, file.LastModifiedTime,
                        file.FileId, file.DisplayPath, file.CloudPathId,file.IsNxlFile));
                }
            }

            SkydrmApp.Singleton
                 .DBFunctionProvider
                 .InsertSharePointDriveFileRoot(RepoId);

            SkydrmApp.Singleton
                    .DBFunctionProvider
                    .UpsertSharePointDriveFile(RepoId, upserts.ToArray());
        }

        private void DeleteLocal(string pathId, bool recursively)
        {
            if (string.IsNullOrEmpty(pathId))
            {
                return;
            }
            if (recursively)
            {
                SkydrmApp.Singleton
                    .DBFunctionProvider
                    .DeleteSharePointDriveFolder(RepoId, pathId);
            }
            else
            {
                SkydrmApp.Singleton
                .DBFunctionProvider
                .DeleteSharePointDriveFile(RepoId, pathId);
            }
        }

        private string QueryServerSite(string repoId)
        {
            return SkydrmApp.Singleton.DBFunctionProvider
                .QueryExternalRepoReserved1(repoId);
        }

        protected string GetRootUrl(string serverSite, bool site)
        {
            string url;
            if (site)
            {
                url = GetRootSitesUrl(serverSite);
            }
            else
            {
                url = GetRootFolderUrl(serverSite);
            }
            if (url.Contains(" "))
            {
                url = url.Replace(" ", "%20");
            }
            return url;
        }

        protected string GetChildSitesUrl(string cloudPath)
        {
            string url = GetChildSiteUrl(cloudPath);
            if (url.Contains(" "))
            {
                url = url.Replace(" ", "%20");
            }
            return url;
        }

        protected string GetFileListsUrl(string cloudPath)
        {
            string url = GetFileLists(cloudPath);
            if (url.Contains(" "))
            {
                url = url.Replace(" ", "%20");
            }
            return AmendServerUrl(url);
        }

        protected string GetFoldersUrl(string cloudPath)
        {
            string url = GetFolderUrlInternal(cloudPath);
            if (url.Contains(" "))
            {
                url = url.Replace(" ", "%20");
            }
            return url;
        }

        protected string GetFilesUrl(string cloudPath)
        {
            string url = GetFilesUrlInternal(cloudPath);
            if (url.Contains(" "))
            {
                url = url.Replace(" ", "%20");
            }
            return url;
        }

        protected string GetDownloadUrl(string cloudPath)
        {
            string url = cloudPath + "/$value";
            if (url.Contains(" "))
            {
                url = url.Replace(" ", "%20");
            }
            return url;
        }

        protected static string GetCurrentUserUrl(string cloudPath)
        {
            string url = GetCurrentUsrInfoUrl(cloudPath);
            if (url.Contains(" "))
            {
                url = url.Replace(" ", "%20");
            }
            return url;
        }

        protected string GetUploadFileUrl(string cloudPath, string name, bool overwrite)
        {
            string url = GetUploadUrl(cloudPath, name, overwrite);
            if (url.Contains(" "))
            {
                url = url.Replace(" ", "%20");
            }
            return url;
        }

        private string AmendServerUrl(string url)
        {
            if (url.EndsWith("/"))
            {
                return url;
            }
            else
            {
                return url + "/";
            }
        }

        private string GetRootFolderUrl(string serverSite)
        {
            if (string.IsNullOrEmpty(serverSite))
            {
                return "";
            }
            if (serverSite.EndsWith("/"))
            {
                return serverSite + "_api/web/lists?$filter=BaseTemplate eq 101&$select=Title,Created,RootFolder";
            }
            return serverSite + "/_api/web/lists?$filter=BaseTemplate eq 101&$select=Title,Created,RootFolder";
        }

        private string GetRootSitesUrl(string serverSite)
        {
            if (string.IsNullOrEmpty(serverSite))
            {
                return "";
            }
            if (serverSite.EndsWith("/"))
            {
                return serverSite + "_api/web/webs?$select=Title,Created";
            }
            return serverSite + "/_api/web/webs?$select=Title,Created";
        }

        private string GetChildSiteUrl(string cloudPath)
        {
            if (string.IsNullOrEmpty(cloudPath))
            {
                return "";
            }
            if (cloudPath.EndsWith("/"))
            {
                return cloudPath + "webs";
            }
            return cloudPath + "/webs";
        }

        private string GetFileLists(string cloudPath)
        {
            if (string.IsNullOrEmpty(cloudPath))
            {
                return "";
            }
            if (cloudPath.EndsWith("/"))
            {
                return cloudPath + "lists?$select=BaseTemplate,Title,Hidden,Id&$filter=BaseTemplate eq 101";
            }
            return cloudPath + "/lists?$select=BaseTemplate,Title,Hidden,Id&$filter=BaseTemplate eq 101";
        }

        private string GetFolderUrlInternal(string cloudPath)
        {
            if (string.IsNullOrEmpty(cloudPath))
            {
                return "";
            }
            if (cloudPath.EndsWith("/"))
            {
                return cloudPath + "Folders?$filter=Name ne 'Forms'";
            }
            return cloudPath + "/Folders?$filter=Name ne 'Forms'";
        }

        private string GetFilesUrlInternal(string cloudPath)
        {
            if (string.IsNullOrEmpty(cloudPath))
            {
                return "";
            }
            if (cloudPath.EndsWith("/"))
            {
                return cloudPath + "Files";
            }
            return cloudPath + "/Files";
        }

        private static string GetCurrentUsrInfoUrl(string serverSite)
        {
            if (string.IsNullOrEmpty(serverSite))
            {
                return "";
            }
            if (serverSite.EndsWith("/"))
            {
                return serverSite + "_api/web/CurrentUser";
            }
            return serverSite + "/_api/web/CurrentUser";
        }

        private string GetCurrentUsrInfoDetailUrl(string serverSite, string usrId)
        {
            if (string.IsNullOrEmpty(serverSite))
            {
                return "";
            }
            if (serverSite.EndsWith("/"))
            {
                return serverSite + "_api/web/SiteUserInfoList/Items" + "(" + usrId + ")";
            }
            return serverSite + "/_api/web/SiteUserInfoList/Items" + "(" + usrId + ")";
        }

        private string GetSiteQuotaUrl(string serverSite)
        {
            if (string.IsNullOrEmpty(serverSite))
            {
                return "";
            }
            if (serverSite.EndsWith("/"))
            {
                return serverSite + "_api/site/usage";
            }
            return serverSite + "/_api/site/usage";
        }

        private string GetUploadUrl(string serverSite, string fileName, bool overwrite)
        {
            if (string.IsNullOrEmpty(serverSite))
            {
                return "";
            }
            var format = "";
            if (serverSite.EndsWith("/"))
            {
                format = serverSite + "Files/add(url='s1',overwrite=s2)";
            }
            else
            {
                format = serverSite + "/Files/add(url='s1',overwrite=s2)";
            }
            return format.Replace("s1", fileName).Replace("s2", overwrite ? "true" : "false");
        }

        /**
         * https://docs.microsoft.com/en-us/sharepoint/dev/sp-add-ins/complete-basic-operations-using-sharepoint-rest-endpoints
         * This method is used to get the url that retrieve all the lists in a specific SharePoint site.
         *
         * @param serverSite site url
         * @return site lists url
         */
        private string GetSiteListsUrl(string serverSite)
        {
            if (string.IsNullOrEmpty(serverSite))
            {
                return "";
            }
            if (serverSite.EndsWith("/"))
            {
                return serverSite + "_api/web/lists";
            }
            return serverSite + "/_api/web/lists";
        }
    }
}
