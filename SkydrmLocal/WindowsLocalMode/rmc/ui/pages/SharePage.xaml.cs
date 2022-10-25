using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using SkydrmLocal.rmc.ui.pages.model;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.ComponentModel;
using SkydrmLocal.rmc.ui.windows;
using SkydrmLocal.rmc.ui.windows.mainWindow.model;
using SkydrmLocal.rmc.helper;
using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.ui.components.ValiditySpecify.model;
using SkydrmLocal.rmc.ui.utils;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.fileSystem.myvault;
using SkydrmLocal.rmc.common.component;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.search;
using SkydrmLocal.rmc.ui.components.RightsDisplay.model;
using SkydrmLocal.rmc.ui.windows.outlookAddressBook;
using SkydrmLocal.rmc.app.process.outlook;
using System.Threading;
using System.Windows.Interop;
using SkydrmLocal.rmc.featureProvider.MyVault;

namespace SkydrmLocal.rmc.ui.pages
{
    /// <summary>
    /// Interaction logic for SharePage.xaml
    /// History:
    ///     by osmond, avoid to use the class NxlFile
    /// </summary>
    public partial class SharePage : Page
    {
        #region Private field
        private SkydrmLocalApp app = (SkydrmLocalApp)SkydrmLocalApp.Current;

        private ShareWindow parentWindow;

        //right display listBox ItemSource
        private IList<RightsItem> rightsItems = new List<RightsItem>();
        //email List ItemSource
        private ObservableCollection<EmailItem> emailItems = new ObservableCollection<EmailItem>();
        //manage email List, whether its valid or not
        private IList<string> sharedEmailLists = new List<string>();
        private IList<string> dirtyEmailLists = new List<string>();

        private SharePageBindConfigs bindConfigs = new SharePageBindConfigs();
        //transfer  'tempConfig' parameter to ShareSccessPage
        private ProtectAndShareConfig tempConfig = new ProtectAndShareConfig();

        //for display ProBar invoke SDK shareFile
        private BackgroundWorker DoShare_BgWorker = new BackgroundWorker();

        //for display ProBar invoke SDK UpdateRecipients
        private BackgroundWorker U_Recipients = new BackgroundWorker();

        //for nxlFile UpdateRecipients
        //private NxlFile nxlFile { get; set; }

        // Error Message
        private string ErrorMsg;

        // Max length that allow to input
        private const int maxLen = 250;
        // Length that has input
        private int usedLen = 0;
        #endregion

        public ShareWindow ParentWindow
        {
            get { return parentWindow; }
            set { parentWindow = value; }
        }

        public SharePage(ProtectAndShareConfig configs)
        {
            InitializeComponent();

            InitData(configs);
            InitBgWorker(configs);
        }

        private void InitBgWorker(ProtectAndShareConfig configs)
        {
            if (configs.FileOperation.Action == FileOperation.ActionType.UpdateRecipients)
            {
                this.StackComment.Visibility = Visibility.Collapsed;
                //init BackgroundWorker
                U_Recipients.WorkerReportsProgress = true;
                U_Recipients.WorkerSupportsCancellation = true;
                U_Recipients.DoWork += U_Recipients_Handler;
                U_Recipients.RunWorkerCompleted += U_RecipientsCompleted_Handler;
            }
            else if (configs.FileOperation.Action == FileOperation.ActionType.Share)
            {
                //init BackgroundWorker
                DoShare_BgWorker.WorkerReportsProgress = true;
                DoShare_BgWorker.WorkerSupportsCancellation = true;
                DoShare_BgWorker.DoWork += DoShare_Handler;
                //m_BgWorker.ProgressChanged += ProgressChanged_Handler;
                DoShare_BgWorker.RunWorkerCompleted += DoShareCompleted_Handler;
            }
        }

