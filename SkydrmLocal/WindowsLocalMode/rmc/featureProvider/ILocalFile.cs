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
        string Name { get; }
        string LocalDiskPath { get; }
        string RMSRemotePath { get; }
        long FileSize { get; }

        string SharedEmails { get; }  // special design for myvault currently, others is "";

        DateTime LastModifiedTime { get; }

        EnumNxlFileStatus Status { get; set; }

        IFileInfo FileInfo { get; }

        void UploadToRms();

        void RemoveFromLocal();

    }

}
