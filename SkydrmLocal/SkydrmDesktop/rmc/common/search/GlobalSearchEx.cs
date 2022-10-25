using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkydrmDesktop;
using SkydrmDesktop.rmc.featureProvider;
using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.fileSystem.basemodel;

namespace SkydrmLocal.rmc.search
{
    public enum SearchFileRepo
    {
        All,
        MyVault,
        Project,
        WorkSpace
    }
    public enum SearchFileTable
    {
        Local,
        Rms
    }

    public interface IGlobalSearchEx
    {
        INxlFile SearchByRmsDisplayPath(string rmsDisplayPath, SearchFileRepo fileRepo, SearchFileTable fileType);
        INxlFile SearchByLocalPath(string fileLocalPath, SearchFileRepo fileRepo, SearchFileTable fileType);
        INxlFile SearchByDUID(string duid, SearchFileRepo fileRepo, SearchFileTable fileType);
    }

    public class GlobalSearchEx : IGlobalSearchEx
    {
        private static GlobalSearchEx instance;
        private static readonly object locker = new object();
        private GlobalSearchEx() { }
        public static GlobalSearchEx GetInstance()
        {
            if (instance == null)
            {
                lock (locker)
                {
                    if (instance == null)
                    {
                        instance = new GlobalSearchEx();
                    }
                }
            }
            return instance;
        }

        public INxlFile SearchByRmsDisplayPath(string rmsDisplayPath, SearchFileRepo fileRepo, SearchFileTable fileType)
        {
            INxlFile result = null;
            ISearchFileInRepoEx searchFileInRepoEx = null;
            switch (fileRepo)
            {
                case SearchFileRepo.All:
                    break;
                case SearchFileRepo.MyVault:
                    searchFileInRepoEx = new SearchMyVaultFileByRmsRemotePathEx();
                    break;
                case SearchFileRepo.Project:
                    searchFileInRepoEx = new SearchProjectFileByRmsRemotePathEx();
                    break;
                case SearchFileRepo.WorkSpace:
                    searchFileInRepoEx = new SearchWorkSpaceFileByRmsRemotePathEx();
                    break;
                default:
                    break;
            }
            if (fileType == SearchFileTable.Local)
            {
                result = searchFileInRepoEx?.SearchInLocalFiles(rmsDisplayPath);
            }
            else
            {
                result = searchFileInRepoEx?.SearchInRmsFiles(rmsDisplayPath);
            }
            return result;
        }

        public INxlFile SearchByLocalPath(string fileLocalPath, SearchFileRepo fileRepo, SearchFileTable fileType)
        {
            INxlFile result = null;
            ISearchFileInRepoEx searchFileInRepoEx = null;
            switch (fileRepo)
            {
                case SearchFileRepo.All:
                    break;
                case SearchFileRepo.MyVault:
                    searchFileInRepoEx = new SearchMyVaultFileByLocalPathEx();
                    break;
                case SearchFileRepo.Project:
                    searchFileInRepoEx = new SearchProjectFileByLocalPathEx();
                    break;
                case SearchFileRepo.WorkSpace:
                    searchFileInRepoEx = new SearchWorkSpaceFileByLocalPathEx();
                    break;
                default:
                    break;
            }
            if (fileType == SearchFileTable.Local)
            {
                result = searchFileInRepoEx?.SearchInLocalFiles(fileLocalPath);
            }
            else
            {
                result = searchFileInRepoEx?.SearchInRmsFiles(fileLocalPath);
            }
            return result;
        }

