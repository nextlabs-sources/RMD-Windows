
using Alphaleonis.Win32.Filesystem;
using Newtonsoft.Json;
using Sdk.helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.sdk
{
    public class User
    {
        private IntPtr hUser;

        #region User Base
        public User(IntPtr hUser)
        {
            this.hUser = hUser;
        }

        public uint UserId
        {
            get
            {
                uint userId;
                Boundary.SDWL_User_GetUserId(hUser, out userId);
                return userId;
            }
        }

        public string Name
        {
            get
            {
                string name;
                Boundary.SDWL_User_GetUserName(hUser, out name);
                return name;
            }
        }

        public string Email
        {
            get

            {
                string email;
                Boundary.SDWL_User_GetUserEmail(hUser, out email);
                return email;
            }
        }

        public string PassCode
        {
            get
            {
                string code;
                Boundary.SDWL_User_GetPasscode(hUser, out code);
                return code;
            }
        }

        public UserType UserType
        {
            get
            {
                int type = -1;
                Boundary.SDWL_User_GetUserType(hUser, ref type);
                return (UserType)type;
            }
        }

        public void UpdateUserInfo()
        {
            uint rt = Boundary.SDWL_User_UpdateUserInfo(hUser);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_User_UpdateUserInfo", rt);
            }
        }

        public void UpdateMyDriveInfo()
        {
            uint rt = Boundary.SDWL_User_UpdateMyDriveInfo(hUser);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_User_UpdateMyDriveInfo", rt);
            }
        }

        public void GetMyDriveInfo(ref Int64 usage, ref Int64 total, ref Int64 vaultUsage, ref Int64 vaultQuota)
        {
            uint rt = Boundary.SDWL_User_GetMyDriveInfo(hUser, ref usage, ref total, ref vaultUsage, ref vaultQuota);
            if (rt != 0)
            {
                // ignore exception, because this is invoked in MainWindow
                //ExceptionFactory.BuildThenThrow("SDWL_User_GetMyDriveInfo", rt);
            }
        }

        public void Logout()
        {
            uint rt = Boundary.SDWL_User_LogoutUser(hUser);
            // by osmond, for the bug 49212, when user logout, we just ignore any error code

            //if (rt != 0)
            //{
            //    string msg = String.Format("exception for SDWL_User_LogoutUser,err={0}", rt);
            //    Console.WriteLine(msg);
            //    throw new Exception(msg);
            //}
        }

        //public LocalFiles GetLocalFiles()
        //{
        //    IntPtr f = IntPtr.Zero;
        //    uint rt = Boundary.SDWL_User_GetLocalFile(hUser, out f);
        //    if (rt != 0 || f == IntPtr.Zero)
        //    {
        //        ExceptionFactory.BuildThenThrow("SDWL_User_GetLocalFile", rt);
        //    }

        //    return new LocalFiles(f);
        //}

        // this is a wrapepr of rmsdk::getLocalFileManager().remove()
        public bool RemoveLocalGeneratedFiles(string file)
        {
            bool result;
            uint rt = Boundary.SDWL_User_RemoveLocalFile(hUser, file, out result);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("RemoveLocalGeneratedFiles", rt);
            }
            return result;
        }

        // fix Bug 66412 - File name converted to lowercase
        private string ChangeFileName(string originalNameWithoutExt, string nxlPath)
        {
            int length = originalNameWithoutExt.Length;
            string nxlName = Path.GetFileName(nxlPath);
            string timeNxlName = nxlName.Substring(length);
            string newNxlName = originalNameWithoutExt + timeNxlName;

            if (newNxlName.Equals(nxlName))
            {
                return nxlPath;
            }

            string dir = Path.GetDirectoryName(nxlPath);
            string newPath = Path.Combine(dir, newNxlName);
           
            File.Move(nxlPath, newPath, MoveOptions.ReplaceExisting);
            return newPath;
        }

        // Fix bug 56038
        //Should handle the Team center automatically rename for the postfix, like: Filename.prt-2019-01-24-07-04-28.1
        private string ReNameFilePath(string nxlPath)
        {
            string directory = Path.GetDirectoryName(nxlPath);
            string fileName = Path.GetFileName(nxlPath);
            string fileNameInput = Path.GetFileNameWithoutExtension(fileName);

            bool result= StringHelper.SpecialReplace(fileNameInput, out string outputFileName,
                                     StringHelper.TIMESTAMP_PATTERN + StringHelper.POSTFIX_1_249);
            if (result)
            {
                string replacePath = directory +"\\"+ outputFileName + ".nxl";
                File.Move(nxlPath, replacePath);
                return replacePath;
            }
            return nxlPath;
        }

        public string ProtectFile(string path, List<FileRights> rights,
            WaterMarkInfo waterMark, Expiration expiration, UserSelectTags tags)
        {
            List<int> r = new List<int>(rights.Count);

            foreach (var i in rights)
            {
                r.Add((int)i);
            }
            string outpath;
            uint rt = Boundary.SDWL_User_ProtectFile(hUser, path, r.ToArray(), r.Count, waterMark, expiration, tags.ToJsonString(), out outpath);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_User_ProtectFile", rt);
            }
            //outpath = ChangeFileName(Path.GetFileNameWithoutExtension(path), outpath);

            return ReNameFilePath(outpath);
        }

        public string ProtectFileToSystemProject(int id, string plain, List<FileRights> rights,
            WaterMarkInfo waterMark, Expiration expiration, UserSelectTags tags)
        {
            return ProtectFileToProject(id, plain, rights, waterMark, expiration, tags);
        }


        public bool IsEnabledWorkSpace()
        {
            bool isEnabled = true;
            // call sdk
            uint rt = Boundary.SDWL_User_CheckWorkSpaceEnable(hUser, ref isEnabled);
            if (rt != 0)
            {
                //ExceptionFactory.BuildThenThrow("IsEnabledWorkSpace", rt);
            }

            return isEnabled;
        }


        public bool IsEnabledAdhocForSystemBucket()
        {
            bool isEnabled = false;
            // call sdk
            uint rt = Boundary.SDWL_User_CheckSystemBucketEnableAdhoc(hUser, ref isEnabled);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("IsEnabledAdhocForProject", rt);
            }

            return isEnabled;
        }

        //public void UpdateRecipients(NxlFile file, List<string> addEmails, List<string> delEmails)
        //{
        //    uint rt = Boundary.SDWL_User_UpdateRecipients(hUser, file.Handle,
        //        addEmails.ToArray(), addEmails.Count,
        //        delEmails.ToArray(), delEmails.Count);
        //    if (rt != 0)
        //    {
        //        ExceptionFactory.BuildThenThrow("SDWL_User_UpdateRecipients", rt);
        //    }
        //}

        // by osmond, add new api
        public void UpdateRecipients(string nxlFilePath, List<string> addEmails, List<string> delEmails, string comment="")
        {
            uint rt = Boundary.SDWL_User_UpdateRecipients(hUser, nxlFilePath,
                addEmails.ToArray(), addEmails.Count,
                delEmails.ToArray(), delEmails.Count, comment);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_User_UpdateRecipients", rt);
            }
        }

        //public void GetRecipients(NxlFile file, out string[] emails, out string[] addEmails, out string[] removeEmails)
        //{

        //    emails = new string[0];
        //    addEmails = new string[0];
        //    removeEmails = new string[0];
        //    //uint rt = 
        //    IntPtr pE;
        //    int sizeArray;
        //    IntPtr pEA;
        //    int sizeArrayAdd;
        //    IntPtr pER;
        //    int sizeArrayRemove;

        //    uint rt = Boundary.SDWL_User_GetRecipients(hUser, file.Handle,
        //                                    out pE, out sizeArray,
        //                                    out pEA, out sizeArrayAdd,
        //                                    out pER, out sizeArrayRemove);
        //    if (rt != 0)
        //    {
        //        return;
        //    }

        //    if (sizeArray > 0)
        //    {
        //        emails = new string[sizeArray];
        //        IntPtr[] ps = new IntPtr[sizeArray];
        //        Marshal.Copy(pE, ps, 0, sizeArray);
        //        for (int i = 0; i < sizeArray; i++)
        //        {
        //            emails[i] = Marshal.PtrToStringAnsi(ps[i]);
        //            Marshal.FreeCoTaskMem(ps[i]);
        //        }
        //        Marshal.FreeCoTaskMem(pE);
        //    }

        //    if (sizeArrayAdd > 0)
        //    {
        //        addEmails = new string[sizeArray];
        //        IntPtr[] ps = new IntPtr[sizeArray];
        //        Marshal.Copy(pEA, ps, 0, sizeArray);
        //        for (int i = 0; i < sizeArray; i++)
        //        {
        //            addEmails[i] = Marshal.PtrToStringAnsi(ps[i]);
        //            Marshal.FreeCoTaskMem(ps[i]);
        //        }
        //        Marshal.FreeCoTaskMem(pEA);
        //    }

        //    if (sizeArrayRemove > 0)
        //    {
        //        removeEmails = new string[sizeArray];
        //        IntPtr[] ps = new IntPtr[sizeArray];
        //        Marshal.Copy(pER, ps, 0, sizeArray);
        //        for (int i = 0; i < sizeArray; i++)
        //        {
        //            removeEmails[i] = Marshal.PtrToStringAnsi(ps[i]);
        //            Marshal.FreeCoTaskMem(ps[i]);
        //        }
        //        Marshal.FreeCoTaskMem(pER);
        //    }

        //}

        public void GetRecipients(string nxlFilePath, out string[] emails, out string[] addEmails, out string[] removeEmails)
        {

            emails = new string[0];
            addEmails = new string[0];
            removeEmails = new string[0];
            //uint rt = 
            IntPtr pE;
            int sizeArray;
            IntPtr pEA;
            int sizeArrayAdd;
            IntPtr pER;
            int sizeArrayRemove;

            uint rt = Boundary.SDWL_User_GetRecipients(hUser, nxlFilePath,
                                            out pE, out sizeArray,
                                            out pEA, out sizeArrayAdd,
                                            out pER, out sizeArrayRemove);
            if (rt != 0)
            {
                return;
            }

            if (sizeArray > 0)
            {
                emails = new string[sizeArray];
                IntPtr[] ps = new IntPtr[sizeArray];
                Marshal.Copy(pE, ps, 0, sizeArray);
                for (int i = 0; i < sizeArray; i++)
                {
                    emails[i] = Marshal.PtrToStringAnsi(ps[i]);
                    Marshal.FreeCoTaskMem(ps[i]);
                }
                Marshal.FreeCoTaskMem(pE);
            }

            if (sizeArrayAdd > 0)
            {
                addEmails = new string[sizeArray];
                IntPtr[] ps = new IntPtr[sizeArray];
                Marshal.Copy(pEA, ps, 0, sizeArray);
                for (int i = 0; i < sizeArray; i++)
                {
                    addEmails[i] = Marshal.PtrToStringAnsi(ps[i]);
                    Marshal.FreeCoTaskMem(ps[i]);
                }
                Marshal.FreeCoTaskMem(pEA);
            }

            if (sizeArrayRemove > 0)
            {
                removeEmails = new string[sizeArray];
                IntPtr[] ps = new IntPtr[sizeArray];
                Marshal.Copy(pER, ps, 0, sizeArray);
                for (int i = 0; i < sizeArray; i++)
                {
                    removeEmails[i] = Marshal.PtrToStringAnsi(ps[i]);
                    Marshal.FreeCoTaskMem(ps[i]);
                }
                Marshal.FreeCoTaskMem(pER);
            }

        }

        public void GetRecipents2(string nxlFilePath, out string[] emails, out string[] addEmails, out string[] removeEmails)
        {
            emails = new string[0];
            addEmails = new string[0];
            removeEmails = new string[0];

            var rt = Boundary.SDWL_User_GetRecipents(
                hUser, nxlFilePath,
                out string recipents,
                out string recipentsAdd,
                out string recipentsRemove);

            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("GetRecipents2", rt);
            }

            emails = DataConvert.ParseRecipents(recipents).ToArray();
            addEmails = DataConvert.ParseRecipents(recipentsAdd).ToArray();
            removeEmails = DataConvert.ParseRecipents(recipentsRemove).ToArray();
        }

        public void CacheRPMFileToken(string NxlFilePath)
        {
            uint rt = Boundary.SDWL_User_CacheRPMFileToken(hUser, NxlFilePath);
            if (rt != 0)
            {
            }

            return;
        }

        // Defined Excpetion:
        //      RmSdkInsufficientRightsException - you dont have any rights
        //public NxlFile OpenNxlFile(string NxlFilePath)
        //{
        //    IntPtr hFile = IntPtr.Zero;
        //    uint rt = Boundary.SDWL_User_OpenFile(hUser, NxlFilePath, out hFile);
        //    if (rt != 0)
        //    {
        //        ExceptionFactory.BuildThenThrow("SDWL_User_OpenFile", rt);
        //    }
        //    return new NxlFile(hFile);
        //}

        //public void CloseNxlFile(NxlFile File)
        //{
        //    uint rt = Boundary.SDWL_User_CloseNxlFile(hUser, File.Handle);
        //    if (rt != 0)
        //    {
        //        ExceptionFactory.BuildThenThrow("SDWL_User_CloseNxlFile", rt);
        //    }
        //}

        // this is a work around ,to force sdk to release file handle,
        public void ForceCloseFile_NoThrow(string nxlFilePath)
        {
            try
            {
                Boundary.SDWL_User_ForceCloseFile(hUser, nxlFilePath);
            }
            catch (Exception ignored)
            {
                // Comment out this code, or else Viewer and Print project can't share link the sdk folder.
                //
                //SkydrmApp.Singleton.Log.Warn(ignored.Message, ignored);
            }
        }

        // this is a work around ,to force sdk to release file handle,
        //public void CloseNxlFile_NoThrow(string file)
        //{
        //    try
        //    {
        //        CloseNxlFile(OpenNxlFile(file));
        //    }
        //    catch(Exception ignored)
        //    {
        //        SkydrmLocalApp.Singleton.Log.Warn(ignored.Message, ignored);
        //    }
        //}


        //public void DecryptNxlFile(NxlFile File, string OutputPath)
        //{
        //    uint rt = Boundary.SDWL_User_DecryptNXLFile(hUser, File.Handle, OutputPath);
        //    if (rt != 0)
        //    {
        //        ExceptionFactory.BuildThenThrow("SDWL_User_DecryptNXLFile", rt);
        //    }
        //}

        public void UploadActivityLogs()
        {
            uint rt = Boundary.SDWL_User_UploadActivityLogs(hUser);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_User_UploadActivityLogs", rt);
            }
        }

        public void SyncHeartBeatInfo(out WaterMarkInfo waterMark, out Int32 heartbeatFrequenceSeconds)
        {
            uint rt = 0;
            rt = Boundary.SDWL_User_GetHeartBeatInfo(hUser);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_User_GetHeartBeatInfo", rt);
            }

            rt = Boundary.SDWL_User_GetWaterMarkInfo(hUser, out waterMark);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_User_GetWaterMarkInfo", rt);
            }

            rt = Boundary.SDWL_User_GetHeartBeatFrequency(hUser, out heartbeatFrequenceSeconds);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_User_GetHeartBeatFrequency", rt);
            }

        }

        public void AddLog(string nxlFilePath, NxlOpLog operation, bool isAllow)
        {
            uint rt = 0;
            rt = Boundary.SDWL_User_AddNxlFileLog(
                hUser, nxlFilePath, (int)operation, isAllow);
            if (rt != 0)
            {
               // ExceptionFactory.BuildThenThrow("SDWL_User_AddNxlFileLog", rt);
            }
        }

        public void GetPreference(out Expiration expiration, out string watermark)
        {
            Expiration theExpiration;
            theExpiration.type = ExpiryType.NEVER_EXPIRE;
            theExpiration.Start = 0;
            theExpiration.End = 0;
            string theWatermark;

            var rt = Boundary.SDWL_User_GetPreference(hUser, out theExpiration, out theWatermark);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_User_GetPreference", rt);
            }

            //rt
            expiration = theExpiration;
            watermark = theWatermark;
        }

        public void SetPreference(Expiration expiration, string watermark)
        {
            var rt = Boundary.SDWL_User_UpdatePreference(hUser, expiration, watermark);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_User_GetPreference", rt);
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 8)]
        public struct InternalFingerPrint
        {
            public string name;
            public string localPath;
            public Int64 size;

            public Int64 created;
            public Int64 modified;

            public Int64 isOwner;

            public Int64 isFromMyVault;

            public Int64 isFromPorject;
            public Int64 isFromSystemBucket;
            public Int64 projectId;

            public Int64 isByAdHoc;
            public Int64 isByCentrolPolicy;

            public string tags;
            public Expiration expiration;
            public string adhocWatermark;
            public Int64 rights;

            public Int64 hasAdminRights;
            public string duid;
        }

        /// <summary>
        ///     may throw InsufficientRightsException, you can not touch this file
        ///         
        /// </summary>
        /// <param name="nxlpath"></param>
        /// <returns></returns>
        /// 
        public NxlFileFingerPrint GetNxlFileFingerPrint(string nxlpath, bool doOwnerCheck = false)
        {
            // Comment out this code, or else Viewer and Print project can't share link the sdk folder.
            //
            //SkydrmApp.Singleton.Log.Info("GetNxlFileFingerPrint:" + nxlpath);

            InternalFingerPrint fp;
            var rt = Boundary.SDWL_User_GetNxlFileFingerPrint(hUser, nxlpath, out fp, doOwnerCheck);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("GetNxlFileFingerPrint", rt);
            }
            return DataConvert.Convert(fp);
        }

        public Dictionary<string, List<string>> GetNxlTagsWithoutToken(string nxlpath)
        {
            string tags;
            var rt = Boundary.SDWL_User_GetNxlFileTagsWithoutToken(hUser, nxlpath, out tags);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("GetNxlFileFingerPrint", rt);
            }
            return DataConvert.ParseClassificationTag(tags);
        }

        public bool UpdateNxlFileRights(string nxlPath,
            List<FileRights> rights,
            WaterMarkInfo watermark, Expiration expiration,
            UserSelectTags tags)
        {
            // prepare for adhoc-rights.
            List<int> r = new List<int>(rights == null ? 0 : rights.Count);
            if (rights != null)
            {
                foreach (var i in rights)
                {
                    r.Add((int)i);
                }
            }

            // pass tags as json string.
            string jTags = tags.ToJsonString();

            var rt = Boundary.SDWL_User_UpdateNxlFileRights(
                hUser,
                nxlPath,
                r.ToArray(), r.Count,
                watermark, expiration,
                jTags);

            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("UpdateNxlFileRights", rt);
            }

            return true;
        }

        public bool SharedWithMeReshareFile(string transactionId, string transactionCode, string[] emails)
        {
            if (string.IsNullOrEmpty(transactionId) || string.IsNullOrEmpty(transactionCode))
            {
                // params are needed.
                return false;
            }
            if (emails == null || emails.Length == 0)
            {
                // no share target exists at all.
                return false;
            }

            StringBuilder emailsEncapulsed = new StringBuilder();
            int size = emails.Length;
            for (int i = 0; i < size; i++)
            {
                emailsEncapulsed.Append(emails[i]);
                if (i != size - 1)
                {
                    // need take comma as each email's sperator if there is one more emails.
                    emailsEncapulsed.Append(",");
                }
            }

            var rt = Boundary.SDWL_User_SharedWitheMeReshareFile(
                hUser,
                transactionId, transactionCode, emailsEncapulsed.ToString());

            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("SharedWithMeReshareFile", rt);
            }

            return true;
        }

        public bool ProjectNxlFileShare2PersonResetSourcePath(string nxlFilePath, string sourcePath)
        {
            if (string.IsNullOrEmpty(nxlFilePath) || string.IsNullOrEmpty(sourcePath))
            {
                return false;
            }

            var rt = Boundary.SDWL_User_ResetSourcePath(hUser, nxlFilePath, sourcePath);

            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("ProjectNxlFileShare2PersonResetSourcePath", rt);
            }

            return true;
        }

        public void EvaulateNxlFileRights(string filePath, out Dictionary<FileRights, List<WaterMarkInfo>> rightsAndWatermarks, bool doOwnerCheck=false)
        {
            rightsAndWatermarks = new Dictionary<FileRights, List<WaterMarkInfo>>();
            IntPtr pArray;
            int pArrSize;
            var rt = Boundary.SDWL_User_EvaulateNxlFileRights(hUser,
                filePath, out pArray, out pArrSize, doOwnerCheck);

            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("EvaulateNxlFileRights", rt);
            }

            if (pArrSize > 0)
            {
                IntPtr cur = pArray;
                int structSize = Marshal.SizeOf(typeof(InternalCentralRights));
                for (int i = 0; i < pArrSize; i++)
                {
                    InternalCentralRights rights = (InternalCentralRights)Marshal.PtrToStructure(cur, typeof(InternalCentralRights));

                    Int64 wmSize = rights.wmSize;
                    if (wmSize > 0)
                    {
                        IntPtr wmCur = rights.waterMarks;
                        if (wmCur == IntPtr.Zero)
                        {
                            return;
                        }
                        int wmStructSize = Marshal.SizeOf(typeof(WaterMarkInfo));

                        List<WaterMarkInfo> waterMarks = new List<WaterMarkInfo>();
                        for (int j = 0; j < wmSize; j++)
                        {
                            WaterMarkInfo waterMarkInfo = (WaterMarkInfo)Marshal.PtrToStructure(wmCur, typeof(WaterMarkInfo));
                            waterMarks.Add(waterMarkInfo);
                            wmCur += wmStructSize;
                        }
                        rightsAndWatermarks.Add((FileRights)rights.rights, waterMarks);

                        //Release WaterMarkInfo arr in com mem.
                        Marshal.FreeCoTaskMem(rights.waterMarks);
                    }
                    else
                    {
                        rightsAndWatermarks.Add((FileRights)rights.rights, null);
                    }
                    cur += structSize;
                }

                //Release InternalCentralRights arr in com mem.
                Marshal.FreeCoTaskMem(pArray);
            }
        }


        public void GetFileRightsFromCentralPolicyByTenant(string tenantName, UserSelectTags tags,
            out Dictionary<FileRights, List<WaterMarkInfo>> rightsAndWatermarks)
        {
            rightsAndWatermarks = new Dictionary<FileRights, List<WaterMarkInfo>>();

            string rawTags = tags.ToJsonString();

            IntPtr pArray;
            int pArrSize;
            var rt = Boundary.SDWL_User_GetFileRightsFromCentralPolicyByTenant(hUser,
                tenantName, rawTags,
                out pArray, out pArrSize);

            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("GetFileRightsFromCentralPolicyByTenant", rt);
            }

            if (pArrSize > 0)
            {
                IntPtr cur = pArray;
                int structSize = Marshal.SizeOf(typeof(InternalCentralRights));
                for (int i = 0; i < pArrSize; i++)
                {
                    InternalCentralRights rights = (InternalCentralRights)Marshal.PtrToStructure(cur, typeof(InternalCentralRights));

                    Int64 wmSize = rights.wmSize;
                    if (wmSize > 0)
                    {
                        IntPtr wmCur = rights.waterMarks;
                        if (wmCur == IntPtr.Zero)
                        {
                            return;
                        }
                        int wmStructSize = Marshal.SizeOf(typeof(WaterMarkInfo));

                        List<WaterMarkInfo> waterMarks = new List<WaterMarkInfo>();
                        for (int j = 0; j < wmSize; j++)
                        {
                            WaterMarkInfo waterMarkInfo = (WaterMarkInfo)Marshal.PtrToStructure(wmCur, typeof(WaterMarkInfo));
                            waterMarks.Add(waterMarkInfo);
                            wmCur += wmStructSize;
                        }
                        rightsAndWatermarks.Add((FileRights)rights.rights, waterMarks);

                        //Release WaterMarkInfo arr in com mem.
                        Marshal.FreeCoTaskMem(rights.waterMarks);
                    }
                    else
                    {
                        rightsAndWatermarks.Add((FileRights)rights.rights, null);
                    }
                    cur += structSize;
                }

                //Release InternalCentralRights arr in com mem.
                Marshal.FreeCoTaskMem(pArray);
            }
        }

        public void GetFileRightsFromCentalPolicyByProjectId(int projectId, UserSelectTags tags,
            out Dictionary<FileRights, List<WaterMarkInfo>> rightsAndWatermarks)
        {
            rightsAndWatermarks = new Dictionary<FileRights, List<WaterMarkInfo>>();

            if (tags == null)
            {
                throw new ArgumentNullException("UserSelectTags is null when GetFileRightsFromCentalPolicyByProjectId");
            }

            string rawTags = tags.ToJsonString();
            IntPtr pArray;
            int pArrSize;
            var rt = Boundary.SDWL_User_GetFileRightsFromCentralPolicyByProjectID(hUser,
                (UInt32)projectId, rawTags,
                out pArray, out pArrSize);

            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("GetFileRightsFromCentalPolicyByProjectId", rt);
            }

            if (pArrSize > 0)
            {
                IntPtr cur = pArray;
                int structSize = Marshal.SizeOf(typeof(InternalCentralRights));
                for (int i = 0; i < pArrSize; i++)
                {
                    InternalCentralRights rights = (InternalCentralRights)Marshal.PtrToStructure(cur, typeof(InternalCentralRights));

                    Int64 wmSize = rights.wmSize;
                    if (wmSize > 0)
                    {
                        IntPtr wmCur = rights.waterMarks;
                        if (wmCur == IntPtr.Zero)
                        {
                            return;
                        }
                        int wmStructSize = Marshal.SizeOf(typeof(WaterMarkInfo));

                        List<WaterMarkInfo> waterMarks = new List<WaterMarkInfo>();
                        for (int j = 0; j < wmSize; j++)
                        {
                            WaterMarkInfo waterMarkInfo = (WaterMarkInfo)Marshal.PtrToStructure(wmCur, typeof(WaterMarkInfo));
                            waterMarks.Add(waterMarkInfo);
                            wmCur += wmStructSize;
                        }
                        rightsAndWatermarks.Add((FileRights)rights.rights, waterMarks);

                        //Release WaterMarkInfo arr in com mem.
                        Marshal.FreeCoTaskMem(rights.waterMarks);
                    }
                    else
                    {
                        rightsAndWatermarks.Add((FileRights)rights.rights, null);
                    }
                    cur += structSize;
                }

                //Release InternalCentralRights arr in com mem.
                Marshal.FreeCoTaskMem(pArray);
            }
        }

        public bool GetAssmblyPathsFromModelFile(string filepath, out List<string> paths,
            out List<string> missingpaths, out UInt32 pmissingCounts)
        {

            paths = new List<string>();
            missingpaths = new List<string>();
            pmissingCounts = 0;

            if (string.IsNullOrEmpty(filepath))
            {
                return false;
            }
            string pathstr;
            string missingpathstr;
            UInt32 missingpathCounts;
            var rt = Boundary.HOOPS_EXCHANGE_GetAssemblyPathsFromModelFile(filepath, out pathstr, out missingpathstr, out missingpathCounts);
            if (rt != 0)
            {
                return false;
            }
            paths = DataConvert.ParseRecipents(pathstr);
            missingpaths = DataConvert.ParseRecipents(missingpathstr);
            pmissingCounts = missingpathCounts;
            return true;
        }

        /// <param name="srcplainfile">native file path which need to be protected</param>
        /// <param name="originalnxlfile">the original NXL file which the new file will use the same rights to protect.
        ///    it can be a NXL file in non-RPM folder, it can be the NXL file with/without NXL extension in RPM folder
        /// </param>
        /// <param name="output">generated file path</param>
        public void ProtectFileFrom(string srcplainfile, string originalnxlfile, out string output)
        {
            var rt = Boundary.SDWL_User_ProtectFileFrom(hUser, srcplainfile, originalnxlfile, out output);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_User_ProtectFileFrom", rt);
            }
        }

        #endregion // User Base

        #region User MyVault

        public void MyVaultFileIsExist(string pathid, out bool bExist)
        {
            var rt = Boundary.SDWL_User_MyVaultFileIsExist(hUser, pathid, out bExist);
            if(rt != 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_User_MyVaultFileIsExist", rt);
            }
        }

        public string MyVaultGetNxlFileHeader(string pathid, string targetFolder)
        {
            string outPath;
            var rt = Boundary.SDWL_User_MyVaultGetNxlFileHeader(hUser, pathid, targetFolder, out outPath);
            if(rt != 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_User_MyVaultGetNxlFileHeader", rt);
            }

            return outPath;
        }

        public void UploadMyVaultFile(string nxlPath, string sourcePath, string recipents = "", string comments = "", bool bOverwrite = false)
        {
            // SDK uploadFile interface is using comma if contains multiple emails -- fix bug 55808.
            string emails = recipents.Contains(";") ? recipents.Replace(';', ',') : recipents;

            uint rt = Boundary.SDWL_User_UploadMyVaultFile(hUser, nxlPath, sourcePath, emails, comments, bOverwrite);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_User_UploadFile", rt,
                    RmSdkExceptionDomain.Rest_MyVault, RmSdkRestMethodKind.Upload);
            }
        }

        public MyVaultFileInfo[] ListMyVaultFiles()
        {
            IntPtr pArray;
            int size;
            var rt = Boundary.SDWL_User_ListMyVaultAllFiles(
                hUser, "fileName", "", out pArray, out size);
            if (rt != 0 || size < 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_User_ListMyVaultFiles", rt,
                    RmSdkExceptionDomain.Rest_MyVault, RmSdkRestMethodKind.List);
            }
            if (size == 0)
            {
                return new MyVaultFileInfo[0];
            }

            // parse com mem to extract the array
            // marshal unmarshal
            MyVaultFileInfo[] pInfo = new MyVaultFileInfo[size];
            int structSize = Marshal.SizeOf(typeof(MyVaultFileInfo));
            IntPtr cur = pArray;
            for (int i = 0; i < size; i++)
            {
                pInfo[i] = (MyVaultFileInfo)Marshal.PtrToStructure(cur, typeof(MyVaultFileInfo));
                cur += structSize;
            }
            Marshal.FreeCoTaskMem(pArray);
            return pInfo;
        }

        public void DownloadMyVaultFile(string rmsPathId, ref string downlaodPath, DownlaodMyVaultFileType type)
        {
            string outpath;
            var rt = Boundary.SDWL_User_DownloadMyVaultFiles(
                hUser, rmsPathId, downlaodPath, (int)type, out outpath);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_User_DownloadMyVaultFiles", rt,
                    RmSdkExceptionDomain.Rest_MyVault, RmSdkRestMethodKind.Download);
            }
            downlaodPath = outpath;
        }

        public void DownloadMyVaultPartialFile(string rmsPathId, ref string downlaodPath, DownlaodMyVaultFileType type)
        {
            string outpath;
            var rt = Boundary.SDWL_User_DownloadMyVaultPartialFiles(
                hUser, rmsPathId, downlaodPath, (int)type, out outpath);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_User_DownloadMyVaultPartialFiles", rt,
                    RmSdkExceptionDomain.Rest_MyVault, RmSdkRestMethodKind.Download);
            }
            downlaodPath = outpath;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 8)]
        public struct InternalMyVaultMetaData
        {
            public string name;
            public string fileLink;

            public Int64 lastModified;
            public Int64 protectedOn;
            public Int64 sharedOn;
            public Int64 isShared;
            public Int64 isDeleted;
            public Int64 isRevoked;
            public Int64 protectionType;
            public Int64 isOwner;
            public Int64 isNxl;

            public string recipents;
            public string pathDisplay;
            public string pathId;
            public string tags;

            public Expiration expiration;
        }

        public MyVaultMetaData GetMyVaultFileMetaData(string nxlPath, string pathId)
        {
            var rt = Boundary.SDWL_User_GetMyVaultFileMetaData(hUser, nxlPath, pathId,
                out InternalMyVaultMetaData md);

            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("GetMyVaultFileMetaData", rt);
            }

            return DataConvert.Convert(md);
        }

        public bool MyVaultShareFile(string nxlLocalPath, string[] recipents,
            string repositoryId, string fileName,
            string filePathId, string filePath,
            string comments)
        {
            if (string.IsNullOrEmpty(nxlLocalPath))
            {
                return false;
            }
            if (recipents == null || recipents.Length == 0)
            {
                return false;
            }

            StringBuilder recipentsBuilder = new StringBuilder();
            for (int i = 0; i < recipents.Length; i++)
            {
                var e = recipents[i];
                // filter out empty email.
                if (string.IsNullOrEmpty(e))
                {
                    continue;
                }
                recipentsBuilder.Append(e);
                if (i != recipents.Length - 1)
                {
                    recipentsBuilder.Append(",");
                }
            }

            if (string.IsNullOrEmpty(recipentsBuilder.ToString()))
            {
                // empty recipents recieved.
                return false;
            }

            var rt = Boundary.SDWL_User_MyVaultShareFile(hUser, nxlLocalPath,
                recipentsBuilder.ToString(), repositoryId,
                fileName, filePathId, filePath,
                comments);

            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("MyVaultShareFile", rt);
            }

            return true;
        }

        #endregion // User MyVault

        #region User MyDrive
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 8)]
        public struct MyDriveFileInfo
        {
            public string pathId;
            public string pathDisplay;
            public string name;
            public ulong lastModified;
            public ulong size;
            public uint isFolder;
        }

        public MyDriveFileInfo[] ListMyDriveFiles(string pathid)
        {
            IntPtr pArray;
            uint size;
            var rt = Boundary.SDWL_User_MyDriveListFiles(hUser, pathid, out pArray, out size);
            if (rt != 0 || size < 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_User_MyDriveListFiles", rt,
                    RmSdkExceptionDomain.Rest_MyDrive, RmSdkRestMethodKind.List);
            }
            if (size == 0)
            {
                return new MyDriveFileInfo[0];
            }
            // parse com mem to extract the array
            // marshal unmarshal
            MyDriveFileInfo[] pInfo = new MyDriveFileInfo[size];
            int structSize = Marshal.SizeOf(typeof(MyDriveFileInfo));
            IntPtr cur = pArray;
            for (int i = 0; i < size; i++)
            {
                pInfo[i] = (MyDriveFileInfo)Marshal.PtrToStructure(cur, typeof(MyDriveFileInfo));
                cur += structSize;
            }
            Marshal.FreeCoTaskMem(pArray);
            return pInfo;
        }

        public void MyDriveDownloadFile(string pathid, ref string targetPath)
        {
            string outPath;
            uint rt = Boundary.SDWL_User_MyDriveDownloadFile(hUser, pathid, targetPath, out outPath);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_User_MyDriveDownloadFile", rt);
            }
            targetPath = outPath;
        }

        public void MyDriveUploadFile(string fileLocalPath, string destFolder, bool overwrite = false)
        {
            uint rt = Boundary.SDWL_User_MyDriveUploadFile(hUser, fileLocalPath, destFolder, overwrite);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_User_MyDriveUploadFile", rt, RmSdkExceptionDomain.Rest_MyDrive,
                     RmSdkRestMethodKind.Upload);
            }
        }

        public bool MyDriveCreateFolder(string name, string parentFolder)
        {
            uint rt = Boundary.SDWL_User_MyDriveCreateFolder(hUser,name, parentFolder);
            return rt == 0;
        }

        public bool MyDriveDeleteItem(string pathId)
        {
            uint rt = Boundary.SDWL_User_MyDriveDeleteItem(hUser, pathId);
            return rt == 0;
        }

        #endregion // User MyDrive

        #region User workspace

        public void WorkSpaceFileIsExist(string pathid, out bool bExist)
        {
            var rt = Boundary.SDWL_User_WorkSpaceFileIsExist(hUser, pathid, out bExist);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_User_WorkSpaceFileIsExist", rt);
            }
        }

        public string WorkSpaceGetNxlFileHeader(string pathid, string targetFolder)
        {
            string outPath;
            var rt = Boundary.SDWL_User_WorkSpaceGetNxlFileHeader(hUser, pathid, targetFolder, out outPath);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_User_WorkSpaceGetNxlFileHeader", rt);
            }

            return outPath;
        }

        public void WorkSpaceFileOverwrite(string parentPathId, string nxlFilePath, bool bOverwrite = false)
        {
            var rt = Boundary.SDWL_User_WorkSpaceFileOverwrite(hUser, parentPathId, nxlFilePath, bOverwrite);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_User_WorkSpaceFileOverwrite", rt, RmSdkExceptionDomain.Rest_WorkSpace,
                     RmSdkRestMethodKind.Upload);
            }
        }


        /// <summary>
        /// Protect file to workSpace
        /// WorkSpace can look as the remote repository of system bucket!
        /// </summary>
        /// <param name="projectId">System bucket id</param>
        /// <param name="filePath"></param>
        /// <param name="rights"></param>
        /// <param name="waterMark"></param>
        /// <param name="expiration"></param>
        /// <param name="tags"></param>
        /// <returns></returns>
        public string ProtectFileToWorkSpace(int projectId, string filePath, List<FileRights> rights,
            WaterMarkInfo waterMark, Expiration expiration, UserSelectTags tags)
        {
            return ProtectFileToProject(projectId, filePath, rights, waterMark, expiration, tags);
        }

        public void DownloadWorkSpaceFile(string rmsPathId, ref string downloadPath, DownlaodWorkSpaceFileType type)
        {
            string outPath;
            var rt = Boundary.SDWL_User_DownloadWorkSpaceFile(hUser, rmsPathId, downloadPath, (int)type, out outPath);
            if(rt != 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_User_DownloadWorkSpaceFile", rt,
                     RmSdkExceptionDomain.Rest_WorkSpace, RmSdkRestMethodKind.Download);
            }

            downloadPath = outPath;
        }

        public void DownloadWorkSpacePartialFile(string rmsPathId, ref string downloadPath, DownlaodWorkSpaceFileType type = DownlaodWorkSpaceFileType.ForVeiwer)
        {
            string outPath;
            var rt = Boundary.SDWL_User_DownloadWorkSpacePartialFile(hUser, rmsPathId, downloadPath, (int)type, out outPath);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_User_DownloadWorkSpacePartialFile", rt,
                     RmSdkExceptionDomain.Rest_WorkSpace, RmSdkRestMethodKind.Download);
            }

            downloadPath = outPath;
        }

        public WorkSpaceFileInfo[] ListWorkSpaceAllFiles(string pathId)
        {
            IntPtr pArray;
            int size;
            var rt = Boundary.SDWL_User_ListWorkSpaceAllFiles(hUser, pathId, "name", "", out pArray, out size);
            if (rt != 0 || size < 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_User_ListWorkSpaceAllFiles", rt);
            }
            if (size == 0)
            {
                return new WorkSpaceFileInfo[0];
            }

            // marsh & unmarsh
            WorkSpaceFileInfo[] pInfo = new WorkSpaceFileInfo[size];
            int structSize = Marshal.SizeOf(typeof(WorkSpaceFileInfo));
            IntPtr cur = pArray;
            for (int i = 0; i < size; i++)
            {
                pInfo[i] = (WorkSpaceFileInfo)Marshal.PtrToStructure(cur, typeof(WorkSpaceFileInfo));
                cur += structSize;
            }
            Marshal.FreeCoTaskMem(pArray);

            return pInfo;
        }

        public bool UpdateWorkSpaceNxlFileRights(string nxlFilePath, string name, string parentPathId,
            List<FileRights> rights, WaterMarkInfo watermark, Expiration expiration, UserSelectTags tags)
        {
            // prepare rights
            List<int> r = new List<int>(rights == null ? 0 : rights.Count);
            if(null != rights)
            {
                foreach(var i in rights)
                {
                    r.Add((int)i);
                }
            }

            string jtags = tags.ToJsonString();
            var rt = Boundary.SDWL_User_UpdateWorkSpaceNxlFileRights(
                hUser,
                nxlFilePath,
                name,
                parentPathId,
                r.ToArray(),
                r.Count,
                watermark,
                expiration,
                jtags);

            if(rt != 0)
            {
                ExceptionFactory.BuildThenThrow("UpdateWorkSpaceNxlFileRights", rt);
            }

            return true;
        }

        // Now ignore this, maybe sdk inner use.
        public void ClassifyWorkSpaceFile(string nxlpath, string name, string parentPathId, string tags)
        {
            var rt = Boundary.SDWL_User_ClassifyWorkSpaceFile(hUser, nxlpath, name, parentPathId, tags);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("ClassifyWorkSpaceFile", rt);
            }
        }

        public void UploadWorkSpaceEditedFile(string destFolder, string nxlFilePath, bool bOverwrite)
        {
            var rt = Boundary.SDWL_User_UploadWorkSpaceFile(hUser, destFolder, nxlFilePath, bOverwrite);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_User_UploadWorkSpaceFile", rt, RmSdkExceptionDomain.Rest_WorkSpace,
                     RmSdkRestMethodKind.Upload);
            }
        }

        public void UploadWorkSpaceFile(string destFolder, string nxlFilePath, bool bOverwrite)
        {
            //var rt = Boundary.SDWL_User_UploadWorkSpaceFile(hUser, destFolder, nxlFilePath, bOverwrite);
            //if (rt != 0)
            //{
            //    ExceptionFactory.BuildThenThrow("SDWL_User_UploadWorkSpaceFile", rt, RmSdkExceptionDomain.Rest_WorkSpace,
            //         RmSdkRestMethodKind.Upload);
            //}
            WorkSpaceFileOverwrite(destFolder, nxlFilePath, bOverwrite);
        }

        public WorkspaceMetaData GetWorkSpaceFileMetadata(string pathId)
        {
            var rt = Boundary.SDWL_User_GetWorkSpaceFileMetadata(hUser, pathId, out InternalWorkSpaceMetaData md);
            if(rt != 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_User_GetWorkSpaceFileMetadata", rt);
            }

            return DataConvert.ConvertMetaData(md);
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 8)]
        public struct InternalWorkSpaceMetaData
        {
            public string name;
            public string fileLink;

            public Int64 lastModified;
            public Int64 protectedOn;
            public Int64 sharedOn;
            public Int64 isShared;
            public Int64 isDeleted;
            public Int64 isRevoked;
            public Int64 protectionType;
            public Int64 isOwner;
            public Int64 isNxl;

            public string recipents;
            public string pathDisplay;
            public string pathId;
            public string tags;

            public Expiration expiration;
        }

        #endregion // User workspace

        #region User Shared WorkSpace

        public string ProtectFileToSharedSpace(int id, string filePath, List<FileRights> rights,
           WaterMarkInfo waterMark, Expiration expiration, UserSelectTags tags)
        {
            return ProtectFileToProject(id, filePath, rights, waterMark, expiration, tags);
        }

        public SharedWorkspaceFileInfo[] ListSharedWorkspaceAllFiles(string repoid, string path)
        {
            IntPtr pArray;
            int size;
            var rt = Boundary.SDWL_User_ListSharedWorkspaceAllFiles(hUser, repoid, "name", "", path, out pArray, out size);
            if (rt != 0 || size < 0)
            {
                ExceptionFactory.BuildThenThrow("ListSharedWorkspaceAllFiles", rt);
            }
            if (size == 0)
            {
                return new SharedWorkspaceFileInfo[0];
            }

            // marsh & unmarsh
            SharedWorkspaceFileInfo[] pInfo = new SharedWorkspaceFileInfo[size];
            int structSize = Marshal.SizeOf(typeof(SharedWorkspaceFileInfo));
            IntPtr cur = pArray;
            for(int i = 0; i < size; i++)
            {
                pInfo[i] = (SharedWorkspaceFileInfo)Marshal.PtrToStructure(cur, typeof(SharedWorkspaceFileInfo));
                cur += structSize;
            }
            Marshal.FreeCoTaskMem(pArray);

            return pInfo;
        }

        /// <summary>
        /// Upload protected file to shared workspace.
        /// Note: There is no immediate requirement to upload non-NXL files or to upload and protect non-NXL files.
        /// </summary>
        /// <param name="repoid">repository id</param>
        /// <param name="destFolder">parent dest path</param>
        /// <param name="nxlFilePath">the local file to upload</param>
        /// <param name="uploadType">
        /// 0: copy and upload operation, System will re-encrypt the file, change the DUID, store it in the shared workspace
        /// 1: should not happen, no upload with type = 1 will be allowed in Server.RMD must change this from 1 to 2 to preserve the current behaviour.Temporarily type 1 will be internally mapped in server to type 2. 
        /// 2: the file was downloaded for offline use, edited and is being uploaded back.Server checks for EDIT rights and permit/deny the operation.
        /// 3: copy and upload of tenant (the old 'system bucket') file, RMS will re-encrypt the file, change the DUID, and store it in the shared workspace. 
        /// 4: upload the file and store it in the shared workspace without re-encryption.
        /// Note: Type will be defaulted to 3, if type is not specified. The type parameter is not needed for native files
        /// </param>
        /// <param name="bOverwrite">If set to 'true' file will be replaced if a file with same file path exists</param>
        public void UploadSharedWorkSpaceFile(string repoid, string destFolder, string nxlFilePath, int uploadType, bool bOverwrite)
        {
            var rt = Boundary.SDWL_User_UploadSharedWorkspaceFile(hUser, repoid, destFolder, nxlFilePath, uploadType, bOverwrite);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("UploadSharedWorkSpaceFile", rt, 
                    RmSdkExceptionDomain.Rest_SharedWorkSpace, RmSdkRestMethodKind.Upload);
            }
        }

        /// <summary>
        /// Download the file from shared workspace
        /// </summary>
        /// <param name="repoid">repository id</param>
        /// <param name="path">the file path</param>
        /// <param name="downloadPath">the local path to download to</param>
        /// <param name="type">
        /// 0 for normal download. This returns a copy of the file with a new DUID. The token group is unchanged. 
        /// 1 for download for viewer(same with forViewer:true in v1). The DUID and token group do not change.
        /// 2 for download for offline.The DUID and token group do not change. 
        /// 3 is similar to 0, it returns a file with a new DUID, encrypted with a token from the tenant TG(the old 'system bucket').
        /// </param>
        public void DownloadSharedWorkSpaceFile(string repoid, string path, ref string downloadPath, int type, bool isNxl)
        {
            string outPath;
            var rt = Boundary.SDWL_User_DownloadSharedWorkspaceFile(hUser, repoid, path, downloadPath, type, isNxl, out outPath);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_User_DownloadSharedWorkspaceFile", rt);
            }

            downloadPath = outPath;
        }

        public void IsSharedWorkSpaceFileExist(string repoid, string path, out bool bExist)
        {
            var rt = Boundary.SDWL_User_IsSharedWorkspaceFileExist(hUser, repoid, path, out bExist);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_User_IsSharedWorkspaceFileExist", rt);
            }
        }

        public string GetSharedWorkSpaceNxlFileHeader(string repoid, string path, string targetFolder)
        {
            string outPath;
            var rt = Boundary.SDWL_User_GetSharedWorkspaceNxlFileHeader(hUser, repoid, path, targetFolder, out outPath);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_User_GetSharedWorkspaceNxlFileHeader", rt);
            }

            return outPath;
        }

        #endregion // User Shared WorkSpace

        #region User Project

        public void ProjectFileIsExist(int projectId, string pathid, out bool bExist)
        {
            var rt = Boundary.SDWL_User_ProjectFileIsExist(hUser, projectId, pathid, out bExist);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_User_ProjectFileIsExist", rt);
            }
        }

        public string ProjectGetNxlFileHeader(int projectId, string pathid, string targetFolder)
        {
            string outPath;
            var rt = Boundary.SDWL_User_ProjectGetNxlFileHeader(hUser, projectId, pathid, targetFolder, out outPath);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_User_ProjectGetNxlFileHeader", rt);
            }

            return outPath;
        }

        public void ProjectFileOverwrite(int projectId, string parentPathId, string nxlFilePath, bool bOverwrite = false)
        {
            var rt = Boundary.SDWL_User_ProjectFileOverwrite(hUser, projectId, parentPathId, nxlFilePath, bOverwrite);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_User_ProjectFileOverwrite", rt, RmSdkExceptionDomain.Rest_MyProject,
                     RmSdkRestMethodKind.Upload);
            }
        }


        public string ProtectFileToProject(int projectId, string path, List<FileRights> rights,
          WaterMarkInfo waterMark, Expiration expiration, UserSelectTags tags)
        {
            List<int> r = new List<int>(rights.Count);

            foreach (var i in rights)
            {
                r.Add((int)i);
            }
            string outpath;
            uint rt = Boundary.SDWL_User_ProtectFileToProject(
                projectId, hUser, path, r.ToArray(),
                r.Count, waterMark, expiration,
                tags.ToJsonString(), out outpath);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_User_ProtectFileToProject", rt);
            }

            //outpath = ChangeFileName(Path.GetFileNameWithoutExtension(path), outpath);

            return ReNameFilePath(outpath);
        }

        public ProjectInfo[] UpdateProjectInfo()
        {
            // as sdk required, sync first and then to get the latest info
            uint rt = Boundary.SDWL_User_GetListProjtects(hUser, 1, 1000, "name", ProjectFilterType.All);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_User_GetListProjtects", rt);
            }

            IntPtr pArray;
            int size;
            rt = Boundary.SDWL_User_GetProjectsInfo(hUser, out pArray, out size);
            if (rt != 0 || size < 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_User_GetProjectsInfo", rt);
            }
            if (size == 0)
            {
                return new ProjectInfo[0];
            }
            // marshal & unmarshal
            ProjectInfo[] pInfo = new ProjectInfo[size];
            int structSize = Marshal.SizeOf(typeof(ProjectInfo));
            IntPtr cur = pArray;
            for (int i = 0; i < size; i++)
            {
                pInfo[i] = (ProjectInfo)Marshal.PtrToStructure(cur, typeof(ProjectInfo));
                cur += structSize;
            }
            Marshal.FreeCoTaskMem(pArray);

            return pInfo;
        }

        public bool IsEnabledAdhocForProject(string ProjectTenandId)
        {
            bool isEnabled = false;
            // call sdk
            uint rt = Boundary.SDWL_User_CheckProjectEnableAdhoc(hUser, ProjectTenandId, ref isEnabled);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("IsEnabledAdhocForProject", rt);
            }

            return isEnabled;
        }

        public int GetSystemProjectId()
        {
            int sysprojectid = 0;
            string sysprojecttenantid = "";
            // call sdk
            uint rt = Boundary.SDWL_User_CheckSystemProject(hUser, "", ref sysprojectid, out sysprojecttenantid);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("GetSystemProjectId", rt);
            }

            return sysprojectid;
        }

        public string GetSystemProjectTenantId()
        {
            int sysprojectid = 0;
            string sysprojecttenantid = "";
            // call sdk
            uint rt = Boundary.SDWL_User_CheckSystemProject(hUser, "", ref sysprojectid, out sysprojecttenantid);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("GetSystemProjectId", rt);
            }

            return sysprojecttenantid;
        }

        public bool GetIsDeleteSource(string ProjectTenandId = "")
        {
            bool isDeleteSource = false;
            // call sdk
            uint rt = Boundary.SDWL_User_CheckInPlaceProtection(hUser, "", ref isDeleteSource);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("GetIsDeleteSource", rt);
            }

            return isDeleteSource;
        }

        public ProjectFileInfo[] ListProjectsFiles(int projectId, string pathId)
        {
            IntPtr pArray;
            int size;
            uint rt = Boundary.SDWL_User_ProjectListAllFiles(hUser, projectId, "name", pathId, "", out pArray, out size);
            if (rt != 0 || size < 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_User_ProjectListFiles", rt);
            }
            if (size == 0)
            {
                return new ProjectFileInfo[0];
            }
            // marshal & unmarshal
            ProjectFileInfo[] pInfo = new ProjectFileInfo[size];
            int structSize = Marshal.SizeOf(typeof(ProjectFileInfo));
            IntPtr cur = pArray;
            for (int i = 0; i < size; i++)
            {
                pInfo[i] = (ProjectFileInfo)Marshal.PtrToStructure(cur, typeof(ProjectFileInfo));
                cur += structSize;
            }
            Marshal.FreeCoTaskMem(pArray);

            return pInfo;
        }

        public void UploadProjectFile(int projectId, string rmsParentFolder, string nxlFilePath, bool userConfirmedFileOverwrite = false)
        {
            // ProjectFileOverwrite(projectId, rmsParentFolder, nxlFilePath, overwrite);
            UploadProjectFileEx(projectId, rmsParentFolder, nxlFilePath, 4, userConfirmedFileOverwrite);
        }

        public void UploadEditedProjectFile(int projectId, string rmsParentFolder, string nxlFilePath)
        {
            UploadProjectFileEx(projectId, rmsParentFolder, nxlFilePath, 2, false);
        }

        /// <summary>
        /// New interface for project upload
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="rmsParentFolder"></param>
        /// <param name="nxlFilePath"></param>
        /// <param name="uploadType">
        /// 0: copy and upload operation, System will re-encrypt the file and change the DUID, store it in the project
        /// 1: should not happen, no upload with type = 1 will be allowed in Server.RMD must change this from 1 to 2 to preserve the current behaviour.Temporarily type 1 will be internally mapped in server to type 2. 
        /// 2: the file was downloaded for offline use, edited and is being uploaded back.Server checks for EDIT rights and permit/deny the operation.
        /// 3: copy and upload of system bucket file/ project token group file, it will re-encrypt the file and store it in the project.
        /// 4: upload project token group file, it will upload the file and store it in the project.
        /// </param>
        /// <param name="userConfirmedFileOverwrite"></param>
        /// Note: Please note that both type=2 and userConfirmedFileOverwrite=true cannot be set in the same request
        private void UploadProjectFileEx(int projectId, string rmsParentFolder, string nxlFilePath, 
            int uploadType, bool userConfirmedFileOverwrite = false)
        {
            uint rt = Boundary.SDWL_User_ProjectUploadFileEx(hUser, projectId, rmsParentFolder, nxlFilePath, uploadType, userConfirmedFileOverwrite);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("UploadProjectFileEx", rt, RmSdkExceptionDomain.Rest_MyProject,
                     RmSdkRestMethodKind.Upload);
            }
        }


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 8)]
        private struct Internal_ProjectClassification
        {
            public string name;
            public int isMultiSelect;
            public int isMandatory;
            public string labels;
            public string defaults;
        }

        public ProjectClassification[] GetProjectClassification(string tenantid)
        {
            IntPtr pArray;
            int size;
            uint rt = Boundary.SDWL_User_ProjectClassifacation(hUser, tenantid, out pArray, out size);
            if (rt != 0 || size < 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_User_ProjectClassifacation", rt);
            }
            if (size == 0)
            {
                return new ProjectClassification[0];
            }

            Internal_ProjectClassification[] pPC = new Internal_ProjectClassification[size];
            int structSize = Marshal.SizeOf(typeof(Internal_ProjectClassification));
            IntPtr cur = pArray;
            for (int i = 0; i < size; i++)
            {
                pPC[i] = (Internal_ProjectClassification)Marshal.PtrToStructure(cur, typeof(Internal_ProjectClassification));
                cur += structSize;
            }
            Marshal.FreeCoTaskMem(pArray);

            //pPC to Outer Struct;
            ProjectClassification[] o = new ProjectClassification[size];
            for (int i = 0; i < size; i++)
            {
                o[i].name = pPC[i].name;
                o[i].isMultiSelect = pPC[i].isMultiSelect == 1;
                o[i].isMandatory = pPC[i].isMandatory == 1;
                o[i].labels = new Dictionary<String, bool>();
                //labels and defautls
                string[] l = pPC[i].labels.Split(new char[] { ';' });
                string[] d = pPC[i].defaults.Split(new char[] { ';' });
                for (int j = 0; j < l.Length; j++)
                {
                    if (l[j].Length > 0)
                    {
                        o[i].labels.Add(l[j], d[j].Equals("1"));
                    }
                }
            }
            return o;
        }

        // for win use, we only supprot offline , not the view, so set bViewOnly == false
        public void DownlaodProjectFile(int projectId, string pathId, ref string destFolder,
            ProjectFileDownloadType type = ProjectFileDownloadType.ForOffline)
        {
            string outpath;
            uint rt = Boundary.SDWL_User_ProjectDownloadFile(
                hUser, projectId, pathId, destFolder, (int)type, out outpath);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_User_ProjectDownloadFile", rt);
            }

            destFolder = outpath;
        }

        public void DownlaodProjectPartialFile(int projectId, string pathId, ref string destFolder, UInt16 type = 1)
        {
            string outpath;
            uint rt = Boundary.SDWL_User_ProjectDownloadPartialFile(
                hUser, projectId, pathId, destFolder, type, out outpath);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_User_ProjectDownloadPartialFile", rt);
            }

            destFolder = outpath;
        }

        public bool UpdateProjectNxlFileRights(string nxlLocalPath, UInt32 projectId, string fileName, string parentPathId,
           List<FileRights> rights, WaterMarkInfo waterMark, Expiration expiration, UserSelectTags tags)
        {
            // sanity check.
            if (string.IsNullOrEmpty(nxlLocalPath))
            {
                return false;
            }

            if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(parentPathId))
            {
                // require necessary params.
                return false;
            }

            // prepare for adhoc-rights.
            List<int> r = new List<int>(rights == null ? 0 : rights.Count);
            if (rights != null)
            {
                foreach (var i in rights)
                {
                    r.Add((int)i);
                }
            }

            // pass tags as json string.
            string jTags = tags.ToJsonString();

            var rt = Boundary.SDWL_User_UpdateProjectNxlFileRights(
                hUser, nxlLocalPath, projectId, fileName, parentPathId,
                r.ToArray(), r.Count,
                waterMark, expiration, jTags);

            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("UpdateProjectNxlFileRights", rt);
            }

            return true;
        }

        #endregion // User project

        #region Sharing transaction for project

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode, Pack = 8)]
        public struct InternalProjectReshareFileResult
        {
           public uint protectType;
           public string newTransactionId;
           public string newSharedList;
           public string alreadySharedList;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 8)]
        public struct InternalProjectShareFileResult
        {
           public string name;
           public string duid;
           public string filePathId;
           public string transactionId;
           public string newSharedList;
           public string alreadySharedList;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 8)]
        public struct InternalProjectSharedFile
        {
           public string id;
           public string duid;
           public string pathDisplay;
           public string pathId;
           public string name;
           public string fileType;
           public ulong lastModified; // Uint64
           public ulong createTime;
           public ulong size;
            //
           public ulong isFolder;
           public ulong isShared;
           public ulong isRevoked;
            //
           public ProjectUserInfo owner;
           public ProjectUserInfo lastModifiedUser;
           public string sharedWithProject;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 8)]
        public struct InternalProjectSharedWithMeFile
        {
            public string duid;
            public string name;
            public string fileType;
            public ulong size;
            public ulong sharedDate;
            public string sharedBy;
            public string transactionId;
            public string transactionCode;
            public string comment;
            public ulong isOwner;
            public uint protectType;
            public string sharedByProject;
            public ulong rights;
        }


        public ProjectFileInfoEx[] ProjectListTotalFiles(uint projectId, string pathId, FilterType type)
        {
            IntPtr pArray;
            uint size;
            uint rt = Boundary.SDWL_User_ProjectListTotalFile(hUser, projectId, "name", pathId, "", type, out pArray, out size);
            if (rt != 0 || size < 0)
            {
                ExceptionFactory.BuildThenThrow("ProjectListTotalFiles", rt);
            }
            if (size == 0)
            {
                return new ProjectFileInfoEx[0];
            }

            // marsh & unmarsh
            InternalProjectSharedFile[] pInfo = new InternalProjectSharedFile[size];
            int structSize = Marshal.SizeOf(typeof(InternalProjectSharedFile));
            IntPtr pCur = pArray;
            for (int i = 0; i < size; i++)
            {
                pInfo[i] = (InternalProjectSharedFile)Marshal.PtrToStructure(pCur, typeof(InternalProjectSharedFile));
                pCur += structSize;
            }

            // free
            Marshal.FreeCoTaskMem(pArray);

            // Convert returned type
            ProjectFileInfoEx[] results = new ProjectFileInfoEx[size];
            for (int i = 0; i < size; i++)
            {
                results[i].id = pInfo[i].id;
                results[i].duid = pInfo[i].duid;
                results[i].pathDisplay = pInfo[i].pathDisplay;
                results[i].pathId = pInfo[i].pathId;
                results[i].name = pInfo[i].name;
                results[i].fileType = pInfo[i].fileType;
                results[i].lastModified = pInfo[i].lastModified;
                results[i].createTime = pInfo[i].createTime;
                results[i].size = pInfo[i].size;
                results[i].isFolder = pInfo[i].isFolder == 1;
                results[i].isShared = pInfo[i].isShared == 1;
                results[i].isRevoked = pInfo[i].isRevoked == 1;
                results[i].owner = pInfo[i].owner;
                results[i].lastModifiedUser = pInfo[i].lastModifiedUser;
                // Shared with project.
                results[i].sharedWithProject = DataConvert.ParseSharedWithProjects(pInfo[i].sharedWithProject);
            }

            return results;

        }

        public ProjectSharedWithMeFile[] ProjectListTotalSharedWithMeFiles(uint projectId)
        {
            IntPtr pArray;
            uint size;
            uint rt = Boundary.SDWL_User_ProjectListTotalSharedWithMeFiles(hUser, projectId, "name", "", out pArray, out size);
            if ( rt != 0 || size < 0)
            {
                ExceptionFactory.BuildThenThrow("ProjectListTotalSharedWithMeFiles", rt);
            }
            if (size == 0)
            {
                return new ProjectSharedWithMeFile[0];
            }

            // marsh 
            InternalProjectSharedWithMeFile[] pFile = new InternalProjectSharedWithMeFile[size];
            int structSize = Marshal.SizeOf(typeof(InternalProjectSharedWithMeFile));
            IntPtr pCur = pArray;
            for(int i = 0; i < size; i++)
            {
                pFile[i] = (InternalProjectSharedWithMeFile)Marshal.PtrToStructure(pCur, typeof(InternalProjectSharedWithMeFile));
                pCur += structSize;
            }
            // free
            Marshal.FreeCoTaskMem(pArray);

            // Convert
            ProjectSharedWithMeFile[] f = new ProjectSharedWithMeFile[size];
            for(int i = 0; i < size; i++)
            {
                f[i].duid = pFile[i].duid;
                f[i].name = pFile[i].name;
                f[i].fileType = pFile[i].fileType;
                f[i].size = pFile[i].size;
                f[i].sharedDate = pFile[i].sharedDate;
                f[i].sharedBy = pFile[i].sharedBy;
                f[i].transactionId = pFile[i].transactionId;
                f[i].transactionCode = pFile[i].transactionCode;
                f[i].comment = pFile[i].comment;
                f[i].isOwner = pFile[i].isOwner == 1 ? true : false;
                f[i].protectType = pFile[i].protectType;
                f[i].sharedByProject = pFile[i].sharedByProject;
                f[i].rights = DataConvert.ParseRights(long.Parse(pFile[i].rights.ToString()));
            }

            return f;
        }

        public void ProjectDownloadSharedWithMeFile(uint projectid, string transactionId, string transactionCode,
            ref string targetPath, bool bForViewer)
        {
            string outPath;
            uint rt = Boundary.SDWL_User_ProjectDownloadSharedWithMeFile(hUser, projectid, transactionCode, transactionId, 
                targetPath, bForViewer, out outPath);
            if(rt != 0)
            {
                ExceptionFactory.BuildThenThrow("ProjectDownloadSharedWithMeFile", rt);
            }
            targetPath = outPath;
        }

        public void ProjectPartialDownloadSharedWithMeFile(uint projectid, string transactionId, string transactionCode,
          ref string targetPath, bool bForViewer)
        {
            string outPath;
            uint rt = Boundary.SDWL_User_ProjectPartialDownloadSharedWithMeFile(hUser, projectid, transactionCode, transactionId,
                targetPath, bForViewer, out outPath);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("ProjectPartialDownloadSharedWithMeFile", rt);
            }
            targetPath = outPath;
        }

        /// <param name="emailLsit">mandatory for myVault only, optional otherwise</param>
        public ProjectReshareFileResult ProjectReshareSharedWithMeFile(uint proId, string transactionCode, string transactionId,
            List<uint> recipients, string emailLsit = "")
        {
            InternalProjectReshareFileResult result;
            uint rt = Boundary.SDWL_User_ProjectReshareSharedWithMeFile(hUser, proId, transactionId, transactionCode,
                emailLsit, recipients.ToArray(), (uint)recipients.Count, out result);
            if(rt != 0)
            {
                ExceptionFactory.BuildThenThrow("ProjectReshareSharedWithMeFile", rt);
            }

            // Convert
            ProjectReshareFileResult ret = new ProjectReshareFileResult()
            {
                protectType = result.protectType,
                newTransactionId = result.newTransactionId,
                newSharedList = DataConvert.ParseProjectRecipients(result.newSharedList),
                alreadySharedList = DataConvert.ParseProjectRecipients(result.alreadySharedList)
            };

            return ret;
        }

        // The returned value is Pair<"newRecipients", list> & Pair<"removedRecipients", list>
        public Dictionary<string, List<uint>> ProjectUpdateSharedFileRecipients(string duid, List<uint> addRecipients,
            List<uint> removedRecipients, string comment)
        {
            IntPtr pAddList, pRemovedList;
            uint addLen, removedLen;
            uint rt = Boundary.SDWL_User_ProjectUpdateSharedFileRecipients(hUser, duid, addRecipients.ToArray(), (uint)addRecipients.Count,
                removedRecipients.ToArray(), (uint)removedRecipients.Count, comment,
                out pAddList, out addLen, out pRemovedList, out removedLen);

            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("ProjectUpdateSharedFileRecipients", rt);
            }

            List<uint> newList = new List<uint>();
            if ( addLen > 0)
            {
                IntPtr pCur = pAddList;
                for(int i = 0; i < addLen; i++)
                {
                    newList.Add((uint)Marshal.ReadInt32(pCur));
                    pCur += Marshal.SizeOf(typeof(Int32));
                }
                Marshal.FreeCoTaskMem(pAddList);
            }

            List<uint> removedList = new List<uint>();
            if(removedLen > 0)
            {
                IntPtr pCur = pRemovedList;
                for(int i = 0; i < removedLen; i++)
                {
                    removedList.Add((uint)Marshal.ReadInt32(pCur));
                    pCur += Marshal.SizeOf(typeof(Int32));
                }
                Marshal.FreeCoTaskMem(pRemovedList);
            }

            // wrapper
            Dictionary<string, List<uint>> ret = new Dictionary<string, List<uint>>();
            ret.Add("newRecipients", newList);
            ret.Add("removedRecipients", removedList);

            return ret;
        }

        public ProjectShareFileResult ProjectShareFile(uint proId, List<uint> recipients, string name,
            string pathid, string filePath, string comment)
        {
            InternalProjectShareFileResult result;
            uint rt = Boundary.SDWL_User_ProjectShareFile(hUser, proId, recipients.ToArray(), (uint)recipients.Count,
                name, pathid, filePath, comment, out result);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("ProjectShareFile", rt);
            }

            // Convert
            ProjectShareFileResult ret = new ProjectShareFileResult()
            {
                name = result.name,
                duid = result.duid,
                pathId = result.filePathId,
                transactionId = result.transactionId,
                newSharedList = DataConvert.ParseProjectRecipients(result.newSharedList),
                alreadySharedList = DataConvert.ParseProjectRecipients(result.alreadySharedList)
            };

            return ret;
        }

        public bool ProjectRevokeShareFile(string duid)
        {
            uint rt = Boundary.SDWL_User_ProjectRevokeShareFile(hUser, duid);
            return rt == 0;
        }

        #endregion // Sharing transaction for project 

        #region User sharedWithMe
        public SharedWithMeFileInfo[] ListSharedWithMeFile()
        {
            IntPtr pArray;
            int size;
            uint rt = Boundary.SDWL_User_ListSharedWithMeAllFiles(hUser,
                 "name", "", out pArray, out size);
            if (rt != 0 || size < 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_User_ListSharedWithMeFiles", rt,
                    RmSdkExceptionDomain.Rest_MySharedWithMe, RmSdkRestMethodKind.List);
            }
            if (size == 0)
            {
                return new SharedWithMeFileInfo[0];
            }

            // received COM encapsuled mem and convert to c# array
            SharedWithMeFileInfo[] info = new SharedWithMeFileInfo[size];
            int structSize = Marshal.SizeOf(typeof(SharedWithMeFileInfo));
            IntPtr cur = pArray;
            for (int i = 0; i < size; i++)
            {
                info[i] = (SharedWithMeFileInfo)Marshal.PtrToStructure(cur, typeof(SharedWithMeFileInfo));
                cur += structSize;
            }
            Marshal.FreeCoTaskMem(pArray);

            return info;
        }

        public void DownLoadSharedWithMeFile(string transactionId, string transactionCode,
            ref string DestLocalFodler, bool isForViewOnly = true)
        {
            string outpath;
            uint rt = Boundary.SDWL_User_DownloadSharedWithMeFiles(hUser,
                transactionId, transactionCode, DestLocalFodler, isForViewOnly, out outpath);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_User_DownloadSharedWithMeFiles", rt,
                    RmSdkExceptionDomain.Rest_MySharedWithMe, RmSdkRestMethodKind.Download);
            }
            DestLocalFodler = outpath;
        }

        public void DownLoadSharedWithMePartialFile(string transactionId, string transactionCode,
            ref string DestLocalFodler)
        {
            string outpath;
            uint rt = Boundary.SDWL_User_DownloadSharedWithMePartialFiles(hUser,
                transactionId, transactionCode, DestLocalFodler, true, out outpath);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_User_DownloadSharedWithMePartialFiles", rt,
                    RmSdkExceptionDomain.Rest_MySharedWithMe, RmSdkRestMethodKind.Download);
            }
            DestLocalFodler = outpath;
        }
        #endregion // User sharedWithMe

        public void CopyNxlFile(string fileName, string filePath, NxlFileSpaceType spaceType, string spaceId,
            string destFileName, string destFolderPath, NxlFileSpaceType destSpaceType, string destSpaceId,
            bool overwrite = false, string transactionCode="", string transactionId="")
        {
            uint rt = Boundary.SDWL_User_CopyNxlFile(hUser, fileName, filePath, spaceType, spaceId,
                destFileName, destFolderPath, destSpaceType, destSpaceId, overwrite, transactionCode, transactionId);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_User_CopyNxlFile", rt);
            }
        }

        #region Repository
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 8)]
        public struct RepositoryInfo
        {
            public uint isShared;
            public uint isDefault;
            //
            public ulong createTime;
            public ulong updateTime;
            //
            public string repoId;
            public string name;
            public string type;
            public string providerClass;
            public string accountName;
            public string accountId;
            public string token;
            public string preference;
        }

        public RepositoryInfo[] ListRepositories()
        {
            IntPtr pArray;
            uint size;
            var rt = Boundary.SDWL_User_GetRepositories(hUser, out pArray, out size);
            if (rt != 0 || size < 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_User_GetRepositories", rt);
            }
            if (size == 0)
            {
                return new RepositoryInfo[0];
            }
            // parse com mem to extract the array
            // marshal unmarshal
            RepositoryInfo[] pInfo = new RepositoryInfo[size];
            int structSize = Marshal.SizeOf(typeof(RepositoryInfo));
            IntPtr cur = pArray;
            for (int i = 0; i < size; i++)
            {
                pInfo[i] = (RepositoryInfo)Marshal.PtrToStructure(cur, typeof(RepositoryInfo));
                cur += structSize;
            }
            Marshal.FreeCoTaskMem(pArray);
            return pInfo;
        }

        public string GetRepositoryAccessToken(string repoId)
        {
            string token;
            uint rt = Boundary.SDWL_User_GetRepositoryAccessToken(hUser, repoId, out token);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_User_GetRepositoryAccessToken", rt);
            }

            return token;
        }

        public string GetRepositoryAuthorizationUrl(string name, string type, string authUrl="")
        {
            uint rt = Boundary.SDWL_User_GetRepositoryAuthorizationUrl(hUser, type, name, out authUrl);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_User_GetRepositoryAuthorizationUrl", rt);
            }

            return authUrl;
        }

        public bool UpdateRepository(string repoid, string token, string name)
        {
            uint rt = Boundary.SDWL_User_UpdateRepository(hUser, repoid, token, name);
            return rt == 0;
        }

        public bool RemoveRepository(string repoid)
        {
            uint rt = Boundary.SDWL_User_RemoveRepository(hUser, repoid);
            return rt == 0;
        }

        #endregion // Repository

    }
}
