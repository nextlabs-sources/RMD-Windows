using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Vanara.PInvoke.CldApi;

namespace nxrmvirtualdrivelib.common
{
    public struct CallbackData
    {
        public CF_CALLBACK_INFO callbackInfo;
        public CF_CALLBACK_PARAMETERS callbackParameters;
    }
}
