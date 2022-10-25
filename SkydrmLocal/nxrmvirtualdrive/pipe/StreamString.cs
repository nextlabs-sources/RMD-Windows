using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nxrmvirtualdrive.pipe
{
    public class StreamString
    {
        private const int BUFFER_SIZE = 4096;
        private Stream m_ioStream;

        public StreamString(Stream stream)
        {
            this.m_ioStream = stream;
        }

        public string Read()
        {
            byte[] buffer = new byte[BUFFER_SIZE];
            m_ioStream.Read(buffer, 0, buffer.Length);

            return Encoding.UTF8.GetString(buffer);
        }

        public Request ReadRequest()
        {
            var content = Read();
            if (string.IsNullOrEmpty(content))
            {
                throw new InvalidRequestException(Request.STATUS_CODE_BAD_REQUEST, Request.STATUS_MALFORMED_REQUEST);
            }
            content = content.Remove(content.IndexOf("\0"));
            return Request.Deserialize(content);
        }

        public int Write(Response response)
        {
            return Write(response.Serialize());
        }

        public int Write(string data)
        {
            byte[] dataBuffer = Encoding.UTF8.GetBytes(data);
            int len = dataBuffer.Length;
            if (len > BUFFER_SIZE)
            {
                len = BUFFER_SIZE;
            }
            m_ioStream.Write(dataBuffer, 0, len);
            m_ioStream.Flush();
            return len;
        }
    }
}
