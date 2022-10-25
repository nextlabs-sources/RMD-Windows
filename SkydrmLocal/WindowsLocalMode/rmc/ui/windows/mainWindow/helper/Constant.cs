using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.ui.windows.mainWindow.helper
{
    public class Constant
    {
        public const string COMMAND_PROTECT = "Cmd_Protect";
        public const string COMMAND_SHARE = "Cmd_Share";
        public const string COMMAND_START_UPLOAD = "Cmd_StartUpload";
        public const string COMMAND_STOP_UPLOAD = "Cmd_StopUpload";
        public const string COMMAND_OPEN_WEB = "Cmd_OpenWeb";
        public const string COMMAND_PREFERENCE = "Cmd_Preference";
        public const string COMMAND_REFRESH = "Cmd_Refresh";

        public const string COMMAND_UPLOAD_FOLDER = "Cmd_UploadFolder";
        public const string COMMAND_OFFLINE_FILES = "Cmd_OfflineFiles";
        public const string COMMAND_OUTBOX = "Cmd_OutBox";

        #region Menu Items CMD

        // File Menu
        public const string MENU_OPEN_FILE = "Open File";
        public const string MENU_PROTECT_FILE = "Protect File";
        public const string MENU_SHARE_FILE = "Share File";
        public const string MENU_REMOVE_FILE = "Remove File";
        public const string MENU_STOPE_UPLOAD = "Stop Upload";
        public const string MENU_START_UPLOAD = "Start Upload";
        public const string MENU_VIEW_FILEINFO = "View File Info";
        public const string MENU_ACTIVITY_LOG = "Activity Log";
        public const string MENU_SIGNOUT = "Signout";
        public const string MENU_EXIT = "Exit";

        // View Menu
        public const string MENU_PREVIOUS = "Previous";
        public const string MENU_NEXT = "Next";
        public const string MENU_REFRESH = "Refresh Screen";
        public const string MENU_SORT_BY_NAME = "File Name";
        public const string MENU_SORT_BY_MODIFIED = "Last Modified";
        public const string MENU_SORT_BY_SIZE = "File Size";

        // Preferences Menu
        public const string MENU_PREFERENCES = "Preferences";

        // Help Menu
        public const string MENU_GETTING_STARTED = "Getting Started";
        public const string MENU_HELP = "Help";
        public const string MENU_CHECK_UPDATES = "Check For Updates";
        public const string MENU_REPORT_ISSUE = "Report an issue";
        public const string MENU_ABOUT = "About Skydrm Desktop";

        #endregion // Menu Items


        // Context menu item
        //public const string CONTEXT_MENU_CMD_SHARE = "ContextMenu_Share";
        public const string CONTEXT_MENU_CMD_VIEW_FILE = "ContextMenu_ViewFile";
        public const string CONTEXT_MENU_CMD_VIEW_FILE_INFO = "ContextMenu_ViewFileInfo";
        public const string CONTEXT_MENU_CMD_MAKE_OFFLINE = "ContextMenu_MakeOffline";
        public const string CONTEXT_MENU_CMD_UNMAKE_OFFLINE = "ContextMenu_UnMakeOffline";
        public const string CONTEXT_MENU_CMD_REMOVE = "ContextMenu_Remove";
        public const string CONTEXT_MENU_CMD_OPEN_SKYDRM = "ContextMenu_OpenSkyDRM";
        public const string CONTEXT_MENU_CMD_SAVE_AS = "ContextMenu_SaveAs";
        public const string CONTEXT_MENU_CMD_EDIT = "ContextMenu_Edit";
        public const string CONTEXT_MENU_CMD_EXTRACT_CONTENT = "ContextMenu_ExtractContent";
        public const string CONTEXT_MENU_CMD_MODIFY_RIGHTS = "ContextMenu_ModifyRights";

        public const string CONTEXT_MENU_CMD_SHARE_TO_PERSON = "ContextMenu_ShareToPerson";
        public const string CONTEXT_MENU_CMD_SHARE_TO_PROJECT = "ContextMenu_ShareToProject";
        public const string CONTEXT_MENU_CMD_UPLOAD = "ContextMenu_Upload";
        // TreeView Context menu item
        public const string CONTEXT_MENU_CMD_ADD_FILE = "ContextMenu_AddFile";

        #region //use CultureStringInfo.cs
        // network status
        //public const string NETWORK_CONNECTED = "Connected";//use CultureStringInfo
        //public const string NETWORK_ERROR = "Network Error";
        // line status
        //public const string STATUS_ON_LINE = "ONLINE";
        //public const string STATUS_OFF_LINE = "OFFLINE";
        // Filter local files
        //public const string FILTER_VIEW_ALL = "View All";
        //public const string FILTER_WAITINIG_UPLOAD = "Waiting for Upload";
        //public const string FILTER_ALL_LOCAL_FILES = "All Local Files";
        // ballon tip
        //public const string BALLON_TIP = "SkyDRM for Windows Local Mode";
        #endregion

        // help page
        public const string HELP_PAGE = "https://help.skydrm.com/docs/windows/help/1.0/en-us/home.htm#t=skydrmintro.htm";
       
    }
}
