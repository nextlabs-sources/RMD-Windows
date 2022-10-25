using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.modifyRights;
using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.shareNxlFeature;
using SkydrmLocal.rmc.ui.pages.model;
using SkydrmLocal.rmc.ui.windows.mainWindow.model;

namespace SkydrmLocal.rmc.ui.pages.model
{
    public class ProtectAndShareConfig
    {
        #region parameter
        private object winTag;
        private FileOperation fileOperation;
        private RightsSelectConfig rightsSelectConfig;
        private SharedWithConfig sharedWithConfig;
        private bool localDriveIsChecked = false;
        private UserSelectTags userSelectTags;
        private bool isProtectToProject;
        private bool centralPolicyRadioIsChecked;
        //Display for UI when create a protected file with central policy successfully.
        private Dictionary<string, List<string>> tags;
        // For share nxlFile
        private int projectId;
        private bool isAdHoc;
        private IShareNxlFeature shareNxlFeature;
        // For modify rights
        private IModifyRights modifyRightsFeature;

        public List<INxlFile> CreatedFiles { get; set; }
        
        //for PageSelectDocumentClassify
        public IMyProject myProject;

        public string SelectProjectFolderPath { get; set; }

        // for systemProject
        public ISystemProject sProject;
        #endregion

        public object WinTag
        {
            get { return winTag; }
            set { winTag = value; }
        }

        public FileOperation FileOperation
        {
            get { return fileOperation; }
            set { fileOperation = value; }
        }
        public RightsSelectConfig RightsSelectConfig
        {
            get { return rightsSelectConfig; }
            set { rightsSelectConfig = value; }
        }
        public SharedWithConfig SharedWithConfig
        {
            get { return sharedWithConfig; }
            set { sharedWithConfig = value; }
        }

        public bool LocalDriveIsChecked
        {
            get => localDriveIsChecked;
            set => localDriveIsChecked = value;
        }

        public UserSelectTags UserSelectTags
        {
            get { return userSelectTags; }
            set { userSelectTags = value; }
        }

        public bool IsProtectToProject
        {
            get { return isProtectToProject; }
            set { isProtectToProject = value; }
        }

        public bool CentralPolicyRadioIsChecked
        {
            get { return centralPolicyRadioIsChecked; }
            set { centralPolicyRadioIsChecked = value; }
        }

        public Dictionary<string, List<string>> Tags
        {
            get => tags;
            set { tags = value; }
        }

        public bool IsAdHoc
        {
            get => isAdHoc;
            set => isAdHoc = value;
        }
        public int ProjectId
        {
            get => projectId;
            set => projectId = value;
        }
        public IShareNxlFeature ShareNxlFeature
        {
            get => shareNxlFeature;
            set => shareNxlFeature = value;
        }
        public IModifyRights ModifyRightsFeature
        {
            get => modifyRightsFeature;
            set => modifyRightsFeature = value;
        }

    }
}
