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
using WpfAnimatedGif;
using Viewer.upgrade.ui.common.imagePage.viewModel;
using System.Windows.Media.Animation;
using Viewer.upgrade.utils;
using Viewer.upgrade.utils.overlay;

namespace Viewer.upgrade.ui.common.imagePage.view
{
    /// <summary>
    /// Interaction logic for ImagePage.xaml
    /// </summary>
    public partial class ImagePage : Page
    {
        private ViewModel mViewModel;

        public ImagePage(string filePath)
        {
            InitializeComponent();
            mViewModel = new ViewModel(filePath , this);
        }

        public ISensor Sensor
        {
            get { return mViewModel; }
        }

        public void Watermark(WatermarkInfo watermarkInfo)
        {
            mViewModel.Watermark(watermarkInfo);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
             mViewModel.Page_Loaded();
        }

        public void Print()
        {
            mViewModel.Print();
        }

        public void RotateLeft()
        {
            mViewModel.RotateLeft();
        }
  
        public void RotateRight()
        {
            mViewModel.RotateRight();
        }

        public void Reset() {
            if (null != Image)
            {
                var st = (ScaleTransform)((TransformGroup)Image.RenderTransform).Children.First(tr => tr is ScaleTransform);
                st.ScaleX = 1.0;
                st.ScaleY = 1.0;

                var tt = (TranslateTransform)((TransformGroup)Image.RenderTransform).Children.First(tr => tr is TranslateTransform);
                tt.X = 0.0;
                tt.Y = 0.0;
            }
        }

        Point start;
        Point origin;
        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Image.CaptureMouse();
            var tt = (TranslateTransform)((TransformGroup)Image.RenderTransform)
                .Children.First(tr => tr is TranslateTransform);
            start = e.GetPosition(Border);
            origin = new Point(tt.X, tt.Y);
        }

        private void Image_MouseMove(object sender, MouseEventArgs e)
        {
            if (Image.IsMouseCaptured)
            {
                var tt = (TranslateTransform)((TransformGroup)Image.RenderTransform)
                    .Children.First(tr => tr is TranslateTransform);
                Vector v = start - e.GetPosition(Border);
                tt.X = origin.X - v.X;
                tt.Y = origin.Y - v.Y;
            }
        }

        private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Image.ReleaseMouseCapture();
        }

        private void Host_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var st = (ScaleTransform)((TransformGroup)Image.RenderTransform).Children.First(tr => tr is ScaleTransform);
            double zoom = e.Delta > 0 ? 0.2 : -0.2;

            if (!(e.Delta > 0) && (st.ScaleX < 0.4 || st.ScaleY < 0.4))
            {
                return;
            }

            st.ScaleX += zoom;
            st.ScaleY += zoom;
        }

    }
}
