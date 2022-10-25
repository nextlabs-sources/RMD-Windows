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
using System.Windows.Forms.Integration;
using System.Windows.Forms;
using Viewer.upgrade.ui.common.email.viewModel;
using Viewer.upgrade.file.basic.utils;

namespace Viewer.upgrade.ui.common.email.view
{
    /// <summary>
    /// Interaction logic for EmailPage.xaml
    /// </summary>
    public partial class EmailPage : Page
    {
        private ViewModel mViewModel;

        public EmailPage(string filePath)
        {
            InitializeComponent();
            mViewModel = new ViewModel(filePath, this);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            mViewModel.Page_Loaded();
        }

        public ISensor Sensor
        {
            get { return mViewModel; }
        }

        public void Watermark(WatermarkInfo watermarkInfo)
        {
            mViewModel.Watermark(watermarkInfo);
        }

        public void Print()
        {
            mViewModel.Print();
        }

    }
}
