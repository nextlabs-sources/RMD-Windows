using nxrmvirtualdrivelib.core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nxrmvirtualdrivelib.sync
{
    public class UpdateOperation
    {
        private string m_userFsPath;
        private FileSystemItemType m_itemType;
        private byte[] m_remoteItemId;
        //update sequence number (USN)
        private long m_usn;

        public string UserFsPath
        {
            get => m_userFsPath;
            set
            {
                m_userFsPath = value;
            }
        }

        public FileSystemItemType ItemType
        {
            get => m_itemType;
            set
            {
                m_itemType = value;
            }
        }

        public byte[] RemoteItemId
        {
            get => m_remoteItemId;
            set
            {
                m_remoteItemId = value;
            }
        }

        public long USN
        {
            get => m_usn;
            set
            {
                m_usn = value;
            }
        }


    }
}
