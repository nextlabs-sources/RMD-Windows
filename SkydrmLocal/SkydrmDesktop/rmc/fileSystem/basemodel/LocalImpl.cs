using SkydrmDesktop.rmc.featureProvider;
using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.featureProvider.common;
using SkydrmLocal.rmc.featureProvider.MyVault;
using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.ui.utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkydrmDesktop.rmc.fileSystem.utils;
using SkydrmLocal.rmc.common.helper;
using SkydrmDesktop.rmc.featureProvider.externalDrive;

namespace SkydrmLocal.rmc.fileSystem.basemodel
{
    public class PendingUploadFile : NxlDoc
    {
        public IPendingUploadFile Raw { get; set; }

        public PendingUploadFile(IPendingUploadFile raw)
        {
            this.Raw = raw;

            this.Name = raw.Name;
            this.Size = raw.FileSize;
            this.DateModified = DateTimeHelper.DateTimeToTimestamp(raw.LastModifiedTime.ToLocalTime()).ToString();
            this.RawDateModified = raw.LastModifiedTime;

            this.Location = EnumFileLocation.Local;
            // Note: don't get data by raw.FileInfo, or else it is very time consuming.
            this.SharedWith = StringHelper.ConvertString2List(raw.SharedEmails);

            this.LocalPath = raw.LocalDiskPath;
            this.DisplayPath = raw.DisplayPath;
            this.FileStatus = raw.Status;
            this.FileRepo = raw.FileRepo;
            this.IsCreatedLocal = true;
            
            this.FileId = "";
            this.SourcePath = GetSourcePath();

            this.IsNxlFile = raw.Name.EndsWith(".nxl") ? true : false;
        }

        // For project
        private string proDisplayName;
        public PendingUploadFile(IPendingUploadFile raw, int projectId, string proName = ""):this(raw)
        {
            this.RepoId = projectId.ToString();
            this.proDisplayName = proName;
            this.SourcePath = GetSourcePath();
        }
        // For SharedWorkSpace
        public PendingUploadFile(IPendingUploadFile raw, string repoId, string repoName) : this(raw)
        {
            this.RepoId = repoId;
            this.proDisplayName = repoName;
            this.SourcePath = GetSourcePath();
        }

        // Used to construct one node to display in listview ui that stands for uploading progress
        // when user directly select one file from local to upload to drive
        // (myDrive and external repository: googleDrive\dropbox\oneDrive and so on.)
        public PendingUploadFile(string localPath, string name, long size, EnumFileRepo fileRepo)
        {
            this.LocalPath = localPath;
            this.Name = name;
            this.Size = size;
            this.FileRepo = fileRepo;
            //
            this.IsCreatedLocal = false;
            this.Location = EnumFileLocation.Local;
            this.FileStatus = EnumNxlFileStatus.WaitingUpload;
            this.SourcePath = "";
        }

        private string GetSourcePath()
        {
            if (FileRepo == EnumFileRepo.REPO_MYVAULT)
                return "SkyDRM://" + FileSysConstant.MYVAULT + DisplayPath;
            else if (FileRepo == EnumFileRepo.REPO_MYDRIVE)
                return "SkyDRM://" + FileSysConstant.MYDRIVE + DisplayPath;
            else if (FileRepo == EnumFileRepo.REPO_WORKSPACE)
                return "SkyDRM://" + FileSysConstant.WORKSPACE + DisplayPath;
            else if (FileRepo == EnumFileRepo.REPO_PROJECT)
                return "SkyDRM://" + FileSysConstant.PROJECT + "/" + proDisplayName + DisplayPath;
            else if (FileRepo == EnumFileRepo.REPO_EXTERNAL_DRIVE)
                return "SkyDRM://" + FileSysConstant.REPOSITORIES + "/" + proDisplayName + DisplayPath;

            return DisplayPath;
        }

        public override string Name
        {
            get
            {
                return Raw.Name;
            }

            set
            {
                Raw.Name = value;
                NotifyPropertyChanged("Name");
            }
        }

        public override string LocalPath
        {
            get
            {
                return Raw.LocalDiskPath;
            }

            set
            {
                Raw.LocalDiskPath = value;
                NotifyPropertyChanged("LocalPath");
            }
        }

        public override EnumNxlFileStatus FileStatus
        {
            get
            {
                return Raw.Status;
            }

            set
            {
                Raw.Status = value;
                NotifyPropertyChanged("FileStatus");
            }
        }

        public override IFileInfo FileInfo
        {
            get
            {
                return Raw.FileInfo;
            }
        }

        public override void Upload(bool isOverWrite = false, IUploadProgressCallback callback = null)
        {
            Raw.Upload(isOverWrite, callback);
        }

        public override void Remove()
        {
            Raw.RemoveFromLocal();
        }

        public virtual void ChangeSharedWithList(string[] emails)
        {
            if(Raw is MyVaultLocalAddedFile)
            {
                var doc = (MyVaultLocalAddedFile)Raw;
                doc.ChangeSharedWithList(emails);

                // trigger event
                NotifyPropertyChanged("SharedWith");
            }
        }

    }

}
