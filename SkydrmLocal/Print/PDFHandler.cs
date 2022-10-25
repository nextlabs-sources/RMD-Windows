using Print.utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Print
{
    public class PDFHandler : FileHandler
    {
        private float DpiX = 96;
        private float DpiY = 96;
        private double DeviceIndependentUnit = Convert.ToDouble(1) / Convert.ToDouble(96);
        private IntPtr mDocument = IntPtr.Zero;
        private string mWatermark = string.Empty;

        public void Watermark(string watermark)
        {
            mWatermark = watermark;
        }

        public PDFHandler(string pdfFilePath)
        {
            using (Graphics graphics = Graphics.FromHwnd(IntPtr.Zero))
            {
                DpiX = graphics.DpiX;
                DpiY = graphics.DpiY;
            }

            FPDF_LIBRARY_CONFIG_ fPDF = new FPDF_LIBRARY_CONFIG_();
            fPDF.version = 2;
            fPDF.m_pUserFontPaths = IntPtr.Zero;
            fPDF.m_pIsolate = IntPtr.Zero;
            fPDF.m_v8EmbedderSlot = 0;

            IntPtr intPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(FPDF_LIBRARY_CONFIG_)));
            Marshal.StructureToPtr(fPDF, intPtr, true);

            FPDF_InitLibraryWithConfig(intPtr);

            mDocument = FPDF_LoadDocument(pdfFilePath, null);
            if (IntPtr.Zero == mDocument)
            {
                CommonUtils.MessageBox_("System internal error. Please contact your system administrator for further help.");
                return;
            }
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

        public System.Drawing.Image GetImage(int index)
        {
            System.Drawing.Image result = null;

            IntPtr pdfPage = FPDF_LoadPage(mDocument, index);

            double width = FPDF_GetPageWidth(pdfPage);

            double Height = FPDF_GetPageHeight(pdfPage);

            int imageWidth = Convert.ToInt32(Math.Round(width * (DeviceIndependentUnit * DpiX)));

            int imageHeight = Convert.ToInt32(Math.Round(Height * (DeviceIndependentUnit * DpiY)));

            IntPtr hMemDC = CreateCompatibleDC(IntPtr.Zero);

            IntPtr hDC = GetDC(IntPtr.Zero);

            // Create a bitmap for output 
            var hBitmap = CreateCompatibleBitmap(hDC, imageWidth, imageHeight);

            // Select the bitmap into the memory DC 
            hBitmap = SelectObject(hMemDC, hBitmap);

            IntPtr brush = CreateSolidBrush((int)ColorTranslator.ToWin32(System.Drawing.Color.White));
            FillRgn(hMemDC, CreateRectRgn(0, 0, imageWidth, imageHeight), brush);

            // _file is an instance of PdfFile from PdfViewer port
            // Now render the page onto the memory DC, which in turn changes the bitmap                 
            FPDF_RenderPage(
                         hMemDC,
                         pdfPage,
                         0,
                         0,
                         imageWidth,
                         imageHeight,
                         0,
                         0
                         );

            hBitmap = SelectObject(hMemDC, hBitmap);

            result = Bitmap.FromHbitmap(hBitmap);

            FPDF_ClosePage(pdfPage);

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

        public int GetPageCount()
        {
            return FPDF_GetPageCount(mDocument);
        }

        public void Release()
        {
            if (IntPtr.Zero != mDocument)
            {
                FPDF_CloseDocument(mDocument);
                FPDF_DestroyLibrary();
            }
        }

        [StructLayoutAttribute(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct FPDF_LIBRARY_CONFIG_
        {
            // Version number of the interface. Currently must be 2.
            public Int32 version;
            // Array of paths to scan in place of the defaults when using built-in
            // FXGE font loading code. The array is terminated by a NULL pointer.
            // The Array may be NULL itself to use the default paths. May be ignored
            // entirely depending upon the platform.
            public IntPtr m_pUserFontPaths;
            // Version 2.
            // pointer to the v8::Isolate to use, or NULL to force PDFium to create one.
            public IntPtr m_pIsolate;
            // The embedder data slot to use in the v8::Isolate to store PDFium's
            // per-isolate data. The value needs to be between 0 and
            // v8::Internals::kNumIsolateDataLots (exclusive). Note that 0 is fine
            // for most embedders.
            public uint m_v8EmbedderSlot;
        }

        [DllImport("pdfium.dll",
           SetLastError = true,
           CallingConvention = CallingConvention.Cdecl,
           CharSet = CharSet.Ansi,
           EntryPoint = "FPDF_InitLibraryWithConfig")]
        public static extern void FPDF_InitLibraryWithConfig(IntPtr config);

        [DllImport("pdfium.dll",
            SetLastError = true,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi,
            EntryPoint = "FPDF_DestroyLibrary")]
        public static extern void FPDF_DestroyLibrary();

        [DllImport("pdfium.dll",
            SetLastError = true,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi,
            EntryPoint = "FPDF_CloseDocument")]
        public static extern void FPDF_CloseDocument(IntPtr document);

        [DllImport("pdfium.dll",
         SetLastError = true,
         CallingConvention = CallingConvention.Cdecl,
         CharSet = CharSet.Ansi,
         EntryPoint = "FPDF_LoadDocument")]
        public static extern IntPtr FPDF_LoadDocument(string file_path, string password);

        [DllImport("pdfium.dll",
        SetLastError = true,
        CallingConvention = CallingConvention.Cdecl,
        CharSet = CharSet.Ansi,
        EntryPoint = "FPDF_GetLastError")]
        public static extern ulong FPDF_GetLastError();

        [DllImport("pdfium.dll",
         SetLastError = true,
         CallingConvention = CallingConvention.Cdecl,
         CharSet = CharSet.Ansi,
         EntryPoint = "FPDF_GetPageCount")]
        public static extern int FPDF_GetPageCount(IntPtr document);


        [DllImport("pdfium.dll",
        SetLastError = true,
        CallingConvention = CallingConvention.Cdecl,
        CharSet = CharSet.Ansi,
        EntryPoint = "FPDF_LoadPage")]
        public static extern IntPtr FPDF_LoadPage(IntPtr document,
                                                  int page_index);

        [DllImport("pdfium.dll",
           SetLastError = true,
           CallingConvention = CallingConvention.Cdecl,
           CharSet = CharSet.Ansi,
           EntryPoint = "FPDF_ClosePage")]
        public static extern void FPDF_ClosePage(IntPtr page);

        [DllImport("pdfium.dll",
        SetLastError = true,
        CallingConvention = CallingConvention.Cdecl,
        CharSet = CharSet.Ansi,
        EntryPoint = "FPDF_RenderPageBitmap")]
        public static extern void FPDF_RenderPageBitmap(IntPtr bitmap,
                                                     IntPtr page,
                                                     int start_x,
                                                     int start_y,
                                                     int size_x,
                                                     int size_y,
                                                     int rotate,
                                                     int flags);

        [DllImport("pdfium.dll",
        SetLastError = true,
        CallingConvention = CallingConvention.Cdecl,
        CharSet = CharSet.Ansi,
        EntryPoint = "FPDF_RenderPage")]
        public static extern void FPDF_RenderPage(IntPtr dc,
                                               IntPtr page,
                                               int start_x,
                                               int start_y,
                                               int size_x,
                                               int size_y,
                                               int rotate,
                                               int flags);

        [DllImport("pdfium.dll",
          SetLastError = true,
          CallingConvention = CallingConvention.Cdecl,
          CharSet = CharSet.Ansi,
          EntryPoint = "FPDFBitmap_Create")]
        public static extern IntPtr FPDFBitmap_Create(int width, int height, int alpha);

        [DllImport("pdfium.dll",
         SetLastError = true,
         CallingConvention = CallingConvention.Cdecl,
         CharSet = CharSet.Ansi,
         EntryPoint = "FPDFBitmap_Destroy")]
        public static extern void FPDFBitmap_Destroy(IntPtr bitmap);

        [DllImport("pdfium.dll",
           SetLastError = true,
           CallingConvention = CallingConvention.Cdecl,
           CharSet = CharSet.Ansi,
           EntryPoint = "FPDF_GetPageWidth")]
        public static extern double FPDF_GetPageWidth(IntPtr page);

        [DllImport("pdfium.dll",
             SetLastError = true,
             CallingConvention = CallingConvention.Cdecl,
             CharSet = CharSet.Ansi,
             EntryPoint = "FPDF_GetPageHeight")]
        public static extern double FPDF_GetPageHeight(IntPtr page);

        [DllImport("pdfium.dll",
             SetLastError = true,
             CallingConvention = CallingConvention.Cdecl,
             CharSet = CharSet.Ansi,
             EntryPoint = "FPDFBitmap_GetBuffer")]
        public static extern IntPtr FPDFBitmap_GetBuffer(IntPtr bitmap);

        [DllImport("pdfium.dll",
           SetLastError = true,
           CallingConvention = CallingConvention.Cdecl,
           CharSet = CharSet.Ansi,
           EntryPoint = "FPDFPage_GetObject")]
        public static extern IntPtr FPDFPage_GetObject(IntPtr page, int index);

        [DllImport("gdi32.dll", EntryPoint = "CreateCompatibleDC", SetLastError = true)]
        public static extern IntPtr CreateCompatibleDC([In] IntPtr hdc);

        [DllImport("gdi32.dll", EntryPoint = "CreateCompatibleBitmap")]
        static extern IntPtr CreateCompatibleBitmap([In] IntPtr hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll", EntryPoint = "SelectObject")]
        public static extern IntPtr SelectObject([In] IntPtr hdc, [In] IntPtr hgdiobj);

        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("gdi32.dll", EntryPoint = "CreateSolidBrush", SetLastError = true)]
        public static extern IntPtr CreateSolidBrush(int crColor);

        [DllImport("gdi32.dll")]
        public static extern bool FillRgn(IntPtr hdc, IntPtr hrgn, IntPtr hbr);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);
    }
}
