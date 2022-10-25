using System;
using System.Collections.Generic;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SkydrmLocal.rmc.common.communicator;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.database.table.myvault;
using SkydrmLocal.rmc.featureProvider.common;
using SkydrmLocal.rmc.featureProvider.FileInfo;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.sdk;
using static Skydrmlocal.rmc.database2.FunctionProvider;

namespace SkydrmLocal.rmc.featureProvider.MyVault
{
    public sealed class MyVault : IMyVault
    {
        private readonly SkydrmLocalApp App;
        private readonly log4net.ILog Log;
        private string working_path;

        public string WorkingFolder { get => working_path;}

        public MyVault(SkydrmLocalApp app)
        {
            this.App = app;
            this.Log = app.Log;
            working_path = App.User.WorkingFolder + "\\MyVault";
            if (!Directory.Exists(working_path))
            {
                Directory.CreateDirectory(working_path);
            }
        }

        public void OnHeartBeat()
        {
            Sync();
        }
        
        public IMyVaultFile[] List()
        {
            // history, in order to boost up speed for UI rendering, 
            // make it return as as soon as possible
            // we splite VaultFile new and init,

            // filter out deleted&revokded myvaultfile.
            return Impl_List(true, true, true);
        }

        public IMyVaultFile[] ListWithoutFilter()
        {
            return Impl_List(false, false, false);
        }

        public IMyVaultFile[] Sync()
        {
            lock (this)
            {
                // add cache
                var remotes = App.Rmsdk.User.ListMyVaultFiles();
                // for merge,we don't need care about marking sure local info fixed.
                var locals = Impl_List(false, false, false);

                // Fix bug 53562, find difference set by (Local - remote) , and delete it 
                var diffset = from i in locals
                              let rNames = from j in remotes select j.nxlName
                              where !rNames.Contains(i.Nxl_Name)
                              select i;
                foreach (var i in diffset)
                {
                    App.DBFunctionProvider.DeleteMyVaultFile(i.Nxl_Name);
                }

                var ff = new List<InsertMyVaultFile>();
                foreach (var i in FilterOutNotModified(locals, remotes))
                {
                    ff.Add(new InsertMyVaultFile()
                    {
                        path_id = i.pathId,
                        display_path = i.displayPath,
                        repo_id = i.repoId,
                        duid = i.duid,
                        nxl_name = i.nxlName,
                        last_modified_time = i.lastModifiedTime,
                        creation_time = i.creationTime,
                        shared_time = i.sharedTime,
                        shared_with_list = i.sharedWithList,
                        size = i.size,
                        is_deleted = i.isDeleted == 1,
                        is_revoked = i.isRevoked == 1,
                        is_shared = i.isShared == 1,
                        source_repo_type = i.sourceRepoType,
                        source_file_display_path = i.sourceFileDisplayPath,
                        source_file_path_id = i.sourceFilePathId,
                        source_repo_name = i.sourceRepoName,
                        source_repo_id = i.sourceRepoId
                    }
                        );
                }
                App.DBFunctionProvider.UpsertMyVaultFileBatch(ff.ToArray());
                return List();
            }
           
        }

        public IMyVaultLocalFile[] ListLocalAdded()
        {
            var rt = new List<MyVaultLocalAddedFile>();
            foreach (var i in App.DBFunctionProvider.ListMyVaultLocalFile())
            {
                if (!FileHelper.Exist(i.Nxl_Local_Path))
                {
                    App.DBFunctionProvider.DeleteMyVaultLocalFile(i.Nxl_Name);
                    continue;
                }
                rt.Add(new MyVaultLocalAddedFile(i));
            }
            return rt.ToArray();
        }

        public IOfflineFile[] GetOfflines()
        {
            IList<IOfflineFile> rt = new List<IOfflineFile>();
            foreach (var i in App.DBFunctionProvider.ListMyVaultFile())
            {
                // filter out, but IsRevoked may be not ,for future release
                if (i.RmsIsDeleted == true || i.RmsIsRevoked == true)
                {
                    continue;
                }

                if (i.Is_Offline == true &&
                    FileHelper.Exist(i.LocalPath)
                    )
                {
                    rt.Add(new MyVaultFile(this,i));
                }
            }            
            return rt.ToArray();
        }

