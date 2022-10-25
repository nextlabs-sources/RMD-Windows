using SkydrmLocal.rmc.ui.components.ValiditySpecify;
using SkydrmLocal.rmc.ui.components.ValiditySpecify.model;
using SkydrmLocal.rmc.ui.utils;
using SkydrmLocal.rmc.ui.windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SkydrmLocal.rmc.ui.pages
{
    /// <summary>
    /// Interaction logic for PagePreferenceDocument.xaml
    /// </summary>
    public partial class PagePreferenceDocument : Page
    {
        private Window parentWindow;
        public Window ParentWindow
        {
            get { return parentWindow; }
            set { parentWindow = value; }
        }
        // For judge apply button isEnable
        private bool isInvalidWaterMark = false;

        public PagePreferenceDocument()
        {
            InitializeComponent();
            //get watermark from database
            string initValue = "$(User)$(Date)$(Break)$(Time)";
            if ( !string.IsNullOrEmpty(SkydrmLocalApp.Singleton.User.Watermark.text))
            {
                initValue = SkydrmLocalApp.Singleton.User.Watermark.text;
            }
            if (initValue.Contains("\n"))
            {
                initValue=initValue.Replace("\n", "$(Break)");
            }
            editWaterMark.doInit(initValue);

            //get expire date from database
            rmc.sdk.Expiration expiration = SkydrmLocalApp.Singleton.User.Expiration;

            //get ExpireDateValue
            IExpiry expiry = null;
            string expireDateValue = "";

            CommonUtils.SdkExpiration2ValiditySpecifyModel(expiration, out expiry, out expireDateValue, true);
       
            this.ValidityComponent.doInitial(expiry, expireDateValue);
        }

        private void Edit_InvalidInputEvent(bool IsInvalid)
        {
            if (IsInvalid)
            {
                this.BtnSave.IsEnabled = false;
                this.BtnApply.IsEnabled = false;
                this.isInvalidWaterMark = true;
            }
            else
            {
                this.BtnSave.IsEnabled = true;
                this.BtnApply.IsEnabled = true;
                this.isInvalidWaterMark = false;
            }
        }

        private void Validity_DateChanged(bool IsChange)
        {
            if (IsChange && !isInvalidWaterMark)
            {
                this.BtnApply.IsEnabled = true;
            }
        }

        private void SaveDocumentPre()
        {
            string selected = editWaterMark.ConvertPresetValue2String();
            string trimmed = selected.Trim();

            if (string.IsNullOrEmpty(trimmed))
            {
                CustomMessageBoxWindow.Show(CultureStringInfo.EditWatermark_DlgBox_Title,
                CultureStringInfo.EditWatermark_DlgBox_Subject,
                CultureStringInfo.EditWatermark_DlgBox_Details,
                CustomMessageBoxWindow.CustomMessageBoxIcon.Warning,
                CustomMessageBoxWindow.CustomMessageBoxButton.BTN_CLOSE);
                return;
            }
            //set watermark to database
            rmc.sdk.WaterMarkInfo markInfo = new sdk.WaterMarkInfo();
            markInfo.text = trimmed;
            SkydrmLocalApp.Singleton.User.Watermark = markInfo;

            //set expire date to database
            IExpiry expiry = null;
            string validityContent = CultureStringInfo.ValidityWin_Never_Description2;
            this.ValidityComponent.GetExpireValue(out expiry, out validityContent);
            //
            rmc.sdk.Expiration expiration = new rmc.sdk.Expiration();
            CommonUtils.ValiditySpecifyModel2SdkExpiration(out expiration, expiry, validityContent, true);

            SkydrmLocalApp.Singleton.User.Expiration = expiration;

            SkydrmLocalApp.Singleton.User.UpdateUserPreference();
            //SkydrmLocalApp.Singleton.ShowBalloonTip("Save succcessfully !");
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            if (parentWindow != null)
            {
                parentWindow.Close();
            }
        }

        private void Save_Button_Click(object sender, RoutedEventArgs e)
        {
            SaveDocumentPre();
            if (parentWindow != null)
            {
                parentWindow.Close();
            }
        }

        private void Apply_Button_Click(object sender, RoutedEventArgs e)
        {
            SaveDocumentPre();
            this.BtnApply.IsEnabled = false;
        }

    }
}
