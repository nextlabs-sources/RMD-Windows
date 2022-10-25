using System;
using System.Collections.Generic;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Viewer.upgrade.application;
using Viewer.upgrade.cookie;
using Viewer.upgrade.file.basic.utils;
using Viewer.upgrade.session;
using Viewer.upgrade.utils;
using Viewer.upgrade.file.utils;

namespace Viewer.upgrade.file.basic
{
    public class _File : IFile
    {
        public string FileName => mFileName;
        public string Extention => mExtention;
        public string FilePath => mFilePath;
        public EnumFileType FileType => mFileType;
      //  public UInt64 StatusCode => mStatusCode;

        protected ViewerApp mApplication;
        protected string mFileName;
        protected string mExtention;
        protected string mFilePath;
      //  protected bool mIsNxlFile;
        protected EnumFileType mFileType = EnumFileType.UNKNOWN;
       // protected UInt64 mStatusCode = FileStatusCode.DEFAULTS;
        protected Cookie mCookie;

        public _File(Cookie cookie)
        {
            try
            {
                mApplication = (ViewerApp)ViewerApp.Current;
                mCookie = cookie;
                mFilePath = cookie.FilePath;
                mFileName = Path.GetFileName(mFilePath);
                //mExtention = Path.GetExtension(mFilePath).ToLower();
                mExtention = NxlFileUtils.GetFileExtention(mFilePath).ToLower();
                mFileType = NxlFileUtils.GetFileTypeByExtentionEx(cookie.FilePath);
            }
            catch (Exception ex)
            {
                mApplication.Log.Error(ex);
                throw ex;
            }
        }

        //public UInt64 Open()
        //{
        //    try
        //    {
        //        mFileName = Path.GetFileName(mFilePath);
        //        mExtention = Path.GetExtension(mFilePath).ToLower();
        //        mFileType ToolKit.GetFileTypeByExtentionEx(mParameter.FilePath);
        //        if (ToolKit.GetFileTypeByExtentionEx(mParameter.FilePath, out mFileType) != ErrorCode.SUCCEEDED)
        //        {
        //            mStatusCode |= FileStatusCode.INTERNAL_ERROR;
        //            return ErrorCode.SYSTEM_INTERNAL_ERROR;
        //        }
        //        mStatusCode |= FileStatusCode.OPENED;
        //        return ErrorCode.SUCCEEDED;
        //    }
        //    catch (Exception ex)
        //    {
        //        mStatusCode |= FileStatusCode.INTERNAL_ERROR;
        //        return ErrorCode.SYSTEM_INTERNAL_ERROR;
        //    }
        //}

        //public void Close()
        //{
        //    return;
        //}

        public void Delete()
        {
            return;
        }

        ///**
        // * ErrorCode
        // *      NOT_NXL_FILE;
        // *      NOT_AUTHORIZED;
        // *      SYSTEM_INTERNAL_ERROR;
        // *      FILE_EXPIRED;   
        // *      SUCCEEDED;
        // * **/
        //public UInt64 Check()
        //{
        //    if ((mStatusCode & FileStatusCode.DEFAULTS) != FileStatusCode.DEFAULTS)
        //    {
        //        return ErrorCode.SYSTEM_INTERNAL_ERROR;
        //    }

        //    return ErrorCode.SUCCEEDED;
        //}
    }
}
