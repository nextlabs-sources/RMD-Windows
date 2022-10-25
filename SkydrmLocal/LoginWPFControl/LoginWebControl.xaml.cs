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
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using Newtonsoft.Json.Linq;

namespace LoginWPFControl
{

    public partial class LoginWebControl : UserControl
    {
        private string weburl;

        public LoginWebControl()
        {
            weburl = "about: blank";
            InitializeComponent();

            DisplayLoading();

            LoginWeb.Navigating += delegate
            {
                SuppressScriptErrors(true);
            };
          
        }        

        public void Show(string url="")
        {
            if (url != "")
            {
                weburl = url;
            }
            else
            {
                weburl = "about: blank";
            }

            LoadLoginPage();
        }
                      

        public void LoadLoginPage()
        {
            ClearSystemHistoryCookies();
            try
            {
                this.LoginWeb.Source = new Uri(weburl);
            }
            catch (Exception e)
            {
                MessageBox.Show("Please input a valid SkyDRM Server URL.", "SkyDRM DESKTOP");
            }

            // we got a requried to hide the main page of web rms
            //LoginWeb.Navigating += (ss, ee) =>
            //{

            //    if (ee.Uri.AbsolutePath.ToLower().Contains("/rms/main"))
            //    {
            //        try
            //        {
            //            ToDefaultWebPage();
            //            DisplayLoading(true);

            //        }
            //        catch (Exception e)
            //        {
            //            var a = e.Message;
            //        }
            //    }

            //};

            // we have to inject js code to set the listener for user login message
            LoginWeb.LoadCompleted += (ss, ee) =>
            {

                if (!ee.Uri.AbsolutePath.ToLower().Contains("login") && !ee.Uri.AbsolutePath.ToLower().Contains("main"))
                {
                    // hide load progress
                    return;
                }

                DisplayLoading(false);

                // for
                var doc = LoginWeb.Document;
                                

                // prepare a method let javascript to call back
                LoginWeb.ObjectForScripting = new ObjectForScriptingHelper(this, weburl);

                try
                {
                    // write ajac intercept code     
                    string InjectedCode =
                        @"
                        {
                            $(document).ajaxSuccess(function( event, xhr, settings ) {
                                window.external.InvokeMeFromJavascript(xhr.responseText);
                            });
                        }
                    ";

                    LoginWeb.InvokeScript("eval", new object[] { InjectedCode });

                    // special for domain account login
                    InjectedCode =
                    @"
                    (function (open) { 
                            XMLHttpRequest.prototype.open = function () { 
                                this.addEventListener('readystatechange', function () { 
                                    window.external.InvokeMeFromJavascript(this.responseText); 
                                }, false); 
                                open.apply(this, arguments); 
                            }; 
                        } 
                    ) 
                    (XMLHttpRequest.prototype.open);
                ";
                    LoginWeb.InvokeScript("eval", new object[] { InjectedCode });
                }
                catch (Exception e)
                {
                    //app.Log.Error(e);
                }


            };

        }

        public void ToDefaultWebPage()
        {
            // we intercept XMLHTTPRequest
            this.LoginWeb.Navigate("about:blank");
        }


        private void DisplayLoading(bool show=true)
        {
            if (show)
            {
                this.LoadingBar.Visibility = Visibility.Visible;
                this.LoginWeb.Visibility = Visibility.Collapsed;
            }
            else
            {
                this.LoadingBar.Visibility = Visibility.Collapsed;
                this.LoginWeb.Visibility = Visibility.Visible;
            }
        }
      

        private void SuppressScriptErrors(bool isHide)
        {
            FieldInfo fiComWebBrowser = typeof(WebBrowser).GetField("_axIWebBrowser2", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fiComWebBrowser == null) return;

            object objComWebBrowser = fiComWebBrowser.GetValue(LoginWeb);
            if (objComWebBrowser == null) return;

            objComWebBrowser.GetType().InvokeMember("Silent", BindingFlags.SetProperty, null, objComWebBrowser, new object[] { isHide });
        }

        private void ClearSystemHistoryCookies()
        {
            //
            // clear all the cookies at this page
            //
            /*
             +// #define CLEAR_COOKIES         0x0002 // Clears cookies
             // #define CLEAR_SHOW_NO_GUI     0x0100 // Do not show a GUI when running the cache clearing
            */
            RunCmd("RunDll32.exe", "InetCpl.cpl,ClearMyTracksByProcess 258");
        }

        // helper to run some external commands
        private void RunCmd(string cmd, string param)
        {
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.FileName = cmd;
            p.StartInfo.Arguments = param;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            p.Start();
            p.WaitForExit();
        }
                     
