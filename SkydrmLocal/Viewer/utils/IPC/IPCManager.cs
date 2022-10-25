using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using Viewer.edit;

namespace Viewer.utils
{
    /// <summary>
    /// The Class is used to manager the IPC between SkydrmLocal process and Viewer process.
    /// Now we expoit sending WM_COPYDATA message to implement the process communication.
    /// </summary>
    public class IPCManager
    {
        public uint WM_CHECK_IF_ALLOW_LOGOUT = 50005;
        public uint WM_START_LOGOUT_ACTION = 50006;

        // MSG code   
        public const Int32 WM_COPYDATA = 0x004A;
        public const Int32 WM_USER = 0x0400;
        // Custom message code
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

        private event Action<int, int, string> onReceived;
        private const long BROADCAST_QUERY_DENY = 0x424D5144;

        [DllImport("User32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Unicode)]
        private static extern int SendMessage(
            IntPtr hWnd,     // handle to destination window
            int Msg,      // message
            int wParam,   // first message para
            int lParam    // second message para
        );

        [DllImport("User32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Ansi)]
        private static extern int SendMessage(
            IntPtr hWnd,     // handle to destination window
            int Msg,      // message
            int wParam,   // first message para
            ref COPYDATASTRUCT lParam // second message para
        );

        public IPCManager(Action<int, int, string> receive)
        {
            this.onReceived = receive;
        }

        public Process CreateProcess(string exeFileName)
        {
            Process proc = new Process();
            proc.StartInfo.FileName = exeFileName; //Print.exe
                                                   // Set Print.exe process dir
            proc.StartInfo.WorkingDirectory = System.Windows.Forms.Application.StartupPath;
            proc.StartInfo.CreateNoWindow = true;

            return proc;
        }

        // Receive info
        public IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            try
            {
                if (msg == WM_COPYDATA)
                {
                    COPYDATASTRUCT cds = (COPYDATASTRUCT)Marshal.PtrToStructure(lParam, typeof(COPYDATASTRUCT));

                    // handle received data from Viewer process if needed.
                    string data = cds.lpData;
                    onReceived?.Invoke(msg, wParam.ToInt32(), data);
                }
                else if (msg == WM_CHECK_IF_ALLOW_LOGOUT)
                {
                    //if (FileEditorHelper.IsbeingFileEdit())
                    //{
                    //    CommonUtils.ShowBalloonTip(CultureStringInfo.Notify_PopBubble_Forbid_Logout,false);

                    //    // Note: must set 'handled' as true, else will invalid if directly return 'BROADCAST_QUERY_DENY'
                    //    handled = true;
                    //    return new IntPtr(BROADCAST_QUERY_DENY);
                    //}

                    // For now we block all logout action if there is any view action happened.
                    CommonUtils.ShowBalloonTip(CultureStringInfo.Notify_PopBubble_Forbid_Logout, false);
                    handled = true;
                    return new IntPtr(BROADCAST_QUERY_DENY);
                }
                else if (msg == WM_START_LOGOUT_ACTION)
                {
                    onReceived?.Invoke(msg, wParam.ToInt32(), "");
                }

                return hwnd;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return IntPtr.Zero;
            }
        }

        public void SendData(IntPtr hwnd, string data)
        {

            COPYDATASTRUCT cds;
            cds.dwData = (IntPtr)100;
            // 2-byte, null-terminated Unicode character string.
            cds.lpData = data;
            cds.cbData = data.Length * 2 + 1;

            // send mgs -- note: the lParam must be not 0 when sending "WM_COPYDATA" msg, or else can't receive it.
            SendMessage(hwnd, WM_COPYDATA, 0, ref cds);
        }


        public void SendData(IntPtr hwnd, int msg, int wParam, string data)
        {

            COPYDATASTRUCT cds;
            cds.dwData = (IntPtr)0;

            cds.lpData = data;
            // byte size
            cds.cbData = (data.Length + 1) * 2;

            // send mgs -- note: the lParam must be not 0 when sending "WM_COPYDATA" msg, or else can't receive it.
            SendMessage(hwnd, msg, wParam, ref cds);
        }

        public void SendSignal(IntPtr hwnd, int msgCode, int IntPtr)
        {
            // send signal notification
            SendMessage(hwnd, msgCode, IntPtr, 0);
        }

    }

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
