using SkydrmDesktop.rmc.featureProvider.externalDrive;
using SkydrmDesktop.rmc.fileSystem.utils;
using SkydrmLocal.rmc.fileSystem;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.ui.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.fileSystem
{
    // Some methods for 'IFileRepo' are blur, should refactor later.
    public abstract class AbstractFileRepo : IFileRepo
    {
        public abstract string RepoDisplayName { get; set; }
        public abstract string RepoType { get; }
        public virtual string RepoId { get; set; } = ""; // Empty in default.

        // The default folder is root folder "/", which is mainly used for GetFolderPathId method.
        public NxlFolder CurrentWorkingFolder { get; set; } = new NxlFolder();

        public virtual RepositoryProviderClass RepoProviderClass { get => RepositoryProviderClass.UNKNOWN; }

        public abstract IList<INxlFile> GetFilePool();

        public abstract IList<INxlFile> GetWorkingFolderFilesFromDB();

        public abstract void SyncFiles(OnRefreshComplete results, string itemFlag = null);

        public abstract IList<INxlFile> GetOfflines();

        // The repository with folder should impl it.
        public virtual void GetRmsFilesRecursivelyFromDB(string pathId, IList<INxlFile> results)
        {
            throw new NotImplementedException();
        }

        public virtual void SyncParentNodeFile(INxlFile file, ref IList<INxlFile> results)
        {
            throw new NotImplementedException();
        }

        // The repository with folder should impl it.
        public virtual void SyncFilesRecursively(string pathId, IList<INxlFile> results)
        {
            throw new NotImplementedException();
        }

        public virtual void SyncDestFile(INxlFile selectedFile, OnSyncDestComplete result, bool bNeedFindParent = false)
        {
            // Mainly used for edit.
            throw new NotImplementedException();
        }

        public virtual IList<INxlFile> GetPendingUploads()
        {
            // Ignore for external drive ?
            throw new NotImplementedException();
        }

        public virtual IList<INxlFile> GetSharedByMeFiles()
        {
            // Ignore for external drive
            throw new NotImplementedException();
        }
        public virtual void SyncSharedByMeFiles(OnSyncComplete callback)
        {
            // Ignore for external drive
            throw new NotImplementedException();
        }

        public virtual IList<INxlFile> GetSharedWithMeFiles()
        {
            // Ignore for external drive
            throw new NotImplementedException();
        }
        public virtual void SyncSharedWithMeFiles(OnSyncComplete callback)
        {
            // Ignore for external drive
            throw new NotImplementedException();
        }

        public virtual void MarkOffline(INxlFile nxlFile, OnDownloadComplete callback)
        {
            DownloadFile(nxlFile, false, callback);
        } 

        // Maybe we should provide a new interface like "DownloadFileEx" with download progress and Cancel operation.
        public abstract void DownloadFile(INxlFile nxl, bool isViewOnly, OnDownloadComplete callback,
            bool isDownloadPartial = false, bool isOnlineView = false);

        public virtual void CreateFolder(string name, string parantFolder)
        {
            throw new NotImplementedException();
        }
        public virtual void UploadFile(string fileLocalPath, string destFolder, OnOprationComplete callback, bool overwrite = false)
        {
            throw new NotImplementedException();
        }

        public virtual void UploadFileEx(string localPath, string name, string cloudPathId,
            bool isOverwrite = false, IUploadProgressCallback callback = null)
        {
            throw new NotImplementedException();
        }

        public virtual void UpdateToken(string newToken)
        {
            throw new NotImplementedException();
        }

        public virtual bool CheckFileExists(string pathId)
        {
            throw new NotImplementedException();
        }
    }
}
