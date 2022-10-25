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
        string Name { get; }

        EnumFileLocation Location { get; set; }

        // Used to bind ui
        string DateModified { get; }

        DateTime RawDateModified { get; }

        long Size { get; }

        bool IsFolder { get; }

        string SharedWith { get; }

        // binding to UI for different icon displayed
        EnumNxlFileStatus FileStatus { get; set; }

        // Flag that files to which repo belongs, mainly used to ui display
        EnumFileRepo FileRepo { get;}

        string LocalPath { get; }

        string PartialLocalPath { get; }

        // Distinguish file source in Filters(offline & outbox)
        string SourcePath { get; set; }

        /// Used to project files db store, using the fileId as the parentId.
        string ParentId { get; }
        
        /// Required all implement can remove itself from Local
        void Remove();

        IFingerPrint FingerPrint { get; }

        IFileInfo FileInfo { get; }

        bool IsCreatedLocal { get; }

        string RmsRemotePath { get; }

        string FileId { get; }

        bool IsMarkedOffline { get; }

        // Used to diaply for file info: SharedWith or ShareBy 
        string[] Emails { get; }

        // Get project id if is project repo, used to "Save As" feature.
        Int32 ProjectId { get; }

        // Used to flag the corresponding remote file that marked offline's whether has been modified or not.
        bool IsMarkedFileRemoteModified { get; set; }

        // Used to flag project file rights if is modified.
        bool IsModifiedRights { get; set; }

        // File if is edited in local or not.
        bool IsEdit { get; set; }

        string Duid { get; }
    }

}
