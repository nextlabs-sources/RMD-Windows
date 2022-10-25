using Alphaleonis.Win32.Filesystem;
using Alphaleonis.Win32.Network;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SkydrmDesktop.Resources.languages;
using SkydrmDesktop.rmc.featureProvider.externalDrive.errorHandler;
using SkydrmDesktop.rmc.featureProvider.externalDrive.RmsRepoMgr;
using SkydrmDesktop.rmc.featureProvider.MessageNotify;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Skydrmlocal.rmc.database2.FunctionProvider;

namespace SkydrmDesktop.rmc.featureProvider.externalDrive
{
    class NxDropBox : IExternalDrive
    {
        private readonly SkydrmApp app = SkydrmApp.Singleton;
        private readonly log4net.ILog log;
        private IRmsRepo rmsRepo;

        public EnumNxlFileStatus Status { get; set; } = EnumNxlFileStatus.Online;

        public ExternalRepoType Type => rmsRepo.Type;

        public string DisplayName { get => rmsRepo.DisplayName; set => rmsRepo.DisplayName = value; }

        public string AccessToken { get => rmsRepo.Token; set => rmsRepo.Token = value; }

        public string WorkingPath { get; }


        public string RepoId => rmsRepo.RepoId;

        public NxDropBox(IRmsRepo repo)
        {
            this.rmsRepo = repo;
            this.log = app.Log;
            WorkingPath = app.User.WorkingFolder + "\\DropBox\\" + repo.RepoId;
            Alphaleonis.Win32.Filesystem.Directory.CreateDirectory(WorkingPath);
            app.DBFunctionProvider.InsertDropBoxFakedRoot(RepoId);
        }

        public IExternalDriveFile[] ListAllFiles()
        {
            return ListFiles("/");
        }


