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

    public interface IFileRepo
    {
        void MarkOffline(INxlFile nxlFile, OnDownloadComplete callback);

        bool UnmarkOffline(INxlFile nxlFile); 

        void DownloadFile(INxlFile nxl, bool isViewOnly, OnDownloadComplete callback, bool isDownloadPartial = false, bool isOnlineView = false);

        /// <summary>
        /// Sync current working folder's all node files from rms
        /// </summary>
        /// <param name="itemFlag">mainly used for project repo, projectId or folder pathid.</param>
        /// <param name="results"></param>
        void SyncFiles(OnRefreshComplete results, string itemFlag = null);

        /// <summary>
        /// Sync the specify file from rms to check if it is modified or not.
        /// </summary>
        /// <param name="selectedFile">the specified file will to sync</param>
        /// <param name="result">the new file node after sync</param>
        /// <param name="bNeedFindParent">Flag that indicates if need to firstly find its parent folder before sync, 
        /// now mainly used for project.</param>
        void SyncDestFile(INxlFile selectedFile, OnSyncDestComplete result, bool bNeedFindParent = false);

        /// <summary>
        /// Get all files from repo database
        ///     --For project and myvault, including local files and remote files in db.
        /// </summary>
        /// <returns></returns>
        IList<INxlFile> GetLocalFiles();

        IList<INxlFile> GetOfflines();

        IList<INxlFile> GetPendingUploads();

        /// <summary>
        /// Get the file source path of offline & outbox file
        /// </summary>
        /// <returns></returns>
        string GetSourcePath(INxlFile nxlfile);

        /// <summary>
        /// Used to update(upload) the edited file to rms after user do Edit in our RMD app.
        ///     --For external file, such as edit the attachment nxl file by explorer, don't need to update to rms after Edit.
        /// </summary>
        void UpdateToRms(INxlFile nxlFile);

        void Export(string destFolder,INxlFile nxlFile);

        void Edit(INxlFile nxlFile, Action<EditCallBack> onFinishedCallback);
    }
}
