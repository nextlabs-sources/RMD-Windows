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
using Viewer.upgrade.file.basic.utils;
using Viewer.upgrade.utils;
using static Viewer.upgrade.utils.NetworkStatus;
using Viewer.upgrade.file.utils;

namespace Viewer.upgrade.file.basic
{
    public class _RpmNxlFile : INxlFile
    {
        public string Duid => mDuid;
        public bool Expired => mExpired;
        public WatermarkInfo WatermarkInfo => mWatermarkInfo;
        public NxlFileFingerPrint NxlFileFingerPrint => mNxlFileFingerPrint;
        public string FileName => mFileName;
        public string Extention => mExtention;
        public string FilePath => mFilePath;
        public EnumFileType FileType => mFileType;
        public Int32 Dirstatus => mDirstatus;

        protected ViewerApp mApplication;
        protected string mDuid;
        protected bool mExpired;
        protected WatermarkInfo mWatermarkInfo;
        protected NxlFileFingerPrint mNxlFileFingerPrint;
        protected string mFileName;
        protected string mExtention;
        protected string mFilePath;
        protected EnumFileType mFileType = EnumFileType.UNKNOWN;
        protected Cookie mCookie;
        protected bool mIsNetworkAvailable;
        protected Int32 mDirstatus;

        public _RpmNxlFile(Cookie cookie, Int32 dirstatus) : this(cookie)
        {
            this.mDirstatus = dirstatus;
        }

        private _RpmNxlFile(Cookie cookie)
        {
            try
            {
                mCookie = cookie;
                mApplication = (ViewerApp)Application.Current;
                AvailabilityChanged += new NetworkStatusChangedHandler(OnNetworkStatusChanged);
                mIsNetworkAvailable = NetworkStatus.IsAvailable;
                mFilePath = mCookie.FilePath;
                mFileName = Path.GetFileName(mFilePath);
                //if (mFilePath.EndsWith(".nxl", StringComparison.CurrentCultureIgnoreCase))
                //{
                //    mExtention = Path.GetExtension(Path.GetFileNameWithoutExtension(mFilePath)).ToLower();
                //}
                //else
                //{
                //    mExtention = Path.GetExtension(mFilePath).ToLower();
                //}
                mExtention = NxlFileUtils.GetFileExtention(mFilePath).ToLower();
                mFileType = NxlFileUtils.GetFileTypeByExtentionEx(mCookie.FilePath);
            }
            catch (Exception ex)
            {
                mApplication.Log.Error(ex);
                throw ex;
            }
        }

        public void Share(Window Owner)
        {
            throw new NotImplementedException();
        }

        public void Print(Window Owner)
        {
            throw new NotImplementedException();
        }

        public void Export(Window Owner)
        {
            throw new NotImplementedException();
        }

        public void Extract(Window Owner)
        {
            throw new NotImplementedException();
        }

        public void FileInfo(Window Owner)
        {
            throw new NotImplementedException();
        }

        public string Decrypt(string outputFileName = "", bool removeTimestamp = true)
        {
            string rpmFilePath = string.Copy(mFilePath);
            if (rpmFilePath.EndsWith(".nxl", StringComparison.CurrentCultureIgnoreCase))
            {
                rpmFilePath = rpmFilePath.Remove(rpmFilePath.Length - 4, 4);
            }
            return rpmFilePath;
        }

        public void Edit(Action<bool> EditSaved, Action ProcessExited)
        {
            throw new NotImplementedException();
        }

        public bool CanShare()
        {
            return false;
        }

        public bool CanPrint()
        {
            return false;
        }

        public bool CanExport()
        {
            return false;
        }

        public bool CanExtract()
        {
            return false;
        }

        public bool CanEdit()
        {
            return false;
        }

        public bool CanFileInfo()
        {
            return false;
        }

        //public void Close()
        //{
        //    return;
        //}

        public void Delete()
        {
            return;
        }

        public void ClearTempFiles()
        {
            return;
        }

        private void OnNetworkStatusChanged(object sender, NetworkStatusChangedArgs e)
        {
            mIsNetworkAvailable = e.IsAvailable;
        }
    }
}
