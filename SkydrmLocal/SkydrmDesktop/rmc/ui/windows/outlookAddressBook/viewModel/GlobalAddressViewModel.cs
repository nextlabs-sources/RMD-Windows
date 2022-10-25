using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.ui.windows.outlookAddressBook.viewModel
{
    public class GlobalAddressViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        
        // will refresh UI when add or remove one entry because of ObservableCollection
        private ObservableCollection<AddressData> fileInfoList = new ObservableCollection<AddressData>();
        //for windows ListView itemSource
        public ObservableCollection<AddressData> FileInfoList
        {
            get { return fileInfoList; }
            set { fileInfoList = value; }
        }

        // used for do search.
        private ObservableCollection<AddressData> copyFileList = new ObservableCollection<AddressData>();

        private OutlookConnector outlook;
        public GlobalAddressViewModel()
        {
            Init();
        }

        private void Init()
        {
            outlook = new OutlookConnector();
            DataTable dt = outlook.GlobalAddressListData();

            if (dt.Columns.Contains("Name")
                && dt.Columns.Contains("Title")
                && dt.Columns.Contains("Location")
                && dt.Columns.Contains("Department")
                && dt.Columns.Contains("EmailAddress")
                && dt.Columns.Contains("Company"))
            {
                foreach (DataRow dr in dt.Rows)
                {
                    fileInfoList.Add(new AddressData(dr["Name"].ToString(), dr["Title"].ToString(), dr["Location"].ToString(),
                        dr["Department"].ToString(), dr["EmailAddress"].ToString(), dr["Company"].ToString()));

                    copyFileList.Add(new AddressData(dr["Name"].ToString(), dr["Title"].ToString(), dr["Location"].ToString(),
                        dr["Department"].ToString(), dr["EmailAddress"].ToString(), dr["Company"].ToString()));
                }
            }
            
        }

        public void Search(string text)
        {
            fileInfoList.Clear();
            string searchText = text;

            if (string.IsNullOrEmpty(searchText))
            {
                foreach (AddressData one in copyFileList)
                {
                    fileInfoList.Add(one);
                }
                return;
            }

            foreach (AddressData one in copyFileList)
            {
                if (one.Name.StartsWith(searchText, StringComparison.CurrentCultureIgnoreCase))
                {
                    fileInfoList.Add(one);
                }
            }
        }

        public class AddressData
        {
            private string name;
            private string title;
            private string location;
            private string department;
            private string emailAddress;
            private string company;

            public AddressData(string namePar, string titlePar, string locationPra, string departmentPra, string emailAddressPar, string companyPra)
            {
                this.Name = namePar;
                this.Title = titlePar;
                this.Location = locationPra;
                this.Department = departmentPra;
                this.EmailAddress = emailAddressPar;
                this.Company = companyPra;
            }

            public string Name { get => name; set => name = value; }
            public string Title { get => title; set => title = value; }
            public string Location { get => location; set => location = value; }
            public string Department { get => department; set => department = value; }
            public string EmailAddress { get => emailAddress; set => emailAddress = value; }
            public string Company { get => company; set => company = value; }
        }
    }
}
