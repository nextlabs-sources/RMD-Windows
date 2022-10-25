using CustomControls.common.helper;
using SkydrmDesktop.rmc.featureProvider.externalDrive;
using SkydrmDesktop.rmc.fileSystem.utils;
using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.featureProvider.FileInfo;
using SkydrmLocal.rmc.fileSystem.basemodel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.fileSystem.externalDrive.externalBase
{
    public class DriveDoc: NxlDoc
    {
        public IExternalDriveFile Raw { get; }

        public DriveDoc(IExternalDrive drive, IExternalDriveFile raw)
        {
            this.Raw = raw;
            //
            this.Raw = raw;
            //
            this.Name = raw.Name;
            this.Size = raw.Size;
            this.LocalPath = raw.LocalPath;
            this.FileRepo = EnumFileRepo.REPO_EXTERNAL_DRIVE;
            this.DateModified = DateTimeHelper.DateTimeToTimestamp(raw.ModifiedTme.ToLocalTime()).ToString();
            this.RawDateModified = raw.ModifiedTme;
            this.FileId = raw.FileId;
            // 
            this.Location = InitLocation(raw);
            this.DisplayPath = raw.DisplayPath; // need to check.
            this.PathId = raw.CloudPathId;
            this.IsMarkedOffline = raw.IsOffline;

            this.FileStatus = raw.Status; 
            this.FileRepo = EnumFileRepo.REPO_EXTERNAL_DRIVE;
            this.IsCreatedLocal = false;
            this.FileId = raw.FileId;
            //this.PartialLocalPath = raw.Partial_Local_Path;  // Should support this later.
            this.SourcePath = "SkyDRM://" + drive.DisplayName + "//" + Raw.DisplayPath;
        }

        public override EnumNxlFileStatus FileStatus
        {
            get
            {
                return Raw.Status;
            }
            set
            {
                Raw.Status = value;// Will update into db in low level.
                NotifyPropertyChanged("FileStatus");
            }
        }

        public override void Remove()
        {
            Raw?.DeleteItem();
        }

        public override void DownloadFile(bool isViewOnly = false)
        {
            Raw?.Download();
            // update
            this.LocalPath = Raw?.LocalPath;
        }

        public override bool UnMark()
        {
            try
            {
                Raw?.RemoveFromLocal();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }
            return true;
        }

        public override void DownloadPartial()
        {
            // todo.
        }

        public override IFileInfo FileInfo => new InternalFileInfo(Raw);

        private EnumFileLocation InitLocation(IExternalDriveFile raw)
        {
            return raw.IsOffline ? EnumFileLocation.Local : EnumFileLocation.Online;
        }

        #region FileInfo for nxl
        private class InternalFileInfo : FileInfoBaseImpl
        {
            public IExternalDriveFile Outer;

            public InternalFileInfo(IExternalDriveFile outer) : base(outer.LocalPath)
            {
                this.Outer = outer;
            }

            public override long Size => Outer.Size;

            public override string Name => Outer.Name; 

            public override DateTime LastModified => Outer.ModifiedTme;

            public override string RmsRemotePath => Outer.DisplayPath;

            public override bool IsCreatedLocal => false;

            public override string[] Emails => new string[0];

            public override EnumFileRepo FileRepo => EnumFileRepo.REPO_EXTERNAL_DRIVE;
        }
        #endregion // FileInfo for nxl
    }
}
