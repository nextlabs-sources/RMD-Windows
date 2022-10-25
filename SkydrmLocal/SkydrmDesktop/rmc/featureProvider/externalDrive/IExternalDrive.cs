using SkydrmDesktop.rmc.featureProvider.externalDrive.errorHandler;
using SkydrmDesktop.rmc.featureProvider.externalDrive.RmsRepoMgr;
using SkydrmLocal.rmc.common.interfaces;
using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.featureProvider.externalDrive
{
    /// <summary>
    /// All external drive abstract interface layer.
    /// </summary>
    public interface IExternalDrive:IHeartBeat, ILocalFile
    {
        ExternalRepoType Type { get; }
        string DisplayName { get; set; }
        string AccessToken { get; set; }
        string RepoId { get; }

        /// <summary>
        /// Get repository all file nodes from local db
        /// </summary>
        IExternalDriveFile[] ListAllFiles();

        /// <summary>
        /// Get repository file nodes of specified cloudPathId from local db.
        /// </summary>
        /// <param name="cloudPathId">cloud path id</param>
        IExternalDriveFile[] ListFiles(string cloudPathId);

        /// <summary>
        /// Get repository file nodes of specified cloudPathId from remote server.
        /// Note:
        ///     1. The level always pass cloudPathId(IExternalDriveFile#CloudPathId) to get, so each cloud drive must padding
        /// this feild according to its situation.
        ///     2. If getting the root nodes, the upper level will always pass "/", so maybe some cloud drive need 
        /// to do innner convert if don't use "/" to get root nodes.
        /// </summary>
        /// <param name="cloudPathId"></param>
        IExternalDriveFile[] SyncFiles(string cloudPathId);

        /// <summary>
        /// Get repository all local created nxl file from local db.
        /// </summary>
        IExternalDriveLocalFile[] ListAllLocalFiles();

        /// <summary>
        /// Get repository local created nxl file nodes of specified cloudPathId from local db.
        /// </summary>
        /// <param name="cloudPathId"></param>
        IExternalDriveLocalFile[] ListLocalFiles(string cloudPathId);

        // Protect nxl file to repository from local(add into repository local file table)
        IExternalDriveLocalFile AddLocalFile(string cloudPathId, string filePath,
                                           List<FileRights> rights, WaterMarkInfo waterMark, 
                                           Expiration expiration, UserSelectTags tags);

        /// <summary>
        /// Upload file to cloud drive
        /// </summary>
        /// <param name="localPath">local file path want to upload</param>
        /// <param name="name">file name</param>
        /// <param name="cloudPathId">the dest folder</param>
        /// <param name="isOverwrite">if overwrite or not, is false in default</param>
        /// <param name="callback">upload callback, null means don't care about the upload progress</param>
        void Upload(string localPath, string name, string cloudPathId, 
            bool isOverwrite = false, IUploadProgressCallback callback = null);
    }

    public interface IUploadProgressCallback
    {
        void OnProgress(long value, long total);
        void OnComplete(bool bSuccess, string uploadFilePath, RepoApiException except);
        void OnCancel(ICancelable cancel);
    }

    public interface ICancelable
    {
        void Cancel();
    }
}
