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
using Viewer.upgrade.file.basic.utils;
using Viewer.upgrade.ui.common.richTextPage.viewModel;

namespace Viewer.upgrade.ui.common.richTextPage.view
{
    /// <summary>
    /// Interaction logic for RichTextPage.xaml
    /// </summary>
    public partial class RichTextPage : Page
    {
        private ViewModel mViewModel;

        public RichTextPage(string filePath)
        {
            InitializeComponent();
            mViewModel = new ViewModel(filePath,this);
        }

        public ISensor Sensor
        {
            get { return mViewModel; }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            mViewModel.Page_Loaded();
        }

        public void Print()
        {
             mViewModel.Print();
        }

        public void Watermark(WatermarkInfo watermarkInfo)
        {
            mViewModel.Watermark(watermarkInfo);
        }

        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = false;
            e.Handled = true;
        }
    }
}