        public IExternalDriveFile[] ListFiles(string folderId)
        {
            try
            {
                var rt = new List<NxDropBoxFile>();
                foreach (var i in app.DBFunctionProvider.ListDropBoxFile(RepoId, folderId))
                {
                    rt.Add(new NxDropBoxFile(this, i));// required each new fill do auto-fix 
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
                var rt = new List<NxDropBoxLocalFile>();
                foreach (var i in app.DBFunctionProvider.ListDropBoxAllLocalFile(RepoId))
                {
                    rt.Add(new NxDropBoxLocalFile(this, i));
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
                var rt = new List<NxDropBoxLocalFile>();
                foreach (var i in app.DBFunctionProvider.ListDropBoxLocalFile(RepoId, cloudPathid))
                {
                    if (!FileHelper.Exist(i.LocalPath))
                    {
                        app.DBFunctionProvider.DeleteDropBoxLocalFile(i.Id);
                        continue;
                    }
                    rt.Add(new NxDropBoxLocalFile(this, i));
                }
                return rt.ToArray();
            }
            catch (Exception e)
            {
                app.Log.Error(e.Message, e);
                throw;
            }
        }

        public IExternalDriveFile[] SyncFiles(string folderId)
        {

            var remote = Impl_SyncFiles(folderId);
            var local = ListFiles(folderId);

            // routine: delete file that had been del on remote but still in local
            var diffset = from i in local
                          let rIds = from j in remote select j.FileId
                          where !rIds.Contains(i.FileId)
                          select i;

            foreach (var i in diffset)
            {
                // Delete from db.
                app.DBFunctionProvider.DeleteDropBoxFile(RepoId, i.FileId);

                // if this file is a folder, remove all its sub file nodes.
                if (i.IsFolder)
                {
                    app.DBFunctionProvider.DeleteDropBoxFolderAndAllSubFiles(RepoId, i.DisplayPath);
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

            app.DBFunctionProvider.UpsertDropBoxFileBatchEx(ff.ToArray());

            return ListFiles(folderId);
        }

        public IOfflineFile[] GetOfflines()
        {
            try
            {
                var rt = new List<IOfflineFile>();
                foreach (var i in app.DBFunctionProvider.ListDropBoxOfflineFile(RepoId))
                {
                    if (i.IsOffline && Alphaleonis.Win32.Filesystem.File.Exists(i.LocalPath))
                    {
                        rt.Add(new NxDropBoxFile(this, i));
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
                foreach (var i in app.DBFunctionProvider.ListDropBoxAllLocalFile(RepoId))
                {
                    // auto fix
                    if (!FileHelper.Exist(i.LocalPath))
                    {
                        app.DBFunctionProvider.DeleteDropBoxLocalFile(i.Id);
                        continue;
                    }
                    if (IsMatchPendingUpload((EnumNxlFileStatus)i.OperationStatus))
                    {
                        rt.Add(new NxDropBoxLocalFile(this, i));
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

        public IExternalDriveLocalFile AddLocalFile(string ParentFolder, string filePath, List<FileRights> rights, WaterMarkInfo waterMark, Expiration expiration, UserSelectTags tags)
        {
            string newAddedName = string.Empty;
            try
            {
                // tell api to convert this this
                app.Log.Info("protect the file to external repo: " + filePath);

                string outPath = string.Empty;
                if (app.IsPersonRouter)
                {
                    outPath = app.Rmsdk.User.ProtectFile(filePath, rights,
                        waterMark, expiration, new UserSelectTags());
                }
                else
                {
                    outPath = app.Rmsdk.User.ProtectFileToSystemProject(app.SystemProject.Id, filePath,
                    rights, waterMark, expiration, tags);
                }


                newAddedName = Alphaleonis.Win32.Filesystem.Path.GetFileName(outPath);
                var newAddedFileSize = new Alphaleonis.Win32.Filesystem.FileInfo(outPath).Length;

                // store this file into db;
                app.Log.Info("store the new protected file into database");
                app.DBFunctionProvider.InsertLocalFileToDropBox(RepoId, ParentFolder,
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

        public void OnHeartBeat()
        {
            // todo: waiting for another requirements
            //ListFiles("/");
        }

        private IExternalDriveFile[] Impl_SyncFiles(string folderId)
        {
            if (folderId == "/")
            {
                folderId = "";
            }

            // global config
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            // config request, verb+uril+header
            string PostParam = "{ \"path\" : \"[FolderID]\" }";
            PostParam = PostParam.Replace("[FolderID]", folderId);
            using (HttpRequestMessage request = new HttpRequestMessage())
            using (HttpClient client = new HttpClient())
            {
                request.Method = HttpMethod.Post;
                request.Headers.Add("Authorization", "Bearer " + AccessToken);
                request.RequestUri = new UriBuilder()
                {
                    Scheme = "https",
                    Host = "api.dropboxapi.com",
                    Path = "/2/files/list_folder",
                    //Query = "fields=modified_at,name,size&limit=1000"            
                }.Uri;
                request.Content = new StringContent(PostParam, Encoding.UTF8, "application/json");

                var rt = client.SendAsync(request).Result;
                if(rt.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new RepoApiException(rt.ReasonPhrase, ErrorCode.AccessTokenExpired);
                }
                // assume rt is success
                var response = rt.Content;
                // extract string from the http_response
                var str = response.ReadAsStringAsync().Result;
                return ParseFoldersFromJson(str);
            }

        }

        private IExternalDriveFile[] ParseFoldersFromJson(string json)
        {
            JObject root = JObject.Parse(json);
            if (root == null)
            {
                return null;
            }

            if (!root.ContainsKey("entries"))
            {
                // syntax error, no any entries
                return new IExternalDriveFile[0];
            }

            var rt = new List<NxDropBoxFile>();

            JArray array = root["entries"] as JArray;
            foreach (var i in array)
            {
                var item = new NxDropBoxFile(this);
                item.FileId = (string)i["id"];
                item.IsFolder = String.Equals((string)i[".tag"], "folder", StringComparison.CurrentCultureIgnoreCase);

                item.IsOffline = false;
                item.IsFavorite = false;

                item.Name = (string)i["name"];
                if (!item.IsFolder)
                {
                    item.Size = long.Parse((string)i["size"]);
                    item.ModifiedTme = DateTime.Parse((string)i["server_modified"]);
                }
                else
                {
                    // folder
                    item.Size = 0;
                    item.ModifiedTme = DateTime.Now; // folder do not have last modified time;

                }

                item.LocalPath = "local path";
                item.DisplayPath = (string)i["path_display"];

                item.CustomString = "";
                item.CloudPathId = (string)i["path_lower"];
                item.IsNxlFile = false;

                rt.Add(item);
            }

            return rt.ToArray();
        }

        class UploadPostParam
        {
            public UploadPostParam()
            {
                mode = "add";
                autorename = false;
                mute = false;
                strict_conflict = false;
            }

            public string path { get; set; }
            public string mode { get; set; }
            public bool autorename { get; set; }
            public bool mute { get; set; }
            public bool strict_conflict { get; set; }



            public string ToJsonString()
            {
                string j = JsonConvert.SerializeObject(this);
                byte[] bs = Encoding.Default.GetBytes(j);
                j = Encoding.UTF8.GetString(bs);
                return j;
            }
            public override string ToString()
            {
                return ToJsonString();
            }
        }

        private bool Upload_Impl(string localPath, string name, string cloudPathId)
        {

            // config request, verb+uril+header
            UploadPostParam param = new UploadPostParam();
            if (cloudPathId == "/")
            {
                param.path = "/" + name;
            }
            else
            {
                param.path = cloudPathId + "/" + name;
            }

            using (FileStream fs = new FileStream(localPath, FileMode.Open))
            using (HttpRequestMessage request = new HttpRequestMessage())
            using (HttpClient client = new HttpClient())
            {

                request.Method = HttpMethod.Post;
                request.Headers.Add("Authorization", "Bearer " + AccessToken);
                request.Headers.Add("Dropbox-API-Arg", param.ToJsonString());
                request.RequestUri = new UriBuilder()
                {
                    Scheme = "https",
                    Host = "content.dropboxapi.com",
                    Path = "/2/files/upload",
                }.Uri;
                // upload request send file'content in request.content
                request.Content = new StreamContent(fs);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                var rt = client.SendAsync(request).Result;
                if (rt.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new RepoApiException(rt.ReasonPhrase, ErrorCode.AccessTokenExpired);
                }

                // assume rt is success
                var response = rt.Content;
                // extract string from the http_response
                var str = response.ReadAsStringAsync().Result;

                return true;
            }
        }

        public void Upload(string localPath, string name, string cloudPathId, bool isOverwrite = false, IUploadProgressCallback callback = null)
        {
            Upload_Impl(localPath, name, cloudPathId);
        }
    }


    sealed class NxDropBoxFile : ExternalDriveFile
    {

        private NxDropBox DropBoxHost;


        public NxDropBoxFile(NxDropBox host) : base()
        {
            DropBoxHost = host;
        }

        public NxDropBoxFile(NxDropBox host, database.table.externalrepo.ExternalDriveFile raw) : base(raw)
        {
            DropBoxHost = host;
        }

        class DeletePostParam
        {
            public string path { get; set; }

            public string ToJsonString()
            {
                string j = JsonConvert.SerializeObject(this);
                byte[] bs = Encoding.Default.GetBytes(j);
                j = Encoding.UTF8.GetString(bs);
                return j;
            }
            public override string ToString()
            {
                return ToJsonString();
            }
        }

        public bool Delete_Impl()
        {
            DeletePostParam post_param = new DeletePostParam()
            {
                path = FileId
            };

            using (HttpRequestMessage request = new HttpRequestMessage())
            using (HttpClient client = new HttpClient())
            {
                request.Method = HttpMethod.Post;
                request.Headers.Add("Authorization", "Bearer " + DropBoxHost.AccessToken);
                request.RequestUri = new UriBuilder()
                {
                    Scheme = "https",
                    Host = "api.dropboxapi.com",
                    Path = "/2/files/delete_v2",
                }.Uri;
                request.Content = new StringContent(post_param.ToJsonString(), Encoding.UTF8, "application/json");

                var rt = client.SendAsync(request).Result;
                if (rt.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new RepoApiException(rt.ReasonPhrase, ErrorCode.AccessTokenExpired);
                }
                if (!rt.IsSuccessStatusCode)
                {
                    return false; //
                }
                return true;
            }
        }

        class DownloadPostParam
        {
            public string path { get; set; }

            public string ToJsonString()
            {
                string j = JsonConvert.SerializeObject(this);
                byte[] bs = Encoding.Default.GetBytes(j);
                j = Encoding.UTF8.GetString(bs);
                return j;
            }
            public override string ToString()
            {
                return ToJsonString();
            }
        }

        public string Download_Impl()
        {
            // config request, verb+uril+header
            DownloadPostParam post_param = new DownloadPostParam()
            {
                path = FileId
            };

            using (HttpRequestMessage request = new HttpRequestMessage())
            using (HttpClient client = new HttpClient())
            {
                request.Method = HttpMethod.Post;
                request.Headers.Add("Authorization", "Bearer " + DropBoxHost.AccessToken);
                request.Headers.Add("Dropbox-API-Arg", post_param.ToString());
                request.RequestUri = new UriBuilder()
                {
                    Scheme = "https",
                    Host = "content.dropboxapi.com",
                    Path = "/2/files/download",
                }.Uri;

                var rt = client.SendAsync(request).Result;
                if (rt.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new RepoApiException(rt.ReasonPhrase, ErrorCode.AccessTokenExpired);
                }
                //path += Name;
                string path = DropBoxHost.WorkingPath + DisplayPath;
                Alphaleonis.Win32.Filesystem.Directory.CreateDirectory(new System.IO.DirectoryInfo(path).Parent.FullName);

                // begin downlaod
                var response = rt.Content;
                using (var n = response.ReadAsStreamAsync().Result)
                {
                    using (FileStream f = new FileStream(path, FileMode.OpenOrCreate))
                    {
                        byte[] buf = new byte[0x400];
                        // todo:
                        // good point to build download Progress bar
                        //
                        while (true)
                        {
                            var actualRead = n.Read(buf, 0, 0x400);
                            if (actualRead > 0)
                            {
                                f.Write(buf, 0, actualRead);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
                return path;
            }
        }

        public override void DeleteItem()
        {
            Delete_Impl();
        }

        public override void Download()
        {
            UpdateStatus(EnumNxlFileStatus.Downloading);
            try
            {
                var downlaodPath = Download_Impl();
                UpdateStatus(EnumNxlFileStatus.DownLoadedSucceed);
                UpdateLocalPath(downlaodPath);

            }
            catch (Exception)
            {
                UpdateStatus(EnumNxlFileStatus.DownLoadedFailed);
                throw;
            }
        }

        public override void Export(string destFolder)
        {
            //throw new NotImplementedException();
        }

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

        protected override void UpdateStatus(EnumNxlFileStatus newValue)
        {
            // Update vaultfile status in db.
            var app = SkydrmApp.Singleton;
            app.DBFunctionProvider.UpdateDropBoxFileStatus(Raw.Id, (int)newValue);
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

        protected override void UpdateOffline(bool offline)
        {
            if (Raw.IsOffline == offline)
            {
                return;
            }

            SkydrmApp.Singleton.DBFunctionProvider.UpdateDropBoxFileOffline(Raw.Id, offline);
            Raw.IsOffline = offline;
        }

        protected override void UpdateLocalPath(string localPath)
        {
            if (LocalPath.Equals(localPath))
            {
                return;
            }
            // update db
            SkydrmApp.Singleton.DBFunctionProvider.UpdateDropBoxFileLocalPath(Raw.Id, localPath);
            // update cache
            LocalPath = localPath;
        }
    }


    sealed class NxDropBoxLocalFile : ExternalDriveLocalFile
    {
        private SkydrmApp app = SkydrmApp.Singleton;
        public override EnumFileRepo FileRepo => EnumFileRepo.REPO_EXTERNAL_DRIVE;
        private NxDropBox DropBoxHost;

        public NxDropBoxLocalFile(NxDropBox host, database.table.externalrepo.ExternalDriveLocalFile raw) : base(raw)
        {
            DropBoxHost = host;
        }

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
            app.DBFunctionProvider.DeleteDropBoxLocalFile(raw.Id);
            // tell skd to remove it
            app.Rmsdk.User.RemoveLocalGeneratedFiles(Name);
        }

        public override void Upload(bool isOverWrite = false, IUploadProgressCallback callback = null)
        {
            //throw new NotImplementedException();
            DropBoxHost.Upload(LocalDiskPath, Name, GetFileCloudPathId());
        }

        protected override string GetFileCloudPathId()
        {
            var rt= app.DBFunctionProvider.GetDropBoxFileCloudFileId(DropBoxHost.RepoId, raw.ExternalDriveFileTablePk);
            return rt;
        }


        protected override void ChangeOperationStaus(EnumNxlFileStatus status)
        {
            if (raw.OperationStatus == (int)status)
            {
                return;
            }
            // change db
            app.DBFunctionProvider.UpdateDropBoxLocalFileStatus(raw.Id, (int)status);
            // update cache
            raw.OperationStatus = (int)status;
        }

        protected override string GetFileDisplayPath()
        {
            var folder = app.DBFunctionProvider.QueryDropBoxLocalFileRMSParentFolder(DropBoxHost.RepoId, raw.ExternalDriveFileTablePk);
            if (!folder.EndsWith("/"))
            {
                folder += "/";
            }

            return folder + raw.Name;
        }
               
    }
}
