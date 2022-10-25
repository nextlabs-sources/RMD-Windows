using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Office.Interop.Outlook;

namespace SkydrmLocal.rmc.ui.windows.outlookAddressBook.viewModel
{
    public class OutlookConnector
    {
        private SkydrmLocalApp app = SkydrmLocalApp.Singleton;
        // Define Outlook Application Object
        private readonly Application outlook;

        private readonly NameSpace oNamespace;

        public OutlookConnector()
        {
            app.Log.Info("Check outlook is or not install");
            // Check outlook is or not install
            Type officeType = Type.GetTypeFromProgID("Outlook.Application");
            if (officeType == null)
            {
                app.ShowBalloonTip("Please install Outlook.");
                outlook = null;
                return;
            }
            
            try
            {
                app.Log.Info("Check outlook is or not open");
                // Check outlook is or not open
                outlook = (Application)Marshal.GetActiveObject("Outlook.Application");

                app.Log.Info("Check user is or not login outlook");
                // Check user is or not log in outlook
                oNamespace = outlook?.GetNamespace("MAPI");
                string userName = oNamespace?.CurrentUser?.Name;
                if (string.IsNullOrEmpty(userName))
                {
                    outlook = null;
                    oNamespace = null;
                    app.ShowBalloonTip("Please login Outlook.");
                }
                app.Log.Info("Outlook.CurrentUser.Name:"+ userName);
            }
            catch (System.Exception e)
            {
                app.Log.Error(e);
                outlook = null;
                oNamespace = null;
                app.ShowBalloonTip("Please open Outlook.");
            }
            
        }

        private AddressLists GetAddressLists()
        {
            //oNamespace.Logon("MS Exchange Settings", "", true, true);
            return oNamespace?.AddressLists;
        }

        private AddressList GetGlobalAddressList()
        {
            //oNamespace.Logon("MS Exchange Settings", "", true, true);
            return oNamespace?.GetGlobalAddressList();
        }

        private DataTable CustomDataTable()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Name", typeof(String));
            dt.Columns.Add("Title", typeof(String));
            dt.Columns.Add("Location", typeof(String));
            dt.Columns.Add("Department", typeof(String));
            dt.Columns.Add("EmailAddress", typeof(String));
            dt.Columns.Add("Company", typeof(String));
            return dt;
        }

        private void BuildData(ref DataTable dt, AddressEntries addEntries)
        {
            for (int i = 1; i < addEntries.Count + 1; i++)
            {
                var nextAddEntry = addEntries[i];
                BuidData(ref dt, nextAddEntry);
            }
        }

        private void BuidData(ref DataTable dt, AddressEntry addEntry)
        {
            var addEntries = addEntry.Members;

            if (addEntries == null)
            {
                var exchangeUser = addEntry?.GetExchangeUser();
                if (exchangeUser == null)
                {
                    Console.WriteLine("*************exchangeUser is null*************");
                    return;
                }
                if (string.IsNullOrEmpty(exchangeUser.PrimarySmtpAddress))
                {
                    Console.WriteLine("*************exchangeUser.PrimarySmtpAddress IsNullOrEmpty*************");
                }
                DataRow dr = dt.NewRow();
                dr["Name"] = exchangeUser.Name;
                dr["Title"] = exchangeUser.JobTitle;
                dr["Location"] = exchangeUser.OfficeLocation;
                dr["Department"] = exchangeUser.Department;
                dr["EmailAddress"] = exchangeUser.PrimarySmtpAddress;
                dr["Company"] = exchangeUser.CompanyName;
                dt.Rows.Add(dr);
            }
            else
            {
                for (int i = 1; i < addEntries.Count + 1; i++)
                {
                    var nextAddEntry = addEntries[i];
                    BuidData(ref dt, nextAddEntry);
                }
            }
        }

        public DataTable GlobalAddressListData()
        {
            DataTable dt = CustomDataTable();

            var globalAdList = GetGlobalAddressList();
            if (globalAdList == null)
            {
                return dt;
            }

            var addEntries = globalAdList.AddressEntries;

            BuildData(ref dt, addEntries);

            return dt;
        }

        public void SelectNameDialog()
        {
            SelectNamesDialog selectNameDialog = outlook.Session.GetSelectNamesDialog();
            selectNameDialog.NumberOfRecipientSelectors = OlRecipientSelectors.olShowTo;

            //selectNameDialog.ForceResolution = true;
            if (selectNameDialog.Display())
            {
                var recipients = selectNameDialog.Recipients;

                for (int i = 1; i < recipients.Count + 1; i++)
                {
                    Recipient recipient = recipients[i];
                    Console.WriteLine("*********recipient.Address：******" + recipient.Address);
                    var addEntry = recipient.AddressEntry;

                    DataTable dt = CustomDataTable();

                    BuidData(ref dt, addEntry);

                }
            }

        }

    }
}
