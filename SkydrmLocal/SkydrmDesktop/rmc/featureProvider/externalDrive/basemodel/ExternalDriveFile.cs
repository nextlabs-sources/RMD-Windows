using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.featureProvider.FileInfo;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.featureProvider.externalDrive
{
    // Derived class must impl this class.
    public abstract class ExternalDriveFile : IExternalDriveFile, IOfflineFile
    {
        private string fileId;
        private bool isFolder;
        private bool isOffline;
        private bool isFavorite;
        private string name;
        private long size;
        private string localPath;
        private string displayPath;
        private string cloudPathId;
        private string customString;
        private bool isNxlFile;
        private DateTime modifiedTime;
        private EnumNxlFileStatus status;

        protected database.table.externalrepo.ExternalDriveFile Raw;

        // Default constructor
        public ExternalDriveFile()
        {
            this.fileId = string.Empty;
            this.isFolder = false;
            this.isOffline = false;
            this.isFavorite = false;
            this.name = string.Empty;
            this.size = 0;
            this.localPath = string.Empty;
            this.displayPath = string.Empty;
            this.cloudPathId = string.Empty;
            this.customString = string.Empty;
            this.isNxlFile = false;
            this.status = EnumNxlFileStatus.Online;
        }

        // Construct from db data(Used for ListFile from db)
        public ExternalDriveFile(database.table.externalrepo.ExternalDriveFile raw)
        {
            this.Raw = raw;
            //
            this.fileId = raw.FileId;
            this.isFolder = raw.IsFolder;
            this.isOffline = raw.IsOffline;
            this.isFavorite = raw.IsFavorite;
            this.name = raw.Name;
            this.size = raw.Size;
            this.localPath = raw.LocalPath;
            this.displayPath = raw.DisplayPath;
            this.cloudPathId = raw.CloudPathId;
            this.customString = raw.CustomString;
            this.isNxlFile = raw.IsNxlFile;
            this.modifiedTime = raw.ModifiedTime;
            this.status = (EnumNxlFileStatus)raw.Status;
        }

        #region Imple IExternalDriveFile
        public string FileId { get => fileId; set => fileId = value; }

        public bool IsFolder { get => isFolder; set => isFolder = value; }

        public bool IsOffline { get => isOffline; set => UpdateOfflineBase(value); }
        public bool IsFavorite { get => isFavorite; set => isFavorite = value; }

        public string Name { get => name; set => name = value; }

        public long Size { get => size; set => size = value; }

        public string LocalPath
        {
            get
            {
                return localPath;
            }
            set
            {
                if (localPath != value)
                {
                    localPath = value;
                    UpdateLocalPath(value);
                }
            }
        }

        public string DisplayPath { get => displayPath; set => displayPath = value; }

        public string CloudPathId { get => cloudPathId; set => cloudPathId = value; }

        public string CustomString { get => customString; set => customString = value; }

        public bool IsNxlFile { get => isNxlFile; set => isNxlFile = value; }

        public DateTime ModifiedTme { get => modifiedTime; set => modifiedTime = value; }
        #endregion // Imple IExternalDriveFile


        #region // Impl IOfflineFile
        public string LocalDiskPath => localPath;

        public string RMSRemotePath => displayPath;

        public long FileSize => size;

        public DateTime LastModifiedTime => modifiedTime;

        public bool IsOfflineFileEdit => false;

        public EnumNxlFileStatus Status
        {
            get => status;
            set
            {
                if(status != value)
                {
                    status = value;
                    UpdateStatus(value);
                }
            }
        }

        public IFileInfo FileInfo => new InternalFileInfo(this);
        #endregion // Impl IOfflineFile

        public abstract void DeleteItem();

        public abstract void Download();

        public abstract void Export(string destFolder);

        // Remove local file -- used for unmark
        public abstract void RemoveFromLocal();

        // Update file status to db
        protected abstract void UpdateStatus(EnumNxlFileStatus newValue);

        private void UpdateOfflineBase(bool offline)
        {
            if(Raw == null)
            {
                return;
            }
            UpdateOffline(offline);
        }

        protected abstract void UpdateOffline(bool offline);

        protected abstract void UpdateLocalPath(string localPath);
        #region Inner class FileInfo
        private sealed class InternalFileInfo : FileInfoBaseImpl
        {
            private ExternalDriveFile outer;
            public InternalFileInfo(ExternalDriveFile outer) : base(outer.LocalDiskPath)
            {
                this.outer = outer;
            }

            // --- start--- Impl the abstract methods
            public override DateTime LastModified => outer.LastModifiedTime;

            public override string RmsRemotePath => outer.RMSRemotePath;

            public override bool IsCreatedLocal => false;

            public override string[] Emails => new string[0];

            public override EnumFileRepo FileRepo => throw new NotImplementedException();
            // --- end--- Impl the abstract methods

            // ---start--- must overwrite the below fields
            public override string Name => outer.Name;
            public override long Size => outer.Size;
            public override bool IsNormalFile => !outer.IsNxlFile;
            public override bool IsOwner
            {
                get
                {
                    if (IsNormalFile) return false;
                    else return base.IsOwner;
                }
            }
            public override bool HasAdminRights
            {
                get
                {
                    if (IsNormalFile) return false;
                    else return base.HasAdminRights;
                }
            }

            public override bool IsByAdHoc
            {
                get
                {
                    if (IsNormalFile) return false;
                    else return base.IsByAdHoc;
                }
            }
            public override bool IsByCentrolPolicy
            {
                get
                {
                    if (IsNormalFile) return false;
                    else return base.IsByCentrolPolicy;
                }
            }
            public override FileRights[] Rights
            {
                get
                {
                    if (IsNormalFile) return new FileRights[0];
                    else return base.Rights;
                }
            }

            public override string WaterMark
            {
                get
                {
                    if (IsNormalFile) return "";
                    else return base.WaterMark;
                }
            }

            public override Expiration Expiration
            {
                get
                {
                    if (IsNormalFile) return new Expiration();
                    else return base.Expiration;
                }
            }
            public override Dictionary<string, List<string>> Tags
            {
                get
                {
                    if (IsNormalFile) return null;
                    else return base.Tags;
                }
            }

            public override string RawTags
            {
                get
                {
                    if (IsNormalFile) return "";
                    else return base.RawTags;
                }
            }

            // ---end--- must overwrite the below fields
        }
        #endregion // Inner class FileInfo

    }
}
