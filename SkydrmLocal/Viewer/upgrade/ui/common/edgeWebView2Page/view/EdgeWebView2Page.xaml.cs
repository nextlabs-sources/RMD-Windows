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
using Viewer.upgrade.file.basic;
using Viewer.upgrade.file.basic.utils;
using Viewer.upgrade.ui.common.edgeWebView2Page.viewModel;

namespace Viewer.upgrade.ui.common.edgeWebView2Page.view
{
    /// <summary>
    /// Interaction logic for EdgeWebView2Page.xaml
    /// </summary>
    public partial class EdgeWebView2Page : Page
    {
        private ViewModel mViewModel;
        public EdgeWebView2Page(string filePath)
        {
            InitializeComponent();
            mViewModel = new ViewModel(filePath, this);
        }

        public ISensor Sensor
        {
            get { return mViewModel; }
        }

        public void Watermark(WatermarkInfo watermarkInfo)
        {
            mViewModel.Watermark(watermarkInfo);
        }

        public INxlFile NxlFile
        {
            get { return mViewModel.NxlFile; }
            set { mViewModel.NxlFile = value; }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            mViewModel.Page_Loaded();
        }

        private void Page_Unload(object sender, RoutedEventArgs e)
        {
            mViewModel.Page_Unload();
        }

        public void Print()
        {
            mViewModel.Print();
        }

        private void Page_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                e.Handled = true;
            }
        }
    }
}
