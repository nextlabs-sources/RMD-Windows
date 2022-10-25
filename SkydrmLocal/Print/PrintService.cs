using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Print.utils;

using System.Diagnostics;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Windows.Threading;
using System.ComponentModel;
using System.IO;
using System.IO.Packaging;
using System.Windows.Xps.Packaging;
using System.Drawing;
using System.Drawing.Printing;
using System.Management;
using System.Threading;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using static Print.PrintApplication;
using SkydrmLocal.rmc.sdk;

namespace Print
{
    public class PrintService
    {
        private PrintApplication mPrintApplication;

        private StartParameters mPrintParameters;

        private string mDecryptedFilePath = string.Empty;

        public delegate void CallBackDelegate();

        private CallBackDelegate CallBack = null;

        private NxlFileHandler mNxlFileHandler;

        private FileHandler mFileHandler;

        public PrintService(StartParameters startParameters, CallBackDelegate callBackDelegate)
        {
            this.mPrintApplication = (PrintApplication)PrintApplication.Current;
            this.mPrintParameters = startParameters;
            this.CallBack = callBackDelegate;
            this.mNxlFileHandler = new NxlFileHandler(mPrintParameters.NxlFilePath);
        }

        public void ExecutePrintInBackground()
        {
            try
            {
                this.mPrintApplication.Log.Info("\t\tExecutePrintInBackground\r\n");
                NxlFileFingerPrint nxlFileFingerPrint;
                if (!mNxlFileHandler.GetFingerPrint(out nxlFileFingerPrint))
                {
                    CommonUtils.MessageBox_("You have no permission to access the file.");
                    return;
                }

                if (!nxlFileFingerPrint.HasRight(FileRights.RIGHT_PRINT))
                {
                    CommonUtils.MessageBox_("have no print rights.");
                    return;
                }

                AppConfig appConfig = mPrintApplication.Appconfig;

                try
                {
                    mDecryptedFilePath = CommonUtils.GenerateDecryptFilePath(appConfig.RPM_FolderPath, mNxlFileHandler.NxlFilePath, false);
                }
                catch (Exception ex)
                {
                    CommonUtils.MessageBox_("System internal error. Please contact your system administrator for further help. Detail error: " + ex.ToString());
                    return;
                }

                if (!mNxlFileHandler.Decrypt(mDecryptedFilePath))
                {
                    CommonUtils.MessageBox_("Decrypt failed");
                    return;
                }

                //StringBuilder wmText = new StringBuilder();
                //CommonUtils.ConvertWatermark2DisplayStyle(nxlFileFingerPrint.adhocWatermark, mPrintApplication.Session.User.Email, ref wmText);
                FileHandlerFactory fileHandlerFactory = new FileHandlerFactoryByExtention();
                mFileHandler = fileHandlerFactory.CreateInstance(mDecryptedFilePath);
                mFileHandler.Watermark(GetWaterMark(nxlFileFingerPrint));
                ShowPrintDialog(mFileHandler.GetPageCount());
            }
            catch (Exception ex)
            {
                //CommonUtils.MessageBox_("System internal error. Please contact your system administrator for further help. Detail error: " + ex.ToString());
                mPrintApplication.Log.Error(ex);
            }
            finally
            {
                Dispose();
                //Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate ()
                //{
                     CallBack();
                //}));
            }
        }

        private string GetWaterMark(NxlFileFingerPrint fp)
        {
            string watermarkStr = string.Empty;
            if (fp.isByCentrolPolicy)
            {
                watermarkStr = GetWatermarkFroCentrolPolicyFile(fp.localPath,true);
            }
            else
            {
                StringBuilder wmText = new StringBuilder();
                CommonUtils.ConvertWatermark2DisplayStyle(fp.adhocWatermark, mPrintApplication.Session.User.Email, ref wmText);
                watermarkStr = wmText.ToString();
            }
            return watermarkStr;
        }

