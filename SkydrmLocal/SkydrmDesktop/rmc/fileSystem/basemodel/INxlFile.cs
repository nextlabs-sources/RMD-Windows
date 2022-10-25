using SkydrmLocal.rmc.Edit;
using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.featureProvider.common;
using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.fileSystem.basemodel
{
    public interface INxlFile
    {
        #region Property
        string Name { get; set; }

        EnumFileLocation Location { get; set; }

        string DateModified { get; }

        DateTime RawDateModified { get; }

        long Size { get; }

        bool IsFolder { get; }

        // Binding to UI for different icon displayed
        EnumNxlFileStatus FileStatus { get; set; }

        // Flag that files to which repo belongs, mainly used to ui display
        EnumFileRepo FileRepo { get;}

        string LocalPath { get; set; }

        string PartialLocalPath { get; }

        // Distinguish file source in Filters(offline & outbox)
        string SourcePath { get; set; }
        
        IFileInfo FileInfo { get; }

        bool IsCreatedLocal { get; }

        string DisplayPath { get; }

        // Remote\cloud path id.
        string PathId { get; }

        string FileId { get; }

        bool IsMarkedOffline { get; }

        // Repository id(for project, its project id string format.)(for sharedWorkSpace, its repo id)
        string RepoId { get; }

        // Used to flag the corresponding remote file that marked offline's whether has been modified or not.
        bool IsMarkedFileRemoteModified { get; set; }

        // Used to flag project file rights if is modified.
        bool IsModifiedRights { get; set; }

        // File if is edited in local or not.
        bool IsEdit { get; set; }

        // Judging if is one protected file by file suffix roughly.
        bool IsNxlFile { get; }

        string Duid { get; }

        #region For sharing transaction
        /// <summary>
        /// Shared by me
        /// </summary>
        bool IsShared { get; }
        bool IsRevoked { get;}
        // It's the project id string format for project.
        List<string> SharedWith { get; } 

        /// <summary>
        /// Shared with me
        /// </summary>
        string SharedDate { get; }
        // Who shared
        string SharedBy { get; }
        // Which project come from.
        string SharedByProject { get; } 
        #endregion // For sharing transaction

        #endregion // Property

        #region Method

        // Will perform the operation tasks in thread pool
        void DownloadFile(bool isViewOnly = false);
        void DownloadPartial();
        void UploadEditedFile();

        void Remove();
        void Edit(Action<IEditComplete> callback);
        void Export(string destFolder);
        bool UnMark();

        // Sharing transaction
        // email or project id(for project id, pass the string format)
        void UpdateRecipients(List<string> addList, List<string> removedList, string comment);
        void Share(List<string> recipients, string comment); // Include reshare
        bool Revoke(); 

        #endregion // Method
    }

}
