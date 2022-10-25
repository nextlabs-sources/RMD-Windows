using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Office.Interop.Outlook;
using SkydrmDesktop.Resources.languages;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.helper;

namespace SkydrmDesktop.rmc.app.process.outlook
{
    public class Outlook
    {
        private SkydrmApp app = SkydrmApp.Singleton;
        // Define Outlook Application Object
        private readonly Application outlookApp;

        private readonly NameSpace oNamespace;

        private IntPtr parentWinHandle;

        public event EventHandler<UpdateEmailEventArgs> UpdateEmailList;

        public Outlook()
        {
            this.parentWinHandle = Win32Common.GetForegroundWindow();

            //// First step, Check outlook is or not install
            app.Log.Info("Check outlook is or not install");

            Type officeType = Type.GetTypeFromProgID("Outlook.Application");
            if (officeType == null)
            {
                app.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Outlook_Install"), false);
                outlookApp = null;
                return;
            }

            try
            {
                //// Second step, Check whether there is an Outlook process running.
                app.Log.Info("Check whether there is an Outlook process running");

                try
                {
                    outlookApp = (Application)Marshal.GetActiveObject("Outlook.Application");
                    oNamespace = outlookApp?.GetNamespace("MAPI");
                }
                catch (System.Exception)
                {
                    // If not, create a new instance of Outlook and log on to the default profile.
                    outlookApp = new Application();
                    oNamespace = outlookApp?.GetNamespace("MAPI");

                    // If user don't log on Outlook, this method will trigger log in flow in Outlook
                    // If user have logined Outlook, this method will show window and need select profile by user.
                    //oNamespace.Logon("", "", true, true);
                }

                //// Third step, Check user is or not log in outlook
                app.Log.Info("Check user is or not login outlook");

                // oNamespace.Accounts will get accounts collection in current profile, don't need Namespace.Logon
                if (oNamespace.Accounts.Count <= 0)
                {
                    ReleaseComObject();
                    outlookApp = null;
                    oNamespace = null;
                    app.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Outlook_LogIn"), false);
                }
            }
            catch (System.Exception e)
            {
                app.Log.Error(e);
                ReleaseComObject();
                outlookApp = null;
                oNamespace = null;
                app.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Outlook_Open"), false);
            }
        }

        private AddressLists GetAddressLists()
        {
            return oNamespace?.AddressLists;
        }

        private AddressList GetGlobalAddressList()
        {
            return oNamespace?.GetGlobalAddressList();
        }

        private void BuidData(AddressEntry addEntry)
        {
            var addEntries = addEntry.Members;

            if (addEntries == null)
            {
                // Fixed bug 55206, the email that the user input is not in AddressBook. Email can't add emailList in SharePage
                if (addEntry.AddressEntryUserType == OlAddressEntryUserType.olSmtpAddressEntry)
                {
                    UpdateEmailList.Invoke(this, new UpdateEmailEventArgs(true, addEntry.Address));
                    return;
                }

                var exchangeUser = addEntry?.GetExchangeUser();
                if (exchangeUser == null)
                {
                    Console.WriteLine("*************exchangeUser is null*************");
                    app.Log.Info("exchangeUser is null");
                    return;
                }
                if (string.IsNullOrEmpty(exchangeUser.PrimarySmtpAddress))
                {
                    Console.WriteLine("*************exchangeUser.PrimarySmtpAddress IsNullOrEmpty*************");
                }

                var eventArgs = new UpdateEmailEventArgs(true, exchangeUser.PrimarySmtpAddress);
                UpdateEmailList.Invoke(this, eventArgs);
            }
            else
            {
                for (int i = 1; i < addEntries.Count + 1; i++)
                {
                    var nextAddEntry = addEntries[i];
                    BuidData(nextAddEntry);
                }
            }
        }

        private void ReleaseComObject()
        {
            if (oNamespace != null)
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oNamespace);
            }
            if (outlookApp != null)
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(outlookApp);
            }
        }

        public void SelectNameDialog()
        {
            if (outlookApp == null)
            {
                return;
            }

            try
            {
                // start monitor
                UpdateEmailList.Invoke(this, new UpdateEmailEventArgs(true, null));

                SelectNamesDialog selectNameDialog = outlookApp.Session.GetSelectNamesDialog();
                selectNameDialog.Caption = CultureStringInfo.ApplicationFindResource("Outlook_Dialog_Caption");
                selectNameDialog.NumberOfRecipientSelectors = OlRecipientSelectors.olShowTo;

                // "#32770" is SelectNamesDialog ClassName, obtain through Spy++
                BringWindowTop("#32770", CultureStringInfo.ApplicationFindResource("Outlook_Dialog_Caption"));

                if (selectNameDialog.Display())
                {
                    var recipients = selectNameDialog.Recipients;

                    new NoThrowTask(false, () =>
                    {
                        try
                        {
                            for (int i = 1; i < recipients.Count + 1; i++)
                            {
                                Recipient recipient = recipients[i];
                                Console.WriteLine("*********recipient.Address：******" + recipient.Address);
                                var addEntry = recipient.AddressEntry;

                                BuidData(addEntry);
                            }
                        }
                        catch (System.Exception e)
                        {
                            app.Log.Error(e);
                        }
                        finally
                        {
                            // end monitor
                            UpdateEmailList.Invoke(this, new UpdateEmailEventArgs(false, null));
                            ReleaseComObject();
                        }
                    }).Do();
                }
                else
                {
                    UpdateEmailList.Invoke(this, new UpdateEmailEventArgs(false, null));
                    ReleaseComObject();
                }

                BringParentWinTop(parentWinHandle);
            }
            catch (System.Exception e)
            {
                app.Log.Error(e);

                UpdateEmailList.Invoke(this, new UpdateEmailEventArgs(false, null));
                ReleaseComObject();

                app.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Outlook_Open"), false);
            }
        }

        private void BringParentWinTop(IntPtr parentWinHd)
        {
            if (parentWinHd == IntPtr.Zero) return;
           
            Process processes = Process.GetCurrentProcess();
            if (processes != null)
            {
                Win32Common.BringWindowToTop(parentWinHd, processes);
            }
        }

        private void BringWindowTop(string lpClassName, string lpWindowName)
        {
            new NoThrowTask(true, () =>
            {
                Thread.Sleep(500);
                
                Process[] anotherApps = Process.GetProcessesByName("OUTLOOK");
                if (anotherApps.Length == 0) return;
                if (anotherApps[0] != null)
                {
                    IntPtr winHandle = new Win32Common.WindowHandleInfo(lpClassName, lpWindowName).Result;
                    if (winHandle == IntPtr.Zero) return;
                    
                    Win32Common.SetForegroundWindow(winHandle);
                }
            }).Do();
        }

        public class UpdateEmailEventArgs : EventArgs
        {
            private bool monitor;
            private string email;

            public UpdateEmailEventArgs(bool monitorPar, string emailPar)
            {
                Monitor = monitorPar;
                Email = emailPar;
            }
            public bool Monitor { get => monitor; set => monitor = value; }
            public string Email { get => email; set => email = value; }
        }

    }
}
