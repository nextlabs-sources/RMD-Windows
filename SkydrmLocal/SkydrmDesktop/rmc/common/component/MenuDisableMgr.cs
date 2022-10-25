using SkydrmLocal.rmc.Edit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.common.component
{
    /// <summary>
    /// The used to control whether disable the "logout" or "Exit" menu when do some special operation.
    /// Currently, the menu should be disbled when following operation is executing: uploading, downloading, protecting, sharing.
    /// </summary>
    class MenuDisableMgr
    {
        private static MenuDisableMgr instance;

        private static readonly object locker = new object();

        private MenuDisableMgr() { }

        public static MenuDisableMgr GetSingleton()
        {
            if (instance == null)
            {
                lock (locker)
                {
                    if (instance == null)
                    {
                        instance = new MenuDisableMgr();
                    }
                }
            }

            return instance;
        }

        // Flag that if user is executing protect.
        public bool IsProtecting { get; set; }

        // Flag that if is executing share.
        public bool IsSharing { get; set; }

        // Flag that if is executing downloading
        public bool IsDownloading { get => DownloadManager.GetSingleton().IsDownloading(); }

        // Flag that if is executing uploading
        public bool IsUploading { get => UploadManagerEx.GetInstance().IsExecuteUploading(); }

        // Menu item disable event
        public event Action<string, bool> MenuItemDisabled;
        public void MenuDisableNotify(string name, bool isDisabled)
        {
            MenuItemDisabled?.Invoke(name, isDisabled);
        }

        // Judge if can enable "logout" or "exit" menu item.
        public bool IsCanEnableMenu()
        {
            return (!IsProtecting) && (!IsSharing) && (!IsDownloading) && (!IsUploading);
        }

        public bool IsAllowLogout()
        {
            return (!IsProtecting) && (!IsSharing) && (!IsDownloading) && (!IsUploading) && !FileEditorHelper.IsbeingFileEdit();
        }

    }
}