        public IPendingUploadFile[] GetPendingUploads()
        {
            IList<IPendingUploadFile> rt = new List<IPendingUploadFile>();
            foreach (var i in App.DBFunctionProvider.ListMyVaultLocalFile())
            {
                // if status is error, del it
                if (!FileHelper.Exist(i.Nxl_Local_Path))
                {
                    App.DBFunctionProvider.DeleteMyVaultLocalFile(i.Nxl_Name);
                    continue;
                }
                if (IsMatchPendingUpload((EnumNxlFileStatus)i.Status))
                {
                    rt.Add(new MyVaultLocalAddedFile(i));
                }
            }           
            return rt.ToArray(); ;
        }

        public bool IsMatchPendingUpload(EnumNxlFileStatus status)
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

        public IMyVaultLocalFile AddLocalAdded(string path, List<FileRights> rights, 
            WaterMarkInfo waterMark, Expiration expiration, UserSelectTags tags)
        {
            try
            {
                // tell api to convert to nxl by protect
                App.Log.Info("try to protect file to myVault: "+ path);
                var outPath = App.Rmsdk.User.ProtectFile(path, rights, waterMark, expiration, tags);

                // find this file in api
                //App.Log.Info("try to get the new protected file's fingerprint");
                //var fp = App.Rmsdk.User.GetNxlFileFingerPrint(outPath);
                // by osmond, feature changed, allow user to portect a file which has not any permissons for this user
                string newAddedName = Alphaleonis.Win32.Filesystem.Path.GetFileName(outPath);
                var newAddedFileSize = new Alphaleonis.Win32.Filesystem.FileInfo(outPath).Length;

                App.Log.Info("store the new protected file into database");
                InsertLocalFile(newAddedName, outPath, File.GetLastAccessTime(outPath), null, newAddedFileSize, EnumNxlFileStatus.WaitingUpload, null, path);

                // tell IRecentTouchedFiles
                App.UserRecentTouchedFile.UpdateOrInsert(EnumNxlFileStatus.WaitingUpload, newAddedName);
                
                // return the new added
                return ListLocalAdded().First((i) =>
                {
                    return i.Nxl_Name.Equals(newAddedName);
                });
            }catch(Exception e)
            {
                App.Log.Error("Error occured when tring to protect file in myault" + e.Message, e);
                throw e;
            }
        }

        public IMyVaultLocalFile CopyLocalAdded(string copyPath, string path, List<FileRights> rights,
            WaterMarkInfo waterMark, Expiration expiration, UserSelectTags tags)
        {
            try
            {
                // tell api to convert to nxl by protect
                App.Log.Info("try to protect file to myVault");
                var outPath = App.Rmsdk.User.ProtectFile(path, rights, waterMark, expiration, tags);

                // find this file in api
                App.Log.Info("try to get the new protected file's fingerprint");
                var fp = App.Rmsdk.User.GetNxlFileFingerPrint(outPath);

                // copy file to dest path
                App.Log.Info(string.Format("copy file to {0}", copyPath));
                string destPath = copyPath + Path.GetFileName(outPath);
                File.Copy(outPath, destPath, true);

                // return the new added
                return new MyVaultLocalAddedFile(
                    new MyVaultLocalFile
                    {
                        Nxl_Name = fp.name,
                        Nxl_Local_Path = destPath,
                        Last_Modified_Time = File.GetLastAccessTime(destPath),
                        Size = fp.size
                    });
            }
            catch (Exception e)
            {
                App.Log.Error("Error occured when tring to protect file in myault" + e.Message, e);
                throw e;
            }
        }

        public IMyVaultLocalFile AddLocalAdded(string path, List<FileRights> rights,
            List<string> recipients, string comments,
            WaterMarkInfo waterMark, Expiration expiration, UserSelectTags tags)
        {
            try
            {
                // tell api to convert to nxl by Share
                App.Log.Info("try to share file to myVault: "+ path);
                //var outPath = App.Rmsdk.User.ShareFile(path, rights, recipients, comments, waterMark, expiration, tags);
                var outPath = App.Rmsdk.User.ProtectFile(path, rights, waterMark, expiration, tags);

                // find this file in api
                App.Log.Info("try to get the new shared file's fingerprint");
                var fp = App.Rmsdk.User.GetNxlFileFingerPrint(outPath);

                App.Log.Info("store the new shared file into database");
                InsertLocalFile(fp.name, outPath, File.GetLastAccessTime(outPath), recipients.ToArray(), fp.size, EnumNxlFileStatus.WaitingUpload, comments, path);
                // tell IRecentTouchedFiles
                App.UserRecentTouchedFile.UpdateOrInsert(EnumNxlFileStatus.WaitingUpload, fp.name);
                // return new added
                return ListLocalAdded().First((i) =>
                {
                    return i.Nxl_Name.Equals(fp.name);
                });
            }catch(Exception e)
            {
                App.Log.Error("Error occured when tring to share file in myault" + e.Message, e);
                throw;
            }
        }

