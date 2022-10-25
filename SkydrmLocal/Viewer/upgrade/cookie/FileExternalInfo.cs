using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.upgrade.cookie
{
    public class FileExternalInfo
    {
        public string FileRepo;
        public string FileStatus;
        public bool IsEdit;  // File if is edited in local or not.
        public bool IsClickFromSkydrmDesktop;
        public string RepoId;
        public string DisplayPath;
        public string[] emails;
        // public string FilePath;
        // public string Intent;
        //public string RmsRepoId = string.Empty;
        //public string TransactionId = string.Empty;
        //public string TransactionCode = string.Empty;
        //public string RmsPathId = string.Empty;
        //public string RmsDisplayPath = string.Empty;
        //public string RmsSharedWith = string.Empty;
        //public string Shared_by = string.Empty;
        //public int ProjectId = -1;
        //public bool IsClickFromRmdDesktop = false;
        //public bool IsRmdDesktopAllowEdit = false;
        //public bool IsRmdDesktopAllowShare = false;
    }
}
