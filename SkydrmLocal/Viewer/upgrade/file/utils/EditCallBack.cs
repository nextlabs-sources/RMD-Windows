using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.upgrade.file.basic.utils
{
    public class EditCallBack
    {
        public bool IsEdit { get; set; }
        public string LocalPath { get; set; }
        public EditCallBack(bool ie, string lp)
        {
            this.IsEdit = ie;
            this.LocalPath = lp;
        }
    }
}
