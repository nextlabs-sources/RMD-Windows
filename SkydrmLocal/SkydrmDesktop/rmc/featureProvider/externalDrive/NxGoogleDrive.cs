using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Alphaleonis.Win32.Filesystem;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SkydrmDesktop.Resources.languages;
using SkydrmDesktop.rmc.featureProvider.externalDrive.errorHandler;
using SkydrmDesktop.rmc.featureProvider.externalDrive.RmsRepoMgr;
using SkydrmDesktop.rmc.featureProvider.MessageNotify;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.common.interfaces;
using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.sdk;
using static Skydrmlocal.rmc.database2.FunctionProvider;

namespace SkydrmDesktop.rmc.featureProvider.externalDrive
{
    public delegate void OnSyncFromRemoteComplete(bool bSuc, List<IExternalDriveFile> ret);

    /// <summary>
    /// GoogleDrive rest api v3 reference document link is:
    /// ---- https://developers.google.com/drive/api/v3/reference/files/list
    /// </summary>
    public class NxGoogleDrive : IExternalDrive
    {
        private readonly SkydrmApp app = SkydrmApp.Singleton;
        private readonly log4net.ILog log;

        private IRmsRepo rmsRepo;

        public string WorkingPath { get; }
        public RestApiSdk sdk { get; }
        
        public NxGoogleDrive(IRmsRepo repo)
        {
            this.rmsRepo = repo;

            this.log = app.Log;
            WorkingPath = app.User.WorkingFolder + "\\GoogleDrive\\" + repo.RepoId;
            Alphaleonis.Win32.Filesystem.Directory.CreateDirectory(WorkingPath);

            sdk = new RestApiSdk(rmsRepo);
        }

        public ExternalRepoType Type => rmsRepo.Type;

        public string DisplayName { get => rmsRepo.DisplayName; set => rmsRepo.DisplayName = value; }

        public string AccessToken
        {
            get => rmsRepo.Token;
            set => rmsRepo.Token = value;
        }

        public string RepoId => rmsRepo.RepoId;

        public IExternalDriveFile[] ListAllFiles()
        {
            try
            {
                var rt = new List<GooglDriveFile>();
                foreach (var i in app.DBFunctionProvider.ListGoogleDriveAllFile(RepoId))
                {
                    // required each new fill do auto-fix 
                    rt.Add(new GooglDriveFile(this, i));
                }
                return rt.ToArray();
            }
            catch (Exception e)
            {
                app.Log.Error(e.Message, e);
                throw;
            }
        }

        public IExternalDriveFile[] ListFiles(string folderId) // CloudPathId
        {
            try
            {
                var rt = new List<GooglDriveFile>();
                foreach (var i in app.DBFunctionProvider.ListGoogleDriveFile(RepoId, folderId))
                {
                    // required each new fill do auto-fix 
                    rt.Add(new GooglDriveFile(this, i));
                }
                return rt.ToArray();
            }
            catch (Exception e)
            {
                app.Log.Error(e.Message, e);
                throw;
            }
        }

        public IExternalDriveLocalFile[] ListAllLocalFiles()
        {
            try
            {
                var rt = new List<GoogleDriveLocalFile>();
                foreach (var i in app.DBFunctionProvider.ListGoogleDriveAllLocalFile(RepoId))
                {
                    rt.Add(new GoogleDriveLocalFile(this, i));
                }
                return rt.ToArray();
            }
            catch (Exception e)
            {
                app.Log.Error(e.Message, e);
                throw;
            }
        }

        public IExternalDriveLocalFile[] ListLocalFiles(string cloudPathid)
        {
            try
            {
                var rt = new List<GoogleDriveLocalFile>();
                foreach (var i in app.DBFunctionProvider.ListGoogleDriveLocalFile(RepoId, cloudPathid))
                {
                    if (!FileHelper.Exist(i.LocalPath))
                    {
                        app.DBFunctionProvider.DeleteGoogleDriveLocalFile(i.Id);
                        continue;
                    }
                    rt.Add(new GoogleDriveLocalFile(this, i));
                }
                return rt.ToArray();
            }
            catch (Exception e)
            {
                app.Log.Error(e.Message, e);
                throw;
            }
        }