        public INxlFile SearchByDUID(string duid, SearchFileRepo fileRepo, SearchFileTable fileType)
        {
            INxlFile result = null;
            ISearchFileInRepoEx searchFileInRepoEx = null;
            switch (fileRepo)
            {
                case SearchFileRepo.All:
                    break;
                case SearchFileRepo.MyVault:
                    searchFileInRepoEx = new SearchMyVaultFileByDuidEx();
                    break;
                case SearchFileRepo.Project:
                    searchFileInRepoEx = new SearchProjectFileByDUIDEx();
                    break;
                case SearchFileRepo.WorkSpace:
                    searchFileInRepoEx = new SearchWorkSpaceFileByDUIDEx();
                    break;
                default:
                    break;
            }
            if (fileType == SearchFileTable.Local)
            {
                result = searchFileInRepoEx?.SearchInLocalFiles(duid);
            }
            else
            {
                result = searchFileInRepoEx?.SearchInRmsFiles(duid);
            }
            return result;
        }

    }

    public interface ISearchFileInRepoEx
    {
        INxlFile SearchInRmsFiles(string param);

        INxlFile SearchInLocalFiles(string param);
    }

    #region Search MyVault
    public class SearchMyVaultFileByRmsRemotePathEx : ISearchFileInRepoEx
    {
        readonly SkydrmApp SkydrmApp = SkydrmApp.Singleton;

        readonly log4net.ILog Log = SkydrmApp.Singleton.Log;

        public INxlFile SearchInRmsFiles(string rmsRemotePath)
        {
            Log.InfoFormat("Search MyVault RmsFile By RmsRemotePath : {0}", rmsRemotePath);
            INxlFile result = null;
            IMyVaultFile[] myVaultFiles = SkydrmApp.MyVault.List();
            foreach (IMyVaultFile myVaultFile in myVaultFiles)
            {
                if (String.Equals(myVaultFile.Display_Path, rmsRemotePath, StringComparison.CurrentCultureIgnoreCase))
                {
                    result = new fileSystem.myvault.MyVaultRmsDoc(myVaultFile);
                    break;
                }
            }
            return result;
        }

        public INxlFile SearchInLocalFiles(string rmsRemotePath)
        {
            Log.InfoFormat("Search MyVault Local File By RmsRemotePath : {0}", rmsRemotePath);
            return null;
        }

    }

    public class SearchMyVaultFileByLocalPathEx : ISearchFileInRepoEx
    {
        readonly SkydrmApp SkydrmApp = SkydrmApp.Singleton;

        readonly log4net.ILog Log = SkydrmApp.Singleton.Log;

        public INxlFile SearchInRmsFiles(string fileLocalPath)
        {
            Log.InfoFormat("Search MyVault RmsFile By FileLocalPath : {0}", fileLocalPath);

            INxlFile result = null;
            IMyVaultFile[] myVaultFiles = SkydrmApp.MyVault.List();
            foreach (IMyVaultFile myVaultFile in myVaultFiles)
            {
                if (String.Equals(myVaultFile.Nxl_Local_Path, fileLocalPath, StringComparison.CurrentCultureIgnoreCase))
                {
                    result = new fileSystem.myvault.MyVaultRmsDoc(myVaultFile);
                    break;
                }
            }
            return result;
        }

        public INxlFile SearchInLocalFiles(string fileLocalPath)
        {
            Log.InfoFormat("Search MyVault Local File By FileLocalPath : {0}", fileLocalPath);
            INxlFile result = null;
            IPendingUploadFile[] myVaultLocalFiles = SkydrmApp.MyVault.GetPendingUploads();
            foreach (IPendingUploadFile myVaultLocalFile in myVaultLocalFiles)
            {
                if (String.Equals(myVaultLocalFile.LocalDiskPath, fileLocalPath, StringComparison.CurrentCultureIgnoreCase))
                {
                    result = new PendingUploadFile(myVaultLocalFile);
                    break;
                }
            }
            return result;
        }

    }

    public class SearchMyVaultFileByDuidEx : ISearchFileInRepoEx
    {
        readonly SkydrmApp SkydrmApp = SkydrmApp.Singleton;

        readonly log4net.ILog Log = SkydrmApp.Singleton.Log;

