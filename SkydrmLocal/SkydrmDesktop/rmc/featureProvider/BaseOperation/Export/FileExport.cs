using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SkydrmDesktop;
using SkydrmLocal.rmc.database2.table.project;
using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.search;
using SkydrmLocal.rmc.ui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.Export
{
    public class FileExport
    {
        private SkydrmApp SkydrmApp = SkydrmApp.Singleton;

        private log4net.ILog Log = SkydrmApp.Singleton.Log;

        public void Export(Param param)
        {
            switch (param.Repo)
            {
                case EnumFileRepo.REPO_PROJECT:

                    ISearchFileInProject searchFileInProject = new SearchProjectFileByRmsRemotePath();
                    IProjectFile projectFile = searchFileInProject.SearchInRmsFiles(param.RmsRemotePath);

                    if (null != projectFile)
                    {
                        projectFile.Export(param.DestinationPath);
                    }

                    break;
                case EnumFileRepo.REPO_MYVAULT:

                    ISearchFileInMyVault searchFileInMyVault = new SearchMyVaultFileByRmsRemotePath();
                    IMyVaultFile myVaultFile = searchFileInMyVault.SearchInRmsFiles(param.RmsRemotePath);

                    if (null != myVaultFile)
                    {
                        myVaultFile.Export(param.DestinationPath);
                    }

                    break;
                case EnumFileRepo.REPO_SHARED_WITH_ME:

                    ISearchFileInSharedWithMe searchFileInSharedWithMe = new SearchSharedWithMeFileByRmsRemotePath();
                    ISharedWithMeFile sharedWithMeFile = searchFileInSharedWithMe.Search(param.RmsRemotePath);

                    if (null != sharedWithMeFile)
                    {
                        sharedWithMeFile.Export(param.DestinationPath);
                    }

                    break;
            }
        }


        public void Export(string exportInfoJson)
        {
            Export(Param.BuildFromJson(exportInfoJson));
        }

        private IMyVaultFile MyVaultFileSearch(string RmsRemotePath)
        {
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

        private ISharedWithMeFile SharedWithMeSearch(string RmsRemotePath)
        {
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


        /**
         * used to get parent folder path by current working path
         */
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

        private IProjectFile ProjectFileSearch(string ParentFolderPath, string RmsRemotePath)
        {
            MyProject myProject = null;
            IProjectFile result = null;

            Project[] ps = SkydrmApp.DBFunctionProvider.ListProject();

            if (ps == null || ps.Length == 0)
            {
                myProject = null;
            }


            foreach (Project project in ps)
            {
                //if (project.Rms_project_id == projectId)
                //{
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
                //}             
            }
            end_of_loop:
            return result;
        }

        public class Param
        {
            public EnumFileRepo Repo { get; }
            public string RmsRemotePath { get; }
            public string FileName { get; }
            public string DestinationPath { get; }

            private Param(EnumFileRepo Repo, string RmsRemotePath, string FileName, string DestinationPath)
            {
                this.Repo = Repo;
                this.RmsRemotePath = RmsRemotePath;
                this.FileName = FileName;
                this.DestinationPath = DestinationPath;
            }

            public static Param BuildFromJson(string exportInfoJson)
            {
                // parse json

                EnumFileRepo enumFileRepo = EnumFileRepo.UNKNOWN;
                string rmsRemotePath = string.Empty;
                string fileName = string.Empty;
                string destinationPath = string.Empty;

                // begin parse
                JObject jo = (JObject)JsonConvert.DeserializeObject(exportInfoJson);
                // parse EnumFileRepo
                if (jo.ContainsKey("EnumFileRepo"))
                {
                    Int32 intEnumFileRepo = -1;
                    string strEnumFileRepo = jo["EnumFileRepo"].ToString();
                    if (int.TryParse(strEnumFileRepo, out intEnumFileRepo))
                    {
                        enumFileRepo = (EnumFileRepo)Enum.ToObject(typeof(EnumFileRepo), intEnumFileRepo);
                    }
                }
                // parse RmsRemotePath
                if (jo.ContainsKey("RmsRemotePath"))
                {
                    rmsRemotePath = jo["RmsRemotePath"].ToString();
                }

                // parse FileName
                if (jo.ContainsKey("FileName"))
                {
                    fileName = jo["FileName"].ToString();
                }
                // parse DestinationPath
                if (jo.ContainsKey("DestinationPath"))
                {
                    destinationPath = jo["DestinationPath"].ToString();
                }

                // logic check
                if(enumFileRepo== EnumFileRepo.UNKNOWN)
                {
                    throw new SkydrmException("no enumFileRepo in json", ExceptionComponent.LogicError);
                }
                if (destinationPath == string.Empty)
                {
                    throw new SkydrmException("no destinationPath in json", ExceptionComponent.LogicError);
                }

                return new Param(enumFileRepo, rmsRemotePath, fileName, destinationPath);
            }

        }
    }
}
