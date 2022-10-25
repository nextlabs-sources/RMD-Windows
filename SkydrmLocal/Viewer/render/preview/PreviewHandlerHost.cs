// Preview Handlers Revisted
// Bradley Smith - 2010/09/17, updated 2013/10/14

using System;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.Win32;
using System.IO;
using System.ComponentModel;
using System.Drawing;
using SkydrmLocal.rmc.drive;
using SkydrmLocal.rmc.sdk;
using Viewer.utils;

namespace Viewer.render.preview
{
    public delegate void RenderEventHandler(bool isSuccess, Exception ex);

    /// <summary>
    /// A Windows Forms host for Preview Handlers.
    /// </summary>
    public class PreviewHandlerHost : Control{
        private string mErrorMessage;
        private Color mBorderColor;
        private BorderStyle mBorderStyle;
        private PreviewHandler mPreviewHandler;
        public  event RenderEventHandler Render;

        /// <summary>
        /// Gets or sets the error message displayed when a problem occurs with a preview handler.
        /// </summary>
        private string ErrorMessage {
            get { return mErrorMessage; }
            set {
                mErrorMessage = value;
                Invalidate();	// repaint the control
            }
        }
        ///// <summary>
        ///// Gets the GUID of the current preview handler.
        ///// </summary>
        //[Browsable(false), ReadOnly(true)]
        //public Guid CurrentPreviewHandler {
        //    get {
        //        return mCurrentPreviewHandlerGUID;
        //    }
        //}
        /// <summary>
        /// Gets or sets the background colour of this PreviewHandlerHost.
        /// </summary>
        [DefaultValue(typeof(Color), "White")]
        public override System.Drawing.Color BackColor {
            get {
                return base.BackColor;
            }
            set {
                base.BackColor = value;
            }
        }
        /// <summary>
        /// Gets or sets the colour of the border to draw around the control.
        /// </summary>
        [DefaultValue(typeof(Color), "ControlDark")]
        public Color BorderColor {
            get {
                return mBorderColor;
            }
            set {
                mBorderColor = value;
                Invalidate();
            }
        }
        /// <summary>
        /// Gets or sets the style of the border to draw around the control.
        /// </summary>
        [DefaultValue(BorderStyle.FixedSingle)]
        public BorderStyle BorderStyle {
            get {
                return mBorderStyle;
            }
            set {
                mBorderStyle = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Initialialises a new instance of the PreviewHandlerHost class.
        /// </summary>
        public PreviewHandlerHost() : base() {         
           // mCurrentPreviewHandlerGUID = Guid.Empty;
            BackColor = Color.White;
            mBorderColor = System.Drawing.SystemColors.ControlDark;
            mBorderStyle = BorderStyle.FixedSingle;
            // Size = new System.Drawing.Size(320, 240);
            //Size = new System.Drawing.Size( ActualWidth , ActualHeight );
            // display default error message (no file)
            ErrorMessage = string.Empty;
            // enable transparency
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.UserPaint, true);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the PreviewHandlerHost and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing) {
        
            base.Dispose(disposing);
        }

        /// <summary>
        /// Paints the error message text on the PreviewHandlerHost control.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);

            if (mErrorMessage != String.Empty) {
                // paint the error message
                TextRenderer.DrawText(
                    e.Graphics,
                    mErrorMessage,
                    Font,
                    ClientRectangle,
                    ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis
                );
            }

            // border
            if (mBorderStyle == BorderStyle.Fixed3D) {
                ControlPaint.DrawBorder(e.Graphics, ClientRectangle, mBorderColor, ButtonBorderStyle.Inset);
            }
            else if (mBorderStyle == BorderStyle.FixedSingle) {
                using (Pen pen = new Pen(mBorderColor)) {
                    Rectangle rect = ClientRectangle;
                    rect.Width--;
                    rect.Height--;
                    e.Graphics.DrawRectangle(pen, rect);
                }
            }
        }

        /// <summary>
        /// Resizes the hosted preview handler when this PreviewHandlerHost is resized.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnResize(EventArgs e) {
            base.OnResize(e);
            if (null == mPreviewHandler)
            {
                return;
            }
            Rectangle r = ClientRectangle;
            mPreviewHandler.SetRect(ref r);
        }

        public bool SpeedUpOpenOfficeFile(string filePath, PreviewHandler previewHandler,log4net.ILog log)
        {
            log.Info("\t\t SpeedUpOpenOfficeFile: " + filePath + "\r\n");
            mPreviewHandler = previewHandler;

            if (Guid.Empty == mPreviewHandler.CurrentPreviewHandlerGUID)
            {
                ErrorMessage = "No preview available.";
                Render?.Invoke(false, new Exception(ErrorMessage));
                return false;
            }

            if (null == mPreviewHandler.CurrentPreviewHandler)
            {
                ErrorMessage = "This file can't be viewed";
                Render?.Invoke(false, new Exception(ErrorMessage));
                return false;
            }

            try
            {
                mPreviewHandler.DoPreview(filePath, Handle, ClientRectangle);
                // by sometimes, COM Preivew handler may calulate a wrong size for the client rect
                // only way we need is to refresh and update the handler'host window
                this.Refresh();
                this.Update();
                Render?.Invoke(true, null);
                return true;
            }
            catch (System.Runtime.InteropServices.COMException comex)
            {
                ErrorMessage = "This file can't be viewed";
                Render?.Invoke(false, new Exception(ErrorMessage));
                log.Error(comex.Message, comex);
                return false;
            }
            catch (Exception ex)
            {
                ErrorMessage = "This file can't be viewed";
                Render?.Invoke(false, ex);
                log.Error(ex.Message, ex);
                return false;
            }
        }

