using CustomControls.common.helper;
using SkydrmDesktop.rmc.featureProvider.externalDrive;
using SkydrmLocal.rmc.fileSystem.basemodel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.fileSystem.externalDrive.externalBase
{
    public class DriveFolder: NxlFolder
    {
        public IExternalDriveFile Raw { get; }

        public DriveFolder(IExternalDriveFile raw)
        {
            this.Raw = raw;
            //
            this.Name = raw.Name;
            this.Size = raw.Size;
            this.Location = EnumFileLocation.Online;
            this.FileStatus = raw.Status;
            this.FileRepo = EnumFileRepo.REPO_EXTERNAL_DRIVE;
            this.DateModified = DateTimeHelper.DateTimeToTimestamp(raw.ModifiedTme.ToLocalTime()).ToString();
            this.RawDateModified = raw.ModifiedTme;
            this.DisplayPath = raw.DisplayPath;
            this.FileRepo = EnumFileRepo.REPO_EXTERNAL_DRIVE;
            this.PathId = raw.CloudPathId;
            this.FileId = raw.FileId;
        }
    }
}
