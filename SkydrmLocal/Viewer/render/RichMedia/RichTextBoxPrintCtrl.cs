using System;

using System.Drawing;
using System.Runtime.InteropServices;
using System.Drawing.Printing;
using Viewer.viewer;
using System.Windows;
using System.Windows.Media;
using Viewer.overlay;
using System.Diagnostics;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Documents;
using System.Text;
using Viewer.utils;

namespace Viewer.render.RichMedia
{
    public class RichTextBoxPrintCtrl : RichTextBox, IOverlay
    {

        //Convert the unit used by the .NET framework (1/100 inch) 
        //and the unit used by Win32 API calls (twips 1/1440 inch)
        private const double AnInch = 14.4;
        private Overlay Overlay = new Overlay();
        private WatermarkInfo mWatermarkInfo { get; set; }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CHARRANGE
        {
            public int cpMin;         //First character of range (0 for start of doc)
            public int cpMax;         //Last character of range (-1 for end of doc)
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct FORMATRANGE
        {
            public IntPtr hdc;             //Actual DC to draw on
            public IntPtr hdcTarget;       //Target DC for determining text formatting
            public RECT rc;                //Region of the DC to draw to (in twips)
            public RECT rcPage;            //Region of the whole DC (page size) (in twips)
            public CHARRANGE chrg;         //Range of text to draw (see earlier declaration)
        }

        private const int WM_USER = 0x0400;
        private const int EM_FORMATRANGE = WM_USER + 57;
        private const int WM_PAINT = 0xF;

        [DllImport("USER32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);

        // Render the contents of the RichTextBox for printing
        // Return the last character printed + 1 (printing start from this point for next page)
        public int Print(int charFrom, int charTo, PrintPageEventArgs e)
        {
            //Calculate the area to render and print
            RECT rectToPrint;
            rectToPrint.Top = (int)(e.MarginBounds.Top * AnInch);
            rectToPrint.Bottom = (int)(e.MarginBounds.Bottom * AnInch);
            rectToPrint.Left = (int)(e.MarginBounds.Left * AnInch);
            rectToPrint.Right = (int)(e.MarginBounds.Right * AnInch);
            
            //Calculate the size of the page
            RECT rectPage;
            rectPage.Top = (int)(e.PageBounds.Top * AnInch);
            rectPage.Bottom = (int)(e.PageBounds.Bottom * AnInch);
            rectPage.Left = (int)(e.PageBounds.Left * AnInch);
            rectPage.Right = (int)(e.PageBounds.Right * AnInch);
            IntPtr hdc = e.Graphics.GetHdc();

            FORMATRANGE fmtRange;
            fmtRange.chrg.cpMax = charTo; //Indicate character from to character to 
            fmtRange.chrg.cpMin = charFrom;
            fmtRange.hdc = hdc;                    //Use the same DC for measuring and rendering
            fmtRange.hdcTarget = hdc;              //Point at printer hDC
            fmtRange.rc = rectToPrint;             //Indicate the area on page to print
            fmtRange.rcPage = rectPage;            //Indicate size of page
            
            IntPtr res = IntPtr.Zero;

            IntPtr wparam = IntPtr.Zero;
            wparam = new IntPtr(1);

            //Get the pointer to the FORMATRANGE structure in memory
            IntPtr lparam = IntPtr.Zero;
            lparam = Marshal.AllocCoTaskMem(Marshal.SizeOf(fmtRange));
            Marshal.StructureToPtr(fmtRange, lparam, false);

            //Send the rendered data for printing 
            res = SendMessage(Handle, EM_FORMATRANGE, wparam, lparam);

            //Free the block of memory allocated
            Marshal.FreeCoTaskMem(lparam);

            //Release the device context handle obtained by a previous call
            e.Graphics.ReleaseHdc(hdc);

            // Print overlay
            PrintOverlay(e.PageSettings.PaperSize.Width, e.PageSettings.PaperSize.Height, e);

            //Return last + 1 character printer
            return res.ToInt32();
        }

        public void PrintImage(Image draw,PrintPageEventArgs e)
        {
            e.Graphics.DrawImage(draw,0,0);
            // Print overlay
            PrintOverlay(e.PageSettings.PaperSize.Width, e.PageSettings.PaperSize.Height, e);
        }

        public void Initialize(FrameworkElement visual , WatermarkInfo watermarkInfo)
        {
            this.mWatermarkInfo = watermarkInfo;
            if (IsAttach())
            {
                if (!Overlay.Initialze)
                {
                    InitializeOverlay(visual, watermarkInfo);
                }
                AttachOverlay(visual.ActualWidth, visual.ActualHeight);
            }
        }

        public void OnSizeChanged(FrameworkElement visual , WatermarkInfo watermarkInfo)
        {
            if (IsAttach())
            {
                if (!Overlay.Initialze)
                {
                    InitializeOverlay(visual, watermarkInfo);
                }
                else
                {
                    Overlay.OnOverlayChange();
                }
            }
        }

        private void InitializeOverlay(FrameworkElement visual, WatermarkInfo WatermarkInfo)
        {
            Overlay.Initialize(visual, WatermarkInfo);
        }

        private void AttachOverlay(double width, double height)
        {
            if (IsAttach())
            {
                Overlay.ParentLayer.Add((Adorner)Attach(width, height));
            }
        }

        public void PrintOverlay(int width, int height, PrintPageEventArgs e)
        {
            if (IsAttach())
            {
                Overlay.Graphics = e.Graphics;
                Overlay.DrawOverlayByGraphics(width, height);
            }
        }

        
        public bool IsAttach()
        {
            return RenderHelper.IsAttachOverlay(mWatermarkInfo);
        }

        public UIElement Attach(double width, double height)
        {
            return Overlay.CreateOverlayInAdornerLayer(width, height);
        }
    }
}
