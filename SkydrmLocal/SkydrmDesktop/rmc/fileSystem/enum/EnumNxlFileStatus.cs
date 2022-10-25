using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.fileSystem.basemodel
{
    public enum EnumNxlFileStatus
    {
        // Clone copy after successful upload (if user checked in “Preferences” -  “Leave a clone copy in SkyDRM Local Folder”)
        CachedFile = 0,

        // Available for offline view(marked "Make Available Offline")
        AvailableOffline = 1,

        // File is uploading
        Uploading = 2,

        //  Waiting for upload(if someone shared or protected file in offline mode and upload is still pending)
        WaitingUpload = 3,

        // File is in remote
        Online = 4,

        // File upload successfully
        UploadSucceed = 5,

        // File upload failed
        UploadFailed = 6,

        // File is removed by user.
        RemovedFromLocal = 7,

        //File is downloading 
        Downloading=8,

        // downloaded succeed
        DownLoadedSucceed=9,
      
        // downloaded failed
        DownLoadedFailed =10,

        // MISC.
        // file missing in local 
        FileMissingInLocal =11,

        // unknown error
        UnknownError =12,

        ProtectSucceeded =13,

        ProtectFailed =14

        // means available offline file is edited.
        //AvailableOffline_Edited = 15,

        // cached file is edited.
        //CachedFile_Edited = 16
    }
}
