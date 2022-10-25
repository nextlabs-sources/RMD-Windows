using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.featureProvider.externalDrive.errorHandler
{
    public class RepoApiException: Exception
    {
        public ErrorCode ErrorCode { get; private set; }

        public RepoApiException(string message) : base(message)
        {
            this.ErrorCode = ErrorCode.Common;
        }

        public RepoApiException(string msg, ErrorCode code) : base(msg)
        {
            this.ErrorCode = code;
        }
    }

    public enum ErrorCode
    {
        Common,
        ParamInvalid,
        AccessTokenExpired,  
        NetWorkIOFailed,
        IllegalOperation,
        NamingCollided,
        NameTooLong,
        DriveStorageExceed,
        FileAlreadyExist,
        InternalServerError,
        NotFound
    }
}
