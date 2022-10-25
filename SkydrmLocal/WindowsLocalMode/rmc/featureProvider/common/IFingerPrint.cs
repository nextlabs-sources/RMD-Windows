using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.featureProvider.common
{
    public interface IFingerPrint
    {
        FileRights[] Rights { get; }
  
        Dictionary<string, List<string>> Tags { get; }

        string RawTags { get; }

        string WaterMark { get; }

        Expiration Expiration { get; }

    }
}
