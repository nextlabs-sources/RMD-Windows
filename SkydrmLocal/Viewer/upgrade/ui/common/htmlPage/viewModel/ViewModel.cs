using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Viewer.upgrade.file.basic.utils;
using Viewer.upgrade.ui.common.htmlPage.view;
using Viewer.upgrade.utils;
using Viewer.upgrade.utils.overlay.utils;
using Viewer.upgrade.utils.overlay.windowOverlay;

namespace Viewer.upgrade.ui.common.htmlPage.viewModel
{
    public class ViewModel : ISensor
    {
        private WatermarkInfo mWatermarkInfo;
        private string mFilePath;
        private HtmlPage mHtmlPage;
        private WindowOverlay mOverlay;
        public event Action<Exception> OnUnhandledExceptionOccurrence;
        public event Action OnLoadFileSucceed;

        public ViewModel(string filePath, HtmlPage htmlPage)
        {
            mFilePath = filePath;
            mHtmlPage = htmlPage;
        }

        public void Watermark(WatermarkInfo watermarkInfo)
        {
            mWatermarkInfo = watermarkInfo;
        }

        public void Page_Loaded()
        {
            try
            {
                LoadFile();
                AttachWatermark();
                OnLoadFileSucceed?.Invoke();
            }
            catch (Exception ex)
            {
                OnUnhandledExceptionOccurrence?.Invoke(ex);
            }
        }

        private void LoadFile()
        {
            try
            {
                mHtmlPage.WebBrowser.Url =new Uri(mFilePath);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void AttachWatermark()
        {
            try
            {
                if (null != mWatermarkInfo)
                {
                    mOverlay = new WindowOverlay();
                    Canvas overlayCanvas = new Canvas();
                     OverlayUtils.DrawWatermark(mWatermarkInfo, ref overlayCanvas);
                    overlayCanvas.Margin = new Thickness(0, 0, 0, 0);
                    mOverlay.OverlayContent = overlayCanvas;
                    mHtmlPage.Host.Children.Add(mOverlay);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void Print()
        {
            //var dataURL = "";
            //using (var image = CreateWatermarkImage("Watermark!",
            //    new Font("Arial", 16, System.Drawing.FontStyle.Bold), Color.Red, new System.Drawing.Size(150, 150)))
            //{
            //    dataURL = GetDataURL(image);
            //}
            //EnablePrintBackground(true);
            //mHtmlPage.WebBrowser.Print();

            //System.Windows.Controls.PrintDialog printDialog = new System.Windows.Controls.PrintDialog();
            //if (printDialog.ShowDialog() == true)
            //{
            //    mOverlay.ChildrenCount = 1;
            //    printDialog.PrintVisual(mHtmlPage.Host, "");
            //    mOverlay.ChildrenCount = 0;
            //}

            mHtmlPage.WebBrowser.ShowPrintPreviewDialog();
        }

        public System.Drawing.Image CreateWatermarkImage(string text, Font font, Color color, System.Drawing.Size size)
        {
            var bm = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bm))
            {
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                g.RotateTransform(45);
                var format = new StringFormat();
                format.Alignment = StringAlignment.Center;
                using (var brush = new SolidBrush(color))
                    g.DrawString(text, font, brush, new Rectangle(System.Drawing.Point.Empty, size), format);
            }
            return bm;
        }

        public static string GetDataURL(System.Drawing.Image image)
        {
            var bytes = (byte[])new ImageConverter().ConvertTo(image, typeof(byte[]));
            return $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
        }

        public void EnablePrintBackground(bool value)
        {
            using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Internet Explorer\PageSetup", true))
            {
                key.SetValue("Print_Background", value ? "yes" : "no",
                    Microsoft.Win32.RegistryValueKind.String);
                key.Close();
            }
        }

    }
}
