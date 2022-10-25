using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Office.Interop.Outlook;
using Viewer.utils;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Interop;

namespace Viewer.share
{
    public class Outlook
    {
        private ViewerApp app = (ViewerApp)System.Windows.Application.Current;
        // Define Outlook Application Object
        private readonly Application outlookApp;

        private readonly NameSpace oNamespace;

        private ShareWindow parentWin;
        private IntPtr mParentWinHandle;

        public event EventHandler<UpdateEmailEventArgs> UpdateEmailList;

        public Outlook(ShareWindow sw)
        {
            this.parentWin = sw;

            //this.mParentWinHandle = Win32Common.GetForegroundWindow();
            this.mParentWinHandle = new WindowInteropHelper(sw).Handle;

            //// First step, Check outlook is or not install
            app.Log.Info("Check outlook is or not install");

            Type officeType = Type.GetTypeFromProgID("Outlook.Application");
            if (officeType == null)
            {
                CommonUtils.ShowBalloonTip(CultureStringInfo.Outlook_Install,false);
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
                    CommonUtils.ShowBalloonTip(CultureStringInfo.Outlook_LogIn,false);
                }
            }
            catch (System.Exception e)
            {
                app.Log.Error(e);
                ReleaseComObject();
                outlookApp = null;
                oNamespace = null;
                CommonUtils.ShowBalloonTip(CultureStringInfo.Outlook_Open,false);
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
                selectNameDialog.Caption = CultureStringInfo.Outlook_Dialog_Caption;
                selectNameDialog.NumberOfRecipientSelectors = OlRecipientSelectors.olShowTo;

                // "#32770" is SelectNamesDialog ClassName, obtain through Spy++
                BringWindowTop("#32770", CultureStringInfo.Outlook_Dialog_Caption);

                if (selectNameDialog.Display())
                {
                    parentWin.IsClosingOutlookAddressBookWin = true;

                    var recipients = selectNameDialog.Recipients;

                    new NoThrowTask(true, () =>
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
                    parentWin.IsClosingOutlookAddressBookWin = true;

                    UpdateEmailList.Invoke(this, new UpdateEmailEventArgs(false, null));
                    ReleaseComObject();
                }

                // Reset flag.
                ResetFlag();

                BringParentWinTop(mParentWinHandle);
            }
            catch (System.Exception e)
            {
                app.Log.Error(e);

                // Reset flag.
                ResetFlag();

                UpdateEmailList.Invoke(this, new UpdateEmailEventArgs(false, null));
                ReleaseComObject();

                CommonUtils.ShowBalloonTip(CultureStringInfo.Outlook_Open,false);
            }
        }

        private void ResetFlag()
        {
            new NoThrowTask(true, () =>
            {
                app.Log.Info(" ----Begin Reset flag----");
                Thread.Sleep(1000);
                parentWin.IsClosingOutlookAddressBookWin = false;
                app.Log.Info(" ----End Reset flag----");
            }).Do();
        }

        private void BringParentWinTop(IntPtr parentWinHd)
        {
            if (parentWinHd == IntPtr.Zero) return;
            Win32Common.BringWindowToTopEx(parentWinHd);
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
