using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using VirtualDrive;
using VirtualDrive.register;

namespace nxrmvirtualdrive.pipe
{
    public class RegisterSyncRootRequest : Request
    {
        public Parameters parameters;

        public override async Task<Response> Process()
        {
            if (parameters == null)
            {
                return new Response(STATUS_CODE_BAD_REQUEST, STATUS_MALFORMED_REQUEST);
            }
            var syncRootIdFormatter = ((VirtualDriveApp)Application.Current).SyncRootId;
            var clientPathFormatter = ((VirtualDriveApp)Application.Current).ClientFolder;
            var version = ((VirtualDriveApp)Application.Current).Version;

            var driveType = CheckDriveType(parameters.remoteDriveType).ToUpper();
            var syncRootId = syncRootIdFormatter.Replace("[DRIVE_TYPE]", driveType)
                .Replace("[REMOTE_USER]", ANONYMOUS_USER.Replace("{DRIVE_TYPE}", driveType));

            var syncRootAddr = parameters.remoteAddress;
            var displayName = parameters.displayName;
            if (string.IsNullOrEmpty(syncRootAddr) || string.IsNullOrEmpty(displayName))
            {
                return new Response(STATUS_CODE_BAD_REQUEST, STATUS_MISSING_REQUIRED_PARAMETERS);
            }

            var clientPath = clientPathFormatter.Replace("{DISPLAY_NAME}", displayName.ToLower());
            var recycleBinUri = string.IsNullOrEmpty(parameters.recycleBin) ? null : new Uri(parameters.recycleBin);

            if (ProviderRegister.IsRegistered(syncRootId, displayName))
            {
                return new Response(STATUS_CODE_EXISTS_ALREADY, string.Format("Cloud provider for {0} with display name {1} already exists.", syncRootAddr, displayName));
            }

            var iconPath = ((VirtualDriveApp)Application.Current).AppPath + @"\Resources\DriveIcon.ico";
            var config = new RegisterConfig
            {
                Id = syncRootId,
                ServerPath = syncRootAddr,
                ClientPath = clientPath,
                DisplayName = displayName,
                IconResource = iconPath,
                Version = version,
                RecyleBin = recycleBinUri
            };

            if (!Directory.Exists(clientPath))
            {
                Directory.CreateDirectory(clientPath);
            }

            await ProviderRegister.RegisterAsync(config);

            await ((VirtualDriveApp)Application.Current).StartEngineAsync(syncRootId);

            return new RegisterSyncRootResponse(STATUS_CODE_OK, STATUS_OK, new RegisterSyncRootResponse.Results(syncRootId));
        }

        public class Parameters
        {
            public string displayName;
            public string remoteAddress;
            public string remoteDriveType;
            public string recycleBin;
        }
    }

}
