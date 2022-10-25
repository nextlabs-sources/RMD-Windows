using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkydrmDesktop.rmc.featureProvider.externalDrive;
using SkydrmLocal.rmc.fileSystem;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.ui.utils;

namespace SkydrmDesktop.rmc.fileSystem.externalRepo
{
    class SharePointOnPremiseRepo : AbstractFileRepo
    {
        private IExternalDrive drive;

        public SharePointOnPremiseRepo(IExternalDrive ed)
        {
            this.drive = ed;
        }

        public override string RepoDisplayName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override string RepoType => throw new NotImplementedException();

        public override void DownloadFile(INxlFile nxl, bool isViewOnly, OnDownloadComplete callback, bool isDownloadPartial = false, bool isOnlineView = false)
        {
            throw new NotImplementedException();
        }

        public override IList<INxlFile> GetFilePool()
        {
            throw new NotImplementedException();
        }

        public override IList<INxlFile> GetWorkingFolderFilesFromDB()
        {
            throw new NotImplementedException();
        }

        public override IList<INxlFile> GetOfflines()
        {
            throw new NotImplementedException();
        }

        public override void SyncFiles(OnRefreshComplete results, string itemFlag = null)
        {
            throw new NotImplementedException();
        }

    }
}