        public INxlFile SearchInRmsFiles(string duid)
        {
            Log.InfoFormat("Search MyVault RmsFile By Duid : {0}", duid);

            INxlFile result = null;
            IMyVaultFile[] myVaultFiles = SkydrmApp.MyVault.ListWithoutFilter();
            foreach (IMyVaultFile myVaultFile in myVaultFiles)
            {
                if (String.Equals(myVaultFile.Duid, duid, StringComparison.CurrentCultureIgnoreCase))
                {
                    result = new fileSystem.myvault.MyVaultRmsDoc(myVaultFile);
                    break;
                }
            }
            return result;
        }

        public INxlFile SearchInLocalFiles(string duid)
        {
            Log.InfoFormat("Search MyVault Local File By Duid : {0}", duid);
            return null;
        }
        
    }
    #endregion

    #region Search Project
    public class SearchProjectFileByRmsRemotePathEx : ISearchFileInRepoEx
    {
        readonly SkydrmApp SkydrmApp = SkydrmApp.Singleton;

        readonly log4net.ILog Log = SkydrmApp.Singleton.Log;

        private String GetParentFolderPath(String pathId)
        {
            if (String.IsNullOrEmpty(pathId))
            {
                return null;
            }
            if (String.Equals(pathId, "/", StringComparison.CurrentCultureIgnoreCase))
            {
                return "/";
            }

            if (pathId.EndsWith("/", StringComparison.CurrentCultureIgnoreCase))
            {
                int FirstLastIndexOf = pathId.LastIndexOf("/");
                String FirstNewPathId = pathId.Substring(0, FirstLastIndexOf);
                int secondLastIndexOf = FirstNewPathId.LastIndexOf("/");
                String newPathId = FirstNewPathId.Substring(0, secondLastIndexOf);
                return newPathId + "/";
            }
            else
            {
                int FirstLastIndexOf = pathId.LastIndexOf("/");
                String FirstNewPathId = pathId.Substring(0, FirstLastIndexOf);
                return FirstNewPathId + "/";
            }
        }

        public INxlFile SearchInRmsFiles(string rmsRemotePath)
        {
            Log.InfoFormat("Search Project RmsFile By RemotePath : {0}", rmsRemotePath);

            INxlFile result = null;

            string ParentFolderPath = GetParentFolderPath(rmsRemotePath);

            foreach (IMyProject MyProject in SkydrmApp.MyProjects.List())
            {
                IProjectFile[] projectFiles = MyProject.ListFiles(ParentFolderPath);

                foreach (IProjectFile projectFile in projectFiles)
                {
                    if (!projectFile.isFolder)
                    {
                        if (String.Equals(projectFile.RMSDisplayPath, rmsRemotePath, StringComparison.CurrentCultureIgnoreCase))
                        {
                            result = new fileSystem.project.ProjectRepo.ProjectRmsDoc(projectFile, MyProject.Id, MyProject.DisplayName);
                            goto end_of_loop;
                        }
                    }
                }
            }
            end_of_loop:
            return result;
        }

        public INxlFile SearchInLocalFiles(string rmsRemotePath)
        {
            Log.InfoFormat("Search Project LocalFile By RemotePath : {0}", rmsRemotePath);
            return null;
        }
    }

    public class SearchProjectFileByLocalPathEx : ISearchFileInRepoEx
    {
        readonly SkydrmApp SkydrmApp = SkydrmApp.Singleton;

        readonly log4net.ILog Log = SkydrmApp.Singleton.Log;

        public INxlFile SearchInRmsFiles(string fileLocalPath)
        {
            Log.InfoFormat("Search Project RmsFile By FileLocalPath : {0}", fileLocalPath);

            INxlFile result = null;

            foreach (IMyProject MyProject in SkydrmApp.MyProjects.List())
            {
                IProjectFile[] projectFiles = MyProject.ListAllProjectFile();

                foreach (IProjectFile projectFile in projectFiles)
                {
                    if (!projectFile.isFolder)
                    {
                        if (String.Equals(projectFile.LocalDiskPath, fileLocalPath, StringComparison.CurrentCultureIgnoreCase))
                        {
                            result = new fileSystem.project.ProjectRepo.ProjectRmsDoc(projectFile, MyProject.Id, MyProject.DisplayName);
                            goto end_of_loop;
                        }
                    }
                }
            }
            end_of_loop:
            return result;
        }

