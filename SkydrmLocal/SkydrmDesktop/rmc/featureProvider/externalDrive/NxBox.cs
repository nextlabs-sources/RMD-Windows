using Microsoft.Office.Interop.Excel;
using SkydrmDesktop.rmc.featureProvider.externalDrive.RmsRepoMgr;
using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Globalization;
using SkydrmLocal.rmc.fileSystem.basemodel;
using System.IO;
using System.Runtime.Remoting.Messaging;
using System.ComponentModel;
using System.Windows.Controls;
using System.Net.Mime;
using SkydrmLocal.rmc.common.helper;
using static Skydrmlocal.rmc.database2.FunctionProvider;
using SkydrmDesktop.Resources.languages;
using SkydrmDesktop.rmc.featureProvider.MessageNotify;
using SkydrmDesktop.rmc.featureProvider.externalDrive.errorHandler;

namespace SkydrmDesktop.rmc.featureProvider.externalDrive
{
    class NxBox : IExternalDrive
    {
        private readonly SkydrmApp app = SkydrmApp.Singleton;
        private readonly log4net.ILog log;
        private IRmsRepo rmsRepo;

        public EnumNxlFileStatus Status { get; set; } = EnumNxlFileStatus.Online;

        public ExternalRepoType Type => rmsRepo.Type;

        public string DisplayName { get => rmsRepo.DisplayName; set => rmsRepo.DisplayName = value; }

        public string AccessToken { get => rmsRepo.Token; set => rmsRepo.Token = value; }

        public string RepoId => rmsRepo.RepoId;

        public string WorkingPath { get; }


        public NxBox(IRmsRepo rmsRepo)
        {
            this.rmsRepo = rmsRepo;
            this.log = app.Log;
            WorkingPath = app.User.WorkingFolder + "\\Box\\" + rmsRepo.RepoId;
            Alphaleonis.Win32.Filesystem.Directory.CreateDirectory(WorkingPath);
            app.DBFunctionProvider.InsertBoxFakedRoot(RepoId);
        }


        public IExternalDriveFile[] ListAllFiles()
        {
            return ListFiles("/");
        }

