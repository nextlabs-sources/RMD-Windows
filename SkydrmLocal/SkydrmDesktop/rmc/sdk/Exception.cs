using Sdk.helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.sdk
{
    public enum RmSdkExceptionDomain
    {
        /// <summary>
        /// general and common error
        /// </summary>
        Sdk_Common = 0X0001,
        /// <summary>
        /// Network IO exception in sdk calling
        /// </summary>
        Sdk_NetworkIO = 0x0010,
        /// <summary>
        /// rest base means error is from the rest api returned by server
        /// </summary>
        Rest_Base = 0x1000, 

        Rest_MyDrive = 0x1001,
        Rest_MyVault = 0X1002,
        Rest_MyProject = 0X1004,
        Rest_MySharedWithMe = 0X1008,
        Rest_WorkSpace = 0X1010,
        Rest_SharedWorkSpace = 0X1020,
        // add another here
        NXL_Handler = 0x2000,
        RMP_Driver = 0x3000,
    }

    // method will not designed as enum, instead,we directly use string,including:
    //  session
    public enum RmSdkRestMethodKind
    {
        Genernal = 0,
        UserSession,
        HeartBeat,
        List,
        Download,
        Upload,
        Share,
        Protect,
        View,
        // project verbs
        Get,
        Create,
        Send,
        Accept,
        Decline,
        Revoke,
        Remove,

    }

    public class RmSdkException : SkydrmException
    {
        static private readonly string sGeneralMsg = CultureStringInfo.Exception_Sdk_General;
        private RmSdkExceptionDomain domain;

        public RmSdkException() :
            this(sGeneralMsg)
        {

        }

        public RmSdkException(string message) :
            this(message, ExceptionComponent.RMSDK)
        {
        }

        public RmSdkException(string message, ExceptionComponent component)
            : this(message, RmSdkExceptionDomain.Sdk_Common, component)
        {

        }

        public RmSdkException(string message, RmSdkExceptionDomain domain)
            : this(message, domain, ExceptionComponent.RMSDK)
        {
        }

        public RmSdkException(string message,
                    RmSdkExceptionDomain domain, ExceptionComponent component)
            : base(message, component)
        {
            this.domain = domain;
        }


        public RmSdkExceptionDomain Domain { get => domain; }
        public override bool IsNetworkIoException { get => base.IsNetworkIoException && domain == RmSdkExceptionDomain.Sdk_NetworkIO; }

    }

    public class ResourceIsInUseException : RmSdkException
    {
        static public readonly int SdkErrorCode = 0xAA;
        static public readonly string DefaultMsg = CultureStringInfo.Exception_Sdk_Resource_Is_In_Use;

        public ResourceIsInUseException() : base(DefaultMsg, RmSdkExceptionDomain.Sdk_Common)
        {
        }

        public ResourceIsInUseException(string message) : base(message, RmSdkExceptionDomain.Sdk_Common)
        {
        }
    }

    public class InsufficientRightsException : RmSdkException
    {
        static public readonly int SdkErrorCode = 0xE001;
        static public readonly string DefaultMsg =
            CultureStringInfo.Exception_Sdk_Insufficient_Rights;

        public InsufficientRightsException() : base(DefaultMsg, RmSdkExceptionDomain.NXL_Handler)
        {
        }

        public InsufficientRightsException(string message) : base(message, RmSdkExceptionDomain.NXL_Handler)
        {
        }
    }

    public class RmSdkNetworkIoException : RmSdkException
    {
        static public readonly string DefaultMsg =
            CultureStringInfo.Exception_Sdk_Network_IO;
        public RmSdkNetworkIoException() : base(DefaultMsg, RmSdkExceptionDomain.Sdk_NetworkIO)
        {

        }
        public RmSdkNetworkIoException(string message) : base(message, RmSdkExceptionDomain.Sdk_NetworkIO)
        {
        }
    }

    public class RmRestApiException : RmSdkException
    {
        static private readonly string sGeneralMsg = CultureStringInfo.Exception_Sdk_Rest_General;
        static private readonly int sGeneralErrorCode = -1;


        private RmSdkRestMethodKind whichMethodInDomain;
        protected int errorCode;

        public RmRestApiException() :
            this(sGeneralMsg, sGeneralErrorCode)
        {

        }

        public RmRestApiException(string message) :
            this(message, sGeneralErrorCode)
        {

        }

        public RmRestApiException(int errorCode) :
            this(sGeneralMsg, errorCode)
        {

        }

        public RmRestApiException(string message, int errorCode) :
            this(message, RmSdkExceptionDomain.Rest_Base, RmSdkRestMethodKind.Genernal, errorCode)
        {
        }

        public RmRestApiException(string message, RmSdkRestMethodKind method, int errorCode) :
            this(message, RmSdkExceptionDomain.Rest_Base, method, errorCode)
        {
        }


        public RmRestApiException(string message,
                                  RmSdkExceptionDomain domain,
                                  RmSdkRestMethodKind method,
                                  int errorCode) :
            base(message, domain, ExceptionComponent.RMSDK_REST_API)
        {
            this.whichMethodInDomain = method;
            this.errorCode = errorCode;
        }



        public int ErrorCode { get => errorCode; }

        public RmSdkRestMethodKind MethodKind { get => whichMethodInDomain; }

        public override string LogUsedMessage()
        {
            return
                "Component:" + Component.ToString() +
                "  Domain:" + Domain.ToString() +
                "  Method:" + whichMethodInDomain +
                " ReturnCode:" + errorCode +
                " Message" + Message;

        }
    }

    // for 400
    public class InvalidMalFormedParamException : RmRestApiException
    {
        private static readonly string gDefaultMsg = CultureStringInfo.Exception_Sdk_Rest_400_InvalidParam;

        public InvalidMalFormedParamException() :
            this(gDefaultMsg)
        {
        }

        public InvalidMalFormedParamException(string message) : base(message, 400)
        {
        }
    }

    // for 401
    public class SessionAuthenticationException : RmRestApiException
    {
        private static readonly string gDefaultMsg = CultureStringInfo.Exception_Sdk_Rest_401_Authentication_Failed;

        public SessionAuthenticationException() :
            this(gDefaultMsg)
        {
        }

        public SessionAuthenticationException(string message) :
            base(message, RmSdkRestMethodKind.UserSession, 401)
        {
        }
    }
    // for 403
    public class AccessForbiddenException : RmRestApiException
    {
        private static readonly string gDefaultMsg = CultureStringInfo.Exception_Sdk_Rest_403_AccessForbidden;

        public AccessForbiddenException() :
            this(gDefaultMsg)
        {
        }

        public AccessForbiddenException(string message) : base(message, 403)
        {
        }
    }

    // for error code 2
    public class SwitchServerException : RmRestApiException
    {
        private static readonly string gDefaultMsg = CultureStringInfo.Exception_Sdk_Rest_403_AccessForbidden;

        public SwitchServerException() :
            this(gDefaultMsg)
        {
        }

        public SwitchServerException(string message) : base(message, 2)
        {
        }
    }

    // for 404
    public class NotFoundException : RmRestApiException
    {
        private static readonly string gDefaultMsg = CultureStringInfo.Exception_Sdk_Rest_404_NotFound;

        public NotFoundException() :
            this(gDefaultMsg)
        {
        }

        public NotFoundException(string message) : base(message, 404)
        {
        }
    }

    // for 500
    public class ServerInternalException : RmRestApiException
    {
        private static readonly string gDefaultMsg = CultureStringInfo.Exception_Sdk_Rest_500_ServerInternal;

        public ServerInternalException()
            : this(gDefaultMsg)
        {
        }

        public ServerInternalException(string message) :
            base(message, 500)
        {
        }
    }

    // for 6001 6002
    public class StorageExceededException : RmRestApiException
    {
        private static readonly string gDefaultMsg = CultureStringInfo.Exception_Sdk_Rest_6001_StorageExceeded;

        public StorageExceededException() :
            this(gDefaultMsg)
        {
        }

        // Required to give a i18l msg
        public StorageExceededException(string message) :
            base(message, 6001)
        {
        }
    }

    public class InvalidFileNameException: RmRestApiException // e.g. file name exceed 128 characters.
    {
        private static readonly string gDefaultMsg = CultureStringInfo.Exception_Sdk_Rest_4005_InvalidFileName;
        public InvalidFileNameException() : this(gDefaultMsg) { }

        public InvalidFileNameException(string msg):base(msg, 4005) { }
    }

}
