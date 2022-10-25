using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SkydrmLocal.rmc.ui.components.ValiditySpecify.model
{
    public class ValiditySpecifyConfig : INotifyPropertyChanged
    {
        private string validityDateValue = @"Access rights will ";
        private string validityCountDaysValue;
        private string blackOutDates;

        private ExpiryMode expiryMode = ExpiryMode.NEVER_EXPIRE;

        public string BlackOutDates
        {
            get
            {
                return blackOutDates;
            }
            set
            {
                blackOutDates = value;
                OnPropertyChanged("BlackOutDates");
            }
        }

        public ExpiryMode ExpiryMode
        {
            get
            {
                return expiryMode;
            }
            set
            {
                expiryMode = value;
                OnPropertyChanged("ExpiryMode");
            }
        }

        public string ValidityCountDaysValue
        {
            get
            {
                return validityCountDaysValue;
            }
            set
            {
                validityCountDaysValue = value;
                OnPropertyChanged("ValidityCountDaysValue");
            }
        }

        public string ValidityDateValue
        {
            get
            {
                return validityDateValue;
            }
            set
            {
                validityDateValue = value;
                OnPropertyChanged("ValidityDateValue");
            }
        }



        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class NeverExpireVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ExpiryMode mode = (ExpiryMode)value;
            switch (mode)
            {
                case ExpiryMode.NEVER_EXPIRE:
                    return @"Visible";
                case ExpiryMode.RELATIVE:
                case ExpiryMode.ABSOLUTE_DATE:
                case ExpiryMode.DATA_RANGE:
                    return @"Collapsed";
                default:
                    return @"Collapsed";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ValidityCountDaysVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ExpiryMode mode = (ExpiryMode)value;
            switch (mode)
            {
                case ExpiryMode.NEVER_EXPIRE:
                    return @"Collapsed";
                case ExpiryMode.RELATIVE:
                case ExpiryMode.ABSOLUTE_DATE:
                case ExpiryMode.DATA_RANGE:
                    return @"Visible";
                default:
                    return @"Collapsed";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class RelativeDateContainerVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ExpiryMode mode = (ExpiryMode)value;
            switch (mode)
            {
                case ExpiryMode.NEVER_EXPIRE:
                    return @"Collapsed";
                case ExpiryMode.RELATIVE:
                    return @"Visible";
                case ExpiryMode.ABSOLUTE_DATE:
                    return @"Collapsed";
                case ExpiryMode.DATA_RANGE:
                    return @"Collapsed";
                default:
                    return @"Collapsed";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class Calendar1VisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ExpiryMode mode = (ExpiryMode)value;
            switch (mode)
            {
                case ExpiryMode.NEVER_EXPIRE:
                    return @"Collapsed";
                case ExpiryMode.RELATIVE:
                    return @"Collapsed";
                case ExpiryMode.ABSOLUTE_DATE:
                    return @"Visible";
                case ExpiryMode.DATA_RANGE:
                    return @"Visible";
                default:
                    return @"Collapsed";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class Calendar2VisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ExpiryMode mode = (ExpiryMode)value;
            switch (mode)
            {
                case ExpiryMode.NEVER_EXPIRE:
                    return @"Collapsed";
                case ExpiryMode.RELATIVE:
                    return @"Collapsed";
                case ExpiryMode.ABSOLUTE_DATE:
                    return @"Collapsed";
                case ExpiryMode.DATA_RANGE:
                    return @"Visible";
                default:
                    return @"Collapsed";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    //for radiobutton checked status
    public class ExpiryModeToBoolConverter : IValueConverter
    {
        //ExpiryMode to radiobutton
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ExpiryMode mode = (ExpiryMode)value;
            return mode == (ExpiryMode)int.Parse(parameter.ToString());
        }
        //radiobutton to ExpiryMode
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isChecked = (bool)value;
            if (!isChecked)
            {
                return null;
            }
            return (ExpiryMode)int.Parse(parameter.ToString());
        }
    }
    public enum ExpiryMode
    {
        NEVER_EXPIRE,
        RELATIVE,
        ABSOLUTE_DATE,
        DATA_RANGE
    }

}
