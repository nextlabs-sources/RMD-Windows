using nxrmvirtualdrivelib.core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nxrmvirtualdrivelib.metadata
{
    public abstract class Metadatabase : IFileSystemItemMetadata
    {
        protected byte[] m_remoteStorageItemId;
        protected string m_name;
        protected FileAttributes m_attributes;
        protected DateTimeOffset m_creationTime;
        protected DateTimeOffset m_lastWriteTime;
        protected DateTimeOffset m_lastAccessTime;
        protected DateTimeOffset m_changeTime;
        protected string m_localPath;
        protected string m_pathId;

        public byte[] RemoteStorageItemId
        {
            get => m_remoteStorageItemId;
            set
            {
                m_remoteStorageItemId = value;
            }
        }

        public string Name
        {
            get => m_name;
            set
            {
                m_name = value;
            }
        }

        public FileAttributes Attributes
        {
            get => m_attributes;
            set
            {
                m_attributes = value;
            }
        }

        public DateTimeOffset CreationTime
        {
            get => m_creationTime;
            set
            {
                m_creationTime = value;
            }
        }

        public DateTimeOffset LastWriteTime
        {
            get => m_lastWriteTime;
            set
            {
                m_lastWriteTime = value;
            }
        }

        public DateTimeOffset LastAccessTime
        {
            get => m_lastAccessTime;
            set
            {
                m_lastAccessTime = value;
            }
        }

        public DateTimeOffset ChangeTime
        {
            get => m_changeTime;
            set
            {
                m_changeTime = value;
            }
        }

        public string LocalPath
        {
            get => m_localPath;
            set
            {
                m_localPath = value;
            }
        }

        public string PathId
        {
            get => m_pathId;
            set
            {
                m_pathId = value;
            }
        }

        public abstract long Length { get; set; }
    }
}
