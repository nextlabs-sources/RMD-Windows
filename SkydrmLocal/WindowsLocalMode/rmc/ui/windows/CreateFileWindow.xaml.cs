using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.ComponentModel;
using SkydrmLocal.Pages;
using SkydrmLocal.rmc.ui.windows;
using SkydrmLocal.rmc.ui.pages.model;
using SkydrmLocal.rmc.ui.windows.mainWindow.model;
using System.Text.RegularExpressions;
using SkydrmLocal.rmc.ui.utils;
using System.Threading;
using System.Windows.Threading;
using SkydrmLocal.rmc.helper;
using SkydrmLocal.rmc;
using SkydrmLocal.rmc.ui;
using SkydrmLocal.rmc.ui.components.ValiditySpecify.model;
using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.fileSystem.myvault;
using SkydrmLocal.rmc.fileSystem.project;
using SkydrmLocal.rmc.common.component;
using Alphaleonis.Win32.Filesystem;
using System.Globalization;

namespace SkydrmLocal
{
    /// <summary>
    /// Interaction logic for CreateFileWindow.xaml
    /// </summary>
    public partial class CreateFileWindow : Window
    {
        private SkydrmLocalApp app = (SkydrmLocalApp)SkydrmLocalApp.Current;

        private static readonly string MY_VAULT = CultureStringInfo.MainWin__TreeView_MyVault;
        private static readonly string PROJECT = CultureStringInfo.MainWin__TreeView_Project;
        //All data config
        private WindowConfigs windowConfigs { get; }
       
        //for display ProBar
        private BackgroundWorker DoProtect_BgWorker = new BackgroundWorker();      

        public CreateFileWindow(FileOperation fileOperation, bool IsCreateByMainWin = true, IMyProject myProject= null)
        {
            InitializeComponent();

            // window title style
            //this.Loaded += delegate
            //{
            //    Logo.Visibility = Visibility.Visible;
            //    WinTitle.Visibility = Visibility.Collapsed;
            //};

            //Set user defined rb checked default.
            this.User_Defined_RadioButton.IsChecked = true;

            #region initialize windowConfigs
            windowConfigs = new WindowConfigs(this, fileOperation, IsCreateByMainWin, myProject);

            // Used to handle window display issue across processse(open the window from viewer process)
            this.Loaded += new RoutedEventHandler(delegate (object sender, RoutedEventArgs e)
            {
                this.Topmost = false;
                this.windowConfigs.tempConfig.WinTag = this.Tag;
            });
            //Bind window data context.
            this.DataContext = windowConfigs;
            #endregion

            //init BackgroundWorker
            DoProtect_BgWorker.WorkerReportsProgress = true;
            DoProtect_BgWorker.WorkerSupportsCancellation = true;
            DoProtect_BgWorker.DoWork += DoProtect_Handler;
            DoProtect_BgWorker.RunWorkerCompleted += DoProtectCompleted_Handler;

            //check NxlFile
            CheckNxlFile(fileOperation.FilePath);
        }

        private void CheckNxlFile(string[] path)
        {
            bool isNxlFile = false;

            for (int i = 0; i < path.Length; i++)
            {
                int startIndex = path[i].LastIndexOf('.');
                if (startIndex<0)
                {
                    continue;
                }
                if (path[i].Substring(startIndex).ToLower().Trim().Contains("nxl"))
                {
                    isNxlFile = true;                  
                    break;
                }
            }

            if (isNxlFile)
            {
                this.protectBtn.IsEnabled = false;
                this.ProtectFailedText.Text = CultureStringInfo.CreateFileWin_Notify_NxlFile_Not_Protect;
                //app.ShowBalloonTip(CultureStringInfo.CreateFileWin_Notify_NxlFile_Not_Protect);
            }
            else
            {
                this.protectBtn.IsEnabled = true;
                this.ProtectFailedText.Text = null;
            }
        }

        #region DoProtect_BackgroundWorker
        private void DoProtect_Handler(object sender, DoWorkEventArgs args)
        {
            bool invoke = CommonUtils.ProtectOrShare(windowConfigs.tempConfig, out windowConfigs.ErrorMsg);

            args.Result = invoke;
        }

