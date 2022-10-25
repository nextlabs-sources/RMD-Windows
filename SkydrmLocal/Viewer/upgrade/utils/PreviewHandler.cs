using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Viewer.upgrade.utils
{
    public class PreviewHandler
    {
        internal const string GUID_ISHELLITEM = "43826d1e-e718-42ee-bc55-a1e261c37bfe";
        private object mCurrentPreviewHandler;
        private Guid mCurrentPreviewHandlerGUID;
        private System.IO.FileStream mCurrentPreviewHandlerStream;
        private Rectangle mRectangle;
        private ViewerApp mViewerApp;

        public object CurrentPreviewHandler
        {
            get
            {
                return mCurrentPreviewHandler;
            }
        }

        public Guid CurrentPreviewHandlerGUID
        {
            get
            {
                return mCurrentPreviewHandlerGUID;
            }
        }


        public PreviewHandler(string extention)
        {
            try
            {
                mViewerApp = (ViewerApp)ViewerApp.Current;
                mCurrentPreviewHandlerGUID = GetPreviewHandlerGUID(extention);
                mViewerApp.Log.InfoFormat("\t\t CurrentPreviewHandlerGUID , GUID:{0} \r\n", mCurrentPreviewHandlerGUID);
                Type comType = Type.GetTypeFromCLSID(mCurrentPreviewHandlerGUID);
                mCurrentPreviewHandler = Activator.CreateInstance(comType);

                //if (Guid.Empty != mCurrentPreviewHandlerGUID)
                //{
                //    mCurrentPreviewHandler = CreatePreviewHandlerInstance(mCurrentPreviewHandlerGUID);
                //}
            }
            catch (Exception ex)
            {
                mViewerApp.Log.Error(ex.Message,ex);
                throw ex;
            }
        }

        //public bool Initialize(string extention)
        //{
        //    bool result = false;
        //    try
        //    {
        //        InitializePreviewHandlerGUID(extention, out mCurrentPreviewHandlerGUID);
        //        if (Guid.Empty != mCurrentPreviewHandlerGUID)
        //        {
        //            InitializePreviewHandler(mCurrentPreviewHandlerGUID, out mCurrentPreviewHandler);
        //            result = true;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //    }
        //    return result;
        //}

        //private void InitializePreviewHandlerGUID(string extention, out Guid currentPreviewHandlerGUID)
        //{
        //    currentPreviewHandlerGUID = GetPreviewHandlerGUID(extention);
        //}

        private Guid GetPreviewHandlerGUID(string extention)
        {
            mViewerApp.Log.InfoFormat("\t\t GetPreviewHandlerGUID , extention:{0} \r\n", extention);
            // open the registry key corresponding to the file extension
            RegistryKey ext = Registry.ClassesRoot.OpenSubKey(extention);
            if (ext != null)
            {
                // open the key that indicates the GUID of the preview handler type
                RegistryKey test = ext.OpenSubKey("shellex\\{8895b1c6-b41f-4c1c-a562-0d564250836f}");
                if (test != null) return new Guid(Convert.ToString(test.GetValue(null)));

                // sometimes preview handlers are declared on key for the class
                string className = Convert.ToString(ext.GetValue(null));
                if (className != null)
                {
                    test = Registry.ClassesRoot.OpenSubKey(className + "\\shellex\\{8895b1c6-b41f-4c1c-a562-0d564250836f}");
                    if (test != null) return new Guid(Convert.ToString(test.GetValue(null)));
                }
            }
            return Guid.Empty;
        }

        private object CreatePreviewHandlerInstance(Guid clsid)
        {
            // use reflection to instantiate the preview handler type
            Type comType = Type.GetTypeFromCLSID(clsid);
            return Activator.CreateInstance(comType);
        }

        public void OnResize(Rectangle r)
        {
            mRectangle = r;
            if (null != mCurrentPreviewHandler)
            {
                if (mCurrentPreviewHandler is IPreviewHandler)
                {
                    // update the preview handler's bounds to match the control's
                    ((IPreviewHandler)mCurrentPreviewHandler).SetRect(ref mRectangle);
                   // ((IPreviewHandler)mCurrentPreviewHandler).DoPreview();
                }
            }
        }

        public void DoPreview(string filePath, IntPtr windowHandle, Rectangle rectangle)
        {
            try
            {
                mRectangle = rectangle;
                if (null == mCurrentPreviewHandler)
                {
                    return;
                }

                if (mCurrentPreviewHandler is IInitializeWithFile)
                {
                    // some handlers accept a filename
                    try
                    {
                        ((IInitializeWithFile)mCurrentPreviewHandler).Initialize(filePath, 0);
                    }
                    catch (System.Runtime.InteropServices.COMException comex)
                    {
                        throw comex;
                    }
                }
                else if (mCurrentPreviewHandler is IInitializeWithStream)
                {
                    // other handlers want an IStream (in this case, a file stream)
                    mCurrentPreviewHandlerStream = File.Open(filePath, System.IO.FileMode.Open);
                    StreamWrapper stream = new StreamWrapper(mCurrentPreviewHandlerStream);
                    ((IInitializeWithStream)mCurrentPreviewHandler).Initialize(stream, 0);
                }
                else if (mCurrentPreviewHandler is IInitializeWithItem)
                {
                    // a third category exists, must be initialised with a shell item
                    IShellItem shellItem;
                    SHCreateItemFromParsingName(filePath, IntPtr.Zero, new Guid(GUID_ISHELLITEM), out shellItem);
                    ((IInitializeWithItem)mCurrentPreviewHandler).Initialize(shellItem, 0);
                }

                if (mCurrentPreviewHandler is IPreviewHandler)
                {
                    // bind the preview handler to the control's bounds and preview the content
                    IPreviewHandler handler = ((IPreviewHandler)mCurrentPreviewHandler);

                    handler.SetWindow(windowHandle, ref mRectangle);
                    handler.SetRect(ref mRectangle); //try fix Bug 51404 - [doc] word file content not display to top 

                    handler.DoPreview();
                    handler.SetFocus();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Unloads the preview handler hosted in this PreviewHandlerHost and closes the file stream.
        /// </summary>
        public void Dispose()
        {
            try
            {
                if (mCurrentPreviewHandler != null)
                {
                    if (mCurrentPreviewHandler is IPreviewHandler)
                    {
                        // explicitly unload the content
                        ((IPreviewHandler)mCurrentPreviewHandler).Unload();
                    }
                }

                if (mCurrentPreviewHandlerStream != null)
                {
                    mCurrentPreviewHandlerStream.Close();
                    mCurrentPreviewHandlerStream = null;
                }

                if (mCurrentPreviewHandler != null)
                {
                    Marshal.FinalReleaseComObject(mCurrentPreviewHandler);
                    mCurrentPreviewHandler = null;
                    GC.Collect();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        #region P/Invoke
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        static extern void SHCreateItemFromParsingName(
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            [In] IntPtr pbc, [In][MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            [Out][MarshalAs(UnmanagedType.Interface, IidParameterIndex = 2)] out IShellItem ppv
        );
        #endregion

        #region COM Interop
        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("8895b1c6-b41f-4c1c-a562-0d564250836f")]
        internal interface IPreviewHandler
        {
            void SetWindow(IntPtr hwnd, ref Rectangle rect);
            void SetRect(ref Rectangle rect);
            void DoPreview();
            void Unload();
            void SetFocus();
            void QueryFocus(out IntPtr phwnd);
            [PreserveSig]
            uint TranslateAccelerator(ref Message pmsg);
        }

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("b7d14566-0509-4cce-a71f-0a554233bd9b")]
        internal interface IInitializeWithFile
        {
            void Initialize([MarshalAs(UnmanagedType.LPWStr)] string pszFilePath, uint grfMode);
        }

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("b824b49d-22ac-4161-ac8a-9916e8fa3f7f")]
        internal interface IInitializeWithStream
        {
            void Initialize(IStream pstream, uint grfMode);
        }

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("7F73BE3F-FB79-493C-A6C7-7EE14E245841")]
        interface IInitializeWithItem
        {
            void Initialize(IShellItem psi, uint grfMode);
        }

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid(PreviewHandler.GUID_ISHELLITEM)]
        interface IShellItem
        {
            void BindToHandler(IntPtr pbc, [MarshalAs(UnmanagedType.LPStruct)]Guid bhid, [MarshalAs(UnmanagedType.LPStruct)]Guid riid, out IntPtr ppv);
            void GetParent(out IShellItem ppsi);
            void GetDisplayName(uint sigdnName, out IntPtr ppszName);
            void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
            void Compare(IShellItem psi, uint hint, out int piOrder);
        };

        /// <summary>
        /// Provides a bare-bones implementation of System.Runtime.InteropServices.IStream that wraps an System.IO.Stream.
        /// </summary>
        [ClassInterface(ClassInterfaceType.AutoDispatch)]
        internal class StreamWrapper : IStream
        {

            private System.IO.Stream mInner;

            /// <summary>
            /// Initialises a new instance of the StreamWrapper class, using the specified System.IO.Stream.
            /// </summary>
            /// <param name="inner"></param>
            public StreamWrapper(System.IO.Stream inner)
            {
                mInner = inner;
            }

            /// <summary>
            /// This operation is not supported.
            /// </summary>
            /// <param name="ppstm"></param>
            public void Clone(out IStream ppstm)
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// This operation is not supported.
            /// </summary>
            /// <param name="grfCommitFlags"></param>
            public void Commit(int grfCommitFlags)
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// This operation is not supported.
            /// </summary>
            /// <param name="pstm"></param>
            /// <param name="cb"></param>
            /// <param name="pcbRead"></param>
            /// <param name="pcbWritten"></param>
            public void CopyTo(IStream pstm, long cb, IntPtr pcbRead, IntPtr pcbWritten)
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// This operation is not supported.
            /// </summary>
            /// <param name="libOffset"></param>
            /// <param name="cb"></param>
            /// <param name="dwLockType"></param>
            public void LockRegion(long libOffset, long cb, int dwLockType)
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// Reads a sequence of bytes from the underlying System.IO.Stream.
            /// </summary>
            /// <param name="pv"></param>
            /// <param name="cb"></param>
            /// <param name="pcbRead"></param>
            public void Read(byte[] pv, int cb, IntPtr pcbRead)
            {
                long bytesRead = mInner.Read(pv, 0, cb);
                if (pcbRead != IntPtr.Zero) Marshal.WriteInt64(pcbRead, bytesRead);
            }

            /// <summary>
            /// This operation is not supported.
            /// </summary>
            public void Revert()
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// Advances the stream to the specified position.
            /// </summary>
            /// <param name="dlibMove"></param>
            /// <param name="dwOrigin"></param>
            /// <param name="plibNewPosition"></param>
            public void Seek(long dlibMove, int dwOrigin, IntPtr plibNewPosition)
            {
                long pos = mInner.Seek(dlibMove, (System.IO.SeekOrigin)dwOrigin);
                if (plibNewPosition != IntPtr.Zero) Marshal.WriteInt64(plibNewPosition, pos);
            }

            /// <summary>
            /// This operation is not supported.
            /// </summary>
            /// <param name="libNewSize"></param>
            public void SetSize(long libNewSize)
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// Returns details about the stream, including its length, type and name.
            /// </summary>
            /// <param name="pstatstg"></param>
            /// <param name="grfStatFlag"></param>
            public void Stat(out System.Runtime.InteropServices.ComTypes.STATSTG pstatstg, int grfStatFlag)
            {
                pstatstg = new System.Runtime.InteropServices.ComTypes.STATSTG();
                pstatstg.cbSize = mInner.Length;
                pstatstg.type = 2; // stream type
                pstatstg.pwcsName = (mInner is System.IO.FileStream) ? ((System.IO.FileStream)mInner).Name : String.Empty;
            }

            /// <summary>
            /// This operation is not supported.
            /// </summary>
            /// <param name="libOffset"></param>
            /// <param name="cb"></param>
            /// <param name="dwLockType"></param>
            public void UnlockRegion(long libOffset, long cb, int dwLockType)
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// Writes a sequence of bytes to the underlying System.IO.Stream.
            /// </summary>
            /// <param name="pv"></param>
            /// <param name="cb"></param>
            /// <param name="pcbWritten"></param>
            public void Write(byte[] pv, int cb, IntPtr pcbWritten)
            {
                mInner.Write(pv, 0, cb);
                if (pcbWritten != IntPtr.Zero) Marshal.WriteInt64(pcbWritten, (Int64)cb);
            }
        }
        #endregion

    }
}
