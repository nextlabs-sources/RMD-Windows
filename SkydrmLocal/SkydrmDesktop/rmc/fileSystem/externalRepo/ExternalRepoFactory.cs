using Microsoft.Office.Interop.Excel;
using Microsoft.VisualBasic.ApplicationServices;
using SkydrmDesktop.rmc.featureProvider.externalDrive;
using SkydrmDesktop.rmc.featureProvider.externalDrive.RmsRepoMgr;
using SkydrmDesktop.rmc.featureProvider.externalDrive.sharepoint;
using SkydrmDesktop.rmc.fileSystem.externalDrive;
using SkydrmDesktop.rmc.fileSystem.externalDrive.externalBase;
using SkydrmLocal.rmc.fileSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.fileSystem.externalRepo
{
    public class ExternalRepoFactory
    {
        public static ExternalRepo Create(IRmsRepo rmsRepo)
        {
            ExternalRepo externalRepo = null;
            IExternalDrive drive = null;
            switch (rmsRepo.Type)
            {
                case ExternalRepoType.GOOGLEDRIVE:
                    drive = new NxGoogleDrive(rmsRepo);
                    break;
                case ExternalRepoType.DROPBOX:
                    drive = new NxDropBox(rmsRepo);
                    break;
                case ExternalRepoType.BOX:
                    drive = new NxBox(rmsRepo);
                    break;
                // todo: wait for father impl
                case ExternalRepoType.ONEDRIVE:
                    drive = new NxOneDrive(rmsRepo);
                    break;
                case ExternalRepoType.SHAREPOINT:
                    drive = new NxSharePointOnPremise(rmsRepo);
                    break;
                case ExternalRepoType.SHAREPOINT_ONLINE:
                    drive = new NxSharePointOnline(rmsRepo);
                    break;
                case ExternalRepoType.SHAREPOINT_ONPREMISE:
                    drive = new NxSharePointOnPremise(rmsRepo);
                    break;
                    //default:
                    //    throw new Exception("Unsupported External Drive");

            }

            if(drive != null)
            {
                externalRepo = new ExternalRepo(drive);
            }

            return externalRepo;
        }

    }
}
