using SkydrmLocal.rmc.common.interfaces;
using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.featureProvider
{
    /// <summary>
    ///  As current login User, to provider as much as possible info to Upper-level
    ///    let user manage nxl log
    /// </summary>
    public interface IUser : IHeartBeat
    {
        string WorkingFolder { get; }

        // this is a tmp work around method, since many features need to get user's sdk folder
        string SDkWorkingFolder { get; }

        int RmsUserId { get; }

        string Name { get; }

        string Email { get; }

        UserType UserType { get; }

        int LoginCounts { get; }

        WaterMarkInfo Watermark { get; set; }

        DateTime LastLogin { get; }

        DateTime LastLogout { get; }

        Expiration Expiration { get; set; }

        bool StartUpload { get; set; }

        bool AutoStartApp { get; set; }

        bool LeaveCopy { get; set; }

        UploadFilePolicy UploadFilePolicy { get; set; }

        // Get WaterMark and Expiration from rms
        void GetUserPreference();

        // Update User WaterMark and Expiration to rms
        void UpdateUserPreference();

        string RPMFolder { get; }

        int HeartBeatIntervalSec { get; }

        LeaveCopy LeaveCopy_Feature {get;}

        void AddNxlFileLog(string strJson);

        void AddNxlFileLog(string LocalDiskPath, NxlOpLog op, bool isAllow);

        void UploadNxlFileLog_Async();

        bool IsFirstProtectFile { get; set; }

        bool IsCentralLocationRadio { get; set; }

        bool IsCentralPlcRadio { get; set; }

    }

    public interface LeaveCopy
    {
        bool AddFile(string FilePath);
        bool Exist(string FileName);
        bool DeleteFile(string FileName);
        bool MoveTo(string FileName, string DestPath);
    }


    public enum UploadFilePolicy
    {
        Automatic = 0,
        Manual = 1,
        Schedule = 2
    }

    public enum DestForNxlConvert
    {
        MyVault = 0,
        Project = 1,
        Local = 2
    }

    public struct Quota
    {
        public long usage;
        public long totalquota;
        public long vaultusage;
        public long vaultquota;
    }


    public class NxlLogJson
    {
        private string localDiskPath;

        private string strlog;

        private bool isAllow = true;

        public NxlLogJson()
        {

        }

        public NxlLogJson(string LocalDiskPath, string Strlog)
        {
            this.LocalDiskPath = "'" + LocalDiskPath + "'";
            this.Strlog = "'" + Strlog + "'";
        }

        public string LocalDiskPath { get => localDiskPath; set => localDiskPath = value; }
        public string Strlog { get => strlog; set => strlog = value; }
        public bool IsAllow { get => isAllow; set => isAllow = value; }
    }


}
