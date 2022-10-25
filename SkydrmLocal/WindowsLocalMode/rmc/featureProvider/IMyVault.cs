using SkydrmLocal.rmc.common.interfaces;
using SkydrmLocal.rmc.featureProvider.common;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.featureProvider
{
    public interface IMyVault : IHeartBeat, ILocalFile
    {
        /// <summary>
        /// Retrieve all files exists in MyVault[Cache in db.]. 
        /// </summary>
        /// <returns>all myvault [local] Files.</returns>
        IMyVaultFile[] List();

        IMyVaultFile[] ListWithoutFilter();
        /// <summary>
        /// Retrieve all files exists Remote.
        /// </summary>
        /// <returns>all myVault remote Files.</returns>
        IMyVaultFile[] Sync();

        /// <summary>
        /// Retrieve all MyVaultLocalFiles.
        /// </summary>
        /// <returns>Array of all the results have been queryed.</returns>
        IMyVaultLocalFile[] ListLocalAdded();

        /// <summary>
        /// Protect a local file and insert it into db.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="rights"></param>
        /// <param name="waterMark"></param>
        /// <param name="expiration"></param>
        /// <param name="tags"></param>
        /// <returns>IMyVaultLocalFile</returns>
        IMyVaultLocalFile AddLocalAdded(string path, List<FileRights> rights,
            WaterMarkInfo waterMark, Expiration expiration, UserSelectTags tags);

        /// <summary>
        /// Protect a local file and copy it into copyPath.
        /// Now it is not used, reserved
        /// </summary>
        /// <param name="copyPath"></param>
        /// <param name="path"></param>
        /// <param name="rights"></param>
        /// <param name="waterMark"></param>
        /// <param name="expiration"></param>
        /// <param name="tags"></param>
        /// <returns></returns>
        IMyVaultLocalFile CopyLocalAdded(string copyPath, string path, List<FileRights> rights,
           WaterMarkInfo waterMark, Expiration expiration, UserSelectTags tags);

        /// <summary>
        /// Share a local file and insert it into db.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="rights"></param>
        /// <param name="recipients"></param>
        /// <param name="comments"></param>
        /// <param name="waterMark"></param>
        /// <param name="expiration"></param>
        /// <param name="tags"></param>
        /// <returns>IMyVaultLocalFile</returns>
        IMyVaultLocalFile AddLocalAdded(string path, List<FileRights> rights, 
            List<String> recipients, string comments, 
            WaterMarkInfo waterMark, Expiration expiration, UserSelectTags tags);
    }

    public interface IMyVaultFile
    {
        string Path_Id { get; }
        string Display_Path { get; }
        string Repo_Id { get; }
        string Duid { get; }
        string Nxl_Name { get; }
        DateTime Last_Modified_Time { get; }
        DateTime Creation_Time { get; }
        DateTime Shared_Time { get; }
        string Shared_With_List { get; }
        long FileSize { get; }
        bool Is_Deleted { get; }
        bool Is_Revoked { get; }
        bool Is_Shared { get; }

        string Nxl_Local_Path { get; }
        string Partial_Local_Path { get; }
        bool Is_Offline { get; set; }
        bool Is_Edit { get; set; }
        bool Is_ModifyRights { get; set; }

        EnumNxlFileStatus Status { get; set; }
        IFileInfo FileInfo { get; }
        void Download(bool isViewOnly = false);
        void DownloadPartial();

        void Remove();

        void ChangeSharedWithList(string[] emails);
        void Export(string destinationFolder);

        MyVaultMetaData GetMetaData();

        void ShareFile(string nxlLocalPath, string[] recipentAdds, string[] recipentRmoves, string comments);
    }

    public interface IMyVaultLocalFile : IPendingUploadFile
    {
        string Nxl_Name { get; }
        string Nxl_Local_Path { get; }
        DateTime Last_Modified_Time { get;}
        //IFileInfo FileInfo { get; }
        string[] Nxl_Shared_With_List { get; }
        void Upload();
        void Remove();

        void ChangeSharedWithList(string[] emails);
    }
}
