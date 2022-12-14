using SkydrmLocal.rmc.common.component;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.fileSystem;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.fileSystem.project;
using SkydrmLocal.rmc.helper;
using SkydrmLocal.rmc.ui;
using SkydrmLocal.rmc.ui.windows.mainWindow.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SkydrmLocal.rmc.ui.windows.CustomMessageBoxWindow;

namespace SkydrmLocal.rmc.Edit
{
    public class EditFeature : IEditFeature
    {
        private readonly SkydrmLocalApp app = SkydrmLocalApp.Singleton;
        private readonly IFileRepo currentRepo;
        private readonly EnumCurrentWorkingArea currentWorkingArea;

        // callback after sync when found updated file
        private Action<INxlFile> updatedFileCallback;

        private string NxlFileDiskPath;

        public EditFeature(IFileRepo fileRepo, EnumCurrentWorkingArea area)
        {
            currentRepo = fileRepo;
            currentWorkingArea = area;
        }

        public bool IsEditing { get => FileEditorHelper.IsFileEditing(NxlFileDiskPath); }

        // Common func for main window repo files
        public void EditFromMainWin(INxlFile nxlFile, Action<EditCallBack> onFinishedCallback)
        {
            app.Log.Info("EditFromMainWin -->");
            try
            {
                NxlFileDiskPath = nxlFile.LocalPath;
                currentRepo?.Edit(nxlFile, onFinishedCallback);
            }
            catch (Exception e)
            {
                app.Log.Error(e.ToString());
            }
        }

        // For edit project file by explorer.
        public void EditFromExplorer(IProjectFile projectFile)
        {
            app.Log.Info("EditFromExplorer -->");
            try
            {
                //projectFile.DoEdit((string file)=> {
                //    Console.WriteLine("Edit is finished!");
                //});
            }
            catch (Exception e)
            {
                app.Log.Error(e.ToString());
            }

        }

        public void CheckVersionFromRms(INxlFile nxl, Action<bool> callback)
        {
            // user do edit from "offline" filter.
            if (currentWorkingArea == EnumCurrentWorkingArea.FILTERS_OFFLINE 
                && nxl.FileRepo == EnumFileRepo.REPO_PROJECT)
            {
                currentRepo.SyncDestFile(nxl, (bool bSuccess, INxlFile updatedFile) =>
                {
                    CheckVerNotify(bSuccess, updatedFile, callback);
                }, true);

            }
            else
            {
                currentRepo.SyncDestFile(nxl, (bool bSuccess, INxlFile updatedFile) =>
                {
                    CheckVerNotify(bSuccess, updatedFile, callback);
                }, false);

            }
        }

        private void CheckVerNotify(bool bSuccess, INxlFile updatedFile, Action<bool> callback)
        {
            if (bSuccess && updatedFile != null)
            {
                callback?.Invoke(updatedFile.IsMarkedFileRemoteModified);
            }
            else
            {
                callback?.Invoke(false);
            }
        }

        public void UpdateToRms(INxlFile nxlFile, Action<INxlFile> updated = null)
        {
            updatedFileCallback = updated;

            // Set file status is edited.
            nxlFile.IsEdit = true;

            // add to uploading queue
            UploadManagerEx.GetInstance().AddToWaitingQueue(nxlFile);

            if (NetworkStatus.IsAvailable && SkydrmLocalApp.Singleton.User.StartUpload)
            {
                CheckVersionFromRms(nxlFile, (bool bModified) =>
                {
                    if (bModified) // conflict
                    {
                        HandleIfOverwriteFile(nxlFile);
                    }
                    else
                    {
                        UploadManagerEx.GetInstance().TryToUploadDirectly();
                    }
                });
            }
        }

