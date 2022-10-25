using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.fileSystem.basemodel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.Edit
{
    public interface IEditFeature
    {
        /// <summary>
        /// Flag that the file whether is editing
        /// </summary>
        bool IsEditing { get; }

        /// <summary>
        /// For edit offline file from main window
        /// </summary>
        void EditFromMainWin(INxlFile nxlFile, Action<IEditComplete> onFinishedCallback);

        /// <summary>
        /// Handle wether sync the file again from rms when conflict occurs.
        /// <param name="nxlFile">old nxl file</param>
        /// <param name="updatedFile">updated nxl file</param>
        void HandleIfSyncFromRms(INxlFile nxlFile, Action<INxlFile> updatedFile);

        /// <summary>
        /// Check nxl file version from rms to see the file whether is updated or not.
        /// </summary>
        void CheckVersionFromRms(INxlFile nxl, Action<bool> callback);

        /// <summary>
        /// Update the edited file to rms
        /// </summary>
        void UpdateToRms(INxlFile nxlFile, Action<INxlFile> updated = null);
    }
}