        private void SetDisplayPath(string cloudPathid, List<GooglDriveFile> files)
        {
            foreach(var one in files)
            {
                if (cloudPathid == "/")
                {
                    if (one.IsFolder)
                        one.DisplayPath = "/" + one.Name + "/";
                    else
                        one.DisplayPath = "/" + one.Name;
                }
                else
                {
                    // Query parent folder display path from db.
                    string parentDisplayPath = app.DBFunctionProvider.GetGoogleDriveFileDisplayPath(RepoId, cloudPathid);
                    if (one.IsFolder)
                        one.DisplayPath = parentDisplayPath + one.Name + "/";
                    else
                        one.DisplayPath = parentDisplayPath + one.Name;
                }
            }
        }

        public IExternalDriveFile[] SyncFiles(string folderId)
        {
            // remote
            var remote = sdk.ListFilesEx(folderId);
            var local = ListFiles(folderId);

            SetDisplayPath(folderId, remote);

            // routine: delete file that had been del on remote but still in local
            var diffset = from i in local
                          let rIds = from j in remote select j.FileId
                          where !rIds.Contains(i.FileId)
                          select i;

            foreach (var i in diffset)
            {
                // Delete from db.
                app.DBFunctionProvider.DeleteGoogleDriveFile(RepoId, i.FileId);

                // if this file is a folder, remove all its sub file nodes.
                if (i.IsFolder)
                {
                    app.DBFunctionProvider.DeleteGoogleDriveFolderAndAllSubFiles(RepoId, i.CloudPathId);
                }
            }

            var ff = new List<InsertExternalDriveFile>();
            foreach (var f in FilterOutNotModified(local, remote.ToArray()))
            {
                ff.Add(new InsertExternalDriveFile()
                {
                    repoId = RepoId,
                    fileId = f.FileId,
                    isFolder = f.IsFolder ? 1 : 0,
                    name = f.Name,
                    size = f.Size,
                    modifiedTime = DateTimeHelper.DateTimeToTimestamp(f.ModifiedTme),
                    displaypath = f.DisplayPath,
                    cloudPathid = f.CloudPathId,
                    isNxlFile = f.IsNxlFile ? 1 : 0
                });
            }
            // Insert\update
            app.DBFunctionProvider.UpsertGoogleDriveFileBatchEx(ff.ToArray());

            // Insert faked root node
            app.DBFunctionProvider.InsertGoogleDriveFakedRoot(RepoId);

            return ListFiles(folderId);

        }

        public void Upload(string localPath, string name, string cloudPathId,
             bool isOverwrite = false, IUploadProgressCallback callback = null)
        {
            sdk.ResumableUpload(localPath, name, cloudPathId, isOverwrite, callback);
        }

        public IOfflineFile[] GetOfflines()
        {
            try
            {
                var rt = new List<IOfflineFile>();
                foreach (var i in app.DBFunctionProvider.ListGoogleDriveOfflineFile(RepoId))
                {
                    if (i.IsOffline && Alphaleonis.Win32.Filesystem.File.Exists(i.LocalPath))
                    {
                        rt.Add(new GooglDriveFile(this, i));
                    }
                }

                return rt.ToArray();

            }
            catch (Exception e)
            {
                app.Log.Error(e.Message, e);
                throw;
            }
        }

        public IPendingUploadFile[] GetPendingUploads()
        {
            try
            {
                IList<IPendingUploadFile> rt = new List<IPendingUploadFile>();
                foreach (var i in app.DBFunctionProvider.ListGoogleDriveAllLocalFile(RepoId))
                {
                    // auto fix
                    if (!FileHelper.Exist(i.LocalPath))
                    {
                        app.DBFunctionProvider.DeleteGoogleDriveLocalFile(i.Id);
                        continue;
                    }
                    if (IsMatchPendingUpload((EnumNxlFileStatus)i.OperationStatus))
                    {
                        rt.Add(new GoogleDriveLocalFile(this, i));
                    }
                }
                return rt.ToArray();

            }
            catch (Exception e)
            {
                app.Log.Error(e.Message, e);
                throw;
            }
        }

        public void OnHeartBeat()
        {
            throw new NotImplementedException();
        }