        public void InsertLocalFile(string nxl_name, 
                                string nxl_local_path,
                                DateTime last_modified_time, 
                                string[] shared_with_list,
                                long size,
                                EnumNxlFileStatus status,
                                string comment,
                                string originalPath)
        {
            App.DBFunctionProvider.InsertMyVaultLocalFile(
                nxl_name, nxl_local_path, 
                last_modified_time,
                MyVaultLocalAddedFile.TranslateSharedWithArrToStr(shared_with_list),
                size, 
                (int)status,
                comment,
                originalPath);
        }

        // called when merging files between local and remote, 
        // return an array contained the modified files: 
        //      a file that remote changed its some fields,but local not update it 
        //      a file that is new added at remote. 
        private MyVaultFileInfo[] FilterOutNotModified(IMyVaultFile[] locals, MyVaultFileInfo[] remotes)
        {
            if (locals.Length == 0)
            {
                return remotes;
            }
            var rt = new List<MyVaultFileInfo>();
            foreach (var i in remotes)
            {
                try
                {
                    // find in local
                    // if use Enumerable.First(), will throw exception when no matching element. 
                    // It will cause blocking UI when switch treeView item during uploading many files.
                    var l = locals.FirstOrDefault((j) =>
                    {
                        if (i.duid != j.Duid)
                        {
                            return false;
                        }
                        return true;
                    });

                    // if no matching element, will return null.
                    if (l == null)
                    {
                        App.Log.Info("MyVault local list no matching element");
                        rt.Add(i);
                        continue;
                    }

                    // judege whether modified

                    // only care is_removed, is_deleted, is_shared
                    if ((i.isDeleted == 1) != l.Is_Deleted ||
                        (i.isRevoked) == 1 != l.Is_Revoked ||
                        (i.isShared == 1) != l.Is_Shared ||
                        !i.sharedWithList.Equals(l.Shared_With_List, StringComparison.CurrentCultureIgnoreCase)
                        )
                    {
                        rt.Add(i);
                    }
                }
                catch (Exception e)
                {
                    App.Log.Error(e);
                    // local find error
                    rt.Add(i);
                }
            }
            return rt.ToArray();
        }
        
        private IMyVaultFile[] Impl_List(bool filterOutDelted = false,
                                bool filterOutRevoked = false,
                                bool bInitFileObjAsync = false)
        {
            var rt = new List<MyVaultFile>();
            foreach (var i in App.DBFunctionProvider.ListMyVaultFile())
            {
                if (filterOutDelted && i.RmsIsDeleted)
                {
                    continue;
                }
                if (filterOutRevoked && i.RmsIsRevoked)
                {
                    continue;
                }
                // impl auto fix, we can not del this item, but we can change its status in constructor
                rt.Add(new MyVaultFile(this, i));             
            }
            // by osmond, I made a mistake by impled some logic code in MyVaultFile's contructor
            // which may in time-consuming
            // init the list at background
            if (bInitFileObjAsync)
            {
                ThreadPool.QueueUserWorkItem((theList) =>
                {
                    bool bTellUI = false;
                    foreach (var i in (List<MyVaultFile>)theList)
                    {
                        var modified=i.Initialize();
                        if (modified)
                        {
                            bTellUI = true;
                        }
                    }
                    if (bTellUI)
                    {
                        //EventBus.GetInstance().Post(new ui.windows.mainWindow.viewModel.ViewModelMainWindow.EventMyVaultFilesLowLevelUpdated(),true);
                        App.InvokeEvent_MyVaultOrSharedWithmeFileLowLevelUpdated();
                    }

                }, rt);
            }
            else
            {
                foreach (var j in rt)
                {
                    j.Initialize();
                }
            }
            return rt.ToArray();
        }

    }

    public sealed class MyVaultFile : IMyVaultFile, IOfflineFile
    {
        private database2.table.myvault.MyVaultFile raw;
        private MyVault myVaultHost;
        private InternalFileInfo fileInfo;  // each get will generate a new-one
        private string partialLocalPath;

        public MyVaultFile(MyVault host,database2.table.myvault.MyVaultFile raw)
        {
            this.myVaultHost = host;
            this.raw = raw;          
        }
       
