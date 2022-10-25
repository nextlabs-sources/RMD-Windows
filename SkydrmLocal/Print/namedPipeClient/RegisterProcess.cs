//using Newtonsoft.Json;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;
//using System.IO.Pipes;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Print.namedPipeClient
//{
//    public class RegisterProcess
//    {
//        public static bool Register(RegisterInfo registerInfo)
//        {
//            // Named pipe should associate user session id for supporting multiple users login at the same time by RDP(Windows server).
//            string PipeName = "544336d7-9086-4369-a9d0-3691ea290376" + "_sid_" + Process.GetCurrentProcess().SessionId.ToString();
//            string UUID = "8986207c-5161-436a-abe9-dfc365c89820";

//            bool result = false;
//            NamedPipeClientStream pipeClient = null;
//            try
//            {
//                pipeClient = new NamedPipeClientStream(".", PipeName,
//                    PipeDirection.InOut, PipeOptions.None);

//                string receivedStr = string.Empty;

//                Console.WriteLine("Connecting to server...\n");
//                pipeClient.Connect(5000);

//                StreamReader stringReader = new StreamReader(pipeClient);
//                StreamWriter streamWriter = new StreamWriter(pipeClient);
//                streamWriter.AutoFlush = true;

//                receivedStr = stringReader.ReadLine();

//                // Validate the server's signature string
//                if (receivedStr.Equals(UUID, StringComparison.CurrentCultureIgnoreCase))
//                {
//                    // The client security token is sent with the first write.
//                    // Send the name of the file whose contents are returned
//                    // by the server.

//                    string json = JsonConvert.SerializeObject(registerInfo);

//                    streamWriter.WriteLine(json);

//                    receivedStr = stringReader.ReadLine();

//                    if (receivedStr.Equals("true", StringComparison.CurrentCultureIgnoreCase))
//                    {
//                        result = true;
//                    }

//                    Console.Write(result);
//                }
//                else
//                {
//                    Console.WriteLine("Server could not be verified.");
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine(ex.Message.ToString());
//            }
//            finally
//            {
//                if (null != pipeClient)
//                {
//                    if (pipeClient.IsConnected)
//                    {
//                        pipeClient.Flush();
//                        pipeClient.Dispose();
//                    }
//                }
//            }
//            return result;
//        }
//    }


//    public class RegisterInfo
//    {
//        public int ProcessId { get; set; }
//        public bool IsNeedRegisterApp { get; set; }
//        public RegisterInfo(int processId, bool isNeedRegisterApp)
//        {
//            this.ProcessId = processId;
//            this.IsNeedRegisterApp = isNeedRegisterApp;
//        }
//    }

//}
