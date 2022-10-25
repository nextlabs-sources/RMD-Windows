using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;

namespace nxrmvirtualdrivelib.ext
{
    public static class ExtensionClass
    {
        public static IntPtr StructuresToPtr<T>(this T[] data, Func<int, IntPtr> allocator) where T : struct
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

        public static IntPtr AllocHGlobal<T>(T? data) where T : struct
        {
            IntPtr ptr = IntPtr.Zero;
            if (data == null || !data.HasValue)
            {
                return ptr;
            }

            ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(T)));
            Marshal.StructureToPtr(data, ptr, false);

            return ptr;
        }

        public static IntPtr AllocCoTaskMem<T>(T? data) where T : struct
        {
            IntPtr ptr = IntPtr.Zero;
            if (data == null || !data.HasValue)
            {
                return ptr;
            }

            ptr = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(T)));
            Marshal.StructureToPtr(data, ptr, false);

            return ptr;
        }

        public static IntPtr AllocHGlobal(this byte[] buffer)
        {
            IntPtr intPtr = IntPtr.Zero;
            if (buffer == null || buffer.Length == 0)
            {
                return intPtr;
            }

            intPtr = Marshal.AllocHGlobal(buffer.Length);
            Marshal.Copy(buffer, 0, intPtr, buffer.Length);

            return intPtr;
        }

        public static void FreeHGlobal(this IntPtr ptr)
        {
            if (ptr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        public static IntPtr AllocCoTaskMem(this byte[] buffer)
        {
            IntPtr intPtr = IntPtr.Zero;
            if (buffer == null || buffer.Length == 0)
            {
                return intPtr;
            }

            intPtr = Marshal.AllocCoTaskMem(buffer.Length);
            Marshal.Copy(buffer, 0, intPtr, buffer.Length);

            return intPtr;
        }

        public static void FreeCoTaskMem(this IntPtr ptr)
        {
            if (ptr != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(ptr);
            }
        }

        public static void Free(this GCHandle[] gcHandle)
        {
            if (gcHandle == null || gcHandle.Length == 0)
            {
                return;
            }
            foreach (var handle in gcHandle)
            {
                if (handle.IsAllocated)
                {
                    try
                    {
                        handle.Free();
                    }
                    catch (Exception)
                    {

                    }
                }
            }
        }

        public static void Free(this GCHandle gcHandle)
        {
            if (gcHandle == null)
            {
                return;
            }
            if (gcHandle.IsAllocated)
            {
                try
                {
                    gcHandle.Free();
                }
                catch (Exception)
                {

                }
            }
        }

        public static async Task CopyToAsync(this Stream source, Stream destination, int bufferSize, long count)
        {
            byte[] buffer = new byte[bufferSize];
            int read;
            while (count > 0 && (read = await source.ReadAsync(buffer, 0, (int)Math.Min(buffer.LongLength, count))) > 0)
            {
                await destination.WriteAsync(buffer, 0, read);
                count -= read;
            }
        }

        public static void CheckResult(this HRESULT result)
        {
            if (result != HRESULT.S_OK)
            {
                result.ThrowIfFailed();
            }
        }
    }
}
