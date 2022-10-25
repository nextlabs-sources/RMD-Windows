using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using Viewer.upgrade.file.basic.utils;

namespace Viewer.upgrade.utils.overlay.window
{
    /// <summary>
    /// Interaction logic for OverlayWindow.xaml
    /// </summary>
    public partial class OverlayWindow : Window
    {
        private static double DeviceIndependentUnit = Convert.ToDouble(1) / Convert.ToDouble(96);
        private static float DpiX = 96;
        private static float DpiY = 96;
        private static double DisplayMonitorX = SystemParameters.WorkArea.Width;
        private static double DisplayMonitorY = SystemParameters.WorkArea.Height;
        private WatermarkInfo mWatermarkInfo;

        public OverlayWindow(WatermarkInfo watermarkInfo)
        {
            InitializeComponent();
            mWatermarkInfo = watermarkInfo;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            FormattedText formattedText = new FormattedText(
                                                mWatermarkInfo.Text,
                                                CultureInfo.CurrentCulture,
                                                FlowDirection.LeftToRight,
                                                new Typeface(mWatermarkInfo.FontName),
                                                mWatermarkInfo.FontSize,
                                                System.Windows.Media.Brushes.Green);
            formattedText.MaxTextWidth = formattedText.Width / 2;
            formattedText.MaxTextHeight = formattedText.Height / 2;
            formattedText.MaxLineCount = 2;

            BrushConverter brushConverter = new BrushConverter();
            Brush brush = (System.Windows.Media.Brush)brushConverter.ConvertFromString(mWatermarkInfo.FontColor);
            brush.Opacity = 0.3;
            formattedText.SetForegroundBrush(brush);
         
            double startX = 0;
            double startY = 0;
            int dpiDisplayMonitorX = Convert.ToInt32(Math.Round(DisplayMonitorX * (DeviceIndependentUnit * DpiX)));
            int dpiDisplayMonitorY = Convert.ToInt32(Math.Round(DisplayMonitorY * (DeviceIndependentUnit * DpiY)));
            //Create Overlay text according to parent's width and height.
            for (double y = startY; y < dpiDisplayMonitorY; y = y + formattedText.MaxTextWidth + formattedText.Height)
            {
                for (double x = startX; x < dpiDisplayMonitorX; x = x + formattedText.MaxTextWidth )
                {

                    //// Create font and brush.
                    //Font drawFont = new Font(mWatermarkInfo.FontName, mWatermarkInfo.FontSize);
                    //SolidBrush drawBrush = new SolidBrush(System.Drawing.Color.FromArgb((int)(0xFF * (mWatermarkInfo.TransparentRatio * 1.0 / 100)),
                    //    ColorTranslator.FromHtml(mWatermarkInfo.FontColor)));

                    drawingContext.PushTransform(new RotateTransform(45));
                    // Draw the formatted text string to the DrawingContext of the control.
                    drawingContext.DrawText(formattedText, new Point(x, y));
                    drawingContext.Pop();
                }
            }
        }
    }
}
