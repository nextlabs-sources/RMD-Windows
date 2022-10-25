using SkydrmLocal.rmc.ui.windows.chooseServerWindow.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using SkydrmLocal.rmc.helper;
using static SkydrmLocal.rmc.helper.NetworkStatus;

namespace SkydrmLocal.rmc.ui.windows
{
    /// <summary>
    /// Interaction logic for ChooseServerWindow.xaml
    /// </summary>
    public partial class ChooseServerWindow : Window
    {
        private SkydrmLocalApp app = (SkydrmLocalApp)SkydrmLocalApp.Current;

        private ChooseServerModel ChooseServerModel;

        private bool isPersonal = true;

        private bool isNetworkAvailable;

        private string Message = null;
        //for modify ui
        private BackgroundWorker backgroundWorker = new BackgroundWorker();

        public ChooseServerWindow()
        {
            InitializeComponent();

            ChooseServerModel = new ChooseServerModel();
            this.DataContext = ChooseServerModel;

            // Set defult radio checked
            this.RadioCompany.IsChecked = true;

            this.Loaded += delegate
            {
                // register trayIcon click popup window.
                ((SkydrmLocalApp)SkydrmLocalApp.Current).TrayIconMger.PopupTargetWin = this;

                this.Topmost = false;
            };
            //this.Activated += new EventHandler(delegate (object sender, EventArgs e)
            //{
            //    this.Topmost = false;
            //    this.Focus();
            //});

            // Regsiter network status event listener
            AvailabilityChanged += new NetworkStatusChangedHandler(OnNetworkStatusChanged);

            // init network status
            isNetworkAvailable = NetworkStatus.IsAvailable;

            // init convert background worker
            backgroundWorker.WorkerReportsProgress = true;
            backgroundWorker.WorkerSupportsCancellation = true;

            // register
            backgroundWorker.DoWork += backgroundDoWorker;
            backgroundWorker.RunWorkerCompleted += backgroundWorkerCompleted;
        }
        private void OnNetworkStatusChanged(object sender, NetworkStatusChangedArgs e)
        {
            isNetworkAvailable = e.IsAvailable;
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
           //judge whether isPersonal from RadioButton isChecked 
            if (this.RadioCompany.IsChecked == true)
            {
                isPersonal = false;
                ChooseServerModel.URL = this.mycombox.Text.Trim();                                 
            }
            else
            {
                isPersonal = true;
                ChooseServerModel.URL = app.Config.Router.Trim();
            }

            //First check: judge Url whether is null or empty
            if (string.IsNullOrEmpty(ChooseServerModel.URL))
            {
                app.ShowBalloonTip(CultureStringInfo.CheckUrl_Notify_UrlEmpty);
                return;
            }
            //add http head if url not contain
            if (!ChooseServerModel.URL.StartsWith("http",StringComparison.CurrentCultureIgnoreCase))
            {
                ChooseServerModel.URL = "https://" + ChooseServerModel.URL;
            }
            //Second check:
            if ( !CheckUrl(ChooseServerModel.URL) )
            {
                app.ShowBalloonTip(CultureStringInfo.CheckUrl_Notify_UrlError);
                return;
            }
            //Third check: judge Network
            if (isNetworkAvailable)
            {
                //if (this.RadioCompany.IsChecked == true)
                //{
                //    if (this.CheckRememberURL.IsChecked == true)
                //    {
                //        //do insert url in .db file
                //        ChooseServerModel.InsertUrl();
                //    }
                //}
                //app.Mediator.OnShowLoginWin(this, isPersonal, ChooseServerModel.URL.Trim());

                //if network connected,start backgroundWorker and check URL from http request.
                this.GridProBar.Visibility = Visibility.Visible;
                if (!backgroundWorker.IsBusy)
                {
                    backgroundWorker.RunWorkerAsync();
                }
               
            }
            else
            {
                app.ShowBalloonTip(CultureStringInfo.CheckUrl_Notify_NetDisconnect);
            }
                   
        }
        private void backgroundDoWorker(object sender, DoWorkEventArgs args)
        {
            bool Urlvalid = true;
            Urlvalid = InvokeSdkSessionInitialize(ChooseServerModel.URL);
            args.Result = Urlvalid;          
        }
        private void backgroundWorkerCompleted(object sender, RunWorkerCompletedEventArgs args)
        {
            bool check= (bool)args.Result;

            //modify ui status
            this.GridProBar.Visibility = Visibility.Collapsed;

            if (check)
            {
                bool IsRember = false;

                if (this.RadioCompany.IsChecked == true)
                {
                    if (this.CheckRememberURL.IsChecked == true)
                    {
                        //do insert url in .db file
                        // ChooseServerModel.InsertUrl();
                        IsRember = true;
                    }
                }
                app.Mediator.OnShowLoginWin(this, isPersonal, ChooseServerModel.URL, IsRember);
            }
            else
            {
                string msg = CultureStringInfo.CheckUrl_Notify_NetworkOrUrlError;
                app.ShowBalloonTip(msg);
                app.Log.Error(msg+Message);
            }
        }
        private bool CheckUrl(string url)
        {
            bool checkUrl = false;

            //string regExp = "(ht|f)tp(s?)\\:\\/\\/[0-9a-zA-Z]([-.\\w]*[0-9a-zA-Z])*(:(0-9)*)*(\\/?)([a-zA-Z0-9\\-\\.\\?\\,\\'\\/\\\\\\+&amp;%\\$#_]*)?";

            //string regExp = @"^((ht|f)tp(s?)://)"
            //                              + @"?(([0-9a-z_!~*'().&=+$%-]+: )?[0-9a-z_!~*'().&=+$%-]+@)?" //ftp user@ 
            //                              + @"(([0-9]{1,3}\.){3}[0-9]{1,3}" //URL- 221.2.162.15
            //                              + @"|" 
            //                              + @"([0-9a-z_!~*'()-]+\.)*" // - www. 
            //                              + @"([0-9a-z][0-9a-z-]{0,61})?[0-9a-z]\." 
            //                              + @"[a-z]{2,9})" // first level domain- .com or .museum 
            //                              + @"(:[0-9]{1,4})?" // port- :80 
            //                              + @"((/?)|" // a slash isn't required if there is no file name 
            //                              + @"(/[0-9a-z_!~*'().;?:@&=+$,%#-]+)+/?)$";
            // if (Regex.IsMatch(url, regExp,RegexOptions.IgnoreCase))
            //{
            //    checkUrl = true;
            //}
            Uri uriResult;
            bool result = Uri.TryCreate(url, UriKind.Absolute, out uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

            if (result)
            {
                checkUrl = true;
            }
            return checkUrl;
        }

        //Invoke SDK Session.Initialize
        private bool InvokeSdkSessionInitialize(string strUrl)
        {
            try
            {
                app.Rmsdk.Initialize(app.Config.RmSdkFolder, strUrl, "");
                return true;
                //HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(strUrl);
                //myRequest.Method = "HEAD";
                //myRequest.Timeout = 10000;  //超时时间10秒
                //HttpWebResponse res = (HttpWebResponse)myRequest.GetResponse();
                //Message = ((int)res.StatusCode).ToString();
                //return (res.StatusCode == HttpStatusCode.OK);
            }
            catch(Exception we)
            {
                Message = we.Message;
                return false;
            }
        }

        private void RadioBtn(object sender, RoutedEventArgs e)
        {
            RadioButton radioButton = sender as RadioButton;
            if (radioButton != null && radioButton.Content != null)
            {
                switch (radioButton.Name.ToString())
                {
                    case "RadioPerson":
                        ChooseServerModel.ServerModel = ServerModel.Personal;
                        //this.mycombox.Text = "";                 //Because  "RadioCompany" modify mycombox.Text, the text must set "".
                        //ChooseServerModel.URL = app.Config.Router.Trim();
                        break;
                    case "RadioCompany":
                        ChooseServerModel.ServerModel = ServerModel.Company;
                        //this.mycombox.Text = app.Config.CompanyRouter.Trim();     //if modify mycombox.Text in background code, it will not trigger ComboBox.Style DataTrigger in UI. 
                        //ChooseServerModel.URL = app.Config.CompanyRouter.Trim();
                        break;
                }
                    
            }
        }

        private void Combox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Console.WriteLine("--text changed!!!----" + this.mycombox.Text);
            string sourceText = (e.OriginalSource as TextBox).Text;
            //fix bug 49936
            var targetComboBox = sender as ComboBox;
            var targetTextBox = targetComboBox?.Template.FindName("PART_EditableTextBox", targetComboBox) as TextBox;

            bool isDropDown;
            if (this.RadioCompany.IsChecked == true)
            {
                this.ChooseServerModel.Serach(sourceText, out isDropDown);

                //Records the position of the currently selected cursor
                int careIndex = targetTextBox.CaretIndex;
                //the text value is selected
                this.mycombox.IsDropDownOpen = isDropDown;
                //Set cursor position,and the text value is not selected 
                targetTextBox.CaretIndex = careIndex;
                
            }
            else
            {
                this.mycombox.IsDropDownOpen = false;
                //when combox text is "",it should  again add item from copylist to UrlList 
                this.ChooseServerModel.Serach(sourceText, out isDropDown);
            }
        }


    }
  

}
