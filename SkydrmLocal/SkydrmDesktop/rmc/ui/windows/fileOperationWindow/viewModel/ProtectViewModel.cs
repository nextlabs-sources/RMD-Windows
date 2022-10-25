using Alphaleonis.Win32.Filesystem;
using CustomControls;
using CustomControls.components;
using SkydrmDesktop.Resources.languages;
using SkydrmDesktop.rmc.ui.windows.fileOperationWindow.operation;
using SkydrmDesktop.rmc.ui.windows.fileOperationWindow.view;
using SkydrmDesktop.rmc.ui.windows.mainWindow.model;
using SkydrmLocal.rmc.common.component;
using SkydrmLocal.rmc.common.helper;
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
    public class ProtectViewModel : BaseViewModel
    {
        private readonly SkydrmApp App = SkydrmApp.Singleton;
        private readonly FileSelectPage fileSelectPage = new FileSelectPage();
        // Use fileDestSelect window instead of page.
        private readonly FileDestSelectPage fileDestSelectPage = new FileDestSelectPage();
        private OrdinaryWin fileDestSelectWindow = new OrdinaryWin();
        private readonly FileRightsSelectPage fileRightsSelectPage = new FileRightsSelectPage();
        private readonly FileRightsPreviewPage fileRightsPreviewPage = new FileRightsPreviewPage();
        private readonly FileRightsResultPage fileRightsResultPage = new FileRightsResultPage();
        private readonly FailedPage failedPage = new FailedPage();

        private readonly BackgroundWorker DoProtect_BgWorker = new BackgroundWorker();

        // use for optimize page load
        private bool HasInitedFileDestPageVM { get; set; }
        private bool HasInitedFileRightsPageVM { get; set; }
        private bool HasInitedFileRightsPreviewPageVM { get; set; }

        private IProtect ProtectOpert { get; }

        private CurrentSelectedSavePath OriginalSavePath { get; set; }
        // for selected tags changed
        private bool tagIsValid { get; set; } = true;
        private Dictionary<string, List<string>> tags { get; set; } = new Dictionary<string, List<string>>();
        // for preview tag rights wartemark
        private string tagPreivewWarterMark { get; set; } = string.Empty;
        // use for notify MainWindow
        private List<SkydrmLocal.rmc.fileSystem.basemodel.INxlFile> createdFiles = new List<SkydrmLocal.rmc.fileSystem.basemodel.INxlFile>();

        public ProtectViewModel(IProtect operation, FileOperationWin win) : base(win)
        {
            this.ProtectOpert = operation;
            OriginalSavePath = ProtectOpert.CurrentSelectedSavePath;

            InitFileOperaWinCommand();

            if (operation.FileInfo.FromSource == model.FileFromSource.SkyDRM_Window_Button)
            {
                InitFileSelectPageViewModel();
                InitFileSelectPageCommand();
                Host.frm.Content = fileSelectPage;
            }
            else
            {
                HasInitedFileRightsPageVM = true;
                InitFRSelectPageViewModel();
                InitFRSelectPageCommand();
                Host.frm.Content = fileRightsSelectPage;
            }

            DoProtect_BgWorker.DoWork += DoProtect_BgWorker_DoWork;
            DoProtect_BgWorker.RunWorkerCompleted += DoProtect_BgWorker_RunWorkerCompleted;
        }

        #region InitFileOperationWinCommand
        private void InitFileOperaWinCommand()
        {
            CommandBinding binding;
            binding = new CommandBinding(FileOpeWin_DataCommands.Back);
            binding.Executed += PageBackCommand;
            Host.CommandBindings.Add(binding);
        }
        private void PageBackCommand(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                var page = HistoryStack.Pop();
                if (page != null)
                {
                    Host.frm.Content = page;
                }
                if (HistoryStack.Count < 1)
                {
                    BackBtnVisible = System.Windows.Visibility.Collapsed;
                }
            }
            catch (Exception)
            {
            }
            
        }
        #endregion

        #region Init FileSelectPage
        private void InitFileSelectPageViewModel()
        {
            fileSelectPage.ViewModel.Title = CultureStringInfo.ApplicationFindResource("ProtectOperation_Win_Title");
            fileSelectPage.ViewModel.Desc = CultureStringInfo.ApplicationFindResource("ProtectOperation_Win_SelectFileDesc");
            var repos = DataTypeConvertHelper.CreateFileRepoList2RepoItem(true);
            foreach (var item in repos)
            {
                fileSelectPage.ViewModel.RepoList.Add(item);
            }
            fileSelectPage.ViewModel.ShowFileType = DIsplayFileType.OnlyNormal;
            fileSelectPage.SetDefultSelectRepo();
        }
        private void InitFileSelectPageCommand()
        {
            CommandBinding binding;
            binding = new CommandBinding(FS_DataCommands.Browser);
            binding.Executed += FileBrowseCommand; ;
            Host.CommandBindings.Add(binding);
            binding = new CommandBinding(FS_DataCommands.Positive);
            binding.Executed += FileSelectPositiveCommand;
            Host.CommandBindings.Add(binding);
            binding = new CommandBinding(FS_DataCommands.Cancel);
            binding.Executed += CancelCommand;
            Host.CommandBindings.Add(binding);
        }
        private void FileBrowseCommand(object sender, ExecutedRoutedEventArgs e)
        {
            //init Dir.
            string[] selectedFile = null;

            try
            {
                // --- Also can use System.Windows.Forms.FolderBrowserDialog!
                Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
                dialog.Title = "Select File";

                //dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory); ; // set init Dir.

                // all files
                dialog.Filter = "All Files|*.*";
                dialog.Multiselect = true;

                if (dialog.ShowDialog() == true) // when user click ok button
                {
                    //fileName = dialog.SafeFileName;
                    selectedFile = dialog.FileNames;

                    if (!ProtectFileHelper.CheckFilePathDoProtect(selectedFile, out string tag, out List<string> rightFilePath))
                    {
                        return;
                    }

                    // update FileInfo
                    model.OperateFileInfo fileInfo = new model.OperateFileInfo(rightFilePath.ToArray(), null, model.FileFromSource.SkyDRM_Window_Button);
                    ProtectOpert.FileInfo = fileInfo;
                    // update CurrentSavePath
                    if (OriginalSavePath == null)
                    {
                        CurrentSelectedSavePath selectedSavePath = new CurrentSelectedSavePath(DataTypeConvertHelper.SYSTEMBUCKET,
                            Path.GetDirectoryName(rightFilePath[0]), Path.GetDirectoryName(rightFilePath[0]),
                            App.SystemProject.Id.ToString());
                        ProtectOpert.CurrentSelectedSavePath = selectedSavePath;
                    }

                    if (!HasInitedFileRightsPageVM)
                    {
                        HasInitedFileRightsPageVM = true;
                        InitFRSelectPageViewModel();
                        InitFRSelectPageCommand();
                    }
                    else
                    {
                        UpdateFileNameInCaption();
                        InitFRSelectPageSubViewModel();
                    }

                    HistoryStack.Push(fileSelectPage);
                    Host.frm.Content = fileRightsSelectPage;
                    if (HistoryStack.Count > 0)
                    {
                        BackBtnVisible = System.Windows.Visibility.Visible;
                    }
                }
            }
            catch (Exception msg)
            {
                App.Log.Warn("Exception in OpenFileDialog," + msg, msg);
            }
        }
        private void FileSelectPositiveCommand(object sender, ExecutedRoutedEventArgs e)
        {
            if (fileSelectPage.ViewModel.CurrentWorkingRepo is fileSystem.localDrive.LocalDriveRepo)
            {
                List<string> filePath = new List<string>();
                foreach (var item in fileSelectPage.ViewModel.FileList)
                {
                    if (item.IsChecked)
                    {
                        if (File.Exists(item.File.LocalPath))
                        {
                            filePath.Add(item.File.LocalPath);
                        }
                        else
                        {
                            App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("ProtectOperation_File_NotExist"), false, item.File.Name);
                        }
                    }
                }
                if (filePath.Count == 0)
                {
                    return;
                }

                if (!ProtectFileHelper.CheckFilePathDoProtect(filePath.ToArray(), out string tag, out List<string> rightFilePath))
                {
                    return;
                }
                // update FileInfo
                ProtectOpert.FileInfo = new model.OperateFileInfo(rightFilePath.ToArray(), null, model.FileFromSource.SkyDRM_Window_Button);
            }
            else
            {
                List<string> fileName = new List<string>();
                foreach (var item in fileSelectPage.ViewModel.FileList)
                {
                    if (item.IsChecked)
                    {
                        fileName.Add(item.File.Name);
                    }
                }
                // update FileInfo
                ProtectOpert.FileInfo = new model.OperateFileInfo(null, fileName.ToArray(), model.FileFromSource.SkyDRM_Window_Button);
            }
            // update CurrentSavePath
            if (OriginalSavePath == null)
            {
                ProtectOpert.CurrentSelectedSavePath = fileSelectPage.ViewModel.SourceFileDirPath;
            }

            if (!HasInitedFileRightsPageVM)
            {
                HasInitedFileRightsPageVM = true;
                InitFRSelectPageViewModel();
                InitFRSelectPageCommand();
            }
            else
            {
                UpdateFileNameInCaption();
                InitFRSelectPageSubViewModel();
            }
            HistoryStack.Push(fileSelectPage);
            Host.frm.Content = fileRightsSelectPage;
            if (HistoryStack.Count > 0)
            {
                BackBtnVisible = System.Windows.Visibility.Visible;
            }
        }
        private void UpdateFileNameInCaption()
        {
            model.OperateFileInfo fileInfo = ProtectOpert.FileInfo;
            int length = fileInfo.FileName.Length;
            fileDestSelectPage.ViewMode.Caption4VM.FileNameList.Clear();
            fileRightsSelectPage.ViewMode.Caption4VM.FileNameList.Clear();
            fileRightsPreviewPage.ViewMode.Caption4VM.FileNameList.Clear();
            for (int i = 0; i < length; i++)
            {
                fileDestSelectPage.ViewMode.Caption4VM.FileNameList.Add(new FileModel()
                { Name = fileInfo.FileName[i], FullName = fileInfo.FileName[i] });
                fileRightsSelectPage.ViewMode.Caption4VM.FileNameList.Add(new FileModel()
                { Name = fileInfo.FileName[i], FullName = fileInfo.FileName[i] });
                fileRightsPreviewPage.ViewMode.Caption4VM.FileNameList.Add(new FileModel()
                { Name = fileInfo.FileName[i], FullName = fileInfo.FileName[i] });
            }

            //update all page captionDesc.
            if (length > 1)
            {
                fileDestSelectPage.ViewMode.Caption4VM.SelectFileDesc = CultureStringInfo.ApplicationFindResource("ProtectOperation_SelectFiles");
                fileDestSelectPage.ViewMode.RadioSpDescribe = CultureStringInfo.ApplicationFindResource("ProtectOperation_LocationSaveFiles");

                fileRightsSelectPage.ViewMode.Caption4VM.SelectFileDesc = CultureStringInfo.ApplicationFindResource("ProtectOperation_SelectFiles");
                fileRightsSelectPage.ViewMode.SavePathDesc = CultureStringInfo.ApplicationFindResource("ProtectOperation_FilesSaveTo");
                fileRightsSelectPage.ViewMode.FileTypeSelect_Sp_Lable = CultureStringInfo.ApplicationFindResource("ProtectOperation_SpecifyRightsFiles");

                fileRightsPreviewPage.ViewMode.Caption4VM.SelectFileDesc = CultureStringInfo.ApplicationFindResource("ProtectOperation_SelectFiles");
                fileRightsPreviewPage.ViewMode.SavePathDesc = CultureStringInfo.ApplicationFindResource("ProtectOperation_FilesSaveTo");
                fileRightsPreviewPage.ViewMode.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.RightsApplyDesc = CultureStringInfo.ApplicationFindResource("ProtectOperation_RightsApplyFiles");
            }
            else
            {
                fileDestSelectPage.ViewMode.Caption4VM.SelectFileDesc = CultureStringInfo.ApplicationFindResource("ProtectOperation_SelectFile");
                fileDestSelectPage.ViewMode.RadioSpDescribe = CultureStringInfo.ApplicationFindResource("ProtectOperation_LocationSaveFile");

                fileRightsSelectPage.ViewMode.Caption4VM.SelectFileDesc = CultureStringInfo.ApplicationFindResource("ProtectOperation_SelectFile");
                fileRightsSelectPage.ViewMode.SavePathDesc = CultureStringInfo.ApplicationFindResource("ProtectOperation_FileSaveTo");
                fileRightsSelectPage.ViewMode.FileTypeSelect_Sp_Lable = CultureStringInfo.ApplicationFindResource("ProtectOperation_SpecifyRightsFile");

                fileRightsPreviewPage.ViewMode.Caption4VM.SelectFileDesc = CultureStringInfo.ApplicationFindResource("ProtectOperation_SelectFile");
                fileRightsPreviewPage.ViewMode.SavePathDesc = CultureStringInfo.ApplicationFindResource("ProtectOperation_FileSaveTo");
                fileRightsPreviewPage.ViewMode.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.RightsApplyDesc = CultureStringInfo.ApplicationFindResource("ProtectOperation_RightsApplyFile");
            }

            fileDestSelectPage.ViewMode.Caption4VM.FileCount = length;
            fileRightsSelectPage.ViewMode.Caption4VM.FileCount = length;
            fileRightsPreviewPage.ViewMode.Caption4VM.FileCount = length;
            
        }
        #endregion

        #region Init FileDestSelectPage
        private void InitFDSelectPageViewModel()
        {
            // caption
            int fileCount = ProtectOpert.FileInfo.FileName.Length;
            fileDestSelectPage.ViewMode.Caption4VM.Title = CultureStringInfo.ApplicationFindResource("ProtectOperation_Win_ChangeLocationTitle");
            if (fileCount > 1)
            {
                fileDestSelectPage.ViewMode.Caption4VM.SelectFileDesc = CultureStringInfo.ApplicationFindResource("ProtectOperation_SelectFiles");
                fileDestSelectPage.ViewMode.RadioSpDescribe = CultureStringInfo.ApplicationFindResource("ProtectOperation_LocationSaveFiles");
            }
            fileDestSelectPage.ViewMode.Caption4VM.FileCount = fileCount;
            fileDestSelectPage.ViewMode.Caption4VM.FileNameList.Clear();
            for (int i = 0; i < fileCount; i++)
            {
                fileDestSelectPage.ViewMode.Caption4VM.FileNameList.Add(new FileModel()
                { Name = ProtectOpert.FileInfo.FileName[i], FullName = ProtectOpert.FileInfo.FileName[i] });
            }

            fileDestSelectPage.ViewMode.SavePathStpVisibility = System.Windows.Visibility.Collapsed;

            // event handler
            fileDestSelectPage.ViewMode.OnRadioButton_Checked += FileDestSelectPage_OnRadioButton_Checked;
            fileDestSelectPage.ViewMode.OnTreeViewItemSelectedChanged += FileDestSelectPage_OnTreeViewItemSelectedChanged;

            //local drive data
            //fileDestSelectPage.ViewMode.LocalDrivePath= GetFileDirectory(ProtectOpert.FileInfo.FilePath);
            //central location data
            fileDestSelectPage.ViewMode.TreeViewVM.Nodes = DataTypeConvertHelper.FileRepoList2CustomControlNodes(ProtectOpert.TreeList, ProtectOpert.CurrentSelectedSavePath);

            //FileDestSelectPage RadioButton 
            bool isSystemBucket = ProtectOpert.CurrentSelectedSavePath.RepoName.Equals(DataTypeConvertHelper.SYSTEMBUCKET);
            if (isSystemBucket && App.SystemProject.IsFeatureEnabled)
            {
                fileDestSelectPage.ViewMode.ProtectLocation = ProtectLocation.LocalDrive;// will trigger OnRadioButton_Checked event
                fileDestSelectPage.ViewMode.SavePath = fileDestSelectPage.ViewMode.LocalDriveVM.CurrentSelectFolder.PathId;
            }
            else
            {
                fileDestSelectPage.ViewMode.ProtectLocation = ProtectLocation.CentralLocation;// will trigger OnRadioButton_Checked event
                fileDestSelectPage.ViewMode.SavePath = ProtectOpert.CurrentSelectedSavePath.DestDisplayPath;
            }

            if (!App.SystemProject.IsFeatureEnabled)
            {
                fileDestSelectPage.ViewMode.LocalDriveRdIsEnable = false;
            }
        }

        private void FileDestSelectPage_OnRadioButton_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (fileDestSelectPage.ViewMode.ProtectLocation == ProtectLocation.LocalDrive)
            {
                CurrentSelectedSavePath selectedSavePath = new CurrentSelectedSavePath(DataTypeConvertHelper.SYSTEMBUCKET,
                            fileDestSelectPage.ViewMode.LocalDriveVM.CurrentSelectFolder.PathId,
                            fileDestSelectPage.ViewMode.LocalDriveVM.CurrentSelectFolder.PathId,
                            App.SystemProject.Id.ToString());

                fileDestSelectPage.ViewMode.SavePath = selectedSavePath.DestDisplayPath;

                ProtectOpert.CurrentSelectedSavePath = selectedSavePath;
            }
            else
            {
                if (tempCentralLocalSelectedSavePath == null)
                {
                    return;
                }

                fileDestSelectPage.ViewMode.SavePath = tempCentralLocalSelectedSavePath?.DestDisplayPath;

                ProtectOpert.CurrentSelectedSavePath = tempCentralLocalSelectedSavePath;
            }
        }

        private CurrentSelectedSavePath tempCentralLocalSelectedSavePath = null;

        //use for FileDestSelectPage Positive Btn CanExcuteCommand
        private bool tempIsEnableFileDestSelectPagePositiveBtn = false;

        private void FileDestSelectPage_OnTreeViewItemSelectedChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<object> e)
        {
            CustomControls.components.TreeView.viewModel.TreeNodeViewModel treeNodeVM = e.NewValue as CustomControls.components.TreeView.viewModel.TreeNodeViewModel;
            
            CurrentSelectedSavePath selectedSavePath = new CurrentSelectedSavePath(treeNodeVM.RootName, treeNodeVM.PathId, treeNodeVM.PathDisplay, treeNodeVM.OwnerId);
            
            tempCentralLocalSelectedSavePath = selectedSavePath;

            if (fileDestSelectPage.ViewMode.ProtectLocation == ProtectLocation.CentralLocation)
            {
                if (treeNodeVM.OwnerId.Equals("-1"))// is 'Project', dummy node
                {
                    tempIsEnableFileDestSelectPagePositiveBtn = false;
                }
                else
                {
                    tempIsEnableFileDestSelectPagePositiveBtn = true;
                }
                fileDestSelectPage.ViewMode.SavePath = selectedSavePath.DestDisplayPath;
                ProtectOpert.CurrentSelectedSavePath = selectedSavePath;
            }
        }

        private void InitFDSelectPageCommand()
        {
            CommandBinding binding;
            binding = new CommandBinding(LD_DataCommands.Browser);
            binding.Executed += BrowseCommand; 
            fileDestSelectPage.CommandBindings.Add(binding);
            binding = new CommandBinding(FDSelect_DataCommands.Positive);
            binding.CanExecute += SelectDestPositiveCommand_CanExecute;
            binding.Executed += SelectDestPositiveCommand;
            fileDestSelectPage.CommandBindings.Add(binding);
            binding = new CommandBinding(FDSelect_DataCommands.Cancel);
            binding.Executed += SelectDestCancelCommand;
            fileDestSelectPage.CommandBindings.Add(binding);
        }
        private void BrowseCommand(object sender, ExecutedRoutedEventArgs e)
        {
            string dftRootFolder = fileDestSelectPage.ViewMode.LocalDriveVM.CurrentSelectFolder.PathId;
            System.Windows.Forms.FolderBrowserDialog dlg = new System.Windows.Forms.FolderBrowserDialog();
            dlg.Description = "Select a Folder";
            if (Directory.Exists(dftRootFolder))
            {
                dlg.SelectedPath = dftRootFolder;
            }
            var result = dlg.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK || result == System.Windows.Forms.DialogResult.Yes)
            {
                string localDrivePath = "";
                //if (!dlg.SelectedPath.EndsWith(Alphaleonis.Win32.Filesystem.Path.DirectorySeparatorChar.ToString()))
                //{
                //    localDrivePath = dlg.SelectedPath + Alphaleonis.Win32.Filesystem.Path.DirectorySeparatorChar;
                //}
                //else
                {
                    localDrivePath = dlg.SelectedPath;
                }

                int option;
                string tags;
                if (App.Rmsdk.RMP_IsSafeFolder(localDrivePath, out option, out tags))
                {
                    App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("ProtectOperation_Notify_RPM_Not_Protect"), false);
                    return;
                }

                CurrentSelectedSavePath selectedSavePath = new CurrentSelectedSavePath(DataTypeConvertHelper.SYSTEMBUCKET, localDrivePath,
                    localDrivePath,
                    App.SystemProject.Id.ToString());

                fileDestSelectPage.ViewMode.SavePath = selectedSavePath.DestDisplayPath;

                ProtectOpert.CurrentSelectedSavePath = selectedSavePath;

                InitFRSelectPageSubViewModel();

                fileDestSelectWindow.Close();

                // save radio selected state
                App.User.IsCentralLocationRadio = fileDestSelectPage.ViewMode.ProtectLocation == ProtectLocation.CentralLocation ? true : false;
            }
        }
        private void SelectDestPositiveCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (fileDestSelectPage.ViewMode.ProtectLocation == ProtectLocation.CentralLocation
                && tempIsEnableFileDestSelectPagePositiveBtn)
            {
                e.CanExecute = true;
            }
            if (fileDestSelectPage.ViewMode.ProtectLocation == ProtectLocation.LocalDrive
                && fileDestSelectPage.ViewMode.LocalDriveVM.CurrentSelectFolder.PathId != "/")
            {
                e.CanExecute = true;
            }
        }
        private void SelectDestPositiveCommand(object sender, ExecutedRoutedEventArgs e)
        {
            if (fileDestSelectPage.ViewMode.ProtectLocation == ProtectLocation.LocalDrive)
            {
                CurrentSelectedSavePath selectedSavePath = new CurrentSelectedSavePath(DataTypeConvertHelper.SYSTEMBUCKET,
                            fileDestSelectPage.ViewMode.LocalDriveVM.CurrentSelectFolder.PathId,
                            fileDestSelectPage.ViewMode.LocalDriveVM.CurrentSelectFolder.PathId,
                            App.SystemProject.Id.ToString());

                fileDestSelectPage.ViewMode.SavePath = selectedSavePath.DestDisplayPath;

                ProtectOpert.CurrentSelectedSavePath = selectedSavePath;
            }

            InitFRSelectPageSubViewModel();

            fileDestSelectWindow.Close();

            // save radio selected state
            App.User.IsCentralLocationRadio = fileDestSelectPage.ViewMode.ProtectLocation == ProtectLocation.CentralLocation ? true : false;
        }
        private void SelectDestCancelCommand(object sender, ExecutedRoutedEventArgs e)
        {
            fileDestSelectWindow.Close();
        }
        #endregion

        #region Init FileRightsSelectPage
        private void InitFRSelectPageViewModel()
        {
            // caption
            int fileCount = ProtectOpert.FileInfo.FileName.Length;
            fileRightsSelectPage.ViewMode.CaptionDescVisible = System.Windows.Visibility.Collapsed;
            fileRightsSelectPage.ViewMode.Caption4VM.Title = CultureStringInfo.ApplicationFindResource("ProtectOperation_Win_Title");
            if (fileCount > 1)
            {
                fileRightsSelectPage.ViewMode.Caption4VM.SelectFileDesc = CultureStringInfo.ApplicationFindResource("ProtectOperation_SelectFiles");
                fileRightsSelectPage.ViewMode.SavePathDesc = CultureStringInfo.ApplicationFindResource("ProtectOperation_FilesSaveTo");
                fileRightsSelectPage.ViewMode.FileTypeSelect_Sp_Lable = CultureStringInfo.ApplicationFindResource("ProtectOperation_SpecifyRightsFiles");
            }
            fileRightsSelectPage.ViewMode.Caption4VM.FileCount = fileCount;
            fileRightsSelectPage.ViewMode.Caption4VM.FileNameList.Clear();
            for (int i = 0; i < fileCount; i++)
            {
                fileRightsSelectPage.ViewMode.Caption4VM.FileNameList.Add(new FileModel()
                { Name = ProtectOpert.FileInfo.FileName[i], FullName = ProtectOpert.FileInfo.FileName[i] });
            }

            //wartermark, expiration 
            fileRightsSelectPage.ViewMode.AdhocPage_ViewModel.Watermarkvalue = App.User.Watermark.text;
            fileRightsSelectPage.ViewMode.AdhocPage_ViewModel.Expiry = DataTypeConvertHelper.SdkExpiry2CustomCtrExpiry(App.User.Expiration,
                out string expiryDate, true);

            fileRightsSelectPage.ViewMode.OnRadioBtnChecked += (ss, ee)=> 
            {
                if (fileRightsSelectPage.ViewMode.ProtectType == ProtectType.Adhoc)
                {
                    fileRightsSelectPage.ViewMode.PositiveBtnContent = CultureStringInfo.ApplicationFindResource("ProtectOperation_Btn_Protect");
                }
                else
                {
                    fileRightsSelectPage.ViewMode.PositiveBtnContent = CultureStringInfo.ApplicationFindResource("ProtectOperation_Btn_Next");
                }
            };

            // if event handler's binding not in the constructor, should bind handler before set value
            // when set fileRightsSelectPage.ViewMode.CtP_Classifications will excute this handler
            fileRightsSelectPage.ViewMode.OnClassificationChanged += (ss, ee) =>
            {
                Console.WriteLine($"Handler SelectClassificationChanged in ProtectViewModel: isValid({ee.NewValue.IsValid}),select count({ee.NewValue.KeyValues.Count})");
                tagIsValid = ee.NewValue.IsValid;
                tags = ee.NewValue.KeyValues;
            };

            InitFRSelectPageSubViewModel();

        }

        /// <summary>
        /// Init FileRightsSelectPage display path, radioButton, tags
        /// </summary>
        private void InitFRSelectPageSubViewModel()
        {
            var selectedSavePath = ProtectOpert.CurrentSelectedSavePath;

            // display path
            fileRightsSelectPage.ViewMode.SavePath = selectedSavePath.DestDisplayPath;
            // selected radio
            bool isCentral = SkydrmApp.Singleton.User.IsCentralPlcRadio;

            // reset radioButton
            fileRightsSelectPage.ViewMode.AdhocRadioIsEnable = true;
            fileRightsSelectPage.ViewMode.CentralRadioIsEnable = true;
            // reset isEnable right
            fileRightsSelectPage.ViewMode.AdhocPage_ViewModel.IsEnableRights = new HashSet<CustomControls.components.DigitalRights.model.FileRights>()
                    { CustomControls.components.DigitalRights.model.FileRights.RIGHT_DECRYPT };
            // reset Classification
            CustomControls.components.CentralPolicy.model.Classification[] classifications = new CustomControls.components.CentralPolicy.model.Classification[0];

            if (selectedSavePath.RepoName.Equals(DataTypeConvertHelper.MY_VAULT))
            {
                fileRightsSelectPage.ViewMode.ProtectType = ProtectType.Adhoc;
                fileRightsSelectPage.ViewMode.CentralRadioIsEnable = false;
                // set extract rights disable
                fileRightsSelectPage.ViewMode.AdhocPage_ViewModel.UncheckRights = new HashSet<CustomControls.components.DigitalRights.model.FileRights>()
                    { CustomControls.components.DigitalRights.model.FileRights.RIGHT_DECRYPT };
                fileRightsSelectPage.ViewMode.AdhocPage_ViewModel.DisableRights = new HashSet<CustomControls.components.DigitalRights.model.FileRights>()
                    { CustomControls.components.DigitalRights.model.FileRights.RIGHT_DECRYPT };
            }
            else if (selectedSavePath.RepoName.Equals(DataTypeConvertHelper.SYSTEMBUCKET)
                || selectedSavePath.RepoName.Equals(DataTypeConvertHelper.WORKSPACE)
                || selectedSavePath.RepoName.Equals(DataTypeConvertHelper.REPOSITORIES))
            {
                if (!isCentral && App.SystemProject.IsEnableAdHoc)
                {
                    fileRightsSelectPage.ViewMode.ProtectType = ProtectType.Adhoc;
                }
                else
                {
                    fileRightsSelectPage.ViewMode.ProtectType = ProtectType.CentralPolicy;
                }
                // IsEnableAdHoc
                if (!App.SystemProject.IsEnableAdHoc)
                {
                    fileRightsSelectPage.ViewMode.AdhocRadioIsEnable = false;
                }

                classifications = DataTypeConvertHelper.SdkTag2CustomControlTag(App.SystemProject.GetClassifications());
            }
            else if (selectedSavePath.RepoName.Equals(DataTypeConvertHelper.PROJECT))
            {
                bool projectIsEnableAdHoc = false;

                foreach (var item in ProtectOpert.TreeList)
                {
                    if (item is SkydrmLocal.rmc.fileSystem.project.ProjectRepo)
                    {
                        IList<SkydrmLocal.rmc.fileSystem.project.ProjectData> projects = (item as SkydrmLocal.rmc.fileSystem.project.ProjectRepo).FilePool;
                        foreach (var project in projects)
                        {
                            if (selectedSavePath.OwnerId.Equals(project.ProjectInfo.ProjectId.ToString()))
                            {
                                projectIsEnableAdHoc = project.ProjectInfo.Raw.IsEnableAdHoc;
                                classifications = DataTypeConvertHelper.SdkTag2CustomControlTag(project.ProjectInfo.Raw.ListClassifications());
                                break;
                            }
                        }
                        break;
                    }
                }
                if (!isCentral && projectIsEnableAdHoc)
                {
                    fileRightsSelectPage.ViewMode.ProtectType = ProtectType.Adhoc;
                }
                else
                {
                    fileRightsSelectPage.ViewMode.ProtectType = ProtectType.CentralPolicy;
                }
                // IsEnableAdHoc
                if (!projectIsEnableAdHoc)
                {
                    fileRightsSelectPage.ViewMode.AdhocRadioIsEnable = false;
                }
            }

            // tags
            fileRightsSelectPage.ViewMode.CtP_Classifications = classifications;

            if (fileRightsSelectPage.ViewMode.CtP_Classifications.Length == 0)
            {
                fileRightsSelectPage.ViewMode.CpWarnDesVisible = System.Windows.Visibility.Visible;
            }
            else
            {
                fileRightsSelectPage.ViewMode.CpWarnDesVisible = System.Windows.Visibility.Collapsed;
            }

            if (fileRightsSelectPage.ViewMode.ProtectType == ProtectType.Adhoc)
            {
                fileRightsSelectPage.ViewMode.PositiveBtnContent = CultureStringInfo.ApplicationFindResource("ProtectOperation_Btn_Protect");
            }
            else
            {
                fileRightsSelectPage.ViewMode.PositiveBtnContent = CultureStringInfo.ApplicationFindResource("ProtectOperation_Btn_Next");
            }

        }
        private void InitFRSelectPageCommand()
        {
            CommandBinding binding;
            binding = new CommandBinding(FRS_DataCommands.ChangeDestination);
            binding.Executed += ChangeDestCommand;
            Host.CommandBindings.Add(binding);
            binding = new CommandBinding(FRS_DataCommands.Positive);
            binding.CanExecute += FRightsSelectPositiveCommand_CanExecute;
            binding.Executed += FRightsSelectPositiveCommand;
            Host.CommandBindings.Add(binding);
            binding = new CommandBinding(FRS_DataCommands.Cancel);
            binding.Executed += CancelCommand;
            Host.CommandBindings.Add(binding);
        }
        private void ChangeDestCommand(object sender, ExecutedRoutedEventArgs e)
        {
            if (!HasInitedFileDestPageVM)
            {
                // think about the background to init
                HasInitedFileDestPageVM = true;
                InitFDSelectPageViewModel();
                InitFDSelectPageCommand();
            }
            fileDestSelectWindow = new OrdinaryWin();
            fileDestSelectWindow.Owner = Host;
            fileDestSelectWindow.SetFrameContent(fileDestSelectPage);
            fileDestSelectWindow.ShowDialog();
        }
        private void FRightsSelectPositiveCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (fileRightsSelectPage.ViewMode.ProtectType == ProtectType.Adhoc)
            {
                e.CanExecute = true;
            }
            else
            {
                if (tagIsValid)
                {
                    e.CanExecute = true;
                }
            }
        }
        private void FRightsSelectPositiveCommand(object sender, ExecutedRoutedEventArgs e)
        {
            bool isCentralPolicy = fileRightsSelectPage.ViewMode.ProtectType == ProtectType.CentralPolicy ? true : false;
            // save radio selected state
            App.User.IsCentralPlcRadio = isCentralPolicy;

            if (isCentralPolicy)
            {
                // init FileRightsPreviewPage
                if (!HasInitedFileRightsPreviewPageVM)
                {
                    HasInitedFileRightsPreviewPageVM = true;
                    InitFRPreviewPageViewModel();
                    InitFRPreviewPageCommand();
                }
                else
                {
                    InitFRPreviewPageSubViewModel();
                }
                HistoryStack.Push(fileRightsSelectPage);
                Host.frm.Content = fileRightsPreviewPage;
                if (HistoryStack.Count > 0)
                {
                    BackBtnVisible = System.Windows.Visibility.Visible;
                }
            }
            else
            {
                var sdkExpiry = DataTypeConvertHelper.CustomCtrExpiry2SdkExpiry(fileRightsSelectPage.ViewMode.AdhocPage_ViewModel.Expiry);
                if (sdkExpiry.type != SkydrmLocal.rmc.sdk.ExpiryType.NEVER_EXPIRE && DateTimeHelper.DateTimeToTimestamp(DateTime.Now) > sdkExpiry.End)
                {
                    App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("ProtectOperation_InValidExpiry"), false);
                    return;
                }
                // do protect
                StartDoProtect_BgWorker();
            }
        }
        #endregion

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
                listRights = ProtectOpert.PreviewRightsByCentralPolicy(id, tags, out warterMark);
                Host.Cursor = Cursors.Arrow;
            }

            tagPreivewWarterMark = warterMark;
            fileRightsPreviewPage.ViewMode.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.CentralTag = tags;
            fileRightsPreviewPage.ViewMode.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.CentralTagScrollViewMargin = new System.Windows.Thickness(78,0,0,0);

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

        #region Init FileRightsResultPage
        private void InitFRResultPageViewModel()
        {
            // caption
            int fileCount = ProtectOpert.FileInfo.FileName.Length;

            fileRightsResultPage.ViewMode.Caption3VM.Title = fileCount > 1 ?
                CultureStringInfo.ApplicationFindResource("ProtectOperation_Win_Titles")  : //For multiple files protect title display.
                CultureStringInfo.ApplicationFindResource("ProtectOperation_Win_Title"); //For single file protect title display.;
            fileRightsResultPage.ViewMode.Caption3VM.PromptText = fileCount > 1 ?
                CultureStringInfo.ApplicationFindResource("ProtectOperation_PromptHave") : //For multiple files protect title display.
                CultureStringInfo.ApplicationFindResource("ProtectOperation_PromptHas"); //For single file protect title display.;

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < fileCount; i++)
            {
                sb.Append(ProtectOpert.FileInfo.FileName[i]);
                if (i != fileCount - 1)
                {
                    sb.Append(";\n");
                }
            }
            fileRightsResultPage.ViewMode.Caption3VM.FileName = sb.ToString();
            fileRightsResultPage.ViewMode.Caption3VM.Desitination = ProtectOpert.CurrentSelectedSavePath.DestDisplayPath;

            bool isCentralPolicy = App.User.IsCentralPlcRadio;
            if (isCentralPolicy)
            {
                fileRightsResultPage.ViewMode.AdhocAndClassifiedRightsVM.FileType = CustomControls.components.FileType.CentralPolicy;

                var frPreClassifiedRightsVM = fileRightsPreviewPage.ViewMode.AdhocAndClassifiedRightsVM.ClassifiedRightsVM;
                fileRightsResultPage.ViewMode.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.CentralTag = frPreClassifiedRightsVM.CentralTag;
                fileRightsResultPage.ViewMode.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.AccessDenyVisibility = frPreClassifiedRightsVM.AccessDenyVisibility;

                if (frPreClassifiedRightsVM.AccessDenyVisibility== System.Windows.Visibility.Visible)
                {
                    return;
                }
                fileRightsResultPage.ViewMode.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.RightsDisplayVM.RightsList = frPreClassifiedRightsVM.RightsDisplayVM.RightsList;
                fileRightsResultPage.ViewMode.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.RightsDisplayVM.WatermarkValue = frPreClassifiedRightsVM.RightsDisplayVM.WatermarkValue;
                fileRightsResultPage.ViewMode.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.RightsDisplayVM.WaterPanlVisibility = frPreClassifiedRightsVM.RightsDisplayVM.WaterPanlVisibility;
                fileRightsResultPage.ViewMode.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.RightsDisplayVM.ValidityPanlVisibility = frPreClassifiedRightsVM.RightsDisplayVM.ValidityPanlVisibility;
            }
            else
            {
                fileRightsResultPage.ViewMode.AdhocAndClassifiedRightsVM.FileType = CustomControls.components.FileType.Adhoc;

                var frSelectAdhocVM = fileRightsSelectPage.ViewMode.AdhocPage_ViewModel;
                fileRightsResultPage.ViewMode.AdhocAndClassifiedRightsVM.AdhocRightsVM.FileRights = frSelectAdhocVM.Rights;
                fileRightsResultPage.ViewMode.AdhocAndClassifiedRightsVM.AdhocRightsVM.WatermarkValue = frSelectAdhocVM.Watermarkvalue;
                fileRightsResultPage.ViewMode.AdhocAndClassifiedRightsVM.AdhocRightsVM.WaterPanlVisibility = frSelectAdhocVM.Rights.Contains(CustomControls.components.DigitalRights.model.FileRights.RIGHT_WATERMARK) ?
                     System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                fileRightsResultPage.ViewMode.AdhocAndClassifiedRightsVM.AdhocRightsVM.ValidityValue = frSelectAdhocVM.ExpireDateValue;
            }

        }
        private void InitFRResultPageCommand()
        {
            CommandBinding binding;
            binding = new CommandBinding(FRResult_DataCommands.Close);
            binding.Executed += CancelCommand;
            Host.CommandBindings.Add(binding);
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

            if (ProtectOpert.FileInfo.FromSource == model.FileFromSource.SkyDRM_Window_Button)
            {
                if (fileSelectPage.ViewModel.CurrentWorkingRepo is fileSystem.localDrive.LocalDriveRepo)
                { }
                else
                {
                    // download file
                    List<string> filepath = new List<string>();
                    List<Task> tasks = new List<Task>();
                    foreach (var item in fileSelectPage.ViewModel.FileList)
                    {
                        if (item.IsChecked)
                        {
                            if (item.File.IsMarkedOffline && File.Exists(item.File.LocalPath))
                            {
                                continue;
                            }
                            else
                            {
                                tasks.Add(Task.Factory.StartNew((nxl) =>
                                {
                                    try
                                    {
                                        ((SkydrmLocal.rmc.fileSystem.basemodel.INxlFile)nxl).DownloadFile();
                                    }
                                    catch (Exception ex)
                                    {
                                        App.MessageNotify.NotifyMsg(item.File.Name, ex.Message, featureProvider.MessageNotify.EnumMsgNotifyType.LogMsg,
                                            featureProvider.MessageNotify.MsgNotifyOperation.DOWNLOAD, featureProvider.MessageNotify.EnumMsgNotifyResult.Failed);
                                    }
                                }, item.File, TaskCreationOptions.None));
                            }

                        }
                    }
                    Task.WaitAll(tasks.ToArray());

                    foreach (var item in fileSelectPage.ViewModel.FileList)
                    {
                        if (item.IsChecked && File.Exists(item.File.LocalPath))
                        {
                            filepath.Add(item.File.LocalPath);
                        }
                    }
                    // fix Bug 66446 - Should pop a bubble when protect myDrive file in network off situation by "Create protected file" button
                    if (filepath.Count < 1)
                    {
                        App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("ProtectOperation_Protect_Failed"), false, "", "Protect failed");
                        e.Result = false;
                        return;
                    }
                    ProtectOpert.FileInfo = new model.OperateFileInfo(filepath.ToArray(), null, model.FileFromSource.SkyDRM_Window_Button);
                }
            }

            var sdkRights = DataTypeConvertHelper.CustomCtrRights2SDKRights(fileRightsSelectPage.ViewMode.AdhocPage_ViewModel.Rights);
            string warterMark = "";
            if (App.User.IsCentralPlcRadio)
            {
                warterMark = tagPreivewWarterMark;
                sdkRights.Clear();
            }
            else
            {
                warterMark = fileRightsSelectPage.ViewMode.AdhocPage_ViewModel.Watermarkvalue;
            }
            var sdkExpiry = DataTypeConvertHelper.CustomCtrExpiry2SdkExpiry(fileRightsSelectPage.ViewMode.AdhocPage_ViewModel.Expiry);
            var selectTags = tags;

            var listNxlFile = ProtectOpert.ProtectFile(sdkRights, warterMark, sdkExpiry, selectTags);

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
                    failedPage.ViewMode.FailedFileList.Add(new FailedFile() { FileName=item.Key, ErrorMsg = item.Value });
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
            else if(ProtectOpert.FileInfo.FailedFileName.Count < 1 && createdFiles.Count < 1)
            {
                // no protect failed file, no protect success file. it's cancel protect file
                //  or all online file download failed
                return;
            }

        }

    }
}