        public void OnLogin_Ok(string loginString)
        {
            ToDefaultWebPage();
            MessageBox.Show(loginString);
        }


        // rmc-sdk required set cookies
        [DllImport("wininet.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool InternetSetCookie(string lpszUrlName, string lpszCookieName, string lpszCookieData);



    }

    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    [ComVisible(true)]
    public class ObjectForScriptingHelper
    {
        bool isLogined = false;
        string webUrl;

        LoginWebControl mExternalWPF;
        public ObjectForScriptingHelper(LoginWebControl w, string webUrl)
        {
            this.mExternalWPF = w;
            this.webUrl = webUrl;
        }
        public void InvokeMeFromJavascript(string jsscript)
        {
            /*
             		jsscript	"{\"statusCode\":200,\"message\":\"Authorized\",\"serverTime\":1526563486530,\"extra\":{\"userId\":25,\"ticket\":\"1A609738A8D9BF01B5CD5609209FB7FF\",\"tenantId\":\"21b06c79-baab-419d-8197-bad3ce3f4476\",\"lt\":\"skydrm.com\",\"ltId\":\"21b06c79-baab-419d-8197-bad3ce3f4476\",\"ttl\":1526649886528,\"name\":\"osmond.ye\",\"email\":\"osmond.ye@nextlabs.com\",\"preferences\":{\"homeTour\":true,\"profile_picture\":\"data:image/jpeg;base64,/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAFA3PEY8MlBGQUZaVVBfeMiCeG5uePWvuZHI////////\KKKKACiiigAooooAKKKKACiiigAoo\\nooAKKKKACpYPvGiigCaiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAZN/q6r0UUAFFFFA\\nBRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABTo/viiigCzRRRQAUUUUAf/Z\\n\"},\"idpType\":0,\"memberships\":[{\"id\":\"m39@skydrm.com\",\"type\":0,\"tenantId\":\"21b06c79-baab-419d-8197-bad3ce3f4476\",\"projectId\":1},{\"id\":\"m332@t-f3d5a9dad6c54454a896a98fcae10c5f\",\"type\":0,\"tenantId\":\"4ead5485-80b0-4e6d-bea0-a9b16119b674\",\"projectId\":163},{\"id\":\"m929@t-36d9566d8eed4495a3608f4ff80064a8\",\"type\":0,\"tenantId\":\"43360a40-a4b2-4061-97e1-c8912f115828\",\"projectId\":503},{\"id\":\"m1138@t-0fb33c1c7f3746f293e0228f94fbed23\",\"type\":0,\"tenantId\":\"2096293b-4f78-460b-9350-7628fe06cbcc\",\"projectId\":608},{\"id\":\"m915@t-cd7ec64c03a349ce940730248b828bba\",\"type\":0,\"tenantId\":\"c689dbf5-b7a1-4345-a630-2765b60de278\",\"projectId\":498},{\"id\":\"m691@t-066f8cd594164c569bf8322b58681ad0\",\"type\":0,\"tenantId\":\"0dde2d00-a3b7-453a-b705-427876045e36\",\"projectId\":367},{\"id\":\"m331@t-dc62646efbee49ceb4c184864d28816a\",\"type\":0,\"tenantId\":\"7929e62c-e4ac-4a81-8043-9c2c345bea8d\",\"projectId\":162},{\"id\":\"m333@t-b719fa508bae411d9f4470221819bd9a\",\"type\":0,\"tenantId\":\"2f69c9e1-d310-4ea1-9b1d-15420453b773\",\"projectId\":17}],\"defaultTenant\":\"skydrm.com\",\"defaultTenantUrl\":\"https://rmtest.nextlabs.solutions/rms\",\"attributes\":{\"displayName\":[\"osmond.ye\"],\"email\":[\"osmond.ye@nextlabs.com\"]}}}"	string

            */
            // try to parse the result
            if (isLogined)
            {
                return;
            }

            if (!IsValidJsonStr_Fast(jsscript))
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
                if (!"Authorized".ToLower().Equals(message.ToLower()))
                {
                    return;
                }

                // we consider user logined 
                isLogined = true;
                mExternalWPF.OnLogin_Ok(jsscript);
            }
            catch (Exception e)
            {
                //app.Log.Warn("error occrued in injected js that will take user login," + e.Message, e);
                return;
            }
        }

        public static bool IsValidJsonStr_Fast(string jsonStr)
        {
            if (jsonStr == null)
            {
                return false;
            }
            if (jsonStr.Length < 2)
            {
                return false;
            }

            if (!jsonStr.StartsWith("{"))
            {
                return false;
            }

            if (!jsonStr.EndsWith("}"))
            {
                return false;
            }

            return true;
        }
    }

}
