using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using Viewer.upgrade.ui.common.statusCode;

namespace Viewer.upgrade.ui.nxl.page.toolbar.viewModel
{
    public class RotateVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility result = Visibility.Collapsed;
            UInt64 statusCode = (UInt64)value;
            if ((statusCode & UIStatusCode.ROTATE_BTN_VISIBLE) == UIStatusCode.ROTATE_BTN_VISIBLE)
            {
                result = Visibility.Visible;
            }
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ExtractVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility result = Visibility.Collapsed;
            UInt64 statusCode = (UInt64)value;
            if ((statusCode & UIStatusCode.EXTRACT_BTN_VISIBLE) == UIStatusCode.EXTRACT_BTN_VISIBLE)
            {
                result = Visibility.Visible;
            }
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class SaveAsVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility result = Visibility.Collapsed;
            UInt64 statusCode = (UInt64)value;
            if ((statusCode & UIStatusCode.SAVE_AS_BTN_VISIBLE) == UIStatusCode.SAVE_AS_BTN_VISIBLE)
            {
                result = Visibility.Visible;
            }
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EditVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility result = Visibility.Collapsed;
            UInt64 statusCode = (UInt64)value;
            if ((statusCode & UIStatusCode.EDIT_BTN_VISIBLE) == UIStatusCode.EDIT_BTN_VISIBLE)
            {
                result = Visibility.Visible;
            }
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public class PrintVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility result = Visibility.Collapsed;
            UInt64 statusCode = (UInt64)value;
            if ((statusCode & UIStatusCode.PRINT_BTN_VISIBLE) == UIStatusCode.PRINT_BTN_VISIBLE)
            {
                result = Visibility.Visible;
            }
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class FileInfoVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility result = Visibility.Collapsed;
            UInt64 statusCode = (UInt64)value;
            if ((statusCode & UIStatusCode.FILE_INFO_BTN_VISIBLE) == UIStatusCode.FILE_INFO_BTN_VISIBLE)
            {
                result = Visibility.Visible;
            }
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ProtectVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility result = Visibility.Collapsed;
            UInt64 statusCode = (UInt64)value;
            if ((statusCode & UIStatusCode.PROTECT_BTN_VISIBLE) == UIStatusCode.PROTECT_BTN_VISIBLE)
            {
                result = Visibility.Visible;
            }
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ShareVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility result = Visibility.Collapsed;
            UInt64 statusCode = (UInt64)value;
            if ((statusCode & UIStatusCode.SHARE_BTN_VISIBLE) == UIStatusCode.SHARE_BTN_VISIBLE)
            {
                result = Visibility.Visible;
            }
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