        public INxlFile SearchInLocalFiles(string fileLocalPath)
        {
            Log.InfoFormat("Search Project LocalFile By Local Path : {0}", fileLocalPath);

            INxlFile result = null;

            foreach (IMyProject MyProject in SkydrmApp.MyProjects.List())
            {
                IPendingUploadFile[] projectLocalFiles = MyProject.GetPendingUploads();

                foreach (IPendingUploadFile projectLocalFile in projectLocalFiles)
                {
                    if (String.Equals(projectLocalFile.LocalDiskPath, fileLocalPath, StringComparison.CurrentCultureIgnoreCase))
                    {
                        result = new PendingUploadFile(projectLocalFile, MyProject.Id, MyProject.DisplayName);
                        goto end_of_loop;
                    }
                }
            }
            end_of_loop:
            return result;
        }
    }

    public class SearchProjectFileByDUIDEx : ISearchFileInRepoEx
    {
        readonly SkydrmApp SkydrmApp = SkydrmApp.Singleton;

        readonly log4net.ILog Log = SkydrmApp.Singleton.Log;

        public INxlFile SearchInRmsFiles(string duid)
        {
            Log.InfoFormat("Search Project RmsFile By Duid : {0}", duid);

            INxlFile result = null;

            foreach (IMyProject MyProject in SkydrmApp.MyProjects.List())
            {
                IProjectFile[] projectFiles = MyProject.ListAllProjectFile();

                foreach (IProjectFile projectFile in projectFiles)
                {
                    if (!projectFile.isFolder)
                    {
                        if (String.Equals(projectFile.RmsDuId, duid, StringComparison.CurrentCultureIgnoreCase))
                        {
                            result = new fileSystem.project.ProjectRepo.ProjectRmsDoc(projectFile, MyProject.Id, MyProject.DisplayName);
                            goto end_of_loop;
                        }
                    }
                }
            }
            end_of_loop:
            return result;
        }

        public INxlFile SearchInLocalFiles(string duid)
        {
            Log.InfoFormat("Search Project LocalFile By Duid : {0}", duid);

            //project local file do not has DUID
            return null;
        }
    }
    #endregion

    #region Search WorkSpace
    public class SearchWorkSpaceFileByRmsRemotePathEx : ISearchFileInRepoEx
    {
        readonly SkydrmApp SkydrmApp = SkydrmApp.Singleton;

        readonly log4net.ILog Log = SkydrmApp.Singleton.Log;

        private String GetParentFolderPath(String pathId)
        {
            if (String.IsNullOrEmpty(pathId))
            {
                return null;
            }
            if (String.Equals(pathId, "/", StringComparison.CurrentCultureIgnoreCase))
            {
                return "/";
            }

            if (pathId.EndsWith("/", StringComparison.CurrentCultureIgnoreCase))
            {
                int FirstLastIndexOf = pathId.LastIndexOf("/");
                String FirstNewPathId = pathId.Substring(0, FirstLastIndexOf);
                int secondLastIndexOf = FirstNewPathId.LastIndexOf("/");
                String newPathId = FirstNewPathId.Substring(0, secondLastIndexOf);
                return newPathId + "/";
            }
            else
            {
                int FirstLastIndexOf = pathId.LastIndexOf("/");
                String FirstNewPathId = pathId.Substring(0, FirstLastIndexOf);
                return FirstNewPathId + "/";
            }
        }

