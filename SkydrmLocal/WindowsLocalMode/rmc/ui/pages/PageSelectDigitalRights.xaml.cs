using SkydrmLocal.rmc.ui.pages.model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SkydrmLocal.rmc.ui.windows;
using SkydrmLocal.rmc.ui.components.ValiditySpecify.model;
using SkydrmLocal.rmc.ui.utils;
using SkydrmLocal.rmc;

namespace SkydrmLocal.Pages
{
    /// <summary>
    /// Interaction logic for PageSelectDigitalRights.xaml
    /// </summary>
    public partial class PageSelectDigitalRights : Page
    {
        private IList<string> rights = new List<string>();
        private RightsSelectConfig rightsSelectConfig = new RightsSelectConfig();

        private string initWatermark = "$(Date)$(Time)$(Break)$(User)";

        // Fix bug 53338
        public delegate void ChangeValidity(bool isValidDate);
        public event ChangeValidity ValidityDateChanged;

        public PageSelectDigitalRights()
        {
            InitializeComponent();

            //get expire date from database
            rmc.sdk.Expiration expiration = SkydrmLocalApp.Singleton.User.Expiration;

            IExpiry Expiry;
            string expireDateValue = "";

            CommonUtils.SdkExpiration2ValiditySpecifyModel(expiration, out Expiry, out expireDateValue, true);

            rightsSelectConfig.Expiry = Expiry;
            rightsSelectConfig.ExpireDateValue = expireDateValue;
            
            //get watermark from database
            if (!string.IsNullOrEmpty(SkydrmLocalApp.Singleton.User.Watermark.text))
            {
                initWatermark = SkydrmLocalApp.Singleton.User.Watermark.text;
            }
            if (initWatermark.Contains("\n"))
            {
                initWatermark = initWatermark.Replace("\n", "$(Break)");
            }
            rightsSelectConfig.Watermarkvalue = initWatermark;

            StringBuilder sb = new StringBuilder();
            CommonUtils.ConvertWatermark2DisplayStyle(initWatermark, ref sb);
            rightsSelectConfig.DispalyWatermark = sb.ToString();

            rightsSelectConfig.Rights = rights;
            rights.Add("View");
            this.stackPanel.DataContext = rightsSelectConfig;
        }

        // Project not share right
        public void SetShareIsEnable(bool isEnable=true)
        {
            this.Share.IsEnabled = isEnable;
        }

        internal RightsSelectConfig RightsSelectConfig
        {
            get { return rightsSelectConfig; }
            set { rightsSelectConfig = value; }
        }

        public void SetCheckedRights(List<string> rights)
        {
            foreach (var item in rights)
            {
                switch (item)
                {
                    case "Edit":
                        this.Edit.IsChecked = true;
                        break;
                    case "Print":
                        this.Print.IsChecked = true;
                        break;
                    case "Share":
                        this.Share.IsChecked = true;
                        break;
                    case "SaveAs":
                    case "Download":
                        this.SaveAs.IsChecked = true;
                        break;
                    case "Watermark":
                        this.Watermark.IsChecked = true;
                        break;
                }
            }
        }

        public void SetExpire(IExpiry Expiry, string expireDateValue)
        {
            rightsSelectConfig.Expiry = Expiry;
            rightsSelectConfig.ExpireDateValue = expireDateValue;
        }

        public void SetWaterMark(string waterMark)
        {
            if (!string.IsNullOrEmpty(waterMark))
            {
                initWatermark = waterMark;
            }
            if (initWatermark.Contains("\n"))
            {
                initWatermark = initWatermark.Replace("\n", "$(Break)");
            }
            rightsSelectConfig.Watermarkvalue = initWatermark;
        }
        /// <summary>
        /// Set select rights page isEnable
        /// </summary>
        /// <param name="isEnable"></param>
        public void SetIsEnable(bool isEnable=true)
        {
            this.stackPanel.IsEnabled = isEnable;
        }

        public void SetChangeWaterMarkIsVisibilty(Visibility visibility)
        {
            this.Change_WaterMark.Visibility = visibility;
        }

        public void SetChangeValidityIsVisibilty(Visibility visibility)
        {
            this.Change_Validity.Visibility = visibility;
        }