        // return true, some fileds has been modified
        // return flase, no one modifed.
        public bool Initialize()
        {
            bool modified = false;
            // auto fix
            if (Is_Offline && !FileHelper.Exist(raw.LocalPath))
            {
                Status = EnumNxlFileStatus.Online;
                Nxl_Local_Path = "";
                modified = true;
            }
            // imple leave a copy   
            bool modified2 = ImplLeaveCopy();
            return modified || modified2;
        }

        public string Path_Id { get => raw.RmsPathId; }

        public string Display_Path { get => raw.RmsDisplayPath; }

        public string Repo_Id { get => raw.RmsRepoId; }

        public string Duid { get => raw.RmsDuid; }

        public string Nxl_Name { get => raw.RmsName; }

        public DateTime Last_Modified_Time { get => raw.RmsLastModifiedTime; }

        public DateTime Creation_Time { get => raw.RmsCreationTime; }

        public DateTime Shared_Time { get => raw.RmsSharedTime; }

        public string Shared_With_List { get => raw.RmsSharedWith; }

        public long FileSize { get => raw.RmsSize; }

        public bool Is_Deleted { get => raw.RmsIsDeleted; }

        public bool Is_Revoked { get => raw.RmsIsRevoked; }

        public bool Is_Shared { get => raw.RmsIsShared; set => UpdateShareStatus(value); }

        public string Nxl_Local_Path { get => raw.LocalPath; set => UpdateLocalPath(value); }

        public bool Is_Offline { get => raw.Is_Offline; set => UpdateOffline(value); }

        public bool Is_Edit
        {
            get => raw.Edit_Status != 0;
            set => UpdateEditStatus(value ? 1 : 0);
        }
        public bool Is_ModifyRights
        {
            get => raw.Modify_Rights_Status != 0;
            set => UpdateModifyRightsStatus(value ? 1 : 0);
        }

        public EnumNxlFileStatus Status { get => (EnumNxlFileStatus)raw.Status; set => UpdateStatus(value); }

        #region IOffline Used
        // IOffline Used
        public string Name => Nxl_Name;
        // IOffline Used
        public string LocalDiskPath => Nxl_Local_Path;
        // IOffline Used
        public DateTime LastModifiedTime => Last_Modified_Time;

        public bool IsOfflineFileEdit => Is_Edit;

        public string RMSRemotePath => Display_Path;

        public IFileInfo FileInfo => new InternalFileInfo(this);
        #endregion

        public string Partial_Local_Path
        {
            get
            {
                if (string.IsNullOrEmpty(partialLocalPath))
                {
                    partialLocalPath = GetPartialLocalPath();
                }

                if (!FileHelper.Exist(partialLocalPath))
                {
                    if (FileHelper.Exist(raw.LocalPath))
                    {
                        partialLocalPath = raw.LocalPath;
                    }
                    else
                    {
                        partialLocalPath = "";
                    }
                }

                return partialLocalPath;
            }
        }

        private string GetPartialLocalPath()
        {
            return myVaultHost.WorkingFolder + "\\" + "partial_" + Nxl_Name;
        }
        
        private void UpdateOffline(bool offline)
        {
            //Sanity check.
            if (raw.Is_Offline == offline)
            {
                return;
            }
            //Update offline marker in db.
            var app = SkydrmLocalApp.Singleton;
            app.DBFunctionProvider.UpdateMyVaultFileOffline(raw.Id, offline);
            //Update obj raw's offline marker.
            raw.Is_Offline = offline;
            if (raw.Is_Offline)
            {
                app.UserRecentTouchedFile.UpdateOrInsert(EnumNxlFileStatus.AvailableOffline, Nxl_Name);
            }
            else
            {
                app.UserRecentTouchedFile.UpdateOrInsert(EnumNxlFileStatus.Online, Nxl_Name);
            }
        }

        private void UpdateShareStatus(bool newStatus)
        {
            if(raw.RmsIsShared == newStatus)
            {
                return;
            }
            //Update offline marker in db.
            var app = SkydrmLocalApp.Singleton;
            app.DBFunctionProvider.UpdateMyVaultFileShareStatus(raw.Id, newStatus);
            raw.RmsIsShared = newStatus;
        }

        private void UpdateStatus(EnumNxlFileStatus status)
        {    
            // sanity check
            if (raw.Status == (int)status)
            {
                return;
            }
            //Update vaultfile status in db.
            var app = SkydrmLocalApp.Singleton;
            app.DBFunctionProvider.UpdateMyVaultFileStatus(raw.Id, (int)status);
            raw.Status = (int)status;
            if (status == EnumNxlFileStatus.Online)
            {
                Is_Offline = false;
            }
            if(status == EnumNxlFileStatus.AvailableOffline)
            {
                Is_Offline = true;
            }

            //Notify Service Manager
            app.UserRecentTouchedFile.UpdateOrInsert(status, Nxl_Name);
        }

