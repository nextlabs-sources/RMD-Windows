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
using Viewer.upgrade.ui.common.previewer2.viewModel;
using Viewer.upgrade.utils;
using static Viewer.upgrade.ui.common.previewer2.viewModel.ViewModel;

namespace Viewer.upgrade.ui.common.previewer2.view
{
    /// <summary>
    /// Interaction logic for PreviewerPage.xaml
    /// </summary>
    public partial class PreviewerPage : Page
    {
        private ViewModel mViewModel;
        public ISensor Sensor
        {
            get { return mViewModel; }
        }

        public INxlFile NxlFile
        {
            get { return mViewModel.NxlFile; }
            set { mViewModel.NxlFile = value; }
        }

        public WatermarkInfo WatermarkInfo
        {
            get { return mViewModel.WatermarkInfo; }
            set { mViewModel.WatermarkInfo = value; }
        }

        public int OverlayOffsetsBottom
        {
            get { return mViewModel.OverlayOffsestBottom; }
            set { mViewModel.OverlayOffsestBottom = value; }
        }

        public PreviewerPage(string filePath , PreviewHandler previewHandler)
        {
            InitializeComponent();
            mViewModel = new ViewModel(filePath, previewHandler, this);
        }

        public void Print()
        {
            mViewModel.Print();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            mViewModel.Page_Loaded();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            mViewModel.Page_Unloaded();
        }
    }
}
