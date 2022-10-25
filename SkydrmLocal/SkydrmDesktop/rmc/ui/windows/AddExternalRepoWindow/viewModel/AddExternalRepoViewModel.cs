using CustomControls;
using Newtonsoft.Json.Linq;
using SkydrmDesktop.rmc.ui.windows.AddExternalRepoWindow.addRepo;
using SkydrmDesktop.rmc.ui.windows.AddExternalRepoWindow.view;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.ui.utils;
using SkydrmLocal.rmc.ui.windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace SkydrmDesktop.rmc.ui.windows.AddExternalRepoWindow.viewModel
{
    class AddExternalRepoViewModel: BaseViewModel
    {
        private readonly SkydrmApp App = SkydrmApp.Singleton;
        private readonly AddRepositoryPage addRepositoryPage = new AddRepositoryPage();
        private readonly WebBrowser webBrowser = new WebBrowser();

        private readonly BackgroundWorker DoGetAuthorUrl_BgWorker = new BackgroundWorker();

        private featureProvider.externalDrive.RmsRepoMgr.ExternalRepoType SelectedRepoType { get; set; }
        private string SelectedRepoName { get; set; }
        private string AuthorizationURL { get; set; }

        private IAddExternalRepo AddRepoOpert { get; }

        public AddExternalRepoViewModel(IAddExternalRepo addExternal, AddExternalRepoWin win) : base(win)
        {
            AddRepoOpert = addExternal;

            InitAddExtRepoPageViewModel();
            InitAddExtRepoPageCommand();

            Host.frm.Content = addRepositoryPage;

            DoGetAuthorUrl_BgWorker.DoWork += DoGetAuthorUrl_BgWorker_DoWork;
            DoGetAuthorUrl_BgWorker.RunWorkerCompleted += DoGetAuthorUrl_BgWorker_RunWorkerCompleted;
        }

        private void InitAddExtRepoPageViewModel()
        {
            // should invoke AddRepoOpert.ListRepo()
            // and convert data to CustomControl data, this convert should move to DataTypeConvertHelper.cs
            addRepositoryPage.ViewMode.ExternalRepoList = DataTypeConvertHelper.SdkExtnRepo2CustCtrExternalRepo();

        }

        private void InitAddExtRepoPageCommand()
        {
            CommandBinding binding;
            binding = new CommandBinding(AddRepo_DataCommands.Connect);
            binding.Executed += ConnectCommand; ;
            Host.CommandBindings.Add(binding);
            binding = new CommandBinding(AddRepo_DataCommands.Positive);
            binding.Executed += PositiveCommand;
            Host.CommandBindings.Add(binding);
            binding = new CommandBinding(AddRepo_DataCommands.Cancel);
            binding.Executed += CancelCommand;
            Host.CommandBindings.Add(binding);
        }
        private void ConnectCommand(object sender, ExecutedRoutedEventArgs e)
        {
            GetSelectedRepoItem();
            if (SelectedRepoType == featureProvider.externalDrive.RmsRepoMgr.ExternalRepoType.SHAREPOINT_ONPREMISE)
            {
                ShowInputPwdWin();
            }
            else
            {
                StartGetAuthorUrl_BgWorker();
            }
            
        }

        private void PositiveCommand(object sender, ExecutedRoutedEventArgs e)
        {
            GetSelectedRepoItem();
            if (SelectedRepoType == featureProvider.externalDrive.RmsRepoMgr.ExternalRepoType.SHAREPOINT_ONPREMISE)
            {
                ShowInputPwdWin();
            }
            else
            {
                StartGetAuthorUrl_BgWorker();
            }
        }
        private void CancelCommand(object sender, ExecutedRoutedEventArgs e)
        {
            Host.Close();
        }

        private void GetSelectedRepoItem()
        {
            ExternalRepoItem selectedItem = null;
            foreach (var item in addRepositoryPage.ViewMode.ExternalRepoList)
            {
                if (item.IsSelected)
                {
                    selectedItem = item;
                    SelectedRepoName = item.Name;
                }
            }
            SelectedRepoType = DataTypeConvertHelper.CustCtrSelectedRepoItem2ExtnRepoType(selectedItem);
        }

        private void ShowInputPwdWin()
        {
            IInputPwdAuth inputPwdAuth = new InputPwdAuth(addRepositoryPage.ViewMode.SiteURL);
            InputNameAndPasswordWin inputNamePwdWin = new InputNameAndPasswordWin(inputPwdAuth);
            inputNamePwdWin.Owner = Host;
            inputNamePwdWin.ShowDialog();

            if (inputPwdAuth.AuthResult)
            {
                Host.Close();
            }
        }

        private void StartGetAuthorUrl_BgWorker()
        {
            if (!DoGetAuthorUrl_BgWorker.IsBusy)
            {
                DoGetAuthorUrl_BgWorker.RunWorkerAsync();

                // display progrss bar UI
                GridProBarVisible = System.Windows.Visibility.Visible;
            }
        }

        private void DoGetAuthorUrl_BgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            string displayName = addRepositoryPage.ViewMode.DisplayName;

            string siteUrl = string.Empty;
            if (SelectedRepoType == featureProvider.externalDrive.RmsRepoMgr.ExternalRepoType.SHAREPOINT_ONLINE)
            {
                siteUrl = addRepositoryPage.ViewMode.SiteURL;
            }

            string uri = AddRepoOpert.GetAuthUri(displayName, SelectedRepoType, siteUrl);
            if (string.IsNullOrEmpty(uri))
            {
                e.Result = false;
                return;
            }
            string rmsUrl = App.Config.UserUrl.Substring(0, App.Config.UserUrl.ToLower().IndexOf("/rms/login"));
            string authPath = rmsUrl + uri;

            Uri uriResult;
            bool result = Uri.TryCreate(authPath, UriKind.Absolute, out uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            if (!result)
            {
                // should notify user
                e.Result = false;
                return;
            }

            string[] param = new string[] {
                "userId", App.User.RmsUserId.ToString(),
                "ticket", App.Config.UserTicket
            };
            StringBuilder builder = new StringBuilder();
            builder.Append(authPath);
            for (int i = 0; i < param.Length;)
            {
                string key = param[i];
                string value = param[i+1];
                if (!string.IsNullOrEmpty(value))
                {
                    builder.Append("&");
                    builder.Append(key);
                    builder.Append("=");
                    builder.Append(value);
                }
                i += 2;
            }

            AuthorizationURL = builder.ToString();
            e.Result = true;
           
        }
       
        private void DoGetAuthorUrl_BgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            bool result = (bool)e.Result;
            GridProBarVisible = System.Windows.Visibility.Collapsed;

            if (!result)
            {
                return;
            }

            bool isRedirect = ShowRedirectDlg(SelectedRepoName);
            if (isRedirect)
            {
                System.Diagnostics.Process.Start(AuthorizationURL);
                Host.Close();

                //Host.frm.Content = webBrowser;
                //webBrowser.Source = new Uri(AuthorizationURL);
                //webBrowser.LoadCompleted += WebBrowser_LoadCompleted;

                //HandleAuthorization(AuthorizationURL);
            }
        }
        public static bool ShowRedirectDlg(string name)
        {
            string subject = "You will be redirected to the " + name + " website now.Please log into your " + name + " account and authorize SkyDRM to access all your files in " + name + ".";

            CustomMessageBoxWindow.CustomMessageBoxResult ret = CustomMessageBoxWindow.Show(
                "",
                subject,
                "",
                CustomMessageBoxWindow.CustomMessageBoxIcon.None,
                CustomMessageBoxWindow.CustomMessageBoxButton.BTN_YES,
                CustomMessageBoxWindow.CustomMessageBoxButton.BTN_NO
            );

            return (ret == CustomMessageBoxWindow.CustomMessageBoxResult.Positive) ? true : false;
        }
        private void WebBrowser_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            var doc = webBrowser.Document;
            // prepare a method let javascript to call back
            webBrowser.ObjectForScripting = new ObjectForScriptingHelper(this);
        }

        private async void HandleAuthorization(string url)
        {
            string redirectURI = string.Format("http://{0}:{1}/", IPAddress.Loopback, GetRandomUnusedPort());

            // Creates an HttpListener to listen for requests on that redirect URI.
            var http = new HttpListener();
            http.Prefixes.Add(redirectURI);
            http.Start();

            string authorizationRequest = string.Format(@"https://rms-centos7308.qapf1.qalab01.nextlabs.com:8444/rms/json/OAuthManager/GDAuth/GDAuthStart?name=123{0}&userId=62&ticket=188F248AA42BC8D336BAA377580DFE12",
                System.Uri.EscapeDataString(redirectURI));

            // Opens request in the browser.
            System.Diagnostics.Process.Start(authorizationRequest);

            // Waits for the OAuth authorization response.
            var context = await http.GetContextAsync();

            // Sends an HTTP response to the browser.
            var response = context.Response;
            //string responseString = string.Format("<html><head><meta http-equiv='refresh' content='10;url=https://google.com'></head><body>Please return to the app.</body></html>");
            //var buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            //response.ContentLength64 = buffer.Length;
            //var responseOutput = response.OutputStream;
            //Task responseTask = responseOutput.WriteAsync(buffer, 0, buffer.Length).ContinueWith((task) =>
            //{
            //    responseOutput.Close();
            //    http.Stop();
            //    Console.WriteLine("HTTP server stopped.");
            //});
        }

        // ref http://stackoverflow.com/a/3978040
        public static int GetRandomUnusedPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }


        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        [ComVisible(true)]
        public class ObjectForScriptingHelper
        {
            AddExternalRepoViewModel  addExternalRepoViewModel;
            public ObjectForScriptingHelper(AddExternalRepoViewModel viewModel)
            {
                this.addExternalRepoViewModel = viewModel;
            }
            public void InvokeMeFromJavascript(string jsscript)
            {
                /*
                        jsscript	
                        {
                            "statusCode":200,
                            "message":"OK",
                            "serverTime":1474591551519,
                            "results": {
                                "repository" : {
                                     "repoId":"e8eb1c55-c4e8-45e6-a273-5ec140c3cbd2",
                                     "name":"DBDocs",
                                     "type":"DROPBOX",
                                     "isShared":false,
                                     "accountName":"xxxxxx@nextlabs.com",
                                     "accountId":"2xxxxxxxxxxxxxxxxxxxxxx",
                                     "creationTime":1470122537982
                                         }
                                     }
                        }
                */
                // try to parse the result


                if (!StringHelper.IsValidJsonStr_Fast(jsscript))
                {
                    return;
                }
                
                try
                {
                    JObject jo = JObject.Parse(jsscript);
                    int statuscode = (int)jo["statusCode"];
                    string message = (string)jo["message"];
                    if (statuscode != 200)
                    {
                        return;
                    }
                    if (!"OK".ToLower().Equals(message.ToLower()))
                    {
                        return;
                    }

                }
                catch (Exception e)
                {
                    addExternalRepoViewModel.App.Log.Warn("error occrued in injected js that will add repo," + e.Message, e);
                    return;
                }
            }
        }
    }
}
