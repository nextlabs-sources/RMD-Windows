using SkydrmDesktop;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.fileSystem.basemodel;
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
        private readonly SkydrmApp app = SkydrmApp.Singleton;
        private static readonly object locker = new object();
        private static NxlFileConflictMgr instance;

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


        /// <summary>
        /// Check project\workspace file if is modified or not when don't know current working repo.
        /// </summary>
        public void CheckFileVersion(INxlFile nxlFile, Action<bool> callback)
        {
            // Have verified the file is modified.
            if (nxlFile.IsMarkedFileRemoteModified)
            {
                callback?.Invoke(nxlFile.IsMarkedFileRemoteModified);
                return;
            }

            // sync from remote
            var repo = app.MainWin.viewModel.GetRepoByNxlFile(nxlFile);
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
        /// Update the modified file to local from rms.
        /// </summary>
        public void SyncFileNodeFromRms(INxlFile nxlFile, Action<INxlFile> callback)
        {
            this.updatedFileCallback = callback;

            // user select sync from rms, should reset this field
            nxlFile.IsMarkedFileRemoteModified = false;

            var repo = app.MainWin.viewModel.GetRepoByNxlFile(nxlFile);
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
