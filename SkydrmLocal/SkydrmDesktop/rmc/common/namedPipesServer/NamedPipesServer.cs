using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SkydrmDesktop;
using SkydrmDesktop.Resources.languages;
using SkydrmLocal.rmc.ui;
using SkydrmLocal.rmc.ui.utils;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using static SkydrmLocal.rmc.Edit.EditFeature;

namespace SkydrmLocal.rmc.namePipesServer
{
    public class NamedPipesServer
    {
        public static SkydrmApp SkydrmApp;
        public static log4net.ILog log ;
        public static int BUF_SIZE = 4096;
        private static bool ExitThreadForced = false;
        //Named pipe should associate user session id for supporting multiple users login at the same time by RDP(Windows server)
        public static string PipeName = "544336d7-9086-4369-a9d0-3691ea290376" + "_sid_" + Process.GetCurrentProcess().SessionId.ToString();
        private static Thread ReadThread;

        public static void Launch()
        {
            SkydrmApp = SkydrmApp.Singleton;
            log = SkydrmApp.Singleton.Log;
            log.Info("\n*** Create named pipe server worker thread. ***\n");
            ReadThread = new Thread(new ThreadStart(ListenForClient));
            ReadThread.Start();
        }

        private static void ListenForClient()
        {
            // Set DACL
            SecurityIdentifier si = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);
            PipeSecurity psa = new PipeSecurity();
            psa.SetAccessRule(new PipeAccessRule(
                si, PipeAccessRights.ReadWrite,
                System.Security.AccessControl.AccessControlType.Allow));
            using (NamedPipeServerStream ss = new NamedPipeServerStream(
                   PipeName,
                   PipeDirection.InOut,
                   NamedPipeServerStream.MaxAllowedServerInstances,
                   PipeTransmissionMode.Message,
                   PipeOptions.None,
                   BUF_SIZE, BUF_SIZE,
                   psa))
            {
                while (true)
                {
                    if (ExitThreadForced)
                    {
                        break;
                    }
                    ss.WaitForConnection();
                    try
                    {
                        // read first
                        string data = ReadDataBy(ss);
                        log.Info("The namedpipe sever recevied data:" + data);
                        // parse
                        ParseAndHandle(data);
                    }
                    catch (Exception e)
                    {
                        log.Error(e.ToString());
                        MessageBox.Show(e.ToString());
                    }
                    ss.Disconnect();
                }
            }
        }

        private static string ReadDataBy(Stream sr)
        {
            var buffer = new byte[BUF_SIZE];
            int bytesRead = sr.Read(buffer, 0, BUF_SIZE);
            string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            log.Info("ReadData: " + data);
            return data;
        }


        public static void ShudownActivePipes()
        {
            ExitThreadForced = true;

            //foreach (NamedPipeServerStream item in Pipes)
            //{
            //    if (null!=item)
            //    {
            //        item.Close();
            //    }
            //}

            //for (int i = 0; i < numThreads; i++)
            //{
            //    try
            //    {
            //        Servers[i].Abort();

            //    }
            //    catch (Exception ex)
            //    {
            //        SkydrmApp.Log.Error(ex.ToString());
            //    }
            //}
        }

        //private static void ServerThread()
        //{
        //    try
        //    {
        //        SecurityIdentifier si = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);
        //        PipeSecurity pipeSecurity = new PipeSecurity();
        //        pipeSecurity.SetAccessRule(new PipeAccessRule(si, PipeAccessRights.ReadWrite, System.Security.AccessControl.AccessControlType.Allow));

        //        System.IO.Pipes.NamedPipeServerStream.MaxAllowedServerInstances
        //        using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(
        //            PipeName,
        //            PipeDirection.InOut, 
        //            MaxNumberOfServerInstances,
        //            PipeTransmissionMode.Byte,
        //            PipeOptions.WriteThrough,
        //            BufSize, // in 
        //            BufSize, // out
        //            pipeSecurity))
        //        {
        //            Pipes.Add(pipeServer);

        //            StreamReader stringReader = new StreamReader(pipeServer);
        //            StreamWriter streamWriter = new StreamWriter(pipeServer);

        //            while (true)
        //            {
        //                string receivedStr = string.Empty;
        //                int threadId = Thread.CurrentThread.ManagedThreadId;

        //                    // Wait for a client to connect
        //                log.Info("Waiting for client connect...\n");
        //                pipeServer.WaitForConnection();

        //                streamWriter.AutoFlush = true;

        //                log.Info(string.Format("Client connected on thread[{0}]." , threadId));

        //                try
        //                {
        //                    // Verify our identity to the connected client.
        //                    streamWriter.WriteLine(UUID);

        //                    // Read in the contents of the file while impersonating the client.
        //                    receivedStr = stringReader.ReadLine();

        //                    // parse
        //                    ParseAndHandle(receivedStr);
        //                }

        //                // Catch the IOException that is raised if the pipe is broken
        //                // or disconnected.
        //                catch (IOException e)
        //                {
        //                    log.Error(e.Message ,e);
        //                }
        //                finally
        //                {
        //                    if (pipeServer.IsConnected)
        //                    {
        //                      //  pipeServer.WaitForPipeDrain();
        //                        pipeServer.Flush();
        //                        pipeServer.Disconnect();
        //                    }
        //                }
        //            }
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        SkydrmApp.Log.Error(ex.Message, ex);

        //        Process.GetCurrentProcess().Close();
             
        //        Process.GetCurrentProcess().Kill();
        //    }
        //}    

        /// <summary>
        /// Parse and handle the received message.
        /// </summary>
        /// <param name="received"></param>
        private static void ParseAndHandle(string received)
        {
            int index = received.IndexOf('\0');
            if (-1 != index)
            {
                received = received.Substring(0, index);
            }

            JObject jo = (JObject)JsonConvert.DeserializeObject(received);

            // tmp path
            if (jo.ContainsKey("Intent"))
            {
                Intent intent =(Intent)Enum.Parse(typeof(Intent), jo.GetValue("Intent").ToString());
                switch (intent)
                {
                    case Intent.LeaveCopy:
                        if (SkydrmApp.Singleton.User.LeaveCopy)
                        {
                            string locaPath =jo.GetValue("obj").ToString();
                            //SkydrmApp.Singleton.User.LeaveCopy_Feature.AddFile(locaPath); // Now not used, is used by viewer in first version
                        }
                        break;

                    case Intent.SyncFileAfterEdit:
                        // For edit, actually we should design common protocol with code.
                        EditInfo editInfo = JsonConvert.DeserializeObject<EditInfo>(jo.GetValue("obj").ToString());
                        SkydrmApp.MainWin.viewModel.SyncFileAfterEditFromViewer(editInfo.LocalPath, editInfo.IsEdit);
                        break;
                }
            }
        }

        public enum Intent
        {
            LeaveCopy,
            SyncFileAfterEdit
        }

        public class Bundle<T>
        {
            public Intent Intent;
            public T obj;
        }

    }
}