        /// <summary>
        /// Opens the specified file using the appropriate preview handler and displays the result in this PreviewHandlerHost.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public bool Open(string filePath , PreviewHandler previewHandler, Session session, log4net.ILog log) {
            log.Info("\t\t PreviewHandlerHost Open: " + filePath + "\r\n");
            //  UnloadPreviewHandler();
            mPreviewHandler = previewHandler;

            if (String.IsNullOrEmpty(filePath)) {
                ErrorMessage = "No file loaded.";
                Render?.Invoke(false,new Exception(ErrorMessage));
                return false;
            }

            if (!mPreviewHandler.Initialize(filePath, log))
            {
                ErrorMessage = "No preview available.";
                Render?.Invoke(false, new Exception(ErrorMessage));
                return false;
            }

            try
            {
                string ext = Path.GetExtension(filePath);
                FileType fileType = CommonUtils.GetFileTypeByFileExtension(ext);
                CommonUtils.ProcessRegister(session, fileType, log);

                mPreviewHandler.DoPreview(filePath, Handle, ClientRectangle);
                // by sometimes, COM Preivew handler may calulate a wrong size for the client rect
                // only way we need is to refresh and update the handler'host window
                this.Refresh();
                this.Update();
                Render?.Invoke(true, null);
                return true;
            }
            catch (System.Runtime.InteropServices.COMException comex)
            {
                ErrorMessage = "This file can't be viewed";
                Render?.Invoke(false, new Exception(ErrorMessage));
                log.Error(comex.Message, comex);
                return false;
            }
            catch (Exception ex)
            {
                ErrorMessage = "This file can't be viewed";
                Render?.Invoke(false, new Exception(ErrorMessage));
                log.Error(ex.Message, ex);
                return false;
            }
        }

        ///// <summary>
        ///// Returns the GUID of the preview handler associated with the specified file.
        ///// </summary>
        ///// <param name="filename"></param>
        ///// <returns></returns>
        //public Guid GetPreviewHandlerGUID(string filename)
        //{
        //    // open the registry key corresponding to the file extension
        //    RegistryKey ext = Registry.ClassesRoot.OpenSubKey(Path.GetExtension(filename));
        //    if (ext != null)
        //    {
        //        // open the key that indicates the GUID of the preview handler type
        //        RegistryKey test = ext.OpenSubKey("shellex\\{8895b1c6-b41f-4c1c-a562-0d564250836f}");
        //        if (test != null) return new Guid(Convert.ToString(test.GetValue(null)));

        //        // sometimes preview handlers are declared on key for the class
        //        string className = Convert.ToString(ext.GetValue(null));
        //        if (className != null)
        //        {
        //            test = Registry.ClassesRoot.OpenSubKey(className + "\\shellex\\{8895b1c6-b41f-4c1c-a562-0d564250836f}");
        //            if (test != null) return new Guid(Convert.ToString(test.GetValue(null)));
        //        }
        //    }

        //    return Guid.Empty;
        //}

        //private static IntPtr MakeLParam(int LoWord, int HiWord)
        //{
        //    int i = (HiWord << 16) | (LoWord & 0xffff);
        //    return new IntPtr(i);
        //}

        ///// <summary>
        ///// Opens the specified stream using the preview handler COM type with the provided GUID and displays the result in this PreviewHandlerHost.
        ///// </summary>
        ///// <param name="stream"></param>
        ///// <param name="previewHandler"></param>
        ///// <returns></returns>
        //public bool Open(Stream stream, PreviewHandler previewHandler, Session session, log4net.ILog log) {
        //    // UnloadPreviewHandler();

        //    ErrorMessage = string.Empty;
        //    mPreviewHandler = previewHandler;

        //    if (stream == null) {
        //        ErrorMessage = "No file loaded.";
        //        Render?.Invoke(false, new Exception(ErrorMessage));
        //        return false;
        //    }

        //    if (Guid.Empty == mPreviewHandler.CurrentPreviewHandlerGUID)
        //    {
        //        ErrorMessage = "No preview available.";
        //        Render?.Invoke(false, new Exception(ErrorMessage));
        //    }

        //    if (null == mPreviewHandler.CurrentPreviewHandler)
        //    {
        //        ErrorMessage = "This file can't be viewed";
        //        Render?.Invoke(false, new Exception(ErrorMessage));
        //    }

        //   // mPreviewHandler.DoPreview();

        //    return false;
        //}

        ///// <summary>
        ///// Unloads the preview handler hosted in this PreviewHandlerHost and closes the file stream.
        ///// </summary>
        //public void UnloadPreviewHandler()
        //{
        //    try
        //    {
        //        if (mCurrentPreviewHandler != null)
        //        {
        //            if (mCurrentPreviewHandler is IPreviewHandler)
        //            {
        //                // explicitly unload the content
        //                ((IPreviewHandler)mCurrentPreviewHandler).Unload();
        //            }
        //        }
        //        if (mCurrentPreviewHandlerStream != null)
        //        {
        //            mCurrentPreviewHandlerStream.Close();
        //            mCurrentPreviewHandlerStream = null;
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e);
        //    }
    }
}