        private void UpdateEditStatus(int newStatus)
        {
            // Check changable first.
            if (raw.Edit_Status == newStatus) {
                return;
            }
            var app = SkydrmLocalApp.Singleton;
            // Update db.
            app.DBFunctionProvider.UpdateMyVaultFileEditStatus(raw.Id, newStatus);
            // Update cache.
            raw.Edit_Status = newStatus;
        }

        private void UpdateModifyRightsStatus(int newStatus)
        {
            // Check changable first.
            if (raw.Modify_Rights_Status == newStatus)
            {
                return;
            }
            var app = SkydrmLocalApp.Singleton;
            // Update db.
            app.DBFunctionProvider.UpdateMyVaultFileModifyRightsStatus(raw.Id, newStatus);
            // Update cache.
            raw.Modify_Rights_Status = newStatus;
        }

        public void ChangeSharedWithList(string[] emails)
        {
            var l = "";
            foreach (var e in emails)
            {
                l += e+ ",";
            }
            if (l.EndsWith(","))
            {
                l = l.Substring(0, l.Length - 1);
            }

            // update in local
            if (raw.RmsSharedWith.Equals(l, StringComparison.CurrentCultureIgnoreCase))
            {
                return;
            }
            SkydrmLocalApp.Singleton.DBFunctionProvider.UpdateMyVaultFileSharedWithList(raw.Id, l);
            raw.RmsSharedWith = l;
        }
       
        private void UpdateLocalPath(string local_path)
        {
            var app = SkydrmLocalApp.Singleton;
            app.DBFunctionProvider.UpdateMyVaultFileLocalPath(raw.Id, local_path);
            raw.LocalPath = local_path;
        }

        public void Export(string destinationFolder)
        {
            var App = SkydrmLocalApp.Singleton;

            App.Log.Info("MyVault Export As file,path=" + destinationFolder);

            string currentUserTempPathOrDownloadFilePath = System.IO.Path.GetTempPath();
  
            //feature check
            if (Is_Deleted)
            {
                // - is_Delete file can not be downloaded
                App.ShowBalloonTip("The file " + raw.RmsName + " has been removed from MyVault permanently.");
                throw new Exception("The file " + raw.RmsName + " in myVault has been deleted.");
            }

            try
            {
                // by commend, sdk will help us to record log: DownloadForOffline
                //App.User.AddNxlFileLog(raw.LocalPath, NxlOpLog.Download, true);
                // download 
                App.Rmsdk.User.DownloadMyVaultFile(Path_Id, ref currentUserTempPathOrDownloadFilePath, sdk.DownlaodMyVaultFileType.Normal);
                File.Copy(currentUserTempPathOrDownloadFilePath, destinationFolder, true);

            }
            catch (Exception e)
            {       
                App.Log.Error("failed SaveAs file=" + Nxl_Local_Path, e);
                // del                
                throw;
            }
            finally
            {
                FileHelper.Delete_NoThrow(currentUserTempPathOrDownloadFilePath);
            }
        }

        public void Download(bool isViewOnly=false)
        {
            var App = SkydrmLocalApp.Singleton;
            //feature check
            if (Is_Deleted)
            {
                // - is_Delete file can not be downloaded
                App.ShowBalloonTip("The file " + raw.RmsName + " has been removed from MyVault permanently.");
                throw new Exception("The file " + raw.RmsName + " in myVault has been deleted.");
            }

            Nxl_Local_Path = myVaultHost.WorkingFolder + "\\" + Nxl_Name;
            //before download delete exist
            FileHelper.Delete_NoThrow(Nxl_Local_Path);
            UpdateStatus(EnumNxlFileStatus.Downloading);
            try
            {
                DownlaodMyVaultFileType type = isViewOnly ? DownlaodMyVaultFileType.ForVeiwer : DownlaodMyVaultFileType.ForOffline;
                // download 
                string workingfolder = myVaultHost.WorkingFolder;
                App.Rmsdk.User.DownloadMyVaultFile(Path_Id, ref workingfolder, type);
                UpdateLocalPath(Nxl_Local_Path);
                UpdateStatus(EnumNxlFileStatus.DownLoadedSucceed);
            }
            catch (Exception e)
            {
                UpdateStatus(EnumNxlFileStatus.DownLoadedFailed);
                App.Log.Error("failed downlond file=" + Nxl_Local_Path, e);
                // del
                FileHelper.Delete_NoThrow(Nxl_Local_Path);
                throw;
            }
        }

