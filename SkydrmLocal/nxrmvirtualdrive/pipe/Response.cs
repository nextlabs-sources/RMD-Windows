using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nxrmvirtualdrive.pipe
{
    public class Response
    {
        public int statusCode;
        public string message;

        public Response(int statusCode, string message)
        {
            this.statusCode = statusCode;
            this.message = message;
        }

        public string Serialize()
        {
            return Serialize(this);
        }

        public static string Serialize(Response response)
        {
            return JsonConvert.SerializeObject(response);
        }
    }
}
