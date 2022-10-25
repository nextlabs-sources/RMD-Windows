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
        string Path { get; }

        int Id { get; }

        string DisplayName { get; }

        string Description { get; }

        bool IsOwner { get; }

        bool IsEnableAdHoc { get; }

        string MemberShipId { get; }
        
        IProjectFile[] ListFiles(string FolderId);

        IProjectFile[] ListAllProjectFile();

        IProjectLocalFile[] ListProjectLocalFiles();

        IProjectFile[] SyncFiles(string FolderId);

        IProjectLocalFile[] ListLocalAdded(string FolderId);
              

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ParentFolder"> RMS parent folder, which folder you want to add a file</param>
        /// <param name="filePath">full path of the plain file user selected</param>
        /// <param name="rights">adhoc rights</param>
        /// <param name="waterMark">adhoc watermark</param>
        /// <param name="expiration">adhoc expiration</param>
        /// <param name="tags">Central policy tags user selected</param>
        IProjectLocalFile AddLocalFile(string ParentFolder, string filePath, 
            List<FileRights> rights,
            WaterMarkInfo waterMark, Expiration expiration, UserSelectTags tags);

        /// <summary>
        /// Protect file to local drive
        /// </summary>
        /// <param name="CopyPath">Local drive path, which folder you want to copy a file</param>
        /// <param name="filePath">full path of the plain file user selected</param>
        /// <param name="rights">adhoc rights</param>
        /// <param name="waterMark">adhoc watermark</param>
        /// <param name="expiration">adhoc expiration</param>
        /// <param name="tags">Central policy tags user selected</param>
        IProjectLocalFile CopyLocalFile(string CopyPath, string filePath,
            List<FileRights> rights,
            WaterMarkInfo waterMark, Expiration expiration, UserSelectTags tags);

        IProjectMember[] ListMembers();

        IProjectMember[] SyncMembers();


        ProjectClassification[] ListClassifications();

        ProjectClassification[] SyncClassifications();
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
        void DownlaodFile(bool isForViewOnly=false);
        void DownloadPartial();

        void Export(string destinationFolder);

        void UploadEditedFile();

        //void GetRights();
        
        //void View()

        void Remove();

        bool ModifyRights(List<FileRights> rights, WaterMarkInfo waterMark, Expiration expiration, UserSelectTags tags);

         void DoEdit(Action<EditCallBack> OnEditCompleteCallback);
    } 

    public interface IProjectLocalFile: IPendingUploadFile
    {
        string LocalPath { get; }
        DateTime LocalModifiedTime { get; }
        void Remove();
        void Upload();    
    }

    // TBD
    public class IProjectMember
    {

    }


}
