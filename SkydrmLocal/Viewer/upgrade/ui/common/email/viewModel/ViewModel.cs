using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using Viewer.upgrade.file.basic.utils;
using Viewer.upgrade.ui.common.email.view;
using Viewer.upgrade.utils.overlay.utils;
using Viewer.upgrade.utils.overlay.windowOverlay;

namespace Viewer.upgrade.ui.common.email.viewModel
{
    public class ViewModel : ISensor
    {
        private string mFilePath;
        private EmailPage mEmailPage;
        private WatermarkInfo mWatermarkInfo;
        private WindowOverlay mOverlay;
        private int mPrintTextIndex;

        //Convert the unit used by the .NET framework (1/100 inch) 
        //and the unit used by Win32 API calls (twips 1/1440 inch)
        private const double AnInch = 14.4;

        public event Action<Exception> OnUnhandledExceptionOccurrence;
        public event Action OnLoadFileSucceed;
        public event Action<bool> EndPrint;
        public event Action<System.Windows.Forms.PrintDialog> BeforePrint;

        public ViewModel(string filePath, EmailPage emailPage)
        {
            this.mFilePath = filePath;
            this.mEmailPage = emailPage;
        }

        public void Page_Loaded()
        {
            try
            {
                Stream messageStream = File.Open(mFilePath, FileMode.Open, FileAccess.Read);
                OutlookStorage.Message message = new OutlookStorage.Message(messageStream);
                messageStream.Close();
                LoadRtf(message.BodyRTF);
                message.Dispose();
                AttachWatermark();
                OnLoadFileSucceed?.Invoke();
            }
            catch (Exception ex)
            {
                OnUnhandledExceptionOccurrence?.Invoke(ex);
            }
        }

        public void LoadRtf(string bodyRTF)
        {
            try
            {
                mEmailPage.RichTextBox.ReadOnly = true;
                mEmailPage.RichTextBox.Visible = true;
                mEmailPage.RichTextBox.EnableAutoDragDrop = false;
                mEmailPage.RichTextBox.ShortcutsEnabled = false;
                mEmailPage.RichTextBox.Rtf = bodyRTF;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public void Watermark(WatermarkInfo watermarkInfo)
        {
            mWatermarkInfo = watermarkInfo;
        }

        public void AttachWatermark()
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
                    mEmailPage.Host_Grid.Children.Add(mOverlay);
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
                    printDlg.Document.PrintPage += delegate (object obj, PrintPageEventArgs eventArgs)
                    {
                        PrintPage(obj, eventArgs);
                    };

                    //printDlg.Document.EndPrint += delegate (object sender, PrintEventArgs e) 
                    //{
                    //    EndPrint?.Invoke(true);
                    //};

                    // Do some print setting
                    printDlg.AllowSelection = true;
                    printDlg.AllowSomePages = true;

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

        private void BeginPrint(object sender, PrintEventArgs eventArgs)
        {
            mPrintTextIndex = 0;
        }

        private void PrintPage(object sender, PrintPageEventArgs e)
        {
           // TextRange txtR = new TextRange(mRichTextPage.RichTextBox.Document.ContentStart, mRichTextPage.RichTextBox.Document.ContentEnd);
           // mEmailPage.RtbPrint.Text = txtR.Text;
            // Print the content of RichTextBox. Store the last character printed.
            mPrintTextIndex =PrintTest(mPrintTextIndex, mEmailPage.RichTextBox.TextLength, e);
            PrintOverlay(e.PageSettings.PaperSize.Width, e.PageSettings.PaperSize.Height, e);

            if (mPrintTextIndex < mEmailPage.RichTextBox.TextLength)
            {
                e.HasMorePages = true;
            }
            else
            {
                e.HasMorePages = false;
            }
        }

        public void PrintOverlay(int width, int height, PrintPageEventArgs e)
        {
            if (null != mWatermarkInfo)
            {
                OverlayUtils.DrawOverlayByGraphics(e.Graphics, width, height, mWatermarkInfo);
            }
        }

        // Render the contents of the RichTextBox for printing
        // Return the last character printed + 1 (printing start from this point for next page)
        public int PrintTest(int charFrom, int charTo, PrintPageEventArgs e)
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
            res = SendMessage(mEmailPage.RichTextBox.Handle, EM_FORMATRANGE, wparam, lparam);

            //Free the block of memory allocated
            Marshal.FreeCoTaskMem(lparam);

            //Release the device context handle obtained by a previous call
            e.Graphics.ReleaseHdc(hdc);

            //Return last + 1 character printer
            return res.ToInt32();
        }


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

    }
}
