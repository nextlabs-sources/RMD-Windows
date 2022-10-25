using SkydrmDesktop;
using SkydrmDesktop.rmc.featureProvider;
using SkydrmLocal.rmc.database2.table.project;
using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.search
{
    public interface IGlobalSearch
    {

        bool SearchByFileName(string FileName, ISearchedCallback Callback);

        bool SearchByRmsRemotePath(string RmsRemotePath, ISearchedCallback Callback);

        bool SearchByLocalPath(string FileLocalPath, ISearchedCallback Callback);

        bool SearchByDUID(string Duid, ISearchedCallback Callback);
    }

    public interface ISearchedCallback
    {
        void FoundInProject(IProjectFile ProjectFile);

        void FoundInProject(IProjectLocalFile ProjectLocalFile);

        void FoundInMyVault(IMyVaultFile MyVaultFile);

        void FoundInMyVault(IMyVaultLocalFile MyVaultLocalFile);

        void FoundInSharedWithMe(ISharedWithMeFile SharedWithMeFile);
    }

    public interface ISearchFileInProject
    {
        IProjectFile SearchInRmsFiles(string Param);

        IProjectLocalFile SearchInLocalFiles(string Param);
    }

    public interface ISearchFileInSharedWithMe
    {
        ISharedWithMeFile Search(string Param);
    }

    public interface ISearchFileInMyVault
    {
        IMyVaultFile SearchInRmsFiles(string Param);

        IMyVaultLocalFile SearchInLocalFiles(string Param);
    }

    public interface ISearchFileInWorkSpace
    {
        IWorkSpaceFile SearchInRmsFiles(string Param);
        IWorkSpaceLocalFile SearchInLocalFiles(string Param);
    }

  


    public class SearchProjectFileByName : ISearchFileInProject
    {
        readonly SkydrmApp SkydrmApp = SkydrmApp.Singleton;

        readonly log4net.ILog Log = SkydrmApp.Singleton.Log;

        public IProjectFile SearchInRmsFiles(string NXLFileName)
        {
            Log.InfoFormat("Search Project RmsFile By NXLFileName : {0}", NXLFileName);

            IProjectFile result = null;

            foreach (IMyProject MyProject in SkydrmApp.MyProjects.List())
            {
                IProjectFile[] projectFiles = MyProject.ListAllProjectFile();

                foreach (IProjectFile projectFile in projectFiles)
                {
                    if (!projectFile.isFolder)
                    {
                        if (String.Equals(projectFile.Name, NXLFileName, StringComparison.CurrentCultureIgnoreCase))
                        {
                            result = projectFile;
                            break;
                        }
                    }
                }
            }

            return result;
        }

        public IProjectLocalFile SearchInLocalFiles(string NXLFileName)
        {
            Log.InfoFormat("Search Project LocalFile By NXLFileName : {0}", NXLFileName);

            IProjectLocalFile result = null;

            foreach (IMyProject MyProject in SkydrmApp.MyProjects.List())
            {

                IProjectLocalFile[] projectLocalFiles = MyProject.ListProjectLocalFiles();

                foreach (IProjectLocalFile projectLocalFile in projectLocalFiles)
                {
                    if (String.Equals(projectLocalFile.Name, NXLFileName, StringComparison.CurrentCultureIgnoreCase))
                    {
                        result = projectLocalFile;
                        break;
                    }
                }
            }
            return result;
        }
    }

    public class SearchProjectFileByRmsRemotePath : ISearchFileInProject
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

        public IProjectFile SearchInRmsFiles(string RmsRemotePath)
        {
            Log.InfoFormat("Search Project RmsFile By RemotePath : {0}", RmsRemotePath);

            MyProject myProject = null;
            IProjectFile result = null;

            string ParentFolderPath = GetParentFolderPath(RmsRemotePath);

            Project[] ps = SkydrmApp.DBFunctionProvider.ListProject();

            if (ps == null || ps.Length == 0)
            {
                myProject = null;
            }

            foreach (Project project in ps)
            {

                myProject = new MyProject(SkydrmApp, project);

                IProjectFile[] projectFiles = myProject.ListFiles(ParentFolderPath);

                foreach (IProjectFile projectFile in projectFiles)
                {
                    if (String.Equals(projectFile.RMSDisplayPath, RmsRemotePath, StringComparison.CurrentCultureIgnoreCase))
                    {
                        result = projectFile;
                        goto end_of_loop;
                    }
                }
            }
            end_of_loop:
            return result;
        }

        public IProjectLocalFile SearchInLocalFiles(string RmsRemotePath)
        {
            Log.InfoFormat("Search Project LocalFile By RemotePath : {0}", RmsRemotePath);
            return null;
        }
    }

    public class SearchProjectFileByLocalPath : ISearchFileInProject
    {
        readonly SkydrmApp SkydrmApp = SkydrmApp.Singleton;

        readonly log4net.ILog Log = SkydrmApp.Singleton.Log;

        public IProjectLocalFile SearchInLocalFiles(string FileLocalPath)
        {
            Log.InfoFormat("Search Project LocalFile By Local Path : {0}", FileLocalPath);

            IProjectLocalFile result = null;

            foreach (IMyProject MyProject in SkydrmApp.MyProjects.List())
            {

                IProjectLocalFile[] projectLocalFiles = MyProject.ListProjectLocalFiles();

                foreach (IProjectLocalFile projectLocalFile in projectLocalFiles)
                {
                    if (String.Equals(projectLocalFile.LocalDiskPath, FileLocalPath, StringComparison.CurrentCultureIgnoreCase))
                    {
                        result = projectLocalFile;
                        break;
                    }
                }
            }
            return result;
        }

        public IProjectFile SearchInRmsFiles(string FileLocalPath)
        {
            Log.InfoFormat("Search Project RmsFile By FileLocalPath : {0}", FileLocalPath);

            IProjectFile result = null;

            foreach (IMyProject MyProject in SkydrmApp.MyProjects.List())
            {
                IProjectFile[] projectFiles = MyProject.ListAllProjectFile();

                foreach (IProjectFile projectFile in projectFiles)
                {
                    if (!projectFile.isFolder)
                    {
                        if (String.Equals(projectFile.LocalDiskPath, FileLocalPath, StringComparison.CurrentCultureIgnoreCase))
                        {
                            result = projectFile;
                            break;
                        }
                    }
                }
            }

            return result;
        }
    }


    public class SearchProjectFileByDUID : ISearchFileInProject
    {
        readonly SkydrmApp SkydrmApp = SkydrmApp.Singleton;

        readonly log4net.ILog Log = SkydrmApp.Singleton.Log;

        public IProjectLocalFile SearchInLocalFiles(string Duid)
        {
            Log.InfoFormat("Search Project LocalFile By Duid : {0}", Duid);

            //project local file do not has DUID
            return null;
        }

        public IProjectFile SearchInRmsFiles(string Duid)
        {
            Log.InfoFormat("Search Project RmsFile By Duid : {0}", Duid);

            IProjectFile result = null;

            foreach (IMyProject MyProject in SkydrmApp.MyProjects.List())
            {
                IProjectFile[] projectFiles = MyProject.ListAllProjectFile();

                foreach (IProjectFile projectFile in projectFiles)
                {
                    if (!projectFile.isFolder)
                    {
                        if (String.Equals(projectFile.RmsDuId, Duid, StringComparison.CurrentCultureIgnoreCase))
                        {
                            result = projectFile;
                            break;
                        }
                    }
                }
            }

            return result;
        }
    }

    public class SearchMyVaultFileByLocalPath : ISearchFileInMyVault
    {
        readonly SkydrmApp SkydrmApp = SkydrmApp.Singleton;

        readonly log4net.ILog Log = SkydrmApp.Singleton.Log;

        public IMyVaultLocalFile SearchInLocalFiles(string FileLocalPath)
        {
            Log.InfoFormat("Search MyVault Local File By FileLocalPath : {0}", FileLocalPath);
            IMyVaultLocalFile result = null;
            IMyVaultLocalFile[] myVaultLocalFiles = SkydrmApp.MyVault.ListLocalAdded();
            foreach (IMyVaultLocalFile myVaultLocalFile in myVaultLocalFiles)
            {
                if (String.Equals(myVaultLocalFile.LocalDiskPath, FileLocalPath, StringComparison.CurrentCultureIgnoreCase))
                {
                    result = myVaultLocalFile;
                    break;
                }
            }
            return result;
        }

        public IMyVaultFile SearchInRmsFiles(string FileLocalPath)
        {
            Log.InfoFormat("Search MyVault RmsFile By FileLocalPath : {0}", FileLocalPath);

            IMyVaultFile result = null;
            IMyVaultFile[] myVaultFiles = SkydrmApp.MyVault.List();
            foreach (IMyVaultFile myVaultFile in myVaultFiles)
            {
                if (String.Equals(myVaultFile.Nxl_Local_Path, FileLocalPath, StringComparison.CurrentCultureIgnoreCase))
                {
                    result = myVaultFile;
                    break;
                }
            }
            return result;
        }
    }

    public class SearchMyVaultFileByName : ISearchFileInMyVault
    {
        readonly SkydrmApp SkydrmApp = SkydrmApp.Singleton;

        readonly log4net.ILog Log = SkydrmApp.Singleton.Log;

        public IMyVaultFile SearchInRmsFiles(string NXlFileName)
        {
            Log.InfoFormat("Search MyVault RmsFile By NXLFileName : {0}", NXlFileName);

            IMyVaultFile result = null;
            IMyVaultFile[] myVaultFiles = SkydrmApp.MyVault.List();
            foreach (IMyVaultFile myVaultFile in myVaultFiles)
            {
                if (String.Equals(myVaultFile.Nxl_Name, NXlFileName, StringComparison.CurrentCultureIgnoreCase))
                {
                    result = myVaultFile;
                    break;
                }
            }
            return result;
        }

        public IMyVaultLocalFile SearchInLocalFiles(string NXlFileName)
        {
            Log.InfoFormat("Search MyVault Local File By NXLFileName : {0}", NXlFileName);
            IMyVaultLocalFile result = null;
            IMyVaultLocalFile[] myVaultLocalFiles = SkydrmApp.MyVault.ListLocalAdded();
            foreach (IMyVaultLocalFile myVaultLocalFile in myVaultLocalFiles)
            {
                if (String.Equals(myVaultLocalFile.Name, NXlFileName, StringComparison.CurrentCultureIgnoreCase))
                {
                    result = myVaultLocalFile;
                    break;
                }
            }
            return result;
        }
    }

    public class SearchMyVaultFileByRmsRemotePath : ISearchFileInMyVault
    {
        readonly SkydrmApp SkydrmApp = SkydrmApp.Singleton;

        readonly log4net.ILog Log = SkydrmApp.Singleton.Log;

        public IMyVaultLocalFile SearchInLocalFiles(string RmsRemotePath)
        {
            Log.InfoFormat("Search MyVault Local File By RmsRemotePath : {0}", RmsRemotePath);
            return null;
        }

        public IMyVaultFile SearchInRmsFiles(string RmsRemotePath)
        {
            Log.InfoFormat("Search MyVault RmsFile By RmsRemotePath : {0}", RmsRemotePath);
            IMyVaultFile result = null;
            IMyVaultFile[] myVaultFiles = SkydrmApp.MyVault.List();
            foreach (IMyVaultFile myVaultFile in myVaultFiles)
            {
                if (String.Equals(myVaultFile.Display_Path, RmsRemotePath, StringComparison.CurrentCultureIgnoreCase))
                {
                    result = myVaultFile;
                    break;
                }
            }
            return result;
        }
    }

    public class SearchMyVaultFileByDuid : ISearchFileInMyVault
    {
        readonly SkydrmApp SkydrmApp = SkydrmApp.Singleton;

        readonly log4net.ILog Log = SkydrmApp.Singleton.Log;

        public IMyVaultLocalFile SearchInLocalFiles(string Duid)
        {
            Log.InfoFormat("Search MyVault Local File By Duid : {0}", Duid);
            return null;
        }

        public IMyVaultFile SearchInRmsFiles(string Duid)
        {
            Log.InfoFormat("Search MyVault RmsFile By Duid : {0}", Duid);

            IMyVaultFile result = null;
            IMyVaultFile[] myVaultFiles = SkydrmApp.MyVault.ListWithoutFilter();
            foreach (IMyVaultFile myVaultFile in myVaultFiles)
            {
                if (String.Equals(myVaultFile.Duid, Duid, StringComparison.CurrentCultureIgnoreCase))
                {
                    result = myVaultFile;
                    break;
                }
            }
            return result;
        }
    }

    public class SearchSharedWithMeFileByLocalPath : ISearchFileInSharedWithMe
    {
        readonly SkydrmApp SkydrmApp = SkydrmApp.Singleton;

        readonly log4net.ILog Log = SkydrmApp.Singleton.Log;

        public ISharedWithMeFile Search(string FileLocalPath)
        {
            Log.InfoFormat("Search SharedWithMe File By FileLocalPath : {0}", FileLocalPath);

            ISharedWithMeFile result = null;
            ISharedWithMeFile[] sharedWithMeFiles = SkydrmApp.SharedWithMe.List();
            foreach (ISharedWithMeFile sharedWithMeFile in sharedWithMeFiles)
            {
                if (String.Equals(sharedWithMeFile.LocalDiskPath, FileLocalPath, StringComparison.CurrentCultureIgnoreCase))
                {
                    result = sharedWithMeFile;
                    break;
                }
            }
            return result;
        }
    }

    public class SearchSharedWithMeFileByName : ISearchFileInSharedWithMe
    {
        readonly SkydrmApp SkydrmApp = SkydrmApp.Singleton;

        readonly log4net.ILog Log = SkydrmApp.Singleton.Log;

        public ISharedWithMeFile Search(string NXLFileName)
        {
            Log.InfoFormat("Search SharedWithMe File By NXLFileName : {0}", NXLFileName);

            ISharedWithMeFile result = null;
            ISharedWithMeFile[] sharedWithMeFiles = SkydrmApp.SharedWithMe.List();
            foreach (ISharedWithMeFile sharedWithMeFile in sharedWithMeFiles)
            {
                if (String.Equals(sharedWithMeFile.Name, NXLFileName, StringComparison.CurrentCultureIgnoreCase))
                {
                    result = sharedWithMeFile;
                    break;
                }
            }
            return result;
        }
    }

    public class SearchSharedWithMeFileByRmsRemotePath : ISearchFileInSharedWithMe
    {
        readonly SkydrmApp SkydrmApp = SkydrmApp.Singleton;

        readonly log4net.ILog Log = SkydrmApp.Singleton.Log;

        public ISharedWithMeFile Search(string RmsRemotePath)
        {
            Log.InfoFormat("Search SharedWithMe File By RmsRemotePath : {0}", RmsRemotePath);
            ISharedWithMeFile result = null;
            ISharedWithMeFile[] sharedWithMeFiles = SkydrmApp.SharedWithMe.List();
            foreach (ISharedWithMeFile sharedWithMeFile in sharedWithMeFiles)
            {
                if (String.Equals(sharedWithMeFile.Name, RmsRemotePath.Substring(1), StringComparison.CurrentCultureIgnoreCase))
                {
                    result = sharedWithMeFile;
                    break;
                }
            }
            return result;
        }
    }

    public class SearchSharedWithMeFileByDuid : ISearchFileInSharedWithMe
    {
        readonly SkydrmApp SkydrmApp = SkydrmApp.Singleton;

        readonly log4net.ILog Log = SkydrmApp.Singleton.Log;

        public ISharedWithMeFile Search(string Duid)
        {
            Log.InfoFormat("Search SharedWithMe File By Duid : {0}", Duid);

            ISharedWithMeFile result = null;
            ISharedWithMeFile[] sharedWithMeFiles = SkydrmApp.SharedWithMe.List();
            foreach (ISharedWithMeFile sharedWithMeFile in sharedWithMeFiles)
            {
                if (String.Equals(sharedWithMeFile.Duid, Duid, StringComparison.CurrentCultureIgnoreCase))
                {
                    result = sharedWithMeFile;
                    break;
                }
            }
            return result;
        }
    }


    public class SearchWorkSpaceFileByName : ISearchFileInWorkSpace
    {
        public IWorkSpaceLocalFile SearchInLocalFiles(string Param)
        {
            throw new NotImplementedException();
        }

        public IWorkSpaceFile SearchInRmsFiles(string Param)
        {
            throw new NotImplementedException();
        }
    }
    public class SearchWorkSpaceFileByRmsRemotePath : ISearchFileInWorkSpace
    {
        public IWorkSpaceLocalFile SearchInLocalFiles(string Param)
        {
            throw new NotImplementedException();
        }

        public IWorkSpaceFile SearchInRmsFiles(string Param)
        {
            throw new NotImplementedException();
        }
    }
    public class SearchWorkSpaceFileByLocalPath : ISearchFileInWorkSpace
    {
        readonly SkydrmApp SkydrmApp = SkydrmApp.Singleton;

        readonly log4net.ILog Log = SkydrmApp.Singleton.Log;

        public IWorkSpaceLocalFile SearchInLocalFiles(string FileLocalPath)
        {
            Log.InfoFormat("Search WorkSpace LocalFile By Local Path : {0}", FileLocalPath);

            IWorkSpaceLocalFile result = null;

            IWorkSpaceLocalFile[] LocalFiles = SkydrmApp.WorkSpace.ListLocalAllAdded();

            foreach (IWorkSpaceLocalFile item in LocalFiles)
            {
                if (String.Equals(item.LocalDiskPath, FileLocalPath, StringComparison.CurrentCultureIgnoreCase))
                {
                    result = item;
                    break;
                }
            }
            return result;
        }

        public IWorkSpaceFile SearchInRmsFiles(string FileLocalPath)
        {
            Log.InfoFormat("Search WorkSpace RmsFile By FileLocalPath : {0}", FileLocalPath);

            IWorkSpaceFile result = null;

            IWorkSpaceFile[] Files = SkydrmApp.WorkSpace.ListAll();

            foreach (IWorkSpaceFile item in Files)
            {
                if (!item.Is_Folder)
                {
                    if (String.Equals(item.Nxl_Local_Path, FileLocalPath, StringComparison.CurrentCultureIgnoreCase))
                    {
                        result = item;
                        break;
                    }
                }
            }

            return result;
        }
    }
    public class SearchWorkSpaceFileByDUID : ISearchFileInWorkSpace
    {
        public IWorkSpaceLocalFile SearchInLocalFiles(string Param)
        {
            throw new NotImplementedException();
        }

        public IWorkSpaceFile SearchInRmsFiles(string Param)
        {
            throw new NotImplementedException();
        }
    }


    public class RealGlobalSearch : IGlobalSearch
    {
        private readonly SkydrmApp SkydrmApp = SkydrmApp.Singleton;
        private readonly log4net.ILog Log = SkydrmApp.Singleton.Log;

        public bool SearchByFileName(string NXlFileName, ISearchedCallback Callback)
        {
            Log.InfoFormat("Search By FileName:{0}", NXlFileName);
            ISearchFileInProject SearchFileInProject = new SearchProjectFileByName();
            ISearchFileInMyVault SearchFileInMyVault = new SearchMyVaultFileByName();
            ISearchFileInSharedWithMe SearchFileInSharedWithMe = new SearchSharedWithMeFileByName();
            return DoSearch(SearchFileInProject, SearchFileInMyVault, SearchFileInSharedWithMe, NXlFileName, Callback);
        }

        public bool SearchByLocalPath(string FileLocalPath, ISearchedCallback Callback)
        {
            Log.InfoFormat("Search By FileLocalPath:{0}", FileLocalPath);
            ISearchFileInProject SearchFileInProject = new SearchProjectFileByLocalPath();
            ISearchFileInMyVault SearchFileInMyVault = new SearchMyVaultFileByLocalPath();
            ISearchFileInSharedWithMe SearchFileInSharedWithMe = new SearchSharedWithMeFileByLocalPath();
            return DoSearch(SearchFileInProject, SearchFileInMyVault, SearchFileInSharedWithMe, FileLocalPath, Callback);
        }

        public bool SearchByRmsRemotePath(string RmsRemotePath, ISearchedCallback Callback)
        {
            Log.InfoFormat("Search By RmsRemotePath:{0}", RmsRemotePath);
            ISearchFileInProject SearchFileInProject = new SearchProjectFileByRmsRemotePath();
            ISearchFileInMyVault SearchFileInMyVault = new SearchMyVaultFileByRmsRemotePath();
            ISearchFileInSharedWithMe SearchFileInSharedWithMe = new SearchSharedWithMeFileByRmsRemotePath();
            return DoSearch(SearchFileInProject, SearchFileInMyVault, SearchFileInSharedWithMe, RmsRemotePath, Callback);
        }

        public bool SearchByDUID(string Duid, ISearchedCallback Callback)
        {
            Log.InfoFormat("Search By Duid:{0}", Duid);
            ISearchFileInProject SearchFileInProject = new SearchProjectFileByDUID();
            ISearchFileInMyVault SearchFileInMyVault = new SearchMyVaultFileByDuid();
            ISearchFileInSharedWithMe SearchFileInSharedWithMe = new SearchSharedWithMeFileByDuid();
            return DoSearch(SearchFileInProject, SearchFileInMyVault, SearchFileInSharedWithMe, Duid, Callback);
        }

        /// <summary>
        /// Find the project nxl rms display path.
        /// </summary>
        /// <param name="localPath">nxl local disk path.</param>
        /// <returns></returns>
        public string GetProjectSourceNxlDisplayPath(string localPath)
        {
            //Sanity check first.
            if(string.IsNullOrEmpty(localPath))
            {
                return "";
            }

            var app = SkydrmApp.Singleton;
            try
            {
                //Get current searched target's FingerPrint.
                //We should take nxl's duid as matches condition.
                //Cause SaveAs file we cannot find it in our own database,
                //but we can find the original nxl file according to saved as nxl's duid.
                //[Ps:We can find the orinal source nxl according to duid
                //the save as target has the same duid with original source save as file]
                var fp = app.Rmsdk.User.GetNxlFileFingerPrint(localPath);

                string projectRmsDisplayName = "";
                string projectNxlRmsDisplayPath = "";

                foreach (var p in SkydrmApp.MyProjects.List())
                {
                    //Recording current searched project's display name.
                    if(p.Id == fp.projectId)
                    {
                        projectRmsDisplayName = p.DisplayName;
                    }

                    IProjectFile[] projectLocalFiles = p.ListAllProjectFile();
                    foreach (var pf in projectLocalFiles)
                    {
                        //Take duid as matches condition, if matches recording what we need.
                        if (string.Equals(pf.RmsDuId, fp.duid, StringComparison.CurrentCultureIgnoreCase))
                        {
                            //Recording current searched project file's rms display path.
                            projectNxlRmsDisplayPath = pf.RMSDisplayPath;

                            if(!string.IsNullOrEmpty(projectNxlRmsDisplayPath))
                            {
                                projectNxlRmsDisplayPath = projectNxlRmsDisplayPath.Replace('\\', '/');
                            }

                            return string.Format("{0}{1}", projectRmsDisplayName, projectNxlRmsDisplayPath);
                        }
                    }
                }

                return string.Format("{0}{1}", projectRmsDisplayName, projectNxlRmsDisplayPath);
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
            
            return "";
        }

        private bool DoSearch(ISearchFileInProject SearchFileInProject,
                      ISearchFileInMyVault SearchFileInMyVault,
                      ISearchFileInSharedWithMe SearchFileInSharedWithMe,
                      string Param,
                      ISearchedCallback callback)
        {

            bool result = false;

            IProjectFile projectFile = SearchFileInProject.SearchInRmsFiles(Param);

            if (null != projectFile)
            {
                callback.FoundInProject(projectFile);
                result = true;
            }

            IProjectLocalFile projectLocalFile = SearchFileInProject.SearchInLocalFiles(Param);
            if (null != projectLocalFile)
            {
                callback.FoundInProject(projectLocalFile);
                result = true;
            }

            IMyVaultFile myVaultFile = SearchFileInMyVault.SearchInRmsFiles(Param);

            if (null != myVaultFile)
            {
                callback.FoundInMyVault(myVaultFile);
                result = true;
            }

            IMyVaultLocalFile myVaultLocalFile = SearchFileInMyVault.SearchInLocalFiles(Param);

            if (null != myVaultLocalFile)
            {
                callback.FoundInMyVault(myVaultLocalFile);
                result = true;
            }

            ISharedWithMeFile sharedWithMeFile = SearchFileInSharedWithMe.Search(Param);

            if (null != sharedWithMeFile)
            {
                callback.FoundInSharedWithMe(sharedWithMeFile);
                result = true;
            }

            return result;
        }


    }

    public class GlobalSearch
    {
        private static readonly RealGlobalSearch RealGlobalSearch = new RealGlobalSearch();

        public static bool SearchByFileName(string NXlFileName, ISearchedCallback Callback)
        {
            return RealGlobalSearch.SearchByFileName(NXlFileName, Callback);
        }

        public static bool SearchByLocalPath(string FileLocalPath, ISearchedCallback Callback)
        {
            return RealGlobalSearch.SearchByLocalPath(FileLocalPath, Callback);
        }

        public static bool SearchByRmsRemotePath(string RmsRemotePath, ISearchedCallback Callback)
        {
            return RealGlobalSearch.SearchByRmsRemotePath(RmsRemotePath, Callback);
        }

        public static string GetProjectSourceNxlDisplayPath(string localPath)
        {
            return RealGlobalSearch.GetProjectSourceNxlDisplayPath(localPath);
        }

        public static bool SearchByDuid(string Duid, ISearchedCallback Callback)
        {
            return RealGlobalSearch.SearchByDUID(Duid, Callback);
        }

    }
}
