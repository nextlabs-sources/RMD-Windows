using SkydrmLocal.rmc.common.component;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.helper;
using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.ui.components;
using SkydrmLocal.rmc.ui.components.RightsDisplay.model;
using SkydrmLocal.rmc.ui.pages.model;
using SkydrmLocal.rmc.ui.utils;
using SkydrmLocal.rmc.ui.windows.mainWindow.model;
using SkydrmLocal.rmc.ui.windows.nxlConvert.subs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
using System.Windows.Threading;

namespace SkydrmLocal.rmc.ui.windows.nxlConvert
{
    /// <summary>
    /// Interaction logic for ShareNxlWindow.xaml
    /// </summary>
    public partial class NxlFileToConvertWindow : Window
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
        private object winTag;
        private Dictionary<string, List<string>> originalFileTag;

        //for display ProBar
        private BackgroundWorker BgWorker = new BackgroundWorker();

        /// <summary>
        /// For online .nxl file
        /// </summary>
        public NxlFileToConvertWindow()
        {
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(delegate (object sender, RoutedEventArgs e)
            {
                this.Topmost = false;
                this.winTag = this.Tag;
            });

            this.sp_Header.Visibility = Visibility.Collapsed;
            this.GridProBar.Visibility = Visibility.Visible;
        }

        /// <summary>
        ///  For offline .nxl file
        /// </summary>
        public NxlFileToConvertWindow(ProtectAndShareConfig config)
        {
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(delegate (object sender, RoutedEventArgs e)
            {
                this.Topmost = false;
                tempConfig.WinTag = this.Tag;
            });

            InitConfig(config);
        }

