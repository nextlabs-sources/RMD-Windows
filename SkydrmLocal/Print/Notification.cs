using Newtonsoft.Json;
using Print.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Print
{
    public class Notification
    {
        [DllImport("User32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Unicode)]
        private static extern int SendMessage(
         IntPtr hWnd,     // handle to destination window
         int Msg,      // message
         int wParam,   // first message para
         ref COPYDATASTRUCT lParam // second message para
        );

        public static void SendMessage(string message,int IntPtr)
        {
            IntPtr intPtr = new IntPtr(IntPtr);

          //  PrintResult printResult = new PrintResult();

          //  printResult.Msg = message;

          //  string data = JsonConvert.SerializeObject(printResult);
    
            SendData(intPtr, IPCManager.WM_PRINT_RESULT, message);
        }


        private static void SendData(IntPtr hwnd, int wParam, string data)
        {
            COPYDATASTRUCT cds;
            cds.dwData = (IntPtr)0;
            cds.lpData = data;
            // byte size
            cds.cbData = (data.Length + 1) * 2;
            // send mgs
            SendMessage(hwnd, IPCManager.WM_COPYDATA, wParam, ref cds);
        }
    }
}
