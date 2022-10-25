using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Print
{
    public class ParseJson
    {

        //Decapsulate base64 into a json string (UTF8 )
        public static string Decodeing(string base64str)
        {
            return Encoding.UTF8.GetString(System.Convert.FromBase64String(base64str));
        }

        public static PrintParameters Parse(string json)
        {
            PrintParameters launchParameters = new PrintParameters();

            JObject jo = (JObject)JsonConvert.DeserializeObject(json);

            if (jo.ContainsKey("IntPtrOfOverlayWindow"))
            {
                launchParameters.IntPtrOfOverlayWindow = (int)jo["IntPtrOfOverlayWindow"];
            }

            if (jo.ContainsKey("IntPtrOfViewerWindow"))
            {
                launchParameters.IntPtrOfViewerWindow = (int)jo["IntPtrOfViewerWindow"];
            }

            if (jo.ContainsKey("CopyedFilePath"))
            {
                launchParameters.CopyedFilePath = jo["CopyedFilePath"].ToString();
            }

            if (jo.ContainsKey("AdhocWatermark"))
            {
                launchParameters.AdhocWatermark = jo["AdhocWatermark"].ToString();
            }

            return launchParameters;
        }

        public class PrintParameters
        {
            public int IntPtrOfOverlayWindow { get; set; }

            public int IntPtrOfViewerWindow { get; set; }

            public string CopyedFilePath { get; set; }

            public string AdhocWatermark { get; set; }

            public PrintParameters()
            {
                #region init member variable
                IntPtrOfOverlayWindow = -1;
                IntPtrOfViewerWindow = -1;
                CopyedFilePath = string.Empty;
                AdhocWatermark = string.Empty;
                #endregion
            }
        }

    }
}
