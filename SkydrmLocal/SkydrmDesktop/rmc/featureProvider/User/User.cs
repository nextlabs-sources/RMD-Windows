using System;
using System.Collections.Generic;
using System.ComponentModel;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SkydrmDesktop;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.database2.table.user;
using SkydrmLocal.rmc.helper;
using SkydrmLocal.rmc.sdk;

namespace SkydrmLocal.rmc.featureProvider.User
{
    /// <summary>
    /// Use to upload PendingUploadFile
    /// </summary>
    public struct PendingUploadFileConfig
    {
        public bool overWriteUpload;
        public bool isExistInRemote;
    }

    public struct UserPreference
    {
        public bool isStartUpload;
        public bool isStartWhenWindowsLogin;
        public bool isLeaveACopyInSkyDRMLocalFolder;
        public UploadFilePolicy uploadFilePolicy;
        public DestForNxlConvert destForNxlConvert;
        public int heartBeatIntervalSec;
        public bool isCentralLocationRadio;
        public bool isCentralPlcRadio;
        public bool applyAllSelectedOption;
        public int selectedOption;
    }
    public class User : IUser
    {
        #region Data
        SkydrmApp app = SkydrmApp.Singleton;
        rmc.database2.table.user.User raw;
        UserPreference preference;
        string home_path;
        string rmsdk_home_path;
        string RPM_folder_path;
        LeaveCopy feature_leavecopy;
        // work around for avoiding mutil threads call uplaod at onece        
        private bool closeDoor_UploadLog = false;
        #endregion

        public User(database2.table.user.User raw)
        {
            this.raw = raw;
            this.preference = JsonConvert.DeserializeObject<UserPreference>(raw.User_preference_setting_json);

            // Config User folder:
            //  {App}/home/{Server}/{User_Email}/

            var path = app.Config.WorkingFolder;
            path += "\\home\\";
            var host = new Uri(app.Rmsdk.GetCurrentTenant().RMSURL).Host;
            path += host + "\\" + Email;
            // Make sure user working folder exist:
            Directory.CreateDirectory(path);
            // Create MyProject and MyVault
            Directory.CreateDirectory(path + "\\MyProject");
            Directory.CreateDirectory(path + "\\MyVault");

            home_path = path;
            RPM_folder_path = app.Config.RpmDir;
            feature_leavecopy = new LeaveCopyImpl();

            // this is very ugly code, but sometimes we have to find it path;
            //Retrive RMSdk's user folder;
            var sdkhome = app.Config.RmSdkFolder;
            var tenant = app.Rmsdk.GetCurrentTenant().Name;
            rmsdk_home_path = sdkhome + "\\" + tenant + "\\" + RmsUserId;

            ImplFolderProtect();

            GetDocumentPreference();


            closeDoor_UploadLog = false;
        }

        public string RPMFolder => RPM_folder_path;

        public string SDkWorkingFolder => rmsdk_home_path;

        public string WorkingFolder => home_path;

        public int RmsUserId { get => raw.Rms_user_id; }

        public string Name { get => raw.Name; }

        public string Email { get => raw.Email; }

        public UserType UserType { get => (UserType)raw.Rms_user_type; }

        public int LoginCounts => raw.Login_counts;

        public WaterMarkInfo Watermark
        {
            get => GetWaterMark();
            set => UpdateNxlWaterMark(value);
        }

        public Expiration Expiration
        {
            get => GetExpiration();
            set => UpdateNxlExpiration(value);
        }

        public DateTime LastLogin => raw.Last_access;

        public DateTime LastLogout => raw.Last_logout;

        public bool StartUpload
        {
            get => preference.isStartUpload;
            set { preference.isStartUpload = value; UpdateUserPrefence(); }
        }

        public bool LeaveCopy { get => app.Config.LeaveCopy; set => app.Config.LeaveCopy = value; }

        public bool ShowNotifyWindow { get => app.Config.ShowNotifyWin; set => app.Config.ShowNotifyWin = value; }

