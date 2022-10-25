using SDWRmcCSharpLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nxrmvirtualdrivelib.nxl
{
    public class NxlFileHandler : IDisposable
    {
        private readonly Session m_session;

        private bool m_disposed;

        public NxlFileHandler()
        {
            try
            {
                TryGetSession(out m_session);
            }
            catch (Exception)
            {

            }
        }

        public bool IsNxlFile(string path, bool validate = false)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }
            if (!File.Exists(path))
            {
                return false;
            }
            if (validate)
            {
                if (m_session == null)
                {
                    throw new SessionInitializationExeption("Failed to initiate session instance.");
                }
                using (NxlFile file = new NxlFile(path, m_session?.User))
                {
                    if (file.TryCheckNxlFile(out var isNxl))
                    {
                        return isNxl.Value;
                    }
                }
                return false;
            }
            else
            {
                return NxlFile.IsNxlFile(path);
            }
        }

        public bool IsRPMDir(string dir)
        {
            if (string.IsNullOrEmpty(dir))
            {
                return false;
            }
            var rpmdirs = GetRPMDirs();
            if (rpmdirs == null || rpmdirs.Count == 0)
            {
                return false;
            }
            foreach (var item in rpmdirs)
            {
                if (item.Equals(dir, StringComparison.InvariantCultureIgnoreCase)
                    || item.StartsWith(dir)
                    || dir.StartsWith(item))
                {
                    return true;
                }
            }
            return false;
        }

        public void AddRPMDir(string dir, int option, string filetags)
        {
            m_session?.RPM_AddDir(dir, option, filetags);
        }

        public void RemoveRPMDir(string dir, out string errorMsg)
        {
            errorMsg = null;
            m_session?.RPM_RemoveDir(dir, out errorMsg);
        }

        public List<string> GetRPMDirs()
        {
            return m_session?.GetRPMdir(1);
        }

        private bool TryGetSession(out Session session)
        {
            session = null;
            if (Apis.GetCurrentLoggedInUser(out session))
            {
                return true;
            }
            return false;
        }

        protected virtual void Dispose(bool dispose)
        {
            if (!m_disposed)
            {
                if (dispose)
                {

                }
                m_disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