        // Download partial file to check file rights.
        public void DownloadPartial()
        {
            var app = SkydrmLocalApp.Singleton;
            //feature check
            if (Is_Deleted)
            {
                // - is_Delete file can not be downloaded
                app.ShowBalloonTip("The file " + raw.RmsName + " has been removed from MyVault permanently.");
                throw new Exception("The file " + raw.RmsName + " in myVault has been deleted.");
            }

            // File name is attached prefix "partial" returned by sdk.
            var partialFPath = myVaultHost.WorkingFolder + "\\" + "partial_" + Nxl_Name;

            //before download delete exist
            FileHelper.Delete_NoThrow(partialFPath);

            app.Log.Info("partical downlaod path: " + partialFPath);
            try
            {
                // download 
                string workingfolder = myVaultHost.WorkingFolder;
                app.Rmsdk.User.DownloadMyVaultPartialFile(Path_Id, ref workingfolder, sdk.DownlaodMyVaultFileType.ForVeiwer);
                partialLocalPath = partialFPath;
            }
            catch (Exception e)
            {
                FileHelper.Delete_NoThrow(Nxl_Local_Path);
                app.Log.Error("failed partial downlond file=" + Nxl_Local_Path, e);
                throw e;
            }
        }

        public void Remove()
        {
            var app = SkydrmLocalApp.Singleton;
            try
            {
                //Delete local downloaded file.
                string local_path = Nxl_Local_Path;
                if (local_path.Equals("") || local_path == null)
                {
                    return;
                }
                if (!File.Exists(local_path))
                {
                    return;
                }
                FileHelper.Delete_NoThrow(local_path);               
                // update db
                UpdateStatus(EnumNxlFileStatus.RemovedFromLocal);
            }
            catch(Exception e)
            {
                app.Log.Error("remove file filed,path=" + Nxl_Local_Path, e);
                throw;
            }
        }

        public void RemoveFromLocal()
        {
            Remove();
        }

        private bool ImplLeaveCopy()
        {
            bool modified = false;
            var leaveACopy = SkydrmLocalApp.Singleton.User.LeaveCopy_Feature;
            if (leaveACopy.Exist(raw.RmsName))
            {
                // mark this file as local cached
                var newLocalPath = myVaultHost.WorkingFolder + "\\" + Nxl_Name;
                if (leaveACopy.MoveTo(raw.RmsName, newLocalPath))
                {
                    // update this file status
                    UpdateLocalPath(newLocalPath);
                    Is_Offline = true;
                    UpdateStatus(EnumNxlFileStatus.CachedFile);
                    modified = true;
                }

            }
            return modified;
        }

        public MyVaultMetaData GetMetaData()
        {
            var app = SkydrmLocalApp.Singleton;

            string localPath = Partial_Local_Path;
            string pathId = Path_Id;

            //Sanity check.
            if (string.IsNullOrEmpty(localPath))
            {
                throw new Exception("The argument localPath is null.");
            }
            if(string.IsNullOrEmpty(pathId))
            {
                throw new Exception("Illegal argument found,maybe transfer issue.");
            }

            return app.Rmsdk.User.GetMyVaultFileMetaData(localPath, pathId);
        }

        public void ShareFile(string nxlLocalPath, string[] recipentAdds, string[] recipentRmoves, string comments)
        {
            // if file is protected upload should invoke share repository api.
            // through isShared field we can find out whether this file is shared upload or protect upload or not.
            if (Is_Shared)
            {
                //Update recipents will be fine.
                UpdateRecipents(nxlLocalPath, recipentAdds, recipentRmoves);
            }
            else
            {
                // Share repository.
                ShareRepository(nxlLocalPath, recipentAdds, comments);
                Is_Shared = true;
            }
        }

        private void ShareRepository(string nxlLocalPath, string[] recipents, string comments)
        {
            var app = SkydrmLocalApp.Singleton;

            string repoId = Repo_Id;
            string fName = Nxl_Name;
            string fpId = Path_Id;
            string fp = Display_Path;

            app.Rmsdk.User.MyVaultShareFile(nxlLocalPath, recipents, repoId, fName, fpId, fp, comments);
        }