        public IExternalDriveLocalFile AddLocalFile(string ParentFolder, string filePath, List<FileRights> rights,
            WaterMarkInfo waterMark, Expiration expiration, UserSelectTags tags)
        {
            string newAddedName = string.Empty;
            try
            {
                // tell api to convert this this
                app.Log.Info("protect the file to external repo: " + filePath);

                string outPath = string.Empty;
                if (app.IsPersonRouter)
                {
                    // using myVault defult token.
                    outPath = app.Rmsdk.User.ProtectFile(filePath, rights,
                        waterMark, expiration, new UserSelectTags());
                }
                else
                {
                    // using system bucket token group.
                    outPath = app.Rmsdk.User.ProtectFileToSystemProject(app.SystemProject.Id, filePath,
                    rights, waterMark, expiration, tags);
                }


                newAddedName = Alphaleonis.Win32.Filesystem.Path.GetFileName(outPath);
                var newAddedFileSize = new Alphaleonis.Win32.Filesystem.FileInfo(outPath).Length;

                // store this file into db;
                app.Log.Info("store the new protected file into database");
                app.DBFunctionProvider.InertLocalFileToGoogleDrive(RepoId, ParentFolder,
                    newAddedName, outPath, (int)newAddedFileSize, Alphaleonis.Win32.Filesystem.File.GetLastAccessTime(outPath));

                // tell service mgr
                app.MessageNotify.NotifyMsg(newAddedName, CultureStringInfo.ApplicationFindResource("Notify_RecordLog_Protect_Succeed"),
                    EnumMsgNotifyType.LogMsg, MsgNotifyOperation.PROTECT, EnumMsgNotifyResult.Succeed, EnumMsgNotifyIcon.WaitingUpload);

                // return this local file to caller
                return ListLocalFiles(ParentFolder).First((i) =>
                {
                    return i.LocalDiskPath.Equals(outPath);
                });

            }
            catch (Exception e)
            {
                app.Log.Error("Failed to Protect the file" + e.Message, e);

                throw;
            }
        }

        #region Private methods

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
                        (long)i.Size != l.Size ||
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

        #endregion // Private methods.
    }

    public sealed class GooglDriveFile : ExternalDriveFile
    {
        private string repoId;
        private string cacheFolder;
        private string accessToken;

        private RestApiSdk sdk;
        public GooglDriveFile() { }
       
        public GooglDriveFile(NxGoogleDrive host) : base()
        {
            this.repoId = host.RepoId;
            this.accessToken = host.AccessToken;
          
            InitCacheFolder(host.WorkingPath);
        }

        // Construct from db data(Used for ListFile from db)
        public GooglDriveFile(NxGoogleDrive host,
            database.table.externalrepo.ExternalDriveFile raw) : base(raw)
        {
            this.repoId = host.RepoId;
            this.accessToken = host.AccessToken;

            this.sdk = host.sdk;
            InitCacheFolder(host.WorkingPath);
        }

        public override void DeleteItem()
        {
            // todo
            throw new NotImplementedException();
        }
        public override void Download()
        {
            if (IsFolder)
            {
                return;
            }

            // Since googleDrive have the same name folder, so we directly download the file
            // into the local directory which is created by its fId.
            var parentFolder = this.cacheFolder + @"\" + FileId;
            FileHelper.CreateDir_NoThrow(parentFolder);
            var localPath = parentFolder + @"\" + Name;

            UpdateStatus(EnumNxlFileStatus.Downloading);
            // delete previous file
            FileHelper.Delete_NoThrow(localPath, true);
            try
            {
                sdk.Download(localPath, FileId, Size);

                // update local path into db
                this.LocalPath = localPath;
                UpdateStatus(EnumNxlFileStatus.DownLoadedSucceed);
            }
            catch (Exception e)
            {
                UpdateStatus(EnumNxlFileStatus.DownLoadedFailed);
                // del 
                FileHelper.Delete_NoThrow(localPath);
                throw e;
            }
           
        }

        // Delete file from local.
        public override void RemoveFromLocal()
        {
            try
            {
                if (IsFolder)
                {
                    return;
                }

                // delete local
                if (!Alphaleonis.Win32.Filesystem.File.Exists(this.LocalPath))
                {
                    return;
                }
                try
                {
                    Alphaleonis.Win32.Filesystem.File.Delete(this.LocalPath);
                }
                catch (Exception e)
                {
                    SkydrmApp.Singleton.Log.Error(e.ToString());
                }

                // update file status -- also will update db
                Status = EnumNxlFileStatus.RemovedFromLocal;
            }
            catch (Exception e)
            {
                SkydrmApp.Singleton.Log.Error(e);
                throw;
            }
        }

