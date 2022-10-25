using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.ui.windows.mainWindow.model
{
    
    /// <summary>
    /// Local nxl file status.
    /// </summary>
    /// 
    public enum EnumLocalNxlFileStatus
    {
        // Clone copy after successful upload
        // (if user checked in “Preferences” -  “Leave a clone copy in SkyDRM Local Folder”)
        CachedFile,
        // Available for offline view(marked "Make Available Offline" from any client)
        AvailableOffline,
        // Uploading - in the process of upload
        Uploading,
        // Waiting for upload(if someone shared or protected file in offline mode and upload is still pending)
        WaitingUpload,

        /********Follow status is for Service Manager****************/

        // Upload succeed
        UploadSucceed,

        // Upload failed
        UploadFailed,

        //removed for User
        RemovedFromLocal,
        
        // downloading a nxl from RMS
        Downloading,

        // downloaded succeed
        DownLoadedSucceed,

        // downloaded failed
        DownLoadedFailed,

        // MISC.
        // file missing in local 
        FileMissingInLocal,

        // unknown error
        UnknownError
    }
}
