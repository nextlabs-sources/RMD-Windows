using Newtonsoft.Json;
using SkydrmLocal.rmc.drive;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Viewer.namedPipesClient
{
    public class NamedPipesClient
    {
        public static bool Register(RegisterInfo registerInfo)
        {
            string PipeName = "544336d7-9086-4369-a9d0-3691ea290376";
            string UUID = "8986207c-5161-436a-abe9-dfc365c89820";

            bool result = false;
            NamedPipeClientStream pipeClient = null;
            try
            {
                pipeClient = new NamedPipeClientStream(".", PipeName,
                    PipeDirection.InOut, PipeOptions.None);

                string receivedStr = string.Empty;

                ViewerApp.Log.Info("\t\t Connecting to server... \r\n");
                pipeClient.Connect(5000);

                StreamReader stringReader = new StreamReader(pipeClient);
                StreamWriter streamWriter = new StreamWriter(pipeClient);
                streamWriter.AutoFlush = true;

                receivedStr = stringReader.ReadLine();

                // Validate the server's signature string
                if (receivedStr.Equals(UUID, StringComparison.CurrentCultureIgnoreCase))
                {
                    // The client security token is sent with the first write.
                    // Send the name of the file whose contents are returned
                    // by the server.
                    ViewerApp.Log.InfoFormat("\t\t register process id:{0} , is need register App:{1} \r\n", registerInfo.ProcessId, registerInfo.IsNeedRegisterApp);
                    string json = JsonConvert.SerializeObject(registerInfo);

                    streamWriter.WriteLine(json);

                    receivedStr = stringReader.ReadLine();

                    if (receivedStr.Equals("true", StringComparison.CurrentCultureIgnoreCase))
                    {
                        result = true;
                    }

                    Console.Write(result);
                    ViewerApp.Log.InfoFormat("\t\t register result:{0} \r\n", result);
                }
                else
                {
                    ViewerApp.Log.Info("\t\t Server could not be verified. \r\n");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
            }
            finally
            {
                if (null != pipeClient)
                {
                    if (pipeClient.IsConnected)
                    {
                        pipeClient.Flush();
                        pipeClient.Dispose();
                    }
                }
            }
            return result;
        }


        public enum PrintNamedPipeServiceResponse
        {
            NoResponse = 0,

            Available = 1,

            Unavailable = 2
        }

        public enum InsertPrintTaskQueueResponse
        {
            NoResponse = 0,

            Connecting = 1,

            Succeeded = 2,

            Failed = 3
        }

        static NamedPipeClientStream pipeClient = null;

        public static InsertPrintTaskQueueResponse StartOfficeFilePrint(string base64Str)
        {
            InsertPrintTaskQueueResponse result = InsertPrintTaskQueueResponse.NoResponse;
            string PipeName = "aee3867b-c030-419d-bd80-a9d719c382e2";
            try
            {
                if (null == pipeClient)
                {
                    pipeClient = new NamedPipeClientStream(".", PipeName,
                    PipeDirection.InOut, PipeOptions.None);
                }

                if (pipeClient.IsConnected)
                {
                    return result = InsertPrintTaskQueueResponse.Connecting;
                }

                StreamWriter streamWriter = new StreamWriter(pipeClient);
                StreamReader stringReader = new StreamReader(pipeClient);
                string receivedStr = string.Empty;

                ViewerApp.Log.Info("\t\t Connecting to server... \r\n");
                pipeClient.Connect(500);

                streamWriter.AutoFlush = true;
                streamWriter.WriteLine(base64Str);

                receivedStr = stringReader.ReadLine();

                if (receivedStr.Equals("true", StringComparison.CurrentCultureIgnoreCase))
                {
                    result = InsertPrintTaskQueueResponse.Succeeded;
                }
                else
                {
                    result = InsertPrintTaskQueueResponse.Failed;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());           
            }
            finally
            {
                if (null != pipeClient)
                {
                    if (pipeClient.IsConnected)
                    {
                        pipeClient.Flush();
                        pipeClient.Dispose();
                        pipeClient = null;
                    }
                }
            }
            return result;
        }

        //public class RegisterInfo
        //{
        //    public int ProcessId { get; set; }
        //    public bool IsNeedRegisterApp { get; set;}
        //    public RegisterInfo(int processId, bool isNeedRegisterApp)
        //    {
        //        this.ProcessId = processId;
        //        this.IsNeedRegisterApp = isNeedRegisterApp;
        //    }
        //}
    }
}