        private void UpdateRecipents(string nxlLocalPath, string[] addRecipents,string[] removedRecipents)
        {
            var app = SkydrmLocalApp.Singleton;

            List<string> added = addRecipents.ToList();
            List<string> removed = removedRecipents.ToList();

            app.Rmsdk.User.UpdateRecipients(nxlLocalPath, added, removed);
        }

        private class InternalFileInfo : FileInfoBaseImpl
        {
            private MyVaultFile Outer;

            public InternalFileInfo(MyVaultFile outer) : base(outer.Partial_Local_Path) //Use Partial_LocalPath get Rights, Change 'LocalPath' to 'Partial_LocalPath'
            {
                Outer = outer;
            }
            public override string Name => Outer.Name;

            public override long Size => Outer.FileSize;

            public override DateTime LastModified => Outer.LastModifiedTime;

            public override string RmsRemotePath => Outer.RMSRemotePath;

            public override bool IsCreatedLocal => false;

            public override string[] Emails => GetEmails();

            public override EnumFileRepo FileRepo => EnumFileRepo.REPO_MYVAULT;
        
            private string[] GetEmails()
            {
                return Outer.Shared_With_List.Split(new char[] { ' ', ';',',' });
            }

            public override IFileInfo Update()
            {
                base.Update();
                return this;
            }

        }

    }

    // User add it in local, once it has been uploaded to RMS, it should be deleted in DB and local Disk
    public sealed class MyVaultLocalAddedFile : IMyVaultLocalFile
    {
        private database.table.myvault.MyVaultLocalFile raw;
        private InternalFileInfo fileInfo; // each get will generate a new-one
        public MyVaultLocalAddedFile(database.table.myvault.MyVaultLocalFile raw)
        {
            this.raw = raw;
        }

        public string Nxl_Name { get => raw.Nxl_Name; }

        public string Nxl_Local_Path { get => raw.Nxl_Local_Path; }

        public DateTime Last_Modified_Time { get => raw.Last_Modified_Time; }

        public string[] Nxl_Shared_With_List { get => TranlateSharedWithFromStr(raw.Shared_With_List); set => UpdateSharedWith(value); }

        public long FileSize { get => raw.Size; }

        public EnumNxlFileStatus Status { get => (EnumNxlFileStatus)raw.Status; set => UpdateStatus(value); }

        #region For upload
        private string Comment => raw.Comment;
        /// <summary>
        /// File source path before encryption
        /// </summary>
        public string OriginalPath { get => raw.OriginalPath; set => UpdateOriginalPath(value); }
        #endregion

        #region IPendingUploadFile used
        public string Name => Nxl_Name;

        public string LocalDiskPath => Nxl_Local_Path;

        // todo: wait for henry to modify Last_Modified_Time as DateTime type
        public DateTime LastModifiedTime => Last_Modified_Time;

        public string RMSRemotePath => "/"+raw.Nxl_Name;

        public string SharedEmails => raw.Shared_With_List;

        public IFileInfo FileInfo => new InternalFileInfo(this);
        #endregion

        public void UpdateStatus(EnumNxlFileStatus status)
        {
            //Sanity check
            //If no changes just return.
            if (raw.Status==(int)status)
            {
                return;
            }
            //update status in db.
            var app = SkydrmLocalApp.Singleton;
            app.DBFunctionProvider.UpdateMyVaultLocalFileStatus(Nxl_Name, (int)status);
            raw.Status = (int)status;
            NotifyIRecentTouchedFile(status);
        }

        public void UpdateSharedWith(string[] sharedWithList)
        {
            string results = TranslateSharedWithArrToStr(sharedWithList);
            string local = raw.Shared_With_List;
            if (results.Equals(local))
            {
                return;
            }
            //update new resutls passed.
            //update sharedwithlist in db.
            var app = SkydrmLocalApp.Singleton;
            app.DBFunctionProvider.UpdateMyVaultLocalFileSharedWithList(Nxl_Name, results);
            raw.Shared_With_List = results;
        }

        private string[] TranlateSharedWithFromStr(string shared_with_list)
        {
            string[] sharedWithArr = shared_with_list.Split(new char[] { ';' });
            return sharedWithArr;
        }

        public static string TranslateSharedWithArrToStr(string[] sharedWithList)
        {
            if (sharedWithList == null || sharedWithList.Length == 0)
            {
                return "";
            }
            StringBuilder rt = new StringBuilder();
            for (int i = 0; i < sharedWithList.Length; i++)
            {
                rt.Append(sharedWithList[i]);
                if (i != sharedWithList.Length - 1)
                {
                    rt.Append(";");
                }
            }
            return rt.ToString();
        }

