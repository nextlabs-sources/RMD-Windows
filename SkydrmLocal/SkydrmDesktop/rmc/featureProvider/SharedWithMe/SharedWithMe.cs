using System;
using System.Collections.Generic;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SkydrmDesktop;
using SkydrmDesktop.Resources.languages;
using SkydrmDesktop.rmc.featureProvider.MessageNotify;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.featureProvider.common;
using SkydrmLocal.rmc.featureProvider.FileInfo;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.sdk;
using static Skydrmlocal.rmc.database2.FunctionProvider;
using SkydrmLocal.rmc.drive;

namespace SkydrmLocal.rmc.featureProvider.SharedWithMe
{
    public sealed class SharedWithMe : ISharedWithMe
    {
        private string working_path;
        public string Dir => working_path;

        readonly private SkydrmApp app;
        readonly private log4net.ILog log;

        public SharedWithMe(SkydrmApp app)
        {
            this.app = app;
            this.log = app.Log;

            // make sure working path exists  <= IUser
            working_path = app.User.WorkingFolder + "\\SharedWithMe" ;
            if (!FileHelper.Exist(working_path))
            {
                Directory.CreateDirectory(working_path);
            }
        }

        public ISharedWithMeFile[] List()
        {            
            var rt = new List<SharedWithMeFile>();
            foreach (var i in app.DBFunctionProvider.ListSharedWithMeFile())
            {
                rt.Add(new SharedWithMeFile(app, Dir, i));
            }
            // by osmond, I made a mistake by impled some logic code in SharedWithMeFile's contructor
            // which may in time-consuming
            // init the list at backgroudn
            ThreadPool.QueueUserWorkItem((theList) =>
            {
                bool bTellUI = false;
                foreach (var i in (List<SharedWithMeFile>)theList)
                {
                    bool bModified=i.Initialize();
                    if (bModified)
                    {
                        bTellUI = true;
                    }
                }
                if (bTellUI)
                {
                    app.InvokeEvent_MyVaultOrSharedWithmeFileLowLevelUpdated();
                }
            }, rt);
            return rt.ToArray();
        }

