using SkydrmLocal.rmc.common.interfaces;
using SkydrmLocal.rmc.Edit;
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
    /* Notes:
     *  Get***  return the values stored at local
     *  Sync*** return the values stored at RM server
     * 
    */       
    public interface IMyProjects : IHeartBeat
    {
        IMyProject[] List();

        IMyProject[] Sync();
    }


    public interface IMyProject : ILocalFile
    {
        // Local path for this prj
        string WorkingPath { get; }

        int Id { get; }

        string DisplayName { get; }

        string Description { get; }

        bool IsOwner { get; }

        bool IsEnableAdHoc { get; }

        string MemberShipId { get; }
        
        IProjectFile[] ListFiles(string FolderId);

        IProjectFile[] ListAllProjectFile(bool toDisplay = false);

        IProjectLocalFile[] ListProjectLocalFiles();

        IProjectFile[] SyncFiles(string FolderId);

        // Support sharing transaction.
        IProjectFile[] SyncFilesEx(string FolderId, FilterType type);
        IProjectSharedWithMeFile[] ListSharedWithMeFiles();
        IProjectSharedWithMeFile[] SyncSharedWithMeFiles();

        IProjectLocalFile[] ListLocalAdded(string FolderId);

        /// <summary>
        /// Protect file
        /// </summary>
        /// <param name="ParentFolder"> RMS parent folder, which folder you want to add a file</param>
        /// <param name="filePath">full path of the plain file user selected</param>
        /// <param name="rights">adhoc rights</param>
        /// <param name="waterMark">adhoc watermark</param>
        /// <param name="expiration">adhoc expiration</param>
        /// <param name="tags">Central policy tags user selected</param>
        IProjectLocalFile AddLocalFile(string parentFolder, string filePath, 
            List<FileRights> rights,
            WaterMarkInfo waterMark, Expiration expiration, UserSelectTags tags);

        IProjectMember[] ListMembers();

        IProjectMember[] SyncMembers();


        ProjectClassification[] ListClassifications();

        ProjectClassification[] SyncClassifications();

        bool CheckFileExists(string pathId);
    }



    public interface IProjectFile
    {
        bool isFolder { get; }
        bool isOffline { get; set; }
        string RsmPathId { get; }
        string RMSDisplayPath { get; }
        string Name { get; }
        string LocalDiskPath { get; }
        string PartialLocalPath { get; }
        DateTime CreationTime { get; }
        DateTime LastModifiedTime { get; }
        Int64 FileSize { get; }
        int RmsOwnerId { get; }
        string OwnerDisplayName { get; }
        string OwnerEmail { get; }
        string RmsFileId { get; }
        string RmsDuId { get; }
        EnumNxlFileStatus Status { get; set; }
        IFileInfo FileInfo { get; }
        bool IsDirty { get; set; }
        bool IsEdit { get; set; }
        bool IsModifyRights { get; set; }

        // Start -- Extend for sharing transaction
        bool IsShared { get; set; }
        bool IsRevoked { get; set; }
        List<uint> SharedToProjects { get; set; }
        void UpdateRecipients(List<uint> addRecipients, // IsShared == true
            List<uint> removedRecipients,
            string comment);

        void ShareFile(List<uint> recipients, string comment); // IsShared == false
        bool RevokeFile();
        // End -- Extend for sharing transaction

        void UpdateWhenOverwriteInLeaveCopy(string duid, EnumNxlFileStatus Status, long size, DateTime lastModifed);

        void DownlaodFile(bool isForViewOnly=false);
        // Deprecated
        void DownloadPartial();

        // It will be compatible with 'DownloadPartial()' for old server(sdk handled this).
        void GetNxlHeader();

        void Export(string destinationFolder);

        void UploadEditedFile();

        //void GetRights();
        
        //void View()

        void Remove();

        bool ModifyRights(List<FileRights> rights, WaterMarkInfo waterMark, Expiration expiration, UserSelectTags tags);

        void DoEdit(Action<EditCallBack> OnEditCompleteCallback);
    }

    public interface IProjectSharedWithMeFile
    {
        string Name { get; }
        string Duid { get; }
        string Type { get; } // "txt","mp3"
        long FileSize { get; }
        DateTime SharedDate { get; }
        string SharedBy { get; }  // who share file to you, like: "john.tyler@nextlabs.com" 
        string sharedByProject { get; } // like: "2"
        string SharedLinkeUrl { get; } // a url you can view it online
        uint ProtectType { get; }
        FileRights[] Rights { get; }
        string Comments { get; }
        bool IsOwner { get; }
        bool IsOffline { get; set; }
        string LocalDiskPath { get; }   // if offlined
        string PartialLocalPath { get; }
        EnumNxlFileStatus Status { get; set; }
        bool IsEdit { get; set; }
        bool IsModifyRights { get; set; }
        IFileInfo FileInfo { get; }

        void Download(bool isForViewOnly = true); 
        void DownloadPartial();

        void Export(string destinationFolder);
        // once the file has been downloaded, user can delete local 
        void Remove();

        // ReShare
        bool ReShare(List<uint> recipients, string emailLsit = "");
    }

    public interface IProjectLocalFile: IPendingUploadFile
    {
        // Reserved for extend
    }

    // TBD
    public class IProjectMember
    {

    }


}
