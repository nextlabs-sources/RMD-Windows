using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkydrmDesktop.rmc.ui.windows.fileOperationWindow.model;
using SkydrmLocal.rmc.exception;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.fileSystem.project;

namespace SkydrmDesktop.rmc.ui.windows.fileOperationWindow.operation
{
    class ReShare : IReShare
    {
        private INxlFile NxlFile;
        private List<ProjectData> projectDatas;

        public ReShare(INxlFile nxlFile, List<ProjectData> pDatas)
        {
            NxlFile = nxlFile;
            projectDatas = pDatas;
        }

        public FileAction FileAction => FileAction.ReShare;

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

        public List<ProjectData> ProjectDatas
        {
            get
            {
                ProjectData currentProject = projectDatas.Find(x => x.ProjectInfo.ProjectId == int.Parse(NxlFile.RepoId));
                projectDatas.Remove(currentProject);
                var listOrderAsce= projectDatas.OrderBy(x => x.ProjectInfo.DisplayName);
                var listOrderDesc = listOrderAsce.OrderByDescending(x => x.ProjectInfo.BOwner);
                return listOrderDesc.ToList();
            }
        }

        public bool ReShareFile(List<string> projectIdList, string comment)
        {
            bool result = true;
            try
            {
                NxlFile.Share(projectIdList, comment);
            }
            catch (SkydrmLocal.rmc.sdk.SkydrmException e)
            {
                GeneralHandler.Handle(e, true);
                result = false;
            }
            return result;
        }

        public SkydrmLocal.rmc.sdk.FileRights[] Rights => NxlFile.FileInfo.Rights;

        public string WaterMark => NxlFile.FileInfo.WaterMark;

        public SkydrmLocal.rmc.sdk.Expiration Expiration => NxlFile.FileInfo.Expiration;

        public Dictionary<string, List<string>> Tags => NxlFile.FileInfo.Tags;

        public bool IsMarkOffline => NxlFile.IsMarkedOffline;
    }
}