        public UploadFilePolicy UploadFilePolicy
        {
            get
            {
                return preference.uploadFilePolicy;
            }
            set
            {
                preference.uploadFilePolicy = value;
                UpdateUserPrefence();
            }
        }

        // Get WaterMark and Expiration from rms
        public void GetDocumentPreference()
        {
            try
            {
                //invoke SDWLResult GetUserPreference
                Expiration eprn;              
                string watermark;

                SkydrmApp.Singleton.Rmsdk.User.GetPreference(out eprn,
                    out watermark);
                
                if (watermark != null)
                {
                    WaterMarkInfo waterMarkInfo = new WaterMarkInfo();
                    waterMarkInfo.text = watermark;

                    //set Watermark
                    Watermark = waterMarkInfo;
                }

                //set expiration
                Expiration = eprn;

            }
            catch (Exception msg)
            {
                app.Log.Error("Error in GetDocumentPreference()", msg);
            }
        }

        // Update User WaterMark and Expiration to rms
        public void UpdateDocumentPreference()
        {
            try
            {
                //invoke SDWLResult UpdateUserPreference
                Expiration eprn;            
                string watermark;

                eprn = Expiration;
                watermark = Watermark.text;

                SkydrmApp.Singleton.Rmsdk.User.SetPreference(eprn, watermark);
            }
            catch (Exception msg)
            {
                app.Log.Error("Error in UpdateDocumentPreference()", msg);
            }
        }

        public int HeartBeatIntervalSec
        {
            get => GetHeartBeatIntervalSec();
        }

        public LeaveCopy LeaveCopy_Feature => feature_leavecopy;

        public bool IsCentralLocationRadio
        {
            get => preference.isCentralLocationRadio;
            set
            {
                if (preference.isCentralLocationRadio == value)
                {
                    return;
                }
                preference.isCentralLocationRadio = value;
                UpdateUserPrefence();
            }
        }

        public bool IsCentralPlcRadio
        {
            get => preference.isCentralPlcRadio;
            set
            {
                if (preference.isCentralPlcRadio == value)
                {
                    return;
                }
                preference.isCentralPlcRadio = value;
                UpdateUserPrefence();
            }
        }

        public bool ApplyAllSelectedOption
        {
            get => preference.applyAllSelectedOption;
            set
            {
                if (preference.applyAllSelectedOption == value)
                {
                    return;
                }
                preference.applyAllSelectedOption = value;
                UpdateUserPrefence();
            }
        }

        public int SelectedOption
        {
            get => preference.selectedOption;
            set
            {
                if (preference.selectedOption == value)
                {
                    return;
                }
                preference.selectedOption = value;
                UpdateUserPrefence();
            }
        }

        // on each heartbeat period, fetch latest config from Register
        // update heatbeatFrequence from server
        public void OnHeartBeat()
        {
            try
            {
                // sync project, policy bundle, user attributes
                WaterMarkInfo wmf;
                Int32 nHeartBeatFrequence;
                SkydrmApp.Singleton.Rmsdk.User.SyncHeartBeatInfo(out wmf, out nHeartBeatFrequence);
                // Update new value into app level config.
                SkydrmApp.Singleton.Config.HeartBeatIntervalSec = nHeartBeatFrequence;
                // returive user settings from rms by sdk
                GetDocumentPreference();
                
                // for safety consideration, reprotect critical folder
                ImplFolderProtect();
                // may need to uplaod nxl file log
                UploadNxlFileLog_Async();
            }
            catch (Exception e)
            {
                SkydrmApp.Singleton.Log.Error("HeartBeat failed: " + e.ToString());
            }
        }


        #region User Private Method

