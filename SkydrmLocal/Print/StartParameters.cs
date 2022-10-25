using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Print
{
    public class StartParameters
    {
        public int IntPtrOfWindowOwner { get; set; }

        public string NxlFilePath { get; set; }

        public StartParameters(int intPtrOfWindowOwner, string NxlFilePath)
        {
            #region init member variable
            this.IntPtrOfWindowOwner = intPtrOfWindowOwner;
            this.NxlFilePath = NxlFilePath;
            #endregion
        }
    }
}
