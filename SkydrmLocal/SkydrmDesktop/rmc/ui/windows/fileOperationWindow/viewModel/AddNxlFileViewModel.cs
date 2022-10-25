using Alphaleonis.Win32.Filesystem;
using CustomControls;
using SkydrmDesktop.Resources.languages;
using SkydrmDesktop.rmc.common.helper;
using SkydrmDesktop.rmc.ui.windows.fileOperationWindow.operation;
using SkydrmDesktop.rmc.ui.windows.fileOperationWindow.view;
using SkydrmDesktop.rmc.ui.windows.mainWindow.model;
using SkydrmLocal.rmc.common.component;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.ui.utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SkydrmDesktop.rmc.ui.windows.fileOperationWindow.viewModel
{
    class AddNxlFileViewModel : BaseViewModel
    {
        private readonly SkydrmApp App = SkydrmApp.Singleton;
        private readonly FileSelectPage fileSelectPage = new FileSelectPage();
        private readonly FileDestSelectPage fileDestSelectPage = new FileDestSelectPage();
        private readonly FileRightsPreviewPage fileRightsPreviewPage = new FileRightsPreviewPage();
        private readonly FileRightsResultPage fileRightsResultPage = new FileRightsResultPage();
        private readonly FailedPage failedPage = new FailedPage();

        private readonly BackgroundWorker DoAddNxlFile_BgWorker = new BackgroundWorker();
        private readonly BackgroundWorker CheckNxlFileExists_BgWorker = new BackgroundWorker();

        private bool HasInitedFDSelectPageVM { get; set; }
        private bool HasInitedFileRightsPreviewPageVM { get; set; }

        private IAddNxlFile AddNxlFileOpert { get; set; }

        private string fileFullPath { get; set; }
        // for preview tag rights wartemark
        private SkydrmLocal.rmc.sdk.WaterMarkInfo tagPreivewWarterMark { get; set; } = new SkydrmLocal.rmc.sdk.WaterMarkInfo() { text = "", fontName = "", fontColor = "" };
        // use for notify MainWindow
        private List<SkydrmLocal.rmc.fileSystem.basemodel.INxlFile> createdFiles = new List<SkydrmLocal.rmc.fileSystem.basemodel.INxlFile>();

        public AddNxlFileViewModel(IAddNxlFile operation, FileOperationWin win) : base(win)
        {
            this.AddNxlFileOpert = operation;

            DoAddNxlFile_BgWorker.DoWork += DoAddNxlFile_BgWorker_DoWork;
            DoAddNxlFile_BgWorker.RunWorkerCompleted += DoAddNxlFile_BgWorker_RunWorkerCompleted;

            CheckNxlFileExists_BgWorker.DoWork += CheckNxlFileExists_BgWorker_DoWork;
            CheckNxlFileExists_BgWorker.RunWorkerCompleted += CheckNxlFileExists_BgWorker_RunWorkerCompleted;

            InitFileOperaWinCommand();

            if (AddNxlFileOpert.FileInfo.FromSource == model.FileFromSource.SkyDRM_Window_Button)
            {
                InitFileSelectPageViewModel();
                InitFileSelectPageCommand();
                Host.frm.Content = fileSelectPage;
            }
            else
            {
                HasInitedFDSelectPageVM = true;
                InitFDSelectPageViewModel();
                InitFDSelectPageCommand();
                Host.frm.Content = fileDestSelectPage;
            }
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
            fileSelectPage.ViewModel.Title = CultureStringInfo.ApplicationFindResource("AddNxlOperation_Win_Title_AddProtected");
            fileSelectPage.ViewModel.Desc = CultureStringInfo.ApplicationFindResource("AddNxlOperation_Win_SelectFileDesc");
            fileSelectPage.ViewModel.CheckBoxVisible = System.Windows.Visibility.Collapsed;

            var repos = DataTypeConvertHelper.CreateFileRepoList2RepoItem(false);
            foreach (var item in repos)
            {
                fileSelectPage.ViewModel.RepoList.Add(item);
            }

            var prepos = DataTypeConvertHelper.CreatProjectRepo2ProjectRepoItem();
            foreach (var item in prepos)
            {
                fileSelectPage.ViewModel.ProjectRepoList.Add(item);
            }
            fileSelectPage.ViewModel.ProjectRepoVisible = System.Windows.Visibility.Visible;

            fileSelectPage.ViewModel.ShowFileType = DIsplayFileType.OnlyNxlFile;
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
            string selectedFile = null;

            try
            {
                // --- Also can use System.Windows.Forms.FolderBrowserDialog!
                Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
                dialog.Title = "Select a NextLabs Protected Files";

                //dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory); // set init Dir.

                // .nxl files
                dialog.Filter = "NextLabs Protected Files (*.nxl)|*.nxl";

                if (dialog.ShowDialog() == true) // when user click ok button
                {
                    selectedFile = dialog.FileName;

                    if (!CheckRightToAddNxl(selectedFile, out SkydrmLocal.rmc.sdk.NxlFileFingerPrint fp))
                    {
                        return;
                    }

                    // update AddNxlFile
                    fileFullPath = selectedFile;
                    model.OperateFileInfo fileInfo = new model.OperateFileInfo(new string[1] { selectedFile }, null, model.FileFromSource.SkyDRM_Window_Button);
                    AddNxlFileOpert = new AddNxlFile(fp, fileInfo, AddNxlFileOpert.TreeList, AddNxlFileOpert.CurrentSelectedSavePath, null);

                    if (!HasInitedFDSelectPageVM)
                    {
                        HasInitedFDSelectPageVM = true;
                        InitFDSelectPageViewModel();
                        InitFDSelectPageCommand();
                    }
                    else
                    {
                        UpdateFileNameInCaption();
                        UpdateFileDestPageVM();
                    }
                    HistoryStack.Push(fileSelectPage);
                    Host.frm.Content = fileDestSelectPage;
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
            GridProBarVisible = System.Windows.Visibility.Visible;

            AsyncHelper.RunAsync(() =>
            {
                bool result = GetSelectedFileAndUpdateFileInfo();
                return result;
            },
            (rt) =>
            {
                GridProBarVisible = System.Windows.Visibility.Collapsed;
                if (rt)
                {
                    if (!HasInitedFDSelectPageVM)
                    {
                        HasInitedFDSelectPageVM = true;
                        InitFDSelectPageViewModel();
                        InitFDSelectPageCommand();
                    }
                    else
                    {
                        UpdateFileNameInCaption();
                        UpdateFileDestPageVM();
                    }
                    HistoryStack.Push(fileSelectPage);
                    Host.frm.Content = fileDestSelectPage;
                    if (HistoryStack.Count > 0)
                    {
                        BackBtnVisible = System.Windows.Visibility.Visible;
                    }
                }

            });
        }
        private bool GetSelectedFileAndUpdateFileInfo()
        {
            bool result = true;
            string selectPath;

            if (fileSelectPage.ViewModel.CurrentWorkingRepo is fileSystem.localDrive.LocalDriveRepo)
            {
                selectPath = fileSelectPage.ViewModel.CurrentSelectFile.LocalPath;
                if (!File.Exists(selectPath))
                {
                    App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("ProtectOperation_File_NotExist"), false, Path.GetFileName(selectPath));
                    return false;
                }

                if (!CheckRightToAddNxl(selectPath, out SkydrmLocal.rmc.sdk.NxlFileFingerPrint fp))
                {
                    return false;
                }
                // update AddNxlFile
                fileFullPath = selectPath;
                model.OperateFileInfo fileInfo = new model.OperateFileInfo(new string[1] { selectPath }, null, model.FileFromSource.SkyDRM_Window_Button);
                AddNxlFileOpert = new AddNxlFile(fp, fileInfo, AddNxlFileOpert.TreeList, AddNxlFileOpert.CurrentSelectedSavePath, null);
            }
            else
            {
                string selectFileName = fileSelectPage.ViewModel.CurrentSelectFile.Name;
                var nxl = fileSelectPage.ViewModel.CurrentSelectFile;
                EnumNxlFileStatus status = nxl.FileStatus;
                if (status == EnumNxlFileStatus.WaitingUpload)
                {
                    App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("AddNxlOperation_Hint_FileWaitingUpload"), false, nxl.Name);
                    return false;
                }

                bool fileExist = nxl.IsMarkedOffline && File.Exists(nxl.LocalPath);
                var path = "";
                try
                {
                    if (!fileExist)
                    {
                        nxl.DownloadPartial();
                        path = nxl.PartialLocalPath;
                    }
                    else
                    {
                        path = nxl.LocalPath;
                    }
                }
                catch (Exception e)
                {
                    App.ShowBalloonTip(e.Message, false, nxl.Name);
                    return false;
                }
                if (!CheckRightToAddNxl(path, nxl.Name, out SkydrmLocal.rmc.sdk.NxlFileFingerPrint fp))
                {
                    return false;
                }

                selectPath = nxl.LocalPath;
                // update AddNxlFile
                fileFullPath = nxl.SourcePath;
                model.OperateFileInfo fileInfo = new model.OperateFileInfo(new string[1] { selectPath }, new string[1] { nxl.Name }, model.FileFromSource.SkyDRM_Window_Button);
                AddNxlFileOpert = new AddNxlFile(fp, fileInfo, AddNxlFileOpert.TreeList, AddNxlFileOpert.CurrentSelectedSavePath, nxl);
            }

            return result;
        }

        private bool CheckRightToAddNxl(string filePath, out SkydrmLocal.rmc.sdk.NxlFileFingerPrint fp)
        {
            return CheckRightToAddNxl(filePath, Path.GetFileName(filePath), out fp);
        }

        private bool CheckRightToAddNxl(string filePath,string fileName, out SkydrmLocal.rmc.sdk.NxlFileFingerPrint fp)
        {
            bool result = true;

            fp = new SkydrmLocal.rmc.sdk.NxlFileFingerPrint();
            try
            {
                fp = App.Rmsdk.User.GetNxlFileFingerPrint(filePath);
            }
            catch (Exception e)
            {
                App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Common_Not_Authorized"), false, fileName);
                return false;
            }

            if (!(fp.isFromMyVault
                      || (fp.isFromSystemBucket && ((fp.hasAdminRights && fp.HasRight(SkydrmLocal.rmc.sdk.FileRights.RIGHT_VIEW))
                      || fp.HasRight(SkydrmLocal.rmc.sdk.FileRights.RIGHT_DECRYPT)
                      || fp.HasRight(SkydrmLocal.rmc.sdk.FileRights.RIGHT_DOWNLOAD) || fp.HasRight(SkydrmLocal.rmc.sdk.FileRights.RIGHT_SAVEAS)))
                      || (fp.isFromPorject && (fp.hasAdminRights || fp.HasRight(SkydrmLocal.rmc.sdk.FileRights.RIGHT_DECRYPT)))))
            {
                App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Common_Not_Authorized"), false, fileName);
                return false;
            }

            return result;
        }

        private void UpdateFileNameInCaption()
        {
            model.OperateFileInfo fileInfo = AddNxlFileOpert.FileInfo;

            //update all page captionDesc.
            fileDestSelectPage.ViewMode.Caption4VM.FileNameList.Clear();
            fileRightsPreviewPage.ViewMode.Caption4VM.FileNameList.Clear();
            int length = fileInfo.FileName.Length;
            for (int i = 0; i < length; i++)
            {
                fileDestSelectPage.ViewMode.Caption4VM.FileNameList.Add(new CustomControls.components.FileModel()
                { Name = fileInfo.FileName[i], FullName = fileFullPath });

                fileRightsPreviewPage.ViewMode.Caption4VM.FileNameList.Add(new CustomControls.components.FileModel()
                { Name = fileInfo.FileName[i], FullName = fileFullPath });
            }

            fileDestSelectPage.ViewMode.Caption4VM.Title = CultureStringInfo.ApplicationFindResource("ProtectOperation_Win_ChangeLocationTitle");
            fileDestSelectPage.ViewMode.Caption4VM.FileCount = length;

            fileRightsPreviewPage.ViewMode.CaptionDescVisible = System.Windows.Visibility.Collapsed;
            fileRightsPreviewPage.ViewMode.Caption4VM.Title =
                CultureStringInfo.ApplicationFindResource("AddNxlOperation_Win_Title_AddProtected");
            fileRightsPreviewPage.ViewMode.Caption4VM.FileCount = length;
        }

        private void UpdateFileDestPageVM()
        {
            List<CustomControls.components.TreeView.model.Node> customNodeList = new List<CustomControls.components.TreeView.model.Node>();
            if (AddNxlFileOpert.CurrentSelectedSavePath != null)
            {
                customNodeList = DataTypeConvertHelper.FileRepoList2CustomControlNodes(AddNxlFileOpert.TreeList, AddNxlFileOpert.CurrentSelectedSavePath, false);
            }
            else
            {
                customNodeList = DataTypeConvertHelper.FileRepoList2CustomControlNodes(AddNxlFileOpert.TreeList);
            }

            if (AddNxlFileOpert.FileInfo.FromSource != model.FileFromSource.SkyDRM_PlugIn)
            {
                RemoveSourceRepoNode(customNodeList);
            }

            fileDestSelectPage.ViewMode.TreeViewVM.Nodes = customNodeList;
        }
        #endregion

        #region Init FileDestSelectPage
        private void InitFDSelectPageViewModel()
        {
            // caption
            int fileCount = AddNxlFileOpert.FileInfo.FileName.Length;
            if (AddNxlFileOpert.FileInfo.FromSource == model.FileFromSource.SkyDRM_Window_Button)
            {
                fileDestSelectPage.ViewMode.Caption4VM.Title = CultureStringInfo.ApplicationFindResource("AddNxlOperation_Win_Title_AddProtected");
            }
            else
            {
                fileDestSelectPage.ViewMode.Caption4VM.Title = CultureStringInfo.ApplicationFindResource("AddNxlOperation_Win_Title");
            }
            fileDestSelectPage.ViewMode.Caption4VM.FileCount = fileCount;
            fileDestSelectPage.ViewMode.Caption4VM.FileNameList.Clear();
            for (int i = 0; i < fileCount; i++)
            {
                fileDestSelectPage.ViewMode.Caption4VM.FileNameList.Add(new CustomControls.components.FileModel()
                { Name = AddNxlFileOpert.FileInfo.FileName[i], FullName = fileFullPath });
            }

            fileDestSelectPage.ViewMode.SavePathStpVisibility = System.Windows.Visibility.Collapsed;

            // radioBtn stackPanel
            fileDestSelectPage.ViewMode.RadioSpDescribe = CultureStringInfo.ApplicationFindResource("AddNxlOperation_RadioSpTitle");
            fileDestSelectPage.ViewMode.RaidoVisibility = System.Windows.Visibility.Collapsed;

            // event handler
            fileDestSelectPage.ViewMode.OnTreeViewItemSelectedChanged += FileDestSelectPage_OnTreeViewItemSelectedChanged;

            //central location data
            List<CustomControls.components.TreeView.model.Node> customNodeList = new List<CustomControls.components.TreeView.model.Node>();
            if (AddNxlFileOpert.CurrentSelectedSavePath != null)
            {
                customNodeList = DataTypeConvertHelper.FileRepoList2CustomControlNodes(AddNxlFileOpert.TreeList, AddNxlFileOpert.CurrentSelectedSavePath, true);
            }
            else
            {
                customNodeList = DataTypeConvertHelper.FileRepoList2CustomControlNodes(AddNxlFileOpert.TreeList);
            }

            if (AddNxlFileOpert.FileInfo.FromSource != model.FileFromSource.SkyDRM_PlugIn)
            {
                RemoveSourceRepoNode(customNodeList);
            }

            fileDestSelectPage.ViewMode.TreeViewVM.Nodes = customNodeList;

            fileDestSelectPage.ViewMode.PositiveBtnContent = CultureStringInfo.ApplicationFindResource("AddNxlOperation_FDestBtn_Next");
        }

        private void RemoveSourceRepoNode(List<CustomControls.components.TreeView.model.Node> nodes)
        {
            CustomControls.components.TreeView.model.Node removeNode = null;

            if (AddNxlFileOpert.OriginalFileRepo == SkydrmLocal.rmc.fileSystem.basemodel.EnumFileRepo.REPO_WORKSPACE)
            {
                foreach (var item in nodes)
                {
                    if (item.RootName.Equals(DataTypeConvertHelper.WORKSPACE))
                    {
                        removeNode = item;
                        break;
                    }
                }
                if (removeNode != null)
                {
                    nodes.Remove(removeNode);
                }
            }
            else if (AddNxlFileOpert.OriginalFileRepo == SkydrmLocal.rmc.fileSystem.basemodel.EnumFileRepo.REPO_PROJECT)
            {
                CustomControls.components.TreeView.model.Node removeParentNode = null;
                foreach (var item in nodes)
                {
                    if (item.RootName.Equals(DataTypeConvertHelper.PROJECT))
                    {
                        foreach (var project in item.Children)
                        {
                            if (project.OwnerId.Equals(AddNxlFileOpert.ProjectId.ToString()))
                            {
                                removeNode = project;
                                break;
                            }
                        }
                        if (removeNode != null)
                        {
                            item.Children.Remove(removeNode);
                        }
                        if (item.Children.Count < 1)
                        {
                            removeParentNode = item;
                        }
                        break;
                    }
                }
                if (removeParentNode != null)
                {
                    nodes.Remove(removeParentNode);
                }
            }
            else if (AddNxlFileOpert.OriginalFileRepo == SkydrmLocal.rmc.fileSystem.basemodel.EnumFileRepo.REPO_EXTERNAL_DRIVE)
            {
                CustomControls.components.TreeView.model.Node removeParentNode = null;
                foreach (var item in nodes)
                {
                    if (item.RootName.Equals(DataTypeConvertHelper.REPOSITORIES))
                    {
                        foreach (var drive in item.Children)
                        {
                            if (drive.OwnerId.Equals(AddNxlFileOpert.RepoId))
                            {
                                removeNode = drive;
                                break;
                            }
                        }
                        if (removeNode != null)
                        {
                            item.Children.Remove(removeNode);
                        }
                        if (item.Children.Count < 1)
                        {
                            removeParentNode = item;
                        }
                        break;
                    }
                }
                if (removeParentNode != null)
                {
                    nodes.Remove(removeParentNode);
                }
            }
            else if (AddNxlFileOpert.OriginalFileRepo == SkydrmLocal.rmc.fileSystem.basemodel.EnumFileRepo.REPO_MYVAULT)
            {
                foreach (var item in nodes)
                {
                    if (item.RootName.Equals(DataTypeConvertHelper.MY_SPACE) 
                        || item.RootName.Equals(DataTypeConvertHelper.MY_VAULT))
                    {
                        removeNode = item;
                        break;
                    }
                }
                if (removeNode != null)
                {
                    nodes.Remove(removeNode);
                }
            }

        }

        //use for FileDestSelectPage Positive Btn CanExcuteCommand
        private bool tempIsEnableFileDestSelectPagePositiveBtn = false;

        private void FileDestSelectPage_OnTreeViewItemSelectedChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null)
            {
                return;
            }
            CustomControls.components.TreeView.viewModel.TreeNodeViewModel treeNodeVM = e.NewValue as CustomControls.components.TreeView.viewModel.TreeNodeViewModel;

            CurrentSelectedSavePath selectedSavePath = new CurrentSelectedSavePath(treeNodeVM.RootName, treeNodeVM.PathId, treeNodeVM.PathDisplay, treeNodeVM.OwnerId);

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
                AddNxlFileOpert.CurrentSelectedSavePath = selectedSavePath;
            }
        }

        private void InitFDSelectPageCommand()
        {
            CommandBinding binding;

            binding = new CommandBinding(FDSelect_DataCommands.Positive);
            binding.CanExecute += SelectDestPositiveCommand_CanExecute;
            binding.Executed += SelectDestPositiveCommand;
            Host.CommandBindings.Add(binding);
            binding = new CommandBinding(FDSelect_DataCommands.Cancel);
            binding.Executed += CancelCommand;
            Host.CommandBindings.Add(binding);
        }
        private void SelectDestPositiveCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (fileDestSelectPage.ViewMode.ProtectLocation == ProtectLocation.CentralLocation
                && tempIsEnableFileDestSelectPagePositiveBtn)
            {
                e.CanExecute = true;
            }
        }
        private void SelectDestPositiveCommand(object sender, ExecutedRoutedEventArgs e)
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
            HistoryStack.Push(fileDestSelectPage);
            Host.frm.Content = fileRightsPreviewPage;
            if (HistoryStack.Count > 0)
            {
                BackBtnVisible = System.Windows.Visibility.Visible;
            }

        }
        #endregion

        #region Init FileRightsPreviewPage
        private void InitFRPreviewPageViewModel()
        {
            // caption
            int fileCount = AddNxlFileOpert.FileInfo.FileName.Length;
            fileRightsPreviewPage.ViewMode.CaptionDescVisible = System.Windows.Visibility.Collapsed;
            if (AddNxlFileOpert.FileInfo.FromSource == model.FileFromSource.SkyDRM_Window_Button)
            {
                fileRightsPreviewPage.ViewMode.Caption4VM.Title = CultureStringInfo.ApplicationFindResource("AddNxlOperation_Win_Title_AddProtected");
            }
            else
            {
                fileRightsPreviewPage.ViewMode.Caption4VM.Title = CultureStringInfo.ApplicationFindResource("AddNxlOperation_Win_Title");
            }
            fileRightsPreviewPage.ViewMode.Caption4VM.FileCount = fileCount;
            fileRightsPreviewPage.ViewMode.Caption4VM.FileNameList.Clear();
            for (int i = 0; i < fileCount; i++)
            {
                fileRightsPreviewPage.ViewMode.Caption4VM.FileNameList.Add(new CustomControls.components.FileModel()
                { Name = AddNxlFileOpert.FileInfo.FileName[i], FullName = fileFullPath });
            }

            fileRightsPreviewPage.ViewMode.BackBtnVisibility = System.Windows.Visibility.Collapsed;
            fileRightsPreviewPage.ViewMode.PositiveBtnContent = CultureStringInfo.ApplicationFindResource("AddNxlOperation_FRPreviewBtn_Add");

            InitFRPreviewPageSubViewModel();
        }
        private void InitFRPreviewPageSubViewModel()
        {
            var selectedSavePath = AddNxlFileOpert.CurrentSelectedSavePath;
            fileRightsPreviewPage.ViewMode.SavePathStpVisibility = System.Windows.Visibility.Visible;
            fileRightsPreviewPage.ViewMode.SavePathDesc = CultureStringInfo.ApplicationFindResource("AddNxlOperation_SavePath_Desc");
            fileRightsPreviewPage.ViewMode.SavePath = selectedSavePath.DestDisplayPath;

            // file is Adhoc
            if (AddNxlFileOpert.NxlType == model.NxlFileType.Adhoc)
            {
                fileRightsPreviewPage.ViewMode.AdhocAndClassifiedRightsVM.FileType = CustomControls.components.FileType.Adhoc;
                bool isAddWarterMark = !string.IsNullOrWhiteSpace(AddNxlFileOpert.NxlAdhocWaterMark.text);
                fileRightsPreviewPage.ViewMode.AdhocAndClassifiedRightsVM.AdhocRightsVM.FileRights = DataTypeConvertHelper.SDKRights2CustomControlRights(AddNxlFileOpert.NxlRights.ToArray(), isAddWarterMark);
                fileRightsPreviewPage.ViewMode.AdhocAndClassifiedRightsVM.AdhocRightsVM.WatermarkValue = AddNxlFileOpert.NxlAdhocWaterMark.text;
                fileRightsPreviewPage.ViewMode.AdhocAndClassifiedRightsVM.AdhocRightsVM.WaterPanlVisibility = AddNxlFileOpert.NxlRights.Contains(SkydrmLocal.rmc.sdk.FileRights.RIGHT_WATERMARK) ?
                     System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                DataTypeConvertHelper.SdkExpiry2CustomCtrExpiry(AddNxlFileOpert.NxlExpiration, out string expiryDate, false);
                fileRightsPreviewPage.ViewMode.AdhocAndClassifiedRightsVM.AdhocRightsVM.ValidityValue = expiryDate;
                return;
            }

            // file is CentralPolicy
            fileRightsPreviewPage.ViewMode.AdhocAndClassifiedRightsVM.FileType = CustomControls.components.FileType.CentralPolicy;

            // think about do this in background, use task await. And In AddNxlFile will use FingerPrint, actually if file is centralPolicy and FingerPrint.Rights is CentralPolicy rights, 
            // maybe should remove this invoke.
            List<SkydrmLocal.rmc.sdk.FileRights> listRights = new List<SkydrmLocal.rmc.sdk.FileRights>();
            SkydrmLocal.rmc.sdk.WaterMarkInfo warterMark = new SkydrmLocal.rmc.sdk.WaterMarkInfo();
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
                listRights = AddNxlFileOpert.PreviewRightsByCentralPolicy(id, AddNxlFileOpert.NxlTags, out warterMark);
                Host.Cursor = Cursors.Arrow;
            }

            tagPreivewWarterMark = warterMark;
            fileRightsPreviewPage.ViewMode.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.CentralTag = AddNxlFileOpert.NxlTags;
            fileRightsPreviewPage.ViewMode.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.CentralTagScrollViewMargin = new System.Windows.Thickness(78, 0, 0, 0);

            if (listRights.Count > 0)
            {
                fileRightsPreviewPage.ViewMode.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.AccessDenyVisibility = System.Windows.Visibility.Collapsed;
                bool isAddWarterMark = !string.IsNullOrWhiteSpace(warterMark.text);
                fileRightsPreviewPage.ViewMode.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.RightsDisplayVM.FileRights = DataTypeConvertHelper.SDKRights2CustomControlRights(listRights.ToArray(), isAddWarterMark, false);
                fileRightsPreviewPage.ViewMode.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.RightsDisplayVM.WatermarkValue = warterMark.text;
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
            if (AddNxlFileOpert.NxlType == model.NxlFileType.Adhoc)
            {
                // file is Adhoc
                if (CheckNxlExpired())
                {
                    App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("AddNxlOperation_FileExpired"), false);
                    return;
                }
            }

            StartCheckNxlFile_BgWorker();
        }
        #endregion

        #region Init FileRightsResultPage
        private void InitFRResultPageViewModel()
        {
            // caption
            int fileCount = AddNxlFileOpert.FileInfo.FileName.Length;

            fileRightsResultPage.ViewMode.Caption3VM.Title = CultureStringInfo.ApplicationFindResource("AddNxlOperation_Win_Title");
            fileRightsResultPage.ViewMode.Caption3VM.PromptText = fileCount > 1 ?
                CultureStringInfo.ApplicationFindResource("ProtectOperation_PromptHave") : //For multiple files protect title display.
                CultureStringInfo.ApplicationFindResource("ProtectOperation_PromptHas"); //For single file protect title display.;

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < fileCount; i++)
            {
                sb.Append(AddNxlFileOpert.FileInfo.FileName[i]);
                if (i != fileCount - 1)
                {
                    sb.Append(";\n");
                }
            }
            fileRightsResultPage.ViewMode.Caption3VM.FileName = sb.ToString();
            fileRightsResultPage.ViewMode.Caption3VM.Desitination = AddNxlFileOpert.CurrentSelectedSavePath.DestDisplayPath;

            if (AddNxlFileOpert.NxlType == model.NxlFileType.Adhoc)
            {
                // file is Adhoc
                fileRightsResultPage.ViewMode.AdhocAndClassifiedRightsVM.FileType = CustomControls.components.FileType.Adhoc;
                bool isAddWarterMark = !string.IsNullOrWhiteSpace(AddNxlFileOpert.NxlAdhocWaterMark.text);
                fileRightsResultPage.ViewMode.AdhocAndClassifiedRightsVM.AdhocRightsVM.FileRights = DataTypeConvertHelper.SDKRights2CustomControlRights(AddNxlFileOpert.NxlRights.ToArray(), isAddWarterMark);
                fileRightsResultPage.ViewMode.AdhocAndClassifiedRightsVM.AdhocRightsVM.WatermarkValue = AddNxlFileOpert.NxlAdhocWaterMark.text;
                fileRightsResultPage.ViewMode.AdhocAndClassifiedRightsVM.AdhocRightsVM.WaterPanlVisibility = AddNxlFileOpert.NxlRights.Contains(SkydrmLocal.rmc.sdk.FileRights.RIGHT_WATERMARK) ?
                     System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                DataTypeConvertHelper.SdkExpiry2CustomCtrExpiry(AddNxlFileOpert.NxlExpiration, out string expiryDate, false);
                fileRightsResultPage.ViewMode.AdhocAndClassifiedRightsVM.AdhocRightsVM.ValidityValue = expiryDate;
            }
            else
            {
                // file is CentralPolicy
                fileRightsResultPage.ViewMode.AdhocAndClassifiedRightsVM.FileType = CustomControls.components.FileType.CentralPolicy;

                var frPreClassifiedRightsVM = fileRightsPreviewPage.ViewMode.AdhocAndClassifiedRightsVM.ClassifiedRightsVM;
                fileRightsResultPage.ViewMode.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.CentralTag = AddNxlFileOpert.NxlTags; // frPreClassifiedRightsVM.CentralTag
                fileRightsResultPage.ViewMode.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.AccessDenyVisibility = frPreClassifiedRightsVM.AccessDenyVisibility;

                if (frPreClassifiedRightsVM.AccessDenyVisibility == System.Windows.Visibility.Visible)
                {
                    return;
                }
                fileRightsResultPage.ViewMode.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.RightsDisplayVM.RightsList = frPreClassifiedRightsVM.RightsDisplayVM.RightsList;
                fileRightsResultPage.ViewMode.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.RightsDisplayVM.WatermarkValue = frPreClassifiedRightsVM.RightsDisplayVM.WatermarkValue;
                fileRightsResultPage.ViewMode.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.RightsDisplayVM.WaterPanlVisibility = frPreClassifiedRightsVM.RightsDisplayVM.WaterPanlVisibility;
                fileRightsResultPage.ViewMode.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.RightsDisplayVM.ValidityPanlVisibility = frPreClassifiedRightsVM.RightsDisplayVM.ValidityPanlVisibility;
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

        private bool CheckNxlExpired()
        {
            bool result = false;
            var sdkExpiry = AddNxlFileOpert.NxlExpiration;
            if (sdkExpiry.type != SkydrmLocal.rmc.sdk.ExpiryType.NEVER_EXPIRE && DateTimeHelper.DateTimeToTimestamp(DateTime.Now) > sdkExpiry.End)
            {
                result = true;
            }
            return result;
        }

        private void StartDoAddNxl_BgWorker(object argument)
        {
            if (!DoAddNxlFile_BgWorker.IsBusy)
            {
                DoAddNxlFile_BgWorker.RunWorkerAsync(argument);

                // display progrss bar UI
                GridProBarVisible = System.Windows.Visibility.Visible;

                MenuDisableMgr.GetSingleton().IsProtecting = true;
                MenuDisableMgr.GetSingleton().MenuDisableNotify("LOGOUT", true);
                MenuDisableMgr.GetSingleton().MenuDisableNotify("EXIT", true);
            }
        }

        private void StartCheckNxlFile_BgWorker()
        {
            if (CheckNxlFileExists_BgWorker.IsBusy)
            {
                return;
            }
            // display progrss bar UI
            GridProBarVisible = System.Windows.Visibility.Visible;

            CheckNxlFileExists_BgWorker.RunWorkerAsync();
        }

        //private void DoAddNxlFile_BgWorker_DoWork(object sender, DoWorkEventArgs e)
        //{
        //    bool result = false;

        //    bool decryptRes = AddNxlFileOpert.DecryptNxlFile(out string decryptNxlPath);
        //    if (decryptRes)
        //    {
        //        SkydrmLocal.rmc.sdk.WaterMarkInfo warterMark = new SkydrmLocal.rmc.sdk.WaterMarkInfo();
        //        if (AddNxlFileOpert.NxlType == model.NxlFileType.Adhoc)
        //        {
        //            warterMark = AddNxlFileOpert.NxlAdhocWaterMark;
        //        }
        //        else
        //        {
        //            warterMark = tagPreivewWarterMark;
        //            // fix Bug 63450 - local app crash when add file to main window
        //            // Some attributes of the watermark obtained from the SDK are null,
        //            // In sdkwrapper not judge string is null, need re-set string is empty.
        //            if (string.IsNullOrEmpty(warterMark.text))
        //            {
        //                warterMark.text = "";
        //            }
        //            warterMark.fontColor = "";
        //            warterMark.fontName = "";
        //        }
        //        string[] filePath = new string[1];
        //        filePath[0] = decryptNxlPath;

        //        var listNxlFile = AddNxlFileOpert.ProtectFile(filePath, AddNxlFileOpert.NxlRights, warterMark, AddNxlFileOpert.NxlExpiration, AddNxlFileOpert.NxlTags);

        //        if (listNxlFile.Count > 0)
        //        {
        //            createdFiles = listNxlFile;
        //            result = true;
        //        }
        //    }
        //    else
        //    {
        //        App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Common_System_Internal_Error"), false, "", "Add nxl file failed");
        //    }

        //    e.Result = result;
        //}
        //private void DoAddNxlFile_BgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        //{
        //    bool result = (bool)e.Result;
        //    GridProBarVisible = System.Windows.Visibility.Collapsed;

        //    MenuDisableMgr.GetSingleton().IsProtecting = false;
        //    if (MenuDisableMgr.GetSingleton().IsCanEnableMenu())
        //    {
        //        MenuDisableMgr.GetSingleton().MenuDisableNotify("LOGOUT", false);
        //        MenuDisableMgr.GetSingleton().MenuDisableNotify("EXIT", false);
        //    }

        //    if (AddNxlFileOpert.FileInfo.FailedFileName.Count > 0 && createdFiles.Count < 1)
        //    {
        //        // all file protect failed
        //        App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("ProtectOperation_Protect_Failed"), false, "", "Protect failed");

        //        if (AddNxlFileOpert.FileInfo.FromSource == model.FileFromSource.SkyDRM_Window_Button)
        //        {
        //            failedPage.ViewMode.Title = CultureStringInfo.ApplicationFindResource("AddNxlOperation_Win_Title_AddProtected");
        //        }
        //        else
        //        {
        //            failedPage.ViewMode.Title = CultureStringInfo.ApplicationFindResource("AddNxlOperation_Win_Title");
        //        }
        //        foreach (var item in AddNxlFileOpert.FileInfo.FailedFileName)
        //        {
        //            failedPage.ViewMode.FailedFileList.Add(new FailedFile() { FileName = item.Key, ErrorMsg = item.Value });
        //        }
        //        failedPage.ViewMode.FailedDesc = "File could not be added to";
        //        failedPage.ViewMode.FailedDest = AddNxlFileOpert.CurrentSelectedSavePath.DestDisplayPath;
        //        InitFailedPageCommand();

        //        Host.frm.Content = failedPage;
        //        BackBtnVisible = System.Windows.Visibility.Collapsed;

        //        AddNxlFileOpert.DeleteDecryptNxlFile();
        //        return;
        //    }
        //    else if (AddNxlFileOpert.FileInfo.FailedFileName.Count < 1 && createdFiles.Count > 0)
        //    {
        //        // all file protect success
        //        AddNxlFileOpert.AddLog();

        //        AddNxlFileOpert.DeleteDecryptNxlFile();
        //        App.MainWin.viewModel.GetCreatedFile(createdFiles);

        //        Host.Close();
        //    }
        //    else if (AddNxlFileOpert.FileInfo.FailedFileName.Count < 1 && createdFiles.Count < 1)
        //    {
        //        // no protect failed file, no protect success file. it's cancel protect file
        //        AddNxlFileOpert.DeleteDecryptNxlFile();
        //        return;
        //    }

        //}

        private void CheckNxlFileExists_BgWorker_DoWork(object sender, DoWorkEventArgs args)
        {
            bool exists = false;
            string suggestionName = AddNxlFileOpert.FileName;
            try
            {
                exists = AddNxlFileOpert.CheckNxlFileExists();

                if (exists)
                {
                    suggestionName = AddNxlFileOpert.AvailableDestFileName;
                }
            }
            catch (Exception e)
            {
                App.Log.Error("Error in CheckNxlFileExists:", e);
            }
            args.Result = new CheckNxlResult(exists, suggestionName);
        }

        private void CheckNxlFileExists_BgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs args)
        {
            GridProBarVisible = System.Windows.Visibility.Collapsed;
            var checkResult = (CheckNxlResult)args.Result;
            //Nxl file already exists in dest space.
            if (checkResult != null && checkResult.Exists)
            {
                string fileName = AddNxlFileOpert.FileName;
                string suggestionName = checkResult.AvailableFileName;

                var result = FileHelper.ShowReplaceDlg(fileName, suggestionName);
                // Replace.
                if (result == SkydrmLocal.rmc.ui.windows.CustomMessageBoxWindow.CustomMessageBoxResult.Positive)
                {
                    StartDoAddNxl_BgWorker(new AddNxlConfig("", true));
                } // Keep both.
                else if (result == SkydrmLocal.rmc.ui.windows.CustomMessageBoxWindow.CustomMessageBoxResult.Neutral)
                {
                    StartDoAddNxl_BgWorker(new AddNxlConfig(suggestionName, false));
                }// Cancel.
                else if (result == SkydrmLocal.rmc.ui.windows.CustomMessageBoxWindow.CustomMessageBoxResult.Negative)
                {
                    //Host.Close();
                }
            }
            else
            {
                StartDoAddNxl_BgWorker(new AddNxlConfig("", false));
            }
        }

        private void DoAddNxlFile_BgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            bool result = false;
            Dictionary<string, string> failedFileName = new Dictionary<string, string>();
            try
            {
                object args = e.Argument;
                if(args == null)
                {
                    result = false;
                    return;
                }
                if(args is AddNxlConfig)
                {
                    var config = args as AddNxlConfig;
                    AddNxlFileOpert.CopyNxlFile(config.FileName, config.Overwrite);
                    result = true;
                }
            }
            catch (Exception exp)
            {
                failedFileName.Add(AddNxlFileOpert.FileInfo.FileName[0], exp.Message);
                result = false;
            }

            AddNxlFileOpert.FileInfo.FailedFileName = failedFileName;
            e.Result = result;
        }

        private void DoAddNxlFile_BgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            bool result = (bool)e.Result;
            GridProBarVisible = System.Windows.Visibility.Collapsed;

            MenuDisableMgr.GetSingleton().IsProtecting = false;
            if (MenuDisableMgr.GetSingleton().IsCanEnableMenu())
            {
                MenuDisableMgr.GetSingleton().MenuDisableNotify("LOGOUT", false);
                MenuDisableMgr.GetSingleton().MenuDisableNotify("EXIT", false);
            }

            if (result)
            {
                Host.Close();
            }
            else
            {
                // all file protect failed
                App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("ProtectOperation_Protect_Failed"), false, "", "Protect failed");

                if (AddNxlFileOpert.FileInfo.FromSource == model.FileFromSource.SkyDRM_Window_Button)
                {
                    failedPage.ViewMode.Title = CultureStringInfo.ApplicationFindResource("AddNxlOperation_Win_Title_AddProtected");
                }
                else
                {
                    failedPage.ViewMode.Title = CultureStringInfo.ApplicationFindResource("AddNxlOperation_Win_Title");
                }
                foreach (var item in AddNxlFileOpert.FileInfo.FailedFileName)
                {
                    failedPage.ViewMode.FailedFileList.Add(new FailedFile() { FileName = item.Key, ErrorMsg = item.Value });
                }
                failedPage.ViewMode.FailedDesc = "File could not be added to";
                failedPage.ViewMode.FailedDest = AddNxlFileOpert.CurrentSelectedSavePath.DestDisplayPath;
                InitFailedPageCommand();

                Host.frm.Content = failedPage;
                BackBtnVisible = System.Windows.Visibility.Collapsed;
            }
        }

        class AddNxlConfig
        {
            public string FileName => fileName;
            public bool Overwrite => overwrite;

            string fileName;
            bool overwrite;

            public AddNxlConfig(string fileName, bool overwrite)
            {
                this.fileName = fileName;
                this.overwrite = overwrite;
            }
        }

        class CheckNxlResult
        {
            public bool Exists => exists;
            public string AvailableFileName => availableFileName;

            bool exists;
            string availableFileName;

            public CheckNxlResult(bool exists, string name)
            {
                this.exists = exists;
                this.availableFileName = name;
            }
        }

    }
}
