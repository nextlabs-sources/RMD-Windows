using System;
using Microsoft.Win32.SafeHandles;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;
using System.Windows;
using SkydrmLocal.rmc.sdk;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Security.Principal;

namespace SkydrmLocal.rmc.common.component
{
    /// <summary>
    /// This used to communicate with explorer shell plugin
    /// </summary>
    public class ExplorerNamedPipeServer
    {

        //private static readonly string PIPE_NAME = @"\\.\pipe\ExplorerPlugin_SkydrmApp_NamedPipe";
        private Thread readThread;
        private const int BUF_SIZE = 4096;
        private bool exitThreadForced = false;

        public void StartServer()
        {
            readThread = new Thread(new ThreadStart(ListenForClient));
            readThread.Start();
        }

        private void ListenForClient()
        {
            // Set DACL
            SecurityIdentifier si = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);
            PipeSecurity psa = new PipeSecurity();
            psa.SetAccessRule(new PipeAccessRule(
                si,PipeAccessRights.ReadWrite,
                System.Security.AccessControl.AccessControlType.Allow));
            using (NamedPipeServerStream ss = new NamedPipeServerStream(
                   "ExplorerPlugin_SkydrmApp_NamedPipe",
                   PipeDirection.InOut,
                   NamedPipeServerStream.MaxAllowedServerInstances,
                   PipeTransmissionMode.Byte,
                   PipeOptions.None,
                   BUF_SIZE, BUF_SIZE,
                   psa))
            {
                while (true)
                {
                    if (exitThreadForced)
                    {
                        break;
                    }
                    ss.WaitForConnection();
                    try
                    {
                        // read first
                        NxlFileFingerPrint fp;
                        fp = ReadDataBy(ss);
                        // response 
                        var buf = Encoding.UTF8.GetBytes(ParseRights(fp));
                        ss.Write(buf, 0, buf.Length);
                        ss.Flush();
                    }
                    catch (Exception e)
                    {
                        SkydrmLocalApp.Singleton.Log.Error(e.ToString());
                    }
                    ss.Disconnect();
                }
            }
        }
    
    

        private NxlFileFingerPrint ReadDataBy(Stream sr)
        {
            var buffer = new byte[BUF_SIZE];
            int bytesRead = sr.Read(buffer, 0, BUF_SIZE);            
            string localPath = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            localPath = localPath.Substring(0, localPath.IndexOf('\0'));
            return SkydrmLocalApp.Singleton.Rmsdk.User.GetNxlFileFingerPrint(localPath);
        }


        //private void ReadData()
        //{
        //    byte[] buffer = null;

        //    while (true)
        //    {

        //        int bytesRead = 0;

        //        try
        //        {
        //            buffer = new byte[BUF_SIZE];
                    
        //            // Read data from pipe.
        //            bytesRead = client.stream.Read(buffer, 0, BUF_SIZE);
        //        }
        //        catch
        //        {
        //            //read error has occurred
        //            break;
        //        }

        //        //client has disconnected
        //        if (bytesRead == 0)
        //            break;

        //        string tmp = System.Text.Encoding.UTF8.GetString(buffer);
        //        string localPath = tmp.Substring(0,tmp.IndexOf('\0'));

        //        NxlFileFingerPrint fp;
        //        try
        //        {
        //            fp = SkydrmLocalApp.Singleton.Rmsdk.User.GetNxlFileFingerPrint(localPath);
        //        }
        //        catch (Exception e)
        //        {
        //            Console.WriteLine("Get Finger print failed: " + e.ToString());
        //            SkydrmLocalApp.Singleton.Log.Info("Get Finger print failed");

        //            break;
        //        }                
        //        Console.WriteLine(" App: Received --- " + localPath);

        //        // write data
        //        WriteData(ParseRights(fp), client);

        //        break;
        //    }

        //    // clean up resources
        //    client.stream.Close();

        //    Console.WriteLine("Named pipe close.");
        //    SkydrmLocalApp.Singleton.Log.Info("Named pipe close");
        //}

        //public void WriteData(string message, Client client)
        //{
        //    ASCIIEncoding encoder = new ASCIIEncoding();
        //    byte[] messageBuffer = encoder.GetBytes(message);