        /// <summary>
        /// Handle wether sync the file again from rms when conflict occurs.
        /// </summary>
        public void HandleIfSyncFromRms(INxlFile nxlFile, Action<INxlFile> updated = null)
        {
            updatedFileCallback = updated;

            // will popup dialog to prompt user if sync from rms
            if (CustomMessageBoxResult.Positive == Edit.Helper.ShowUpdateDialog(nxlFile.Name))
            {
                UpdateFileFromRms(nxlFile);
            }
            else // Don't sync from rms
            {
                EditFromMainWin(nxlFile, (EditCallBack cb) => {
                    if (cb.IsEdit)
                    {
                        UpdateToRms(nxlFile, updated);
                    }
                });
            }
        }

        // Update modified file to local from rms.
        private void UpdateFileFromRms(INxlFile nxlFile, bool isDoEdit = true)
        {
            // user select sync from rms, should reset this field
            nxlFile.IsMarkedFileRemoteModified = false;

            // user do edit from "offline" filter.
            if (currentWorkingArea == EnumCurrentWorkingArea.FILTERS_OFFLINE
                && nxlFile.FileRepo == EnumFileRepo.REPO_PROJECT)
            {
                currentRepo.SyncDestFile(nxlFile, (bool bSuccess, INxlFile updatedFile) =>
                {
                    InnerUpdate(bSuccess, updatedFile, isDoEdit);
                }, 
                true);
            }
            else
            {
                currentRepo.SyncDestFile(nxlFile, (bool bSuccess, INxlFile updatedFile) =>
                {
                    InnerUpdate(bSuccess, updatedFile, isDoEdit);
                },
                false);
            }
        }

        private void InnerUpdate(bool bSuccess, INxlFile updatedFile, bool isDoEdit = true)
        {
            if (bSuccess && updatedFile != null)
            {
                // update this list item ui about the file node.
                updatedFileCallback?.Invoke(updatedFile);

                // re - download
                ReDownload(updatedFile, isDoEdit);
            }
            else
            {
                app.ShowBalloonTip(CultureStringInfo.EditOfflineFile_Redownload_Failed);
            }
        }

        private void ReDownload(INxlFile updatedFile, bool isDoEdit = true)
        {
            updatedFile.FileStatus = EnumNxlFileStatus.Downloading;
            (updatedFile as NxlDoc).IsMarkedOffline = true; // used to bind ui
            (updatedFile as NxlDoc).IsEdit = false; // will update into db.

            currentRepo.MarkOffline(updatedFile, (bool result) =>
            {
                UpdateStatus((NxlDoc)updatedFile, result);

                if (result)
                {
                    app.ShowBalloonTip(CultureStringInfo.EditOfflineFile_Redownload_Succeed);

                    if (isDoEdit)
                    {
                        EditFromMainWin(updatedFile, (EditCallBack cb) => {
                            if (cb.IsEdit)
                            {
                                // upload
                                UpdateToRms(updatedFile);
                            }
                        });
                    }

                }
                else
                {
                    app.ShowBalloonTip(CultureStringInfo.EditOfflineFile_Redownload_Failed);
                }

            });
        }

        // Handle the complete operation of downloading.
        private void UpdateStatus(NxlDoc doc, bool result)
        {
            // update file status and set source path
            doc.FileStatus = EnumNxlFileStatus.AvailableOffline;
            doc.Location = EnumFileLocation.Local;
            doc.SourcePath = currentRepo.GetSourcePath(doc);
        }


        /// <summary>
        /// Handle wether overwrite when conflict occurs before uploading edited file to rms.
        /// </summary>
        private void HandleIfOverwriteFile(INxlFile nxlFile)
        {
            var result = Edit.Helper.ShowOverwriteDialog(nxlFile.Name);

            // overwrite
            if (CustomMessageBoxResult.Positive == result)
            {
                UploadManagerEx.GetInstance().TryToUploadDirectly();
            }

            // discard & update file to local.
            else if (CustomMessageBoxResult.Negative == result)
            {
                FileHelper.Delete_NoThrow(nxlFile.LocalPath);
                UpdateFileFromRms(nxlFile, false);
            }

            // Click "X" to close
            else
            {
                // means edited file just now is saved into local.
            }
        }


    }
}
