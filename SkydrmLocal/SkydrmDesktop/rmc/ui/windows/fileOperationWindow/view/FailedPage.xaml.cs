using SkydrmDesktop.rmc.ui.utils;
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

namespace SkydrmDesktop.rmc.ui.windows.fileOperationWindow.view
{
    public struct FailedFile
    {
        public string FileName { get; set; }
        public string ErrorMsg { get; set; }
    }

    #region Convert
    /// <summary>
    /// List file type icon
    /// </summary>
    public class ListFile2IconConverterEx : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                string name = (string)value;

                string originFilename = name;

                bool isNxl = System.IO.Path.GetExtension(originFilename).Equals(".nxl");

                if (isNxl)
                {
                    int lastindex = name.LastIndexOf('.');
                    if (lastindex != -1)
                    {
                        originFilename = name.Substring(0, lastindex); // remove .nxl
                    }
                }

                string fileType = System.IO.Path.GetExtension(originFilename); // return .txt or null or empty
                if (string.IsNullOrEmpty(fileType))
                {
                    fileType = "---";
                }
                else if (fileType.IndexOf('.') != -1)
                {
                    fileType = fileType.Substring(fileType.IndexOf('.') + 1).ToLower();
                    if (!FileIconSupportHelper.IsSupportFileTypeEx(fileType))
                    {
                        fileType = "---";
                    }
                }
                else
                {
                    fileType = "---";
                }

                string uritemp = "";
                if (isNxl)
                {
                    uritemp = string.Format(@"/rmc/resources/fileTypeIcons/{0}_G.png", fileType.ToUpper());
                }
                else
                {
                    uritemp = string.Format(@"/rmc/resources/fileTypeIcons/{0}.png", fileType.ToUpper());
                }
                var stream = new Uri(uritemp, UriKind.Relative);
                return new BitmapImage(stream);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());

                return new BitmapImage(new Uri(@"/rmc/resources/fileTypeIcons/---_G.png", UriKind.Relative));
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    #endregion

    /// <summary>
    /// FailedPage.xaml  DataCommands
    /// </summary>
    public class FailedP_DataCommands
    {
        private static RoutedCommand close;
        static FailedP_DataCommands()
        {
            InputGestureCollection input = new InputGestureCollection();
            input.Add(new KeyGesture(Key.Escape));
            close = new RoutedCommand(
              "Close", typeof(FailedP_DataCommands), input);
        }
        /// <summary>
        /// FailedPage.xaml close button command
        /// </summary>
        public static RoutedCommand Close
        {
            get { return close; }
        }
    }

    /// <summary>
    /// ViewModel for FailedPage.xaml
    /// </summary>
    public class FailedPageViewMode : INotifyPropertyChanged
    {
        private string mTitle = "test Title";

        private ObservableCollection<FailedFile> failedFileList = new ObservableCollection<FailedFile>();

        private string failedDesc;
        private string failedDest;

        private string successDesc1;
        private string successDesc2;
        private string successDesc3;
        private string successDest;

        public FailedPageViewMode()
        {
        }

        /// <summary>
        /// Title, defult value is 'test Title'
        /// </summary>
        public string Title { get => mTitle; set { mTitle = value; OnBindUIPropertyChanged("Title"); } }

        /// <summary>
        /// Failed file list
        /// </summary>
        public ObservableCollection<FailedFile> FailedFileList => failedFileList;

        /// <summary>
        ///First part failed describe, foreground is #EB5757
        /// </summary>
        public string FailedDesc { get => failedDesc; set { failedDesc = value; OnBindUIPropertyChanged("FailedDesc"); } }

        /// <summary>
        /// Second part failed describe that destination, foreground is Black
        /// </summary>
        public string FailedDest { get => failedDest; set { failedDest = value; OnBindUIPropertyChanged("FailedDest"); } }

        /// <summary>
        ///First part success describe, foreground is #868686
        /// </summary>
        public string SuccessDesc1 { get => successDesc1; set { successDesc1 = value; OnBindUIPropertyChanged("SuccessDesc1"); } }

        /// <summary>
        /// Second part success describe, foreground is #45B649
        /// </summary>
        public string SuccessDesc2 { get => successDesc2; set { successDesc2 = value; OnBindUIPropertyChanged("SuccessDesc2"); } }

        /// <summary>
        /// Third part success describe, foreground is #868686
        /// </summary>
        public string SuccessDesc3 { get => successDesc3; set { successDesc3 = value; OnBindUIPropertyChanged("SuccessDesc3"); } }

        /// <summary>
        /// Fourth part success describe that destination, foreground is black
        /// </summary>
        public string SuccessDest { get => successDest; set { successDest = value; OnBindUIPropertyChanged("SuccessDest"); } }


        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnBindUIPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }

    /// <summary>
    /// Interaction logic for FailedPage.xaml
    /// </summary>
    public partial class FailedPage : Page
    {
        private FailedPageViewMode viewMode;
        public FailedPage()
        {
            InitializeComponent();

            this.DataContext = viewMode = new FailedPageViewMode();
        }

        /// <summary>
        ///  ViewModel for FailedPage.xaml
        /// </summary>
        public FailedPageViewMode ViewMode { get => viewMode; }
    }
}
