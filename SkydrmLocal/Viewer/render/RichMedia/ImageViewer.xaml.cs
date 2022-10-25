using System;
using System.Collections.Generic;
using System.Drawing.Printing;
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
using Viewer.viewer;
using Viewer.overlay;
using System.Drawing;
using WpfAnimatedGif;
using System.Windows.Media.Animation;
using System.IO;
using Viewer.utils;

namespace Viewer.render.RichMedia
{
    /// <summary>
    /// Interaction logic for ImageViewer.xaml
    /// </summary>
    public partial class ImageViewer : Page, IPrintable, IOverlay
    {
        // Check the content length of RichTextBox to see if using one page to print or more pages.
        private int CheckPrint;
        private Overlay Overlay = new Overlay();
        private ViewerWindow viewerWin;
        private WatermarkInfo WatermarkInfo;
        private string mFilePath = string.Empty;
        private log4net.ILog mLog;

        public ImageViewer(ViewerWindow viewerWin, string  path, WatermarkInfo watermarkInfo,log4net.ILog log)
        {
            log.Info("\t\t ImageViewer \r\n");
            InitializeComponent();
            this.viewerWin = viewerWin;
            this.viewerWin.Closed += ViewerWin_Closed;
            this.mFilePath = path;
            this.WatermarkInfo = watermarkInfo;
            this.mLog = log;
            LoadImage();
        }

        private void ViewerWin_Closed(object sender, EventArgs e)
        {
            mLog.Info("\t\t ImageViewer Closed \r\n");
        }

        /// <summary>
        /// Load Image into image control 
        /// </summary>
        private void LoadImage()
        {

            try
            {         
                //fileStream = new FileStream(NxlConverterResult.TmpPath,
                //                  FileMode.Open,
                //                  FileAccess.Read,
                //                  FileShare.None, 4096,
                //                  FileOptions.None);

              //  var image = new BitmapImage();
               // image.BeginInit();
               // image.StreamSource = fileStream;        
                //image.CacheOption = BitmapCacheOption.None;
                //image.EndInit();
                //image.Freeze();
               
                // add support for GIf
                 var image = new BitmapImage(new Uri(mFilePath));

            ImageBehavior.SetAnimatedSource(Img, image);
            ImageBehavior.SetAutoStart(Img, true);
            ImageBehavior.SetRepeatBehavior(Img, RepeatBehavior.Forever);
            }
            catch (Exception ex)
            {
                Scroll.Visibility = Visibility.Collapsed;
                viewerWin.HandlerException(StatusOfView.ERROR_DECRYPTFAILED, CultureStringInfo.VIEW_DLGBOX_DETAILS_IMAGE_DAMAGED);                 
                Console.WriteLine(ex.ToString());
            }
        }

        #region Overlay
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (IsAttach())
            {
                if (!Overlay.Initialze)
                {
                    InitOverlay();
                }
                AttachOverlay();
            }
        }

        private void InitOverlay()
        {
            Overlay.Initialize(Scroll,WatermarkInfo);
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (IsAttach())
            {
                if (Overlay.Initialze)
                {
                    Overlay.OnOverlayChange();
                }
                else
                {
                    InitOverlay();
                }
            }           
        }

        private void AttachOverlay()
        {
            try
            {
                if (IsAttach())
                {
                    Overlay.ParentLayer.Add((Adorner)Attach(Scroll.ActualWidth, Scroll.ActualHeight));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public bool IsAttach()
        {
            return RenderHelper.IsAttachOverlay(WatermarkInfo);
        }

        public UIElement Attach(double width, double height)
        {
            return Overlay.CreateOverlayInAdornerLayer(width, height);
        }

        #endregion Overlay

        #region Print
        /// <summary>
        /// Print entry
        /// </summary>
        public void Print()
        {
            using (var printDlg = new System.Windows.Forms.PrintDialog())
            {
                printDlg.Document = new PrintDocument();

                // Begin print handler
                printDlg.Document.BeginPrint += new PrintEventHandler(BeginPrint);

                // Register the print handler
                printDlg.Document.PrintPage += delegate (object obj, PrintPageEventArgs eventArgs)
                {
                    PrintPage(obj, eventArgs);
                };

                // Do some print setting
                printDlg.AllowSelection = true;
                printDlg.AllowSomePages = true;

                // Popup print dialog
                if (printDlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    // Execute print task, here will callback print handler.
                    printDlg.Document.Print();
                }
            }
        }
        /// <summary>
        /// Init "checkPrint" begin print.
        /// </summary>
        private void BeginPrint(object sender, PrintEventArgs eventArgs)
        {
            CheckPrint = 0;
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
            TransformGroup tg = Img.LayoutTransform as TransformGroup;
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
            BitmapImage bitmap = (BitmapImage)this.Img.Source;
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
            if (IsAttach())
            {
                Overlay.Graphics = e.Graphics;
                Overlay.DrawOverlayByGraphics(width, height);
            }
        }
        #endregion Print


        /// <summary>
        /// Rotate left
        /// </summary>
        public void RotateLeft()
        {
            TransformGroup tg = Img.LayoutTransform as TransformGroup;
            var tgnew = tg.CloneCurrentValue();
            if (tgnew != null)
            {
                RotateTransform rt = tgnew.Children[2] as RotateTransform;
                Img.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
                rt.Angle -= 90;
            }
            Img.LayoutTransform = tgnew;

        }
        /// <summary>
        /// Rotate right
        /// </summary>
        public void RotateRight()
        {
            TransformGroup tg = Img.LayoutTransform as TransformGroup;
            var tgnew = tg.CloneCurrentValue();
            if (tgnew != null)
            {
                RotateTransform rt = tgnew.Children[2] as RotateTransform;
                Img.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
                rt.Angle += 90;
            }
            Img.LayoutTransform = tgnew;
        }
    }
}