        public override void Export(string destFolder)
        {
            throw new NotImplementedException();
        }

        protected override void UpdateOffline(bool offline)
        {
            if(Raw.IsOffline == offline)
            {
                return;
            }

            SkydrmApp.Singleton.DBFunctionProvider.UpdateGoogleDriveFileOffline(Raw.Id, offline);
            Raw.IsOffline = offline;
        }

        protected override void UpdateStatus(EnumNxlFileStatus newValue)
        {
            // Update vaultfile status in db.
            var app = SkydrmApp.Singleton;
            app.DBFunctionProvider.UpdateGoogleDriveFileStatus(Raw.Id, (int)newValue);
            Raw.Status = (int)newValue;
            if (Status == EnumNxlFileStatus.Online)
            {
                IsOffline = false;
            }
            if (Status == EnumNxlFileStatus.AvailableOffline)
            {
                IsOffline = true;
            }
        }

        protected override void UpdateLocalPath(string localPath)
        {
            if(Raw == null || Raw.LocalPath == localPath)
            {
                return;
            }
            // update db
            SkydrmApp.Singleton.DBFunctionProvider.UpdateGoogleDriveFileLocalPath(Raw.Id, localPath);
            // update cache
            Raw.LocalPath = localPath;
        }

        private void InitCacheFolder(string homePath)
        {
            this.cacheFolder = homePath;
            FileHelper.CreateDir_NoThrow(cacheFolder);
        }
    }

    public sealed class GoogleDriveLocalFile : ExternalDriveLocalFile
    {
        private SkydrmApp app = SkydrmApp.Singleton;
        private string repoId;
        private string accessToken;
        private RestApiSdk sdk;

        public GoogleDriveLocalFile(NxGoogleDrive host,
            database.table.externalrepo.ExternalDriveLocalFile raw):base(raw)
        {
            this.repoId = host.RepoId;
            this.accessToken = host.AccessToken;
            this.sdk = host.sdk;
        }

        public override EnumFileRepo FileRepo => EnumFileRepo.REPO_EXTERNAL_DRIVE;

        public override void RemoveFromLocal()
        {
            // delete at local disk
            if (FileHelper.Exist(raw.LocalPath))
            {
                FileHelper.Delete_NoThrow(raw.LocalPath);
            }
            else
            {
                app.Log.Warn("file to be del,but not in local, " + raw.LocalPath);
            }

            // remove from database
            app.DBFunctionProvider.DeleteGoogleDriveLocalFile(raw.Id);
        }

        public override void Upload(bool isOverWrite = false, IUploadProgressCallback callback = null)
        {
            if (string.IsNullOrEmpty(PathId))
            {
                throw new Exception("The parent pathId is empty");
            }
            try
            {
                // Invoke
                sdk.ResumableUpload(LocalDiskPath, Name, PathId, isOverWrite, callback);
                // delete from local file db
                app.DBFunctionProvider.DeleteGoogleDriveLocalFile(raw.Id);

                if (app.User.LeaveCopy)
                {
                    app.User.LeaveCopy_Feature.AddFile(LocalDiskPath);
                    FileHelper.Delete_NoThrow(LocalDiskPath);
                }
            }
            catch (Exception e)
            {
                app.MessageNotify.NotifyMsg(raw.Name, e.Message, EnumMsgNotifyType.LogMsg,
                    MsgNotifyOperation.UPLOAD, EnumMsgNotifyResult.Failed, EnumMsgNotifyIcon.WaitingUpload);

                throw;
            }
        }

        protected override void ChangeOperationStaus(EnumNxlFileStatus status)
        {
            if (raw.OperationStatus == (int)status)
            {
                return;
            }
            // change db
            app.DBFunctionProvider.UpdateGoogleDriveLocalFileStatus(raw.Id, (int)status);
            // update cache
            raw.OperationStatus = (int)status;
        }

        protected override string GetFileDisplayPath()
        {
            var folder = app.DBFunctionProvider.QueryGoogleDriveLocalFileDisplayPath(repoId, raw.ExternalDriveFileTablePk);
            if (!folder.EndsWith("/"))
            {
                folder += "/";
            }

            return folder + raw.Name;
        }

        protected override string GetFileCloudPathId() 
        {
            var folder = app.DBFunctionProvider.GetGoogleDriveFileCloudPathId(repoId, raw.ExternalDriveFileTablePk);
            if (!folder.EndsWith("/"))
            {
                folder += "/";
            }

            return folder;
        }
    }

