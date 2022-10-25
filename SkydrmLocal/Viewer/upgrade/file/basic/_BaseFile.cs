using System;
using System.Collections.Generic;
using System.Diagnostics;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using SkydrmLocal.rmc.sdk;
using Viewer.upgrade.application;
using Viewer.upgrade.cookie;
using Viewer.upgrade.exception;
using Viewer.upgrade.file.basic.utils;


namespace Viewer.upgrade.file.basic
{
    public class _BaseFile : INxlFile
    {
        public string Duid { get => mNxlFile.Duid; }

        public bool Expired { get => mNxlFile.Expired; }

        public WatermarkInfo WatermarkInfo { get => mNxlFile.WatermarkInfo; }

        public NxlFileFingerPrint NxlFileFingerPrint { get => mNxlFile.NxlFileFingerPrint; }

        public string FileName { get => mIsNxlFile ? mNxlFile.FileName : mFile.FileName; }

        public string Extention { get => mIsNxlFile ? mNxlFile.Extention : mFile.Extention; }

        public string FilePath { get => mIsNxlFile ? mNxlFile.FilePath : mFile.FilePath; }

        public EnumFileType FileType { get => mIsNxlFile ? mNxlFile.FileType : mFile.FileType; }

        public Int32 Dirstatus { get => mDirstatus; }

        public bool IsNxlFile { get => mIsNxlFile; }
        private ViewerApp mApplication;
        private Int32 mDirstatus;
        private bool mIsNxlFile;
        private Cookie mCookie;
        private INxlFile mNxlFile;
        private IFile mFile;

        public _BaseFile(Cookie cookie)
        {
            this.mApplication = (ViewerApp)Application.Current;
            this.mCookie = cookie;
            try
            {
                if (!mApplication.SdkSession.SDWL_RPM_GetFileStatus(mCookie.FilePath, out mDirstatus, out mIsNxlFile))
                {
                    throw new RmSdkException();
                }

                if (!mIsNxlFile && (mCookie.FilePath.EndsWith(".nxl", StringComparison.CurrentCultureIgnoreCase)))
                {
                    throw new NxlFileException(mApplication.FindResource("Common_Invalid_Nxl_File_Error").ToString());
                }

                if (mIsNxlFile)
                {
                    mNxlFile = new _NxlFile(cookie);
                }
                else
                {
                    mFile = new _File(cookie);
                }
            }
            catch (Exception ex)
            {
                mApplication.Log.Error(ex);
                throw ex;
            }
        }

        //private UInt64 Check()
        //{
        //    if ((mStatusCode & FileStatusCode.INTERNAL_ERROR) == FileStatusCode.INTERNAL_ERROR)
        //    {
        //        return ErrorCode.SYSTEM_INTERNAL_ERROR;
        //    }
        //    return ErrorCode.SUCCEEDED;
        //}

        //public UInt64 Close()
        //{
        //    UInt64 res = Check();
        //    if (res != ErrorCode.SUCCEEDED)
        //    {
        //        return res;
        //    }
        //    return mIsNxlFile ? mNxlFile.Close() : mFile.Close();
        //}

        public string Decrypt(string outputFileName = "", bool removeTimestamp = true)
        {
            return mNxlFile?.Decrypt(outputFileName, removeTimestamp);
        }

        public void Delete()
        {
            if (mIsNxlFile)
            {
                mNxlFile?.Delete();
            }else
            {
                mFile?.Delete();
            }
        }

        public void Edit(Action<bool> EditSaved, Action ProcessExited)
        {
             mNxlFile?.Edit(EditSaved, ProcessExited);
        }

        public void Export(System.Windows.Window Owner)
        {
             mNxlFile?.Export(Owner);
        }

        public void Extract(System.Windows.Window Owner)
        {
             mNxlFile?.Extract(Owner);
        }

        public void FileInfo(System.Windows.Window Owner)
        {
             mNxlFile?.FileInfo(Owner);
        }

        public void Print(System.Windows.Window Owner)
        {
             mNxlFile?.Print(Owner);
        }

        public void Share(System.Windows.Window Owner)
        {
             mNxlFile?.Share(Owner);
        }

        public bool CanShare()
        {
            if(null == mNxlFile)
            {
                return false;
            }
           return mNxlFile.CanShare();
        }

        public bool CanPrint()
        {
            if (null == mNxlFile)
            {
                return false;
            }
            return mNxlFile.CanPrint();
        }

        public bool CanExport()
        {
            if (null == mNxlFile)
            {
                return false;
            }
            return mNxlFile.CanExport();
        }

        public bool CanExtract()
        {
            if (null == mNxlFile)
            {
                return false;
            }
            return mNxlFile.CanExtract();
        }

        public bool CanEdit()
        {
            if (null == mNxlFile)
            {
                return false;
            }
            return mNxlFile.CanEdit();
        }

        public bool CanFileInfo()
        {
            if (null == mNxlFile)
            {
                return false;
            }
            return mNxlFile.CanFileInfo();
        }

        public void ClearTempFiles()
        {
            if (null == mNxlFile)
            {
                return;
            }
            mNxlFile.ClearTempFiles();
        }

    }
}
