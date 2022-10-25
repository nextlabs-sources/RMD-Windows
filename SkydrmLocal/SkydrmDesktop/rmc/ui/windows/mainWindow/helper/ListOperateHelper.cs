using SkydrmLocal.rmc.fileSystem.basemodel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.ui.windows.mainWindow.helper
{
    class ListOperateHelper
    {
        /// <summary>
        /// Use file name and DateModified time to remove
        /// </summary>
        /// <param name="listfiles"></param>
        /// <param name="toRemoveFile"></param>
        /// <returns></returns>
        public static bool RemoveListFileByDateTime(IList<INxlFile> listfiles, INxlFile toRemoveFile)
        {
            bool result = false;
            int index = -1;
            for (int i = 0; i < listfiles.Count; i++)
            {
                if (listfiles[i].Name.Equals(toRemoveFile.Name)
                    && listfiles[i].DateModified.Equals(toRemoveFile.DateModified))
                {
                    index = i;
                    break;
                }
            }

            if (index != -1)
            {
                listfiles.RemoveAt(index);
                result = true;
            }
            return result;
        }

        public static void RemoveListFile(IList<INxlFile> listfiles, IList<INxlFile> copyfiles, INxlFile toRemoveFile)
        {
            INxlFile toFind = null;
            foreach (var one in listfiles)
            {
                if (one.Equals(toRemoveFile))
                {
                    toFind = one;
                    break;
                }
            }

            if (toFind != null)
            {
                listfiles.Remove(toFind);
                copyfiles.Remove(toFind);
            }
        }

        /// <summary>
        /// Used to update the specified file status of current working listview. 
        ///   -- Fix bug 51388, because there are may different file node objects when re-get from local db 
        ///   since the refresh which caused by switch treeview item during downloading.
        /// </summary>
        public static void UpdateListViewFileStatusForMarkOffline(IList<INxlFile> listfiles, IList<INxlFile> copyfiles, INxlFile specified)
        {
            foreach (var one in listfiles)
            {
                if (one.Equals(specified)
                    && one.IsCreatedLocal == false) // fix bug 62976
                {
                    one.FileStatus = specified.FileStatus;
                    one.Location = specified.Location;
                    break;
                }
            }

            foreach (var one in copyfiles)
            {
                if (one.Equals(specified)
                    && one.IsCreatedLocal == false)
                {
                    one.FileStatus = specified.FileStatus;
                    one.Location = specified.Location;
                    break;
                }
            }

        }

        // Need perform special merge for "leave a copy" file.
        public static void MergeLeaveAcopyFile(IList<INxlFile> syncResults,
            IList<INxlFile> nxlFileList,
            IList<INxlFile> copyFileList)
        {
            // 
            // Fix bug that handle project "leave a copy" file delete issue, sometimes need to delete twice.
            // Need update the listView "leave copy file" with new node after uploading and sync.
            //

            // Record the updated node index, <OldIndex, NewIndex> 
            Dictionary<int, int> IndexMap = new Dictionary<int, int>();
            for (int i = 0; i < syncResults.Count; i++)
            {
                int newIndex = -1;
                int oldIndex = -1;

                for (int j = 0; j < nxlFileList.Count; j++)
                {
                    if (syncResults[i].Equals(nxlFileList[j])
                        && syncResults[i].FileStatus == EnumNxlFileStatus.CachedFile
                        && nxlFileList[j].FileStatus == EnumNxlFileStatus.Uploading
                        && syncResults[i].IsCreatedLocal == false
                        && nxlFileList[j].IsCreatedLocal == true)
                    {
                        newIndex = i;
                        oldIndex = j;
                        break;
                    }
                }

                if (newIndex != -1 && oldIndex != -1)
                {
                    IndexMap.Add(oldIndex, newIndex);
                }

            }

            // Handle the case with the same file overwrite in "leave a copy" model,
            // and the listView should only display the latest one (should remove the old one) by comparing dateModifed.
            List<int> toReplaceOriginalSameNameFileIndex = new List<int>();
            for (int i = 0; i < syncResults.Count; i++)
            {
                for (int j = 0; j < nxlFileList.Count; j++)
                {
                    // "syncResults[i]" is original remote file node in local(the synced new overwrite file actually don't updated into db yet)
                    // and "nxlFileList[j]" is newly protected file in "leave a copy" model (PendingUploadFile).
                    if (syncResults[i].Equals(nxlFileList[j])
                        && (syncResults[i].RawDateModified < nxlFileList[j].RawDateModified || IsConditioinMatchedForSharedWorkspace(syncResults[i],nxlFileList[j])))
                    {
                        toReplaceOriginalSameNameFileIndex.Add(j);
                    }
                }
            }

            // 1. Remove the old node(new node have already added) if user overwrite the same name file.
            foreach (int i in toReplaceOriginalSameNameFileIndex)
            {
                if (i >= 0 && i < nxlFileList.Count)
                {
                    nxlFileList.RemoveAt(i);
                    copyFileList.RemoveAt(i);
                }
            }

            // 2. Replace the old node using the new node.
            foreach (var one in IndexMap)
            {
                if(one.Key < nxlFileList.Count && one.Value < syncResults.Count
                   && syncResults[one.Value].Name == nxlFileList[one.Key].Name /* fix bug 63666 */)
                {
                    nxlFileList[one.Key] = syncResults[one.Value];
                    copyFileList[one.Key] = syncResults[one.Value];
                }
            }

        }

        // Fix bug 64252
        private static bool IsConditioinMatchedForSharedWorkspace(INxlFile synFile, INxlFile listFile)
        {
            return synFile.FileRepo == EnumFileRepo.REPO_EXTERNAL_DRIVE
                    && listFile.FileRepo == EnumFileRepo.REPO_EXTERNAL_DRIVE 
                    && synFile.RawDateModified > listFile.RawDateModified;
        }


        /// <summary>
        /// Merge listview ui nodes after sync from rms.
        /// </summary>
        public static void MergeListView(
            IList<INxlFile> newfiles,
            IList<INxlFile> oldfiles,
            IList<INxlFile> oldCopyfiles)
        {
            for (int i = oldfiles.Count - 1; i >= 0; i--)
            {
                INxlFile one = oldfiles[i];

                INxlFile find = null;
                for (int j = 0; j < newfiles.Count; j++)
                {
                    INxlFile f = newfiles[j];
                    if (one.Equals(f)                 
                        // For fix bug 63300 that file list not auto-refresh after the same name overwrite.
                        && one.DateModified == f.DateModified)
                    {
                        // "one" belongs to old set and belongs to new set
                        find = f;
                        break;
                    }
                }

                // "one" belongs to old set but not belongs to new set -- should remove it from old set.
                // should exclude local created file
                // If local file status is waiting upload | uploading we should keep it in the file list.
                if (find == null
                    && one.FileStatus != EnumNxlFileStatus.WaitingUpload
                    && one.FileStatus != EnumNxlFileStatus.Uploading
                    && one.FileStatus != EnumNxlFileStatus.UploadFailed)
                {
                    oldfiles.Remove(one);
                    oldCopyfiles.Remove(one);
                }
            }


            for (int j = 0; j < newfiles.Count; j++)
            {
                INxlFile one = newfiles[j];

                INxlFile find = null;
                for (int i = 0; i < oldfiles.Count; i++)
                {
                    INxlFile f = oldfiles[i];
                    if (one.Equals(f)
                        && one.DateModified == f.DateModified /* fix bug 63300 */)
                    {
                        // "one" belongs to new set and belongs to old set
                        find = f;
                        break;
                    }
                }

                // "one" belongs to new set but not belongs to old set -- should add it 
                if (find == null)
                {
                    oldfiles.Add(one);
                    oldCopyfiles.Add(one);
                }
            }
        }

        /// <summary>
        /// Merge listview ui nodes after sync from rms, and record the added & removed files.
        /// </summary>
        public static void MergeListView(
            IList<INxlFile> newfiles,
            IList<INxlFile> oldfiles,
            IList<INxlFile> oldCopyfiles,
            out IList<INxlFile> addFiles,
            out IList<INxlFile> removeFiles)
        {
            addFiles = new List<INxlFile>();
            removeFiles = new List<INxlFile>();

            for (int i = oldfiles.Count - 1; i >= 0; i--)
            {
                INxlFile one = oldfiles[i];

                INxlFile find = null;
                for (int j = 0; j < newfiles.Count; j++)
                {
                    INxlFile f = newfiles[j];
                    if (one.Equals(f) && one.RawDateModified == f.RawDateModified)
                    {
                        // "one" belongs to old set and belongs to new set
                        find = f;
                        break;
                    }
                }

                // "one" belongs to old set but not belongs to new set -- should remove it from old set.
                // should exclude local created file
                // If local file status is waiting upload | uploading we should keep it in the file list.
                if (find == null
                    && one.FileStatus != EnumNxlFileStatus.WaitingUpload
                    && one.FileStatus != EnumNxlFileStatus.Uploading
                    && one.FileStatus != EnumNxlFileStatus.UploadFailed)
                {
                    oldfiles.Remove(one);
                    oldCopyfiles.Remove(one);

                    removeFiles.Add(one);
                }
            }


            for (int j = 0; j < newfiles.Count; j++)
            {
                INxlFile one = newfiles[j];

                INxlFile find = null;
                for (int i = 0; i < oldfiles.Count; i++)
                {
                    INxlFile f = oldfiles[i];
                    if (one.Equals(f) && one.RawDateModified == f.RawDateModified)
                    {
                        // "one" belongs to new set and belongs to old set
                        find = f;
                        break;
                    }
                }

                // "one" belongs to new set but not belongs to old set -- should add it 
                if (find == null)
                {
                    oldfiles.Add(one);
                    oldCopyfiles.Add(one);

                    addFiles.Add(one);
                }
            }

        }
    }
}