        private SharedWithMeFileInfo[] FilterOutNotModified(
            ISharedWithMeFile[] locals, 
            SharedWithMeFileInfo[] remotes)
        {
            if (locals.Length == 0)
            {
                return remotes;
            }
            var rt = new List<SharedWithMeFileInfo>();
            foreach (var i in remotes)
            {
                // find in local
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
                        app.Log.Info("SharedWithMe local list no matching element");
                        rt.Add(i);
                        continue;
                    }

                    // judege whether modified

                    // only care file size
                    if (i.size  != l.FileSize )
                    {
                        rt.Add(i);
                    }
                }
                catch (Exception e)
                {
                    app.Log.Error(e);
                    // local find error
                    rt.Add(i);
                }
            }
            return rt.ToArray();
        }


        public ISharedWithMeFile[] Sync()
        {
            lock (this)
            {
                var remotes = app.Rmsdk.User.ListSharedWithMeFile();
                var locals = List();

                // find difference set by (Local - remote) , and delete it 
                // delete file that had been deleted on remote but still in local

                // Note: SharedWithMe list can contains same name files, which were shared by different users.
                // So can't compare by name,fix bug 63308
                var diffset = from i in locals
                              let rDuid = from j in remotes select j.duid
                              where !rDuid.Contains(i.Duid)
                              select i;
                foreach (var i in diffset)
                {
                    app.DBFunctionProvider.DeleteSharedWithMeFile(i.Name);
                }


                var ff = new List<InsertSharedWithMeFile>();
                foreach (var i in FilterOutNotModified(locals,remotes))
                {
                    ff.Add(new InsertSharedWithMeFile()
                    {
                        duid=i.duid,
                        name=i.nxlName,
                        type=i.fileType,
                        size=i.size,
                        shared_date=i.sharedDateJavaTimeMillis,
                        shared_by=i.sharedByWho,
                        transcation_id=i.transactionId,
                        transcation_code=i.transactionCode,
                        shared_link_url=i.sharedlinkUrl,
                        rights_json=i.rights,
                        comments=i.comments,
                        is_owner=i.isOwner
                    });
                }
                app.DBFunctionProvider.UpsertSharedWithMeFileBatch(ff.ToArray());
                return List();
            }
            
        }

        public void OnHeartBeat()
        {
            Sync();
        }

        public IOfflineFile[] GetOfflines()
        {
            var rt = new List<IOfflineFile>();
            foreach (var i in List())
            {
                if (i.IsOffline || i.Status== EnumNxlFileStatus.AvailableOffline)
                {
                    rt.Add(i as SharedWithMeFile);
                }
            }
            return rt.ToArray();
        }

        public IPendingUploadFile[] GetPendingUploads()
        {
            // by design, SharedWithMe does not supprot Uplaod by now
            return new IPendingUploadFile[0];
        }
    }

    public sealed class SharedWithMeFile : ISharedWithMeFile, IOfflineFile
    {
        readonly private SkydrmApp app;
        readonly private log4net.ILog log;
        rmc.database.table.sharedwithme.SharedWithMeFile raw;
        string working_dir;
        InternalFileInfo fileInfo;// each get will generate a new-one
        private string partialLocalPath;

        public SharedWithMeFile(SkydrmApp app,
            string homePath,
            rmc.database.table.sharedwithme.SharedWithMeFile raw)
        {
            this.app = app;
            this.log = app.Log;
            this.raw = raw;
            this.working_dir = homePath;
        }
        // return true, some fileds has been modified
        // return flase, no one modifed.
        public bool Initialize()
        {
            bool bModified = false;
            //
            // file status auto fix
            //
            if (raw.Is_offline)
            {
                if (!FileHelper.Exist(raw.Local_path))
                {
                    Status = EnumNxlFileStatus.Online;
                    IsOffline = false;
                    bModified = true;
                }
                else
                {
                    if (Status != EnumNxlFileStatus.AvailableOffline)
                    {
                        Status = EnumNxlFileStatus.AvailableOffline;
                        bModified = true;
                    }
                }
            }
            // comments by Allen: avoid 52838, online autofix in not important
            //else // Not offline
            //{

            //    var path = working_dir + "\\" + Name;
            //    if (FileHelper.Exist(path))
            //    {
            //        // May file has downloaded but the "status" and "Is_offline" has not been modified immediately since db operation delay, 
            //        // fix bug 52838 about sharedWithMe mark offline issue(when user switch back and forth treeview item very fast during file downloading)
            //        if (Status != EnumNxlFileStatus.Downloading)
            //        {
            //            FileHelper.Delete_NoThrow(path);
            //        }
            //    }

            //    // May file is downloading, fix bug 52838 about sharedWithMe mark offline issue(Also should add the condition that status can't equal Downloading).
            //    if (Status != EnumNxlFileStatus.Online && Status != EnumNxlFileStatus.Downloading)
            //    {
            //        // reset online status
            //        Status = EnumNxlFileStatus.Online;
            //        bModified = true;
            //    }

            //}

            return bModified;
        }

        public string Name => raw.Name;

        public string Duid => raw.Duid;

        public string Type => raw.Type;

        public long FileSize => raw.Size;

        public DateTime SharedDate => raw.Shared_date;

        public string SharedBy => raw.Shared_by;

        public string SharedLinkeUrl => raw.Shared_link_url;

        public FileRights[] Rights => GetRights();

        public string Comments => raw.Comments;

        public bool IsOwner => raw.Is_owner;

        public bool IsOffline { get => raw.Is_offline; set => OnChangedIsOfflineMark(value); }


        public EnumNxlFileStatus Status
        {
            get => (EnumNxlFileStatus)raw.Operation_status;
            set => OnChangeOperationStatus(value);
        }

        public bool IsEdit
        {
            get => raw.Edit_Status != 0;
            set => UpdateEditStatus(value ? 1 : 0);
        }

        public bool IsOfflineFileEdit => IsEdit;

        public bool IsModifyRights
        {
            get => raw.Modify_Rights_Status != 0;
            set => UpdateModifyRightsStatus(value ? 1 : 0);
        }

        public string PartialLocalPath
        {
            get
            {
                if (string.IsNullOrEmpty(partialLocalPath))
                {
                    partialLocalPath = GetPartialLocalPath();
                }

                if (!FileHelper.Exist(partialLocalPath))
                {
                    partialLocalPath = "";
                }

                return partialLocalPath;
            }
        }

        #region IOffline Used
        public string LocalDiskPath => raw.Local_path;

        public DateTime LastModifiedTime => SharedDate;

        public string RMSRemotePath => "/"+raw.Name;

        public IFileInfo FileInfo => new InternalFileInfo(this);

        public string TransactionId => raw.Transaction_id;
        #endregion

        private string GetPartialLocalPath()
        {
            return working_dir + @"\" + "partial_" + raw.Name;
        }

        private FileRights[] GetRights()
        {           
            return NxlHelper.FromRightStrings(JsonConvert.DeserializeObject<string[]>(raw.Rights_json));
        }


        private void OnChangedIsOfflineMark(bool newIsOffline)
        {
            // sanity check
            if (raw.Is_offline == newIsOffline)
            {
                return;
            }
            // update db
            app.DBFunctionProvider.UpdateSharedWithMeFileIsOffline(raw.Id, newIsOffline);
            // update cache
            raw.Is_offline = newIsOffline;
        }

        private void OnChangeOperationStatus(EnumNxlFileStatus newStatus)
        {
            if (raw.Operation_status == (int)newStatus)
            {
                return;
            }
            // change db
            app.DBFunctionProvider.UpdateSharedWithMeFileOperationStatus(
                raw.Id, (int)newStatus);

            // update cache;
            raw.Operation_status = (int)newStatus;
            //
            if (newStatus == EnumNxlFileStatus.Online)
            {
                IsOffline = false;
            }
            if (newStatus == EnumNxlFileStatus.AvailableOffline)
            {
                IsOffline = true;
            }

        }

        private void UpdateEditStatus(int newStatus)
        {
            // Check changable first.
            if(raw.Edit_Status == newStatus)
            {
                return;
            }
            // Update db.
            app.DBFunctionProvider.UpdateSharedWithMeFileEditStatus(raw.Id, newStatus);
            // Update cache.
            raw.Edit_Status = newStatus;
        }

        private void UpdateModifyRightsStatus(int newStatus)
        {
            // Check changable first.
            if(raw.Modify_Rights_Status == newStatus)
            {
                return;
            }
            // Update db.
            app.DBFunctionProvider.UpdateSharedWithMeFileModifyRightsStatus(raw.Id, newStatus);
            // Update cache.
            raw.Modify_Rights_Status = newStatus;
        }

        public void Export(string destinationPath)
        {
            app.Log.Info("ShareWithMe Export As file,path=" + destinationPath);

            string currentUsertempPathOrDownloadFilePath = System.IO.Path.GetTempPath();

            try
            {
                // by commend, sdk will help us to record log: DownloadForOffline
                //app.User.AddNxlFileLog(raw.Local_path, NxlOpLog.Download, true);

                app.Log.Info("\t\t SharedWithMe Export \r\n" +
                             "\t\t\t\t transactionId :" + raw.Transaction_id +"\r\n"+
                             "\t\t\t\t transactionCode :" + raw.Transaction_code + "\r\n"+
                             "\t\t\t\t DestLocalFodler :" + currentUsertempPathOrDownloadFilePath + "\r\n"+
                             "\t\t\t\t isForViewOnly :" + false+ "\r\n");

                app.Rmsdk.User.CopyNxlFile(Name, RMSRemotePath, NxlFileSpaceType.shared_with_me, "",
                    Path.GetFileName(destinationPath), currentUsertempPathOrDownloadFilePath, NxlFileSpaceType.local_drive, "",
                    true, raw.Transaction_code, raw.Transaction_id);

                //app.Rmsdk.User
                //    .DownLoadSharedWithMeFile(
                //        raw.Transaction_id,
                //        raw.Transaction_code,
                //        ref currentUsertempPathOrDownloadFilePath,false);

                string downloadFilePath = currentUsertempPathOrDownloadFilePath + Path.GetFileName(destinationPath);

                app.Log.Info("\t\t SharedWithMe Export DownLoaded SharedWithMe File \r\n" +
                             "\t\t\t\t downloadFilePath :" + downloadFilePath + "\r\n");

                File.Copy(downloadFilePath, destinationPath, true);
            }
            catch (Exception e)
            {
                app.Log.Error("failed in Export file=" + currentUsertempPathOrDownloadFilePath, e);      
              
                throw;
            }
            finally
            {
                FileHelper.Delete_NoThrow(currentUsertempPathOrDownloadFilePath + Path.GetFileName(destinationPath));
            }
        }

        public void Download(bool isForViewOnly = false)
        {
            app.Log.Info("SharedWithMe File download");
            OnChangeOperationStatus(EnumNxlFileStatus.Downloading);
            var localPath = working_dir + @"\" + raw.Name;
            try
            {
                // before download, delete same file at local first
                FileHelper.Delete_NoThrow(localPath);
                var outdir = working_dir;
                app.Rmsdk.User.DownLoadSharedWithMeFile(raw.Transaction_id, raw.Transaction_code,
                        ref outdir, isForViewOnly);

                // check out path, if file name exceed 128 characters, the server return name will be truncated.
                if (!localPath.Equals(outdir, StringComparison.CurrentCultureIgnoreCase))
                {
                    // use for delete file.
                    localPath = outdir;
                    throw new SkydrmException(CultureStringInfo.ApplicationFindResource("Notify_PopBubble_Download_FileName128"),
                        ExceptionComponent.FEATURE_PROVIDER);
                }

                // update loacal path in db
                OnChangeLocalPath(localPath);

                OnChangeOperationStatus(EnumNxlFileStatus.DownLoadedSucceed);

            }
            catch (Exception e)
            {
                app.Log.Error("failed in downlaod file=" + localPath, e);
                OnChangeOperationStatus(EnumNxlFileStatus.DownLoadedFailed);

                FileHelper.Delete_NoThrow(localPath);
                throw;
            }
        }

        // Download partial file to check file rights.
        public void DownloadPartial()
        {
            var app = SkydrmApp.Singleton;

            // File name is attached prefix "partial" returned by sdk.
            var partialFPath = working_dir + @"\" + "partial_" + raw.Name;

            app.Log.Info("partical downlaod path: " + partialFPath);
            try
            {
                //before download delete exist
                FileHelper.Delete_NoThrow(partialFPath);

                var outdir = working_dir;
                app.Rmsdk.User.DownLoadSharedWithMePartialFile(raw.Transaction_id, raw.Transaction_code, ref outdir);
                partialFPath = outdir;
                // update loacal path in db
                partialLocalPath = partialFPath;
            }
            catch (Exception e)
            {
                app.Log.Error("failed partial downlond file=" + partialFPath, e);
                FileHelper.Delete_NoThrow(partialFPath);
                
                throw e;
            }
        }

        public void Remove()
        {
            app.Log.Info("SharedWithMe File remove");
            // tell skd to remove it, ignore error        
            //NxlHelper.TellSDKRemoveNxlFile(LocalDiskPath.Length > 0 ? LocalDiskPath : Name);
            // update in db
            app.DBFunctionProvider.UpdateSharedWithMeFileOperationStatus(
                raw.Id, (int)EnumNxlFileStatus.Online);
            // update its local path as "" in db
            OnChangeLocalPath("");
            // delete local copy
            var path = working_dir + "\\" + Name;
            if (FileHelper.Exist(path))
            {
                FileHelper.Delete_NoThrow(path);
            }
            else
            {
                FileHelper.Delete_NoThrow(LocalDiskPath);
            }
        }

        private void OnChangeLocalPath(string newPath)
        {
            app.Log.Info("SharedWithMe File changeLocalPath");
            if (raw.Local_path.Equals(newPath))
            {
                return;
            }
            // update db
            app.DBFunctionProvider.UpdateSharedWithMeFileLocalpath(raw.Id, newPath);
            // update cache
            raw.Local_path = newPath;
        }

        //private void NotifyIRecentTouchedFile(EnumNxlFileStatus newStatus)
        //{
        //    try
        //    {
        //        SkydrmApp.
        //            Singleton.
        //            UserRecentTouchedFile.
        //            UpdateOrInsert(newStatus,
        //            Name);
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e);
        //    }
        //}

        

        public void RemoveFromLocal()
        {
            Remove();
        }

        public bool ShareFile(string nxlLocalPath, string[] addEmails, string[] removeEmails, string comment="")
        {
            // For sharedwithme file, as owner share nxl throught UpdateRecipents
            // otherwise if has share rights call reShare.

            return IsOwner ? UpdateRecipents(nxlLocalPath, addEmails, removeEmails, comment)
                : Reshare(addEmails);
        }

        private bool UpdateRecipents(string nxlLocalPath, string[] addEmails, string[] removeEmails, string comment)
        {
            var app = SkydrmApp.Singleton;

            if(string.IsNullOrEmpty(nxlLocalPath))
            {
                return false;
            }

            List<string> addedRecipents = addEmails == null ? new List<string>() : addEmails.ToList();
            List<string> removedRecipents = removeEmails == null ? new List<string>() : removeEmails.ToList();

            //If add emails & remove emails are both empty, just ignore this update request.
            if (addedRecipents.Count == 0 && removedRecipents.Count == 0)
            {
                return false;
            }

            app.Rmsdk.User.UpdateRecipients(nxlLocalPath, addedRecipents, removedRecipents, comment);

            return true;
        }

        private bool Reshare(string[] emails)
        {
            if (emails == null || emails.Length == 0)
            {
                throw new Exception("One more shared with email list required.");
            }

            var app = SkydrmApp.Singleton;

            string tid = raw.Transaction_id;
            string tcode = raw.Transaction_code;

            return app.Rmsdk.User.SharedWithMeReshareFile(tid, tcode, emails);
        }

        private class InternalFileInfo : FileInfoBaseImpl
        {
            private SharedWithMeFile Outer;

            public InternalFileInfo(SharedWithMeFile outer): base(outer.PartialLocalPath) //Use Partial_LocalPath get Rights, Change 'LocalPath' to 'Partial_LocalPath'
            {
                Outer = outer;
            }

            public override string Name => Outer.Name;

            public override long Size => Outer.FileSize;

            public override DateTime LastModified => Outer.LastModifiedTime;

            public override string RmsRemotePath => Outer.RMSRemotePath;

            public override bool IsCreatedLocal => false;

            public override string[] Emails => GetEmails();

            public override EnumFileRepo FileRepo => EnumFileRepo.REPO_SHARED_WITH_ME;

            public override IFileInfo Update()
            {
                // future feature
                base.Update();
                return this;
            }

            private string[] GetEmails()
            {
                return new string[1] { Outer.SharedBy };
            }

        }

    }
}
