using SDWRmcCSharpLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nxrmvirtualdrivelib.nxl
{
    public class NxlFile : IDisposable
    {
        //private static readonly string MAGIC_CODE = "NXLFMT@";
        public static readonly string NXL_EXT = ".nxl";
        private static readonly byte[] MAGIC_CODE_BYTES = new byte[] { 0x4E, 0x58, 0x4C, 0x46, 0x4D, 0x54, 0x40, 0x00 };
        private readonly User m_user;
        private IntPtr? m_nxlFilePtr;

        private bool m_disposed;

        public NxlFile(string path, User user)
        {
            this.m_user = user;
            this.m_nxlFilePtr = user?.OpenNxlFile(path);
        }

        public bool TryCheckNxlFile(out bool? isNxlFile)
        {
            isNxlFile = null;
            if (m_nxlFilePtr.HasValue)
            {
                isNxlFile = m_user.IsValidNxl(m_nxlFilePtr.Value);
                return true;
            }
            return false;
        }

        public static bool IsNxlFile(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }
            if (!File.Exists(path))
            {
                return false;
            }
            if (path.ToLower().EndsWith(".nxl") || Path.GetFileName(path).ToLower().EndsWith(".nxl"))
            {
                return true;
            }
            byte[] buffer = new byte[8];

            try
            {
                using (var fs = File.Open(path, FileMode.Open, FileAccess.Read))
                {
                    fs.Read(buffer, 0, buffer.Length);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            //string magicCode = Encoding.Default.GetString(buffer);
            //return MAGIC_CODE.Equals(magicCode, StringComparison.InvariantCultureIgnoreCase);
            return MAGIC_CODE_BYTES.SequenceEqual(buffer);
        }

        public static bool IsAnonymousNxlFile(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }
            if (!File.Exists(path))
            {
                return false;
            }
            var ext = Path.GetExtension(path);
            bool isNxlFile = IsNxlFile(path);

            return isNxlFile && !NXL_EXT.Equals(ext, StringComparison.InvariantCultureIgnoreCase);
        }

        public static string AppendNxlExt(string name, bool anonymous = false)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }
            if (anonymous)
            {
                if (name.EndsWith(NXL_EXT))
                {
                    return name.Remove(name.IndexOf(NXL_EXT), NXL_EXT.Length);
                }
                return name;
            }
            if (name.EndsWith(NXL_EXT))
            {
                return name;
            }
            return name + NXL_EXT;
        }

        public void Dispose()
        {
            if (!m_disposed)
            {
                if (m_nxlFilePtr.HasValue)
                {
                    m_user?.CloseNxlFile(m_nxlFilePtr.Value);
                    m_nxlFilePtr = IntPtr.Zero;
                }
                m_disposed = true;
            }
        }
    }
}