        private void DoProtectCompleted_Handler(object sender, RunWorkerCompletedEventArgs args)
        {
            bool invoke = (bool)args.Result;
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
                app.User.UploadNxlFileLog_Async();


                // show protect success window.
                ProtectWindow protectWindow = new ProtectWindow(windowConfigs.tempConfig);
                // close current window
                this.Close();

                // Using modeless dialog, or else(using modal dialog) will crash when user open tray context menu at the same time.
                protectWindow.Show();

            }
            else
            {
                this.ProtectFailedText.Text = windowConfigs.ErrorMsg;
            }

        }
        #endregion

        #region UI event
        private void ChangeFile_MouseLeftBtn(object sender, MouseButtonEventArgs e)
        {
            FileOperation tempFileOperation = windowConfigs.tempConfig.FileOperation;

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

                    FileOperation operation = new FileOperation(selectedFile, tempFileOperation.Action);
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
                    windowConfigs.FileCount = length;
                    windowConfigs.FileName = sb.ToString();
                    windowConfigs.tempConfig.FileOperation = operation;

                    CheckNxlFile(selectedFile);
                }

            }
            catch (Exception msg)
            {
                SkydrmLocalApp.Singleton.Log.Warn("Exception in OpenFileDialog," + msg, msg);
            }
        }

        private void Button_CreateFile(object sender, RoutedEventArgs e)
        {
            bool companyDefinedRadioButtonIsChecked = Company_defined_RadioButton.IsChecked==true? true : false;
            bool isProtectToProject = !windowConfigs.SelectProjectFolderPath.Equals(MY_VAULT);

            if (companyDefinedRadioButtonIsChecked && isProtectToProject)
            {         
                List<string> incorrectChoosedTags = windowConfigs.pageSelectDocumentClassify.IsCorrectChooseClassification();

                if (incorrectChoosedTags.Count!=0)
                {
                    //Fix bug 50560.
                    MessageBox.Show("Mandatory categories require at least one classification label.");
                    return;
                }                  
            }

            //Set ShareWindow configures
            windowConfigs.tempConfig.UserSelectTags = windowConfigs.pageSelectDocumentClassify?.GetClassification();
            windowConfigs.tempConfig.Tags = windowConfigs.pageSelectDocumentClassify?.GetClassificationForUI();
            windowConfigs.tempConfig.IsProtectToProject = isProtectToProject;
            windowConfigs.tempConfig.SelectProjectFolderPath = windowConfigs.SelectProjectFolderPath;
            windowConfigs.tempConfig.CentralPolicyRadioIsChecked = companyDefinedRadioButtonIsChecked;


            if (this.windowConfigs.tempConfig.FileOperation.Action == FileOperation.ActionType.Protect)
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
            else
            {
                ShareWindow ShareWindow = new ShareWindow(windowConfigs.tempConfig);
                ShareWindow.Owner = this;
                ShareWindow.ShowDialog();
            }
            
        }

        private void Button_Cancel(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // during file-protecting , can't  be closed. 
            if (MenuDisableMgr.GetSingleton().IsProtecting)
            {
                e.Cancel = true;
                SkydrmLocalApp.Singleton.ShowBalloonTip(CultureStringInfo.CreateFileWin_Notify_Wait_Protect);
            }
        }

        private void User_Defined_RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (SelectDigitalRights.Visibility == Visibility.Hidden)
            {
                windowConfigs.OperationDesc = CultureStringInfo.CreateFileWin_Operation_Info_ADhoc;
                SelectDigitalRights.Visibility = Visibility.Visible;
            }
            if (SelectDocumentClassification.Visibility==Visibility.Visible)
            {
                SelectDocumentClassification.Visibility = Visibility.Hidden;
            }
        }

        private void Company_Defined_RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (SelectDocumentClassification.Visibility == Visibility.Hidden)
            {
                windowConfigs.OperationDesc = CultureStringInfo.CreateFileWin_Operation_Info_Central;
                SelectDocumentClassification.Visibility = Visibility.Visible;
            }

            if (SelectDigitalRights.Visibility == Visibility.Visible)
            {
                SelectDigitalRights.Visibility = Visibility.Hidden;
            }

        }

        private void ChangeDestination_MouseLeftBtn(object sender, MouseButtonEventArgs e)
        {
            try
            {
                SelectProjectFolderWindow selectProjectFolderWindow = new SelectProjectFolderWindow(windowConfigs.SelectProjectFolderPath,
                                                                                                                         windowConfigs.tempConfig.myProject);

                selectProjectFolderWindow.PathChangedEvent += (ss, ee) =>
                {
                    DisplaySelectProjectFolderPath(ee.TransmitPath, ee.TransmitWorkingProject);
                };

                selectProjectFolderWindow.ShowDialog();
            }
            catch (Exception msg)
            {
                app.Log.Error("SelectProjectFolderWindow error:", msg);
            }
            
        }

        private void DisplaySelectProjectFolderPath(string selectpath, ProjectData myProjectData)
        {
            windowConfigs.SelectProjectFolderPath = selectpath;

            //MainWin save Selectpath and Selectproject
            app.MainWin.viewModel.CurrentSaveFilePath = selectpath;
            app.MainWin.viewModel.projectRepo.SelectedSaveProject = myProjectData.ProjectInfo.Raw;

            if (selectpath != MY_VAULT)
            {
                //Page Select rights
                windowConfigs.pageSelectDigitalRights = new PageSelectDigitalRights();
                windowConfigs.pageSelectDigitalRights.SetShareIsEnable(false);
                this.windowConfigs.tempConfig.RightsSelectConfig = windowConfigs.pageSelectDigitalRights.RightsSelectConfig;
                this.SelectDigitalRights.Content = windowConfigs.pageSelectDigitalRights;

                this.User_Defined_RadioButton.IsChecked = true;

                //display textblock and radioBtn
                windowConfigs.Visibility = Visibility.Visible;

                //Page project
                this.windowConfigs.tempConfig.myProject = myProjectData.ProjectInfo.Raw;
                ProjectClassification[] projectClassifications = this.windowConfigs.tempConfig.myProject.ListClassifications();
                if (null == projectClassifications || projectClassifications.Length == 0)
                {
                    Company_defined_RadioButton.IsEnabled = false;
                }
                else
                {
                    Company_defined_RadioButton.IsEnabled = true;
                }
                windowConfigs.pageSelectDocumentClassify = new PageSelectDocumentClassify();
                windowConfigs.pageSelectDocumentClassify.SetProject(windowConfigs.tempConfig.myProject);
                this.SelectDocumentClassification.Content = windowConfigs.pageSelectDocumentClassify;
            }
            else
            {
                //Page select rights
                windowConfigs.pageSelectDigitalRights = new PageSelectDigitalRights();
                this.windowConfigs.tempConfig.RightsSelectConfig = windowConfigs.pageSelectDigitalRights.RightsSelectConfig;
                this.SelectDigitalRights.Content = windowConfigs.pageSelectDigitalRights;

                this.User_Defined_RadioButton.IsChecked = true;

                if (SelectDigitalRights.Visibility == Visibility.Hidden)
                {
                    SelectDigitalRights.Visibility = Visibility.Visible;
                }
                if (SelectDocumentClassification.Visibility == Visibility.Visible)
                {
                    SelectDocumentClassification.Visibility = Visibility.Hidden;
                }
                windowConfigs.Visibility = Visibility.Collapsed;
            }
           
        }


        #endregion

    }

    #region DataMode WindowConfigs
    public class WindowConfigs:INotifyPropertyChanged
    {
        private static readonly string MY_VAULT = CultureStringInfo.MainWin__TreeView_MyVault;

        public WindowConfigs(CreateFileWindow win, FileOperation fileOperation, bool IsCreateByMainWin, IMyProject myProject)
        {
            this.CreateFileWin = win;
            this.tempConfig.myProject = myProject;
            this.isCreateByMainWin = IsCreateByMainWin;

            InitializeWindowConfigs(fileOperation);
        }

        public CreateFileWindow CreateFileWin;

        #region for transfer parameters
        //Pass parameters to the next window
        public ProtectAndShareConfig tempConfig = new ProtectAndShareConfig();

        //Determine if it is protected from plug-in
        public bool isCreateByMainWin;

        // Error Message
        public string ErrorMsg;

        //UI page content
        public PageSelectDocumentClassify pageSelectDocumentClassify;
        public PageSelectDigitalRights pageSelectDigitalRights;
        #endregion

        #region for binding UI
        private string operationTitle;
        private string operationDesc;
        private string fileName;
        private int fileCount;
        private string operationButton;
        //prompt text and raidoButton Visibility
        private Visibility visibility=Visibility.Visible;
        private string selectProjectFolderPath= MY_VAULT;

        /// <summary>
        /// Operation title property.
        /// </summary>
        public string OperationTitle
        {
            get
            {
                return operationTitle;
            }
            set
            {
                operationTitle = value;
                OnPropertyChanged("OperationTitle");
            }
        }
        /// <summary>
        /// Operation description property.
        /// </summary>
        public string OperationDesc
        {
            get
            {
                return operationDesc;
            }
            set
            {
                operationDesc = value;
                OnPropertyChanged("OperationDesc");
            }
        }

        public string FileName
        {
            get
            {
                return fileName;
            }
            set
            {
                fileName = value;
                OnPropertyChanged("FileName");
            }
        }

        public int FileCount
        {
            get
            {
                return fileCount;
            }
            set
            {
                fileCount = value;
                OnPropertyChanged("FileCount");
            }
        }

        public string OperationButton
        {
            get
            {
                return operationButton;
            }
            set
            {
                operationButton = value;
                OnPropertyChanged("OperationButton");
            }
        }
        //prompt text and raidoButton Visibility
        public Visibility Visibility
        {
            get => visibility;
            set
            {
                visibility = value;
                OnPropertyChanged("Visibility");
            }
        }
        public string SelectProjectFolderPath
        {
            get => selectProjectFolderPath;
            set
            {
                selectProjectFolderPath = value;
                OnPropertyChanged("SelectProjectFolderPath");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this,new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Initialize windowConfigs Method
        private void InitializeWindowConfigs(FileOperation operation)
        {
            //Set operation description bind to default checked rb.
            this.OperationDesc = CultureStringInfo.CreateFileWin_Operation_Info_ADhoc;

            if (operation != null)
            {
                this.tempConfig.FileOperation = operation;
                GenerateWindowConfigsByOperation(operation);
            }
            else
            {
                Console.WriteLine("operation obj in InitializeConfigs in null.");
            }
            InitializeContentPages(operation);
        }

        private void GenerateWindowConfigsByOperation(FileOperation operation)
        {
            if (operation != null)
            {
                //Append file name string from FileOperation.FileName string[].
                StringBuilder sb = new StringBuilder();
                int length = operation.FileName.Length;
                FileCount = length;
                for (int i = 0; i < length; i++)
                {
                    sb.Append(operation.FileName[i]);
                    if (i != length - 1)
                    {
                        sb.Append(";\r");
                    }
                }
                //Bind to windowConfigs.FileName property and notify UI changed.
                this.FileName = sb.ToString();
                //Protect view section.
                if (operation.Action == FileOperation.ActionType.Protect)
                {
                    this.OperationTitle = length > 1 ?
                        CultureStringInfo.CreateFileWin__Operation_Title_MProtect : //For multiple files protect title display.
                        CultureStringInfo.CreateFileWin__Operation_Title_Protect; //For single file protect title display.
                    this.OperationButton = CultureStringInfo.CreateFileWin_Btn_Protect;
                }
                else  //Share view section.
                {
                    this.OperationTitle = length > 1 ?
                        CultureStringInfo.CreateFileWin_Operation_Title_MShare : //For multiple files share title dispaly.
                        CultureStringInfo.CreateFileWin_Operation_Title_Share; //For single file share title display.
                    this.OperationButton = CultureStringInfo.CreateFileWin_Btn_Share;
                    //radioBtn and textblock collapsed
                    this.Visibility = Visibility.Collapsed;
                }

                if (isCreateByMainWin)
                {
                    this.CreateFileWin.ChangeDestinationText.Visibility = Visibility.Collapsed;//not display Changedestination                  
                }

                if (SkydrmLocalApp.Singleton.MainWin != null)
                {
                    if (!string.IsNullOrEmpty(SkydrmLocalApp.Singleton.MainWin.viewModel.CurrentSaveFilePath))
                    {
                        this.SelectProjectFolderPath = SkydrmLocalApp.Singleton.MainWin.viewModel.CurrentSaveFilePath;
                    }
                    else//when CurrentSaveFilePath="",user click project in MainWin and protect file by nxrmshell
                    {
                        this.SelectProjectFolderPath = MY_VAULT;
                    }
                }

                if (this.SelectProjectFolderPath == MY_VAULT)
                {
                    this.Visibility = Visibility.Collapsed;//not display choose radio, PageSelectDocumentClassify
                }


            }
        }

        private void InitializeContentPages(FileOperation operation)
        {
            //if by nxrmshell,when SelectProjectFolderPath= MY_VAULT,don't display PageSelectDocumentClassify
            if (operation.Action == FileOperation.ActionType.Protect)
            {
                if (this.SelectProjectFolderPath != MY_VAULT)
                {
                    if (this.tempConfig.myProject == null)//if user protect file by nxrmshell,the ProjectInfo is null
                    {
                        if (SkydrmLocalApp.Singleton.MainWin != null)
                        {
                            try
                            {
                                this.tempConfig.myProject = SkydrmLocalApp.Singleton.MainWin.viewModel.projectRepo.SelectedSaveProject;
                            }
                            catch (Exception e)
                            {
                                SkydrmLocalApp.Singleton.Log.Error("app.MainWin.viewModel.projectRepo.SelectedSaveProject.ProjectInfo.Raw is error", e);
                            }

                        }
                        else
                        {
                            SkydrmLocalApp.Singleton.Log.Error("SkydrmLocalApp.Singleton.MainWin is null in CreatefileWindow");
                            return;
                        }
                    }
                    //Page select rights
                    this.pageSelectDigitalRights = new PageSelectDigitalRights();
                    this.pageSelectDigitalRights.SetShareIsEnable(false);
                    this.tempConfig.RightsSelectConfig = this.pageSelectDigitalRights.RightsSelectConfig;
                    this.CreateFileWin.SelectDigitalRights.Content = this.pageSelectDigitalRights;

                    //Page project
                    ProjectClassification[] projectClassifications = this.tempConfig.myProject.ListClassifications();
                    if (null == projectClassifications || projectClassifications.Length == 0)
                    {
                        this.CreateFileWin.Company_defined_RadioButton.IsEnabled = false;
                        this.CreateFileWin.User_Defined_RadioButton.IsChecked = true;
                    }
                    this.pageSelectDocumentClassify = new PageSelectDocumentClassify();
                    this.pageSelectDocumentClassify.SetProject(this.tempConfig.myProject);
                    this.CreateFileWin.SelectDocumentClassification.Content = this.pageSelectDocumentClassify;
                }
                else
                {
                    //Page select rights
                    this.pageSelectDigitalRights = new PageSelectDigitalRights();                    
                    this.tempConfig.RightsSelectConfig = this.pageSelectDigitalRights.RightsSelectConfig;
                    this.CreateFileWin.SelectDigitalRights.Content = this.pageSelectDigitalRights;
                }
                //if SelectProjectFolderPath = MY_VAULT, will not display choose radio, PageSelectDocumentClassify,in void GenerateWindowConfigsByOperation have achieved 
            }
            else//if share a file by nxrmshell, can not protect to project
            {
                this.SelectProjectFolderPath = MY_VAULT;
                this.CreateFileWin.ChangeDestinationText.Visibility = Visibility.Collapsed;//not display Changedestination  

                ////Page select rights
                this.pageSelectDigitalRights = new PageSelectDigitalRights();
                this.tempConfig.RightsSelectConfig = this.pageSelectDigitalRights.RightsSelectConfig;
                this.CreateFileWin.SelectDigitalRights.Content = this.pageSelectDigitalRights;
            }

        }
        #endregion
    }
    #endregion

    #region Converter
    public class ChangeButtonTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                int fileCount = (int)value;
                if (fileCount > 1)
                {
                    return CultureStringInfo.CreateFileWin_Btn_Edit;
                }
                return CultureStringInfo.CreateFileWin_Btn_Change;
            }
            catch (Exception)
            {
                return CultureStringInfo.CreateFileWin_Btn_Change;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // When select mulit files,display file count
    public class SelectFileCountTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                int fileCount = (int)value;
                if (fileCount > 1)
                {
                    return string.Format(@"({0})", fileCount);
                }
                return "";
            }
            catch (Exception)
            {
                return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    #endregion
}
