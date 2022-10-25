using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.upgrade.application
{
    public class AppStatusCode
    {
        public const UInt64 DEFAULT = 0x00;
        public const UInt64 INTERNAL_ERROR = 0x01;
        public const UInt64 REQUEST_USER_LOGIN = 0x02;
        public const UInt64 RPM_DRIVER_DOES_NOT_EXIST = 0x04;
        public const UInt64 ADD_TO_WHITE_LIST_FAILED = 0x08;
        public const UInt64 INITIAL_DATABASE_FAILED = 0x10;
        public const UInt64 INITIAL_ESSENTIAL_FAILED = 0x20;
        public const UInt64 INITIAL_LOG_FAILED = 0x40;
    }
}
