using SkydrmLocal.rmc.database2.table.project;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Skydrmlocal.rmc.database2.FunctionProvider;

namespace SkydrmLocal.rmc.database.memcache.project
{
    /// <summary>
    /// This is the callback when invoke UpsertProject success.
    /// </summary>
    /// <param name="id">Project table primary key.</param>
    public delegate void OnUpsertProjectResult(int id);

    /// <summary>
    /// This is the callback when delete project successfully
    /// </summary>
    public delegate void OnDeleteSuccess();

    interface IProjectCache
    {
        #region Project cache lifecyle.
        void OnInitialize();
        void OnDestroy();
        #endregion

        #region Project table.
        List<Project> ListProject();
        bool DeleteProject(int rms_project_id, OnDeleteSuccess callback);
        bool UpsertProject(int project_id, string project_name, string project_display_name,
                                 string project_description, bool isOwner, string tenant_id, OnUpsertProjectResult Callback);
        bool UpsertProjectIsEnabledAdhoc(int project_table_pk, bool isEnabled);
        #endregion

        #region Project file table.
        List<ProjectFile> ListAllProjectFile(int project_table_pk);
        List<ProjectFile> ListProjectFile(int project_table_pk, string path);
        List<ProjectFile> ListProjectOfflineFile(int project_table_pk);
        bool DeleteProjectFolderAndAllSubFiles(int project_table_pk, string rms_path_id);
        bool DeleteProjectFile(int project_table_pk, string rms_file_id);
        bool UpsertProjectFileBatch(InstertProjectFile[] files, Dictionary<int, int> Project_Id2PK);
        bool InsertFakedRoot(int project_table_pk);
        bool UpdateProjectFileOperationStatus(int project_table_pk, int project_file_table_pk, int newStatus);
        bool UpdateProjectFileOfflineMark(int project_table_pk, int project_file_table_pk, bool newMark);
        bool UpdateProjectFileLocalpath(int project_table_pk, int project_file_table_pk, string newPath);
        bool UpdateProjectFileLastModifiedTime(int project_table_pk, int project_file_table_pk, DateTime lastModifiedTime);
        bool UpdateProjectFileFileSize(int project_table_pk, int project_file_table_pk, long filesize);
        bool UpdateProjectFileEditStatus(int project_table_pk, int project_file_table_pk, int newStatus);
        bool UpdateProjectFileModifyRightsStatus(int project_table_pk, int project_file_table_pk, int newStatus);

        // Query project file id (row number)
        int QueryProjectFileId(int project_table_pk, string rms_path_id);
        #endregion

        #region Project local file table.
        List<ProjectLocalFile> ListProjectLocalFiles(int project_table_pk);
        List<ProjectLocalFile> ListProjectLocalFiles(int project_table_pk, string FolderId);

        /// <summary>
        /// Delete specified local file in specified project 
        /// </summary>
        /// <param name="project_table_pk"> field for 'project_table_pk'</param>
        /// <param name="project_local_file_table_pk">field for 'id'</param>
        bool DeleteProjectLocalFile(int project_table_pk, int project_local_file_table_pk);

        /// <summary>
        /// Delete all project local files that uploaded specified project folder(projectFile_RowNumber).
        /// </summary>
        bool DeleteProjectFolderLocalFiles(int project_table_pk, int projectFile_RowNumber);

        bool DeleteProjectAllLocalFiles(int project_table_pk);

        bool AddLocalFileToProject(int project_table_pk, string FolderId, string name, string path, int size, DateTime lastModified);
        string QueryProjectLocalFileRMSParentFolder(int project_table_pk, int projectFile_RowNumber);
        bool UpdateProjectLocalFileOperationStatus(int project_table_pk, int project_local_file_table_pk, int newStatus);
        #endregion

        #region Project classification table.
        string GetProjectClassification(int project_table_pk);
        bool UpdateProjectClassification(int project_table_pk, string classificationJson);
        #endregion
    }
}
