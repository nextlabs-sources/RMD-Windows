using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkydrmDesktop.rmc.database.table.externalrepo.sharepoint;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.fileSystem.basemodel;

namespace SkydrmDesktop.rmc.featureProvider.externalDrive.sharepoint
{
    public abstract class SharePointBaseFile : ExternalDriveFile
    {
        private readonly NxSharePointBase mHost;
        private bool isSite;
        private string cacheFolder;

        public bool IsSite { get => isSite; set => isSite = value; }

        public SharePointBaseFile(NxSharePointBase host)
        {
            this.mHost = host;
        }

        public SharePointBaseFile(NxSharePointBase host, SharePointDriveFile raw) : base(raw)
        {
            this.mHost = host;
            InitCacheFolder(host.WorkingPath);
        }

        private void InitCacheFolder(string homePath)
        {
            this.cacheFolder = homePath;
            FileHelper.CreateDir_NoThrow(cacheFolder);
        }

        public override void Download()
        {
            if (IsFolder)
            {
                return;
            }

            // Since googleDrive have the same name folder, so we directly download the file
            // into the local directory which is created by its fId.
            var parentFolder = this.cacheFolder + @"\" + CloudPathId;
            FileHelper.CreateDir_NoThrow(parentFolder);
            var localPath = parentFolder + @"\" + Name;

            UpdateStatus(EnumNxlFileStatus.Downloading);
            // delete previous file
            FileHelper.Delete_NoThrow(localPath, true);
            try
            {
                mHost.Download(localPath, CloudPathId, Size);

                // update local path into db
                this.LocalPath = localPath;
                UpdateStatus(EnumNxlFileStatus.DownLoadedSucceed);
            }
            catch (Exception e)
            {
                UpdateStatus(EnumNxlFileStatus.DownLoadedFailed);
                // del 
                FileHelper.Delete_NoThrow(localPath);
                throw e;
            }

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
                var path = cacheFolder + "\\" + this.LocalPath;
                if (!Alphaleonis.Win32.Filesystem.File.Exists(path))
                {
                    return;
                }
                try
                {
                    Alphaleonis.Win32.Filesystem.File.Delete(cacheFolder + "\\" + this.LocalPath);
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

            SkydrmApp.Singleton.DBFunctionProvider.UpdateSharePointDriveFileOfflineMark(Raw.Id, offline);

            Raw.IsOffline = offline;
        }

        protected override void UpdateStatus(EnumNxlFileStatus newValue)
        {
            // Update vaultfile status in db.
            SkydrmApp.Singleton
                .DBFunctionProvider
                .UpdateSharePointDriveFileStatus(Raw.Id, (int)newValue);

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
            if (Raw == null || Raw.LocalPath == localPath)
            {
                return;
            }
            // update db
            SkydrmApp.Singleton
                .DBFunctionProvider
                .UpdateSharePointDriveFileLocalPath(Raw.Id, localPath);

            // update cache
            Raw.LocalPath = localPath;
        }

        public override void DeleteItem()
        {
        }

        public override void Export(string destFolder)
        {
        }

    }

}
