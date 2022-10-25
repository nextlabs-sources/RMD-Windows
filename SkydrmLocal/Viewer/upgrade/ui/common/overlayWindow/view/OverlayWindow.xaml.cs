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
using System.Windows.Shapes;
using Viewer.upgrade.file.basic.utils;
using Viewer.upgrade.ui.common.overlayWindow.viewModel;

namespace Viewer.upgrade.ui.common.overlayWindow.view
{
    /// <summary>
    /// Interaction logic for OverlayWindow.xaml
    /// </summary>
    public partial class OverlayWindow : Window
    {
        private ViewModel mViewModel;
        private System.Windows.Point mPoint;
        private int mActualWidth;
        private int mActualHeight;
        public OverlayWindow(System.Windows.Point point, int actualWidth, int actualHeight)
        {
            InitializeComponent();
            this.mPoint = point;
            this.mActualWidth = actualWidth;
            this.mActualHeight = actualHeight;
            Initialize();
            mViewModel = new ViewModel(this);
        }

        public OverlayWindow(WatermarkInfo watermarkInfo, System.Windows.Point point, int actualWidth, int actualHeight)
        {
            InitializeComponent();
            this.mPoint = point;
            this.mActualWidth = actualWidth;
            this.mActualHeight = actualHeight;
            Initialize();
            mViewModel = new ViewModel(watermarkInfo,this);
        }

        private void Initialize()
        {
            WindowStyle = WindowStyle.None;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            mViewModel.Window_Loaded(Host_Grid);
        }

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            mViewModel.Window_MouseWheel(e);
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            mViewModel.Window_MouseLeftButtonDown(e);
        }

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            mViewModel.Window_MouseLeftButtonUp(e);
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            mViewModel.SetRect(mPoint, mActualWidth, mActualHeight);
        }

        public void SetRect(System.Windows.Point point, int actualWidth, int actualHeight)
        {
            this.mPoint = point;
            this.mActualWidth = actualWidth;
            this.mActualHeight = actualHeight;
            mViewModel.SetRect(mPoint, mActualWidth, mActualHeight);
        }
    }
}
