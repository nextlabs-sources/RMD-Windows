using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using Viewer.database;
using Viewer.utils;

namespace Viewer.share
{
    public class SharePageViewModel : INotifyPropertyChanged
    {
        private string fileName;
        private string watermark;
        private string validity;
        private string operationTitle;
        private ObservableCollection<RightsItem> rights = new ObservableCollection<RightsItem>();
        private ObservableCollection<EmailItem> emails = new ObservableCollection<EmailItem>();
        private Int32 status;
        private string comments;
        private string message;

        //manage email List, whether its valid or not
        private List<string> mSharedEmailLists = new List<string>();
        private List<string> mDirtyEmailLists = new List<string>();

        private ViewerApp mViewerInstance;
        private ShareWindow mParentWindow;
        private log4net.ILog mLog;

        // Max length that allow to input
        private const int maxLen = 250;
        private NxlFileFingerPrint mNxlFileFingerPrint;
        private ShareAdhocFileInProject mShareAdhocFileInProject;
        private UpdateRecipients mUpdateRecipients;
        private ReShare mReShare;
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
        public string Watermark
        {
            get
            {
                return watermark;
            }
            set
            {
                watermark = value;
                OnPropertyChanged("Watermark");
            }
        }
        public string Validity
        {
            get
            {
                return validity;
            }
            set
            {
                validity = value;
                OnPropertyChanged("Validity");
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
        public ObservableCollection<RightsItem> Rights
        {
            get
            {
                return rights;
            }
            set
            {
                rights = value;
                OnPropertyChanged("Rights");
            }
        }
        public ObservableCollection<EmailItem> Emails
        {
            get
            {
                return emails;
            }
            set
            {
                emails = value;
                OnPropertyChanged("Emails");
            }
        }
        public Int32 Status
        {
            get
            {
                return status;
            }
            set
            {
                status = value;
                OnPropertyChanged("Status");
            }
        }
        public string Comments
        {
            get
            {
                return comments;
            }
            set
            {
                comments = value;
                OnPropertyChanged("Comments");
            }
        }
        public string Message
        {
            get
            {
                return message;
            }
            set
            {
                message = value;
                OnPropertyChanged("Message");
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public SharePageViewModel(string nxlFilePath, ShareWindow parentWindow)
        {
            this.mParentWindow = parentWindow;

            try
            {
                mViewerInstance = (ViewerApp)System.Windows.Application.Current;
                mLog = mViewerInstance.Log;
                mNxlFileFingerPrint = mViewerInstance.User.GetNxlFileFingerPrint(nxlFilePath);
                InitData(mNxlFileFingerPrint);
            }
            catch (RmSdkException e)
            {
                mViewerInstance.Log.Error(e);
            }
            catch (Exception e)
            {
                mViewerInstance.Log.Error(e);
            }
        }

        public SharePageViewModel(NxlFileFingerPrint nxlFileFingerPrint, ShareWindow parentWindow)
        {
            mParentWindow = parentWindow;
            this.mNxlFileFingerPrint = nxlFileFingerPrint;
            InitData(nxlFileFingerPrint);
        }

        private void ShowProgressBar()
        {
            Status = Status | ShareStatus.PROGRESS_BAR;
        }

        private void HideProgressBar()
        {
            Status = Status ^ ShareStatus.PROGRESS_BAR;
        }

        private void InitData(NxlFileFingerPrint nxlFileFingerPrint)
        {
            FileName = nxlFileFingerPrint.name;
            Watermark = nxlFileFingerPrint.adhocWatermark;
            Validity = TransformValidity(nxlFileFingerPrint.expiration);
            // ShareButtonEnabled = true;
         //   Status = Status | ShareStatus.SHARE_BUTTON_ENABLED;

            OperationTitle = "Share a protected file";
            AddRightsItem(nxlFileFingerPrint.Helper_GetRightsStr(), ref rights);
            //// check expiry, fix bug 56452 
            if (CheckExpired(mNxlFileFingerPrint))
            {
                Status = Status | ShareStatus.EXPIRED;
                Message = CultureStringInfo.Expired;
            }

            // RemaingLength = maxLen.ToString();
            mShareAdhocFileInProject = new ShareAdhocFileInProject(ShareProjectFileCompletedEventHandler);
            mUpdateRecipients = new UpdateRecipients(UpdateRecipientsCompletedEventHandler);
            mReShare = new ReShare(ReShareCompletedEventHandler);

            if (mNxlFileFingerPrint.isFromMyVault)
            {
                if (mNxlFileFingerPrint.isOwner)
                {
                    // From My Vault
                    Status = Status | ShareStatus.IS_FROM_MYVAULT;
                }
                else
                {
                    // From Share With Me
                    Status = Status | ShareStatus.IS_FROM_SHAREWITHME;
                }
            }
            else if (mNxlFileFingerPrint.isFromPorject)
            {
                // From Share With Me
                Status = Status | ShareStatus.IS_FROM_PROJECT;
            }

        }

        private string TransformValidity(Expiration expiration)
        {
            IExpiry Expiry;
            string expireDateValue = string.Empty;
            Helper.SdkExpiration2ValiditySpecifyModel(expiration, out Expiry, out expireDateValue, false);
            return expireDateValue;
        }

        public void AddRightsItem(IList<string> rights, ref ObservableCollection<RightsItem> rightsItems, bool isAddValidity = true)
        {
            rightsItems.Add(new RightsItem(@"/resources/icons/icon_rights_view.png", CultureStringInfo.SelectRights_View));
            if (rights != null && rights.Count != 0)
            {
                //In order to keep the rihts display order use the method below traversal list manually instead of 
                //using foreach loop.
                if (rights.Contains("Edit"))
                {
                    rightsItems.Add(new RightsItem(@"/resources/icons/icon_rights_edit.png", CultureStringInfo.SelectRights_Edit));
                }
                if (rights.Contains("Print"))
                {
                    rightsItems.Add(new RightsItem(@"/resources/icons/icon_rights_print.png", CultureStringInfo.SelectRights_Print));
                }
                if (rights.Contains("Share"))
                {
                    rightsItems.Add(new RightsItem(@"/resources/icons/icon_rights_share.png", CultureStringInfo.SelectRights_Share));
                }
                if (rights.Contains("SaveAs"))
                {
                    rightsItems.Add(new RightsItem(@"/resources/icons/icon_rights_save_as.png", CultureStringInfo.SelectRights_SaveAs));
                }
                // Fix bug 54210
                if (rights.Contains("Decrypt"))
                {
                    rightsItems.Add(new RightsItem(@"/resources/icons/icon_rights_extract.png", CultureStringInfo.SelectRights_Extract));
                }
                if (rights.Contains("Watermark"))
                {
                    rightsItems.Add(new RightsItem(@"/resources/icons/icon_rights_watermark.png", CultureStringInfo.SelectRights_Watermark));
                }
            }
            if (isAddValidity)
            {
                rightsItems.Add(new RightsItem(@"/resources/icons/icon_rights_validity.png", CultureStringInfo.SelectRights_Validity));
            }
        }

        public void EmailInputTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            System.Windows.Controls.TextBox textBox = sender as System.Windows.Controls.TextBox;
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

        public void EmailInput_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                System.Windows.Controls.TextBox textBox = sender as System.Windows.Controls.TextBox;
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

        public void On_GetOutlookEmail_Btn(object sender, MouseButtonEventArgs e)
        {
            mViewerInstance.Log.Info("----IsClosingOutlookAddressBookWin---->" + mParentWindow.IsClosingOutlookAddressBookWin);
            if (mParentWindow.IsClosingOutlookAddressBookWin)
            {
                return;
            }

            Outlook outlook = new Outlook(mParentWindow);
            outlook.UpdateEmailList += (ss, ee) =>
            {
                mViewerInstance.Dispatcher.Invoke(() =>
                {
                    OutlookAddEmail(ee.Monitor, ee.Email);
                });
            };
            outlook.SelectNameDialog();
        }

        public void DeleteEmailItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
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

        private bool CheckExpired(NxlFileFingerPrint fp)
        {
            bool result = false;
            if (fp.expiration.type != SkydrmLocal.rmc.sdk.ExpiryType.NEVER_EXPIRE
                                    &&
            CommonUtils.DateTimeToTimestamp(DateTime.Now) > fp.expiration.End)
            {
                mViewerInstance.Log.Info("\t\t File is expire \r\n");
                result = true;
            }
            return result;
        }

        public void Button_Ok(object sender, RoutedEventArgs e)
        {
            if (mParentWindow.IsClosingOutlookAddressBookWin)
            {
                return;
            }

            if (!NetworkStatus.IsAvailable)
            {
                Status = Status | ShareStatus.ERROR;
                Message = CultureStringInfo.ShareFileWin_Notify_File_Share_Failed_No_network;
                return;
            }

            ShowProgressBar();

            if (mNxlFileFingerPrint.isFromMyVault)
            {
                if (mNxlFileFingerPrint.isOwner)
                {
                    // From My Vault
                    if (!mUpdateRecipients.IsBusy)
                    {
                        mUpdateRecipients.Share(mNxlFileFingerPrint, mSharedEmailLists, Comments);
                    }
                }
                else
                {
                    // From Share With Me
                    if (!mReShare.IsBusy)
                    {
                        mReShare.Share(mNxlFileFingerPrint, mSharedEmailLists, "");
                    }
                }
            }
            else if (mNxlFileFingerPrint.isFromPorject || mNxlFileFingerPrint.isFromSystemBucket)
            {
                // From Share With Me
                if (!mShareAdhocFileInProject.IsBusy)
                {
                    mShareAdhocFileInProject.Share(mNxlFileFingerPrint, mSharedEmailLists, Comments);
                }
            }
        }

        private void SendLog(bool isAllow)
        {
            try
            {
                mLog.InfoFormat("\t\t SendLog isAllow:{0}", isAllow);
                mViewerInstance.User.AddLog(mNxlFileFingerPrint.localPath, NxlOpLog.Share, isAllow);
            }
            catch (Exception ex)
            {
                mLog.Error(ex);
            }
        }

        public void ReShareCompletedEventHandler(object sender, RunWorkerCompletedEventArgs args)
        {
            HideProgressBar();
            if (args.Result != null)
            {
                // means some error happended
                Exception e = (Exception)args.Result;
                Status = Status | ShareStatus.ERROR;
                ErrorHandler(e);
            }
            else
            {
                Status = Status | ShareStatus.RESHARE_SUCCEEDED;
                Message = CultureStringInfo.Successfully_Shared;
            }
        }

        public void UpdateRecipientsCompletedEventHandler(object sender, RunWorkerCompletedEventArgs args)
        {
            HideProgressBar();
            if (args.Result != null)
            {
                // means some error happended
                Exception e = (Exception)args.Result;
                Status = Status | ShareStatus.ERROR;
                ErrorHandler(e);
                SendLog(false);
            }
            else
            {
                Status = Status | ShareStatus.SHARE_MY_VAULT_FILE_SUCCEEDED;
                Message = CultureStringInfo.Successfully_Shared;
                SendLog(true);
            }
        }

        private void ErrorHandler(Exception ex)
        {
            RmRestApiException rmRestApiException = ex as RmRestApiException;
            if (null != rmRestApiException)
            {
                if (rmRestApiException.ErrorCode == 4001) // File has been revoked.
                {
                    Message = CultureStringInfo.ShareFileWin_Notify_File_Share_Failed_Because_revoked;
                    MessageNotify.NotifyMsg(mNxlFileFingerPrint.name, CultureStringInfo.ShareFileWin_Notify_File_Share_Failed_Because_revoked, EnumMsgNotifyType.PopupBubble, MsgNotifyOperation.SHARE, EnumMsgNotifyResult.Failed, EnumMsgNotifyIcon.Online);
                }
                else
                {
                  //  CommonUtils.ShowBalloonTip(ex.Message, false, mNxlFileFingerPrint.name);
                    Message = ex.Message;
                    MessageNotify.NotifyMsg(mNxlFileFingerPrint.name, CultureStringInfo.Unsuccessful_Shared, EnumMsgNotifyType.PopupBubble, MsgNotifyOperation.SHARE, EnumMsgNotifyResult.Failed, EnumMsgNotifyIcon.Online);
                }
            }
            else
            {
               //  CommonUtils.ShowBalloonTip(ex.Message, false, mNxlFileFingerPrint.name);
                //if (string.Equals(ex.Message, CultureStringInfo.Unknown_File,StringComparison.InvariantCultureIgnoreCase))
                //{
                //    Message = CultureStringInfo.Unknown_File;
                //    return;
                //}
                Message = ex.Message;
                MessageNotify.NotifyMsg(mNxlFileFingerPrint.name, CultureStringInfo.Unsuccessful_Shared, EnumMsgNotifyType.PopupBubble, MsgNotifyOperation.SHARE, EnumMsgNotifyResult.Failed, EnumMsgNotifyIcon.Online);
            }
        }

        public void ShareProjectFileCompletedEventHandler(object sender, RunWorkerCompletedEventArgs args)
        {
            HideProgressBar();
            if (args.Result != null)
            {
                ShareResult shareResult = args.Result as ShareResult;
                if (shareResult.Code == 0)
                {
                    FileName = shareResult.Result.ToString();
                    Status = Status | ShareStatus.SHARE_PROJECT_FILE_SUCCEEDED;
                    Message = CultureStringInfo.Successfully_Shared;
                    SendLog(true);
                }
                else
                {
                    // means some error happended
                    Status = Status | ShareStatus.ERROR;
                    ErrorHandler(shareResult.Exception);
                    SendLog(false);
                }
            }
        }

        public void Button_Cancel(object sender, RoutedEventArgs e)
        {
            if (mParentWindow != null)
            {
                if (mParentWindow.IsClosingOutlookAddressBookWin)
                {
                    return;
                }
                mParentWindow.Close();
            }
        }

        public void CommentTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            string sourceText = (e.Source as System.Windows.Controls.TextBox).Text;

            if ((maxLen - sourceText.Length) < 0)
            {
                DisableShareButton();
            }
            else
            {
                ShareBtnEnableByEmailList();
            }
        }

        private void DeleteEmailItem(string target)
        {
            //Traversal emailItems
            EmailItem deleteItem = null;
            foreach (var item in Emails)
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
                Emails.Remove(deleteItem);
                if (deleteItem.EmailStatus.Equals(EmailStatus.NORMAL))
                {
                    if (mSharedEmailLists.Contains(deleteItem.Emails))
                    {
                        mSharedEmailLists.Remove(deleteItem.Emails);
                    }
                }
                else
                {
                    if (mDirtyEmailLists.Contains(deleteItem.Emails))
                    {
                        mDirtyEmailLists.Remove(deleteItem.Emails);
                    }
                }
                ShareBtnEnableByEmailList();
            }
        }

        private void DisableShareButton()
        {
            if ((Status & ShareStatus.SHARE_BUTTON_ENABLED) == ShareStatus.SHARE_BUTTON_ENABLED)
            {
                Status = Status ^ ShareStatus.SHARE_BUTTON_ENABLED;
            }
        }

        private void EnableShareButton()
        {
            if ((Status & ShareStatus.SHARE_BUTTON_ENABLED) != ShareStatus.SHARE_BUTTON_ENABLED)
            {
                Status = Status | ShareStatus.SHARE_BUTTON_ENABLED;
            }
        }

        private void OutlookAddEmail(bool shareBtnDisEnable, string addEmail)
        {
            if (!string.IsNullOrEmpty(addEmail))
            {
                bool emailExist = IsEmailExisted(addEmail, mSharedEmailLists);
                if (!emailExist)
                {
                    WrapEmail(addEmail);
                }
            }

            if (shareBtnDisEnable)
            {
                DisableShareButton();
            }
            else
            {
                //bindConfigs.ShareButtonEnabled = true;

                //Fix bug 55202
                ShareBtnEnableByEmailList();
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

        private void ShareBtnEnableByEmailList()
        {
            if (mSharedEmailLists.Count != 0 && mDirtyEmailLists.Count == 0)
            {
                EnableShareButton();
            }
            else
            {
                DisableShareButton();
            }
        }

        /*
        *Wrap email with different style.
        *Distinguish the email from sharedEmailLists to dirtyEmailLists
        */
        private void WrapEmail(string email)
        {
            string trimedText = email.Trim();
            bool isemailExist = IsEmailExisted(trimedText, mSharedEmailLists);
            if (isemailExist)
            {
                CommonUtils.ShowBalloonTip(trimedText + "\n" + CultureStringInfo.Share_Email_Details, true);
                return;
            }
            if (CheckEmail(trimedText) && !isemailExist)
            {
                Emails.Add(new EmailItem(trimedText, EmailStatus.NORMAL));
                mSharedEmailLists.Add(trimedText);
            }
            else
            {
                Emails.Add(new EmailItem(trimedText, EmailStatus.DIRTY));
                mDirtyEmailLists.Add(trimedText);
            }
        }

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

        private bool IsEmailExisted(String email, IList<String> emailList)
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

    }
}
