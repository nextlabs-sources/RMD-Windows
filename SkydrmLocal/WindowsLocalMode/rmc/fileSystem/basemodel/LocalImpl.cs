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

namespace SkydrmLocal.rmc.fileSystem.basemodel
{
    public class PendingUploadFile : NxlDoc
    {
        public IPendingUploadFile Raw { get; set; }

        public PendingUploadFile(IMyVaultLocalFile raw)
        {           
            this.Raw = raw;

            this.Name = raw.Nxl_Name;
            this.Size = Convert.ToInt64(raw.FileSize);
            this.DateModified = CommonUtils.DateTimeToTimestamp(raw.Last_Modified_Time.ToLocalTime()).ToString();
            this.RawDateModified = raw.Last_Modified_Time;
            this.Location = EnumFileLocation.Local;
            this.SharedWith = CommonUtils.ConvertList2String(raw.Nxl_Shared_With_List?.ToList());

            this.LocalPath = raw.Nxl_Local_Path;
            this.RmsRemotePath = "/" + raw.Name;

            this.FileStatus = raw.Status;
            this.FileRepo = EnumFileRepo.REPO_MYVAULT;
            this.IsCreatedLocal = true;
            this.Emails = raw.Nxl_Shared_With_List;
            this.FileId = "";
        }

        public PendingUploadFile(IProjectLocalFile raw, int projectId)
        {
            this.Raw = raw;

            this.ProjectId = projectId;
            this.Name = raw.Name;
            this.Size = raw.FileSize;
            this.DateModified = CommonUtils.DateTimeToTimestamp(raw.LastModifiedTime.ToLocalTime()).ToString();
            this.RawDateModified = raw.LastModifiedTime;
            this.Location = EnumFileLocation.Local;
            this.SharedWith = "";

            this.LocalPath = raw.LocalPath;
            this.RmsRemotePath = raw.RMSRemotePath;

            this.FileStatus = raw.Status;
            this.FileRepo = EnumFileRepo.REPO_PROJECT;
            this.IsCreatedLocal = true;
            this.Emails = new string[0]; // project don't support share.
            this.FileId = ""; // should export fileId for project.
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

        public override IFingerPrint FingerPrint
        {
            get
            {
                return new InnerFingerPrint(Raw?.FileInfo);
            }
        }

        public override IFileInfo FileInfo
        {
            get
            {
                return Raw.FileInfo;
            }
        }

        public override void Upload()
        {
            Raw.UploadToRms();
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

                // Mainly used to display for fileInfo.
                this.Emails = emails;

                this.SharedWith = CommonUtils.ConvertList2String(emails.ToList());
                // trigger event
                NotifyPropertyChanged("SharedWith");
            }
        }

    }

    class InnerFingerPrint : IFingerPrint
    {
        private IFileInfo fileInfo;
        public InnerFingerPrint(IFileInfo fileInfo)
        {
            this.fileInfo = fileInfo;
        }

        public FileRights[] Rights => fileInfo.Rights;

        public Dictionary<string, List<string>> Tags => fileInfo.Tags;

        public string WaterMark => fileInfo.WaterMark;

        public Expiration Expiration => fileInfo.Expiration;

        public string RawTags => fileInfo.RawTags;
    }

}
