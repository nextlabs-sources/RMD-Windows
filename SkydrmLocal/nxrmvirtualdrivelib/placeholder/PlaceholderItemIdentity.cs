using nxrmvirtualdrivelib.core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nxrmvirtualdrivelib.placeholder
{
    public class PlaceholderItemIdentity
    {
        private static readonly byte MagicNum = 5;
        public bool IsFile;
        public FileSystemItemType ItemType;
        public byte[] RemoteStorageItemId;

        public byte[] ReservedId;
        public ulong? ParentFileId;
        public string FileName;
        public bool IsNxlFile;
        public bool IsAnonymousNxlFile;
        public bool IsConflict;

        private PlaceholderItemIdentity()
        {

        }

        public PlaceholderItemIdentity(FileSystemItemType itemType, byte[] remoteStorageItemId, byte[] reservedId, bool checkFile,
            ulong? parentFileId = null, string fileName = null, bool isNxlFile = false, bool isAnonymousNxlFile = false, bool isConflict = false)
        {
            this.IsFile = itemType != FileSystemItemType.Folder && checkFile;
            this.ItemType = itemType;
            this.RemoteStorageItemId = remoteStorageItemId;
            this.ReservedId = reservedId;
            this.ParentFileId = parentFileId;
            this.FileName = fileName;
            this.IsNxlFile = isNxlFile;
            this.IsAnonymousNxlFile = isAnonymousNxlFile;
            this.IsConflict = isConflict;
        }

        public byte[] MarshalAs()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write(MagicNum);
                    bw.Write(IsFile);
                    bw.Write(Convert.ToBoolean(ItemType));
                    bw.Write(RemoteStorageItemId != null);
                    if (RemoteStorageItemId != null)
                    {
                        bw.Write(RemoteStorageItemId.Length);
                        bw.Write(RemoteStorageItemId);
                    }
                    bw.Write(ReservedId != null);
                    if (ReservedId != null)
                    {
                        bw.Write(ReservedId.Length);
                        bw.Write(ReservedId);
                    }
                    if (ItemType == FileSystemItemType.File)
                    {
                        bw.Write(ParentFileId.HasValue);
                        if (ParentFileId.HasValue)
                        {
                            bw.Write(ParentFileId.Value);
                        }
                        bw.Write(FileName);
                        bw.Write(IsNxlFile);
                        bw.Write(IsAnonymousNxlFile);
                        bw.Write(IsConflict);
                    }
                }
                return ms.ToArray();
            }
        }

        public static PlaceholderItemIdentity MarshalFrom(byte[] itemId, bool checkMagicNum)
        {
            PlaceholderItemIdentity rt = new PlaceholderItemIdentity();
            using (MemoryStream ms = new MemoryStream(itemId))
            {
                using (BinaryReader br = new BinaryReader(ms))
                {
                    if (MagicNum != br.ReadByte())
                    {
                        throw new Exception("Invalid item id.");
                    }
                    rt.IsFile = br.ReadBoolean();
                    rt.ItemType = (FileSystemItemType)Convert.ToInt32(br.ReadBoolean());
                    rt.RemoteStorageItemId = br.ReadBoolean() ? br.ReadBytes(br.ReadInt32()) : null;
                    rt.ReservedId = br.ReadBoolean() ? br.ReadBytes(br.ReadInt32()) : null;
                    if (rt.ItemType == FileSystemItemType.File)
                    {
                        if (br.ReadBoolean())
                        {
                            rt.ParentFileId = br.ReadUInt64();
                        }
                        else
                        {
                            rt = null;
                        }
                        rt.FileName = br.ReadString();
                        rt.IsNxlFile = br.ReadBoolean();
                        rt.IsAnonymousNxlFile = br.ReadBoolean();
                        rt.IsConflict = br.ReadBoolean();
                    }
                }
            }
            return rt;
        }
    }
}