        public IExternalDriveFile[] ListFiles(string cloudPathId)
        {
            
            try
            {
                if(cloudPathId == "0")
                {
                    cloudPathId = "/";
                }
                var rt = new List<NxBoxFile>();

                foreach (var i in app.DBFunctionProvider.ListBoxFile(RepoId, cloudPathId))
                {
                    rt.Add(new NxBoxFile(this, i));// required each new fill do auto-fix 
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
                var rt = new List<NxBoxLocalFile>();
                foreach (var i in app.DBFunctionProvider.ListBoxAllLocalFile(RepoId))
                {
                    rt.Add(new NxBoxLocalFile(this, i));
                }
                return rt.ToArray();
            }
            catch (Exception e)
            {
                app.Log.Error(e.Message, e);
                throw;
            }
        }

        public IExternalDriveLocalFile[] ListLocalFiles(string folderId)
        {
            try
            {
                var rt = new List<NxBoxLocalFile>();
                foreach (var i in app.DBFunctionProvider.ListBoxLocalFile(RepoId, folderId))
                {
                    if (!FileHelper.Exist(i.LocalPath))
                    {
                        app.DBFunctionProvider.DeleteBoxLocalFile(i.Id);
                        continue;
                    }
                    rt.Add(new NxBoxLocalFile(this, i));
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
                app.DBFunctionProvider.DeleteBoxFile(RepoId, i.FileId);

                // if this file is a folder, remove all its sub file nodes.
                if (i.IsFolder)
                {
                    app.DBFunctionProvider.DeleteBoxFolderAndAllSubFiles(RepoId, i.DisplayPath);
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

            app.DBFunctionProvider.UpsertBoxFileBatchEx(ff.ToArray());

            return ListFiles(folderId);
        }

        public IOfflineFile[] GetOfflines()
        {
            try
            {
                var rt = new List<IOfflineFile>();
                foreach (var i in app.DBFunctionProvider.ListBoxOfflineFile(RepoId))
                {
                    if (i.IsOffline && Alphaleonis.Win32.Filesystem.File.Exists(i.LocalPath))
                    {
                        rt.Add(new NxBoxFile(this, i));
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
                foreach (var i in app.DBFunctionProvider.ListBoxAllLocalFile(RepoId))
                {
                    // auto fix
                    if (!FileHelper.Exist(i.LocalPath))
                    {
                        app.DBFunctionProvider.DeleteBoxLocalFile(i.Id);
                        continue;
                    }
                    if (IsMatchPendingUpload((EnumNxlFileStatus)i.OperationStatus))
                    {
                        rt.Add(new NxBoxLocalFile(this, i));
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
                app.DBFunctionProvider.InsertLocalFileToBox(RepoId, ParentFolder,
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

        // greate point to make the test
        public void OnHeartBeat()
        {
            // todo: waiting for another requirements
            //ListFiles("/");
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

        private IExternalDriveFile[] Impl_SyncFiles(string folderId)
        {
            // box request folderID
            string cloudId = "";
            if (folderId == "/")
            {
                cloudId = "0";  // root must using 0, for box determined
            }
            else
            {
                var arr = folderId.Split(new char[] { '/' },StringSplitOptions.RemoveEmptyEntries);
                cloudId = arr[arr.Length - 1];
            }

            // global config
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            // config request, verb+uril+header
            using (HttpRequestMessage request = new HttpRequestMessage())
            using (HttpClient client = new HttpClient())
            {
                request.Method = HttpMethod.Get;
                request.Headers.Add("Authorization", "Bearer " + AccessToken);
                request.RequestUri = new UriBuilder()
                {
                    Scheme = "https",
                    Host = "api.box.com",
                    Path = "/2.0/folders/" + cloudId + "/items",
                    Query = "fields=modified_at,name,size&limit=1000"
                }.Uri;
                var rt = client.SendAsync(request).Result;
                if (rt.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new RepoApiException(rt.ReasonPhrase, ErrorCode.AccessTokenExpired);
                }
                // assume rt is success
                if (!rt.IsSuccessStatusCode)
                {
                    throw new Exception("Box List rest api field");
                }

                var response = rt.Content;
                // extract string from the http_response
                var str = response.ReadAsStringAsync().Result;
                var fs = ParseFoldersFromJson(str);
                if (folderId == "/")
                {
                    // fix root
                    foreach (var i in fs)
                    {
                        i.DisplayPath = "/" + i.DisplayPath;
                        i.CloudPathId = "/" + i.CloudPathId;
                        if (i.IsFolder)
                        {
                            i.DisplayPath += "/";
                            i.CloudPathId += "/";
                        }

                    }
                }
                else  // for other folder
                {
                    //query datebase to find parent cloud_display_path
                    string dp = app.DBFunctionProvider.GetBoxCloudDispalyPathByFileId(RepoId, cloudId);
                    string pp = app.DBFunctionProvider.GetBoxCloudPathIdByFileId(RepoId, cloudId);
                    foreach (var i in fs)
                    {
                        i.DisplayPath = dp + i.DisplayPath;
                        i.CloudPathId = folderId + i.CloudPathId;
                        if (i.IsFolder)
                        {
                            i.DisplayPath +="/";
                            i.CloudPathId +="/";
                        }
                    }
                }
                return fs.ToArray();
            }
        }

        private NxBoxFile[] ParseFoldersFromJson(string json)
        {
            JObject root = JObject.Parse(json);
            if (root == null)
            {
                return null;
            }

            if (!root.ContainsKey("total_count"))
            {
                return null;
            }
            int total = (int)root["total_count"];
            if (total == 0)
            {
                return new NxBoxFile[0];
            }
            if (!root.ContainsKey("entries"))
            {
                // syntax error, no any entries
                return new NxBoxFile[0];
            }

            var rt = new List<NxBoxFile>(total);

            JArray array = root["entries"] as JArray;
            foreach (var i in array)
            {
                var item = new NxBoxFile(this);
                item.FileId = (string)i["id"];
                item.IsFolder = String.Equals((string)i["type"], "folder", StringComparison.CurrentCultureIgnoreCase);

                item.IsOffline = false;
                item.IsFavorite = false;

                item.Name = (string)i["name"];
                item.Size = long.Parse((string)i["size"]);

                item.LocalPath = item.Name;              // fake
                item.DisplayPath = item.Name;          // fake
                // shit C# how to parst RFC3339
                //item.ModifiedTme = DateTime.Parse("yyyy-MM-dd'T'HH:mm:ss");
                item.ModifiedTme = DateTime.Parse((string)i["modified_at"]);

                item.CustomString = "";

                item.CloudPathId = item.FileId;
                item.IsNxlFile = false;

                rt.Add(item);
            }

            return rt.ToArray();
        }

        private bool Upload_Impl(string localPath, string name, string cloudPathId)
        {
            if (cloudPathId == "/")
            {
                cloudPathId = "0";
            }
            // config request, verb+uril+header
            using (HttpRequestMessage request = new HttpRequestMessage())
            using (HttpClient client = new HttpClient())
            {
                request.Method = HttpMethod.Post;
                request.Headers.Add("Authorization", "Bearer " + AccessToken);
                request.RequestUri = new UriBuilder()
                {
                    Scheme = "https",
                    Host = "upload.box.com",
                    Path = "/api/2.0/files/content",
                }.Uri;

                var multipartContent = new MultipartFormDataContent("----osmond_test_boundary------");

                string attributes = @"{'name':'[TAG_FILE_NAME]', 'parent':{'id':'[TAG_PARENT_FOLDER_ID]'}}";
                attributes = attributes.Replace("\'", "\"");
                attributes = attributes.Replace("[TAG_FILE_NAME]", name);
                attributes = attributes.Replace("[TAG_PARENT_FOLDER_ID]", cloudPathId);

                var content_1 = new StringContent(attributes);
                var content_2 = new StreamContent(new FileStream(localPath, FileMode.Open));

                multipartContent.Add(content_1, "attributes");
                multipartContent.Add(content_2, "file", name);

                request.Content = multipartContent;


                var rt = client.SendAsync(request).Result;
                if (rt.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new RepoApiException(rt.ReasonPhrase, ErrorCode.AccessTokenExpired);
                }
                //if (!rt.IsSuccessStatusCode)
                //{
                //    return false;
                //}
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

    sealed class NxBoxFile : ExternalDriveFile
    {
        private NxBox boxHost;
        private SkydrmApp app = SkydrmApp.Singleton;
        public NxBoxFile(NxBox host) : base()
        {
            boxHost = host;
        }

        public NxBoxFile(NxBox host, database.table.externalrepo.ExternalDriveFile raw) : base(raw)
        {
            boxHost = host;
        }

        private bool Delete_Impl()
        {
            // config request, verb+uril+header
            using (HttpClient client = new HttpClient())
            using (HttpRequestMessage request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Delete;
                request.Headers.Add("Authorization", "Bearer " + boxHost.AccessToken);
                request.RequestUri = new UriBuilder()
                {
                    Scheme = "https",
                    Host = "api.box.com",
                    Path = "/2.0/files/" + FileId,
                }.Uri;

                var rt = client.SendAsync(request).Result;
                if (rt.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new RepoApiException(rt.ReasonPhrase, ErrorCode.AccessTokenExpired);
                }
                // netcode is 204 for delete successfully
                if (!rt.IsSuccessStatusCode)
                {
                    return false;
                }
                return true;
            }
        }

        private string Download_Impl()
        {
            // begin downlaod
            using (HttpClient client = new HttpClient())
            using (HttpRequestMessage request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Get;
                request.Headers.Add("Authorization", "Bearer " + boxHost.AccessToken);
                request.RequestUri = new UriBuilder()
                {
                    Scheme = "https",
                    Host = "api.box.com",
                    Path = "/2.0/files/" + FileId + "/content",
                }.Uri;
                var rt = client.SendAsync(request).Result;

                var response = rt.Content;
                if (rt.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new RepoApiException(rt.ReasonPhrase, ErrorCode.AccessTokenExpired);
                }

                string path = boxHost.WorkingPath + DisplayPath;
                Alphaleonis.Win32.Filesystem.Directory.CreateDirectory(new System.IO.DirectoryInfo(path).Parent.FullName);

                using (var n = response.ReadAsStreamAsync().Result)
                using (FileStream f = new FileStream(path, FileMode.OpenOrCreate))
                {
                    byte[] buf = new byte[0x400];
                    // todo:
                    // great point to build download Progress bar
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

        protected override void UpdateOffline(bool offline)
        {
            if (Raw.IsOffline == offline)
            {
                return;
            }

            SkydrmApp.Singleton.DBFunctionProvider.UpdateBoxFileOffline(Raw.Id, offline);
            Raw.IsOffline = offline;
        }

        protected override void UpdateStatus(EnumNxlFileStatus newValue)
        {
            // Update vaultfile status in db.
            var app = SkydrmApp.Singleton;
            app.DBFunctionProvider.UpdateBoxFileStatus(Raw.Id, (int)newValue);
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
            if (LocalPath.Equals(localPath))
            {
                return;
            }
            // update db
            SkydrmApp.Singleton.DBFunctionProvider.UpdateBoxFileLocalPath(Raw.Id, localPath);
            // update cache
            LocalPath = localPath;
        }
    }

    sealed class NxBoxLocalFile : ExternalDriveLocalFile
    {
        private SkydrmApp app = SkydrmApp.Singleton;
        public override EnumFileRepo FileRepo => EnumFileRepo.REPO_EXTERNAL_DRIVE;
        private NxBox BoxHost;

        public NxBoxLocalFile(NxBox host, database.table.externalrepo.ExternalDriveLocalFile raw) : base(raw)
        {
            BoxHost = host;
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
            app.DBFunctionProvider.DeleteBoxLocalFile(raw.Id);
            // tell skd to remove it
            app.Rmsdk.User.RemoveLocalGeneratedFiles(Name);
        }

        public override void Upload(bool isOverWrite = false, IUploadProgressCallback callback = null)
        {
            //throw new NotImplementedException();
            BoxHost.Upload(LocalDiskPath, Name, GetFileCloudPathId());
        }
        protected override string GetFileCloudPathId()
        {
            var rt = app.DBFunctionProvider.GetBoxFileCloudFileId(BoxHost.RepoId, raw.ExternalDriveFileTablePk);
            if(rt == "/")
            {
                rt = "0";
            }
            return rt;
        }

        protected override void ChangeOperationStaus(EnumNxlFileStatus status)
        {
            if (raw.OperationStatus == (int)status)
            {
                return;
            }
            // change db
            app.DBFunctionProvider.UpdateBoxLocalFileStatus(raw.Id, (int)status);
            // update cache
            raw.OperationStatus = (int)status;
        }

        protected override string GetFileDisplayPath()
        {
            var folder = app.DBFunctionProvider.QueryDropBoxLocalFileRMSParentFolder(BoxHost.RepoId, raw.ExternalDriveFileTablePk);
            if (!folder.EndsWith("/"))
            {
                folder += "/";
            }

            return folder + raw.Name;
        }

    }
}
