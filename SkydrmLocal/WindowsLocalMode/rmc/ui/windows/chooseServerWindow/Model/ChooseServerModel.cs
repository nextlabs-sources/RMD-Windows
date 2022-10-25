using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SkydrmLocal.rmc.ui.windows.chooseServerWindow.Model
{
    public class ChooseServerModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private SkydrmLocalApp app = (SkydrmLocalApp)SkydrmLocalApp.Current;

        public ChooseServerModel()
        {
            //Save all router to this List
            List<string> AllList = new List<string>();

            //get router from registry
            url = app.Config.CompanyRouter;

            if (!string.IsNullOrEmpty(url))
            {
                string[] urlArray = url.Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < urlArray.Length; i++)
                {
                    if (string.IsNullOrEmpty(urlArray[i].Trim()))
                    {
                        continue;
                    }
                    if (!AllList.Contains(urlArray[i].Trim().ToLower()))//Do not add,if it already exists
                    {
                        AllList.Add(urlArray[i].Trim().ToLower());
                    }              
                }
               
            }

            //get router from DB
            List<string> list = new List<string>();
            list = app.DBFunctionProvider.GetRouterUrl();

            for (int i = 0; i < list.Count; i++)
            {
                if (!AllList.Contains(list[i].Trim().ToLower()) 
                    && !list[i].Trim().Equals(app.Config.Router, StringComparison.CurrentCultureIgnoreCase))//Do not add,if it already exists. And don't add personal account router.
                {
                    AllList.Add(list[i].Trim().ToLower());
                }
            }

            //set ui display router List from AllList
            for (int i = 0; i < AllList.Count; i++)
            {
                UrlList.Add(new UrlDataModel(i, AllList[i]));
            }
            //set search List from UrlList
            foreach (UrlDataModel one in UrlList)
            {
                copyUrlList.Add(one);
            }
        }

        private ObservableCollection<UrlDataModel> urlList = new ObservableCollection<UrlDataModel>();
        public ObservableCollection<UrlDataModel> UrlList
        {
            get { return urlList; }
            set { urlList = value; }
        }

        // Used for do search.
        private ObservableCollection<UrlDataModel> copyUrlList = new ObservableCollection<UrlDataModel>();
        public void Serach(string text, out bool isDropDownOpen)
        {
            isDropDownOpen = false;

            UrlList.Clear();
            string searchText = text.Trim().ToLower();

            if (string.IsNullOrEmpty(searchText))
            {
                foreach (UrlDataModel one in copyUrlList)
                {
                    UrlList.Add(one);
                    isDropDownOpen = true;
                }

                return;
            }

            foreach (UrlDataModel one in copyUrlList)
            {
                if (one.listUrl.ToLower().Contains(searchText))
                {
                    UrlList.Add(one);
                    isDropDownOpen = true;
                }
            }
        }

        //public void InsertUrl()
        //{
        //    bool uniqueUrl = true;
        //    foreach (UrlDataModel one in UrlList)
        //    {
        //        if (one.Url.Equals(URL,StringComparison.CurrentCultureIgnoreCase))
        //        {
        //            uniqueUrl = false;
        //        }
        //    }
        //    if (uniqueUrl)
        //    {
        //        app.dBHelper.SetServerUrl(URL);
        //    }

        //}

        //This url for transfer parameters, not display in UI 
        private string url = "";
        public string URL
        {
            get
            {
                return url;
            }
            set
            {
                url = value;
            }
        }

        private ServerModel serverModel = ServerModel.Personal;
        public ServerModel ServerModel
        {
            get
            {
                return serverModel;
            }
            set
            {
                serverModel = value;
                OnPropertyChanged("ServerModel");
            }
        }

    }
    public enum ServerModel
    {
        Personal,
        Company
    }

    public class ChooseServerForTextboxConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ServerModel mode = (ServerModel)value;
            switch (mode)
            {
                case ServerModel.Personal:
                    return @"False";
                case ServerModel.Company:
                    return @"True";
                default:
                    return @"False";

            }
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class ChooseServerForTextboxBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ServerModel mode = (ServerModel)value;
            switch (mode)
            {
                case ServerModel.Personal:
                    return @"#A8F2F2F2";
                case ServerModel.Company:
                    return @"White";
                default:
                    return @"White";

            }
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class ChooseServerForTextblockConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ServerModel mode = (ServerModel)value;
            SkydrmLocalApp app = (SkydrmLocalApp)SkydrmLocalApp.Current;
            switch (mode)
            {
                case ServerModel.Personal:
                    return app.Config.Router;
                case ServerModel.Company:
                    return @"example:  https://skydrm.microsoft.com";
                default:
                    return app.Config.Router;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class ChooseServerForTextblockLabelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ServerModel mode = (ServerModel)value;
            switch (mode)
            {
                case ServerModel.Personal:
                    return @"URL";
                case ServerModel.Company:
                    return @"Enter URL";
                default:
                    return @"URL";

            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class ChooseServerForCheckboxConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ServerModel mode = (ServerModel)value;
            switch (mode)
            {
                case ServerModel.Personal:
                    return @"Collapsed";
                case ServerModel.Company:
                    return @"Visible";
                default:
                    return @"Collapsed";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