        /*
         *This is the callback of rights checkbox checked or unchecked.  
         */
        private void CheckBox_RightsChecked(object sender, RoutedEventArgs e)
        {
            CheckBox rightsCheckBox = sender as CheckBox;
            if (rightsCheckBox != null && rightsCheckBox.Name != null)
            {
                switch (rightsCheckBox.Name.ToString())
                {
                    case "Edit":
                        FillRights("Edit");
                        break;
                    case "Print":
                        FillRights("Print");
                        break;
                    case "Share":
                        FillRights("Share");
                        break;
                    case "SaveAs":
                        FillRights("SaveAs");
                        break;
                    case "Watermark":
                        rightsSelectConfig.CheckStatus = (bool)rightsCheckBox.IsChecked? CheckStatus.CHECKED: CheckStatus.UNCHECKED;
                        FillRights("Watermark");
                        break;
                    case "Decrypt":
                        FillRights("Decrypt");
                        break;
                }
            }
        }

        private void FillRights(string rightsItem)
        {
            if (rights.Contains(rightsItem))
            {
                rights.Remove(rightsItem);
            }
            else
            {
                rights.Add(rightsItem);
            }
        }

        private void ChangeValidity_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                ValiditySpecifyWindow validitySpecifyWindow = new ValiditySpecifyWindow(rightsSelectConfig.Expiry, rightsSelectConfig.ExpireDateValue);
                validitySpecifyWindow.ValidationUpdated += (ss, ee) =>
                {
                    UpdateValidity(ee.Expiry, ee.ValidityContent);
                    ValidityDateChanged?.Invoke(true);
                };
                validitySpecifyWindow.ShowDialog();
            }
            catch (Exception msg)
            {
                SkydrmLocalApp.Singleton.Log.Error("Error in ValiditySpecifyWindow:", msg);
            }
            
        }

        private void UpdateWatermarkValue_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                EditWatermarkWindow editWatermarkWindow = new EditWatermarkWindow(rightsSelectConfig.Watermarkvalue);
                editWatermarkWindow.WatermarkHandler += (ss, ee) =>
                {
                    UpdateWatermarkValue(ee.Watermarkvalue);
                };
                editWatermarkWindow.ShowDialog();
            }
            catch (Exception msg)
            {
                SkydrmLocalApp.Singleton.Log.Error("Error in EditWatermarkWindow:", msg);
            }
            
        }

        public void UpdateValidity(IExpiry expiry, string description)
        {
            int operationType = expiry.GetOpetion();
            switch (operationType)
            {
                case 0:
                    INeverExpire neverExpire = (INeverExpire)expiry;
                    break;
                case 1:
                    IRelative relative = (IRelative)expiry;
                    int years = relative.GetYears();
                    int months = relative.GetMonths();
                    int weeks = relative.GetWeeks();
                    int days = relative.GetDays();
                    Console.WriteLine("years:{0}-months:{1}-weeks:{2}-days{3}",years,months,weeks,days);
                    break;
                case 2:
                    IAbsolute absolute = (IAbsolute)expiry;
                    long endAbsDate = absolute.EndDate();
                    Console.WriteLine("absEndDate:{0}",endAbsDate);
                    break;
                case 3:
                    IRange range = (IRange)expiry;
                    long startDate = range.StartDate();
                    long endDate = range.EndDate();
                    Console.WriteLine("StartDate:{0},EndDate{1}",startDate,endDate);
                    break;
            }
            rightsSelectConfig.ExpireDateValue = description;            
            rightsSelectConfig.Expiry = expiry;
        }

        public void UpdateWatermarkValue(string value)
        {
            rightsSelectConfig.Watermarkvalue = value;

            StringBuilder sb = new StringBuilder();
            CommonUtils.ConvertWatermark2DisplayStyle(value, ref sb);
            rightsSelectConfig.DispalyWatermark = sb.ToString();

        }

        private void OnExpanded(object sender, RoutedEventArgs e)
        {
            if (this.expander.IsExpanded)
            {
                this.Decrypt.Visibility = Visibility.Visible;
            }
            else
            {
                this.Decrypt.Visibility = Visibility.Collapsed;
            }
        }

    }
}