        private void InitData(ProtectAndShareConfig configs)
        {
            tempConfig = configs;
            IList<string> rights = configs.RightsSelectConfig.Rights;
            StringBuilder sb = new StringBuilder();
            int length = configs.FileOperation.FileName.Length;
            for (int i = 0; i < length; i++)
            {            
                sb.Append(configs.FileOperation.FileName[i]);
                if (i != length - 1)
                {
                    sb.Append(";\r");
                }
            }
            //Set operation title for share success page.
            bindConfigs.OperationTitle = length > 1 ?
                 CultureStringInfo.CreateFileWin_Operation_Title_MShare : //For multiple files share title dispaly.
                 CultureStringInfo.CreateFileWin_Operation_Title_Share; //For single file share title display.

            bindConfigs.FileName = sb.ToString();
            //bindConfigs.FileName = configs.FileOperation.FileName;
            bindConfigs.WatermarkValue = configs.RightsSelectConfig.Watermarkvalue;
            bindConfigs.ValidityValue = configs.RightsSelectConfig.ExpireDateValue;
            bindConfigs.ShareButtonEnabled = false;

            //Add divide line.
            DivideLine.Children.Add(CreateDivideLine());

            rightsItems = CommonUtils.GetRightsIcon(rights);
            foreach (var item in rightsItems)
            {
                if (item.Rights.Equals("Watermark"))
                {
                    this.WatermarkPanel.Visibility = Visibility.Visible;
                }
            }
            //this.ShareText.Text = CultureStringInfo.ShareFileWin_Protect_MyVault;
            this.rightsDisplayBoxes.ItemsSource = rightsItems;
            this.emailListsControl.ItemsSource = emailItems;
            this.DataContext = bindConfigs;

            if (tempConfig.FileOperation.Action == FileOperation.ActionType.UpdateRecipients)
            {
                var fp = app.Rmsdk.User.GetNxlFileFingerPrint(tempConfig.FileOperation.FilePath[0]);
                bindConfigs.IsOwnerVisibility = fp.isOwner ? Visibility.Visible : Visibility.Collapsed;
            }

            CheckShareable(tempConfig.ShareNxlFeature?.GetSourceNxlLocalPath());
        }

        private Line CreateDivideLine()
        {
            return new Line
            {
                Stroke = (Brush)new BrushConverter().ConvertFromString("#BDBDBD"),
                StrokeThickness = 2.0,
                X1 = 0,
                X2 = 1000,
                Y1 = 0,
                Y2 = 0,
            };
        }

        private void ShareBtnEnableByEmailList()
        {
            if (sharedEmailLists.Count != 0 && dirtyEmailLists.Count == 0)
            {
                bindConfigs.ShareButtonEnabled = true;
            }
            else
            {
                bindConfigs.ShareButtonEnabled = false;
            }
        }

        #region manage email method

        private bool CheckEmail(string email)
        {
            if (email.Equals(" ") || email == null)
            {
                return false;
            }
            string regExpn = "^(([\\w-]+\\.)+[\\w-]+|([a-zA-Z]{1}|[\\w-]{2,}))@"
                + "((([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\\.([0-1]?"
                + "[0-9]{1,2}|25[0-5]|2[0-4][0-9])\\."
                + "([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\\.([0-1]?"
                + "[0-9]{1,2}|25[0-5]|2[0-4][0-9])){1}|"
                + "([a-zA-Z0-9]+[\\w-]+\\.)+[a-zA-Z]{2,})$";
            if (Regex.IsMatch(email, regExpn))
            {
                return true;
            }
            return false;
        }

