using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace nxrmvirtualdrive.pipe
{
    public class NamedPipeServer
    {
        private const int NUM_THREADS = 1;
        private const int BUFFER_SIZE = 4096;
        private Thread[] m_servers = new Thread[NUM_THREADS];
        private static readonly int remoteUserSessionId = System.Diagnostics.Process.GetCurrentProcess().SessionId;
        private static readonly string PIPE_NAME = "nxrmvirtualdrive_" + remoteUserSessionId.ToString();

        private bool m_exitPipeServer;

        public void Start()
        {
            for (int i = 0; i < NUM_THREADS; i++)
            {
                m_servers[i] = new Thread(ServerThread);
                m_servers[i].Start();
            }
        }

        public void Stop()
        {
            m_exitPipeServer = true;
        }

        private async void ServerThread(object data)
        {
            SecurityIdentifier identifier = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);
            PipeSecurity pipeSecurity = new PipeSecurity();
            pipeSecurity.SetAccessRule(new PipeAccessRule(identifier, PipeAccessRights.ReadWrite, AccessControlType.Allow));

            using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(PIPE_NAME, PipeDirection.InOut,
                NUM_THREADS,
                PipeTransmissionMode.Message, PipeOptions.None,
                BUFFER_SIZE, BUFFER_SIZE,
                pipeSecurity))
            {
                do
                {
                    pipeServer.WaitForConnection();

                    StreamString streamString = new StreamString(pipeServer);
                    try
                    {
                        var content = streamString.ReadRequest();
                        var response = await content.Process();
                        streamString.Write(response);
                    }
                    catch (InvalidRequestException e)
                    {
                        streamString.Write(new Response(e.StatusCode, e.Message));
                    }
                    catch (Exception e)
                    {
                        streamString.Write(new Response(500, e.Message));
                    }

                    pipeServer.Disconnect();

                } while (!m_exitPipeServer);
            }
        }

    }
}