        public void InitConfig(ProtectAndShareConfig config)
        {
            tempConfig = config;

            tempConfig.WinTag = this.Tag;

            originalFileTag = tempConfig.Tags;
            //Initialize save path
            //InitializeSavePath();

            InitFileCapDesc();

            if (tempConfig.FileOperation.Action == FileOperation.ActionType.Protect)
            {
                InitFileLocaltionDest();
            }

            InitFileRightsSelect();

            InitFileRightsDisplay();

            SetPageConfig();

            InitProtectBgWorker();

            this.GridProBar.Visibility = Visibility.Collapsed;
            this.sp_Header.Visibility = Visibility.Visible;

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

        #region Init FileCapDesc
        private void InitFileCapDesc()
        {
            //Append file name string from FileOperation.FileName string[].
            StringBuilder sb = new StringBuilder();
            int length = tempConfig.FileOperation.FileName.Length;
            this.Header.FileCount = length;
            for (int i = 0; i < length; i++)
            {
                sb.Append(tempConfig.FileOperation.FileName[i]);
                if (i != length - 1)
                {
                    sb.Append(";\r");
                }
            }
            //Bind to windowConfigs.FileName property and notify UI changed.
            this.Header.FilesName = sb.ToString();
            //Protect view section.
            if (tempConfig.FileOperation.Action == FileOperation.ActionType.Protect)
            {
                this.Header.Title = CultureStringInfo.NxlFileToCvetWin_Header_Title;
                this.Header.SetDisplayTags(originalFileTag);
            }
            else if(tempConfig.FileOperation.Action == FileOperation.ActionType.Share)  //Share view section.
            {
                this.Header.Title = length > 1 ?
                    CultureStringInfo.CreateFileWin_Operation_Title_MShare : //For multiple files share title dispaly.
                    CultureStringInfo.CreateFileWin_Operation_Title_Share; //For single file share title display.
            }
            else if (tempConfig.FileOperation.Action == FileOperation.ActionType.ModifyRights)
            {
                this.Header.Title = CultureStringInfo.NxlFileToCvetWin_Header_Title_ModifyRights;
                this.Header.SetDisplayTags(originalFileTag);
            }
            
            this.Header.Description = CultureStringInfo.NxlFileToCvetWin_Header_Descriptio;
            // Hiden change button
            this.Header.ChangeFileIsVisibilty(Visibility.Collapsed);
        }
        #endregion

        #region Init FileLocationDest
        private void InitFileLocaltionDest()
        {
            fileLocaltionDest = new FileLocaltionDest();
            fileLocaltionDest.OkBtnClicked += FileLocaltionDest_OkBtnClicked;
            fileLocaltionDest.CancelBtnClicked += CancelBtnClicked;

            fm_Body.Content = fileLocaltionDest;

            fileLocaltionDest.sp_Path.Margin = new Thickness(155,5,0,15);
        }

        private void FileLocaltionDest_OkBtnClicked(object sender, RoutedEventArgs e)
        {
            savePath = fileLocaltionDest.Path;
            tempConfig.myProject = fileLocaltionDest.Project;

            SetFileRightsPage(savePath, tempConfig.myProject);

            // If user change destination,should not change mainWindow 'CurrentSaveFilePath'.Do second protect will display wrong path.
            // But in ViewModelMainWin triggre 'HandleNewFiles' method need 'CurrentSaveFilePath' set sourPath, should use DoRefresh() method get new file.

            // MainWin save CurrentSaveFilePath and SelectProject 
            //SkydrmLocalApp.Singleton.MainWin.viewModel.CurrentSaveFilePath = savePath;
            SkydrmLocalApp.Singleton.MainWin.viewModel.projectRepo.SelectedSaveProject = tempConfig.myProject;

            DisplayFrameBody(false);
        }
        #endregion

        #region Init FileRightsSelect
        private void InitFileRightsSelect()
        {
            fileRightsSelect = new FileRightsSelect();
            fileRightsSelect.ChangeDestClicked += FileRightsSelect_ChangeBtnClicked;
            fileRightsSelect.OkBtnClicked += FileRightsSelect_OkBtnClicked;
            fileRightsSelect.CancelBtnClicked += CancelBtnClicked;
            fileRightsSelect.RadioCheckChanged += FileRightsSelect_RadioCheckChanged;
            fileRightsSelect.ValidityDateChanged += FileRightsSelect_ValidityDateChanged;

            fm_Body2.Content = fileRightsSelect;

            fileRightsSelect.sp_Path.Margin = new Thickness(155, 5, 0, 15);
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
        private void FileRightsSelect_ChangeBtnClicked(object sender, MouseButtonEventArgs e)
        {
            try
            {
                DisplayFrameBody(true);
            }
            catch (Exception msg)
            {
                SkydrmLocalApp.Singleton.Log.Error("Display FileLocation page error:", msg);
            }
        }

        private void FileRightsSelect_OkBtnClicked(object sender, RoutedEventArgs e)
        {
            bool cPoRadioIsChecked = fileRightsSelect.rb_Central.IsChecked == true ? true : false;
            bool isProtectToProject = !savePath.Equals(MY_VAULT);

            if (cPoRadioIsChecked && isProtectToProject)
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

            UserSelectTags selectTags = new UserSelectTags();
            selectTags = fileRightsSelect?.GetSelectedTags();
            Dictionary<string, List<string>> selectTagsUI= fileRightsSelect?.GetSelectedTagsForUI();

            #region get original Tags And reset Tags
            if (this.tempConfig.FileOperation.Action != FileOperation.ActionType.ModifyRights)
            {
                Dictionary<string, List<string>> allTags = fileRightsSelect?.GetProjectClassification();
                //Get tags setting by config.
                var tags = originalFileTag;
                //Check nonull for tags.
                if (tags != null || tags.Count != 0)
                {
                    //Get the iterator of the dictionary.
                    var iterator = tags.GetEnumerator();
                    //If there is any items inside it.
                    while (iterator.MoveNext())
                    {
                        //Get the current one.
                        var current = iterator.Current;
                        var key = current.Key;
                        var values = current.Value;

                        // Fixed bug 54264, 54352
                        // 54264: Can't have same key.
                        // 54352: If the original file tag name same as project tag name, user can unselected tag value.The selectTags should not contain original tag.
                        if (!selectTagsUI.Keys.Contains(key) && !allTags.Keys.Contains(key))
                        {
                            selectTags.AddTag(key, values);
                            selectTagsUI.Add(key, values);
                        }
                    }
                }
            }
            #endregion

            //Set configures
            tempConfig.UserSelectTags = selectTags;
            tempConfig.Tags = selectTagsUI;
            tempConfig.IsProtectToProject = isProtectToProject;
            tempConfig.SelectProjectFolderPath = savePath;
            tempConfig.CentralPolicyRadioIsChecked = cPoRadioIsChecked;
            tempConfig.RightsSelectConfig = fileRightsSelect.AdHocRights;

            // Save centralPolicyRadio status
            SkydrmLocalApp.Singleton.User.IsCentralPlcRadio = cPoRadioIsChecked;

            if (this.tempConfig.FileOperation.Action == FileOperation.ActionType.Protect
                || this.tempConfig.FileOperation.Action == FileOperation.ActionType.ModifyRights)
            {
                if (cPoRadioIsChecked)
                {
                    //CentralPolicy
                    //Get rights and display
                    if ( !InitFileRightsDisplayConfig())
                    {
                        return;
                    }
                    FrameBody2Switch(false);
                }
                else
                {
                    //AdHoc
                    StartBgWorker();
                }
            }
            else if( this.tempConfig.FileOperation.Action == FileOperation.ActionType.Share)
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

            if (!BgWorker.IsBusy)
            {
                BgWorker.RunWorkerAsync();
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
                }
                else
                {
                    Header.Description = CultureStringInfo.CreateFileWin_Operation_Info_Central;
                }
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

            fileRightsDisplay.sp_Title.Margin = new Thickness(0, 10, 0, 0);
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

        #region Process compent
        private void SetPageConfig()
        {
            // Add file to project
            if (tempConfig.FileOperation.Action == FileOperation.ActionType.Protect)
            {
                InitFileLocaltionConfig();
                DisplayFrameBody(true);
            }
            else if (tempConfig.FileOperation.Action == FileOperation.ActionType.Share)  // Share file to person(share file to MyVault)
            {
                InitFileRightsConfigDoShare();
                DisplayFrameBody(false);
            }
            else if (tempConfig.FileOperation.Action == FileOperation.ActionType.ModifyRights)
            {
                InitFileRightsConfigDoModify();
                DisplayFrameBody(false);
            }
        }

        private void InitFileLocaltionConfig()
        {
            if (tempConfig.myProject == null)
            {
                tempConfig.myProject = GetMyProject();
            }
            // set treeView defult selected in central location page
            // Reinitialization centralLocaltionPage before set centralRadio. Because set centralRadio will trigger frame content binding.
            fileLocaltionDest.ProcessCentral_ReInitTreeView();
            fileLocaltionDest.ProcessCentral_RemoveTvItem(tempConfig.ProjectId);

            fileLocaltionDest.OkBtn.Content = CultureStringInfo.NxlFileToCvetWin_Btn_Next;
            fileLocaltionDest.SetCentralRadio(true);

            // Fixed bug 54370 change UI display
            //fileLocaltionDest.spRadioTitle.Visibility = Visibility.Collapsed;
            this.Header.tb_Desc.Visibility = Visibility.Collapsed;
            fileLocaltionDest.tb_Title.Text = CultureStringInfo.NxlFileToCvetWin_FileLocation_Title_Addfile;
            fileLocaltionDest.tb_Title.Margin = new Thickness(0,0,0,0);
            fileLocaltionDest.sp_radio.Visibility = Visibility.Collapsed;

            fileLocaltionDest.ProcessCentral_Title(Visibility.Collapsed);
            fileLocaltionDest.Path = "";
            fileLocaltionDest.OkBtn.IsEnabled = false;        

        }
        private void InitFileRightsConfigDoShare()
        {
            //Keep cpy of savePath in rights page[for ui display].
            savePath = MY_VAULT;
            fileRightsSelect.Path = savePath;

            // Don't display changeDestination btn.
            fileRightsSelect.ChangDestVisible = Visibility.Collapsed;

            fileRightsSelect.SetAdhocRadio(true);
            fileRightsSelect.SetCentralPlcRadio(false, false);
            //fileRightsSelect.DescAndRbVisible = Visibility.Collapsed;
            fileRightsSelect.ProtectBtnContent = CultureStringInfo.NxlFileToCvetWin_Btn_Share;

            //Hiden change button
            fileRightsSelect.ProcessAdHocPageWaterMarkBtn(Visibility.Collapsed);
            fileRightsSelect.ProcessAdHocPageValidityBtn(Visibility.Collapsed);

            //Set original rights
            SetOriginalRights();

        }

        /// <summary>
        /// When click FilelocationDest ok button, use this method to process fileRightsSelect page.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="project"></param>
        private void SetFileRightsPage(string path, IMyProject project)
        {
            fileRightsSelect.Path = path;

            fileRightsSelect.ProtectBtnContent = CultureStringInfo.NxlFileToCvetWin_Btn_Next;

            //display textblock and radioBtn
            fileRightsSelect.DescAndRbVisible = Visibility.Visible;

            if (tempConfig.IsAdHoc)
            {
                fileRightsSelect.SetAdhocRadio(true);

                //Hiden change button
                fileRightsSelect.ProcessAdHocPageWaterMarkBtn(Visibility.Collapsed);
                fileRightsSelect.ProcessAdHocPageValidityBtn(Visibility.Collapsed);

                // Set original rights
                SetOriginalRights();

                fileRightsSelect.SetCentralPlcRadio(false, false);
            }
            else
            {
                fileRightsSelect.SetCentralPlcRadio(true);
                fileRightsSelect.SetAdhocRadio(false, false);
                // Set Project tag
                fileRightsSelect.ProcessCentralPage(project, true);
                // For get tag rights
                tempConfig.ProjectId = tempConfig.myProject.Id;
                //Set original nxlFile tag
                //fileRightsSelect.ProcessCentralPageOriginTag(tempConfig.Tags);
                //Set defult select tag
                fileRightsSelect.ProcessCentralPageDefultSelectTag(tempConfig.Tags);
            }

        }

        private void SetOriginalRights()
        {
            // If protected to project,don't have share right
            //fileRightsSelect.ProcessAdHocPage(false, true);
            fileRightsSelect.ProcessAdHocPageRights_Cb((List<string>)tempConfig.RightsSelectConfig.Rights);
            fileRightsSelect.ProcessAdHocPageWaterMark(tempConfig.RightsSelectConfig.Watermarkvalue);
            fileRightsSelect.ProcessAdHocPageExpire(tempConfig.RightsSelectConfig.Expiry, tempConfig.RightsSelectConfig.ExpireDateValue);
            //Maybe set page isEnable by adminRights
            fileRightsSelect.ProcessAdHocPageIsEnable(false);
        }

        private void InitFileRightsConfigDoModify()
        {
            //Keep cpy of savePath in rights page[for ui display].
            InitializeSavePath();

            if (tempConfig.ModifyRightsFeature.IsFromLocalDrive())
            {
                fileRightsSelect.Path = Alphaleonis.Win32.Filesystem.Path.GetDirectoryName(tempConfig.FileOperation.FilePath[0]);
                // Fixed bug 54386
                savePath = fileRightsSelect.Path;
            }
            else
            {
                fileRightsSelect.Path = savePath;
            }

            // Don't display changeDestination btn.
            fileRightsSelect.ChangDestVisible = Visibility.Collapsed;

            fileRightsSelect.ProtectBtnContent = CultureStringInfo.NxlFileToCvetWin_Btn_Next;

            //display textblock and radioBtn
            fileRightsSelect.DescAndRbVisible = Visibility.Visible;

            //Set original rights
            fileRightsSelect.ProcessAdHocPageRights_Cb((List<string>)tempConfig.RightsSelectConfig.Rights);
            fileRightsSelect.ProcessAdHocPageWaterMark(tempConfig.RightsSelectConfig.Watermarkvalue);
            fileRightsSelect.ProcessAdHocPageExpire(tempConfig.RightsSelectConfig.Expiry, tempConfig.RightsSelectConfig.ExpireDateValue);
           
            //Set original tags
            if (tempConfig.ProjectId == tempConfig.sProject.Id)
            {
                // Set sysProject tag
                fileRightsSelect.ProcessCentralPage(tempConfig.sProject, false);
            }
            else
            {
                // Set Project tag
                fileRightsSelect.ProcessCentralPage(tempConfig.myProject, false);
                // For get tag rights
                tempConfig.ProjectId = tempConfig.myProject.Id;
            }
            //Fix bug 54422, Add inherited tags
            fileRightsSelect.ProcessCentralPageAddInheritedTag(tempConfig.Tags);
            //Set defult select tag
            fileRightsSelect.ProcessCentralPageDefultSelectTag(tempConfig.Tags);

            if (tempConfig.IsAdHoc)
            {
                fileRightsSelect.SetAdhocRadio(true);
                fileRightsSelect.SetCentralPlcRadio(false, false);
            }
            else
            {
                fileRightsSelect.SetCentralPlcRadio(true);
                fileRightsSelect.SetAdhocRadio(false, false);
            }

        }

        private bool InitFileRightsDisplayConfig()
        {
            bool invoke = true;
            try
            {
                if (tempConfig.FileOperation.Action == FileOperation.ActionType.Protect)// add file to project
                {
                    fileRightsDisplay.SetOkBtnContent(CultureStringInfo.NxlFileToCvetWin_Btn_AddFile);
                }
                else if (tempConfig.FileOperation.Action == FileOperation.ActionType.ModifyRights)
                {
                    fileRightsDisplay.SetOkBtnContent(CultureStringInfo.NxlFileToCvetWin_Btn_Modify);
                }
                if (tempConfig.myProject != null)
                {
                    fileRightsDisplay.SetProjectName(tempConfig.myProject.DisplayName);
                }
                else
                {
                    fileRightsDisplay.SetSectionTitle(CultureStringInfo.FileTagRightDisplay_SysBucktTitle);
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
                SkydrmLocalApp.Singleton.Log.Error("Error in InitFileRightsDisplayConfig:",e);
                SkydrmLocalApp.Singleton.ShowBalloonTip(CultureStringInfo.Common_System_Internal_Error);
            }

            return invoke;
        }
        #endregion

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
                this.Header.TagVisible = Visibility.Visible;
                fm_Body2.Content = fileRightsSelect;
            }
            else
            {
                this.Header.TagVisible = Visibility.Collapsed;
                fm_Body2.Content = fileRightsDisplay;
            }
        }

        private void InitProtectBgWorker()
        {
            //init BackgroundWorker
            BgWorker.WorkerReportsProgress = true;
            BgWorker.WorkerSupportsCancellation = true;
            if (tempConfig.FileOperation.Action == FileOperation.ActionType.Protect)
            {
                BgWorker.DoWork += DoProtect_Handler;
                BgWorker.RunWorkerCompleted += DoProtectCompleted_Handler;
            }
            else if (tempConfig.FileOperation.Action == FileOperation.ActionType.ModifyRights)
            {
                BgWorker.DoWork += DoModifyRights_Handler;
                BgWorker.RunWorkerCompleted += DoProtectCompleted_Handler;
            }
            
        }
        #region Do modifyRights
        private void DoModifyRights_Handler(object sender, DoWorkEventArgs e)
        {
            bool invoke = CommonUtils.ModifyRights(tempConfig, out errorMsg);

            e.Result = invoke;
        }
        private void DoModifyRightsCompleted_Handler(object sender, RunWorkerCompletedEventArgs e)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Do protection.
        private void DoProtect_Handler(object sender, DoWorkEventArgs e)
        {
            bool invoke = false;

            if (tempConfig.ShareNxlFeature == null)
            {
                e.Result = false;
                return;
            }
            string decryptPath;
            bool isDecrypt = tempConfig.ShareNxlFeature.IsDecrypt(out decryptPath);
            if (isDecrypt)
            {
                string[] filePath = new string[1];
                filePath[0] = decryptPath;
                tempConfig.FileOperation.FilePath = filePath;
                invoke = CommonUtils.ProtectOrShare(tempConfig, out errorMsg);
            }
            else
            {
                invoke = false;
                errorMsg = CultureStringInfo.Common_System_Internal_Error;
            }
            
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
                //For add file to project
                if (tempConfig.FileOperation.Action == FileOperation.ActionType.Protect)
                {
                    ProjectShareAddLog();
                }
                //else if (tempConfig.FileOperation.Action == FileOperation.ActionType.ModifyRights)
                //{
                //    //We won't send log if project admin modify rights, because RMS will keep both success & failed classify log for us.
                //    ModifyRightsAddLog();
                //}
                //sdk will add a log, but we need to call upload explicitly
                //SkydrmLocalApp.Singleton.User.UploadNxlFileLog_Async();


                // show protect success window.
                ProtectWindow protectWindow = new ProtectWindow(this,tempConfig);
                //// close current window
                //this.Close();

                //// Using modeless dialog, or else(using modal dialog) will crash when user open tray context menu at the same time.
                //protectWindow.Show();

                // If using modeless dialog, the user can change mainWindow list. It will have other bugs.
                protectWindow.Owner = this;
                protectWindow.ShowDialog();

            }
            else
            {
                this.ProtectFailedText.Visibility = Visibility.Visible;
                this.ProtectFailedText.Text = errorMsg;
            }
        }
        private void ProjectShareAddLog()
        {
            var tconfig = tempConfig;
            if (tconfig == null)
            {
                return;
            }
            var sf = tconfig?.ShareNxlFeature;
            if (sf == null)
            {
                return;
            }
            var action = sf.GetProjectShareAction();
            // ADhoc Share to myVault
            if (action == shareNxlFeature.ShareNxlFeature.ShareNxlAction.AddFileToProject)
            {
                SkydrmLocalApp.Singleton.User.AddNxlFileLog(sf.GetSourceNxlLocalPath(), NxlOpLog.Share, true);
            }
        }

        private void ModifyRightsAddLog()
        {
            var tconfig = tempConfig;
            if (tconfig == null)
            {
                return;
            }
            var sf = tconfig?.ModifyRightsFeature;
            if (sf == null)
            {
                return;
            }
            SkydrmLocalApp.Singleton.User.AddNxlFileLog(tempConfig.FileOperation.FilePath[0], NxlOpLog.Classify, true);
        }
        #endregion

        private void CancelBtnClicked(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // during file-protecting , can't  be closed. 
            if (MenuDisableMgr.GetSingleton().IsProtecting)
            {
                e.Cancel = true;
                SkydrmLocalApp.Singleton.ShowBalloonTip(CultureStringInfo.CreateFileWin_Notify_Wait_Protect);
                return;
            }
            tempConfig?.ShareNxlFeature?.DeleteNxlFile();
            tempConfig?.ShareNxlFeature?.DeleteRPM_File();
            tempConfig?.ModifyRightsFeature?.DeleteNxlFile();
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