        /*
       *Wrap email with different style.
       *Distinguish the email from sharedEmailLists to dirtyEmailLists
       */
        private void WrapEmail(string email)
        {
            string trimedText = email.Trim();
            bool isemailExist = isEmailExisted(trimedText, sharedEmailLists);
            if (isemailExist)
            {
                //CustomMessageBoxWindow.Show(CultureStringInfo.Share_Email_Title,
                //   CultureStringInfo.Share_Email_Subject,
                //  CultureStringInfo.Share_Email_Details,
                //   CustomMessageBoxWindow.CustomMessageBoxIcon.Warning,
                //   CustomMessageBoxWindow.CustomMessageBoxButton.BTN_CLOSE);
                //this.emailInputTB.Text = "";
                //this.emailInputTB.Select(this.emailInputTB.Text.Length, 0);
                SkydrmLocalApp.Singleton.ShowBalloonTip(trimedText + "\n" + CultureStringInfo.Share_Email_Details, 500);
                return;
            }
            if (CheckEmail(trimedText) && !isemailExist)
            {
                emailItems.Add(new EmailItem(trimedText, EmailStatus.NORMAL));
                sharedEmailLists.Add(trimedText);
            }
            else
            {
                emailItems.Add(new EmailItem(trimedText, EmailStatus.DIRTY));
                dirtyEmailLists.Add(trimedText);
            }
        }

        // check the email address if has existed.
        public bool isEmailExisted(String email, IList<String> emailList)
        {
            foreach (String one in emailList)
            {
                if (one.ToUpper().Equals(email.ToUpper()))
                {
                    return true;
                }
            }
            return false;
        }

        /*
       * Remove the selected item from ObservableCollection{link emailItems}.
       */
        private void DeleteEmailItem(string target)
        {
            //Traversal emailItems
            EmailItem deleteItem = null;
            foreach (var item in emailItems)
            {
                if (target.Equals(item.Emails))
                {
                    deleteItem = item;
                    break;
                }
            }
            //Delete the emailItem contains the target email
            if (deleteItem != null)
            {
                emailItems.Remove(deleteItem);
                if (deleteItem.EmailStatus.Equals(EmailStatus.NORMAL))
                {
                    if (sharedEmailLists.Contains(deleteItem.Emails))
                    {
                        sharedEmailLists.Remove(deleteItem.Emails);
                    }
                }
                else
                {
                    if (dirtyEmailLists.Contains(deleteItem.Emails))
                    {
                        dirtyEmailLists.Remove(deleteItem.Emails);
                    }
                }
                ShareBtnEnableByEmailList();
            }
        }
        #endregion

