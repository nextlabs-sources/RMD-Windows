
using SkydrmDesktop.rmc.common.helper;
using SkydrmDesktop.rmc.featureProvider.externalDrive.RmsRepoMgr;
using SkydrmDesktop.rmc.fileSystem.externalDrive;
using SkydrmDesktop.rmc.fileSystem.sharedWorkspace;
using SkydrmDesktop.rmc.fileSystem.utils;
using SkydrmDesktop.rmc.ui.windows.mainWindow.model;
using SkydrmLocal.rmc.fileSystem;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.fileSystem.project;
using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.ui.windows.mainWindow.treeViewComponent.viewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.ui.windows.mainWindow.viewModel
{
    // Now only for test.
    public delegate void OnSyncRmsRepoComplete(bool bSucceed, IList<IRmsRepo> results);

    public class ViewModelHelper
    {
        public static RootViewModel FindRootViewModel(TreeViewItemViewModel fvm)
        {
            var parent = fvm.Parent;
            if( (parent is FolderViewModel) || (parent is ProjectViewModel))
            {
                return FindRootViewModel(parent);
            }
            else if(parent is RootViewModel)
            {
                return parent as RootViewModel;
            }

            // will never reach this, or else occur error.
            return null;
        }

        // Find the specified project by user selected folder item(FolderViewModel).
        public static ProjectData FindProject(FolderViewModel folder)
        {
            TreeViewItemViewModel parent = folder.Parent;
            if (parent is FolderViewModel)
            {
                return FindProject(parent as FolderViewModel);
            }
            else if(parent is ProjectViewModel)
            {
                return (parent as ProjectViewModel).Project;
            }

            // will never reach this, or else occur error.
            return null;
        }

        // Mainly used for external repository root view model.
        public static RootViewModel FindRootViewModel(FolderViewModel fvm)
        {
            TreeViewItemViewModel parent = fvm.Parent;
            if(parent is FolderViewModel)
            {
                return FindRootViewModel(parent as FolderViewModel);
            }
            else if(parent is RootViewModel)
            {
                return parent as RootViewModel;
            }

            return null;
        }

        public static void  AsyncRmsRepo(OnSyncRmsRepoComplete callback)
        {
            // Async worker
            Func<object> asyncTask = new Func<object>(() => {

                bool bSucceed = true;
                List<IRmsRepo> ret = new List<IRmsRepo>();
                try
                {
                   ret = SkydrmApp.Singleton.RmsRepoMgr.SyncRepositories();
                }
                catch (Exception e)
                {
                    bSucceed = false;
                }

                return new RefreshRepositoriesInfo(bSucceed, ret);
            });

            // Async callback
            Action<object> cb = new Action<object>((rt) => {
                RefreshRepositoriesInfo rtValue = (RefreshRepositoriesInfo)rt;
                callback?.Invoke(rtValue.IsSuc, rtValue.Results);
            });

            // Invoke
            AsyncHelper.RunAsync(asyncTask, cb);

        }

        public static bool IsExternalRepoLoading(ObservableCollection<IFileRepo> repos)
        {
            bool ret = false;
            foreach(var one in repos)
            {
                if(one is ExternalRepo)
                {
                    var repo = one as ExternalRepo;
                    if (repo.IsLoading)
                    {
                        ret = true;
                        break;
                    }
                } else if(one is SharedWorkspaceRepo)
                {
                    var repo = one as SharedWorkspaceRepo;
                    if (repo.IsLoading)
                    {
                        ret = true;
                        break;
                    }
                }
            }
            return ret;
        }

        public static bool IsExternalRepo(string type)
        {
            return type.Equals(FileSysConstant.DROPBOX, StringComparison.CurrentCultureIgnoreCase)
                || type.Equals(FileSysConstant.ONEDRIVE, StringComparison.CurrentCultureIgnoreCase)
                || type.Equals(FileSysConstant.GOOGLE_DRIVE, StringComparison.CurrentCultureIgnoreCase)
                || type.Equals(FileSysConstant.BOX, StringComparison.CurrentCultureIgnoreCase)
                || type.Equals(FileSysConstant.SHAREPOINT, StringComparison.CurrentCultureIgnoreCase)
                || type.Equals(FileSysConstant.SHAREPOINT_ONLINE, StringComparison.CurrentCultureIgnoreCase)
                || type.Equals(FileSysConstant.SHAREPOINT_ONPREMISE, StringComparison.CurrentCultureIgnoreCase);            
        }

        public static IFileRepo GetExternalRepo(string id, ObservableCollection<IFileRepo> externalRepos)
        {
            foreach (var one in externalRepos)
            {
                if (one.RepoId.Equals(id))
                {
                    return one;
                }
            }
            return null;
        }

        public static bool IsNeedCheckVersion(INxlFile nxlFile)
        {
            return nxlFile.FileRepo == EnumFileRepo.REPO_PROJECT
                || nxlFile.FileRepo == EnumFileRepo.REPO_WORKSPACE
                || nxlFile.FileRepo == EnumFileRepo.REPO_MYVAULT  // // for overwrite
                || nxlFile.FileRepo == EnumFileRepo.REPO_MYDRIVE // for overwrite
                || nxlFile.FileRepo == EnumFileRepo.REPO_EXTERNAL_DRIVE; 
        }

        public static NxlFileConflictType CheckConflict(INxlFile nxlFile)
        {
            Dictionary<string, List<string>> localPathFPtags = new Dictionary<string, List<string>>();
            Dictionary<string, List<string>> partialPathFPtags = new Dictionary<string, List<string>>();

            try
            {
                // old
                var fpOld = SkydrmApp.Singleton.Rmsdk.User.GetNxlFileFingerPrint(nxlFile.LocalPath);
                localPathFPtags = fpOld.tags;

                // new
                var fpNew = SkydrmApp.Singleton.Rmsdk.User.GetNxlFileFingerPrint(nxlFile.PartialLocalPath);
                partialPathFPtags = fpNew.tags;

                if(fpOld.duid != fpNew.duid)
                {
                    return NxlFileConflictType.FILE_IS_OVERWROTE;
                }
                else
                {
                    if(fpOld.isByCentrolPolicy && fpNew.isByCentrolPolicy && !NxlFileFingerPrint.IsSameTags(localPathFPtags, partialPathFPtags))
                    {
                        return NxlFileConflictType.FILE_IS_MODIFIED_RIGHTS;
                    } else
                    {
                        return NxlFileConflictType.FILE_IS_EDITED;
                    }
                }
            }
            catch (Exception e)
            {
                SkydrmApp.Singleton.Log.Error("Error in IsModifiedRights->GetNxlFileFingerPrint(nxlFile.PartialLocalPath):", e);
            }

            return NxlFileConflictType.UNKNOWN;
        }

    }

    // Record the local marked offline file's conflict type with the remote one.
    public enum NxlFileConflictType
    {
        UNKNOWN = 0,
        FILE_IS_MODIFIED_RIGHTS,
        FILE_IS_EDITED,
        FILE_IS_OVERWROTE
    }

}
