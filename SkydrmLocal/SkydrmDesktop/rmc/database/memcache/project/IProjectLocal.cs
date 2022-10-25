using SkydrmDesktop.rmc.database.table.project;
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
        /// <summary>
        /// The operation for 'project table' cache
        /// </summary>
        #region Project table.

        List<Project> ListProject();
        bool DeleteProject(int rms_project_id);
        int UpsertProject(int project_id, string project_name, string project_display_name,
                                 string project_description, bool isOwner, string tenant_id);

        // IsEnableAdhoc
        bool UpsertProjectIsEnabledAdhoc(int project_table_pk, bool isEnabled);
        // Project classification
        string GetProjectClassification(int project_table_pk);
        bool UpdateProjectClassification(int project_table_pk, string classificationJson);

        #endregion // Project table


        /// <summary>
        /// The operation for 'project file table' cache
        /// </summary>
        #region Project file table.
        List<ProjectFile> ListAllProjectFile(int project_table_pk);
        List<ProjectFile> ListProjectFile(int project_table_pk, string path);
        List<ProjectFile> ListProjectOfflineFile(int project_table_pk);

        bool DeleteProjectFolderAndAllSubFiles(int project_table_pk, string rms_path_id);
        bool DeleteProjectFile(int project_table_pk, string rms_file_id);

        bool UpsertProjectFileBatch(InstertProjectFileEx[] files, Dictionary<int, int> Project_Id2PK);
        bool InsertFakedRoot(int project_table_pk);

        // File status
        bool UpdateProjectFileOperationStatus(int project_file_table_pk, int newStatus);
        // Offline mark
        bool UpdateProjectFileOfflineMark(int project_file_table_pk, bool newMark);
        // Local path
        bool UpdateProjectFileLocalpath(int project_file_table_pk, string newPath);
        // Last modified
        bool UpdateProjectFileLastModifiedTime(int project_file_table_pk, DateTime lastModifiedTime);
        // File size
        bool UpdateProjectFileFileSize(int project_file_table_pk, long filesize);
        // Edit status
        bool UpdateProjectFileEditStatus(int project_file_table_pk, int newStatus);
        // Modified rights status
        bool UpdateProjectFileModifyRightsStatus(int project_file_table_pk, int newStatus);

        bool UpdateProjectFileWhenOverwriteInLeaveCopy(int project_file_table_pk, string duid,
           int Status, long size, DateTime lastModifed);

        //-- For sharing transaction.
        // IsShared
        bool UpdateProjectFileIsShared(int project_file_table_pk, int newValue);
        // IsRevoked
        bool UpdateProjectFileIsRevoked(int project_file_table_pk, int newValue);
        // SharedWith
        bool UpdateProjectFileSharedWith(int project_file_table_pk, List<uint> newList);

        // Project file id (row number)
        int QueryProjectFileId(int project_table_pk, string rms_path_id);

        #endregion // Project file table.


        /// <summary>
        /// The operation for 'project local file table' cache
        /// </summary>
        #region Project local file table.
        List<ProjectLocalFile> ListProjectLocalFiles(int project_table_pk);
        List<ProjectLocalFile> ListProjectLocalFiles(int project_table_pk, string FolderId);
        bool DeleteProjectLocalFile(int project_local_file_table_pk);

        int AddLocalFileToProject(int project_table_pk, string FolderId, string name, string path, int size, DateTime lastModified, string reserved1);
        string QueryProjectLocalFileRMSParentFolder(int project_table_pk, int projectFile_RowNumber);
        // Status
        bool UpdateProjectLocalFileOperationStatus(int project_local_file_table_pk, int newStatus);

        bool UpdateProjectLocalFileName(int project_local_file_table_pk, string newName);
        bool UpdateProjectLocalFileLocalPath(int project_local_file_table_pk, string localPath);
        bool UpdateProjectLocalFileReserved1(int project_local_file_table_pk, string reserved1);
        #endregion // Project local file table.


        /// <summary>
        /// The operation for 'Shared with project file table' cache
        /// </summary>
        #region Shared with project file table
        List<SharedWithProjectFile> ListSharedWithProjectFile(int project_table_pk);
        List<SharedWithProjectFile> ListSharedWithProjectOfflineFile(int project_table_pk);
        bool UpsertSharedWithProjectFileBatch(InstertSharedWithProjectFile[] files, Dictionary<int, int> Project_Id2PK);

        // Delete file
        bool DeleteSharedWithProjectFile(string duid);

        // File status
        bool UpdateSharedWithProjectFileStatus(int shared_with_project_file_table_pk, int newStatus);
        // Offline mark
        bool UpdateSharedWithProjectFileOfflineMark(int shared_with_project_file_table_pk, bool newMark);
        // Local path
        bool UpdateSharedWithProjectFileLocalpath(int shared_with_project_file_table_pk, string newPath);

        #endregion // Shared with project file table

    }
}
