using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.featureProvider.FileInfo;
using SkydrmLocal.rmc.fileSystem.basemodel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.featureProvider.externalDrive
{
    public abstract class ExternalDriveLocalFile : IExternalDriveLocalFile
    {
        protected database.table.externalrepo.ExternalDriveLocalFile raw;

        public ExternalDriveLocalFile(database.table.externalrepo.ExternalDriveLocalFile raw)
        {
            this.raw = raw;
        }

        #region Impl IPendingUploadFile
        public string Name { get => raw.Name; set => throw new NotImplementedException(); }

        public string LocalDiskPath { get => raw.LocalPath; set => throw new NotImplementedException(); }

        public long FileSize => raw.Size;

        public string SharedEmails => ""; // Not support

        public DateTime LastModifiedTime => raw.ModifiedTime;

        public EnumNxlFileStatus Status
        {
            get => (EnumNxlFileStatus)raw.OperationStatus; set => ChangeOperationStaus(value);
        }

        public string DisplayPath => GetFileDisplayPath();

        public virtual string PathId => GetFileCloudPathId();

        public IFileInfo FileInfo => new InternalFileInfo(this);

        public bool OverWriteUpload { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool IsExistInRemote { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        #endregion // Impl IPendingUploadFile

        //
        // Need each repository to impl for the following.
        //
        public abstract EnumFileRepo FileRepo { get; }

        // Remove file from local
        public abstract void RemoveFromLocal();

        public abstract void Upload(bool isOverWrite = false, IUploadProgressCallback callback = null);

        // Update status into db
        protected abstract void ChangeOperationStaus(EnumNxlFileStatus status);
        // Need query from db.
        protected abstract string GetFileDisplayPath();
        protected abstract string GetFileCloudPathId();

        #region FileInfo for nxl file
        private class InternalFileInfo : FileInfoBaseImpl
        {
            private ExternalDriveLocalFile Outer;

            public InternalFileInfo(ExternalDriveLocalFile outer) : base(outer.LocalDiskPath)
            {
                Outer = outer;
            }

            public override string Name => Outer.Name;

            public override long Size => Outer.FileSize;

            public override DateTime LastModified => Outer.LastModifiedTime;

            public override string RmsRemotePath => Outer.DisplayPath;

            public override bool IsCreatedLocal => true;

            public override string[] Emails => GetEmails();

            public override EnumFileRepo FileRepo => Outer.FileRepo;

            public override IFileInfo Update()
            {
                base.Update();
                return this;
            }

            // project does not supprot share
            private string[] GetEmails()
            {
                SkydrmApp.Singleton.Log.Info("local added file does not supprot share_feature");
                return new string[0];
            }
        }
        #endregion // FileInfo for nxl file
    }
}