        private WaterMarkInfo GetWaterMark()
        {
            var e = raw.Rms_nxl_watermark_setting;
            try
            {
                return JsonConvert.DeserializeObject<WaterMarkInfo>(e);
            }
            catch(Exception msg)
            {
                SkydrmApp.Singleton.Log.Error("Error in DeserializeObject<WaterMarkInfo>:", msg);
            }

            // give a default value
            var rt = new WaterMarkInfo(); 
            rt.text = "$(User)$(Break)$(Date)$(Time)";
            raw.Rms_nxl_watermark_setting = JsonConvert.SerializeObject(rt);
            // update db;
            SkydrmApp.Singleton.DBFunctionProvider.UpdateUserWaterMark(raw.Rms_nxl_watermark_setting);
            return rt;
        }

        private void UpdateNxlWaterMark(WaterMarkInfo s)
        {
            string json = JsonConvert.SerializeObject(s);
            if (raw.Rms_nxl_watermark_setting.Equals(json))
            {
                return;
            }
            // update cache
            raw.Rms_nxl_watermark_setting = json;
            // update db;
            SkydrmApp.Singleton.DBFunctionProvider.UpdateUserWaterMark(json);
        }

        private Expiration GetExpiration()
        {
            var e = raw.Rms_nxl_expiration_setting;
            try
            {
                return JsonConvert.DeserializeObject<Expiration>(e);
            }
            catch(Exception msg)
            {
                SkydrmApp.Singleton.Log.Error("Error in DeserializeObject<Expiration>:", msg);
            }

            // give a default value
            var rt = new Expiration(); 
            raw.Rms_nxl_expiration_setting = JsonConvert.SerializeObject(rt);
            // update db;
            SkydrmApp.Singleton.DBFunctionProvider.UpdateUserExpiration(raw.Rms_nxl_expiration_setting);
            return rt;
        }

        private void UpdateNxlExpiration(Expiration e)
        {
            string json = JsonConvert.SerializeObject(e);
            if (raw.Rms_nxl_expiration_setting.Equals(json))
            {
                return;
            }

            // update cache
            raw.Rms_nxl_expiration_setting = json;
            // update db;
            SkydrmApp.Singleton.DBFunctionProvider.UpdateUserExpiration(json);

        }

        private void UpdateUserPrefence()
        {
            SkydrmApp.Singleton.DBFunctionProvider.UpdateUserPreference(
                JsonConvert.SerializeObject(preference));
        }

        private int GetHeartBeatIntervalSec()
        {
            //SkydrmApp.Singleton.Config.GetRegistryLocalApp();
            var sec = SkydrmApp.Singleton.Config.HeartBeatIntervalSec;

            //if HeartBeatIntervalSec registry modified by other software or user 
            if (sec != preference.heartBeatIntervalSec)
            {
                preference.heartBeatIntervalSec = sec;
                //update database
                UpdateUserPrefence();

                return preference.heartBeatIntervalSec;
            }
            return preference.heartBeatIntervalSec;
        }

        public void AddNxlFileLog(string strJson)
        {
            var app = SkydrmApp.Singleton;
            try
            {
                // decapsulate base64 into a json string;
                strJson = Encoding.UTF8.GetString(System.Convert.FromBase64String(strJson));

                // deserialize
                var log = JsonConvert.DeserializeObject<NxlLogJson>(strJson);
                app.Rmsdk.User.AddLog(log.LocalDiskPath,
                    (NxlOpLog)Enum.Parse(typeof(NxlOpLog), log.Strlog),
                    log.IsAllow
                    );
                UploadNxlFileLog_Async();
            }
            catch(Exception e)
            {
                app.Log.Warn("error when sending nxl log to rms,e=" + e.Message, e);
            }
        }

        public void AddNxlFileLog(string LocalDiskPath, NxlOpLog op, bool isAllow)
        {
            var app = SkydrmApp.Singleton;
            try
            {
                app.Rmsdk.User.AddLog(LocalDiskPath,op,isAllow);
                UploadNxlFileLog_Async();
            }
            catch(Exception e)
            {
                app.Log.Warn("error when sending nxl log to rms,e=" + e.Message, e);
            }
        }

