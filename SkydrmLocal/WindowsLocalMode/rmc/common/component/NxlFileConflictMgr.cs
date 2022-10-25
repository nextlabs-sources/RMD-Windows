using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.fileSystem;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.fileSystem.project;
using SkydrmLocal.rmc.ui;
using SkydrmLocal.rmc.ui.windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SkydrmLocal.rmc.ui.windows.CustomMessageBoxWindow;

namespace SkydrmLocal.rmc.common.component
{
    /// <summary>
    /// Used to manage the nxl file if is modified or not(including may content is edited or rights is modified)
    /// </summary>
    public class NxlFileConflictMgr
    {
        private static readonly object locker = new object();
        private static NxlFileConflictMgr instance;

        // Now only project repo file support edit content and modify rights
        // Will judge by RepoType if support other repo later.
        private IFileRepo repo;

        // Callback after sync when found updated file.
        private Action<INxlFile> updatedFileCallback;

        public static NxlFileConflictMgr GetInstance()
        {
            if (instance == null)
            {
                lock (locker)
                {
                    if (instance == null)
                    {
                        instance = new NxlFileConflictMgr();
                    }
                }
            }

            return instance;
        }

        public NxlFileConflictMgr SetFileRepo(IFileRepo fileRepo)
        {
            repo = fileRepo;
            return GetInstance();
        }

        /// <summary>
        /// Check project file if is modified or not when don't know current working repo whether is project repo.
        /// </summary>
        public void CheckFileVersion(INxlFile nxlFile, Action<bool> callback)
        {
            // Now only support project repo file
            if (nxlFile.FileRepo != EnumFileRepo.REPO_PROJECT)
            {
                callback?.Invoke(false);
                return;
            }

            // Have verified the file is modified.
            if (nxlFile.IsMarkedFileRemoteModified)
            {
                callback?.Invoke(nxlFile.IsMarkedFileRemoteModified);
                return;
            }

            // sync from remote
            repo?.SyncDestFile(nxlFile, (bool bSuccess, INxlFile updated) =>
            {
                if (bSuccess && updated != null)
                {
                    callback?.Invoke(updated.IsMarkedFileRemoteModified);
                }
                else
                {
                    callback?.Invoke(false);
                }
            }, 
            true);

        }

        /// <summary>
        /// Check project file if is modified or not when make sure current working repo is project repo.
        /// </summary>
        public void CheckFileVersionKnownCurrentRepo(INxlFile nxlFile, Action<bool> callback)
        {
            // Now only support project repo file
            if (nxlFile.FileRepo != EnumFileRepo.REPO_PROJECT)
            {
                callback?.Invoke(false);
                return;
            }

            // Have verified.
            if (nxlFile.IsMarkedFileRemoteModified)
            {
                callback?.Invoke(nxlFile.IsMarkedFileRemoteModified);
                return;
            }

            repo?.SyncDestFile(nxlFile, (bool bSuccess, INxlFile updated) =>
            {
                if (bSuccess && updated != null)
                {
                    callback?.Invoke(updated.IsMarkedFileRemoteModified);
                }
                else
                {
                    callback?.Invoke(false);
                }
            },
            false);

        }

        /// <summary>
        /// Update the modified file to local from rms.
        /// </summary>
        public void SyncFileNodeFromRms(INxlFile nxlFile, Action<INxlFile> callback)
        {
            this.updatedFileCallback = callback;

            // user select sync from rms, should reset this field
            nxlFile.IsMarkedFileRemoteModified = false;

            repo?.SyncDestFile(nxlFile, (bool bSuccess, INxlFile updated) =>
            {
                if (bSuccess && updated != null)
                {
                    // update this list item ui about the file node.
                    updatedFileCallback?.Invoke(updated);
                }
                else
                {
                    updatedFileCallback?.Invoke(null);
                }

            },
            true);

        }

    }
}
