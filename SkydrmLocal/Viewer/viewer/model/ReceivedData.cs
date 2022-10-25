using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Viewer.overlay;
using Viewer.utils;
using Viewer.viewer.model;

namespace Viewer.viewer
{
    public enum EnumFileRepo
    {
        UNKNOWN = 0,
        EXTERN = 1, // extern nxl file.
        REPO_MYVAULT = 2,
        REPO_PROJECT = 3,
        REPO_SHARED_WITH_ME
    }

    public enum ExpiryType
    {
        NEVER_EXPIRE = 0,
        RELATIVE_EXPIRE,
        ABSOLUTE_EXPIRE,
        RANGE_EXPIRE,
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct Expiration
    {
        public ExpiryType type;
        public Int64 Start;
        public Int64 End;
    }

    public enum EnumNxlFileStatus
    {
        // Clone copy after successful upload (if user checked in “Preferences” -  “Leave a clone copy in SkyDRM Local Folder”)
        CachedFile = 0,

        // Available for offline view(marked "Make Available Offline")
        AvailableOffline = 1,

        // File is uploading
        Uploading = 2,

        //  Waiting for upload(if someone shared or protected file in offline mode and upload is still pending)
        WaitingUpload = 3,

        // File is in remote
        Online = 4,

        /********Follow status is mainly used for Service Manager Display****************/

        // File upload successfully
        UploadSucceed = 5,

        // File upload failed
        UploadFailed = 6,

        // File is removed by user.
        RemovedFromLocal = 7,

        //File is downloading 
        Downloading = 8,

        // downloaded succeed
        DownLoadedSucceed = 9,

        // downloaded failed
        DownLoadedFailed = 10,

        // MISC.
        // file missing in local 
        FileMissingInLocal = 11,

        // unknown error
        UnknownError = 12,

        ProtectSucceeded = 13,

        ProtectFailed = 14

        // means available offline file is edited.
        //AvailableOffline_Edited = 15,

        // cached file is edited.
        //CachedFile_Edited = 16

    }

    public class Log
    {

        public Log()
        {

        }

        public Log(string LocalDiskPath, string Strlog, bool IsAllow)
        {
            this.LocalDiskPath = LocalDiskPath ;
            this.Strlog = Strlog ;
            this.IsAllow = IsAllow;
        }

        public string LocalDiskPath { get; set; }
        public string Strlog { get; set; }
        public bool IsAllow { get; set; }
    }

    [Serializable]
    public class NxlConverterResult
    {
        public string UserEmail { get; }
        public string TmpPath { get; }
        public bool IsConverterSucceed { get; }
        public string ErrorMsg { get; }
        public Int32 ErrorCode { get; }
        public string FileName { get; }
        public long Size { get; }
        public string DateModified { get; }
        public string LocalDiskPath { get; }
        public string RmsRemotePath { get; }
        public bool IsCreatedLocal { get; }
        public string[] SharedWith { get; }
        public FileRights[] Rights { get; }
        public string AdhocWaterMark { get; }
        public Expiration Expiration { get; }
        public string Tags { get; }
        public EnumFileRepo EnumFileRepo { get; }
        public bool IsDecryptFromRPM { get; }
        public bool IsDisplayEditButton { get; }
        public bool IsDisplayPrintButton { get; }
        public bool IsDisplayShareButton { get; }
        public bool IsDisplaySaveAsButton { get; }
        public bool IsDisplayExtractButton { get; }
        public bool IsOwner { get;}
        public bool IsByAdHoc { get; }
        public bool IsByCentrolPolicy { get;}
        public string ForPrintFilePath { get;}

        public NxlConverterResult(string UserEmail,
                                    string TmpPath,
                                    bool IsConverterSucceed,
                                    string ErrorMsg,
                                    Int32 ErrorCode,
                                    string FileName,
                                    long Size,
                                    string DateModified,
                                    string LocalDiskPath,
                                    string RmsRemotePath,
                                    bool IsCreatedLocal,
                                    string[] SharedWith,
                                    FileRights[] Rights,
                                    string AdhocWaterMark,
                                    Expiration Expiration,
                                    string Tags,
                                    EnumFileRepo EnumFileRepo,
                                    bool IsDecryptFromRPM,
                                    bool IsDisplayEditButton,
                                    bool IsDisplayPrintButton,
                                    bool IsDisplayShareButton,
                                    bool IsDisplaySaveAsButton,
                                    bool IsOwner,
                                    bool IsDisplayExtractButton,
                                    bool IsByAdHoc,
                                    bool IsByCentrolPolicy,
                                    string ForPrintFilePath
                                    )
        {
            this.UserEmail = UserEmail;
            this.TmpPath = TmpPath;
            this.IsConverterSucceed = IsConverterSucceed;
            this.ErrorMsg = ErrorMsg;
            this.ErrorCode = ErrorCode;
            this.FileName = FileName;
            this.Size = Size;
            this.DateModified = DateModified;
            this.LocalDiskPath = LocalDiskPath;
            this.RmsRemotePath = RmsRemotePath;
            this.IsCreatedLocal = IsCreatedLocal;
            this.SharedWith = SharedWith;
            this.Rights = Rights;
            this.AdhocWaterMark = AdhocWaterMark;
            this.Expiration = Expiration;
            this.Tags = Tags;
            this.EnumFileRepo = EnumFileRepo;
            this.IsDecryptFromRPM = IsDecryptFromRPM;
            this.IsDisplayEditButton = IsDisplayEditButton;
            this.IsDisplayPrintButton = IsDisplayPrintButton;
            this.IsDisplayShareButton = IsDisplayShareButton;
            this.IsDisplaySaveAsButton = IsDisplaySaveAsButton;
            this.IsOwner = IsOwner;
            this.IsDisplayExtractButton = IsDisplayExtractButton;
            this.IsByAdHoc = IsByAdHoc;
            this.IsByCentrolPolicy = IsByCentrolPolicy;
            this.ForPrintFilePath = ForPrintFilePath;
        }
    }
}
