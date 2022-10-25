using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace nxrmvirtualdrivelib.utils
{
    public class MarshalUtils
    {
        public static IntPtr StructuresToPtr<T>(T[] data, Func<int, IntPtr> allocator) where T : struct
        {
            if (data == null || data.Length == 0)
            {
                return IntPtr.Zero;
            }
            var arrLen = data.Length;
            var structureSize = Marshal.SizeOf(typeof(T));
            IntPtr arrPtr = allocator(structureSize * arrLen);

            long longPtr = arrPtr.ToInt64();
            for (int i = 0; i < arrLen; i++)
            {
                IntPtr itemPtr = new IntPtr(longPtr);
                Marshal.StructureToPtr(data[i], itemPtr, false);
                longPtr += structureSize;
            }
            return arrPtr;
        }

        public static IntPtr ByteArrayToPtr(byte[] buffer, Func<int, IntPtr> allocator)
        {
            IntPtr bufferPtr = IntPtr.Zero;
            if (buffer == null || buffer.Length == 0)
            {
                return bufferPtr;
            }
            bufferPtr = allocator(buffer.Length);
            if (bufferPtr != IntPtr.Zero)
            {
                Marshal.Copy(buffer, 0, bufferPtr, buffer.Length);
            }
            return bufferPtr;
        }

        public static void Free(IntPtr intPtr, Action<IntPtr> allocator)
        {
            if (intPtr != IntPtr.Zero)
            {
                allocator(intPtr);
            }
        }
    }
}
