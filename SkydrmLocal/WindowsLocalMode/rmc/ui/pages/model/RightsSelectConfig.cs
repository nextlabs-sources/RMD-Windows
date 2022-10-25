using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Data;
using System.Globalization;
using SkydrmLocal.rmc.ui.components.ValiditySpecify.model;
using SkydrmLocal.rmc.ui.windows;
using SkydrmLocal.rmc.ui.components.ValiditySpecify;

namespace SkydrmLocal.rmc.ui.pages.model
{
    public class RightsSelectConfig : INotifyPropertyChanged
    {
        private string watermarkvalue;
        private string displayWatermark;
        private string expireDateValue;
        private int expireOperation=0;
        private IExpiry expiry= new NeverExpireImpl();
        private IList<string> rights;
        private CheckStatus checkStatus = CheckStatus.UNCHECKED;

        public CheckStatus CheckStatus
        {
            get
            {
                return checkStatus;
            }
            set
            {
                checkStatus = value;
                OnPropertyChanged("CheckStatus");
            }
        }
        public string Watermarkvalue
        {
            get
            {
                return watermarkvalue;
            }
            set
            {
                watermarkvalue = value;
                OnPropertyChanged("Watermarkvalue");
            }
        }

        public string DispalyWatermark
        {
            get
            {
                return displayWatermark;
            }
            set
            {
                displayWatermark = value;
                OnPropertyChanged("DispalyWatermark");
            }
        }

        public string ExpireDateValue
        {
            get
            {
                return expireDateValue;
            }
            set
            {
                expireDateValue = value;
                OnPropertyChanged("ExpireDateValue");
            }
        }
        public int ExpireOperation
        {
            get { return expireOperation; }
            set { expireOperation=value; }
        }
        public IList<string> Rights
        {
            get { return rights; }
            set { rights = value; }
        }

        public IExpiry Expiry { get => expiry; set => expiry = value; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public enum CheckStatus
    {
        CHECKED,
        UNCHECKED
    }

    public class WatermarkContainerVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            CheckStatus status = (CheckStatus)value;
            switch (status)
            {
                case CheckStatus.CHECKED:
                    return @"Visible";
                case CheckStatus.UNCHECKED:
                    return @"Hidden";
                default:
                    return @"Hidden";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
