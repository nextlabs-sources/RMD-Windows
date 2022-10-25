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
using System.Windows.Media;

namespace SkydrmDesktop.rmc.ui.windows.fileOperationWindow.viewModel
{
    public class UpdateRecipientViewModel : BaseViewModel
    {
        private readonly SkydrmApp App = SkydrmApp.Singleton;
        private readonly FileRightsSharePage fileRightsSharePage = new FileRightsSharePage();
        private readonly FileRightsShareResultPage fileRightsShareResultPage = new FileRightsShareResultPage();

        private readonly BackgroundWorker DoUpdateRecipient_BgWorker = new BackgroundWorker();

        private IUpdateRecipients UpdateRecipiOpert { get; }

        private bool HasInitedFileRightsSharePageVM { get; set; }

        private ObservableCollection<string> emails = new ObservableCollection<string>();
        // Max length that allow to input
        private const int maxLen = 250;
        private bool msgExceedMax { get; set; }
        private string message { get; set; } = string.Empty;

        public UpdateRecipientViewModel(IUpdateRecipients operation, FileOperationWin win) : base(win)
        {
            this.UpdateRecipiOpert = operation;

            if (SanityCheckCanUpdateRecipi())
            {
                InitFRSharePageViewModel();
                InitFRSharePageCommand();
                Host.frm.Content = fileRightsSharePage;

                DoUpdateRecipient_BgWorker.DoWork += DoUpdateRecipient_BgWorker_DoWork;
                DoUpdateRecipient_BgWorker.RunWorkerCompleted += DoUpdateRecipient_BgWorker_RunWorkerCompleted;
            }
            else
            {
                Host.frm.Content = fileRightsShareResultPage;
            }
            
        }

        private bool SanityCheckCanUpdateRecipi()
        {
            if (UpdateRecipiOpert.NxlExpiration.type != SkydrmLocal.rmc.sdk.ExpiryType.NEVER_EXPIRE && DateTimeHelper.DateTimeToTimestamp(DateTime.Now) > UpdateRecipiOpert.NxlExpiration.End)
            {
                // special set label string
                fileRightsShareResultPage.ViewMode.ResultLabel = CultureStringInfo.ApplicationFindResource("UpdateRecipiOperation_ShareExpiredFile");
                fileRightsShareResultPage.ViewMode.ResultLabelForegrd= new SolidColorBrush(Color.FromRgb(0XFF, 0X00, 0X00));

                InitFRShareResultPageViewModel();
                InitFRShareResultPageCommand();
                return false;
            }
            if (UpdateRecipiOpert.IsRevoked(out List<string> sharedEmail))
            {
                // special set label string, email
                fileRightsShareResultPage.ViewMode.ResultLabel = CultureStringInfo.ApplicationFindResource("UpdateRecipiOperation_Rights_Revoked");
                fileRightsShareResultPage.ViewMode.ResultLabelForegrd = new SolidColorBrush(Color.FromRgb(0XFF, 0X00, 0X00));
                foreach (var item in sharedEmail)
                {
                    emails.Add(item);
                }

                InitFRShareResultPageViewModel();
                InitFRShareResultPageCommand();
                return false;
            }
            return true;
        }
        

