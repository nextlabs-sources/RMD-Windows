using CustomControls.windows.fileInfo.view;
using Microsoft.Win32;
using Newtonsoft.Json;
using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Viewer.upgrade.application;
using Viewer.upgrade.cookie;
using Viewer.upgrade.exception;
using Viewer.upgrade.file.basic.utils;

namespace Viewer.upgrade.file.basic
{
    public class _NxlFile : INxlFile
    {
        public string Duid { get => mNxlFile.Duid; }
        public bool Expired { get => mNxlFile.Expired; }
        public WatermarkInfo WatermarkInfo { get => mNxlFile.WatermarkInfo; }
        public NxlFileFingerPrint NxlFileFingerPrint { get => mNxlFile.NxlFileFingerPrint; }
        public string FileName { get => mNxlFile.FileName; }
        public string Extention { get => mNxlFile.Extention; }
        public string FilePath { get => mNxlFile.FilePath; }
        public EnumFileType FileType { get => mNxlFile.FileType; }
        public Int32 Dirstatus => mDirstatus;

        protected ViewerApp mApplication;
        protected Int32 mDirstatus;
        protected bool mIsNxlFile;
        protected INxlFile mNxlFile;
        protected Cookie mCookie;

        public const Int32 NORMAL_DIR = 0x00000000;
        public const Int32 RPM_SAFEDIRRELATION_SAFE_DIR = 0x00000001;
        public const Int32 RPM_SAFEDIRRELATION_ANCESTOR_OF_SAFE_DIR = 0x00000002;
        public const Int32 RPM_SAFEDIRRELATION_DESCENDANT_OF_SAFE_DIR = 0x00000004;

        public _NxlFile(Cookie cookie)
        {
            mApplication = (ViewerApp)Application.Current;
            mCookie = cookie;
            try
            {
                if (!mApplication.SdkSession.SDWL_RPM_GetFileStatus(mCookie.FilePath, out mDirstatus, out mIsNxlFile))
                {
                    throw new RmSdkException();
                }

                if (IsUnderRpmFolder())
                {
                    mApplication.Log.Info("\t\t Is Rpm Nxl File \r\n");
                    mNxlFile = new _RpmNxlFile(cookie, mDirstatus);
                }
                else
                {
                    mApplication.Log.Info("\t\t Is Std Nxl File \r\n");
                    mNxlFile = new _StdNxlFile(cookie, mDirstatus);
                }
            }
            catch (Exception ex)
            {
                mApplication.Log.Error(ex);
                throw ex;
            }
        }

        private bool IsUnderRpmFolder()
        {
            bool result = false;
            switch (mDirstatus)
            {
                case RPM_SAFEDIRRELATION_SAFE_DIR:
                case RPM_SAFEDIRRELATION_DESCENDANT_OF_SAFE_DIR:
                case RPM_SAFEDIRRELATION_SAFE_DIR + _NxlFile.RPM_SAFEDIRRELATION_DESCENDANT_OF_SAFE_DIR:
                case RPM_SAFEDIRRELATION_DESCENDANT_OF_SAFE_DIR + _NxlFile.RPM_SAFEDIRRELATION_ANCESTOR_OF_SAFE_DIR:
                case RPM_SAFEDIRRELATION_SAFE_DIR + _NxlFile.RPM_SAFEDIRRELATION_DESCENDANT_OF_SAFE_DIR + _NxlFile.RPM_SAFEDIRRELATION_ANCESTOR_OF_SAFE_DIR:
                    result = true;
                    break;

                case NORMAL_DIR:
                case RPM_SAFEDIRRELATION_ANCESTOR_OF_SAFE_DIR:
                    result = false;
                    break;
            }
            return result;
        }

        public void Share(Window Owner)
        {
            mNxlFile.Share(Owner);
        }

        public void Print(Window Owner)
        {
            mNxlFile.Print(Owner);
        }

        public void Export(Window Owner)
        {
            mNxlFile.Export(Owner);
        }

        public void Extract(Window Owner)
        {
            mNxlFile.Extract(Owner);
        }

        public void FileInfo(Window Owner)
        {
            mNxlFile.FileInfo(Owner);
        }

        public string Decrypt(string outputFileName = "", bool removeTimestamp = true)
        {
            return mNxlFile.Decrypt(outputFileName, removeTimestamp);
        }

        public void Edit(Action<bool> EditSaved, Action ProcessExited)
        {
             mNxlFile.Edit(EditSaved, ProcessExited);
        }

        public bool CanShare()
        {
            return mNxlFile.CanShare();
        }

        public bool CanPrint()
        {
            return mNxlFile.CanPrint();
        }

        public bool CanExport()
        {
            return mNxlFile.CanExport();
        }

        public bool CanExtract()
        {
            return mNxlFile.CanExtract();
        }

        public bool CanFileInfo()
        {
            return mNxlFile.CanFileInfo();
        }

        public bool CanEdit()
        {
            return mNxlFile.CanEdit();
        }

        public void ClearTempFiles()
        {
            mNxlFile.ClearTempFiles();
        }

        //public ulong Open()
        //{
        //    if (!mApplication.SdkSession.SDWL_RPM_GetFileStatus(mCookie.FilePath, out mDirstatus, out mIsNxlFile))
        //    {
        //        return ErrorCode.SYSTEM_INTERNAL_ERROR;
        //    }
        //    UInt64 res = ErrorCode.SUCCEEDED;
        //    if (IsUnderRpmFolder())
        //    {
        //        res = _mRpmNxlFile.Open();
        //    }
        //    else
        //    {
        //        res = _mStdNxlFile.Open();
        //    }
        //    return res;
        //}

        //public ulong Close()
        //{
        //    UInt64 res = ErrorCode.SUCCEEDED;
        //    if (IsUnderRpmFolder())
        //    {
        //        res = _mRpmNxlFile.Close();
        //    }
        //    else
        //    {
        //        res = _mStdNxlFile.Close();
        //    }
        //    return res;
        //}

        public void Delete()
        {
            mNxlFile.Delete();
        }
    }
 }
