using nxrmvirtualdrivelib.core;
using nxrmvirtualdrivelib.nxl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nxrmvirtualdrivelib.metadata
{
    public class FileMetadata : Metadatabase, IFileMetadata
    {
        private long m_length;
        private bool m_isNxlFile;
        private bool m_isAnonymousNxlFile;
        private bool m_isConflict;
        private string m_conflictName;

        public override long Length
        {
            get => m_length;
            set
            {
                m_length = value;
            }
        }

        public bool IsNxlFile
        {
            get => m_isNxlFile;
            set
            {
                m_isNxlFile = value;
            }
        }

        public bool IsAnonymousNxlFile
        {
            get => m_isAnonymousNxlFile;
            set
            {
                m_isAnonymousNxlFile = value;
            }
        }

        public bool IsConfict
        {
            get => m_isConflict;
            set
            {
                m_isConflict = value;
            }
        }

        public string ConfictName
        {
            get
            {
                var name = m_name;
                bool removedNxlExt = false;
                if (m_isConflict)
                {
                    if (m_isNxlFile)
                    {
                        //Remove ".nxl" ext if exists.
                        if (name.EndsWith(NxlFile.NXL_EXT))
                        {
                            name = name.Remove(name.IndexOf(NxlFile.NXL_EXT), NxlFile.NXL_EXT.Length);
                            removedNxlExt = true;
                        }
                    }
                    var ext = Path.GetExtension(name);
                    var nameWithoutExt = Path.GetFileNameWithoutExtension(name);
                    var fileIdLong = BitConverter.ToUInt64(m_remoteStorageItemId, 0);

                    name = nameWithoutExt + "_conflict_" + fileIdLong + ext;
                }
                if (m_isAnonymousNxlFile || removedNxlExt)
                {
                    name = NxlFile.AppendNxlExt(name);
                }
                return name;
            }
            set
            {
                m_conflictName = value;
            }
        }
    }
}
