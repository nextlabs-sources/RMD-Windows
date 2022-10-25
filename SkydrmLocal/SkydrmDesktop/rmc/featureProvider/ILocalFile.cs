using SkydrmDesktop.rmc.featureProvider.externalDrive;
using SkydrmLocal.rmc.featureProvider.common;
using SkydrmLocal.rmc.fileSystem.basemodel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.featureProvider
{   
    public interface ILocalFile
    {
        /// <summary>
        /// OfflineFile means: user successfully marked a file as offline from RMS. 
        /// DataSource inlcudes SharedWithMeFile, MyVaultFile, ProjectFile
        /// </summary>
        /// <returns></returns>
        IOfflineFile[] GetOfflines();
        /// <summary>
        /// PendingUploadFile means: User add some file from local to Vault or Project, but did not upload it to RMS
        /// DataSource includes MyVaultLocalFile, ProjectLocalFile
        /// </summary>
        /// <returns></returns>
        IPendingUploadFile[] GetPendingUploads();

    }

    public interface IOfflineFile
    {
        string Name { get; }
        string LocalDiskPath { get; }
        string RMSRemotePath { get; } 
        long FileSize { get; }
        DateTime LastModifiedTime { get; }

        bool IsOfflineFileEdit { get; }

        EnumNxlFileStatus Status { get; set; }

        IFileInfo FileInfo { get; }

        void RemoveFromLocal(); 

    }

    public interface IPendingUploadFile
    {
        string Name { get; set; }

        string LocalDiskPath { get; set; }

        string DisplayPath { get; }

        string PathId { get; }

        long FileSize { get; }

        string SharedEmails { get; }  // special design for myvault currently, others is "";

        DateTime LastModifiedTime { get; }

        EnumNxlFileStatus Status { get; set; }

        EnumFileRepo FileRepo { get;}

        IFileInfo FileInfo { get; }

        /// <summary>
        /// User selected option
        /// </summary>
        bool OverWriteUpload { get; set; }

        bool IsExistInRemote { get; set; }

        /// <summary>
        /// Upload waiting pending file to remote.
        /// </summary>
        /// <param name="isOverWrite">If is overwrite or not, its false in default.</param>
        /// <param name="callback">Upload progress callback, its null in default, means don't care progress.</param>
        void Upload(bool isOverWrite = false, IUploadProgressCallback callback = null);

        void RemoveFromLocal();

    }

}
