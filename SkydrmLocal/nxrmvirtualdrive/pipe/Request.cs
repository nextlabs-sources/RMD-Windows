using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nxrmvirtualdrive.pipe
{
    public abstract class Request
    {
        public static string PATH_REGISTER = "/register";
        public static string PATH_UNREGISTER = "/unregister";
        public static string PATH_FETCH_SYNC_ROOTS = "/fetchsyncroots";
        public static string REMOTE_DRIVE_TYPE_NAS = "NAS";
        public static string REMOTE_DRIVE_TYPE_SHAREPOINT_FOR_NAS = "SHAREPOINT_NAS";
        public static string ANONYMOUS_USER = "{DRIVE_TYPE}_Anonymous";

        public const int STATUS_CODE_OK = 200;
        public const int STATUS_CODE_BAD_REQUEST = 400;
        public const int STATUS_CODE_EXISTS_ALREADY = 4001;
        public const int STATUS_CODE_INVALID_REQUEST_TYPE = 4003;

        public static string STATUS_OK = "OK";
        public static string STATUS_MISSING_REQUIRED_PARAMETERS = "Missing required parameters.";
        public static string STATUS_MALFORMED_REQUEST = "Malformed request.";
        public static string STATUS_INVALID_REQUEST_TYPE = "Invalid request type.";
        public static string STATUS_INVALID_DRIVE_TYPE = "Invalid drive type.";
        public static string STATUS_COMMON = "Failed to process request, please try again later.";

        public string pathId;

        public abstract Task<Response> Process();

        public static Request Deserialize(string jsonContent)
        {
            if (string.IsNullOrEmpty(jsonContent))
            {
                throw new InvalidRequestException(STATUS_CODE_BAD_REQUEST, STATUS_MALFORMED_REQUEST);
            }
            JObject requestObj = null;
            try
            {
                requestObj = JObject.Parse(jsonContent);
            }
            catch (Exception e)
            {
                throw new InvalidRequestException(STATUS_CODE_BAD_REQUEST, string.Format("Malformed request {0}.", e.Message));
            }

            var pathId = CheckPathId((string)requestObj["pathId"]);
            if (IsRegisterRequest(pathId))
            {
                return JsonConvert.DeserializeObject<RegisterSyncRootRequest>(jsonContent);
            }
            if (IsUnRegisterRequest(pathId))
            {
                return JsonConvert.DeserializeObject<UnRegisterSyncRootRequest>(jsonContent);
            }
            return JsonConvert.DeserializeObject<FetchSyncRootsRequest>(jsonContent);
        }

        public static string CheckPathId(string pathId)
        {
            if (string.IsNullOrEmpty(pathId))
            {
                throw new InvalidRequestException(STATUS_CODE_BAD_REQUEST, STATUS_MISSING_REQUIRED_PARAMETERS);
            }
            if (pathId.Equals(PATH_REGISTER, StringComparison.InvariantCultureIgnoreCase)
                || pathId.Equals(PATH_UNREGISTER, StringComparison.InvariantCultureIgnoreCase)
                || pathId.Equals(PATH_FETCH_SYNC_ROOTS, StringComparison.InvariantCultureIgnoreCase))
            {
                return pathId;
            }
            throw new InvalidRequestException(STATUS_CODE_INVALID_REQUEST_TYPE, STATUS_INVALID_REQUEST_TYPE);
        }

        public static string CheckDriveType(string driveType)
        {
            if (string.IsNullOrEmpty(driveType))
            {
                throw new InvalidRequestException(STATUS_CODE_BAD_REQUEST, STATUS_MISSING_REQUIRED_PARAMETERS);
            }
            if (driveType.Equals(REMOTE_DRIVE_TYPE_NAS, StringComparison.InvariantCultureIgnoreCase)
                || driveType.Equals(REMOTE_DRIVE_TYPE_SHAREPOINT_FOR_NAS, StringComparison.InvariantCultureIgnoreCase))
            {
                return driveType;
            }
            throw new InvalidRequestException(STATUS_CODE_INVALID_REQUEST_TYPE, STATUS_INVALID_DRIVE_TYPE);
        }

        public static bool IsRegisterRequest(string pathId)
        {
            if (string.IsNullOrEmpty(pathId))
            {
                throw new InvalidRequestException(STATUS_CODE_BAD_REQUEST, STATUS_MISSING_REQUIRED_PARAMETERS);
            }
            return PATH_REGISTER.Equals(pathId, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsUnRegisterRequest(string pathId)
        {
            if (string.IsNullOrEmpty(pathId))
            {
                throw new InvalidRequestException(STATUS_CODE_BAD_REQUEST, STATUS_MISSING_REQUIRED_PARAMETERS);
            }
            return PATH_UNREGISTER.Equals(pathId, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
