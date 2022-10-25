using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace SkydrmLocal.rmc.ui.windows.mainWindow.helper
{
    /// <summary>
    /// IPC between SkydrmLocal  and Viewer.
    /// With the help of WM_COPYDATA, Comunicate between each process's MainWindow
    /// </summary>
    public class IPCManager
    {
        // MSG code
        public const Int32 WM_COPYDATA = 0x004A;
        public const int WM_USER = 0x0400;
        //
        // Customized message code
        //
        public const Int32 WM_VIEWER_WINDOW_LOADED = WM_USER + 1;
        public const Int32 WM_PROTECT_FILE = WM_USER + 2;
        public const Int32 WM_SHARE_FILE = WM_USER + 3;
        public const Int32 WM_VIEW_FILE_INFO = WM_USER + 4;
        public const Int32 WM_PRINT_RESULT = WM_USER + 5;
        public const Int32 WM_DECRYPTED_RESULT = WM_USER + 6;
        public const Int32 WM_HAS_NO_RIGHTS = WM_USER + 7;
        public const Int32 WM_DOWNLOAD_FAILED = WM_USER + 8;
        public const Int32 WM_UNSUPPORTED_FILE_TYPE = WM_USER + 9;
        public const Int32 WM_HIDE_VIEWER = WM_USER + 10;

        private event Action<int, int, string> OnReceived;

        public IPCManager(Action<int, int, string> receive)
        {
            this.OnReceived = receive;
        }

        public Process CreateProcess(string exeFileName, string intPtr)
        {
            Process proc = new Process();
            proc.StartInfo.FileName = exeFileName;             
            proc.StartInfo.WorkingDirectory = System.Windows.Forms.Application.StartupPath;
            proc.StartInfo.Arguments += exeFileName;
            proc.StartInfo.Arguments += " ";
            proc.StartInfo.Arguments += "IntPtr";
            proc.StartInfo.Arguments += " ";
            proc.StartInfo.Arguments += intPtr;


            //fix bug 49822, will result in other serious problems, so commnet it.
            //proc.StartInfo.UseShellExecute = false; 

            return proc;
        }

        // Receive data
        public IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            try
            {
                if (msg == WM_COPYDATA) // receive data
                {

                    COPYDATASTRUCT cds = (COPYDATASTRUCT)Marshal.PtrToStructure(lParam, typeof(COPYDATASTRUCT));
                    // handle received data from Viewer process if needed.

                    string data = cds.lpData;

                    OnReceived?.Invoke(msg, wParam.ToInt32(), data);
                }
                else if (/*msg == WM_VIEWER_WINDOW_LOADED
                    ||*/ msg == WM_PROTECT_FILE
                    || msg == WM_SHARE_FILE
                    || msg == WM_VIEW_FILE_INFO) // only receive siganl notification
                { 
                    OnReceived?.Invoke(msg, wParam.ToInt32(), "");
                }
                return hwnd;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return IntPtr.Zero;
            }
        }

        // Send data
        public static void SendData(IntPtr hwnd, int wParam, string data)
        {
            // A 2-byte, null-terminated Unicode character string.
            COPYDATASTRUCT cds;
            cds.dwData = (IntPtr)0;
            cds.lpData = data;
            // byte size
            cds.cbData = (data.Length + 1)*2;

            // send mgs
            SendMessage(hwnd, WM_COPYDATA, wParam, ref cds);
        }


        [DllImport("User32.dll", EntryPoint = "SendMessage",CharSet = CharSet.Unicode)]
        private static extern int SendMessage(
            IntPtr hWnd,     // handle to destination window
            int Msg,      // message
            int wParam,   // first message para
            ref COPYDATASTRUCT lParam // second message para
        );

        /// <summary>
        /// Copy data for IPC
        /// </summary>
        public struct COPYDATASTRUCT
        {
            public IntPtr dwData;
            public int cbData;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpData;
        }
    }



}
