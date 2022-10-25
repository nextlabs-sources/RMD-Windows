using SkydrmDesktop.rmc.featureProvider.externalDrive;
using SkydrmDesktop.rmc.fileSystem.utils;
using SkydrmLocal.rmc.Edit;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.ui.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.fileSystem
{
    // Down complete
    public delegate void OnDownloadComplete(bool result);
    public delegate void OnOprationComplete(bool isSucceed);

    public interface IFileRepo
    {
        /// <summary>
        /// Get repo display name, for external repository, user can change the display name.
        /// </summary>
        string RepoDisplayName { get; set; }

        string RepoType { get; }

        /// <summary>
        /// Get the external repository provider class.
        /// </summary>
        RepositoryProviderClass RepoProviderClass { get; }

        /// <summary>
        /// Get repo id(or project id string format) if the repository has, maily for external drive.
        /// For project, return current working project id string format.
        /// </summary>
        string RepoId { get; }

        /// <summary>
        /// Record the current working folder of this file repository.
        /// </summary>
        NxlFolder CurrentWorkingFolder { get; set; }

        /// <summary>
        /// Get each repo file pool.
        /// For project, return current working project file pool.
        /// </summary>
        IList<INxlFile> GetFilePool();

        void MarkOffline(INxlFile nxlFile, OnDownloadComplete callback);

        void DownloadFile(INxlFile nxl, bool isViewOnly, OnDownloadComplete callback, bool isDownloadPartial = false, bool isOnlineView = false);

        /// <summary>
        /// Get current folder all files from rms, asynchronous operation.
        /// </summary>
        /// <param name="itemFlag">Can be projectId or folder pathid. If as pathId, the defalt is root path '/'.</param>
        /// <param name="results">call back</param>
        void SyncFiles(OnRefreshComplete results, string itemFlag = null);

        /// <summary>
        /// Sync obtain all files of the parent node of the file
        /// </summary>
        /// <param name="file"></param>
        /// <param name="results"></param>
        void SyncParentNodeFile(INxlFile file, ref IList<INxlFile> results);

        /// <summary>
        /// Sync specified folder's all files from rms recursively, synchronous operation.
        /// </summary>
        /// <param name="pathId">node path id. Note: must pass "Path" for Shared Workspace.</param>
        /// <param name="results">returned file nodes</param>
        void SyncFilesRecursively(string pathId, IList<INxlFile> results);

        /// <summary>
        /// Sync the specify file from rms to check if it is modified or not. (Modify rights\Edit\Overwrite)
        /// </summary>
        /// <param name="selectedFile">the specified file will to sync</param>
        /// <param name="result">the new file node after sync</param>
        /// <param name="bNeedFindParent">Flag that indicates if need to firstly find its parent folder before sync</param>
        void SyncDestFile(INxlFile selectedFile, OnSyncDestComplete result, bool bNeedFindParent = false);

        /// <summary>
        /// Get current working folder files from local database, including local protected file and remote file nodes.
        /// </summary>
        /// <returns></returns>
        IList<INxlFile> GetWorkingFolderFilesFromDB();

        /// <summary>
        /// Get specified folder's all rms nodes from local db recursively.
        /// </summary>
        /// <param name="pathId">file path id. Note: must pass "Path" for Shared Workspace.</param>
        /// <param name="results"></param>
        void GetRmsFilesRecursivelyFromDB(string pathId, IList<INxlFile> results);

        IList<INxlFile> GetOfflines();
        IList<INxlFile> GetPendingUploads();

        /// <summary>
        /// Get files from local
        /// </summary>
        /// <returns></returns>
        IList<INxlFile> GetSharedByMeFiles();

        /// <summary>
        /// Get files from rms by async way, actually by listing all files from rms, then filter again.
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        void SyncSharedByMeFiles(OnSyncComplete callback);

        IList<INxlFile> GetSharedWithMeFiles();
        void SyncSharedWithMeFiles(OnSyncComplete callback);

        void CreateFolder(string name, string parantFolder);

        /// <summary>
        /// User add file into repository from local browser.
        /// Maybe should Provide extend version UploadFileEx for external drive that can support PROGRESS and CANCEL.
        /// </summary>
        void UploadFile(string fileLocalPath, string destFolder, OnOprationComplete callback, bool overwrite = false);

        /// <summary>
        /// Upload file to external cloud drive
        /// </summary>
        /// <param name="localPath">local file path want to upload</param>
        /// <param name="name">file name</param>
        /// <param name="cloudPathId">the dest folder</param>
        /// <param name="isOverwrite">if overwrite or not, is false in default</param>
        /// <param name="callback">upload callback, null means don't care about the upload progress</param>
        void UploadFileEx(string localPath, string name, string cloudPathId,
            bool isOverwrite = false, IUploadProgressCallback callback = null);

        /// <summary>
        /// Update token to local when acquired latest access token(token maybe expired), used for external repository.
        /// </summary>
        /// <param name="newToken"></param>
        void UpdateToken(string newToken);

        bool CheckFileExists(string pathId);
    }
}
