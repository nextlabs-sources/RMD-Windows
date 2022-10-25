using CustomControls;
using CustomControls.components;
using SkydrmDesktop.Resources.languages;
using SkydrmDesktop.rmc.ui.windows.fileOperationWindow.operation;
using SkydrmDesktop.rmc.ui.windows.fileOperationWindow.view;
using SkydrmDesktop.rmc.ui.windows.mainWindow.model;
using SkydrmLocal.rmc.common.component;
using SkydrmLocal.rmc.ui.utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SkydrmDesktop.rmc.ui.windows.fileOperationWindow.viewModel
{
    public class SpecialProtectViewModel : BaseViewModel
    {
        private readonly SkydrmApp App = SkydrmApp.Singleton;
        private OrdinaryWin fileDestSelectWindow = new OrdinaryWin();
        private readonly FileRightsPreviewPage fileRightsPreviewPage = new FileRightsPreviewPage();
        private readonly FailedPage failedPage = new FailedPage();

        private readonly BackgroundWorker DoProtect_BgWorker = new BackgroundWorker();

        // use for optimize page load
        private bool HasInitedFileDestPageVM { get; set; }
        private bool HasInitedFileRightsPageVM { get; set; }
        private bool HasInitedFileRightsPreviewPageVM { get; set; }

        private ISpecialProtect ProtectOpert { get; }

        // for preview tag rights wartemark
        private string tagPreivewWarterMark { get; set; } = string.Empty;
        // use for notify MainWindow
        private List<SkydrmLocal.rmc.fileSystem.basemodel.INxlFile> createdFiles = new List<SkydrmLocal.rmc.fileSystem.basemodel.INxlFile>();

        public SpecialProtectViewModel(ISpecialProtect operation, FileOperationWin win) : base(win)
        {
            this.ProtectOpert = operation;

            InitFRPreviewPageViewModel();
            InitFRPreviewPageCommand();
            Host.frm.Content = fileRightsPreviewPage;

            DoProtect_BgWorker.DoWork += DoProtect_BgWorker_DoWork;
            DoProtect_BgWorker.RunWorkerCompleted += DoProtect_BgWorker_RunWorkerCompleted;
        }

        #region Init FileRightsPreviewPage
        private void InitFRPreviewPageViewModel()
        {
            // caption
            int fileCount = ProtectOpert.FileInfo.FileName.Length;
            fileRightsPreviewPage.ViewMode.CaptionDescVisible = System.Windows.Visibility.Collapsed;
            fileRightsPreviewPage.ViewMode.Caption4VM.Title = CultureStringInfo.ApplicationFindResource("ProtectOperation_Win_Title");
            if (fileCount > 1)
            {
                fileRightsPreviewPage.ViewMode.Caption4VM.SelectFileDesc = CultureStringInfo.ApplicationFindResource("ProtectOperation_SelectFiles");
                fileRightsPreviewPage.ViewMode.SavePathDesc = CultureStringInfo.ApplicationFindResource("ProtectOperation_FilesSaveTo");
                fileRightsPreviewPage.ViewMode.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.RightsApplyDesc = CultureStringInfo.ApplicationFindResource("ProtectOperation_RightsApplyFiles");
            }
            fileRightsPreviewPage.ViewMode.Caption4VM.FileCount = fileCount;
            fileRightsPreviewPage.ViewMode.Caption4VM.FileNameList.Clear();
            for (int i = 0; i < fileCount; i++)
            {
                fileRightsPreviewPage.ViewMode.Caption4VM.FileNameList.Add(new FileModel()
                { Name = ProtectOpert.FileInfo.FileName[i], FullName = ProtectOpert.FileInfo.FileName[i] });
            }

            fileRightsPreviewPage.ViewMode.BackBtnVisibility = System.Windows.Visibility.Collapsed;
            fileRightsPreviewPage.ViewMode.PositiveBtnContent = CultureStringInfo.ApplicationFindResource("ProtectOperation_Btn_Protect");

            InitFRPreviewPageSubViewModel();
        }
        private void InitFRPreviewPageSubViewModel()
        {
            var selectedSavePath = ProtectOpert.CurrentSelectedSavePath;
            fileRightsPreviewPage.ViewMode.SavePathStpVisibility = System.Windows.Visibility.Visible;
            fileRightsPreviewPage.ViewMode.SavePath = selectedSavePath.DestDisplayPath;
            fileRightsPreviewPage.ViewMode.AdhocAndClassifiedRightsVM.FileType = CustomControls.components.FileType.CentralPolicy;

            // think about do this in background, use task await
            List<SkydrmLocal.rmc.sdk.FileRights> listRights = new List<SkydrmLocal.rmc.sdk.FileRights>();
            string warterMark = string.Empty;
            string previewId;
            // externRepo will use systemBucket token to protect file
            if (selectedSavePath.RepoName.Equals(DataTypeConvertHelper.REPOSITORIES))
            {
                previewId = App.SystemProject.Id.ToString();
            }
            else
            {
                previewId = selectedSavePath.OwnerId;
            }

            bool cvResult = int.TryParse(previewId, out int id);
            if (cvResult)
            {
                Host.Cursor = Cursors.Wait;
                listRights = ProtectOpert.PreviewRightsByCentralPolicy(id, ProtectOpert.SelectedTags, out warterMark);
                Host.Cursor = Cursors.Arrow;
            }

            tagPreivewWarterMark = warterMark;
            fileRightsPreviewPage.ViewMode.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.CentralTag = ProtectOpert.SelectedTags;
            fileRightsPreviewPage.ViewMode.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.CentralTagScrollViewMargin = new System.Windows.Thickness(78, 0, 0, 0);

            if (listRights.Count > 0)
            {
                fileRightsPreviewPage.ViewMode.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.AccessDenyVisibility = System.Windows.Visibility.Collapsed;
                bool isAddWarterMark = !string.IsNullOrEmpty(warterMark);
                fileRightsPreviewPage.ViewMode.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.RightsDisplayVM.FileRights = DataTypeConvertHelper.SDKRights2CustomControlRights(listRights.ToArray(), isAddWarterMark, false);
                fileRightsPreviewPage.ViewMode.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.RightsDisplayVM.WatermarkValue = warterMark;
                fileRightsPreviewPage.ViewMode.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.RightsDisplayVM.WaterPanlVisibility = isAddWarterMark ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                fileRightsPreviewPage.ViewMode.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.RightsDisplayVM.ValidityPanlVisibility = System.Windows.Visibility.Collapsed;
            }
            else
            {
                fileRightsPreviewPage.ViewMode.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.AccessDenyVisibility = System.Windows.Visibility.Visible;
            }
        }
        private void InitFRPreviewPageCommand()
        {
            CommandBinding binding;
            binding = new CommandBinding(FRPreview_DataCommands.Positive);
            binding.Executed += FRPreviewPositiveCommand;
            Host.CommandBindings.Add(binding);
            binding = new CommandBinding(FRPreview_DataCommands.Cancel);
            binding.Executed += CancelCommand;
            Host.CommandBindings.Add(binding);
        }

        private void FRPreviewPositiveCommand(object sender, ExecutedRoutedEventArgs e)
        {
            StartDoProtect_BgWorker();
        }
        #endregion

        #region Init FailedPage
        private void InitFailedPageCommand()
        {
            CommandBinding binding;
            binding = new CommandBinding(FailedP_DataCommands.Close);
            binding.Executed += CancelCommand;
            Host.CommandBindings.Add(binding);
        }
        #endregion

        private void CancelCommand(object sender, ExecutedRoutedEventArgs e)
        {
            Host.Close();
        }

        private void StartDoProtect_BgWorker()
        {
            if (!DoProtect_BgWorker.IsBusy)
            {
                DoProtect_BgWorker.RunWorkerAsync();

                // display progrss bar UI
                GridProBarVisible = System.Windows.Visibility.Visible;

                MenuDisableMgr.GetSingleton().IsProtecting = true;
                MenuDisableMgr.GetSingleton().MenuDisableNotify("LOGOUT", true);
                MenuDisableMgr.GetSingleton().MenuDisableNotify("EXIT", true);
            }
        }
        private void DoProtect_BgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            bool result = false;

            var listNxlFile = ProtectOpert.ProtectFile(ProtectOpert.SelectedTags);

            if (listNxlFile.Count > 0)
            {
                createdFiles = listNxlFile;
                result = true;
            }

            e.Result = result;
        }
        private void DoProtect_BgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            bool result = (bool)e.Result;
            GridProBarVisible = System.Windows.Visibility.Collapsed;

            MenuDisableMgr.GetSingleton().IsProtecting = false;
            if (MenuDisableMgr.GetSingleton().IsCanEnableMenu())
            {
                MenuDisableMgr.GetSingleton().MenuDisableNotify("LOGOUT", false);
                MenuDisableMgr.GetSingleton().MenuDisableNotify("EXIT", false);
            }

            if (ProtectOpert.FileInfo.FailedFileName.Count > 0 && createdFiles.Count < 1)
            {
                // all file protect failed
                App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("ProtectOperation_Protect_Failed"), false, "", "Protect failed");

                failedPage.ViewMode.Title = CultureStringInfo.ApplicationFindResource("ProtectOperation_Win_Title");
                foreach (var item in ProtectOpert.FileInfo.FailedFileName)
                {
                    failedPage.ViewMode.FailedFileList.Add(new FailedFile() { FileName = item.Key, ErrorMsg = item.Value });
                }
                failedPage.ViewMode.FailedDesc = "File could not be protected.";
                InitFailedPageCommand();

                Host.frm.Content = failedPage;
                BackBtnVisible = System.Windows.Visibility.Collapsed;
            }
            else if (ProtectOpert.FileInfo.FailedFileName.Count > 0 && createdFiles.Count > 0)
            {
                // some file protect success, some file protect failed
                App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("ProtectOperation_Protect_Failed"), false, "", "Protect failed");

                failedPage.ViewMode.Title = CultureStringInfo.ApplicationFindResource("ProtectOperation_Win_Title");
                foreach (var item in ProtectOpert.FileInfo.FailedFileName)
                {
                    failedPage.ViewMode.FailedFileList.Add(new FailedFile() { FileName = item.Key, ErrorMsg = item.Value });
                }
                int faildeCount = ProtectOpert.FileInfo.FailedFileName.Count;
                int successCount = createdFiles.Count;
                failedPage.ViewMode.FailedDesc = $"{faildeCount} of {faildeCount + successCount} files could not be protected.";
                failedPage.ViewMode.SuccessDesc1 = $"{successCount} of {faildeCount + successCount} are protected";
                failedPage.ViewMode.SuccessDesc2 = "successfully";
                failedPage.ViewMode.SuccessDesc3 = "and saved to";
                failedPage.ViewMode.SuccessDest = ProtectOpert.CurrentSelectedSavePath.DestDisplayPath;
                InitFailedPageCommand();

                Host.frm.Content = failedPage;
                BackBtnVisible = System.Windows.Visibility.Collapsed;
            }
            else if (ProtectOpert.FileInfo.FailedFileName.Count < 1 && createdFiles.Count > 0)
            {
                // all file protect success
                App.MainWin.viewModel.GetCreatedFile(createdFiles);

                Host.Close();
            }
            else if (ProtectOpert.FileInfo.FailedFileName.Count < 1 && createdFiles.Count < 1)
            {
                // no protect failed file, no protect success file. it's cancel protect file
                //  or all online file download failed
                return;
            }

        }

    }
}
