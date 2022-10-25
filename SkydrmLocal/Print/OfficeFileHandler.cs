using Print.utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Xps.Packaging;
using Alphaleonis.Win32.Filesystem;

namespace Print
{
    public class OfficeFileHandler : FileHandler
    {
        private float DpiX = 96;
        private float DpiY = 96;
        private double DeviceIndependentUnit = Convert.ToDouble(1) / Convert.ToDouble(96);
        private System.IO.FileStream mXpsfileStream;
        private Package mPackage;
        private Uri mPackageUri;
        private XpsDocument mXpsDocument;
        private FixedDocumentSequence mFixedDocumentSequence;
        private DocumentPaginator mDocumentPaginator;
        private string mWatermark = string.Empty;
        private string mOfficeFilePath;
        private string mXPSFilePath;
        private Int32 mNeedPrintPageCount;

        public void Watermark(string watermark)
        {
            mWatermark = watermark;
        }

        public OfficeFileHandler(string OfficeFilePath)
        {
            using (Graphics graphics = Graphics.FromHwnd(IntPtr.Zero))
            {
                DpiX = graphics.DpiX;
                DpiY = graphics.DpiY;
            }

            mOfficeFilePath = OfficeFilePath;

            mXPSFilePath = Path.GetTempPath() + System.Guid.NewGuid().ToString() + ".xps";

            OfficeToXpsConversionResult officeToXpsConversionResult = OfficeToXPS.ConvertToXps(mOfficeFilePath, ref mXPSFilePath);

            if (officeToXpsConversionResult.Result != ConversionResult.OK)
            {
                CommonUtils.MessageBox_("System internal error. Please contact your system administrator for further help.");
                return;
            }

            if (!File.Exists(mXPSFilePath))
            {
                return;
            }
            mNeedPrintPageCount = ComputeNeedPrintPageCount(mXPSFilePath);
        }

        private Int32 ComputeNeedPrintPageCount(string XPSFilePath)
        {
            Int32 result = -1;

            //lock this XPS file
            this.mXpsfileStream = new System.IO.FileStream(XPSFilePath,
                          System.IO.FileMode.Open,
                          System.IO.FileAccess.ReadWrite,
                          System.IO.FileShare.None, 4096,
                          System.IO.FileOptions.DeleteOnClose);

            mPackage = Package.Open(mXpsfileStream);

            string inMemoryPackageName = "memorystream://" + XPSFilePath;

            mPackageUri = new Uri(inMemoryPackageName);

            PackageStore.AddPackage(mPackageUri, mPackage);

            mXpsDocument = new XpsDocument(mPackage, CompressionOption.NotCompressed, inMemoryPackageName);

            mFixedDocumentSequence = mXpsDocument.GetFixedDocumentSequence();
            mDocumentPaginator = mFixedDocumentSequence.DocumentPaginator;
            mDocumentPaginator.ComputePageCount();
            result = mDocumentPaginator.PageCount;

            return result;
        }

        public bool IsNeedPrintWatermark()
        {
            bool result = true;

            if (string.IsNullOrEmpty(mWatermark))
            {
                return false;
            }

            return result;
        }

        private System.Drawing.Image GetImageFromXps(int index)
        {
            System.Drawing.Image result = null;

            FixedPage page = (FixedPage)mDocumentPaginator.GetPage(index).Visual;

            //bool isNeedPrintWatermark = IsNeedPrintWatermark();

            //if (isNeedPrintWatermark)
            //{
            //    if (null == mOverlay)
            //    {
            //        // Add the overlay to the fixed page.           
            //        mOverlay = Overlay.CreateOverlay(page.ActualWidth, page.ActualHeight, mPrintParameters.AdhocWatermark);
            //    }

            //    mOverlay.Visibility = Visibility.Visible;
            //    page.Children.Add(mOverlay);
            //    page.UpdateLayout();
            //}

            result = Convert2Image(page);

            if (IsNeedPrintWatermark())
            {
                using (Graphics graphics = Graphics.FromImage(result))
                {
                    AddWatermark(graphics, result, mWatermark);
                }
            }

            return result;
        }

