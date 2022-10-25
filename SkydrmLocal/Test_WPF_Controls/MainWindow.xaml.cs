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
using LoginWPFControl;
using System.Collections.ObjectModel;

namespace Test_WPF_Controls
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();           

            sel.Viewmodel = new ServerSelectViewModel()
            {
                ServerType = URLType.Personal,
                UrlPersonal = "https://www.google.com",
                UrlCompanies = new List<string>()
                {
                    "www.12333.com","www.nextlabs.com","www.abc.com","www.789.com","www.1231.com"
                }
            };

            sel.Viewmodel.OnURlSelected += (url)=>
            {
                if (sel.Viewmodel.IsCompany)
                {
                    MessageBox.Show("for company:" + url);
                }
                else if (sel.Viewmodel.IsPersonal)
                {
                    MessageBox.Show("for personal:" + url);
                }
            };


            web.Show(@"https://rms-centos7513.qapf1.qalab01.nextlabs.com:8444/rms/login");

            
            
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //FileInfoWindow fileInfoWindow = new FileInfoWindow()
            //{
                //ViewModel = new CustomControls.windows.fileInfo.viewModel.FileInfoWindowViewModel
                //{
                //    Name = "ExcelTest-2019-07-10-03-27-19.xlsx.nxl",
                //    Path = "ExcelTest-2019-07-10-03-27-19.xlsx.nxl",
                //    Size = "ExcelTest-2019-07-10-03-27-19.xlsx.nxl",
                //    LastModified = "ExcelTest-2019-07-10-03-27-19.xlsx.nxl",
                //    Expiration = "ExcelTest-2019-07-10-03-27-19.xlsx.nxl",
                //    WaterMark = "ExcelTest-2019-07-10-03-27-19.xlsx.nxl",
                //    Emails = new ObservableCollection<string>
                //    {
                //        "www.12333.com","www.nextlabs.com","www.abc.com","www.789.com","www.1231.com"
                //    },
                //    FileRights = new ObservableCollection<FileRights>
                //    {
                //         FileRights.RIGHT_VIEW,
                //         FileRights.RIGHT_EDIT ,
                //         FileRights.RIGHT_PRINT,
                //         FileRights.RIGHT_SAVEAS ,
                //         FileRights.RIGHT_SHARE ,
                //         FileRights.RIGHT_DOWNLOAD ,
                //         FileRights.RIGHT_WATERMARK ,
                //    },
                //    IsByCentrolPolicy = false,
            // }
            //};
            //fileInfoWindow.Show();
            
        }
    }
}
