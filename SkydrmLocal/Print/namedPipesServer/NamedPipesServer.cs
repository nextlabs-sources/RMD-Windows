using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using Alphaleonis.Win32.Filesystem;
using System.IO.Pipes;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using static Print.ParseJson;

namespace Print.namedPipesServer
{
    public class NamedPipesServer
    {
        public static int numThreads = 1;
        public static int MaxNumberOfServerInstances = 1;

        public static BlockingCollection<NamedPipeServerStream> Pipes = new BlockingCollection<NamedPipeServerStream>();

        public static PrintApplication mPrintApplication = null;

        public static Thread[] Servers = null;

        public static ParameterizedThreadStart[] ParameterizedThreadStarts = null;

        public static void Launch(PrintApplication printApplication)
        {
            mPrintApplication = printApplication;

            Servers = new Thread[numThreads];

            ParameterizedThreadStarts = new ParameterizedThreadStart[numThreads];
            ParameterizedThreadStarts[0] = PrintServer;

            for (int i = 0; i < numThreads; i++)
            {
                Servers[i] = new Thread(ParameterizedThreadStarts[i]);
                Servers[i].Priority = ThreadPriority.Normal;
                Servers[i].Start();
            }
        }


        public static void ShudownActivePipes()
        {
            foreach (NamedPipeServerStream item in Pipes)
            {
                if (null != item)
                {
                    item.Close();
                }
            }

            for (int i = 0; i < numThreads; i++)
            {
                try
                {
                    Servers[i].Abort();

                }
                catch (Exception ex)
                {

                }
            }
        }

        private static void PrintServer(object data)
        {
            //try
            //{
            //    string PipeName = "aee3867b-c030-419d-bd80-a9d719c382e2";
            //    PipeSecurity pipeSecurity = new PipeSecurity();
            //    pipeSecurity.AddAccessRule(new PipeAccessRule(WindowsIdentity.GetCurrent().Owner, PipeAccessRights.FullControl, AccessControlType.Allow));

            //    using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(
            //        PipeName,
            //        PipeDirection.InOut,
            //        MaxNumberOfServerInstances,
            //        PipeTransmissionMode.Byte,
            //        PipeOptions.WriteThrough,
            //        1024,
            //        1024,
            //        pipeSecurity
            //        ))
            //    {
            //        Pipes.Add(pipeServer);

            //        StreamReader stringReader = new StreamReader(pipeServer);
            //        StreamWriter streamWriter = new StreamWriter(pipeServer);

            //        while (true)
            //        {
            //            string receivedStr = string.Empty;
            //            int threadId = Thread.CurrentThread.ManagedThreadId;

            //            // Wait for a client to connect
            //            pipeServer.WaitForConnection();

            //            streamWriter.AutoFlush = true;

            //            try
            //            {
            //                receivedStr = stringReader.ReadLine();

            //                PrintParameters startParameters = ParseJson.Parse(ParseJson.Decodeing(receivedStr));

            //             //   bool b = mPrintApplication.PrepareNextPrintFile(startParameters);

            //                if (b)
            //                {
            //                    streamWriter.WriteLine("true");
            //                    mPrintApplication.StartPrint(startParameters);
            //                }
            //                else
            //                {
            //                    streamWriter.WriteLine("false");
            //                }
            //            }
            //            // Catch the IOException that is raised if the pipe is broken
            //            // or disconnected.
            //            catch (IOException e)
            //            {

            //            }
            //            finally
            //            {
            //                if (pipeServer.IsConnected)
            //                {
            //                    pipeServer.WaitForPipeDrain();
            //                    pipeServer.Flush();
            //                    pipeServer.Disconnect();
            //                }
            //            }
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show("System internal error. Please contact your system administrator for further help.");
            //    PrintApplication.Current.Shutdown();
            //    Process.GetCurrentProcess().Kill();
            //    Environment.Exit(0);
            //}
        }
    }
}
