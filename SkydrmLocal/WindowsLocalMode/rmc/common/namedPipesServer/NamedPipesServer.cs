using Newtonsoft.Json;
using SkydrmLocal.rmc.drive;
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

namespace SkydrmLocal.rmc.namePipesServer
{
    public class NamedPipesServer
    {
        public static SkydrmLocalApp SkydrmLocalApp;
        public static log4net.ILog log ;
        public static int numThreads = 1;
        public static int MaxNumberOfServerInstances = 1;
        public static string PipeName = "544336d7-9086-4369-a9d0-3691ea290376";
        public static string UUID = "8986207c-5161-436a-abe9-dfc365c89820";

        public static BlockingCollection<NamedPipeServerStream> Pipes = new BlockingCollection<NamedPipeServerStream>();
        public static Thread[] Servers;

        public static void Launch()
        {
            SkydrmLocalApp = SkydrmLocalApp.Singleton;
            log = SkydrmLocalApp.Singleton.Log;
            Servers = new Thread[numThreads];

            log.Info("\n*** Named pipe server stream ***\n");
            log.Info("Waiting for client connect...\n");
            
            for (int i = 0; i < numThreads; i++)
            {
                Servers[i] = new Thread(ServerThread);
                Servers[i].Priority = ThreadPriority.Highest;
                Servers[i].Start();
            }
        }

        public static void ShudownActivePipes()
        {
            foreach (NamedPipeServerStream item in Pipes)
            {
                if (null!=item)
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

        private static void ServerThread(object data)
        {
            try
            {

          //  PipeSecurity pipeSecurity = new PipeSecurity();
          //  pipeSecurity.AddAccessRule(new PipeAccessRule(WindowsIdentity.GetCurrent().Owner, PipeAccessRights.FullControl, AccessControlType.Allow));
                SecurityIdentifier si = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);
                PipeSecurity pipeSecurity = new PipeSecurity();
                pipeSecurity.SetAccessRule(new PipeAccessRule(
                    si, PipeAccessRights.ReadWrite,
                    System.Security.AccessControl.AccessControlType.Allow));

                using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(
                PipeName,
                PipeDirection.InOut, 
                MaxNumberOfServerInstances,
                PipeTransmissionMode.Byte,
                PipeOptions.WriteThrough,
                1024,
                1024,
                pipeSecurity
                ))
            {
                Pipes.Add(pipeServer);

                StreamReader stringReader = new StreamReader(pipeServer);
                StreamWriter streamWriter = new StreamWriter(pipeServer);

                while (true)
                {
                    string receivedStr = string.Empty;
                    int threadId = Thread.CurrentThread.ManagedThreadId;

                    // Wait for a client to connect
                    pipeServer.WaitForConnection();

                    streamWriter.AutoFlush = true;

                    log.Info(string.Format("Client connected on thread[{0}]." , threadId));

                    try
                    {
                        // Verify our identity to the connected client using a
                        streamWriter.WriteLine(UUID);
                        // Read in the contents of the file while impersonating the client.
                        receivedStr = stringReader.ReadLine();

                        RegisterInfo registerInfo = JsonConvert.DeserializeObject<RegisterInfo>(receivedStr);
                        streamWriter.WriteLine(AuditOffice.Register(SkydrmLocalApp, registerInfo));

                        // Display the name of the user we are impersonating.
                        log.Info(string.Format("Reading file: {0} on thread[{1}] as user: {2}." , receivedStr , threadId , pipeServer.GetImpersonationUserName()));

                    }
                    // Catch the IOException that is raised if the pipe is broken
                    // or disconnected.
                    catch (IOException e)
                    {
                        log.Error(e.Message ,e);
                    }
                    finally
                    {
                        if (pipeServer.IsConnected)
                        {
                            pipeServer.WaitForPipeDrain();
                            pipeServer.Flush();
                            pipeServer.Disconnect();
                        }
                    }
                }
            }

            }
            catch (Exception ex)
            {
                SkydrmLocalApp.Singleton.ShowBalloonTip(CultureStringInfo.Exception_Sdk_General);

                Process.GetCurrentProcess().Close();
             
                Process.GetCurrentProcess().Kill();
            }
        }    
    }

    // Contains the method executed in the context of the impersonated user
    public class AuditOffice
    {     
        public static Int32 StringToInt32(string strProcessId)
        {
           Int32 ProcessId = -1;  
           Int32.TryParse(strProcessId , out ProcessId);
           return ProcessId;
        }

        public static bool Register(SkydrmLocalApp skydrmLocalApp , RegisterInfo registerInfo)
        {       
            bool result = false;
            try
            {
                if (null != skydrmLocalApp)
                {
                    Process process = Process.GetProcessById(registerInfo.ProcessId);
                    if (registerInfo.IsNeedRegisterApp)
                    {
                        string fullPath = Path.GetFullPath(process.MainModule.FileName);
                        skydrmLocalApp.Rmsdk.RPM_RegisterApp(fullPath);
                        skydrmLocalApp.Log.Info("RPM_RegisterApp "+ fullPath);
                    }
                    // SkydrmLocalApp.Rmsdk.SDWL_RPM_NotifyRMXStatus(true);
                    result = skydrmLocalApp.Rmsdk.RMP_AddTrustedProcess(process.Id);
                    skydrmLocalApp.Log.Info("RMP_AddTrustedProcess " + process.Id);
                }
            }
            catch (Exception ex)
            {
                skydrmLocalApp.Log.Error(ex.Message, ex);
            }
            return result;
        }
    }

    [Serializable]
    public class RegisterInfo
    {
        public int ProcessId { get; set; }
        public bool IsNeedRegisterApp { get; set; }
        public RegisterInfo(int processId, bool isNeedRegisterApp)
        {
            this.ProcessId = processId;
            this.IsNeedRegisterApp = isNeedRegisterApp;
        }
    }

}