    public sealed class RestApiSdk : ICancelable
    {
        private IRmsRepo rmsRepo;
        private readonly string BASE_URL = @"https://www.googleapis.com/drive/v3/files";
        private readonly string REQUEST_UPLOAD_URL = @"https://www.googleapis.com/upload/drive/v3/files?uploadType=resumable";

        private readonly int BUF_SIZE = 1024;
        private static int CHUNK_LIMIT = 262144; // 256kb(256*1024)
        private static int INCOMPLETE = 308;

        private string resumableSessionUri;
        private HttpClient httpClient;
        private System.Threading.CancellationTokenSource cancellationTokenSource;

        public RestApiSdk(IRmsRepo repo)
        {
            this.rmsRepo = repo;
            httpClient = new HttpClient();
            cancellationTokenSource = new System.Threading.CancellationTokenSource();
        }

        /*
        {
             "incompleteSearch": false,
             "files": [
              {
               "id": "1_C4Hc0n0ZScw90V1fHhfUPHR4uh7aaEi",
               "name": "test",
               "mimeType": "application/vnd.google-apps.folder",
               "modifiedTime": "2020-04-29T10:36:01.069Z"
              },
              {
               "id": "0B8lyHyLJIARec3RhcnRlcl9maWxl",
               "name": "Getting started",
               "mimeType": "application/pdf",
               "modifiedTime": "2020-03-04T03:28:18.003Z",
               "size": "1560010"
              },
              {
               "id": "1g7WbUeBfKM-92Wx7j8wj9TOYoiwzB32B",
               "name": "offline.png",
               "mimeType": "image/png",
               "modifiedTime": "2019-03-08T05:22:25.000Z",
               "size": "39905"
              },
              {
               "id": "1Xi256w04BKzjc3rZyeO9L8828AfJIW30",
               "name": "CannotView.png",
               "mimeType": "image/png",
               "modifiedTime": "2018-12-18T12:26:54.000Z",
               "size": "9231"
              }
             ]
       }*/


        // Sync operation.
        public List<GooglDriveFile> ListFilesEx(string pathId)
        {
            HttpResponseMessage response;

            // params
            bool isRoot;
            string q_parma = string.Empty;
            if (pathId == "/")
            {
                q_parma = "q=" + "'root' in parents and trashed != true";
                isRoot = true;
            }
            else
            {
                q_parma = "q=" + "'" + GetFileId(pathId) + "'" + " in parents and trashed != true";
                isRoot = false;
            }
            string fields_param = "fields=incompleteSearch,nextPageToken,files(id,name,mimeType,modifiedTime,size)";
            string pageSize_param = "pageSize=1000";
            string url = BASE_URL + "?" + q_parma + "&" + fields_param + "&" + pageSize_param;

            List<GooglDriveFile> rt = new List<GooglDriveFile>();
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", rmsRepo.Token);
                var task = httpClient.SendAsync(request);
                task.Wait();

                response = task.Result;
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var contentTask = response.Content.ReadAsStringAsync();
                    contentTask.Wait();

                    string content = contentTask.Result;
                    //Console.WriteLine(content);

                    // parse json
                    ParseJson(pathId, content, ref rt);

                } else if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new RepoApiException(response.ReasonPhrase, ErrorCode.AccessTokenExpired);
                }