        private string GetWatermarkFroCentrolPolicyFile(string nxlFilePath,bool b)
        {
            string watermarkStr = string.Empty;
            Dictionary<FileRights, List<WaterMarkInfo>> rightsAndWatermarks;
            try
            {
                mPrintApplication.Session.User.EvaulateNxlFileRights(nxlFilePath, out rightsAndWatermarks, b);
                foreach (var v in rightsAndWatermarks)
                {
                    List<WaterMarkInfo> waterMarkInfoList = v.Value;
                    if (waterMarkInfoList == null)
                    {
                        continue;
                    }
                    foreach (var w in waterMarkInfoList)
                    {
                        watermarkStr = w.text;
                        if (!string.IsNullOrEmpty(watermarkStr))
                        {
                            break;
                        }
                    }
                    if (!string.IsNullOrEmpty(watermarkStr))
                    {
                        break;
                    }
                }

                return watermarkStr;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private int GetfinalValue(Int32 param1, Int32 param2)
        {
            Int32 result = 0;

            double integraValue = Math.Floor(Convert.ToDouble(param1) * (Convert.ToDouble(param2) / Convert.ToDouble(param1)));

            result = Convert.ToInt32(integraValue);

            return result;
        }

        private int GetFinallyMarginValue(Int32 param1, Int32 param2)
        {
            return Convert.ToInt32(Math.Round(Convert.ToDouble(param1 - param2) / 2));
        }

        private void DrawImage(System.Drawing.Image image, PrintPageEventArgs eventArgs)
        {
            PaperSize rectangle = eventArgs.PageSettings.PaperSize;

            int paperWith = rectangle.Width;

            int paperHeight = rectangle.Height;

            int imageWith = image.Width;

            int imageHeight = image.Height;

            int finalWith = imageWith > paperWith ? GetfinalValue(imageWith, paperWith) : imageWith;

            int finaHeight = imageHeight > paperHeight ? GetfinalValue(imageHeight, paperHeight) : imageHeight;

            int marginLeft = imageWith < paperWith ? GetFinallyMarginValue(paperWith, imageWith) : 0;

            int marginTop = imageHeight < paperHeight ? GetFinallyMarginValue(paperHeight, imageHeight) : 0;

            eventArgs.Graphics.DrawImage(image, new System.Drawing.Rectangle(marginLeft, marginTop, finalWith, finaHeight));
        }


        private void ShowPrintDialog(int needPrintPageCount)
        {
            if (needPrintPageCount<=0)
            {
                return;
            }
            try
            {
                using (var printDlg = new System.Windows.Forms.PrintDialog())
                {
                    printDlg.Document = new PrintDocument();

                    // do some printing settings.
                    printDlg.AllowSelection = true;
                    printDlg.AllowSomePages = true;
                    printDlg.UseEXDialog = true;
                    int i = 0;

                    // register print handler
                    printDlg.Document.PrintPage += delegate (object obj, PrintPageEventArgs eventArgs)
                    {
                        try
                        {
                            if (eventArgs.Cancel)
                            {
                                eventArgs.HasMorePages = false;
                                return;
                            }

                            System.Drawing.Image image = mFileHandler.GetImage(i);

                            DrawImage(image, eventArgs);

                            image.Dispose();

                            GC.Collect();

                        }

                        finally
                        {

                            i++;

                            if (i < needPrintPageCount)
                            {
                                eventArgs.HasMorePages = true;
                            }
                            else
                            {
                                eventArgs.HasMorePages = false;
                            }
                        }
                    };

                    printDlg.Document.EndPrint += delegate (object sender, PrintEventArgs e)
                    {
                        mPrintApplication.Session.User.AddLog(mPrintParameters.NxlFilePath, NxlOpLog.Print, true);
                    };
                    if (mPrintParameters.IntPtrOfWindowOwner == -1)
                    {
                        if (printDlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                           printDlg.Document.DocumentName += ":"+ mPrintParameters.NxlFilePath;
                           printDlg.Document.Print(); // will callback print handler  
                        }
                    }
                    else
                    {
                        if (printDlg.ShowDialog(new WindowWrapper(new IntPtr(mPrintParameters.IntPtrOfWindowOwner))) == System.Windows.Forms.DialogResult.OK)
                        {
                            printDlg.Document.DocumentName += ":" + mPrintParameters.NxlFilePath;
                            printDlg.Document.Print(); // will callback print handler              
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                mPrintApplication.Session.User.AddLog(mPrintParameters.NxlFilePath, NxlOpLog.Print, false);
                throw ex;
                //Detail error :System.ComponentModel.Win32Exception (0x80004005):A StartDocPrinter call was not issued
            }

        }

        private void Dispose()
        {
            if (null != mNxlFileHandler)
            {
                mNxlFileHandler.DeleteFile();
            }

            if (null != mFileHandler)
            {
                mFileHandler.Release();
            }
        }

        /// <summary>
        /// Wrapper class so that we can return an IWin32Window given a hwnd
        /// </summary>
        public class WindowWrapper : System.Windows.Forms.IWin32Window
        {
            public WindowWrapper(IntPtr handle)
            {
                _hwnd = handle;
            }

            public IntPtr Handle
            {
                get { return _hwnd; }
            }

            private IntPtr _hwnd;
        }

    }
}
