using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.fileSystem.basemodel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.featureProvider.externalDrive
{
    /// <summary>
    /// All external drive file node interface
    /// </summary>
    public interface IExternalDriveFile
    {
        string FileId { get; }
        bool IsFolder { get; }
        string Name { get; }
        long Size { get; }
        string LocalPath { get; }
        string DisplayPath { get; }
        DateTime ModifiedTme { get; }

        // Note: upper level will pass this parameter to get file lists from remote.
        string CloudPathId { get; }

        // Reserved, to store some special info such as 'mimetype' for googledrive.
        string CustomString { get; }

        // Judging roughly if is nxl file by file suffix.
        bool IsNxlFile { get; }

        bool IsOffline { get; set; }
        EnumNxlFileStatus Status { get; set; } 
        bool IsFavorite { get; set; } 

        // Delete the node from the drive
        void DeleteItem();

        // Remove file from local(used for unmark offline)
        void RemoveFromLocal();

        // Download file to local, the implementer should prepare the "LocalPath"
        void Download();

        // Save as the file to other place(download first then copy out)
        void Export(string destFolder);
    }
}