        //    if (client.stream.CanWrite)
        //    {
        //        // write data to pipe.
        //        client.stream.Write(messageBuffer, 0, messageBuffer.Length);
        //        client.stream.Flush();
        //    }
        //}

        // Will call this when app exit.
        public void StopServer()
        {
            exitThreadForced = true;
        }

        /// <summary>
        /// The content that wrote into named pipe like following:
        ///  "RIGHT_VIEW=true|RIGHT_EDIT=true|RIGHT_SAVEAS=false;isByAdHoc=true|isByCentrolPolicy=fale"
        /// </summary>
        private string ParseRights(NxlFileFingerPrint fp)
        {
            Dictionary<string, bool> rights = new Dictionary<string, bool>();
           
            rights.Add("RIGHT_VIEW", fp.HasRight(FileRights.RIGHT_VIEW));
            rights.Add("RIGHT_EDIT", fp.HasRight(FileRights.RIGHT_EDIT));
            rights.Add("RIGHT_PRINT", fp.HasRight(FileRights.RIGHT_PRINT));
            rights.Add("RIGHT_CLIPBOARD", fp.HasRight(FileRights.RIGHT_CLIPBOARD));
            rights.Add("RIGHT_SAVEAS", fp.HasRight(FileRights.RIGHT_SAVEAS));
            rights.Add("RIGHT_DECRYPT", fp.HasRight(FileRights.RIGHT_DECRYPT));
            rights.Add("RIGHT_SCREENCAPTURE", fp.HasRight(FileRights.RIGHT_SCREENCAPTURE));
            rights.Add("RIGHT_SEND", fp.HasRight(FileRights.RIGHT_SEND));
            rights.Add("RIGHT_CLASSIFY", fp.HasRight(FileRights.RIGHT_CLASSIFY));
            rights.Add("RIGHT_SHARE", fp.HasRight(FileRights.RIGHT_SHARE));
            rights.Add("RIGHT_DOWNLOAD", fp.HasRight(FileRights.RIGHT_DOWNLOAD));
            rights.Add("RIGHT_WATERMARK", fp.HasRight(FileRights.RIGHT_WATERMARK));

            StringBuilder sb = new StringBuilder();
            // Append file rights
            foreach(var one in rights)
            {
                sb.Append(one.Key);
                sb.Append("=");
                sb.Append(one.Value);
                sb.Append("|");
            }

            // Append is adhoc or is policy
            sb.Append(";");
            sb.Append("isByAdHoc=");
            sb.Append(fp.isByAdHoc);

            sb.Append("|");
            sb.Append("isByCentrolPolicy=");
            sb.Append(fp.isByCentrolPolicy);

            sb.Append("|");
            sb.Append("hasAdminRights=");
            sb.Append(fp.hasAdminRights);

            sb.Append("|");
            sb.Append("isFromMyVault=");
            sb.Append(fp.isFromMyVault);

            sb.Append("|");
            sb.Append("isFromPorject=");
            sb.Append(fp.isFromPorject);

            sb.Append("|");
            sb.Append("isFromSystemBucket=");
            sb.Append(fp.isFromSystemBucket);

            return sb.ToString();
        }


        #region // Dll import
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern SafeFileHandle CreateNamedPipe(
               String pipeName,
               uint dwOpenMode,
               uint dwPipeMode,
               uint nMaxInstances,
               uint nOutBufferSize,
               uint nInBufferSize,
               uint nDefaultTimeOut,
               IntPtr lpSecurityAttributes);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int ConnectNamedPipe(
               SafeFileHandle hNamedPipe,
               IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int DisconnectNamedPipe(SafeFileHandle hNamedPipe);
        #endregion // For Dll import

    }


    //public Client client = null;
    //private SafeFileHandle clientHandle;

    //private const uint DUPLEX = (0x00000003);
    //private const uint FILE_FLAG_OVERLAPPED = (0x40000000);
    //private const uint MAX_INSTANCE = 255;

    //public class Client
    //{
    //    public SafeFileHandle handle;
    //    public FileStream stream;
    //}
}
