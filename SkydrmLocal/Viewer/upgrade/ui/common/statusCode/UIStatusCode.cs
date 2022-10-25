using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.upgrade.ui.common.statusCode
{
    public class UIStatusCode
    {
       // public const UInt64 NETWORK_AVAILABLE = 0x00000000;
        public const UInt64 CANNOT_RENDER_THE_FILE = 0x00000001;
        public const UInt64 ROTATE_BTN_VISIBLE = 0x00000002;
        public const UInt64 EXTRACT_BTN_VISIBLE = 0x00000004;
        public const UInt64 SAVE_AS_BTN_VISIBLE = 0x00000008;
        public const UInt64 EDIT_BTN_VISIBLE = 0x00000010;
        public const UInt64 PRINT_BTN_VISIBLE = 0x00000020;
        public const UInt64 FILE_INFO_BTN_VISIBLE = 0x00000040;
        public const UInt64 PROTECT_BTN_VISIBLE = 0x00000080;
        public const UInt64 SHARE_BTN_VISIBLE = 0x00000100;
        public const UInt64 VIEWER_CONTEN_VISIBLE = 0x00000200;
        public const UInt64 LOADING_BAR_VISIBLE = 0x00000400;
        public const UInt64 ERROR_INFO_CONTAINE_VISIBLE = 0x00000800;
    }
}
