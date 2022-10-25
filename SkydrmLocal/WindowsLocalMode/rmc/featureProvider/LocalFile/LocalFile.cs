using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkydrmLocal.rmc.fileSystem.basemodel;

namespace SkydrmLocal.rmc.featureProvider.LocalFile
{
    class LocalFile : ILocalFile
    {
        readonly private SkydrmLocalApp app;
        readonly private log4net.ILog log;

        public LocalFile(SkydrmLocalApp app)
        {
            this.app = app;
            this.log = app.Log;
        }


        public IOfflineFile[] GetOfflines()
        {
            List<IOfflineFile> rt = new List<IOfflineFile>();
            // get offline files from vault, sharedwithme, and project
            foreach (var i in app.MyVault.GetOfflines())
            {               
                rt.Add(i);                
            }
            foreach (var i in app.SharedWithMe.GetOfflines())
            {
                rt.Add(i);
            }
            foreach (var i in app.MyProjects.List())
            {
                // add new fun to supprot list all offlines
                foreach (var j in i.GetOfflines())
                {
                    rt.Add(j);
                }
            }
            return rt.ToArray();
        }

        public IPendingUploadFile[] GetPendingUploads()
        {
            List<IPendingUploadFile> rt = new List<IPendingUploadFile>();
            // get local files from Vault and Project
            foreach (var i in app.MyVault.GetPendingUploads())
            {
                rt.Add(i);
            }
            foreach (var i in app.MyProjects.List())
            {
                foreach (var j in i.GetPendingUploads())
                {                   
                    rt.Add(j);
                }
            }

            return rt.ToArray();

        }
    } // end LocalFile
}
