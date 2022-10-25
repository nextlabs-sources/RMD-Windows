using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nxrmvirtualdrivelib.core
{
    public interface IValidateData
    {
        void Validate(long offset, long length, bool valid);
    }
}
