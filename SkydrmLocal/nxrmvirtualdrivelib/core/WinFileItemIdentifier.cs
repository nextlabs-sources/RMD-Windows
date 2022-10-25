using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nxrmvirtualdrivelib.core
{
    /// <summary>
    /// Contains identification information for a file
    /// This structure is returned from the GetFileInformationByHandleEx function when FileIdInfo is passed in the FileInformationClass parameter.
    /// </summary>
    public class WinFileItemIdentifier
    {
        /// <summary>
        /// The 128-bit file identifier for the file. 
        /// </summary>
        protected byte[] ItemIdentifierData;
        /// <summary>
        /// The serial number of the volume that contains a file.
        /// </summary>
        public readonly ulong VolumeSerialNum;

        public readonly ulong ItemIdLow;
        public readonly ulong ItemIdHigh;

        public WinFileItemIdentifier(byte[] itemIdData, uint volumeSerialNum)
        {
            this.ItemIdentifierData = itemIdData;
            this.VolumeSerialNum = volumeSerialNum;

            //File id low part
            this.ItemIdLow = BitConverter.ToUInt64(itemIdData, 0);
            //File id high part
            this.ItemIdHigh = BitConverter.ToUInt64(itemIdData, 8);
        }

        public byte[] MarshalAs()
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
                {
                    binaryWriter.Write(this.ItemIdentifierData.Length);
                    binaryWriter.Write(this.ItemIdentifierData);
                    binaryWriter.Write(this.VolumeSerialNum);
                }
                return memoryStream.ToArray();
            }
        }

        public static bool TryMarshalFrom(byte[] itemIdData, out WinFileItemIdentifier fileId)
        {
            fileId = null;
            if (itemIdData == null)
            {
                return false;
            }
            try
            {
                byte[] itemIdentifierData;
                uint volumeSerialNum;
                using (MemoryStream input = new MemoryStream(itemIdData))
                {
                    using (BinaryReader binaryReader = new BinaryReader(input))
                    {
                        int itemIdentifierDataLen = binaryReader.ReadInt32();
                        itemIdentifierData = binaryReader.ReadBytes(itemIdentifierDataLen);
                        volumeSerialNum = binaryReader.ReadUInt32();
                    }
                }
                fileId = new WinFileItemIdentifier(itemIdentifierData, volumeSerialNum);

                return true;
            }
            catch
            {
            }
            return false;
        }
    }
}
