using SkydrmDesktop;
using SkydrmDesktop.Resources.languages;
using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.ui.utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using static SkydrmLocal.rmc.ui.windows.FileInformationWindow;

namespace SkydrmLocal.rmc.ui.windows.fileInformation.helper
{
    public class LocalFileRights2ResouceConverter : IValueConverter
    {


        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // WrapperFileRights fileRights = (WrapperFileRights)(value.GetType().GetField(value.ToString()));
            WrapperFileRights fileRights = (WrapperFileRights)value;
            switch (fileRights)
            {
                case WrapperFileRights.RIGHT_VIEW:
                    return @"/rmc/resources/icons/rights_view.png";

                case WrapperFileRights.RIGHT_SHARE:
                    return @"/rmc/resources/icons/rights_share.png";

                case WrapperFileRights.RIGHT_PRINT:
                    return @"/rmc/resources/icons/rights_print.png";

                case WrapperFileRights.RIGHT_DOWNLOAD:
                    return @"/rmc/resources/icons/rights_download.png";

                case WrapperFileRights.RIGHT_WATERMARK:
                    return @"/rmc/resources/icons/rights_watermark.png";

                case WrapperFileRights.RIGHT_VALIDITY:
                    return @"/rmc/resources/icons/rights_validity.png";
                    
                case WrapperFileRights.RIGHT_EDIT:
                    return @"/rmc/resources/icons/rights_edit.png";

                case WrapperFileRights.RIGHT_SAVEAS:
                    return @"/rmc/resources/icons/rights_save_as.png";

                case WrapperFileRights.RIGHT_DECRYPT:
                    return @"/rmc/resources/icons/rights_extract2.png";
                default:
                    return @"/rmc/resources/icons/Icon_access_denied.png";
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DisplayWaterMark2DisplayWaterMarkVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string s = (string)value;
            if (string.IsNullOrEmpty(s))
            {
                return Visibility.Collapsed;
            }
            else
            {
                return Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ListCount2BoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int)value > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string displayExpiration = (string)value;
            if (displayExpiration == "Expired")
            {
                return @"#EB5757";
            }
            return @"Gray";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NameToBackground : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return NameConvertHelper.SelectionBackgroundColor(value.ToString());

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NameToForeground : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return NameConvertHelper.SelectionTextColor(value.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class CheckoutFirstChar : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (string.IsNullOrEmpty(value.ToString()))
            {
                return "";
            }
            else
            {
                return value.ToString().Substring(0, 1).ToUpper();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ValidityHidenProperty2ValidityVisiblitiyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool hiden = (bool)value;
            return hiden ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ShareWithCount2StringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int count = 0;
            count = (int)value;
            string result;
            if (count>1)
            {
                result = CultureStringInfo.ApplicationFindResource("FileInfoWin_Members");
            }
            else
            {
                result = CultureStringInfo.ApplicationFindResource("FileInfoWin_Member");
            }
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class OriginalFileVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                string path = value.ToString();

                if (string.IsNullOrEmpty(path))
                {
                    return Visibility.Hidden;
                }
                return Visibility.Visible;
            }
            catch (Exception e)
            {
                SkydrmApp.Singleton.Log.Error(e.Message, e);
            }
            return Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class LastModifyDateVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                string date = value.ToString();
                DateTime result;
                if (string.IsNullOrEmpty(date) || date.Equals("UnKnown") || !DateTime.TryParse(date, out result))
                {
                    return Visibility.Hidden;
                }
                return Visibility.Visible;
            }
            catch (Exception e)
            {
                SkydrmApp.Singleton.Log.Error(e.Message, e);
            }
            return Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ShareWithStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                Visibility visibility = (Visibility)value;
                if (visibility == Visibility.Visible)
                {
                    return CultureStringInfo.ApplicationFindResource("FileInfoWin_Shared_With");
                }
                else
                {
                    return CultureStringInfo.ApplicationFindResource("FileInfoWin_Shared_By");
                }
            }
            catch (Exception e)
            {
                SkydrmApp.Singleton.Log.Error(e.Message, e);
            }
            return CultureStringInfo.ApplicationFindResource("FileInfoWin_Shared_With");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