        // Now don't need to call this since service will do auto update.
        public void UploadNxlFileLog_Async()
        {
            // every log fire background task
            new NoThrowTask(true, () =>
            {
                // find a way to avoid multi thread call smartly
                try
                {
                    if (!closeDoor_UploadLog)
                    {
                        closeDoor_UploadLog = true;
                        SkydrmApp.Singleton.Rmsdk.User.UploadActivityLogs();
                    }
                }
                finally
                {
                    closeDoor_UploadLog = false;
                }
            }).Do();
        }

        // for Local folder safety, it need to remove current user's dir-list permission,
        // currently requred to set:
        //    Database, RMSDK, UserHome, RPM
        private void ImplFolderProtect()
        {
            bool isSet = SkydrmApp.Singleton.Config.IsFolderProtect;
            string path_database = SkydrmApp.Singleton.Config.DataBaseFolder;
            string path_rmsdk = SkydrmApp.Singleton.Config.RmSdkFolder;
            // as required protect some user folder
            //FileHelper.ProtectFolder(home_path, isSet);
            //FileHelper.ProtectFolder(RPM_folder_path, isSet);
            FileHelper.ProtectFolder(path_database, isSet);
            //FileHelper.ProtectFolder(path_rmsdk, isSet);

        }


        #endregion

        private class LeaveCopyImpl : LeaveCopy
        {
            public bool AddFile(string FilePath) 
            {
                try
                {
                    string tmpFolder = FileHelper.GetLeaveCopyTempFolder(FilePath);
                    if (string.IsNullOrEmpty(tmpFolder))
                    {
                        return false;
                    }

                    if (!FileHelper.Exist(FilePath))
                    {
                        return false;
                    }
                    var name = Path.GetFileName(FilePath);
                    File.Copy(FilePath, tmpFolder + "\\" + name,true);
                    return true;
                }
                catch(Exception e)
                {
                    SkydrmApp.Singleton.Log.Warn("Exception:" + e.Message, e);
                }
                return false;
                
            }

            public bool DeleteFile(string FilePath) 
            {
                try
                {
                    string tmpFolder = FileHelper.GetLeaveCopyTempFolder(FilePath);
                    if (string.IsNullOrEmpty(tmpFolder))
                    {
                        return false;
                    }

                    var path = tmpFolder + "\\" + Path.GetFileName(FilePath);
                    File.Delete(path);
                    return true;
                }
                catch (Exception e)
                {
                    SkydrmApp.Singleton.Log.Warn("Exception:" + e.Message, e);
                }
                return false;
            }


            public bool Exist(string FileName, string cacheFolder, string localPath) 
            {
                try
                {
                    var tmpFolder = "";
                    if (!string.IsNullOrEmpty(localPath))
                    {
                        tmpFolder = FileHelper.GetLeaveCopyTempFolder(localPath);
                    }
                    else
                    {
                        tmpFolder = FileHelper.GetLeaveCopyTempFolderEx(cacheFolder);
                    }

                    if (string.IsNullOrEmpty(tmpFolder))
                    {
                        return false;
                    }


                    return File.Exists(tmpFolder + "\\" + FileName);
                }
                catch (Exception e)
                {
                    SkydrmApp.Singleton.Log.Warn("Exception:" + e.Message, e);
                }
                return false;
            }

            public bool MoveTo(string cacheFolder, string FileName) 
            {
                try
                {
                    string tmpFolder = FileHelper.GetLeaveCopyTempFolderEx(cacheFolder);
                    if (string.IsNullOrEmpty(tmpFolder))
                    {
                        return false;
                    }

                    var path = tmpFolder + "\\" + FileName;
                    var destPath = cacheFolder + "\\" + FileName;

                    // Allow replace the existing same name file when move if execute overwrite uploading. 
                    File.Move(path, destPath, MoveOptions.ReplaceExisting);
                    return true;
                }
                catch (Exception e)
                {
                    SkydrmApp.Singleton.Log.Warn("Exception:" + e.Message, e);
                }
                return false;
            }
        }

    }

   

}
