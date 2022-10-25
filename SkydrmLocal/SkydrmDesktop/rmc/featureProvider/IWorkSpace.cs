using SkydrmLocal.rmc.common.interfaces;
using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.featureProvider
{
    public interface IWorkSpace:IHeartBeat, ILocalFile
    {
        /// <summary>
        /// Get all files existed in WorkSpace db 
        /// </summary>
        /// <returns>All workspace local files</returns>
        IWorkSpaceFile[] ListAll(bool toDisplay = false);

        /// <summary>
        /// Get specified folder files 
        /// </summary>
        /// <param name="folderId"></param>
        /// <returns></returns>
        IWorkSpaceFile[] List(string folderId);

        /// <summary>
        /// Sync specified folder files. 
        /// </summary>
        /// <param name="folderId"></param>
        /// <returns></returns>
        IWorkSpaceFile[] Sync(string folderId);

        /// <summary>
        /// Get all WorkSpace Local created Files in db.
        /// </summary>
        /// <returns></returns>
        IWorkSpaceLocalFile[] ListLocalAllAdded();

        /// <summary>
        /// Get workspace files in specified folder.
        /// </summary>
        /// <returns>Array of all the results have been queryed.</returns>
        IWorkSpaceLocalFile[] ListLocalAdded(string folderId);

        /// <summary>
        /// Added created file to workspace.
        /// </summary>
        /// <param name="parentFolder"></param>
        /// <param name="filepath"></param>
        /// <param name="rights"></param>
        /// <param name="waterMark"></param>
        /// <param name="expiration"></param>
        /// <param name="tags"></param>
        /// <returns></returns>
        IWorkSpaceLocalFile AddLocalAdded(string parentFolder, string filepath,
            List<FileRights> rights,
            WaterMarkInfo waterMark,
            Expiration expiration,
            UserSelectTags tags);

        bool CheckFileExists(string pathId);
    }

    public interface IWorkSpaceFile
    {
        // rms fields
        string File_Id { get; }
        string Duid { get; }
        string Path_Display { get; }
        string Path_Id { get; }
        string Nxl_Name { get; }
        string File_Type { get; }
        DateTime Last_Modified { get; }
        DateTime Created_Time { get; }
        int Size { get; }
        bool Is_Folder { get; }
        int Owner_Id { get; }
        string Owner_Display_Name { get; }
        string Owner_Email { get; }
        int Modified_By { get; } // by who(userId) modified
        string Modified_By_Name { get; }
        string Modified_By_Email { get; }

        //
        string Nxl_Local_Path { get; }
        string Partial_Local_Path { get; }
        bool Is_Dirty { get; set; }
        bool Is_Offline { get; set; }
        bool Is_Edit { get; set; }
        bool Is_ModifyRights { get; set; }

        EnumNxlFileStatus Status { get; set; }
        IFileInfo FileInfo { get; }

        void UpdateWhenOverwriteInLeaveCopy(string duid, EnumNxlFileStatus Status, long size, DateTime lastModifed);

        void Download(bool isViewOnly = false);
        // Deprecated
        void DownloadPartial();

        // It will be compatible with 'DownloadPartial()' for old server(sdk handled this).
        void GetNxlHeader();

        void UploadEditedFile();
        void Export(string destinationFolder);

        void Remove();

        WorkspaceMetaData GetMetaData();

        bool ModifyRights(List<FileRights> rights, WaterMarkInfo waterMark, Expiration expiration, UserSelectTags tags);
    }

    public interface IWorkSpaceLocalFile: IPendingUploadFile
    {
        // Reserved for extend
    }

}