        #region Init FileRightsSharePage
        private void InitFRSharePageViewModel()
        {
            fileRightsSharePage.ViewMode.SavePath = UpdateRecipiOpert.CurrentSelectedSavePath.DestDisplayPath;

            fileRightsSharePage.ViewMode.EmailTB_PreviewKeyDown += FRSharePage_EmailTB_PreviewKeyDown;
            fileRightsSharePage.ViewMode.EmailTB_TextChanged += FRSharePage_EmailTB_TextChanged;
            fileRightsSharePage.ViewMode.DeleteEmailItem_MouseLeftButtonUp += FRSharePage_DeleteEmailItem_MouseLeftButtonUp;
            fileRightsSharePage.ViewMode.MessageTB_TextChanged += FRSharePage_MessageTB_TextChanged;

            fileRightsSharePage.ViewMode.EmailList = emails;

            fileRightsSharePage.ViewMode.MsgStpVisibility = UpdateRecipiOpert.IsOwner ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

            fileRightsSharePage.ViewMode.PositiveBtnContent = CultureStringInfo.ApplicationFindResource("ShareOperation_Btn_Protect");
            fileRightsSharePage.ViewMode.BackBtnVisibility = System.Windows.Visibility.Collapsed;

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
            int fileCount = UpdateRecipiOpert.FileInfo.FileName.Length;
            fileRightsSharePage.ViewMode.Caption4VM.Title = CultureStringInfo.ApplicationFindResource("ShareOperation_Win_Title");
            if (fileCount > 1)
            {
                fileRightsSharePage.ViewMode.Caption4VM.SelectFileDesc = CultureStringInfo.ApplicationFindResource("ProtectOperation_SelectFiles");
            }
            fileRightsSharePage.ViewMode.Caption4VM.FileCount = fileCount;
            fileRightsSharePage.ViewMode.Caption4VM.FileNameList.Clear();
            for (int i = 0; i < fileCount; i++)
            {
                fileRightsSharePage.ViewMode.Caption4VM.FileNameList.Add(new FileModel()
                { Name = UpdateRecipiOpert.FileInfo.FileName[i], FullName = UpdateRecipiOpert.FileInfo.FileName[i] });
            }

            // file rights
            fileRightsSharePage.ViewMode.AdhocAndClassifiedRightsVM.FileType = CustomControls.components.FileType.Adhoc;

            bool isAddWarterMark = !string.IsNullOrWhiteSpace(UpdateRecipiOpert.NxlAdhocWaterMark.text);
            fileRightsSharePage.ViewMode.AdhocAndClassifiedRightsVM.AdhocRightsVM.FileRights = DataTypeConvertHelper.SDKRights2CustomControlRights(UpdateRecipiOpert.NxlRights.ToArray(), isAddWarterMark);
            fileRightsSharePage.ViewMode.AdhocAndClassifiedRightsVM.AdhocRightsVM.WatermarkValue = UpdateRecipiOpert.NxlAdhocWaterMark.text;
            fileRightsSharePage.ViewMode.AdhocAndClassifiedRightsVM.AdhocRightsVM.WaterPanlVisibility = isAddWarterMark ?
                 System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            DataTypeConvertHelper.SdkExpiry2CustomCtrExpiry(UpdateRecipiOpert.NxlExpiration, out string expiryDate, false);
            fileRightsSharePage.ViewMode.AdhocAndClassifiedRightsVM.AdhocRightsVM.ValidityValue = expiryDate;

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
            binding = new CommandBinding(FRShare_DataCommands.Cancel);
            binding.Executed += CancelCommand;
            Host.CommandBindings.Add(binding);
        }

