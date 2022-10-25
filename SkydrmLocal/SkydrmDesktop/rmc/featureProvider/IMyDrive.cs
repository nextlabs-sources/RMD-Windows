using SkydrmLocal.rmc.common.interfaces;
using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.fileSystem.basemodel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.featureProvider
{
    public interface IMyDrive: IHeartBeat, ILocalFile
    {
        IMyDriveFile[] ListAll(bool toDisplay=false);

        IMyDriveFile[] List(string folderId);

        IMyDriveFile[] Sync(string folderId);

        void CreateFolder(string name, string parantFolder);

        void UploadFile(string fileLocalPath, string destFolder, bool overwrite = false);

        IMyDriveLocalFile[] ListLocalAdded(string folderId);

        IMyDriveLocalFile AddLocalAdded(string parentFolder, string filePath);
    }

    public interface IMyDriveFile : IOfflineFile
    {
        string PathId { get; }
        string PathDisplay { get; }
        bool IsFolder { get; }

        // Used to record the marked offline file of local when finding the file is modifed in remote(such as: overwrite).
        bool Is_Dirty { get; set; }

        bool IsNormalFile { get; }
        bool IsOffline { get; set; }
        bool IsFavorite { get; set; } // reserved

        void DeleteItem(); // file or folder
        void Download();
        
    }

    public interface IMyDriveLocalFile : IPendingUploadFile
    {
        // Reserved for extend
    }
}
