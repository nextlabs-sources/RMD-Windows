using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Viewer.upgrade.exception;
using Viewer.upgrade.file.basic.utils;

namespace Viewer.upgrade.utils.overlay.utils
{
    public class OverlayUtils
    {
        private static double DeviceIndependentUnit = Convert.ToDouble(1) / Convert.ToDouble(96);
        private static float DpiX = 96;
        private static float DpiY = 96;
        private static double DisplayMonitorX = SystemParameters.WorkArea.Width;
        private static double DisplayMonitorY = SystemParameters.WorkArea.Height;
        private const int MinimunMargin = 500;

        //public static Canvas DrawWatermark(WatermarkInfo watermarkInfo)
        //{
        //    Canvas canvas = new Canvas();
        //    DrawWatermarkInner(watermarkInfo, canvas);
        //    return canvas;
        //}

        public static void DrawWatermark(WatermarkInfo watermarkInfo, ref Canvas canvas)
        {
            DrawWatermarkInner(watermarkInfo, canvas);
        }

        private static void DrawWatermarkInner(WatermarkInfo watermarkInfo, Canvas canvas)
        {
            try
            {
                if (null == watermarkInfo || null == canvas)
                {
                    throw new ArgumentNullException();
                }
                canvas.IsEnabled = false;
                canvas.Focusable = false;
                canvas.Background = System.Windows.Media.Brushes.Transparent;

                int DpiDisplayMonitorX = Convert.ToInt32(Math.Round(DisplayMonitorX * (DeviceIndependentUnit * DpiX)));
                int DpiDisplayMonitorY = Convert.ToInt32(Math.Round(DisplayMonitorY * (DeviceIndependentUnit * DpiY)));

                //This operation is used to get TextBlock's width and height.
                TextBlock element;
                WrapperTextBlock(watermarkInfo, out element);
                element.Measure(new System.Windows.Size(Double.PositiveInfinity, Double.PositiveInfinity));
                element.Arrange(new Rect(element.DesiredSize));

                if (element.ActualWidth == 0 || element.ActualHeight == 0)
                {
                    return;
                }

                double startX = 0;
                double startY = 0;
                double rightMargin = element.ActualWidth < MinimunMargin ? MinimunMargin : element.ActualWidth;
                double topMargin = element.ActualHeight < MinimunMargin ? MinimunMargin : element.ActualHeight;

                for (double y = startY; y < DpiDisplayMonitorY; y += topMargin)
                {
                    for (double x = startX; x < DpiDisplayMonitorX; x += rightMargin)
                    {
                        WrapperTextBlock(watermarkInfo, out element);
                        Canvas.SetLeft(element, x);
                        Canvas.SetTop(element, y);
                        canvas.Children.Add(element);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public static void WrapperTextBlock(WatermarkInfo WatermarkInfo , out TextBlock textBlock)
        {
            textBlock = new TextBlock();
            try
            {
                BrushConverter brushConverter = new BrushConverter();
                System.Windows.Media.Brush brush = (System.Windows.Media.Brush)brushConverter.ConvertFromString(WatermarkInfo.FontColor);
                textBlock.Foreground = brush;
                // test for rotation --- 45
                textBlock.LayoutTransform = new RotateTransform(45);
                textBlock.Text = WatermarkInfo.Text;
                textBlock.FontFamily = new System.Windows.Media.FontFamily(WatermarkInfo.FontName);
                textBlock.FontSize = WatermarkInfo.FontSize;
                textBlock.Opacity = WatermarkInfo.TransparentRatio / 100f > 1.0 ? 1.0 : WatermarkInfo.TransparentRatio / 100f;
                textBlock.IsEnabled = false;
                textBlock.Focusable = false;
                textBlock.Background = System.Windows.Media.Brushes.Transparent;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static void DrawOverlayByGraphics(Graphics graphics, 
                                                double parentWidth,
                                                double parentHeight, 
                                                WatermarkInfo watermarkInfo)
        {
            if (null == graphics)
            {
                return;
            }
            if (null == watermarkInfo)
            {
                return;
            }
            if (parentWidth == 0 || parentHeight == 0)
            {
                return;
            }
            if (string.IsNullOrEmpty(watermarkInfo.Text) || string.IsNullOrWhiteSpace(watermarkInfo.Text))
            {
                return;
            }
            Console.WriteLine(parentWidth + "---" + parentHeight);
            //This operation is used to get Text's width and height.
            float emSize = (72.0f / 96.0f) * watermarkInfo.FontSize;
            SizeF size = graphics.MeasureString(watermarkInfo.Text, new Font(watermarkInfo.FontName, emSize, System.Drawing.FontStyle.Regular));
            double startX = 0;
            double startY = 0;
            double hypotenuse = size.Width;
            double height = size.Height;
            //  int width = (int)(hypotenuse / Math.Sqrt(2));
            int width = (int)hypotenuse;

            //Create Overlay text according to parent's width and height.
            for (double y = startY; y < parentHeight; y = y + width + height)
            {
                for (double x = startX; x < parentWidth; x = x + hypotenuse)
                {
                    // Create font and brush.
                    Font drawFont = new Font(watermarkInfo.FontName, emSize);
                    SolidBrush drawBrush = new SolidBrush(System.Drawing.Color.FromArgb((int)(0xFF * (watermarkInfo.TransparentRatio * 1.0 / 100)),
                        ColorTranslator.FromHtml(watermarkInfo.FontColor)));
                    // Create rectangle for drawing.
                    RectangleF drawRect = new RectangleF((float)x, (float)y, (float)hypotenuse, (float)height);
                    //Create matrix to rotate rectangle.
                    System.Drawing.Drawing2D.Matrix matrix = new System.Drawing.Drawing2D.Matrix();

                    // test for rotation --- 45
                    matrix.RotateAt(45,new PointF(drawRect.Left + (drawRect.Width / 2), drawRect.Top + (drawRect.Height / 2)));
                    graphics.Transform = matrix;

                    graphics.DrawString(watermarkInfo.Text, drawFont, drawBrush, drawRect);
                    graphics.ResetTransform();
                }
            }
        }
    }
}
