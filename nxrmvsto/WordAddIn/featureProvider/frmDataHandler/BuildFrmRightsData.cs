using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinFormControlLibrary;
using System.Windows.Input;
using System.Drawing;

namespace WordAddIn.featureProvider
{
    public class BuildFrmRightsData
    {
        private RightsSelectDataModel dataModel;

        // use for protect .nxl file
        public BuildFrmRightsData(Bitmap fileIcon, string filePath, bool adRdIsEnable, string waterMark, Expiration expiration, 
            HashSet<Rights> rights, Classification[] classifications,bool infoTextVisible, bool skipBtnVisible)
            : this(fileIcon, filePath, adRdIsEnable, waterMark, expiration, classifications, infoTextVisible, skipBtnVisible)
        {
            if (adRdIsEnable)
            {
                dataModel.SelectedRights = rights;
            }
        }

        // use for protect normal file
        public BuildFrmRightsData(Bitmap fileIcon, string filePath, bool adRdIsEnable, string waterMark, Expiration expiration,
            Classification[] classifications, bool infoTextVisible = true, bool skipBtnVisible = false, bool positiveBtnIsEnable = true,
            string positiveBtnContent = "Protect", string cancelBtnContent="Cancel")
        {
            dataModel = new RightsSelectDataModel()
            {
                FileIcon = fileIcon,
                FilePath = filePath,
                AdhocRadioIsEnable = adRdIsEnable,
                Classifications = classifications,
                IsInfoTextVisible= infoTextVisible,
                IsSkipBtnVisible = skipBtnVisible,
                IsPositiveBtnIsEnable = positiveBtnIsEnable,
                PositiveBtnContent = positiveBtnContent,
                CancelBtnContent = cancelBtnContent
            };

            if (classifications.Length == 0)
            {
                dataModel.IsWarningVisible = true;
            }

            if (adRdIsEnable)
            {
                dataModel.Watermark = waterMark;
                dataModel.Expiry = expiration;
            }
        }

        public RightsSelectDataModel DataModel { get => dataModel;}

    }
}
