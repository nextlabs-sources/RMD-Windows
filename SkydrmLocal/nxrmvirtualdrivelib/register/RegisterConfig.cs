using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nxrmvirtualdrivelib.register
{
    public class RegisterConfig
    {
        private string m_id;
        private string m_serverPath;
        private string m_clientPath;
        private string m_displayName;
        private string m_iconResource;
        private string m_version;
        private Uri m_recycleBin;

        public RegisterConfig()
        {

        }

        public RegisterConfig(string id, string serverPath, string clientPath,
            string displayName, string iconResource, string version, Uri recyleBin)
        {
            m_id = id;
            m_serverPath = serverPath;
            m_clientPath = clientPath;
            m_displayName = displayName;
            m_iconResource = iconResource;
            m_version = version;
            m_recycleBin = recyleBin;
        }

        public string Id
        {
            get => m_id;
            set
            {
                m_id = value;
            }
        }

        public string ServerPath
        {
            get => m_serverPath;
            set
            {
                m_serverPath = value;
            }
        }

        public string ClientPath
        {
            get => m_clientPath;
            set
            {
                m_clientPath = value;
            }
        }

        public string DisplayName
        {
            get => m_displayName;
            set
            {
                m_displayName = value;
            }
        }

        public string IconResource
        {
            get => m_iconResource;
            set
            {
                m_iconResource = value;
            }
        }

        public string Version
        {
            get => m_version;
            set
            {
                m_version = value;
            }
        }

        public Uri RecyleBin
        {
            get => m_recycleBin;
            set
            {
                m_recycleBin = value;
            }
        }

        public string SyncRootContext
        {
            get
            {
                return m_serverPath + "->" + m_clientPath;
            }
        }

    }
}