        #region UI Event
        /*
         * TextChangedListener of the email input box.
         * */
        private void EmailInputTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox == null)
            {
                Console.Write("sender is a null obj.");
                return;
            }
            string text = textBox.Text;
            //Clear the space input.
            if (text.Equals(" ") || text == null)
            {
                //clear the email input box.
                textBox.Text = "";
                return;
            }
            if (text.EndsWith(" ") || text.EndsWith("\n"))
            {
                SpiltInputEmail(text);
                textBox.Text = "";
            }            
        }
        /*
         * KeyEnter Listener of the email input box.
         * */
        private void EmailInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TextBox textBox = sender as TextBox;
                if (textBox == null)
                {
                    Console.Write("sender is a null obj.");
                    return;
                }
                string text = textBox.Text;
                //Clear the space input.
                if (text.Equals("") || text == null)
                {
                    //clear the email input box.
                    textBox.Text = "";
                    return;
                }
                SpiltInputEmail(text);

                textBox.Text = "";
            }
        }
        private void SpiltInputEmail(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }
            string[] emailArray = text.Split(new char[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < emailArray.Length; i++)
            {
                WrapEmail(emailArray[i]);
            }
            ShareBtnEnableByEmailList();
        }

        /*
         * Receive the DeleteEmailItemClickEvent which send by a delete image.
         */
        private void DeleteEmailItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var item = sender as Image;
            if (item != null)
            {
                //Cannot from ItemsControl get the click item. So Choose to bind the emails as a Tag of the delete image.
                //When found a delete event send by the image,just get the the binded tag from it then operate it.
                string emailName = (string)item.Tag;
                DeleteEmailItem(emailName);
            }
        }

        //Share Button Click
        private void Button_Ok(object sender, RoutedEventArgs e)
        {
            app.Log.Info("----IsClosingOutlookAddressBookWin----> " + parentWindow.IsClosingOutlookAddressBookWin);
            if (parentWindow.IsClosingOutlookAddressBookWin)
            {
                return;
            }

            if (tempConfig.FileOperation.Action == FileOperation.ActionType.UpdateRecipients)
            {
                // fix bug 49752
                if (bindConfigs.FileName.EndsWith(".nxl") && !FileHelper.Exist(tempConfig.FileOperation.FilePath[0]))
                {
                    app.ShowBalloonTip(CultureStringInfo.Common_File_Not_Exist);
                    return;
                }
            }

            // check expiry, fix bug 56452 
            bool result = CommonUtils.CheckExpiry(tempConfig, out Expiration expiration, out string message);
            if (!result)
            {
                this.ShareText.Foreground = new SolidColorBrush(Color.FromRgb(0XFF, 0X00, 0X00));
                this.ShareText.Text = CultureStringInfo.Validity_ShareFile_Expired;
                return;
            }

            //display ui ProBar           
            this.GridProBar.Visibility = Visibility.Visible;
  
            SharedWithConfig sharetemp = new SharedWithConfig();
            sharetemp.SharedEmailLists = sharedEmailLists;
            sharetemp.Comments = commentTB.Text;
            //for invoke SDkshare and display config 
            tempConfig.SharedWithConfig = sharetemp;

            MenuDisableMgr.GetSingleton().MenuDisableNotify(CultureStringInfo.MenuItem_Logout, true);
            MenuDisableMgr.GetSingleton().MenuDisableNotify(CultureStringInfo.MenuItem_Exit, true);
            MenuDisableMgr.GetSingleton().IsSharing = true;

            if (tempConfig.FileOperation.Action == FileOperation.ActionType.UpdateRecipients)
            {
                if (!U_Recipients.IsBusy)
                {
                    U_Recipients.RunWorkerAsync();
                }
            }
            else if(tempConfig.FileOperation.Action == FileOperation.ActionType.Share)
            {
                if (!DoShare_BgWorker.IsBusy)
                {
                    DoShare_BgWorker.RunWorkerAsync();
                }
            }
           
        }
        //Cancel Button Click
        private void Button_Cancel(object sender, RoutedEventArgs e)
        {
            if (parentWindow != null)
            {
                app.Log.Info("----IsClosingOutlookAddressBookWin----> " + parentWindow.IsClosingOutlookAddressBookWin);
                if (parentWindow.IsClosingOutlookAddressBookWin)
                {
                    return;
                }
                parentWindow.Close();
            }
        }

        private void CheckShareable(string nxlLocalPath)
        {
            var app = SkydrmLocalApp.Singleton;

            if(string.IsNullOrEmpty(nxlLocalPath))
            {
                return;
            }
            var fp = app.Rmsdk.User.GetNxlFileFingerPrint(nxlLocalPath);
            //If nxl file was from myVault
            if(fp.isFromMyVault)
            {
                string pathId = "";
                ISearchFileInMyVault searchFileInMyVault = new SearchMyVaultFileByDuid();
                var results = searchFileInMyVault.SearchInRmsFiles(fp.duid);
                if (results != null)
                {
                    pathId = results.Path_Id;
                    if(results.Is_Revoked)
                    {
                        DisableShareStatus(new List<string>() { results.Shared_With_List });
                        return;
                    }
                }

                //Sanity check.
                if (string.IsNullOrEmpty(pathId))
                {
                    return;
                }
                //we will get its metadata to judge whether this file revoked or not.
                var md = app.Rmsdk.User.GetMyVaultFileMetaData(nxlLocalPath, pathId);
                if (md.isRevoked)
                {
                    DisableShareStatus(md.recipents);
                }
            }
        }

        private void DisableShareStatus(List<string> sharedWithList)
        {
            //Disable share button and comment widget
            //Hint user with "Rights to this file have been revoked for all users."
            emailInputTB.Visibility = Visibility.Collapsed;
            Ig_outlookEmai.Visibility = Visibility.Collapsed;
            StackComment.Visibility = Visibility.Collapsed;
            ShareBtPanel.Visibility = Visibility.Hidden;
            emailListsControl.Visibility = Visibility.Visible;
            emailListsControl.IsEnabled = false;

            foreach (var e in sharedWithList)
            {
                WrapEmail(e);
            }

            this.ShareWithTitleTB.Text = CultureStringInfo.ShareFileWin_Has_SharedWith;
            this.ShareText.Foreground = new SolidColorBrush(Color.FromRgb(0XFF, 0X00, 0X00));
            //this.ShareText.Text = CultureStringInfo.ShareFileWin_Notify_File_Share_Failed;
            this.ShareText.Text = CultureStringInfo.ShareFileWin_Rights_Revoked;
        }

        private void commentTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Cal length
            //usedLen = this.commentTB.Text.Length;

            Console.WriteLine("--text changed!!!----" + this.commentTB.Text);
            string sourceText = (e.Source as TextBox).Text;

            usedLen = sourceText.Length;
            int remainingLength = maxLen - usedLen;
            this.TB_RemaingLength.Text = remainingLength.ToString();

            int len = int.Parse(this.TB_RemaingLength.Text);
            if (int.Parse(this.TB_RemaingLength.Text) < 0)
            {
                this.Tb_PromptInfo.Visibility = Visibility.Visible;
                TB_RemaingLength.Foreground = new SolidColorBrush(Colors.Red);
                bindConfigs.ShareButtonEnabled = false;
            }
            else
            {
                this.Tb_PromptInfo.Visibility = Visibility.Collapsed;
                TB_RemaingLength.Foreground = new SolidColorBrush(Colors.Black);
                ShareBtnEnableByEmailList();
            }
        }

        private void OutlookAddEmail(bool shareBtnDisEnable, string addEmail)
        {
            if (!string.IsNullOrEmpty(addEmail))
            {
                bool emailExist = isEmailExisted(addEmail, sharedEmailLists);
                if (!emailExist)
                {
                    WrapEmail(addEmail);
                }
            }

            if (shareBtnDisEnable)
            {
                bindConfigs.ShareButtonEnabled = false;
            }
            else
            {
                //bindConfigs.ShareButtonEnabled = true;

                //Fix bug 55202
                ShareBtnEnableByEmailList();
            }
        }
        #endregion

        #region DoShare BackgroundWorker
        private void DoShare_Handler(object sender, DoWorkEventArgs args)
        {
            bool invoke = false;

            if (tempConfig.ShareNxlFeature != null)
            {
                // .nxl file do share In project
                var action = tempConfig.ShareNxlFeature.GetProjectShareAction();
                if (action == shareNxlFeature.ShareNxlFeature.ShareNxlAction.Share)
                {
                    string decryptPath;
                    bool isDecrypt = tempConfig.ShareNxlFeature.IsDecrypt(out decryptPath);
                    if (isDecrypt)
                    {
                        string[] filePath = new string[1];
                        filePath[0] = decryptPath;
                        tempConfig.FileOperation.FilePath = filePath;
                        invoke = CommonUtils.ProtectOrShare(tempConfig, out ErrorMsg);
                    }
                    else
                    {
                        invoke = false;
                        ErrorMsg = CultureStringInfo.Common_System_Internal_Error;
                    }
                }
            }
            else
            {
                // normal file do share.
                invoke = CommonUtils.ProtectOrShare(tempConfig, out ErrorMsg);
            }

            args.Result = invoke;
        }
        private void DoShareCompleted_Handler(object sender, RunWorkerCompletedEventArgs args)
        {
            bool invoke = (bool)args.Result;
            this.GridProBar.Visibility = Visibility.Collapsed;

            MenuDisableMgr.GetSingleton().IsSharing = false;
            if (MenuDisableMgr.GetSingleton().IsCanEnableMenu())
            {
                MenuDisableMgr.GetSingleton().MenuDisableNotify(CultureStringInfo.MenuItem_Logout, false);
                MenuDisableMgr.GetSingleton().MenuDisableNotify(CultureStringInfo.MenuItem_Exit, false);
            }

            if (invoke)
            {

                try
                {
                    // For project share to person
                    ProjectShareAddLog();
                    ProjectShare2PersonResetSourcePath();
                }
                catch(Exception e)
                {
                    Console.WriteLine(e);
                }

                //sdk will add a log, but we need to call upload explicitly
                app.User.UploadNxlFileLog_Async();

                ParentWindow.SwitchPage(new ShareSuccessPage(tempConfig) { ParentWindow = ParentWindow });

            }
            else
            {
                this.ShareText.Foreground= new SolidColorBrush(Color.FromRgb(0XFF, 0X00, 0X00));
                //this.ShareText.Text = CultureStringInfo.ShareFileWin_Notify_File_Share_Failed;
                this.ShareText.Text = ErrorMsg;
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
            if (action == shareNxlFeature.ShareNxlFeature.ShareNxlAction.Share)
            {
                app.User.AddNxlFileLog(sf.GetSourceNxlLocalPath(), NxlOpLog.Share, true);
            }
        }

        private bool ProjectShare2PersonResetSourcePath()
        {
            var tconfig = tempConfig;
            if(tconfig == null)
            {
                return false;
            }
            // Reset source path if project nxl file share to person[MyVault]
            var sf = tconfig?.ShareNxlFeature;
            if(sf == null)
            {
                return false;
            }
            var action = sf.GetProjectShareAction();
            // ADhoc Share to myVault
            if (action == shareNxlFeature.ShareNxlFeature.ShareNxlAction.Share)
            {
                // Get new created nxl file.
                var newNxl = tempConfig.CreatedFiles;
                if (newNxl.Count != 0)
                {
                    // We only support share one nxl at a time currently.
                    // So get first exist item will be fine.
                    var singleItem = newNxl[0];
                    // Here we get the newly created nxl file's local path.
                    var newNxlLocalPath = singleItem.LocalPath;
                    // Sanity check first.
                    if(string.IsNullOrEmpty(newNxlLocalPath))
                    {
                        return false;
                    }
                    var originalSourceNxlLocalPath = sf.GetSourceNxlLocalPath();
                    // Get original nxl source path.[Actually for project file Rms doesn't return this field.]
                    // We create a source file display path like myVaultFile do which indicates where the nxl file from.
                    // like: Project:{ProjectName}\{RmsDisplayPath}
                    var originalSourceNxlDisplayPath = GetProjectSourceNxlDisplayPath(originalSourceNxlLocalPath);
                    // Sanity check again.
                    if(string.IsNullOrEmpty(originalSourceNxlDisplayPath))
                    {
                        return false;
                    }

                    //return ProjectNxlShare2PersonResetSourcePath_Sdk(newNxlLocalPath, originalSourceNxlDisplayPath);

                    if (singleItem is PendingUploadFile)
                    {
                        PendingUploadFile file = singleItem as PendingUploadFile;
                        if (file.Raw is MyVaultLocalAddedFile)
                        {
                            MyVaultLocalAddedFile vaultLocalAddedFile = file.Raw as MyVaultLocalAddedFile;
                            vaultLocalAddedFile.OriginalPath = originalSourceNxlDisplayPath;
                            return true;
                        }
                    }
                    
                }
            }
            return false;
        }

        private string GetProjectSourceNxlDisplayPath(string localPath)
        {
            return string.Format("Project: {0}", GlobalSearch.GetProjectSourceNxlDisplayPath(localPath));
        }

        /// <summary>
        /// This api is used when share a nxl file from project to person, change the intermediate nxl file's path to orignal one. 
        /// The process of share nxl file to person is that:
        /// 1. Decript nxl file.[RPM decript]
        /// 2. [Share protect]Protect a new nxl file.
        /// 3. Upload the newly protected file.
        /// SDK will record the newly protected file's source path which which like C:\Users\hhu\AppData\Local\Nextlabs\SkyDRM\home\rms-centos7306.qapf1.qalab01.nextlabs.com\john.tyler@qapf1.qalab01.nextlabs.com\RPM\9c454343-a52a-45a0-b2ae-8b7d8931d63b\testSky.docx
        /// actually we need the orignal file's source file display path which like C:\Users\ncao\Desktop\3-deny-activity-log.docx
        /// </summary>
        /// <param name="nxlPath">The original nxl file path.</param>
        /// <returns></returns>
        private bool ProjectNxlShare2PersonResetSourcePath_Sdk(string newNxlLocalPath, string originalNxlSourcePath)
        {
            if (string.IsNullOrEmpty(newNxlLocalPath))
            {
                return false;
            }
            var app = SkydrmLocalApp.Singleton;

            return app.Rmsdk.User.ProjectNxlFileShare2PersonResetSourcePath(newNxlLocalPath, originalNxlSourcePath);
        }

        #endregion

        #region Do Update_Recipients BackgroundWorker
        private void U_Recipients_Handler(object sender, DoWorkEventArgs args)
        {
            //bool invoke = invokeSDKupdateRecipients(tempConfig);
            bool invoke = DoUpdateRecipients(tempConfig);

            args.Result = invoke;
        }
        private void U_RecipientsCompleted_Handler(object sender, RunWorkerCompletedEventArgs args)
        {
            bool invoke = (bool)args.Result;
            this.GridProBar.Visibility = Visibility.Collapsed;

            MenuDisableMgr.GetSingleton().IsSharing = false;
            if (MenuDisableMgr.GetSingleton().IsCanEnableMenu())
            {
                MenuDisableMgr.GetSingleton().MenuDisableNotify(CultureStringInfo.MenuItem_Logout, false);
                MenuDisableMgr.GetSingleton().MenuDisableNotify(CultureStringInfo.MenuItem_Exit, false);
            }


            if (invoke)
            {
                ParentWindow.SwitchPage(new ShareSuccessPage(tempConfig) { ParentWindow = ParentWindow});

            }
        }

        /// <summary>
        /// When nxl file do share should distinguish nxlFile FileRepo.
        ///  In myVault file, if the file 'isShared' property is ture that will use UpdateRecipients api. Or else use ShareRepository api.
        ///  In shareWithme file, use ReShare api.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        private bool DoUpdateRecipients(ProtectAndShareConfig config)
        {
            bool invoke = true;
            try
            {
                var fp = app.Rmsdk.User.GetNxlFileFingerPrint(config.FileOperation.FilePath[0]);

                var nxlLocalPath = config.FileOperation.FilePath[0];

                ISearchFileInMyVault SearchFileInMyVault = new SearchMyVaultFileByDuid();
                IMyVaultFile myVaultFile = SearchFileInMyVault.SearchInRmsFiles(fp.duid);
                if (myVaultFile != null)
                {
                    string[] removeEmail = new string[0];
                    myVaultFile?.ShareFile(nxlLocalPath, tempConfig.SharedWithConfig.SharedEmailLists.ToArray(), removeEmail, tempConfig.SharedWithConfig.Comments);
                    //Update the file 'share with' in MainWindow
                    app.MainWin.viewModel.UpdateFileShareWith(myVaultFile.Duid);
                    return true;
                }

                ISearchFileInSharedWithMe SearchFileInSharedWithMe = new SearchSharedWithMeFileByDuid();
                ISharedWithMeFile sharedWithMeFile = SearchFileInSharedWithMe.Search(fp.duid);
                if (sharedWithMeFile != null)
                {
                    string[] removeEmail = new string[0];
                    sharedWithMeFile?.ShareFile(nxlLocalPath, tempConfig.SharedWithConfig.SharedEmailLists.ToArray(), removeEmail);
                }

            }
            catch(RmRestApiException e)
            {
                invoke = false;
                if(e.ErrorCode == 4001) // File has been revoked.
                {
                    app.ShowBalloonTip(CultureStringInfo.ShareFileWin_Notify_File_Share_Failed_Because_revoked);
                } else
                {
                    app.ShowBalloonTip(CultureStringInfo.ShareFileWin_Notify_File_Share_Failed);
                }
            }
            catch (Exception msg)
            {
                // to do, will popup prompt info
                invoke = false;
                app.Log.Info("app.Session.User.UpdateRecipients is failed", msg);
                app.ShowBalloonTip(CultureStringInfo.ShareFileWin_Notify_File_Share_Failed);
            }

            return invoke;
        }

        //for U_Recipients backgroudWorker invoke SDK
        private bool invokeSDKupdateRecipients(ProtectAndShareConfig config)
        {
            bool Invoke = true;
            try
            {
                // if file has been uploaded, the recipients will immediately updated to server online, or esle, the recipients will cache into local,
                // and will sync into server  only when file upload into server.
                List<string> delEmail = new List<string>();
                app.Rmsdk.User.UpdateRecipients(tempConfig.FileOperation.FilePath[0], (List<string>)config.SharedWithConfig.SharedEmailLists, delEmail);
            }
            catch (Exception msg)
            {
                // to do, will popup prompt info
                Invoke = false;
                app.Log.Info("app.Session.User.UpdateRecipients is failed", msg);
                app.ShowBalloonTip(CultureStringInfo.ShareFileWin_Notify_File_Share_Failed);
            }

            return Invoke;
        }
        #endregion

        private void On_GetOutlookEmail_Btn(object sender, MouseButtonEventArgs e)
        {
            app.Log.Info("----IsClosingOutlookAddressBookWin----> " + parentWindow.IsClosingOutlookAddressBookWin);
            if (parentWindow.IsClosingOutlookAddressBookWin)
            {
                return;
            }

            Outlook outlook = new Outlook(parentWindow);
            outlook.UpdateEmailList += (ss, ee) =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    OutlookAddEmail(ee.Monitor, ee.Email);
                });
            };
            outlook.SelectNameDialog();

            //GlobalAddressListWindow addressListWindow = new GlobalAddressListWindow();
            //addressListWindow.EmailListUpdateEvent += (ss, ee) =>
            //{
            //    SpiltInputEmail(ee.EmailList);
            //};
            //addressListWindow.ShowDialog();
        }
    }

    public class SharePageBindConfigs : INotifyPropertyChanged
    {
        private string fileName;
        private string watermarkValue;
        private string validityValue;
        private bool shareButtonEnabled;
        private string operationTitle;
        private Visibility isOwnerVisibility = Visibility.Visible;

        public bool ShareButtonEnabled
        {
            get
            {
                return shareButtonEnabled;
            }
            set
            {
                shareButtonEnabled = value;
                OnPropertyChanged("ShareButtonEnabled");
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
        public string WatermarkValue
        {
            get
            {
                return watermarkValue;
            }
            set
            {
                watermarkValue = value;
                OnPropertyChanged("WatermarkValue");
            }
        }
        public string ValidityValue
        {
            get
            {
                return validityValue;
            }
            set
            {
                validityValue = value;
                OnPropertyChanged("ValidityValue");
            }
        }

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

        public Visibility IsOwnerVisibility
        {
            get
            {
                return isOwnerVisibility;
            }
            set
            {
                isOwnerVisibility = value;
                OnPropertyChanged("IsOwnerVisibility");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