        public INxlFile SearchInRmsFiles(string rmsRemotePath)
        {
            Log.InfoFormat("Search WorkSpace RmsFile By RemotePath : {0}", rmsRemotePath);

            INxlFile result = null;

            string ParentFolderPath = GetParentFolderPath(rmsRemotePath);

            IWorkSpaceFile[] Files = SkydrmApp.WorkSpace.List(ParentFolderPath);

            foreach (IWorkSpaceFile workSpaceFile in Files)
            {
                if (!workSpaceFile.Is_Folder)
                {
                    if (String.Equals(workSpaceFile.Path_Display, rmsRemotePath, StringComparison.CurrentCultureIgnoreCase))
                    {
                        result = new SkydrmDesktop.rmc.fileSystem.workspace.WorkSpaceRepo.WorkSpaceRmsDoc(workSpaceFile);
                        break;
                    }
                }
            }
            return result;
        }

        public INxlFile SearchInLocalFiles(string rmsRemotePath)
        {
            Log.InfoFormat("Search WorkSpace LocalFile By RemotePath : {0}", rmsRemotePath);
            return null;
        }
    }

    public class SearchWorkSpaceFileByLocalPathEx : ISearchFileInRepoEx
    {
        readonly SkydrmApp SkydrmApp = SkydrmApp.Singleton;

        readonly log4net.ILog Log = SkydrmApp.Singleton.Log;

        public INxlFile SearchInRmsFiles(string fileLocalPath)
        {
            Log.InfoFormat("Search WorkSpace RmsFile By FileLocalPath : {0}", fileLocalPath);

            INxlFile result = null;

            IWorkSpaceFile[] Files = SkydrmApp.WorkSpace.ListAll();

            foreach (IWorkSpaceFile item in Files)
            {
                if (!item.Is_Folder)
                {
                    if (String.Equals(item.Nxl_Local_Path, fileLocalPath, StringComparison.CurrentCultureIgnoreCase))
                    {
                        result = new SkydrmDesktop.rmc.fileSystem.workspace.WorkSpaceRepo.WorkSpaceRmsDoc(item);
                        break;
                    }
                }
            }
            return result;
        }

        public INxlFile SearchInLocalFiles(string fileLocalPath)
        {
            Log.InfoFormat("Search WorkSpace LocalFile ByFileLocalPath : {0}", fileLocalPath);

            INxlFile result = null;

            IPendingUploadFile[] LocalFiles = SkydrmApp.WorkSpace.GetPendingUploads();

            foreach (IPendingUploadFile item in LocalFiles)
            {
                if (String.Equals(item.LocalDiskPath, fileLocalPath, StringComparison.CurrentCultureIgnoreCase))
                {
                    result = new PendingUploadFile(item);
                    break;
                }
            }
            return result;
        }
       
    }

    public class SearchWorkSpaceFileByDUIDEx : ISearchFileInRepoEx
    {
        readonly SkydrmApp SkydrmApp = SkydrmApp.Singleton;

        readonly log4net.ILog Log = SkydrmApp.Singleton.Log;

        public INxlFile SearchInRmsFiles(string duid)
        {
            Log.InfoFormat("Search WorkSpace RmsFile By duid : {0}", duid);

            INxlFile result = null;

            IWorkSpaceFile[] Files = SkydrmApp.WorkSpace.ListAll();

            foreach (IWorkSpaceFile item in Files)
            {
                if (!item.Is_Folder)
                {
                    if (String.Equals(item.Duid, duid, StringComparison.CurrentCultureIgnoreCase))
                    {
                        result = new SkydrmDesktop.rmc.fileSystem.workspace.WorkSpaceRepo.WorkSpaceRmsDoc(item);
                        break;
                    }
                }
            }
            return result;
        }

        public INxlFile SearchInLocalFiles(string duid)
        {
            Log.InfoFormat("Search WorkSpace LocalFile By duid : {0}", duid);

            return null;
        }

    }
    #endregion
}
