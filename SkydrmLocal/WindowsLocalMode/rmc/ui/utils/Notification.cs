using SkydrmLocal.rmc.fileSystem.basemodel;
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

    // Upload status pool serialize notify  callback
    public delegate void CacheFileInfoDelegate(FileStatus fileUploadStatus, string dateModified);

    // Refresh current working folder callback
    public delegate void OnRefreshComplete(bool bSuccess, IList<INxlFile> result, string itemFlag);

    // Sync specified file callback.
    public delegate void OnSyncDestComplete(bool bSuccee, INxlFile updatedNode);

    // Get local files callback
    public delegate void OnGetLocalsComplete(bool bSuccess, IList<INxlFile> result);

    // Upload complete callback(succeed or failed)
    public delegate void OnUploadComplete(bool bUploadSucceed, INxlFile uploadFile);

    // Upload complete callback extend.
    public delegate void OnUploadCompleteEx(object result);

    public delegate void OnDownloadCompleteEx(bool bSucceed, INxlFile downloadFile);
}