        private void UpdateOriginalPath(string path)
        {
            string local = raw.OriginalPath;
            if (path.Equals(local))
            {
                return;
            }
            //update new resutls passed.
            //update originalPath in db.
            var app = SkydrmLocalApp.Singleton;
            app.DBFunctionProvider.UpdateMyVaultLocalFileOriginalPath(Nxl_Name, path);
            raw.OriginalPath = path;
        }

        public void Upload()
        {
            try
            {
                string locaPath = Nxl_Local_Path ?? "";
                //check file exists.
                if (!FileHelper.Exist(locaPath))
                {
                    throw new Exception("Ileagal local file path state.");
                }

                // tell ServiceMgr 
                NotifyIRecentTouchedFile(EnumNxlFileStatus.Uploading);
                // Uplaod
                var App = SkydrmLocalApp.Singleton;

                App.Log.InfoFormat("###Call Upload MyVault File api, NxlLocalPath:{0}, OriginalPath:{1}, SharedEmails:{2}, Comment:{3}", Nxl_Local_Path, OriginalPath, SharedEmails, Comment);
                // For protected and share file
                App.Rmsdk.User.UploadMyVaultFile(Nxl_Local_Path, OriginalPath, SharedEmails, Comment);

                // delete in db
                App.DBFunctionProvider.DeleteMyVaultLocalFile(Nxl_Name);
                //// tell ServiceMgr
                NotifyIRecentTouchedFile(EnumNxlFileStatus.UploadSucceed);
                // Every done, begin impl leave a copy featue
                if (App.User.LeaveCopy)
                {
                    App.User.LeaveCopy_Feature.AddFile(locaPath);
                }
            }
            catch
            {
                NotifyIRecentTouchedFile(EnumNxlFileStatus.UploadFailed);
                throw;
            }
            
        }

        public void Remove()
        {
            // Delele in local disk
            var App = SkydrmLocalApp.Singleton;
            if (FileHelper.Exist(raw.Nxl_Local_Path)){
                FileHelper.Delete_NoThrow(raw.Nxl_Local_Path);
            }
            else
            {
                App.Log.Warn("file to be del,but not in local, " + raw.Nxl_Local_Path);
            }
            //Delete in db.
            App.DBFunctionProvider.DeleteMyVaultLocalFile(Nxl_Name);
            //Delete in api.
            App.Rmsdk.User.RemoveLocalGeneratedFiles(Nxl_Name);
            
            // Fix bug 51938, If LeaveCopy,should delete file in LeaveCopy Folder
            if (App.User.LeaveCopy)
            {
                var leaveACopy = SkydrmLocalApp.Singleton.User.LeaveCopy_Feature;
                if (leaveACopy.Exist(Nxl_Name))
                {
                    leaveACopy.DeleteFile(Nxl_Name);
                }
            }
            // Tell service mgr
            if (App.User.LeaveCopy)
            {
                if (raw.Status == (int)EnumNxlFileStatus.CachedFile)
                {
                    NotifyIRecentTouchedFile(EnumNxlFileStatus.Online);
                }
                else
                {
                    NotifyIRecentTouchedFile(EnumNxlFileStatus.RemovedFromLocal);
                }
            }
            else
            {
                NotifyIRecentTouchedFile(EnumNxlFileStatus.RemovedFromLocal);
            }
           
        }

        private void NotifyIRecentTouchedFile(EnumNxlFileStatus newStatus)
        {
            try
            {
                SkydrmLocalApp.
                    Singleton.
                    UserRecentTouchedFile.
                    UpdateOrInsert(newStatus,
                    Nxl_Name);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public void UploadToRms()
        {
            Upload();
        }

        public void RemoveFromLocal()
        {
            Remove();
        }

        public void ChangeSharedWithList(string[] emails)
        {
            Nxl_Shared_With_List = emails;
        }

        private class InternalFileInfo : FileInfoBaseImpl
        {
            private MyVaultLocalAddedFile Outer;

            public InternalFileInfo(MyVaultLocalAddedFile outer): base(outer.Nxl_Local_Path)
            {
                Outer = outer;
            }
                        
            public override DateTime LastModified => Outer.LastModifiedTime;

            public override string RmsRemotePath => Outer.RMSRemotePath;

            public override bool IsCreatedLocal => true;

            public override string[] Emails => Outer.Nxl_Shared_With_List;

            public override EnumFileRepo FileRepo => EnumFileRepo.REPO_MYVAULT;

            public override IFileInfo Update()
            {
                base.Update();
                return this;
            }
        }

    }
}
