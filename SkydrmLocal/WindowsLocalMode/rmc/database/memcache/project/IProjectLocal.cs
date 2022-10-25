using SkydrmLocal.rmc.database2.table.project;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Skydrmlocal.rmc.database2.FunctionProvider;

namespace SkydrmLocal.rmc.database.memcache.project
{
    interface IProjectLocal
    {
        #region Project table.
        List<Project> ListProject();
        bool DeleteProject(int rms_project_id);
        int UpsertProject(int project_id, string project_name, string project_display_name,
                                 string project_description, bool isOwner, string tenant_id);
        bool UpsertProjectIsEnabledAdhoc(int project_table_pk, bool isEnabled);
        #endregion // Project table

        #region Project file table.
        List<ProjectFile> ListAllProjectFile(int project_table_pk);
        List<ProjectFile> ListProjectFile(int project_table_pk, string path);
        List<ProjectFile> ListProjectOfflineFile(int project_table_pk);
        bool DeleteProjectFolderAndAllSubFiles(int project_table_pk, string rms_path_id);
        bool DeleteProjectFile(int project_table_pk, string rms_file_id);
        bool UpsertProjectFileBatch(InstertProjectFile[] files, Dictionary<int, int> Project_Id2PK);
        bool InsertFakedRoot(int project_table_pk);
        bool UpdateProjectFileOperationStatus(int project_file_table_pk, int newStatus);
        bool UpdateProjectFileOfflineMark(int project_file_table_pk, bool newMark);
        bool UpdateProjectFileLocalpath(int project_file_table_pk, string newPath);
        bool UpdateProjectFileLastModifiedTime(int project_file_table_pk, DateTime lastModifiedTime);
        bool UpdateProjectFileFileSize(int project_file_table_pk, long filesize);
        bool UpdateProjectFileEditStatus(int project_file_table_pk, int newStatus);
        bool UpdateProjectFileModifyRightsStatus(int project_file_table_pk, int newStatus);

        // Query project file id (row number)
        int QueryProjectFileId(int project_table_pk, string rms_path_id);
        #endregion // Project file table.

        #region Project local file table.
        List<ProjectLocalFile> ListProjectLocalFiles(int project_table_pk);
        List<ProjectLocalFile> ListProjectLocalFiles(int project_table_pk, string FolderId);
        bool DeleteProjectLocalFile(int project_local_file_table_pk);
        int AddLocalFileToProject(int project_table_pk, string FolderId, string name, string path, int size, DateTime lastModified);
        string QueryProjectLocalFileRMSParentFolder(int project_table_pk, int projectFile_RowNumber);
        bool UpdateProjectLocalFileOperationStatus(int project_local_file_table_pk, int newStatus);
        #endregion // Project local file table.

        #region Project classification table.
        string GetProjectClassification(int project_table_pk);
        bool UpdateProjectClassification(int project_table_pk, string classificationJson);
        #endregion // Project classification table.
    }
}
