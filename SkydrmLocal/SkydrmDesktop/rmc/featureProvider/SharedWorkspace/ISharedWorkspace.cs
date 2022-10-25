using SkydrmDesktop.rmc.featureProvider.externalDrive.RmsRepoMgr;
using SkydrmLocal.rmc.common.interfaces;
using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.featureProvider.SharedWorkspace
{
    // External repositories of using 'Application account'
    public interface ISharedWorkspace: IHeartBeat, ILocalFile
    {
        ExternalRepoType Type { get; }
        string DisplayName { get; set; }
        string RepoId { get; }

        /// <summary>
        /// Get all files existed from db 
        /// </summary>
        /// <returns>All shared workspace local files</returns>
        ISharedWorkspaceFile[] ListAll(bool toDisplay = false);

        /// <summary>
        /// Get specified folder files 
        /// </summary>
        /// <param name="Path"></param>
        /// <returns></returns>
        ISharedWorkspaceFile[] List(string path);

        /// <summary>
        /// Sync specified folder files. 
        /// </summary>
        /// <param name="folderId"></param>
        /// <returns></returns>
        ISharedWorkspaceFile[] Sync(string path);

        /// <summary>
        /// Get all Shared WorkSpace Local created Files in db.
        /// </summary>
        /// <returns></returns>
        ISharedWorkspaceLocalFile[] ListLocalAllAdded();

        /// <summary>
        /// Get shared workspace files in specified folder.
        /// </summary>
        /// <returns>Array of all the results have been queryed.</returns>
        ISharedWorkspaceLocalFile[] ListLocalAdded(string path);

        /// <summary>
        /// Added created file to shared workspace.
        /// </summary>
        /// <param name="folderId"></param>
        /// <param name="folderDisplayPath"></param>
        /// <param name="filepath"></param>
        /// <param name="rights"></param>
        /// <param name="waterMark"></param>
        /// <param name="expiration"></param>
        /// <param name="tags"></param>
        /// <returns></returns>
        ISharedWorkspaceLocalFile AddLocalAdded(string folderId, string folderDisplayPath, string filepath,
            List<FileRights> rights,
            WaterMarkInfo waterMark,
            Expiration expiration,
            UserSelectTags tags);

        bool CheckFileExists(string pathId);
    }

    public interface ISharedWorkspaceFile
    {
        // rms fields
        string File_Id { get; }
        string Path_Display { get; }
        string Path_Id { get; }
        string Nxl_Name { get; }
        string File_Type { get; }
        DateTime Last_Modified { get; }
        DateTime Created_Time { get; }
        int Size { get; }
        bool Is_Folder { get; }
        bool Is_ProtectedFile { get; }
        //
        string Nxl_Local_Path { get; }
        string Partial_Local_Path { get; }
        bool Is_Dirty { get; set; }
        bool Is_Offline { get; set; }
        bool Is_Edit { get; set; }
        bool Is_ModifyRights { get; set; }

        EnumNxlFileStatus Status { get; set; }
        IFileInfo FileInfo { get; }
        void UpdateWhenOverwriteInLeaveCopy(EnumNxlFileStatus Status, long size, DateTime lastModifed);

        void Download(bool isViewOnly = false);

        void GetNxlHeader();

        void UploadEditedFile();
        void Export(string destinationFolder);

        void Remove();

    }

    public interface ISharedWorkspaceLocalFile: IPendingUploadFile
    {
        // Reserved for extend
    }

}
