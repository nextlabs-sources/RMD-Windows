using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.featureProvider.externalDrive.RmsRepoMgr
{
    public interface IRmsRepoMgr
    {
        List<IRmsRepo> ListRepositories(); // Get from db
        List<IRmsRepo> SyncRepositories(); // Get from rms then insert into db

        /// <summary>
        /// Create rms repository info instance by json result after authentication results, then add info data to db.
        /// </summary>
        /// <param name="resultJson">The acquired josn result when authentication success</param>
        IRmsRepo AddRepository(string resultJson); 
        void RemoveRepository(string repoid);

        /// <summary>
        /// Get access token from rms, if acquired succeed, should call IFileRepo#UpdateToken to update current repository instance token.
        /// If local token expired, should first try to acquire from rms again by invoking this, if invoking failed 
        /// and error code is 5005(means not authorized or expired), Client should launch browser to re-authorize.
        /// </summary>
        /// <param name="repoid">repository id.</param>
        string GetAccessToken(string repoid);
        string GetAuthorizationURI(string name, ExternalRepoType type, string authUrl="");

        void UpdateRepoName(string repoid, string name);
        void UpdateRepoToken(string repoID, string token);
    }
}
