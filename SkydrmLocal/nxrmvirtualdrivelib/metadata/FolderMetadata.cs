using nxrmvirtualdrivelib.core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nxrmvirtualdrivelib.metadata
{
    public class FolderMetadata : Metadatabase, IFolderMetadata
    {
        public override long Length
        {
            get => 0;
            set => throw new NotImplementedException();
        }
    }
}
