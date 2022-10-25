using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkydrmDesktop.rmc.database.table.externalrepo.sharepoint;
using SkydrmDesktop.rmc.featureProvider.MessageNotify;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.featureProvider.FileInfo;
using SkydrmLocal.rmc.fileSystem.basemodel;

namespace SkydrmDesktop.rmc.featureProvider.externalDrive.sharepoint
{
    public abstract class SharePointBaseLocalFile : ExternalDriveLocalFile
    {
        private readonly NxSharePointBase mHost;

        public SharePointBaseLocalFile(NxSharePointBase host, SharePointDriveLocalFile raw) : base(raw)
        {
            this.mHost = host;
        }

        public override EnumFileRepo FileRepo => EnumFileRepo.REPO_EXTERNAL_DRIVE;

        public override void RemoveFromLocal()
        {
            // delete at local disk
            FileHelper.Delete_NoThrow(raw.LocalPath);
            // remove from database
            SkydrmApp.Singleton.DBFunctionProvider.DeleteSharePointDriveLocalFile(raw.Id);
            // tell skd to remove it
            SkydrmApp.Singleton.Rmsdk.User.RemoveLocalGeneratedFiles(Name);
        }

        public override void Upload(bool isOverWrite = false, IUploadProgressCallback callback = null)
        {
            if (string.IsNullOrEmpty(PathId))
            {
                throw new Exception("The parent pathId is empty");
            }
            try
            {
                // Invoke
                mHost.Upload(LocalDiskPath, Name, PathId, isOverWrite, callback);
                // delete from local file db
                SkydrmApp.Singleton
                    .DBFunctionProvider
                    .DeleteSharePointDriveLocalFile(raw.Id);

                if (SkydrmApp.Singleton.User.LeaveCopy)
                {
                    SkydrmApp.Singleton.User.LeaveCopy_Feature.AddFile(LocalDiskPath);
                    FileHelper.Delete_NoThrow(LocalDiskPath);
                }
            }
            catch (Exception e)
            {
                SkydrmApp.Singleton.MessageNotify.NotifyMsg(raw.Name, e.Message, EnumMsgNotifyType.LogMsg,
                    MsgNotifyOperation.UPLOAD, EnumMsgNotifyResult.Failed, EnumMsgNotifyIcon.WaitingUpload);

                throw;
            }
        }

        protected override void ChangeOperationStaus(EnumNxlFileStatus status)
        {
            if (raw.OperationStatus == (int)status)
            {
                return;
            }
            // change db
            SkydrmApp.Singleton
                .DBFunctionProvider
                .UpdateSharePointDriveLocalFileStatus(raw.Id, (int)status);
            // update cache
            raw.OperationStatus = (int)status;
        }

        protected override string GetFileDisplayPath()
        {
            var folder = SkydrmApp.Singleton
                .DBFunctionProvider
                .QuerySharePointDriveFilePathId(raw.ExternalDriveFileTablePk);

            if (!folder.EndsWith("/"))
            {
                folder += "/";
            }

            return folder + raw.Name;
        }

        protected override string GetFileCloudPathId()
        {
            return SkydrmApp.Singleton
                .DBFunctionProvider
                .QuerySharePointDriveFilePathId(raw.ExternalDriveFileTablePk);
        }

    }
}
