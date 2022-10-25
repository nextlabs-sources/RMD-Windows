using SkydrmDesktop.rmc.fileSystem.externalDrive;
using SkydrmLocal.rmc.fileSystem;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.fileSystem.project;
using SkydrmLocal.rmc.ui.windows.mainWindow.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.ui.utils
{
    // the callback when create file success (include protect & share)
    public delegate void CreateSuccessDelegate(params object[] values);

   // public delegate void UpdateRecipientsDelegate();

    // Upload status pool serialize notify  callback
    public delegate void CacheFileInfoDelegate(FileStatus fileUploadStatus, string dateModified);

    // Refresh current working folder callback
    public delegate void OnRefreshComplete(bool bSuccess, IList<INxlFile> result, string itemFlag);

    public delegate void OnSyncComplete(bool bSuccess, IList<INxlFile> result);

    // Sync specified file callback.
    public delegate void OnSyncDestComplete(bool bSuccee, INxlFile updatedNode);

    // Get local files callback
    public delegate void OnGetLocalsComplete(bool bSuccess, IList<INxlFile> result);

    // Upload complete callback(succeed or failed)
    public delegate void OnUploadComplete(bool bUploadSucceed, INxlFile uploadFile);

    // Upload complete callback extend.
    public delegate void OnUploadCompleteEx(object result);

    public delegate void OnDownloadCompleteEx(bool bSucceed, INxlFile downloadFile);

    /// <summary>
    /// FileSystem ProjectRepo Sync rms project data complete
    /// </summary>
    /// <param name="bSucceed"></param>
    /// <param name="result"></param>
    public delegate void OnSyncProjectComplete(bool bSucceed, IList<ProjectData> result);

    /// <summary>
    /// Notify to refresh file listview when refresh treeview item.
    /// </summary>
    /// <param name="repoName"></param>
    /// <param name="syncResults"></param>
    /// <param name="pathId"></param>
    public delegate void NotifyRefreshFileListView(string repoName, IList<INxlFile> syncResults, string pathId);

    /// <summary>
    /// Notify to refresh project listview when refresh treeview 'Project' item
    /// </summary>
    /// <param name="addList"></param>
    /// <param name="removeList"></param>
    public delegate void NotifyRefreshProjectListView(List<ProjectData> addList, List<ProjectData> removeList);

    /// <summary>
    /// Notify to refresh external repo listview when refresh treeview 'REPOSITORIES' item
    /// </summary>
    /// <param name="addList"></param>
    /// <param name="removeList"></param>
    public delegate void NotifyRefreshExternalRepoListView(List<IFileRepo> addList, List<IFileRepo> removeList);
}
