using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkydrmDesktop.Resources.languages;
using SkydrmDesktop.rmc.ui.windows.fileOperationWindow.model;
using SkydrmLocal.rmc.exception;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.fileSystem.project;
using SkydrmLocal.rmc.sdk;

namespace SkydrmDesktop.rmc.ui.windows.fileOperationWindow.operation
{
    class ReShareUpdate : IReShareUpdate
    {
        private INxlFile NxlFile;
        private List<SkydrmLocal.rmc.fileSystem.project.ProjectData> projectDatas;
        private List<int> sharedWithProject;

        public ReShareUpdate(INxlFile nxlFile, List<SkydrmLocal.rmc.fileSystem.project.ProjectData> pDatas, List<int> sharedWithProjectId)
        {
            NxlFile = nxlFile;
            projectDatas = pDatas;
            sharedWithProject = sharedWithProjectId;
        }

        public FileAction FileAction => FileAction.ReShareUpdate;

        public OperateFileInfo FileInfo
        {
            get
            {
                string[] filePath = new string[1] { NxlFile.PartialLocalPath };
                string[] fileName = new string[1] { NxlFile.Name };
                return new OperateFileInfo(filePath, fileName);
            }
            set => throw new NotImplementedException();
        }

        public NxlFileType NxlType => NxlFile.FileInfo.IsByAdHoc ? NxlFileType.Adhoc : NxlFileType.CentralPolicy;

        public List<SkydrmLocal.rmc.fileSystem.project.ProjectData> ProjectDatas
        {
            get
            {
                SkydrmLocal.rmc.fileSystem.project.ProjectData currentProject = projectDatas.Find(x => x.ProjectInfo.ProjectId == int.Parse(NxlFile.RepoId));
                projectDatas.Remove(currentProject);
                var listOrderAsce = projectDatas.OrderBy(x => x.ProjectInfo.DisplayName);
                var listOrderDesc = listOrderAsce.OrderByDescending(x => x.ProjectInfo.BOwner);
                return listOrderDesc.ToList();
            }
        }

        public FileRights[] Rights => NxlFile.FileInfo.Rights;

        public string WaterMark => NxlFile.FileInfo.WaterMark;

        public Expiration Expiration => NxlFile.FileInfo.Expiration;

        public Dictionary<string, List<string>> Tags => NxlFile.FileInfo.Tags;

        public bool IsMarkOffline => NxlFile.IsMarkedOffline;

        public bool IsAdmin => NxlFile.FileInfo.HasAdminRights;

        public List<int> SharedWithProject => sharedWithProject;

        public bool ReShareFile(List<string> projectIdList, string comment)
        {
            throw new NotImplementedException();
        }

        public bool ReShareUpdateFile(List<string> addProjectIdList, List<string> removedProjectIdList, string comment)
        {
            bool result = true;
            try
            {
                NxlFile.UpdateRecipients(addProjectIdList, removedProjectIdList, comment);
            }
            catch (SkydrmLocal.rmc.sdk.SkydrmException e)
            {
                GeneralHandler.Handle(e, true);
                result = false;
            }
            return result;
        }
        public bool RevokeSharing()
        {
            bool result = false;
            result = NxlFile.Revoke();
            if (!result)
            {
                SkydrmApp.Singleton.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("ReShareUpOperation_Win_RevokeFailed"), 
                    false, NxlFile.Name, "Revoke sharing");
            }
            return result;
        }
    }
}
