using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using Viewer.upgrade.file.basic;
using Viewer.upgrade.file.basic.utils;
using Viewer.upgrade.ui.common.edgeWebView2Page.view;
using Viewer.upgrade.ui.common.overlayWindow.view;
using Viewer.upgrade.utils.overlay.utils;
using Viewer.upgrade.utils.overlay.windowOverlay;

namespace Viewer.upgrade.ui.common.edgeWebView2Page.viewModel
{
    public class ViewModel : ISensor
    {
        private WatermarkInfo mWatermarkInfo;
        private string mFilePath;
        private EdgeWebView2Page mEdgeWebView2Page;
        private WindowOverlay mOverlay;
        public event Action<Exception> OnUnhandledExceptionOccurrence;
        public event Action OnLoadFileSucceed;
        private Window mParentWindow;
        private INxlFile mNxlFile;
        private ViewerApp mViewerApp;

        public INxlFile NxlFile
        {
            get { return mNxlFile; }
            set { mNxlFile = value; }
        }

        public ViewModel(string filePath, EdgeWebView2Page edgeWebView2Page)
        {
            mFilePath = filePath;
            mEdgeWebView2Page = edgeWebView2Page;
            mViewerApp = (ViewerApp)ViewerApp.Current;
  
        }

        public void Watermark(WatermarkInfo watermarkInfo)
        {
            mWatermarkInfo = watermarkInfo;
        }

        public void Page_Unload()
        {

        }

        public void myKeyEventHandler(object sender, KeyEventArgs e){
            e.Handled = true;
        }

        public async void Page_Loaded()
        {
            try
            {
                mParentWindow = Window.GetWindow(mEdgeWebView2Page);
                string userDataFolder = mViewerApp.WorkingFolder;
                var env = await CoreWebView2Environment.CreateAsync(@"\Microsoft.WebView2.FixedVersionRuntime.104.0.1293.47.x64", userDataFolder);
                await mEdgeWebView2Page.myEdgeWebView.EnsureCoreWebView2Async(env);

                mEdgeWebView2Page.myEdgeWebView.CoreWebView2.Settings.HiddenPdfToolbarItems =
                                                    CoreWebView2PdfToolbarItems.Save |
                                                    CoreWebView2PdfToolbarItems.SaveAs |
                                                    CoreWebView2PdfToolbarItems.Print;
                mEdgeWebView2Page.myEdgeWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                mEdgeWebView2Page.myEdgeWebView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
                mEdgeWebView2Page.myEdgeWebView.AllowDrop = false;
                mEdgeWebView2Page.myEdgeWebView.AllowExternalDrop = false;
                mEdgeWebView2Page.myEdgeWebView.KeyDown += new KeyEventHandler(myKeyEventHandler);

                mEdgeWebView2Page.myEdgeWebView.Source = new Uri(mFilePath);
            
                if (IsAttachWatermark())
                {
                   AttachWatermark();
                }
      
                OnLoadFileSucceed?.Invoke();
            }
            catch (Exception ex)
            {
                OnUnhandledExceptionOccurrence?.Invoke(ex);
            }
        }

        //private async void LoadFile()
        //{
        //    try
        //    {
        //        ViewerApp viewerApp = (ViewerApp)ViewerApp.Current;
        //        string userDataFolder = viewerApp.WorkingFolder;
        //        var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
        //        await mEdgeWebView2Page.myEdgeWebView.EnsureCoreWebView2Async(env);

        //        mEdgeWebView2Page.myEdgeWebView.CoreWebView2.Settings.HiddenPdfToolbarItems =
        //           CoreWebView2PdfToolbarItems.Save |
        //           CoreWebView2PdfToolbarItems.SaveAs |
        //           CoreWebView2PdfToolbarItems.Print;
        //        mEdgeWebView2Page.myEdgeWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        //        mEdgeWebView2Page.myEdgeWebView.Source = new Uri(mFilePath);

        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //        //OnUnhandledExceptionOccurrence?.Invoke(ex);
        //    }
        //}

        private bool IsAttachWatermark()
        {
            if (null != mWatermarkInfo)
            {
                return true;
            }
            return false;
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
                    mEdgeWebView2Page.Host.Children.Add(mOverlay);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public void Print()
        {
            if (null == mParentWindow)
            {
                return;
            }
            IntPtr OverLay_hwnd = new WindowInteropHelper(mParentWindow).Handle;
            Process mProc = new Process();
            mProc.StartInfo.FileName = "nxrmprint.exe";
            mProc.StartInfo.WorkingDirectory = System.Windows.Forms.Application.StartupPath;
            mProc.StartInfo.CreateNoWindow = true;
            mProc.StartInfo.Arguments += "-print";
            mProc.StartInfo.Arguments += " ";
            mProc.StartInfo.Arguments += OverLay_hwnd.ToInt32();
            mProc.StartInfo.Arguments += " ";
            mProc.StartInfo.Arguments += "\"" + NxlFile.FilePath + "\"";
            mProc.Start();
        }

    }
}
