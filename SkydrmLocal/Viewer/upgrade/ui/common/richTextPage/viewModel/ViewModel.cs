using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using Alphaleonis.Win32.Filesystem;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Xps.Packaging;
using Viewer.upgrade.file.basic.utils;
using Viewer.upgrade.ui.common.richTextPage.view;
using Viewer.upgrade.utils;
using Viewer.upgrade.utils.overlay.utils;
using Viewer.upgrade.utils.overlay.windowOverlay;

namespace Viewer.upgrade.ui.common.richTextPage.viewModel
{
    public class ViewModel : ISensor
    {
        private string mFilePath;
        private RichTextPage mRichTextPage;
        private WatermarkInfo mWatermarkInfo;
        private WindowOverlay mOverlay;
        private int mPrintTextIndex;

        public event Action<Exception> OnUnhandledExceptionOccurrence;
        public event Action OnLoadFileSucceed;
        public event Action<bool> EndPrint;
        public event Action<System.Windows.Forms.PrintDialog> BeforePrint;

        public ViewModel(string filePath, RichTextPage richTextPage)
        {
            this.mFilePath = filePath;
            this.mRichTextPage = richTextPage;
        }

        public void Page_Loaded()
        {
            try
            {
                LoadText();
                AttachWatermark();
                OnLoadFileSucceed?.Invoke();
            }
            catch (Exception ex)
            {
                OnUnhandledExceptionOccurrence?.Invoke(ex);
            }
        }

        public void LoadText()
        {
            mRichTextPage.RichTextBox.Document.Blocks.Clear();
            try
            {
                //fileStream = new FileStream(NxlConverterResult.TmpPath,
                //                  FileMode.Open,
                //                  FileAccess.Read,
                //                  FileShare.None, 4096,
                //                  FileOptions.None);
               
                string edtext;
                Encoding edres =ToolKit.DetectTextEncoding(mFilePath, out edtext, 1000);
                using (System.IO.StreamReader streamReader = new System.IO.StreamReader(mFilePath, edres, true))
                {
                    Paragraph paragraph = new Paragraph();
                    Run run = new Run(streamReader.ReadToEnd());
                    paragraph.Inlines.Add(run);
                    mRichTextPage.RichTextBox.Document.Blocks.Add(paragraph);
                }
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
                    OverlayUtils.DrawWatermark(mWatermarkInfo,ref overlayCanvas);
                    overlayCanvas.Margin = new Thickness(0, 0, 0, 0);
                    mOverlay.OverlayContent = overlayCanvas;
                    mRichTextPage.Host_Grid.Children.Add(mOverlay);
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
            TextRange txtR = new TextRange(mRichTextPage.RichTextBox.Document.ContentStart, mRichTextPage.RichTextBox.Document.ContentEnd);
            mRichTextPage.RtbPrint.Text = txtR.Text;
            // Print the content of RichTextBox. Store the last character printed.
            mPrintTextIndex = mRichTextPage.RtbPrint.PrintTest(mPrintTextIndex, mRichTextPage.RtbPrint.TextLength, e);
            PrintOverlay(e.PageSettings.PaperSize.Width, e.PageSettings.PaperSize.Height, e);
          
            if (mPrintTextIndex < mRichTextPage.RtbPrint.TextLength)
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

        //private void BeginPrint(object sender, PrintEventArgs eventArgs)
        //{
        //    mIndex = 0;
        //}
        ///// <summary>
        ///// Print content
        ///// </summary>
        //private void PrintPage(object sender, PrintPageEventArgs e)
        //{
        //    PrintText(e);
        //    PrintOverlay(e.PageSettings.PaperSize.Width, e.PageSettings.PaperSize.Height, e);
        //}

        //public void PrintText(PrintPageEventArgs e)
        //{
        ////TextRange txtR = new TextRange(mRichTextPage.RichTextBox.Document.ContentStart, mRichTextPage.RichTextBox.Document.ContentEnd);
        //// string text = txtR.Text;

        //string edtext;
        //Encoding edres = ToolKit.DetectTextEncoding(mFilePath, out edtext, 1000);
        //using (StreamReader streamReader = new StreamReader(mFilePath, edres, true))
        //{
        //    Paragraph paragraph = new Paragraph();
        //    Run run = new Run(streamReader.ReadToEnd());
        //    paragraph.Inlines.Add(run);
        //    mRichTextPage.RichTextBox.Document.Blocks.Add(paragraph);
        //}

        ////// Print the content of RichTextBox. Store the last character printed.
        ////checkPrint = RtbPrint.Print(checkPrint, RtbPrint.TextLength, e);
        //// Check for more pages
        //if (checkPrint < RtbPrint.TextLength)
        //{
        //    e.HasMorePages = true;
        //}
        //else
        //{
        //    e.HasMorePages = false;
        //}
        //}

        //public void Print2()
        //{
        //    TextRange txtR = new TextRange(mRichTextPage.RichTextBox.Document.ContentStart, mRichTextPage.RichTextBox.Document.ContentEnd);
        //    string text = txtR.Text;
        //    PrintDialog printDialog = new PrintDialog();
        //    if (printDialog.ShowDialog() == true)
        //    {
        //        printDialog.PrintVisual(mRichTextPage.Host_Grid,"");
        //    }
        //}
    }
}
