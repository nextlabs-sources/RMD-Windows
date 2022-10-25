using Alphaleonis.Win32.Filesystem;
using CustomControls;
using CustomControls.components;
using SkydrmDesktop.Resources.languages;
using SkydrmDesktop.rmc.app.process.outlook;
using SkydrmDesktop.rmc.ui.windows.fileOperationWindow.operation;
using SkydrmDesktop.rmc.ui.windows.fileOperationWindow.view;
using SkydrmLocal.rmc.common.component;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.ui.utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace SkydrmDesktop.rmc.ui.windows.fileOperationWindow.viewModel
{
    public class ProtectAndShareViewModel : BaseViewModel
    {
        private readonly SkydrmApp App = SkydrmApp.Singleton;
        private readonly FileRightsSelectPage fileRightsSelectPage = new FileRightsSelectPage();
        private readonly FileRightsSharePage fileRightsSharePage = new FileRightsSharePage();
        private readonly FileRightsShareResultPage fileRightsShareResultPage = new FileRightsShareResultPage();

        private readonly BackgroundWorker DoShare_BgWorker = new BackgroundWorker();

        private IProtectAndShare ShareOpert { get; }

        private bool HasInitedFileRightsSharePageVM { get; set; }

        private ObservableCollection<string> emails = new ObservableCollection<string>();
        // Max length that allow to input
        private const int maxLen = 250;
        private bool msgExceedMax { get; set; }
        private string message { get; set; }
        // use for notify MainWindow
        private List<SkydrmLocal.rmc.fileSystem.basemodel.INxlFile> createdFiles;

        public ProtectAndShareViewModel(IProtectAndShare operation, FileOperationWin win) : base(win)
        {
            this.ShareOpert = operation;

            InitCaptionDescCommand();

            InitFRSelectPageViewModel();
            InitFRSelectPageCommand();
            Host.frm.Content = fileRightsSelectPage;

            DoShare_BgWorker.DoWork += DoShare_BgWorker_DoWork;
            DoShare_BgWorker.RunWorkerCompleted += DoShare_BgWorker_RunWorkerCompleted;
        }

        #region Binding CaptionDesc Command
        private void InitCaptionDescCommand()
        {
            CommandBinding binding;
            binding = new CommandBinding(CustomControls.components.CapD_DataCommands.Change);
            binding.Executed += ChangeFileCommand;
            Host.CommandBindings.Add(binding);
        }
        private void ChangeFileCommand(object sender, ExecutedRoutedEventArgs e)
        {
            model.OperateFileInfo tempFileInfo = ShareOpert.FileInfo;

            //init Dir.
            string filePath;
            string[] selectedFile = null;

            try
            {
                // --- Also can use System.Windows.Forms.FolderBrowserDialog!
                Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
                dialog.Title = "Select File";

                filePath = tempFileInfo.FilePath[0].Substring(0,
                    tempFileInfo.FilePath[0].LastIndexOf(Path.DirectorySeparatorChar) + 1);

                if (!Directory.Exists(filePath) || filePath.Contains(App.User.WorkingFolder))
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

                    if (!ProtectFileHelper.CheckFilePathDoProtect(selectedFile, out string tag, out List<string> rightFilePath))
                    {
                        return;
                    }

                    model.OperateFileInfo fileInfo = new model.OperateFileInfo(rightFilePath.ToArray(), null, tempFileInfo.FromSource);
                    StringBuilder sb = new StringBuilder();
                    int length = fileInfo.FileName.Length;

                    for (int i = 0; i < length; i++)
                    {
                        sb.Append(fileInfo.FileName[i]);
                        if (i != length - 1)
                        {
                            sb.Append(";\n");
                        }
                    }
                    //update all page captionDesc.
                    fileRightsSelectPage.ViewMode.CaptionVM.Title = length > 1 ?
                        CultureStringInfo.ApplicationFindResource("ProtectOperation_Win_Titles") : //For multiple files protect title display.
                        CultureStringInfo.ApplicationFindResource("ProtectOperation_Win_Title"); //For single file protect title display.;
                    fileRightsSelectPage.ViewMode.CaptionVM.FileCount = length;
                    fileRightsSelectPage.ViewMode.CaptionVM.FileName = sb.ToString();

                    ShareOpert.FileInfo = fileInfo;
                }
            }
            catch (Exception msg)
            {
                App.Log.Warn("Exception in OpenFileDialog," + msg, msg);
            }
        }
        private string GetFileDirectory(string[] path)
        {
            if (path.Length < 1)
            {
                return "";
            }
            string fileDirectory = Path.GetDirectoryName(path[0]);
            if (!fileDirectory.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                fileDirectory = fileDirectory + Path.DirectorySeparatorChar;
            }

            int option;
            string tags;
            if (App.Rmsdk.RMP_IsSafeFolder(fileDirectory,out option,out tags) || fileDirectory.Contains(App.User.WorkingFolder))
            {
                fileDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            }

            return fileDirectory;
        }
        #endregion

        #region Init FileRightsSelectPage
        private void InitFRSelectPageViewModel()
        {
            // caption
            int fileCount = ShareOpert.FileInfo.FileName.Length;
            fileRightsSelectPage.ViewMode.CaptionDescVisible = System.Windows.Visibility.Collapsed;
            fileRightsSelectPage.ViewMode.Caption4VM.Title = CultureStringInfo.ApplicationFindResource("ShareOperation_Win_Title");
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
                { Name = ShareOpert.FileInfo.FileName[i], FullName = ShareOpert.FileInfo.FileName[i] });
            }

            // display path
            fileRightsSelectPage.ViewMode.SavePath = ShareOpert.CurrentSelectedSavePath.DestDisplayPath;
            fileRightsSelectPage.ViewMode.ChangDestBtnVisible = System.Windows.Visibility.Collapsed;

            fileRightsSelectPage.ViewMode.ProtectType = ProtectType.Adhoc;
            fileRightsSelectPage.ViewMode.CentralRadioIsEnable = false;
            //wartermark, expiration 
            fileRightsSelectPage.ViewMode.AdhocPage_ViewModel.Watermarkvalue = App.User.Watermark.text;
            fileRightsSelectPage.ViewMode.AdhocPage_ViewModel.Expiry = DataTypeConvertHelper.SdkExpiry2CustomCtrExpiry(App.User.Expiration,
                out string expiryDate, true);

            // set extract rights disable
            fileRightsSelectPage.ViewMode.AdhocPage_ViewModel.UncheckRights = new HashSet<CustomControls.components.DigitalRights.model.FileRights>()
                    { CustomControls.components.DigitalRights.model.FileRights.RIGHT_DECRYPT };
            fileRightsSelectPage.ViewMode.AdhocPage_ViewModel.DisableRights = new HashSet<CustomControls.components.DigitalRights.model.FileRights>()
                    { CustomControls.components.DigitalRights.model.FileRights.RIGHT_DECRYPT };

            fileRightsSelectPage.ViewMode.PositiveBtnContent = CultureStringInfo.ApplicationFindResource("ShareOperation_Btn_Protect");
        }
        private void InitFRSelectPageCommand()
        {
            CommandBinding binding;
            binding = new CommandBinding(FRS_DataCommands.Positive);
            binding.Executed += FRightsSelectPositiveCommand;
            Host.CommandBindings.Add(binding);
            binding = new CommandBinding(FRS_DataCommands.Cancel);
            binding.Executed += CancelCommand;
            Host.CommandBindings.Add(binding);
        }
        private void FRightsSelectPositiveCommand(object sender, ExecutedRoutedEventArgs e)
        {
            bool isCentralPolicy = fileRightsSelectPage.ViewMode.ProtectType == ProtectType.CentralPolicy ? true : false;
            // save radio selected state,
            App.User.IsCentralPlcRadio = isCentralPolicy;

            var sdkExpiry = DataTypeConvertHelper.CustomCtrExpiry2SdkExpiry(fileRightsSelectPage.ViewMode.AdhocPage_ViewModel.Expiry);
            if (sdkExpiry.type != SkydrmLocal.rmc.sdk.ExpiryType.NEVER_EXPIRE && DateTimeHelper.DateTimeToTimestamp(DateTime.Now) > sdkExpiry.End)
            {
                App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("ProtectOperation_InValidExpiry"), false);
                return;
            }

            // init FileRightsSharePage
            if (!HasInitedFileRightsSharePageVM)
            {
                HasInitedFileRightsSharePageVM = true;
                InitFRSharePageViewModel();
                InitFRSharePageCommand();
            }
            else
            {
                InitFRSharePageSubViewModel();
            }
            Host.frm.Content = fileRightsSharePage;
        }
        #endregion

        #region Init FileRightsSharePage
        private void InitFRSharePageViewModel()
        {
            fileRightsSharePage.ViewMode.SavePath = ShareOpert.CurrentSelectedSavePath.DestDisplayPath;

            fileRightsSharePage.ViewMode.EmailTB_PreviewKeyDown += FRSharePage_EmailTB_PreviewKeyDown;
            fileRightsSharePage.ViewMode.EmailTB_TextChanged += FRSharePage_EmailTB_TextChanged;
            fileRightsSharePage.ViewMode.DeleteEmailItem_MouseLeftButtonUp += FRSharePage_DeleteEmailItem_MouseLeftButtonUp;
            fileRightsSharePage.ViewMode.MessageTB_TextChanged += FRSharePage_MessageTB_TextChanged;

            fileRightsSharePage.ViewMode.EmailList = emails;

            fileRightsSharePage.ViewMode.PositiveBtnContent = CultureStringInfo.ApplicationFindResource("ShareOperation_Btn_Protect");

            InitFRSharePageSubViewModel();
        }

        private void FRSharePage_EmailTB_PreviewKeyDown(object sender, System.Windows.RoutedEventArgs e)
        {
            if (e is KeyEventArgs)
            {
                KeyEventArgs ke = e as KeyEventArgs;
                if (ke.Key != Key.Enter)
                {
                    return;
                }

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
        private void FRSharePage_EmailTB_TextChanged(object sender, System.Windows.RoutedEventArgs e)
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
        }
        private void WrapEmail(string email)
        {
            string trimedText = email.Trim();
            bool isemailExist = isEmailExisted(trimedText, emails);
            if (isemailExist)
            {
                App.ShowBalloonTip(trimedText + "\n" + CultureStringInfo.ApplicationFindResource("ShareOperation_EmailExist"), false);
                return;
            }
            if (IsEmailValid(trimedText))
            {
                emails.Add(trimedText);
            }
            else
            {
                App.ShowBalloonTip(trimedText + "\n" + CultureStringInfo.ApplicationFindResource("ShareOperation_EmailInvalid"), false);
                return;
            }
        }
        private bool isEmailExisted(String email, IList<String> emailList)
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
        private bool IsEmailValid(string email)
        {
            if (email.Equals(" ") || email == null)
            {
                return false;
            }
            string regExpn = @"^\w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$";
            if (Regex.IsMatch(email, regExpn))
            {
                return true;
            }
            return false;
        }
        private void FRSharePage_DeleteEmailItem_MouseLeftButtonUp(object sender, System.Windows.RoutedEventArgs e)
        {
            var item = sender as Image;
            if (item != null)
            {
                //Cannot from ItemsControl get the click item. So Choose to bind the emails as a Tag of the delete image.
                //When found a delete event send by the image,just get the the binded tag from it then operate it.
                string emailName = (string)item.Tag;
                string findEmail = string.Empty;
                foreach (string email in emails)
                {
                    if (emailName.Equals(email))
                    {
                        findEmail = email;
                        break;
                    }
                }
                if (!string.IsNullOrEmpty(findEmail))
                {
                    emails.Remove(findEmail);
                }
            }
        }
       
        private void FRSharePage_MessageTB_TextChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            string sourceText = (e.Source as TextBox).Text;
            message = sourceText;

            int usedLen = sourceText.Length;
            int remainingLength = maxLen - usedLen;
            fileRightsSharePage.ViewMode.RemainCharacters = remainingLength.ToString();

            if (remainingLength < 0)
            {
                fileRightsSharePage.ViewMode.MsgExceedInfoVisibility = System.Windows.Visibility.Visible;
                msgExceedMax = true;
            }
            else
            {
                fileRightsSharePage.ViewMode.MsgExceedInfoVisibility = System.Windows.Visibility.Collapsed;
                msgExceedMax = false;
            }
        }

        private void InitFRSharePageSubViewModel()
        {
            // caption
            int fileCount = ShareOpert.FileInfo.FileName.Length;
            fileRightsSharePage.ViewMode.Caption4VM.Title = CultureStringInfo.ApplicationFindResource("ShareOperation_Win_Title");
            if (fileCount > 1)
            {
                fileRightsSharePage.ViewMode.Caption4VM.SelectFileDesc = CultureStringInfo.ApplicationFindResource("ProtectOperation_SelectFiles");
                fileRightsSharePage.ViewMode.AdhocAndClassifiedRightsVM.AdhocRightsApplyDesc = CultureStringInfo.ApplicationFindResource("ProtectOperation_RightsApplyFiles");
                fileRightsSharePage.ViewMode.SavePathLabel = CultureStringInfo.ApplicationFindResource("ShareOperation_SaveFiles_Label");
            }
            fileRightsSharePage.ViewMode.Caption4VM.FileCount = fileCount;
            fileRightsSharePage.ViewMode.Caption4VM.FileNameList.Clear();
            for (int i = 0; i < fileCount; i++)
            {
                fileRightsSharePage.ViewMode.Caption4VM.FileNameList.Add(new FileModel()
                { Name = ShareOpert.FileInfo.FileName[i], FullName = ShareOpert.FileInfo.FileName[i] });
            }

            // file rights
            fileRightsSharePage.ViewMode.AdhocAndClassifiedRightsVM.FileType = CustomControls.components.FileType.Adhoc;

            var frSelectAdhocVM = fileRightsSelectPage.ViewMode.AdhocPage_ViewModel;
            fileRightsSharePage.ViewMode.AdhocAndClassifiedRightsVM.AdhocRightsVM.FileRights = frSelectAdhocVM.Rights;
            fileRightsSharePage.ViewMode.AdhocAndClassifiedRightsVM.AdhocRightsVM.WatermarkValue = frSelectAdhocVM.Watermarkvalue;
            fileRightsSharePage.ViewMode.AdhocAndClassifiedRightsVM.AdhocRightsVM.WaterPanlVisibility = frSelectAdhocVM.Rights.Contains(CustomControls.components.DigitalRights.model.FileRights.RIGHT_WATERMARK) ?
                 System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            fileRightsSharePage.ViewMode.AdhocAndClassifiedRightsVM.AdhocRightsVM.ValidityValue = frSelectAdhocVM.ExpireDateValue;

        }
        private void InitFRSharePageCommand()
        {
            CommandBinding binding;
            binding = new CommandBinding(FRShare_DataCommands.AddEmail);
            binding.Executed += FRShareAddEmailCommand;
            Host.CommandBindings.Add(binding);
            binding = new CommandBinding(FRShare_DataCommands.Positive);
            binding.CanExecute += FRSharePositiveCommand_CanExecute;
            binding.Executed += FRSharePositiveCommand;
            Host.CommandBindings.Add(binding);
            binding = new CommandBinding(FRShare_DataCommands.Back);
            binding.Executed += FRShareBackCommand;
            Host.CommandBindings.Add(binding);
            binding = new CommandBinding(FRShare_DataCommands.Cancel);
            binding.Executed += CancelCommand;
            Host.CommandBindings.Add(binding);
        }

        private void FRShareAddEmailCommand(object sender, ExecutedRoutedEventArgs e)
        {
            Task.Factory.StartNew(()=> {
                Outlook outlook = new Outlook();
                outlook.UpdateEmailList += (ss, ee) =>
                {
                    Host.Dispatcher.Invoke(() => {
                        OutlookAddEmail(ee.Monitor, ee.Email);
                    });
                };
                outlook.SelectNameDialog();
            });
        }
        private void OutlookAddEmail(bool monitor, string addEmail)
        {
            if (!string.IsNullOrEmpty(addEmail))
            {
                bool emailExist = isEmailExisted(addEmail, emails);
                if (!emailExist)
                {
                    WrapEmail(addEmail);
                }
            }

            if (monitor)
            {
                // display progrss bar UI
                GridProBarVisible = System.Windows.Visibility.Visible;
                MenuDisableMgr.GetSingleton().IsSharing = true;
                MenuDisableMgr.GetSingleton().MenuDisableNotify("LOGOUT", true);
                MenuDisableMgr.GetSingleton().MenuDisableNotify("EXIT", true);
            }
            else
            {
                GridProBarVisible = System.Windows.Visibility.Collapsed;
                MenuDisableMgr.GetSingleton().IsSharing = false;
                MenuDisableMgr.GetSingleton().MenuDisableNotify("LOGOUT", true);
                MenuDisableMgr.GetSingleton().MenuDisableNotify("EXIT", true);
            }
        }
        private void FRSharePositiveCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (emails.Count > 0 && !msgExceedMax)
            {
                e.CanExecute = true;
            }
        }
        private void FRSharePositiveCommand(object sender, ExecutedRoutedEventArgs e)
        {
            StartDoShare_BgWorker();
        }
        private void FRShareBackCommand(object sender, ExecutedRoutedEventArgs e)
        {
            Host.frm.Content = fileRightsSelectPage;
        }
        #endregion

        #region Init FileRightsShareResultPage
        private void InitFRShareResultPageViewModel()
        {
            // caption
            int fileCount = ShareOpert.FileInfo.FileName.Length;
            fileRightsShareResultPage.ViewMode.Caption4VM.Title = CultureStringInfo.ApplicationFindResource("ShareOperation_Win_Title");
            if (fileCount > 1)
            {
                fileRightsShareResultPage.ViewMode.Caption4VM.SelectFileDesc = CultureStringInfo.ApplicationFindResource("ProtectOperation_SelectFiles");
                fileRightsShareResultPage.ViewMode.AdhocAndClassifiedRightsVM.AdhocRightsApplyDesc = CultureStringInfo.ApplicationFindResource("ProtectOperation_RightsApplyFiles");
            }
            fileRightsShareResultPage.ViewMode.Caption4VM.FileCount = fileCount;
            fileRightsShareResultPage.ViewMode.Caption4VM.FileNameList.Clear();
            for (int i = 0; i < fileCount; i++)
            {
                fileRightsShareResultPage.ViewMode.Caption4VM.FileNameList.Add(new FileModel()
                { Name = ShareOpert.FileInfo.FileName[i], FullName = ShareOpert.FileInfo.FileName[i] });
            }

            fileRightsShareResultPage.ViewMode.AdhocAndClassifiedRightsVM.FileType = CustomControls.components.FileType.Adhoc;

            var frShareAdhocVM = fileRightsSharePage.ViewMode.AdhocAndClassifiedRightsVM.AdhocRightsVM;
            var fileRightsSelectVM = fileRightsSelectPage.ViewMode.AdhocPage_ViewModel;
            fileRightsShareResultPage.ViewMode.AdhocAndClassifiedRightsVM.AdhocRightsVM.FileRights = frShareAdhocVM.FileRights;
            fileRightsShareResultPage.ViewMode.AdhocAndClassifiedRightsVM.AdhocRightsVM.WatermarkValue = fileRightsSelectVM.Watermarkvalue;
            fileRightsShareResultPage.ViewMode.AdhocAndClassifiedRightsVM.AdhocRightsVM.WaterPanlVisibility = frShareAdhocVM.WaterPanlVisibility;
            fileRightsShareResultPage.ViewMode.AdhocAndClassifiedRightsVM.AdhocRightsVM.ValidityValue = frShareAdhocVM.ValidityValue;

            fileRightsShareResultPage.ViewMode.EmailList = emails;
            fileRightsShareResultPage.ViewMode.Message = message;

        }
        private void InitFRShareResultPageCommand()
        {
            CommandBinding binding;
            binding = new CommandBinding(FRShareResult_DataCommands.Close);
            binding.Executed += CancelCommand;
            Host.CommandBindings.Add(binding);
        }
        #endregion

        private void CancelCommand(object sender, ExecutedRoutedEventArgs e)
        {
            Host.Close();
        }

        private void StartDoShare_BgWorker()
        {
            if (!DoShare_BgWorker.IsBusy)
            {
                DoShare_BgWorker.RunWorkerAsync();

                // display progrss bar UI
                GridProBarVisible = System.Windows.Visibility.Visible;

                MenuDisableMgr.GetSingleton().IsSharing = true;
                MenuDisableMgr.GetSingleton().MenuDisableNotify("LOGOUT", true);
                MenuDisableMgr.GetSingleton().MenuDisableNotify("EXIT", true);
            }
        }
        private void DoShare_BgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            bool result = false;
            var sdkRights = DataTypeConvertHelper.CustomCtrRights2SDKRights(fileRightsSelectPage.ViewMode.AdhocPage_ViewModel.Rights);
            SkydrmLocal.rmc.sdk.WaterMarkInfo warterMark = new SkydrmLocal.rmc.sdk.WaterMarkInfo() { text="", fontColor="", fontName=""};
            warterMark.text = fileRightsSelectPage.ViewMode.AdhocPage_ViewModel.Watermarkvalue;
            var sdkExpiry = DataTypeConvertHelper.CustomCtrExpiry2SdkExpiry(fileRightsSelectPage.ViewMode.AdhocPage_ViewModel.Expiry);

            var listNxlFile = ShareOpert.ProtectAndShareFile(sdkRights, warterMark, sdkExpiry, emails.ToList(), message);

            if (listNxlFile.Count > 0)
            {
                createdFiles = listNxlFile;
                result = true;
            }

            e.Result = result;
        }
        private void DoShare_BgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            bool result = (bool)e.Result;
            GridProBarVisible = System.Windows.Visibility.Collapsed;

            MenuDisableMgr.GetSingleton().IsSharing = false;
            if (MenuDisableMgr.GetSingleton().IsCanEnableMenu())
            {
                MenuDisableMgr.GetSingleton().MenuDisableNotify("LOGOUT", false);
                MenuDisableMgr.GetSingleton().MenuDisableNotify("EXIT", false);
            }

            if (ShareOpert.FileInfo.FailedFileName.Count > 0)
            {
                App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("ProtectOperation_Protect_Failed"), false, "", "Protect failed");
            }

            if (!result)
            {
                return;
            }
            // init FileRightsShareResultPage
            InitFRShareResultPageViewModel();
            InitFRShareResultPageCommand();

            Host.Closed += (ss, ee) =>
            {
                if (createdFiles?.Count > 0)
                {
                    App.MainWin.viewModel.GetCreatedFile(createdFiles);
                }
            };

            //sdk will add a log, but we need to call upload explicitly
            App.User.UploadNxlFileLog_Async();

            Host.frm.Content = fileRightsShareResultPage;

        }

    }
}
