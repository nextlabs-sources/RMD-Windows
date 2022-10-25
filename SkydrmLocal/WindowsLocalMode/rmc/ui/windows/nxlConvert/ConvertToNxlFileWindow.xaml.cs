using SkydrmLocal.rmc.common.component;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.fileSystem.project;
using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.ui.components.RightsDisplay.model;
using SkydrmLocal.rmc.ui.components.ValiditySpecify.model;
using SkydrmLocal.rmc.ui.pages.model;
using SkydrmLocal.rmc.ui.utils;
using SkydrmLocal.rmc.ui.windows.mainWindow.model;
using SkydrmLocal.rmc.ui.windows.nxlConvert.subs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SkydrmLocal.rmc.ui.windows.nxlConvert
{
    /// <summary>
    /// Interaction logic for ConvertToNxlFileWindow.xaml
    /// </summary>
    public partial class ConvertToNxlFileWindow : Window, INotifyPropertyChanged
    {
        private static readonly string MY_VAULT = CultureStringInfo.MainWin__TreeView_MyVault;

        private FileLocaltionDest fileLocaltionDest;
        private FileRightsSelect fileRightsSelect;

        private FileRightsDisplay fileRightsDisplay;

        // For UI display Error message
        private string errorMsg;

        // Transmit para
        private string savePath;
        private ProtectAndShareConfig tempConfig = new ProtectAndShareConfig();

        // For judge systemProject isEnable
        private bool sProjedctIsEnable;

        //for display ProBar
        private BackgroundWorker DoProtect_BgWorker = new BackgroundWorker();

        public bool IsClosing { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public ConvertToNxlFileWindow(FileOperation fileOperation, bool isCreateByMainWin = true, IMyProject myProject = null)
        {
            InitializeComponent();

            // Used to handle window display issue across processse(open the window from viewer process)
            this.Loaded += new RoutedEventHandler(delegate (object sender, RoutedEventArgs e)
            {
                this.Topmost = false;
                this.tempConfig.WinTag = this.Tag;
            });

            tempConfig.FileOperation = fileOperation;
            tempConfig.myProject = myProject;
            tempConfig.sProject = SkydrmLocalApp.Singleton.SystemProject;
            // Get systemProject isEnable
            sProjedctIsEnable = tempConfig.sProject.IsFeatureEnabled;

            // Get last selected status
            //SkydrmLocalApp.Singleton.Config.GetRadioChecked();           
            
            //Initialize save path
            InitializeSavePath();

            // FileCapDesc
            InitFileCapDesc(fileOperation);

            // FileLocaltion
            InitFileLocaltionDest(fileOperation);

            // FileRightsSelect
            InitFileRightsSelect(fileOperation);

            // FileRightsDisplay
            InitFileRightsDisplay();

            // Init each page config
            SetPageConfig(fileOperation, isCreateByMainWin);

            //init BackgroundWorker
            DoProtect_BgWorker.WorkerReportsProgress = true;
            DoProtect_BgWorker.WorkerSupportsCancellation = true;
            DoProtect_BgWorker.DoWork += DoProtect_Handler;
            DoProtect_BgWorker.RunWorkerCompleted += DoProtectCompleted_Handler;         

        }

        /// <summary>
        /// Initialize save path according to MainWin.viewModel.CurrentSaveFilePath
        /// </summary>
        private void InitializeSavePath()
        {
            if (!string.IsNullOrEmpty(SkydrmLocalApp.Singleton.MainWin.viewModel.CurrentSaveFilePath))
            {
                savePath = SkydrmLocalApp.Singleton.MainWin.viewModel.CurrentSaveFilePath;
            }
            else//when CurrentSaveFilePath="",user click project in MainWin and protect file by nxrmshell
            {
                savePath = MY_VAULT;
            }
        }

        #region Set each page config
        /// <summary>
        /// Initializes the Settings for the individual pages of each component
        /// </summary>
        /// <param name="fileOperation"></param>
        /// <param name="isCreateByMainWin"></param>
        private void SetPageConfig(FileOperation fileOperation, bool isCreateByMainWin)
        {
            // Action by MainWindow, init fileRights page config
            if (isCreateByMainWin)
            {
                InitFileRightsConfig();
                DisplayFrameBody(false);
            }
            else
            {
                // Protect by Plug-in, init fileLocaltion page config
                if (fileOperation.Action == FileOperation.ActionType.Protect)
                {
                    InitFileLocaltionConfig();
                    DisplayFrameBody(true);
                }
                else  // Share by Plug-in
                {
                    InitFileRightsConfigByPlugIn();
                    DisplayFrameBody(false);
                }
            }

        }

        #region Process FilelocationDest compent
        /// <summary>
        ///  Process fileLocationDest compent
        /// </summary>
        private void InitFileLocaltionConfig()
        {
            // Get last selected from registery.
            //SkydrmLocalApp.Singleton.Config.GetRadioChecked();
            bool isCentralLocation = SkydrmLocalApp.Singleton.User.IsCentralLocationRadio;
            InitFileLocaltionConfig(isCentralLocation);      
        }
        private void InitFileLocaltionConfig(bool central)
        {
            // Judge file protected to myVault or project.
            if (IsFromMyVault(savePath))
            {
                tempConfig.myProject = null;
            }
            else
            {
                if (tempConfig.myProject == null)
                {
                    tempConfig.myProject = GetMyProject();
                }
            }

            //Process central location page.
            InitPageCentralLocationConfig(tempConfig.myProject);

            //Process local location page.
            string localpath = GetFileDirectory(tempConfig.FileOperation.FilePath);
            InitPageLocalDriveConfig(localpath, tempConfig.myProject);

            //If central location is selected. Firstly set radio will trigger OnChecked event
            if (central)
            {
                fileLocaltionDest.SetCentralRadio(true);
            }
            else
            {
                fileLocaltionDest.SetLocalRadio(true);
            }

            // If don't have systemProject, should hidden local drive page.
            if (!sProjedctIsEnable)
            {
                fileLocaltionDest.SetCentralRadio(true);
                fileLocaltionDest.SetLocalRadio(false, false);
            }
        }
        private void InitPageCentralLocationConfig(IMyProject project)
        {
            //Get save path which will be used to natigate tree view item.
            string tempSavePath = savePath;

            // set treeView defult selected in central location page
            fileLocaltionDest.ProcessCentral_TreeViewSelected(tempSavePath, project);
        }
        private void InitPageLocalDriveConfig(string localpath, IMyProject project)
        {
            bool systemProjectIsEnabled = sProjedctIsEnable;
            if (!systemProjectIsEnabled)
            {
                if (false)
                {
                    fileLocaltionDest.ProcessLocal_TreeViewVisible(Visibility.Visible);
                    fileLocaltionDest.ProcessLocal_TreeViewSelected(savePath, project);
                }
                else
                {
                    fileLocaltionDest.ProcessLocal_CbVisible(Visibility.Visible);
                    fileLocaltionDest.ProcessLocal_CbItemSource(null);
                    fileLocaltionDest.ProcessLocal_CbSelectItem(project);
                }
            }

            //Set loca path display text.
            fileLocaltionDest.LocalPath = localpath;
        }
        #endregion

        #region Process FileRightsSelect compent
        /// <summary>
        /// Protect or Share file by MainWindow, Process fileRightsSelect compent.
        /// </summary>
        private void InitFileRightsConfig()
        {            
            // Don't display changeDestination btn when share.
            if (tempConfig.FileOperation.Action==FileOperation.ActionType.Share)
            {
                savePath = MY_VAULT;
                fileRightsSelect.ChangDestVisible = Visibility.Collapsed;
            }

            //Keep cpy of savePath in rights page[for ui display].
            fileRightsSelect.Path = savePath;

            // Judge file protected to myVault or project.
            if (IsFromMyVault(savePath))
            {
                fileRightsSelect.SetAdhocRadio(true);
                fileRightsSelect.SetCentralPlcRadio(false, false);
                //fileRightsSelect.DescAndRbVisible = Visibility.Collapsed;
            }
            else
            {
                if (tempConfig.myProject == null)
                {
                    tempConfig.myProject = GetMyProject();
                }

                // If protect to project, not share right
                fileRightsSelect.ProcessAdHocPage(false, false);

                // set select radio
                SetFileRightsRadio(tempConfig.myProject);

                fileRightsSelect.ProcessCentralPage(tempConfig.myProject, false);

                // For get rights from tags
                tempConfig.ProjectId = tempConfig.myProject.Id;
            }            
        }

        /// <summary>
        /// Share file by plug-in, init fileRightsSelect compent.
        /// </summary>
        private void InitFileRightsConfigByPlugIn()
        {
            //Keep cpy of savePath in rights page[for ui display].
            savePath = MY_VAULT;
            fileRightsSelect.Path = savePath;

            // Don't display changeDestination btn.
            fileRightsSelect.ChangDestVisible = Visibility.Collapsed;

            fileRightsSelect.SetAdhocRadio(true);
            fileRightsSelect.SetCentralPlcRadio(false, false);
            //fileRightsSelect.DescAndRbVisible = Visibility.Collapsed;       
        }
        #endregion

        #region Process FileRightsDisplay compent
        private bool InitFileRightsDisplayConfig()
        {
            bool invoke = true;
            try
            {
                if (tempConfig.FileOperation.Action == FileOperation.ActionType.Protect)
                {
                    fileRightsDisplay.SetOkBtnContent(CultureStringInfo.CreateFileWin_Btn_Protect);
                }

                if (tempConfig.LocalDriveIsChecked)//systemBucket
                {
                    fileRightsDisplay.SetSectionTitle(CultureStringInfo.FileTagRightDisplay_SysBucktTitle);
                }
                else
                {
                    fileRightsDisplay.SetProjectName(tempConfig.myProject.DisplayName);
                }
                
                // Inoke sdk api, get rights
                Dictionary<FileRights, List<WaterMarkInfo>> rightsAndWatermarks;
                SkydrmLocalApp.Singleton.Rmsdk.User.GetFileRightsFromCentalPolicyByProjectId(tempConfig.ProjectId, tempConfig.UserSelectTags,
                    out rightsAndWatermarks);

                List<FileRights> fileRights = new List<FileRights>();
                foreach (var item in rightsAndWatermarks.Keys)
                {
                    fileRights.Add(item);
                }

                List<string> rights = new List<string>();
                List<RightsItem> rightsItems = new List<RightsItem>();
                if (fileRights.Count != 0)
                {
                    rights = NxlHelper.Helper_GetRightsStr(fileRights, false, false);
                    rightsItems = CommonUtils.GetRightsIcon(rights, false);
                }

                // Set TagRights display
                fileRightsDisplay.SetTag(tempConfig.Tags);
                fileRightsDisplay.SetTagRights(rightsItems, null, null, Visibility.Collapsed, Visibility.Collapsed);
                fileRightsDisplay.ActualRights = rights;
                
            }
            catch (Exception e)
            {
                invoke = false;
                SkydrmLocalApp.Singleton.Log.Error("Error in InitFileRightsDisplayConfig:", e);
                SkydrmLocalApp.Singleton.ShowBalloonTip(CultureStringInfo.Common_System_Internal_Error);
            }

            return invoke;
        }
        #endregion

        private bool IsFromMyVault(string path)
        {
            return !string.IsNullOrEmpty(path) && path == MY_VAULT;
        }

        /// <summary>
        /// If myProject is null in ConvertToNxlFileWin.tempConfig, will get value from MainWin.
        /// </summary>
        private IMyProject GetMyProject()
        {
            try
            {
                return SkydrmLocalApp.Singleton.MainWin.viewModel.projectRepo.SelectedSaveProject;
            }
            catch (Exception e)
            {
                SkydrmLocalApp.Singleton.Log.Error("app.MainWin.viewModel.projectRepo.SelectedSaveProject.ProjectInfo.Raw is error", e);
            }
            return null;
        }

        /// <summary>
        /// Display location page or rights page in frame content.
        /// </summary>
        /// <param name="showLocationPage">true means load location page into frame content | false means load rights page.</param>
        private void DisplayFrameBody(bool showLocationPage)
        {
            if (showLocationPage)
            {
                //Frame load location page.
                fm_Body.Visibility = Visibility.Visible;
                //Frame load rights page.
                fm_Body2.Visibility = Visibility.Collapsed;
            }
            else
            {
                fm_Body.Visibility = Visibility.Collapsed;
                fm_Body2.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// For display tag rights preview
        /// </summary>
        /// <param name="isRightSelect"></param>
        private void FrameBody2Switch(bool isRightSelect)
        {
            if (isRightSelect)
            {
                fm_Body2.Content = fileRightsSelect;
            }
            else
            {
                fm_Body2.Content = fileRightsDisplay;
            }
        }
        #endregion

        #region Common Method
        private string GetFileDirectory(string[] path)
        {
            if (path.Length < 1)
            {
                return "";
            }
            string fileDirectory = Alphaleonis.Win32.Filesystem.Path.GetDirectoryName(path[0]);
            if (!fileDirectory.EndsWith(Alphaleonis.Win32.Filesystem.Path.DirectorySeparatorChar.ToString()))
            {
                fileDirectory = fileDirectory + Alphaleonis.Win32.Filesystem.Path.DirectorySeparatorChar;
            }
            return fileDirectory;
        }

        /// <summary>
        /// Set fileRights Radio by last selected. Last selected defult value is central radio
        /// And set adhoc radio from (field)IsEnableAdHoc.
        /// </summary>
        /// <param name="project">myProject</param>
        private void SetFileRightsRadio(IMyProject project)
        {
            if (project==null)
            {
                SkydrmLocalApp.Singleton.Log.Info("ConvertToNxlFileWindow SetFileRightsRadio method param project is null");
                return;
            }

            //ProjectClassification[] projectClassifications = project.ListClassifications();
            //if (null == projectClassifications || projectClassifications.Length == 0)
            //{
            //    fileRightsSelect.SetAdhocRadio(true);
            //    fileRightsSelect.SetCentralPlcRadio(false, false);
            //}
            //else
            //{
            //    fileRightsSelect.SetCentralPlcRadio(true);
            //}

            // Get last selected.
            bool isCentral = SkydrmLocalApp.Singleton.User.IsCentralPlcRadio;
            if (!isCentral && project.IsEnableAdHoc)
            {
                fileRightsSelect.SetAdhocRadio(true);
                fileRightsSelect.SetCentralPlcRadio(false, true);
            }
            else
            {
                fileRightsSelect.SetCentralPlcRadio(true);
            }
            
            // IsEnableAdHoc
            if ( !project.IsEnableAdHoc)
            {               
                fileRightsSelect.SetAdhocRadio(false, false);
            }

        }

        /// <summary>
        /// Set fileRights Radio by last selected. Last selected defult value is central radio
        /// And set adhoc radio from (field)IsEnableAdHoc.
        /// </summary>
        /// <param name="sProject">systemProject</param>
        private void SetFileRightsRadio(ISystemProject sProject)
        {
            if (sProject == null)
            {
                SkydrmLocalApp.Singleton.Log.Info("ConvertToNxlFileWindow SetFileRightsRadio method param project is null");
                return;
            }

            // Get last selected.
            bool isCentral = SkydrmLocalApp.Singleton.User.IsCentralPlcRadio;
            if (!isCentral && sProject.IsEnableAdHoc)
            {
                fileRightsSelect.SetAdhocRadio(true);
                fileRightsSelect.SetCentralPlcRadio(false, true);
            }
            else
            {
                fileRightsSelect.SetCentralPlcRadio(true);
            }

            // IsEnableAdHoc
            if (!sProject.IsEnableAdHoc)
            {
                fileRightsSelect.SetAdhocRadio(false, false);
            }

        }

        /// <summary>
        ///  Control FileRightsSelect UI display by savePath and myProject. 
        /// </summary>
        /// <param name="path">save file path</param>
        /// <param name="project">myProject</param>
        private void SetFileRightsPage(string path, IMyProject project)
        {
            fileRightsSelect.Path = path;

            // Protect to myVault
            if (project == null)
            {
                fileRightsSelect.ProcessAdHocPage(true, true);

                fileRightsSelect.SetAdhocRadio(true);
                fileRightsSelect.SetCentralPlcRadio(false, false);
                //fileRightsSelect.DescAndRbVisible = Visibility.Collapsed;
            }
            else
            {
                // If protected to project,don't have share right
                fileRightsSelect.ProcessAdHocPage(false, true);

                //display textblock and radioBtn
                fileRightsSelect.DescAndRbVisible = Visibility.Visible;

                SetFileRightsRadio(project);

                fileRightsSelect.ProcessCentralPage(project, true);

                // For get rights from tags
                tempConfig.ProjectId = tempConfig.myProject.Id;

            }
        }

        /// <summary>
        /// Control FileRightsSelect UI display by savePath and systemProject. 
        /// </summary>
        /// <param name="path">save file path</param>
        /// <param name="sProject">systemProject</param>
        private void SetFileRightsPage(string path, ISystemProject sProject)
        {
            fileRightsSelect.Path = path;

            // Protect to systemProject
            // If protected to project,don't have share right
            fileRightsSelect.ProcessAdHocPage(false, true);

            //display textblock and radioBtn
            fileRightsSelect.DescAndRbVisible = Visibility.Visible;

            SetFileRightsRadio(sProject);

            fileRightsSelect.ProcessCentralPage(sProject, true);

            // For get rights from tags
            tempConfig.ProjectId = tempConfig.sProject.Id;
        }
        #endregion

        #region Init FileCapDesc compent
        private void InitFileCapDesc(FileOperation fileOperation)
        {
            //Append file name string from FileOperation.FileName string[].
            StringBuilder sb = new StringBuilder();
            int length = fileOperation.FileName.Length;
            this.Header.FileCount = length;
            for (int i = 0; i < length; i++)
            {
                sb.Append(fileOperation.FileName[i]);
                if (i != length - 1)
                {
                    sb.Append(";\r");
                }
            }
            //Bind to windowConfigs.FileName property and notify UI changed.
            this.Header.FilesName = sb.ToString();
            //Protect view section.
            if (fileOperation.Action == FileOperation.ActionType.Protect)
            {
                this.Header.Title = length > 1 ?
                    CultureStringInfo.CreateFileWin__Operation_Title_MProtect : //For multiple files protect title display.
                    CultureStringInfo.CreateFileWin__Operation_Title_Protect; //For single file protect title display.
            }
            else  //Share view section.
            {
                this.Header.Title = length > 1 ?
                    CultureStringInfo.CreateFileWin_Operation_Title_MShare : //For multiple files share title dispaly.
                    CultureStringInfo.CreateFileWin_Operation_Title_Share; //For single file share title display.
            }
        }

        private void ChangeFile_MouseLeftBtn(object sender, MouseButtonEventArgs e)
        {
            FileOperation tempFileOperation = tempConfig.FileOperation;

            //init Dir.
            string filePath;
            string[] selectedFile = null;

            try
            {
                // --- Also can use System.Windows.Forms.FolderBrowserDialog!
                Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
                dialog.Title = "Select a File";

                filePath = tempFileOperation.FilePath[0].Substring(0,
                    tempFileOperation.FilePath[0].LastIndexOf(Alphaleonis.Win32.Filesystem.Path.DirectorySeparatorChar) + 1);

                if (!Directory.Exists(filePath))
                {
                    // Get system desktop dir.
                    filePath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory); ;
                }
                dialog.InitialDirectory = filePath; // set init Dir.

                // all files
                dialog.Filter = "All Files|*.*";
                dialog.Multiselect = true;

                if (dialog.ShowDialog() == true) // when user click ok button
                {                
                    //fileName = dialog.SafeFileName;
                    selectedFile = dialog.FileNames;

                    if (!CommonUtils.CheckFilePathDoProtect(selectedFile, out string tag, out List<string>rightFilePath))
                    {
                        return;
                    }

                    FileOperation operation = new FileOperation(rightFilePath.ToArray(), tempFileOperation.Action);
                    StringBuilder sb = new StringBuilder();
                    int length = operation.FileName.Length;

                    for (int i = 0; i < length; i++)
                    {
                        sb.Append(operation.FileName[i]);
                        if (i != length - 1)
                        {
                            sb.Append(";\r");
                        }
                    }
                    //Bind to windowConfigs.FileName property and notify UI changed.
                    Header.FileCount = length;
                    Header.FilesName = sb.ToString();

                    tempConfig.FileOperation = operation;

                    // Set fileLocalDest Path
                    if (fileLocaltionDest.rb_Local.IsChecked == true)
                    {
                        fileLocaltionDest.LocalPath = GetFileDirectory(selectedFile);
                        fileLocaltionDest.Path = GetFileDirectory(selectedFile);
                    }
                    
                }

            }
            catch (Exception msg)
            {
                SkydrmLocalApp.Singleton.Log.Warn("Exception in OpenFileDialog," + msg, msg);
            }
        }
        #endregion

        #region Init FileLocaltionDest compent
        private void InitFileLocaltionDest(FileOperation fileOperation)
        {
            if (fileOperation.Action==FileOperation.ActionType.Share)
            {
                return;
            }
            fileLocaltionDest = new FileLocaltionDest();
            fileLocaltionDest.OkBtnClicked += FileLocaltionDest_OkBtnClicked;
            fileLocaltionDest.CancelBtnClicked += CancelBtnClicked;
            fm_Body.Content = fileLocaltionDest;

            fileLocaltionDest.sp_Path.Margin = new Thickness(125,5,0,15);
        }

        private void FileLocaltionDest_OkBtnClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                savePath = fileLocaltionDest.Path;
                tempConfig.myProject = fileLocaltionDest.Project;
                tempConfig.LocalDriveIsChecked = fileLocaltionDest.LocalRadioChecked;

                // Check LocalDrive IsChecked status
                bool central_locationChecked = true;
                if (tempConfig.LocalDriveIsChecked)
                {
                    central_locationChecked = false;
                    // If systemProject is not null, set fileRightsPage by systemProjecrt. Otherwise set by myProject
                    if (sProjedctIsEnable)
                    {
                        SetFileRightsPage(savePath, tempConfig.sProject);
                    }
                    else
                    {
                        SetFileRightsPage(savePath, tempConfig.myProject);
                    }
                }
                else
                {
                    SetFileRightsPage(savePath, tempConfig.myProject);

                    // If user change destination,should not change mainWindow 'CurrentSaveFilePath'.Do second protect will display wrong path.
                    // But in ViewModelMainWin triggre 'HandleNewFiles' method need 'CurrentSaveFilePath' set sourPath, should use DoRefresh() method get new file.

                    // MainWin save CurrentSaveFilePath and SelectProject
                    //SkydrmLocalApp.Singleton.MainWin.viewModel.CurrentSaveFilePath = savePath;
                    SkydrmLocalApp.Singleton.MainWin.viewModel.projectRepo.SelectedSaveProject = tempConfig.myProject;
                }

                // Save location status
                //SkydrmLocalApp.Singleton.Config.SetRadioChecked(true, central_locationChecked);
                SkydrmLocalApp.Singleton.User.IsCentralLocationRadio = central_locationChecked;

                DisplayFrameBody(false);
            }
            catch (Exception msg)
            {
                SkydrmLocalApp.Singleton.Log.Error("Error in ConvertToNxlFileWindow When click select button", msg);
                //SkydrmLocalApp.Singleton.ShowBalloonTip(msg.Message);
            }
           
        }
        #endregion

        #region Init FileRightSelect compent
        private void InitFileRightsSelect(FileOperation fileOperation)
        {
            // In FileRightsSelect should identify is MyVault or Project
            fileRightsSelect = new FileRightsSelect();
            fileRightsSelect.ChangeDestClicked += FileRightsSelect_ChangeBtnClicked;
            fileRightsSelect.OkBtnClicked += FileRightsSelect_OkBtnClicked;
            fileRightsSelect.CancelBtnClicked += CancelBtnClicked;
            fileRightsSelect.RadioCheckChanged += FileRightsSelect_RadioCheckChanged;
            // Fix bug 53338
            fileRightsSelect.ValidityDateChanged += FileRightsSelect_ValidityDateChanged;

            fm_Body2.Content = fileRightsSelect;

            if (fileOperation.Action == FileOperation.ActionType.Protect)
            {
                fileRightsSelect.ProtectBtnContent = CultureStringInfo.CreateFileWin_Btn_Protect;
            }
            else
            {
                fileRightsSelect.ProtectBtnContent = CultureStringInfo.CreateFileWin_Btn_Share;
                fileRightsSelect.SetAdhocRadio(true);
                fileRightsSelect.SetCentralPlcRadio(false, false);
                //fileRightsSelect.DescAndRbVisible = Visibility.Collapsed;
            }

            fileRightsSelect.sp_Path.Margin = new Thickness(125,5,0,15);
        }

        private void FileRightsSelect_ValidityDateChanged(bool isValidDate)
        {
            if (isValidDate)
            {
                if (this.ProtectFailedText.Text.Equals(CultureStringInfo.Validity_DlgBox_Details))
                {
                    this.ProtectFailedText.Text = null;
                    this.ProtectFailedText.Visibility = Visibility.Collapsed;
                }              
            }
        }

        private void FileRightsSelect_RadioCheckChanged(object sender, RoutedEventArgs e)
        {
            RadioButton radioButton = sender as RadioButton;
            if (radioButton != null)
            {
                if (radioButton.Name.Contains("Adhoc"))
                {
                    Header.Description = CultureStringInfo.CreateFileWin_Operation_Info_ADhoc;
                    if (tempConfig.FileOperation.Action == FileOperation.ActionType.Protect)
                    {
                        fileRightsSelect.ProtectBtnContent = CultureStringInfo.CreateFileWin_Btn_Protect;
                    }
                }
                else
                {
                    Header.Description = CultureStringInfo.CreateFileWin_Operation_Info_Central;
                    if (tempConfig.FileOperation.Action == FileOperation.ActionType.Protect)
                    {
                        fileRightsSelect.ProtectBtnContent = CultureStringInfo.NxlFileToCvetWin_Btn_Next;
                    }
                    // fix bug 56400
                    this.ProtectFailedText.Text = null;
                    this.ProtectFailedText.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void FileRightsSelect_ChangeBtnClicked(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // Display fileLocalDest UI
                InitFileLocaltionConfig();

                // If first login app, open FileLocationDest page by mainWindow,should defult select Central_Location radio.
                //bool isFirstOpen = SkydrmLocalApp.Singleton.User.IsFirstProtectFile;
                //if (isFirstOpen)
                //{
                //    fileLocaltionDest.SetCentralRadio(true);
                //}

                if (Directory.Exists(savePath))
                {
                    fileLocaltionDest.SetLocalRadio(true);
                }
                else
                {
                    fileLocaltionDest.SetCentralRadio(true);
                }
                // Fix Bug 53112 
                if (fileLocaltionDest.rb_Local.IsChecked==true)
                {
                    fileLocaltionDest.LocalPath = fileLocaltionDest.Path;
                }

                DisplayFrameBody(true);
            }
            catch (Exception msg)
            {
                SkydrmLocalApp.Singleton.Log.Error("Display FileLocation page error:", msg);
            }
        }

        private void FileRightsSelect_OkBtnClicked(object sender, RoutedEventArgs e)
        {
            bool centralPolicyRadioIsChecked = fileRightsSelect.rb_Central.IsChecked == true ? true : false;
            bool isProtectToProject = !savePath.Equals(MY_VAULT);

            if (centralPolicyRadioIsChecked && isProtectToProject)
            {
                List<string> incorrectChoosedTags = fileRightsSelect.GetIncorrectSelectedTags();

                if (incorrectChoosedTags.Count != 0)
                {
                    //Fix bug 50560.
                    CustomMessageBoxWindow.Show(CultureStringInfo.Common_DlgBox_Title,
               CultureStringInfo.Common_DlgBox_Subject,
               CultureStringInfo.Common_Mandatory_Require,
               CustomMessageBoxWindow.CustomMessageBoxIcon.Warning,
               CustomMessageBoxWindow.CustomMessageBoxButton.BTN_CLOSE);
                    
                    return;
                }
            }

            //Set configures
            tempConfig.UserSelectTags = fileRightsSelect?.GetSelectedTags();
            tempConfig.Tags = fileRightsSelect?.GetSelectedTagsForUI();
            tempConfig.IsProtectToProject = isProtectToProject;
            tempConfig.SelectProjectFolderPath = savePath;
            tempConfig.CentralPolicyRadioIsChecked = centralPolicyRadioIsChecked;
            tempConfig.RightsSelectConfig = fileRightsSelect.AdHocRights;

            // Save centralPolicyRadio status
            SkydrmLocalApp.Singleton.User.IsCentralPlcRadio = centralPolicyRadioIsChecked;

            // check expiry
            bool result = CommonUtils.CheckExpiry(tempConfig, out Expiration expiration, out string message);
            if (!result)
            {
                this.ProtectFailedText.Visibility = Visibility.Visible;
                this.ProtectFailedText.Text = message;
                return;
            }

            if (tempConfig.FileOperation.Action == FileOperation.ActionType.Protect)
            {
                if (centralPolicyRadioIsChecked)
                {
                    if (!InitFileRightsDisplayConfig())
                    {
                        return;
                    }
                    FrameBody2Switch(false);
                }
                else
                {
                    //adHoc
                    StartBgWorker();
                }
            }
            else if(tempConfig.FileOperation.Action == FileOperation.ActionType.Share)
            {
                ShareWindow ShareWindow = new ShareWindow(tempConfig);
                ShareWindow.Owner = this;
                ShareWindow.ShowDialog();
            }
        }
        private void StartBgWorker()
        {
            //display ui ProBar           
            this.GridProBar.Visibility = Visibility.Visible;

            MenuDisableMgr.GetSingleton().MenuDisableNotify(CultureStringInfo.MenuItem_Logout, true);
            MenuDisableMgr.GetSingleton().MenuDisableNotify(CultureStringInfo.MenuItem_Exit, true);
            MenuDisableMgr.GetSingleton().IsProtecting = true;

            if (!DoProtect_BgWorker.IsBusy)
            {
                DoProtect_BgWorker.RunWorkerAsync();
            }
        }
        #endregion

        #region Init FileRightsDisplay
        private void InitFileRightsDisplay()
        {
            fileRightsDisplay = new FileRightsDisplay();
            fileRightsDisplay.OkBtnClicked += FileRightsDisplay_OkBtnClicked;
            fileRightsDisplay.BackBtnClicked += FileRightsDisplay_BackBtnClicked;
            fileRightsDisplay.CancelBtnClicked += FileRightsDisplay_CancelBtnClicked;

            fileRightsDisplay.sp_Title.Margin = new Thickness(0,10,0,0);
        }

        private void FileRightsDisplay_OkBtnClicked(object sender, RoutedEventArgs e)
        {
            tempConfig.RightsSelectConfig.Rights = fileRightsDisplay.ActualRights;
            StartBgWorker();
        }
        private void FileRightsDisplay_BackBtnClicked(object sender, RoutedEventArgs e)
        {
            FrameBody2Switch(true);
        }
        private void FileRightsDisplay_CancelBtnClicked(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        #endregion

        #region Do protection.
        private void DoProtect_Handler(object sender, DoWorkEventArgs e)
        {
            bool invoke = CommonUtils.ProtectOrShare(tempConfig, out errorMsg);

            e.Result = invoke;
        }

        private void DoProtectCompleted_Handler(object sender, RunWorkerCompletedEventArgs e)
        {
            bool invoke = (bool)e.Result;
            this.GridProBar.Visibility = Visibility.Collapsed;

            MenuDisableMgr.GetSingleton().IsProtecting = false;
            if (MenuDisableMgr.GetSingleton().IsCanEnableMenu())
            {
                MenuDisableMgr.GetSingleton().MenuDisableNotify(CultureStringInfo.MenuItem_Logout, false);
                MenuDisableMgr.GetSingleton().MenuDisableNotify(CultureStringInfo.MenuItem_Exit, false);
            }

            if (invoke)
            {
                //sdk will add a log, but we need to call upload explicitly
                SkydrmLocalApp.Singleton.User.UploadNxlFileLog_Async();


                // show protect success window.
                ProtectWindow protectWindow = new ProtectWindow(this,tempConfig);

                // Using modeless dialog, or else(using modal dialog) will crash when user open tray context menu at the same time -- fix bug 55248.
                protectWindow.Owner = this;
                this.IsEnabled = false;
                protectWindow.Show();
            }
            else
            {
                this.ProtectFailedText.Visibility = Visibility.Visible;
                this.ProtectFailedText.Text = errorMsg;
            }
        }
        #endregion

        private void CancelBtnClicked(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SkydrmLocalApp.Singleton.Log.Info("ConvertToNxlFileWin closing event, the menuDisable IsProtecting:"+ MenuDisableMgr.GetSingleton().IsProtecting.ToString());
            // during file-protecting , can't  be closed. 
            if (MenuDisableMgr.GetSingleton().IsProtecting)
            {
                e.Cancel = true;
                SkydrmLocalApp.Singleton.ShowBalloonTip(CultureStringInfo.CreateFileWin_Notify_Wait_Protect);
                // fixed bug 55854
                return;
            }

            IsClosing = true;
            SkydrmLocalApp.Singleton.Log.Info("ConvertToNxlFileWin closing event, the IsClosing:" + IsClosing.ToString());
        }

        /// <summary>
        ///  When set window SizeToContent(attribute),the WindowStartupLocation will failure
        ///  Use this method to display UI.
        /// </summary>
        /// <param name="sizeInfo"></param>
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            //Calculate half of the offset to move the form

            if (sizeInfo.HeightChanged)
                this.Top += (sizeInfo.PreviousSize.Height - sizeInfo.NewSize.Height) / 2;

            if (sizeInfo.WidthChanged)
                this.Left += (sizeInfo.PreviousSize.Width - sizeInfo.NewSize.Width) / 2;
        }

    }

}