        private void FRShareAddEmailCommand(object sender, ExecutedRoutedEventArgs e)
        {
            Task.Factory.StartNew(() => {
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
            if (!App.MainWin.viewModel.IsNetworkAvailable)
            {
                App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("UpdateRecipiOperation_ShareFailed_NoNetwork"), false, 
                    UpdateRecipiOpert.FileInfo.FileName[0]);
                return;
            }
            if (UpdateRecipiOpert.FileInfo.FileName[0].EndsWith(".nxl") && !FileHelper.Exist(UpdateRecipiOpert.FileInfo.FilePath[0]))
            {
                App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Common_File_Not_Exist"), false, UpdateRecipiOpert.FileInfo.FileName[0]);
                return;
            }

            StartDoUpdateRecipient_BgWorker();
        }
        #endregion

        #region Init FileRightsShareResultPage
        private void InitFRShareResultPageViewModel()
        {
            // caption
            int fileCount = UpdateRecipiOpert.FileInfo.FileName.Length;

            fileRightsShareResultPage.ViewMode.Caption4VM.Title = CultureStringInfo.ApplicationFindResource("ShareOperation_Win_Title");
            if (fileCount > 1)
            {
                fileRightsShareResultPage.ViewMode.Caption4VM.SelectFileDesc = CultureStringInfo.ApplicationFindResource("ProtectOperation_SelectFiles");
            }
            fileRightsShareResultPage.ViewMode.Caption4VM.FileCount = fileCount;
            fileRightsShareResultPage.ViewMode.Caption4VM.FileNameList.Clear();
            for (int i = 0; i < fileCount; i++)
            {
                fileRightsShareResultPage.ViewMode.Caption4VM.FileNameList.Add(new FileModel()
                { Name = UpdateRecipiOpert.FileInfo.FileName[i], FullName = UpdateRecipiOpert.FileInfo.FileName[i] });
            }

            fileRightsShareResultPage.ViewMode.AdhocAndClassifiedRightsVM.FileType = CustomControls.components.FileType.Adhoc;

            bool isAddWarterMark = !string.IsNullOrWhiteSpace(UpdateRecipiOpert.NxlAdhocWaterMark.text);
            fileRightsShareResultPage.ViewMode.AdhocAndClassifiedRightsVM.AdhocRightsVM.FileRights = DataTypeConvertHelper.SDKRights2CustomControlRights(UpdateRecipiOpert.NxlRights.ToArray(), isAddWarterMark);
            fileRightsShareResultPage.ViewMode.AdhocAndClassifiedRightsVM.AdhocRightsVM.WatermarkValue = UpdateRecipiOpert.NxlAdhocWaterMark.text;
            fileRightsShareResultPage.ViewMode.AdhocAndClassifiedRightsVM.AdhocRightsVM.WaterPanlVisibility = isAddWarterMark ?
                 System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            DataTypeConvertHelper.SdkExpiry2CustomCtrExpiry(UpdateRecipiOpert.NxlExpiration, out string expiryDate, false);
            fileRightsShareResultPage.ViewMode.AdhocAndClassifiedRightsVM.AdhocRightsVM.ValidityValue = expiryDate;

            fileRightsShareResultPage.ViewMode.MsgStpVisibility = UpdateRecipiOpert.IsOwner ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

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

        private void StartDoUpdateRecipient_BgWorker()
        {
            if (!DoUpdateRecipient_BgWorker.IsBusy)
            {
                DoUpdateRecipient_BgWorker.RunWorkerAsync();

                // display progrss bar UI
                GridProBarVisible = System.Windows.Visibility.Visible;

                MenuDisableMgr.GetSingleton().IsSharing = true;
                MenuDisableMgr.GetSingleton().MenuDisableNotify("LOGOUT", true);
                MenuDisableMgr.GetSingleton().MenuDisableNotify("EXIT", true);
            }
        }
        private void DoUpdateRecipient_BgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            bool result = false;

            result = UpdateRecipiOpert.UpdateRecipients(emails.ToList(), new List<string>(), message);

            e.Result = result;
        }
        private void DoUpdateRecipient_BgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            bool result = (bool)e.Result;
            GridProBarVisible = System.Windows.Visibility.Collapsed;

            MenuDisableMgr.GetSingleton().IsSharing = false;
            if (MenuDisableMgr.GetSingleton().IsCanEnableMenu())
            {
                MenuDisableMgr.GetSingleton().MenuDisableNotify("LOGOUT", false);
                MenuDisableMgr.GetSingleton().MenuDisableNotify("EXIT", false);
            }

            if (!result)
            {
                return;
            }
            // init FileRightsShareResultPage
            InitFRShareResultPageViewModel();
            InitFRShareResultPageCommand();

            Host.frm.Content = fileRightsShareResultPage;

        }

    }
}
