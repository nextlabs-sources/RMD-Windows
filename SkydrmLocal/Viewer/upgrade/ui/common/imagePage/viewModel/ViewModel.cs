using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using Viewer.upgrade.file.basic.utils;
using Viewer.upgrade.ui.common.imagePage.view;
using Viewer.upgrade.utils.overlay.windowOverlay;
using System;
using System.Windows.Media.Imaging;
using WpfAnimatedGif;
using System.Windows.Media.Animation;
using Viewer.upgrade.utils;
using Viewer.upgrade.utils.overlay.utils;
using System.Drawing.Printing;
using System.Drawing;
using Alphaleonis.Win32.Filesystem;

namespace Viewer.upgrade.ui.common.imagePage.viewModel
{
    public class ViewModel : ISensor
    {
        private WatermarkInfo mWatermarkInfo;
        private ImagePage mImagePage;
        private WindowOverlay mOverlay;
        private string mFilePath;
        private int mPrint_Index;

        public event Action<Exception> OnUnhandledExceptionOccurrence;
        public event Action OnLoadFileSucceed;
        public event Action<bool> EndPrint;
        public event Action<System.Windows.Forms.PrintDialog> BeforePrint;

        public ViewModel(string filePath , ImagePage imagePage)
        {
            mFilePath = filePath;
            mImagePage = imagePage;
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
                // support GIf
                var image = new BitmapImage(new Uri(mFilePath));
                ImageBehavior.SetAnimatedSource(mImagePage.Image, image);
                ImageBehavior.SetAutoStart(mImagePage.Image, true);
                ImageBehavior.SetRepeatBehavior(mImagePage.Image, RepeatBehavior.Forever);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void RotateLeft()
        {
            TransformGroup tg = mImagePage.Image.LayoutTransform as TransformGroup;
            var tgnew = tg.CloneCurrentValue();
            if (tgnew != null)
            {
                RotateTransform rt = tgnew.Children[2] as RotateTransform;
                mImagePage.Image.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
                rt.Angle -= 90;
            }
            mImagePage.Image.LayoutTransform = tgnew;
        }

        public void RotateRight()
        {
            TransformGroup tg = mImagePage.Image.LayoutTransform as TransformGroup;
            var tgnew = tg.CloneCurrentValue();
            if (tgnew != null)
            {
                RotateTransform rt = tgnew.Children[2] as RotateTransform;
                mImagePage.Image.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
                rt.Angle += 90;
            }
            mImagePage.Image.LayoutTransform = tgnew;
        }

        private void AttachWatermark()
        {
            try
            {
                if (null != mWatermarkInfo)
                {
                    mOverlay = new WindowOverlay();
                    Canvas overlayCanvas = new Canvas();
                    OverlayUtils.DrawWatermark(mWatermarkInfo,ref overlayCanvas);
                    overlayCanvas.Margin = new Thickness(0, 0, 0, 0);
                    mOverlay.OverlayContent = overlayCanvas;
                    mImagePage.Host.Children.Add(mOverlay);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void Print()
        {
            try
            {
                using (var printDlg = new System.Windows.Forms.PrintDialog())
                {
                    printDlg.Document = new PrintDocument();
                    PrintController printController = new StandardPrintController();
                    printDlg.Document.PrintController = printController;

                    // Begin print handler
                    printDlg.Document.BeginPrint += new PrintEventHandler(BeginPrint);

                    // Register the print handler
                    printDlg.Document.PrintPage +=new PrintPageEventHandler(PrintPage);

                    //printDlg.Document.EndPrint += delegate (object sender, PrintEventArgs e){
                    //    EndPrint?.Invoke(true);
                    //};

                    // Do some print setting
                    printDlg.AllowSelection = true;
                    printDlg.AllowSomePages = true;
                    printDlg.UseEXDialog = true;

                    // Popup print dialog
                    if (printDlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        BeforePrint?.Invoke(printDlg);
                        // Execute print task, here will callback print handler.
                        printDlg.Document.Print();
                        EndPrint?.Invoke(true);
                    }
                }
            }
            catch (Exception ex)
            {
                EndPrint?.Invoke(false);
            }
        }

        /// <summary>
        /// Init "checkPrint" begin print.
        /// </summary>
        private void BeginPrint(object sender, PrintEventArgs eventArgs)
        {
            mPrint_Index = 0;
        }
        /// <summary>
        /// Print content
        /// </summary>
        private void PrintPage(object sender, PrintPageEventArgs e)
        {
            // print image
            System.Drawing.Image drawImg = GetDrawImage();

            int x = 0, y = 0;
            //image rotate angle
            double angle = 0;
            TransformGroup tg = mImagePage.Image.LayoutTransform as TransformGroup;
            var tgnew = tg.CloneCurrentValue();
            if (tgnew != null)
            {
                RotateTransform rt = tgnew.Children[2] as RotateTransform;
                angle = rt.Angle;
            }
            //rotate memory image to print
            switch (angle)
            {
                case -270:
                case 90:
                    drawImg.RotateFlip(RotateFlipType.Rotate90FlipNone);
                    break;
                case -180:
                case 180:
                    drawImg.RotateFlip(RotateFlipType.Rotate180FlipNone);
                    break;
                case -90:
                case 270:
                    drawImg.RotateFlip(RotateFlipType.Rotate270FlipNone);
                    break;
                default:
                    break;
            }
            //judge image size
            if (e.PageSettings.PaperSize.Width >= drawImg.Width && e.PageSettings.PaperSize.Height >= drawImg.Height)
            {
                x = (e.PageSettings.PaperSize.Width - drawImg.Width) / 2;
                y = (e.PageSettings.PaperSize.Height - drawImg.Height) / 2;
                e.Graphics.DrawImage(drawImg, x + 1, y + 1, drawImg.Width - 2, drawImg.Height - 2);

            }
            else if (e.PageSettings.PaperSize.Width > drawImg.Width && e.PageSettings.PaperSize.Height < drawImg.Height)
            {
                x = (e.PageSettings.PaperSize.Width - drawImg.Width) / 2;
                y = 1;
                e.Graphics.DrawImage(drawImg, x, y, drawImg.Width, e.PageSettings.PaperSize.Height - 2);
            }
            else if (e.PageSettings.PaperSize.Width < drawImg.Width && e.PageSettings.PaperSize.Height > drawImg.Height)
            {
                x = 1;
                y = (e.PageSettings.PaperSize.Height - drawImg.Height) / 2; ;
                e.Graphics.DrawImage(drawImg, x, y, e.PageSettings.PaperSize.Width - 2, drawImg.Height);
            }
            else
            {
                x = 1;
                y = 1;
                e.Graphics.DrawImage(drawImg, x, y, e.PageSettings.PaperSize.Width - 2, e.PageSettings.PaperSize.Height - 2);
            }

            // Print overlay
            PrintOverlay(e.PageSettings.PaperSize.Width, e.PageSettings.PaperSize.Height, e);
        }

        private System.Drawing.Image GetDrawImage()
        {
            BitmapImage bitmap = (BitmapImage)mImagePage.Image.Source;
            System.Drawing.Image drawImage = BitmapImageConvertToGDI(bitmap);
            return drawImage;
        }
        /// <summary>
        /// System.Windows.Media.Imaging.BitmapImage  Convert To System.Drawing.Image
        /// </summary>
        private System.Drawing.Image BitmapImageConvertToGDI(BitmapImage bitmap)
        {
            System.Drawing.Image Image;
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                System.Windows.Media.Imaging.BmpBitmapEncoder encoder = new System.Windows.Media.Imaging.BmpBitmapEncoder();
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
                encoder.Save(ms);
                ms.Flush();
                Image = System.Drawing.Image.FromStream(ms);
            }
            return Image;
        }

        public void PrintOverlay(int width, int height, PrintPageEventArgs e)
        {
            if (null != mWatermarkInfo)
            {
                OverlayUtils.DrawOverlayByGraphics(e.Graphics, width, height, mWatermarkInfo);
            }
        }

    }
}