                return rt;
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public void Download(string localPath, string fId,
            long length, int start = 0, bool bPartialDownload = false)
        {
            if (string.IsNullOrEmpty(localPath) || string.IsNullOrEmpty(fId))
            {
                throw new Exception("Invalid parameters in Download.");
            }

            var uri = BASE_URL + "/" + fId + "?alt=media";
            using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
            {
                if (bPartialDownload)
                {
                    request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(start, length);
                }

                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", rmsRepo.Token);
                var task = httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                task.Wait();

                HttpResponseMessage response = task.Result;
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new RepoApiException(response.ReasonPhrase, ErrorCode.AccessTokenExpired);
                }

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    using (FileStream fs = Alphaleonis.Win32.Filesystem.File.Open(localPath, FileMode.OpenOrCreate))
                    {
                        var content = response.Content;
                        using (var t = content.ReadAsStreamAsync())
                        {
                            t.Wait();
                            Stream s = t.Result;

                            byte[] buf = new byte[BUF_SIZE];
                            int readCount = 0;
                            while ((readCount = s.Read(buf, 0 , BUF_SIZE)) > 0)
                            {
                                fs.Write(buf, 0, readCount);
                            }
                        }
                    }
                }

            }
        }

        /// <summary>
        /// Reference to "https://developers.google.com/drive/api/v3/manage-uploads"
        ///   ---- Perform a resumable upload.
        /// </summary>
        public void ResumableUpload(string localPath, string name, string cloudPathId, 
            bool isOverwrite = false, IUploadProgressCallback callback = null)
        {
            long size = 0;
            using (FileStream fs = Alphaleonis.Win32.Filesystem.File.Open(localPath, FileMode.Open))
            {
                size = fs.Length;
            }

            try
            {
                // Session uri like following:
                // https://www.googleapis.com/upload/drive/v3/files?uploadType=resumable
                // &upload_id=AAANsUkPuvg1-ULb5-h50cRE31UuOgCcuE8xL4Zk5Vy-o_Nx2QQd2Ck95Ci6mU0wWEB2GJAPWEpxtMJuUr1hD9wNGUc
                //
                string sessionUri = RequestUploadUrl(localPath, cloudPathId, name, size);

                long uploadedNum = 0;
                for(long i = 1, j = CHUNK_LIMIT; i <= size; i += CHUNK_LIMIT)
                {
                    if(i + CHUNK_LIMIT >= size)
                    {
                        j = size - i + 1;
                    }

                    var status = UploadFileChunk(localPath, name, i - 1, j);

                    if((int)status == INCOMPLETE)
                    {
                        // progress
                        uploadedNum += j;
                        callback?.OnProgress(uploadedNum, size);

                        continue;
                    }
                    else if(status == HttpStatusCode.OK)
                    {
                        callback?.OnProgress(size, size);
                        callback?.OnComplete(true, localPath, null);
                    }
                    else if(status == HttpStatusCode.Created)
                    {
                        // todo
                    }
                    // Error handler
                    else if(status == HttpStatusCode.Unauthorized)
                    {
                        var e = new RepoApiException("GoogleDrive upload failed.", ErrorCode.AccessTokenExpired);
                        callback?.OnComplete(false, localPath, e);
                    } else
                    {
                        var e = new RepoApiException("GoogleDrive upload failed.", ErrorCode.Common);
                        callback?.OnComplete(false, localPath, e);
                    }                    
                }
                
            }
            catch (Exception e)
            {
                callback?.OnComplete(false, localPath, new RepoApiException(e.ToString(), ErrorCode.Common));
            }
        }

        private HttpStatusCode UploadFileChunk(string localPath, string name, long chunkStart, long uploadBytes)
        {
            if (string.IsNullOrEmpty(resumableSessionUri))
            {
                throw new Exception("resumable session uri is empty");
            }

            using (var request = new HttpRequestMessage(HttpMethod.Put, resumableSessionUri))
            using (FileStream fs = Alphaleonis.Win32.Filesystem.File.Open(localPath, FileMode.Open))
            {
                
                    fs.Seek(chunkStart, SeekOrigin.Begin);
                    var buffer = new byte[uploadBytes];
                    var readTask = fs.ReadAsync(buffer, 0, buffer.Length, cancellationTokenSource.Token);
                    readTask.Wait();

                    var readCount = readTask.Result;
                    if(readCount != 0)
                    {
                        request.Content = new ByteArrayContent(buffer);
                        //request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf"); 
                        request.Content.Headers.ContentLength = uploadBytes;
                        request.Content.Headers.ContentRange = new ContentRangeHeaderValue(chunkStart, chunkStart + uploadBytes - 1);

                        // send
                        var task = httpClient.SendAsync(request, cancellationTokenSource.Token);
                        task.Wait();

                        HttpResponseMessage response = task.Result;

                        return response.StatusCode;
                    }
                }
            

            throw new Exception("Upload file chunk failed!");
        }

        // First, send the initial request, then we should save the resumable session uri.
        private string RequestUploadUrl(string localPath, string cloudPathid, string name, long size)
        {

            string fid = GetFileId(cloudPathid);
            string fileMetaData = "";
            if (fid == "/") // root folder
            {
                // Build the metadata
                fileMetaData = @"{'name':'FILE_NAME'}";
                fileMetaData = fileMetaData.Replace("\'", "\"");
                fileMetaData = fileMetaData.Replace("FILE_NAME", name);
            }
            else
            {
                // Build the metadata
                fileMetaData = @"{'name':'FILE_NAME', 'parents': ['FOLDER_ID']}";
                fileMetaData = fileMetaData.Replace("\'", "\"");
                fileMetaData = fileMetaData.Replace("FILE_NAME", name);
                fileMetaData = fileMetaData.Replace("FOLDER_ID", fid);
            }

            byte[] body = System.Text.Encoding.Default.GetBytes(fileMetaData);

            using (var request = new HttpRequestMessage(HttpMethod.Post, REQUEST_UPLOAD_URL))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", rmsRepo.Token);

                //var mimeType = 
                request.Headers.Add("X-Upload-Content-Type", "text/plain");
                request.Headers.Add("X-Upload-Content-Length", size.ToString());

                Stream sBody = new MemoryStream(body);
                request.Content = new StreamContent(sBody);

                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                request.Content.Headers.ContentLength = body.Length;

                // send
                var task = httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                task.Wait();

                HttpResponseMessage response = task.Result;
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new RepoApiException(response.ReasonPhrase, ErrorCode.AccessTokenExpired);
                }

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    resumableSessionUri = response.Headers.Location.ToString();
                    Console.WriteLine(resumableSessionUri);
                }
            }

            return resumableSessionUri;
        }

        // Cancel upload operation.
        public void Cancel()
        {
            cancellationTokenSource?.Cancel();
        }

        private void ParseJson(string pathId, string jonsContent, ref List<GooglDriveFile> ret)
        {
            JObject jo = (JObject)JsonConvert.DeserializeObject(jonsContent);
            if (jo == null)
            {
                // todo, error handler.
                throw new Exception("Parse json failed.");
            }

            string incompleteSearch = string.Empty;
            if (jo.ContainsKey("incompleteSearch"))
            {
                incompleteSearch = jo["incompleteSearch"].ToString();
            }
            if (jo.ContainsKey("files"))
            {
                var ja = jo["files"].ToArray();
                foreach (var one in ja)
                {
                    string fid = string.Empty;
                    string name = string.Empty;
                    string mimeType = string.Empty;
                    string modified = string.Empty;
                    string size = string.Empty; // No this field for folder.

                    JObject j = (JObject)JsonConvert.DeserializeObject(one.ToString());
                    if (j.ContainsKey("id"))
                    {
                        fid = j["id"].ToString();
                    }
                    if (j.ContainsKey("name"))
                    {
                        name = j["name"].ToString();
                    }
                    if (j.ContainsKey("mimeType"))
                    {
                        mimeType = j["mimeType"].ToString();
                    }
                    if (j.ContainsKey("modifiedTime"))
                    {
                        modified = j["modifiedTime"].ToString();
                    }
                    if (j.ContainsKey("size"))
                    {
                        size = j["size"].ToString();
                    }

                    string cloudPathid = (pathId == "/") ? pathId + fid : pathId + "/" + fid;

                    var file = new GooglDriveFile();
                    {
                        file.FileId = fid;
                        file.Name = name;
                        file.IsFolder = mimeType.Equals("application/vnd.google-apps.folder") ? true : false;
                        file.ModifiedTme = DateTime.Parse(modified);
                        file.Size = string.IsNullOrEmpty(size) ? 0 : long.Parse(size);
                        file.IsNxlFile = name.EndsWith(".nxl") ? true : false;
                        file.CustomString = mimeType; 
                        file.CloudPathId = cloudPathid; // like: '/1_C4Hc0n0ZScw90V1fHhfUPHR4uh7aaEi'
                        file.DisplayPath = ""; // the caller will set this field.
                    };

                    ret.Add(file);

                }
            }
        }

        private string GetFileId(string pathid)
        {
            if (pathid == "/")
                return pathid;

            if (pathid.Length > 0 && pathid.EndsWith("/"))
            {
                int pos = pathid.LastIndexOf("/");
                var removeLastSlash = pathid.Substring(0, pos);
                return removeLastSlash.Substring(removeLastSlash.LastIndexOf("/") + 1);
            }
            else
            {
                return pathid.Substring(pathid.LastIndexOf("/") + 1);
            }
        }
    }
}
