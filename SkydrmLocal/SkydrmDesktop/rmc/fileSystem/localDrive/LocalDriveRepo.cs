using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Alphaleonis.Win32.Filesystem;
using SkydrmDesktop.rmc.fileSystem.utils;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.ui.utils;

namespace SkydrmDesktop.rmc.fileSystem.localDrive
{
    public class LocalDriveRepo : AbstractLocalDriveRepo
    {
        private readonly SkydrmApp App = SkydrmApp.Singleton;
        private IList<INxlFile> FilePool { get; set; }

        public LocalDriveRepo()
        { }

        public override string RepoDisplayName { get => FileSysConstant.LOCAL_DRIVE; set => throw new NotImplementedException(); }

        public override string RepoType => FileSysConstant.LOCAL_DRIVE;

        public override IList<INxlFile> GetFilePool()
        {
            return FilePool;
        }

        /// <summary>
        /// Get folder files from local drive
        /// </summary>
        /// <returns></returns>
        public override IList<INxlFile> GetWorkingFolderFilesFromDB()
        {
            List<INxlFile> ret = new List<INxlFile>();

            InnerGetFilesFromDrive(GetFolderPathId(), ret);

            return ret;
        }
        private string GetFolderPathId()
        {
            return CurrentWorkingFolder.PathId;
        }
        /// <summary>
        /// Inner impl to get files from drive, including folders and files.
        /// </summary>
        /// <param name="pathId"></param>
        /// <returns></returns>
        private void InnerGetFilesFromDrive(string pathId, List<INxlFile> results)
        {
            try
            {
                if (pathId.StartsWith("This PC") || pathId.Equals("/"))
                {
                    results.AddRange(GetThisPCList());
                }
                else if (Directory.Exists(pathId))
                {
                    GetSonFoldersAndFiles(pathId, results);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                App.Log.Error("Invoke InnerGetFilesFromDrive failed.", e);
            }
        }
        private List<INxlFile> GetThisPCList()
        {
            List<INxlFile> ret = new List<INxlFile>();
            var drives = DriveInfo.GetDrives();

            foreach (var drive in drives)
            {
                if (drive.DriveType != System.IO.DriveType.Fixed
                   && drive.DriveType != System.IO.DriveType.Network)
                {
                    continue;
                }

                string flagLabel = string.Empty;
                if (drive.DriveType == System.IO.DriveType.Network)
                {
                    flagLabel = "Network";
                }
                else
                {
                    flagLabel = drive.VolumeLabel;
                }

                string driverName = drive.RootDirectory.FullName.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries)[0];
                // Here treat the drive as a folder
                LocalDriveFolder localDrive = new LocalDriveFolder();

                localDrive.Name = flagLabel + "(" + driverName + ")";
                localDrive.PathId = drive.RootDirectory.FullName;
                localDrive.LocalPath = drive.RootDirectory.FullName;

                ret.Add(localDrive);
            }

            // add desktop         
            string path = Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
            LocalDriveFolder desktop = new LocalDriveFolder();
            desktop.Name = "Desktop";
            desktop.PathId = path;
            desktop.LocalPath = path;
            ret.Add(desktop);
            

            // add documents
            string documentPath = Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            LocalDriveFolder document = new LocalDriveFolder();
            document.Name = "Documents";
            document.PathId = documentPath;
            document.LocalPath = documentPath;
            ret.Add(document);

            return ret;
        }
        private static void GetSonFoldersAndFiles(string directory, List<INxlFile> collections)
        {
            List<INxlFile> directoryList = new List<INxlFile>();
            List<INxlFile> fileList = new List<INxlFile>();
            string[] resourceArray = Directory.GetFileSystemEntries(directory);
            foreach (var item in resourceArray)
            {
                try
                {
                    if (Directory.Exists(item))
                    {
                        // filter hidden folder
                        DirectoryInfo directoryInfo = new DirectoryInfo(item);
                        if (directoryInfo.Attributes.ToString().Contains(System.IO.FileAttributes.Hidden.ToString()))
                        {
                            continue;
                        }
                        LocalDriveFolder folder = new LocalDriveFolder();
                        folder.Name = directoryInfo.Name;
                        folder.PathId = directoryInfo.FullName;
                        folder.LocalPath = directoryInfo.FullName;
                        
                        directoryList.Add(folder);
                    }
                    else
                    {
                        // filter hidden file
                        FileInfo fileInfo = new FileInfo(item);
                        if (fileInfo.Attributes.ToString().Equals("-1") 
                            || fileInfo.Attributes.ToString().Contains(System.IO.FileAttributes.Hidden.ToString()))
                        {
                            continue;
                        }
                        LocalDriveDoc file = new LocalDriveDoc();
                        file.Name = fileInfo.Name;
                        file.PathId = fileInfo.FullName;
                        file.LocalPath = fileInfo.FullName;
                        file.IsNxlFile = fileInfo.Extension.Equals(".nxl");

                        fileList.Add(file);
                    }
                }
                catch (Exception ex)
                {
                    continue;
                }
            }
            // Add the folder first and then add the file
            collections.AddRange(directoryList);
            collections.AddRange(fileList);
        }

        public override void SyncFiles(OnRefreshComplete results, string itemFlag = null)
        {
            results?.Invoke(false, null, itemFlag);
        }
    }

    public sealed class LocalDriveDoc : NxlDoc
    {
        public LocalDriveDoc()
        {
            this.Location = EnumFileLocation.Local;
            this.FileStatus = EnumNxlFileStatus.WaitingUpload;
            this.FileRepo = EnumFileRepo.LOCAL_DRIVE;
        }

    }

    public sealed class LocalDriveFolder : NxlFolder
    {
        public LocalDriveFolder()
        {
            this.Location = EnumFileLocation.Local;
            this.FileStatus = EnumNxlFileStatus.WaitingUpload;
            this.FileRepo = EnumFileRepo.LOCAL_DRIVE;
        }
    }
}
