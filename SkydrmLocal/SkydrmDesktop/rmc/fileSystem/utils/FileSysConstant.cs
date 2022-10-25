using SkydrmDesktop.Resources.languages;
using SkydrmDesktop.rmc.featureProvider.externalDrive.RmsRepoMgr;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.fileSystem.utils
{
    public class FileSysConstant
    {
        // repository name
        public static string HOME = CultureStringInfo.ApplicationFindResource("MainWin__TreeView_Home");
        public static string WORKSPACE = CultureStringInfo.ApplicationFindResource("MainWin__TreeView_WorkSpace");
        public static string MYSPACE = CultureStringInfo.ApplicationFindResource("MainWin__TreeView_MySpace");
        public static string MYVAULT = CultureStringInfo.ApplicationFindResource("MainWin__TreeView_MyVault");
        public static string MYDRIVE = CultureStringInfo.ApplicationFindResource("MainWin__TreeView_MyDrive");
        public static string SHAREDWITHME = CultureStringInfo.ApplicationFindResource("MainWin__TreeView_ShareWithMe");
        public static string PROJECT = CultureStringInfo.ApplicationFindResource("MainWin__TreeView_Project");
        // external repository name
        public static string REPOSITORIES = CultureStringInfo.ApplicationFindResource("MainWin__TreeView_Repositories");
        public static string DROPBOX = CultureStringInfo.ApplicationFindResource("MainWin__TreeView_DropBox");
        public static string ONEDRIVE = CultureStringInfo.ApplicationFindResource("MainWin__TreeView_OneDrive");
        public static string BOX = CultureStringInfo.ApplicationFindResource("MainWin__TreeView_Box");
        public static string SHAREPOINT = CultureStringInfo.ApplicationFindResource("MainWin__TreeView_SharePoint");
        public static string SHAREPOINT_ONLINE = CultureStringInfo.ApplicationFindResource("MainWin__TreeView_SharePointOnline");
        public static string SHAREPOINT_ONPREMISE = CultureStringInfo.ApplicationFindResource("MainWin__TreeView_SharePointOnPremise");
        public static string GOOGLE_DRIVE = CultureStringInfo.ApplicationFindResource("MainWin__TreeView_GoogleDrive");

        // 
        public static string LOCAL_DRIVE = "Local Drive";
        public static string UNKNOWN = "Unknown";

        // repository provider class name
        public static string REPO_CLASS_PERSONAL = "PERSONAL";
        public static string REPO_CLASS_BUSINESS = "BUSINESS";
        public static string REPO_CLASS_APPLICATION = "APPLICATION";

        // Note: must keep the same order with enum "ExternalRepoType" members
        public static string[] ExternalRepoName = 
            {
                DROPBOX, // 0
                ONEDRIVE,
                GOOGLE_DRIVE,
                BOX,
                SHAREPOINT,
                SHAREPOINT_ONLINE,
                SHAREPOINT_ONPREMISE,
                LOCAL_DRIVE,
                UNKNOWN
            };

        public static string GetExternalRepoName(ExternalRepoType type)
        {
            if (type == ExternalRepoType.UNKNOWN)
                return ExternalRepoName[1]; // onedrive, rms bug

            return ExternalRepoName[(int)type];
        }
    }

    // The external reopsitory provider class
    public enum RepositoryProviderClass
    {
        UNKNOWN = 0,
        PERSONAL = 1,
        BUSINESS = 2,
        APPLICATION
    }
}