        private void AddWatermark(Graphics graphics, System.Drawing.Image image, string watermark)
        {
            string fontName = "Arial";
            string fontColor = "#008015";
            int fontSize = 22;
            int transparentRatio = 70;
            int rotation = 45;

            //This operation is used to get Text's width and height.
            float emSize = (72.0f / 96.0f) * fontSize;
            SizeF size = graphics.MeasureString(watermark, new Font(fontName, emSize, System.Drawing.FontStyle.Regular));
            double startX = 0;
            double startY = 0;
            double hypotenuse = size.Width;
            double height = size.Height;
            int width = (int)(hypotenuse / Math.Sqrt(2));

            //Create Overlay text according to parent's width and height.
            for (double y = startY; y < image.Height; y = y + width + height)
            {
                for (double x = startX; x <= image.Width; x = x + hypotenuse)
                {
                    // Create font and brush.
                    Font drawFont = new Font(fontName, emSize);
                    SolidBrush drawBrush = new SolidBrush(System.Drawing.Color.FromArgb((int)(0xFF * (transparentRatio * 1.0 / 100)),
                        ColorTranslator.FromHtml(fontColor)));
                    // Create rectangle for drawing.
                    RectangleF drawRect = new RectangleF((float)x, (float)y, (float)hypotenuse, (float)height);
                    //Create matrix to rotate rectangle.
                    System.Drawing.Drawing2D.Matrix matrix = new System.Drawing.Drawing2D.Matrix();

                    // test for rotation --- 45
                    matrix.RotateAt(rotation,
                        new PointF(drawRect.Left + (drawRect.Width / 2), drawRect.Top + (drawRect.Height / 2)));
                    graphics.Transform = matrix;

                    graphics.DrawString(watermark, drawFont, drawBrush, drawRect);
                    graphics.ResetTransform();
                }
            }
        }

        public Image GetImage(int index)
        {
            return GetImageFromXps(index);
        }

        public int GetPageCount()
        {
            return mNeedPrintPageCount;
        }

        private System.Drawing.Image BitmapImageConvertToGDI(BitmapImage bitmap)
        {
            System.Drawing.Image Image;
            System.IO.MemoryStream ms = null;
            System.Windows.Media.Imaging.BmpBitmapEncoder encoder = null;

            try
            {
                ms = new System.IO.MemoryStream();
                encoder = new System.Windows.Media.Imaging.BmpBitmapEncoder();
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
                encoder.Save(ms);
                ms.Flush();
                Image = System.Drawing.Image.FromStream(ms);
            }
            finally
            {
                if (null != ms)
                {
                    ms.Close();
                }
                if (null != encoder)
                {
                    encoder.Frames.Clear();
                }
            }

            return Image;
        }

        private System.Drawing.Image Convert2Image(FixedPage page)
        {
            System.Drawing.Image image = null;

            BitmapImage bitmapImage = null;

            try
            {
                int width = Convert.ToInt32(Math.Round(page.ActualWidth * (DeviceIndependentUnit * DpiX)));

                int height = Convert.ToInt32(Math.Round(page.ActualHeight * (DeviceIndependentUnit * DpiY)));

                RenderTargetBitmap rt = new RenderTargetBitmap(width, height, DpiX, DpiY, PixelFormats.Pbgra32);

                rt.Render(page);

                PngBitmapEncoder encoder = new PngBitmapEncoder();

                encoder.Frames.Add(BitmapFrame.Create(rt));

                bitmapImage = new BitmapImage();

                using (System.IO.Stream stream = new System.IO.MemoryStream())
                {
                    encoder.Save(stream);
                    stream.Seek(0, System.IO.SeekOrigin.Begin);
                    stream.Position = 0;
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = stream;
                    bitmapImage.EndInit();
                }

                encoder.Frames.Clear();

                image = BitmapImageConvertToGDI(bitmapImage);

            }
            catch (Exception ex)
            {

            }
            finally
            {
                bitmapImage.StreamSource.Close();
                bitmapImage.StreamSource.Dispose();
            }

            return image;
        }

        public void Release()
        {
            if (null != mXpsDocument)
            {
                mXpsDocument.Close();
            }

            if (null != mPackageUri)
            {
                PackageStore.RemovePackage(mPackageUri);
            }

            if (null != mPackage)
            {
                mPackage.Close();
            }

            if (null != mXpsfileStream)
            {
                mXpsfileStream.Close();
            }
        }
    }
}
