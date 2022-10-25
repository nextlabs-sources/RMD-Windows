using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.sdk
{
    public class Apis
    {
        public static uint Version
        {
            get { return Boundary.GetSDKVersion(); }
        }

        public static Session CreateSession(string TempPath)
        {
            IntPtr hSession = IntPtr.Zero;
            uint rt = Boundary.CreateSDKSession(TempPath, out hSession);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("CreateSession", rt);
            }
            return new Session(hSession);

        }
    }

    public class Session
    {
        private IntPtr hSession;
        public Session(IntPtr hSession)
        {
            this.hSession = hSession;
        }

        ~Session()
        {
            DeleteSession();
        }


        //session need to hold the hUser
        private User user = null;

        public IntPtr Handle { get { return hSession; } }

        public User User { get { return user; } }

        // used to release res
        public void DeleteSession()
        {
            if (this.hSession == IntPtr.Zero)
            {
                return;
            }
            DeleteSession(this);
            this.hSession = IntPtr.Zero;
        }

        //public void Initialize(string Router, string Tenant)
        //{
        //    SDK_Initialize(Handle, Router, Tenant);
        //}

        public void Initialize(string WorkingFolder, string Router, string Tenant)
        {
            uint rt = Boundary.SDK_Initialize(
                Handle, WorkingFolder, Router, Tenant);
            if (0 != rt)
            {
                // init may failed,  like: user try to change another server
                ExceptionFactory.BuildThenThrow("Initialize", rt);
            }
        }

        public void SaveSession(string Folder)
        {
            Boundary.SDK_SaveSession(Handle, Folder);
        }

        public void GetLogingParams(out string loginURL, out Dictionary<string, string> values)
        {
            values = new Dictionary<string, string>();
            int size;
            IntPtr pcookies = IntPtr.Zero;
            uint rt = Boundary.SDWL_Session_GetLoginParams(
                Handle, out loginURL, out pcookies, out size);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("GetLogingParams", rt);
            }
            // handle datas from c++
            if (size <= 0)
            {
                return;
            }
            // convert all values to Dictionary format
            // c# c+= inter_op
            // copy non-managed array into manged arrars
            IntPtr pp = pcookies;
            Cookie[] cookies = new Cookie[size];
            for (int i = 0; i < size; i++)
            {
                // extract each k,v from Inptr[i]         
                cookies[i] = (Cookie)Marshal.PtrToStructure(pp, typeof(Cookie));
                pp += Marshal.SizeOf(typeof(Cookie));
            }
            Marshal.FreeCoTaskMem(pcookies);

            // fill out param values
            foreach (var c in cookies)
            {
                if (values.ContainsKey(c.key))
                {
                    values.Remove(c.key);
                }
                values.Add(c.key, c.value);
            }

        }

        public Tenant GetCurrentTenant()
        {
            IntPtr hTenant = IntPtr.Zero;
            uint rt = Boundary.SDK_GetCurrentTenant(hSession, out hTenant);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("GetCurrentTenant", rt);
            }
            return new Tenant(hTenant);
        }

        public void SetLoginRequest(string loginstr)
        {
            IntPtr hUser = IntPtr.Zero;
            string security = "{6829b159-b9bb-42fc-af19-4a6af3c9fcf6}";
            uint rt = Boundary.SDK_SetLoginRequest(hSession, loginstr, security, out hUser);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("SetLoginRequest", rt);
            }
            this.user = new User(hUser);

        }

        public bool RecoverUser(string email, string passcode)
        {
            try
            {
                // sanity check
                if (email == null || email.Length < 5)
                {
                    throw new Exception();
                }
                if (passcode == null || passcode.Length < 5)
                {
                    throw new Exception();
                }

                IntPtr hUser = IntPtr.Zero;
                uint rt = Boundary.SDWL_Session_GetLoginUser(
                    hSession, email, passcode, out hUser);
                if (rt != 0 || hUser == IntPtr.Zero)
                {
                    ExceptionFactory.BuildThenThrow("RecoverUser", rt);
                }
                this.user = new User(hUser);

                return true;
            }
            catch (Exception ignored)
            {
            }

            return false;
        }


        private static void DeleteSession(Session session)
        {
            uint rt = Boundary.DeleteSDKSession(session.Handle);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("DeleteSession", rt);
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct Cookie
        {
            public string key;
            public string value;

        }

        public bool RPM_IsDriverExist()
        {
            bool isExist;
            var rt = Boundary.SDWL_RPM_IsRPMDriverExist(
                hSession, out isExist);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("IsRPMDriverExist", rt);
            }
            return isExist;
        }

        public void RPM_AddDir(string dir)
        {
            var rt = Boundary.SDWL_RPM_AddRPMDir(Handle, dir);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("AddRPMDir", rt);
            }
        }

        public void RPM_RemoveDir(string dir)
        {
            var rt = Boundary.SDWL_RPM_RemoveRPMDir(Handle, dir);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("RemoveRPMDir", rt);
            }
        }


        /**
         rpm only handle nxl file, pass the nxl as @param srcPath
         anything is ok, path @returnParam from an RPM Folder,
         other steps depend on @returnParam
         for example:
            office process, use it as docoment to edit    
         */
        public string RPM_EditFile(string srcPath)
        {
            string file;
            var rt = Boundary.SDWL_RPM_EditFile(hSession, srcPath, out file);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("RPMEditFile", rt);
            }
            return file;
        }


        public void RPM_DeleteFile(string srcPath)
        {
            var rt = Boundary.SDWL_RPM_DeleteFile(hSession, srcPath);
            if (rt != 0)
            {
              //  ExceptionFactory.BuildThenThrow("RPM_DeleteFile", rt);
            }
        }


        public void RPM_RegisterApp(string appPath)
        {
            var rt = Boundary.SDWL_RPM_RegisterApp(hSession, appPath);
            if (rt != 0)
            {
                //  ExceptionFactory.BuildThenThrow("SDWL_RPM_RegisterApp", rt);
            }
        }

        public void RPM_UnregisterApp(string appPath)
        {
            var rt = Boundary.SDWL_RPM_UnregisterApp(hSession, appPath);
            if (rt != 0)
            {
                //  ExceptionFactory.BuildThenThrow("SDWL_RPM_RegisterApp", rt);
            }
        }

        public bool RMP_AddTrustedProcess(int pid)
        {
            bool result = true;
            var rt = Boundary.SDWL_RPM_AddTrustedProcess(hSession, pid);
            if (rt != 0)
            {
                result = false;
                //  ExceptionFactory.BuildThenThrow("RMP_AddTrustedProcess", rt);
            }
            return result;
        }

        public void RMP_RemoveTrustedProcess(int pid)
        {
            var rt = Boundary.SDWL_RPM_RemoveTrustedProcess(hSession, pid);
            if (rt != 0)
            {
                //  ExceptionFactory.BuildThenThrow("RMP_RemoveTrustedProcess", rt);
            }
        }

        public bool RMP_IsSafeFolder(string path)
        {
            bool result = false;
            var rt = Boundary.SDWL_RPM_IsSafeFolder(hSession, path, ref result);
            if (rt != 0)
            {
                // RMP ignore exception
                //  ExceptionFactory.BuildThenThrow("SDWL_RPM_IsSafeFolder", rt);
            }
            return result;
        }

        public void SDWL_RPM_NotifyRMXStatus(bool running)
        {
            var rt = Boundary.SDWL_RPM_NotifyRMXStatus(hSession, running);
            if (rt != 0)
            {
                //  ExceptionFactory.BuildThenThrow("RMP_RemoveTrustedProcess", rt);
            }
        }

        public bool IsPluginWell(string wszAppType, string wszPlatform)
        {
            return Boundary.IsPluginWell(wszAppType, wszPlatform);
        }

        public void SDWL_RPM_MonitorRegValueDeleted(string rmpPathInReg,Boundary.RegChangedCallback callback)
        {

            var rt = Boundary.SDWL_SYSHELPER_MonitorRegValueDeleted(rmpPathInReg,callback);
            if (rt != 0)
            {
                ExceptionFactory.BuildThenThrow("SDWL_SYSHELPER_RegChangeMonitor", rt);
            }
        }


    }

}
