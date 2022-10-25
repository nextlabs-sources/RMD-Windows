using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
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

namespace LoginWPFControl
{
    public enum URLType
    {
        Personal,
        Company
    }
    
    public class URLTypeToBoolenConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string parameterString = parameter as string;
            if (parameterString == null)
                return DependencyProperty.UnsetValue;

            if (Enum.IsDefined(value.GetType(), value) == false)
                return DependencyProperty.UnsetValue;

            object parameterValue = Enum.Parse(value.GetType(), parameterString);

            return parameterValue.Equals(value);

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string parameterString = parameter as string;
            if (parameterString == null)
                return DependencyProperty.UnsetValue;

            return Enum.Parse(targetType, parameterString);
        }
    }

    public class IntToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int)value > 0 ? true : false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public delegate void SelectedURLEventHandler(string url);

    public class ServerSelectViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public event SelectedURLEventHandler OnURlSelected;

        private URLType serverType;
        private string urlPersonal;
        private List<string> urlCompanies;
        private string urlSelected;

        private CmdSelectServer Cmd_SelectServer;

        public URLType ServerType
        {
            get => serverType;
            set
            {
                serverType = value;
                OnPropertyChanged("ServerType");                
            }
        }

        public bool IsCompany => serverType == URLType.Company;
        
        public bool IsPersonal => serverType == URLType.Personal;

        public string UrlPersonal { get => urlPersonal; set => urlPersonal = value; }

        public ICommand CmdSelectServer => Cmd_SelectServer;

        public List<string> UrlCompanies { get => urlCompanies; set => urlCompanies = value; }
        public string UrlSelected { get => urlSelected; set => urlSelected = value; }

        public ServerSelectViewModel()
        {
            ServerType = URLType.Company;
            Cmd_SelectServer = new CmdSelectServer(this);
            UrlPersonal = "https://www.skydrm.com";     // by default 
            UrlCompanies = new List<string>();
            urlSelected = "";
        }

        public bool IsCanGoNext()
        {
            // check if user inputed is valid
            return true;  
        }

        public void GoNext(string selectedURL)
        {
            
            if (OnURlSelected != null)
            {
                OnURlSelected.Invoke(selectedURL);
            }
        }


        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


    }

    public class CmdSelectServer : ICommand
    {
        public event EventHandler CanExecuteChanged;
        private ServerSelectViewModel vm;

        public CmdSelectServer(ServerSelectViewModel vm)
        {
            this.vm = vm;
        }

        public bool CanExecute(object parameter)
        {
            return vm.IsCanGoNext();
        }

        public void Execute(object parameter)
        {
            vm.GoNext((string)parameter);
        }
    }

    /// <summary>
    /// Interaction logic for ServerSelectControl.xaml
    /// </summary>
    public partial class ServerSelectControl : UserControl
    {
        private ServerSelectViewModel viewmodel;

        public ServerSelectControl()
        {
            InitializeComponent();
            DataContext = Viewmodel = new ServerSelectViewModel();
        }

        public ServerSelectViewModel Viewmodel { get => viewmodel; set=> DataContext=viewmodel = value; }
    }
}
