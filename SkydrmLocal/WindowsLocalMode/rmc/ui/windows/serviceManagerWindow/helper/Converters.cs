using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.ui.utils;
using SkydrmLocal.rmc.ui.windows.mainWindow.helper;
using SkydrmLocal.rmc.ui.windows.mainWindow.model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SkydrmLocal.rmc.ui.windows.serviceManagerWindow.helper
{
    /// <summary>
    /// Convert local file status map image to display.
    /// </summary>
    public class Status2ImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            EnumNxlFileStatus status = (EnumNxlFileStatus)value;
            switch (status)
            {
                case EnumNxlFileStatus.UploadSucceed:
                    return @"/rmc/resources/icons/file_upload_success.png";
                case EnumNxlFileStatus.Uploading:
                    return @"/rmc/resources/icons/Icon_blue_circle.png";
                case EnumNxlFileStatus.UploadFailed:
                    return @"/rmc/resources/icons/file_upload_failure.png";
                case EnumNxlFileStatus.WaitingUpload:
                    return null;
                case EnumNxlFileStatus.DownLoadedFailed:
                    return @"/rmc/resources/icons/file_upload_failure.png";
                case EnumNxlFileStatus.DownLoadedSucceed:
                    return @"/rmc/resources/icons/file_upload_success.png";
                case EnumNxlFileStatus.Downloading:
                    return @"/rmc/resources/icons/Icon_blue_circle.png";
                case EnumNxlFileStatus.AvailableOffline:
                //case EnumNxlFileStatus.AvailableOffline_Edited:
                //case EnumNxlFileStatus.CachedFile_Edited:
                //    return @"/rmc/resources/icons/file_upload_success.png";
                case EnumNxlFileStatus.CachedFile:
                    return @"/rmc/resources/icons/file_upload_success.png";
                case EnumNxlFileStatus.Online:
                    return @"/rmc/resources/icons/file_upload_success.png";
                case EnumNxlFileStatus.ProtectFailed:
                    return @"/rmc/resources/icons/file_upload_failure.png";
                case EnumNxlFileStatus.ProtectSucceeded:
                    return @"/rmc/resources/icons/file_upload_success.png";
                default:
                    return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class LocalUploadStatus2ImageUploadArrowVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            EnumNxlFileStatus status = (EnumNxlFileStatus)value;
            switch (status)
            {
                case EnumNxlFileStatus.UploadSucceed:
                    return Visibility.Collapsed;
                case EnumNxlFileStatus.Uploading:
                case EnumNxlFileStatus.WaitingUpload:
                    return Visibility.Visible;
                case EnumNxlFileStatus.UploadFailed:
                    return Visibility.Visible;
                case EnumNxlFileStatus.Downloading:
                    return Visibility.Visible;
                case EnumNxlFileStatus.DownLoadedSucceed:
                    return Visibility.Collapsed;
                case EnumNxlFileStatus.DownLoadedFailed:
                    return Visibility.Visible;
               // case EnumNxlFileStatus.AvailableOffline_Edited:
                //case EnumNxlFileStatus.CachedFile_Edited:
                //    return Visibility.Visible;
                default:
                    return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class LocalUploadStatus2TextConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            EnumNxlFileStatus status = (EnumNxlFileStatus)values[0];
            DateTime dateTime=(DateTime)values[1];

            switch (status)
            {
                case EnumNxlFileStatus.UploadSucceed:
                    return DateStringFromNow(dateTime, status);

                case EnumNxlFileStatus.Uploading:
                    return CultureStringInfo.ServiceManageWin_Updating;

                case EnumNxlFileStatus.UploadFailed:
                    return CultureStringInfo.MainWin_List_UploadFailed;

                case EnumNxlFileStatus.WaitingUpload:
                    return CultureStringInfo.ServiceManageWin_Waiting_Upload;

                case EnumNxlFileStatus.RemovedFromLocal:
                    return CultureStringInfo.ServiceManageWin_Removed_Local;

                case EnumNxlFileStatus.DownLoadedFailed:
                    return CultureStringInfo.ServiceManageWin_Downloaded_Failed;

                case EnumNxlFileStatus.DownLoadedSucceed:
                    return DateStringFromNow(dateTime, status);

                case EnumNxlFileStatus.Downloading:
                    return CultureStringInfo.ServiceManageWin_Downloading;

                case EnumNxlFileStatus.FileMissingInLocal:
                    return CultureStringInfo.ServiceManageWin_File_Missing_In_Local;

                case EnumNxlFileStatus.UnknownError:
                    return CultureStringInfo.ServiceManageWin_UnknownError;

                case EnumNxlFileStatus.AvailableOffline:
            
                    return CultureStringInfo.ServiceManageWin_AvailableOffline;

                //case EnumNxlFileStatus.AvailableOffline_Edited:
                //case EnumNxlFileStatus.CachedFile_Edited:
                //    return CultureStringInfo.ServiceManageWin_Edit_In_Local;

                case EnumNxlFileStatus.Online:
                    return CultureStringInfo.ServiceManageWin_Online;

                case EnumNxlFileStatus.CachedFile:
                    return CultureStringInfo.ServiceManageWin_CachedFile;

                case EnumNxlFileStatus.ProtectFailed:
                    return CultureStringInfo.ServiceManageWin_ProtectFailed;

                case EnumNxlFileStatus.ProtectSucceeded:
                    return CultureStringInfo.ServiceManageWin_ProtectSucceeded;

                default:
                    return null;
            }
        }

        public string DateStringFromNow(DateTime dt, EnumNxlFileStatus status)
        {
            string result=string.Empty;

            TimeSpan span = DateTime.Now - dt;
 
            if (span.TotalDays > 60)
            {
                result = dt.ToShortDateString();
            }
            else if (span.TotalDays > 30)
            {
                result = CultureStringInfo.ServiceManageWin_One_Month;
            }
            else if (span.TotalDays > 14)
            {
                result = CultureStringInfo.ServiceManageWin_Two_Weeks;
            }
            else if (span.TotalDays > 7)
            {
                result = CultureStringInfo.ServiceManageWin_One_Week;
            }

            else if (span.TotalDays >= 1 && span.TotalDays<2)
            {
                result = string.Format("{0} {1}",
                (int)Math.Floor(span.TotalDays), CultureStringInfo.ServiceManageWin_Day_Ago);
            }
            else if (span.TotalDays >= 2)
            {
                result = string.Format("{0} {1}",
                (int)Math.Floor(span.TotalDays), CultureStringInfo.ServiceManageWin_Days_Ago);
            }
            else if (span.TotalHours >= 1 && span.TotalHours < 2)
            {
                result = string.Format("{0} {1} ", (int)Math.Floor(span.TotalHours), CultureStringInfo.ServiceManageWin_Hour_Ago);
            }
            else if (span.TotalHours >= 2)
            {
                result = string.Format("{0} {1} ", (int)Math.Floor(span.TotalHours), CultureStringInfo.ServiceManageWin_Hours_Ago);
            }
            else if (span.TotalMinutes >= 1 && span.TotalMinutes< 2) 
            {
                result = string.Format("{0} {1}", (int)Math.Floor(span.TotalMinutes), CultureStringInfo.ServiceManageWin_Minute_Ago);
            }
            else if (span.TotalMinutes >= 2)
            {
                result = string.Format("{0} {1}", (int)Math.Floor(span.TotalMinutes), CultureStringInfo.ServiceManageWin_Minutes_Ago);
            }
            else if (span.TotalMilliseconds >= 0)
            {
                switch (status)
                {
                    case EnumNxlFileStatus.UploadSucceed:
                        result = CultureStringInfo.ServiceManageWin_Uploaded_Just;
                        break;
                    case EnumNxlFileStatus.DownLoadedSucceed:
                        result = CultureStringInfo.ServiceManageWin_Downloaded_Succeed;
                        break;
                    default:
                        result = "";
                        break;
                }
              
            }

            else
            {
                switch (status)
                {
                    case EnumNxlFileStatus.UploadSucceed:
                        result = CultureStringInfo.ServiceManageWin_Uploaded_Just;
                        break;
                    case EnumNxlFileStatus.DownLoadedSucceed:
                        result = CultureStringInfo.ServiceManageWin_Downloaded_Succeed;
                        break;
                    default:
                        result = "";
                        break;
                }
            }

            switch (status)
            {
                case EnumNxlFileStatus.UploadSucceed:

                    if (!String.Equals(result, CultureStringInfo.ServiceManageWin_Uploaded_Just, StringComparison.CurrentCultureIgnoreCase))
                    {
                        result = CultureStringInfo.ServiceManageWin_Uploaded + " " + result;
                    }
                    break;
                case EnumNxlFileStatus.DownLoadedSucceed:
                    if (!String.Equals(result, CultureStringInfo.ServiceManageWin_Downloaded_Succeed, StringComparison.CurrentCultureIgnoreCase))
                    {
                        result = CultureStringInfo.ServiceManageWin_Downloaded_Succeed + " " + result;
                    }
                    break;
                default:
                    result = "";
                    break;
            }

            return result;

        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class Status2ExceptionTextVisibilityConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            EnumNxlFileStatus status = (EnumNxlFileStatus)value;
            switch (status)
            {
                case EnumNxlFileStatus.UploadSucceed:
                    return Visibility.Collapsed;
                case EnumNxlFileStatus.Uploading:
                    return Visibility.Collapsed;
                case EnumNxlFileStatus.UploadFailed:
                    return Visibility.Visible; 
                case EnumNxlFileStatus.WaitingUpload:
                    return Visibility.Collapsed;
                case EnumNxlFileStatus.DownLoadedFailed:
                    return Visibility.Visible;
                default:
                    return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public class LocalUploadStatus2ImageRedOrBlueCirclrConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            EnumNxlFileStatus status = (EnumNxlFileStatus)value;
            switch (status)
            {
                case EnumNxlFileStatus.UploadSucceed:
                    return @"/rmc/resources/icons/success.png";
                case EnumNxlFileStatus.Uploading:
                    return @"/rmc/resources/icons/Icon_blue_circle.png";
                case EnumNxlFileStatus.UploadFailed:
                    return @"/rmc/resources/icons/Icon_red_circle.png";

                default:
                    return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public class LocalFileUploadStatus2ProgressBarForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            EnumNxlFileStatus status = (EnumNxlFileStatus)value;
            SolidColorBrush solidColorBrush = null;
            switch (status)
            {
                //#4468B1
                case EnumNxlFileStatus.UploadSucceed:
                    solidColorBrush = new SolidColorBrush
                    {
                        Color = Color.FromArgb(
                               255,    // Specifies the transparency of the color.  
                               68,    // Specifies the amount of red.  
                               104,      // specifies the amount of green.  
                               177)     // Specifies the amount of blue.  
                    };
                    return solidColorBrush;
                case EnumNxlFileStatus.Uploading:
                    solidColorBrush = new SolidColorBrush
                    {
                        Color = Color.FromArgb(
                               255,    // Specifies the transparency of the color.  
                               68,    // Specifies the amount of red.  
                               104,      // specifies the amount of green.  
                               177)     // Specifies the amount of blue.  
                    };
                    return solidColorBrush;
                 
                case EnumNxlFileStatus.UploadFailed:
                    //#EB5757
                    solidColorBrush = new SolidColorBrush
                    {
                        Color = Color.FromArgb(
                           255,    // Specifies the transparency of the color.  
                           235,    // Specifies the amount of red.  
                           87,      // specifies the amount of green.  
                           87)     // Specifies the amount of blue.  
                    };
                    return solidColorBrush;         
                default:
                    return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Using to Converter network status(bool type) to Image flag
    /// </summary>
    public class NetworkStatusBool2ShortLineImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
            {
                return @"/rmc/resources/icons/online_long_short.png";
            }
            else
            {
                return @"/rmc/resources/icons/Icon_offline_short_line.png";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    /// <summary>
    /// Using to Converter network status(bool type) to Image flag
    /// </summary>
    public class NetworkStatusBool2LongLineImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
            {
                return @"/rmc/resources/icons/online_long_line.png";
            }
            else
            {
                return @"/rmc/resources/icons/Icon_offline_long_line.png";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Using to Converter network status(bool type) to prompt info(string type)
    /// </summary>
    public class NetworkStatusBool2StringInfo : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
            {
                return CultureStringInfo.STATUS_ON_LINE;
            }
            else
            {
                return CultureStringInfo.STATUS_OFF_LINE;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NetworkStatusBool2StringForeground : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
            {
                return "Green";
            }
            else
            {
                return "Red";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class Collection2GuideVisibl : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
           int count= (int)value;
            if (count>0)
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

    /// <summary>
    /// Using to Converter network status(bool type) to line status(online\offline) color
    /// </summary>
    public class NetStatusBool2LineColor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
            {
                return new SolidColorBrush(Color.FromRgb(39, 174, 96));
            }
            else
            {
                return new SolidColorBrush(Color.FromRgb(255, 0, 0));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convert local file status map image to display.
    /// </summary>
    public class LocalFileStatus2ImageConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            string name = (string)value[0];
            EnumNxlFileStatus status = (EnumNxlFileStatus)value[1];
            string originFilename = "";
            int lastindex = name.LastIndexOf('.');
            if (lastindex != -1)
            {
                originFilename = name.Substring(0, lastindex);
            }
            string fileType = "---";
            if (originFilename.LastIndexOf('.') > 0)
            {
                fileType = originFilename.Substring(originFilename.LastIndexOf('.') + 1).ToLower();
                if (string.Equals(fileType, "3dxml", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "bmp", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "c", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "catpart", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "catshape", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "cgr", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "cpp", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "csv", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "doc", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "docm", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "docx", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "dotx", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "dwg", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "dxf", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "err", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "exe", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "ext", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "file", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "gif", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "h", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "hsf", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(fileType, "hwf", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "igs", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "iges", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "ipt", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "java", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "jpg", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "js", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "json", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "jt", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "log", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "m", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "md", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "model", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "par", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "pdf", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "png", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "potm", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "potx", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "ppt", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "pptx", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "properties", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "prt", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "psm", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "py", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "rft", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "rh", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "rtf", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "sldasm", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "sldprt", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "sql", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "step", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "stl", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "stp", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "swift", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "tif", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "tiff", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "txt", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "vb", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "vds", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "vsd", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "vsdx", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "x_b", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "xls", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "xlsb", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "xlsm", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "xlsx", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "xlt", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "xltm", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "xltx", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "xml", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "xmt_txt", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "x_t", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(fileType, "zip", StringComparison.CurrentCultureIgnoreCase)
                 )
                {
                    try
                    {
                        string uritemp_d = string.Format(@"/rmc/resources/fileimages/{0}_d.ico", fileType);
                        var stream_d = new Uri(uritemp_d, UriKind.Relative);
                        BitmapImage bitmap_d = new BitmapImage(stream_d);

                        string uritemp_o = string.Format(@"/rmc/resources/fileimages/{0}_o.ico", fileType);
                        var stream_o = new Uri(uritemp_o, UriKind.Relative);
                        BitmapImage bitmap_o = new BitmapImage(stream_o);

                        string uritemp_u = string.Format(@"/rmc/resources/fileimages/{0}_u.ico", fileType);
                        var stream_u = new Uri(uritemp_u, UriKind.Relative);
                        BitmapImage bitmap_u = new BitmapImage(stream_u);

                        string uritemp_w = string.Format(@"/rmc/resources/fileimages/{0}_w.ico", fileType);
                        var stream_w = new Uri(uritemp_w, UriKind.Relative);
                        BitmapImage bitmap_w = new BitmapImage(stream_w);

                        string uritemp_s = string.Format(@"/rmc/resources/fileimages/{0}_s.ico", fileType);
                        var stream_s = new Uri(uritemp_s, UriKind.Relative);
                        BitmapImage bitmap_s = new BitmapImage(stream_s);

                        string uritemp_g = string.Format(@"/rmc/resources/fileimages/{0}_g.ico", fileType);
                        var stream_g = new Uri(uritemp_g, UriKind.Relative);
                        BitmapImage bitmap_g = new BitmapImage(stream_g);

                        if (bitmap_d != null && bitmap_o != null && bitmap_u != null && bitmap_w != null&& bitmap_g!=null)
                        {

                // case EnumNxlFileStatus.CachedFile:
                //    return @"/rmc/resources/icons/Icon_cachedFile.ico";
                //case EnumNxlFileStatus.AvailableOffline:
                //    return @"/rmc/resources/icons/Icon_availableOffline.ico";
                //case EnumNxlFileStatus.Uploading:
                //    return @"/rmc/resources/icons/uploading.ico";
                //case EnumNxlFileStatus.WaitingUpload:
                //    return @"/rmc/resources/icons/Icon_waitinigUpload.ico";
                //case EnumNxlFileStatus.UploadSucceed:                  
                //    return @"/rmc/resources/icons/Icon_cachedFile.ico";
                //case EnumNxlFileStatus.UploadFailed:
                //    return @"/rmc/resources/icons/Icon_waitinigUpload.ico";
                //case EnumNxlFileStatus.RemovedFromLocal:
                //    return @"/rmc/resources/icons/Icon_waitinigUpload.ico";

                            switch (status)
                            {
                                case EnumNxlFileStatus.CachedFile:
                                    return bitmap_d;
                                case EnumNxlFileStatus.AvailableOffline:
                                //case EnumNxlFileStatus.AvailableOffline_Edited:
                                //case EnumNxlFileStatus.CachedFile_Edited:
                                    return bitmap_o;
                                case EnumNxlFileStatus.Uploading:
                                    return bitmap_u;
                                case EnumNxlFileStatus.WaitingUpload:
                                    return bitmap_w;
                                case EnumNxlFileStatus.UploadSucceed:
                                    return bitmap_d;
                                case EnumNxlFileStatus.UploadFailed:
                                    return bitmap_w;
                                case EnumNxlFileStatus.RemovedFromLocal:
                                    return bitmap_w;
                                case EnumNxlFileStatus.Downloading:
                                    return bitmap_u;
                                case EnumNxlFileStatus.DownLoadedSucceed:
                                    return bitmap_d;
                                case EnumNxlFileStatus.DownLoadedFailed:
                                    return bitmap_g;
                                case EnumNxlFileStatus.FileMissingInLocal:
                                    return bitmap_g;
                                case EnumNxlFileStatus.Online:
                                    return bitmap_d;
                                case EnumNxlFileStatus.UnknownError:
                                    return bitmap_g;
                                case EnumNxlFileStatus.ProtectFailed:
                                    return bitmap_g;
                                case EnumNxlFileStatus.ProtectSucceeded:
                                    return bitmap_d;
                                default:
                                    return bitmap_g;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        switch (status)
                        {
                            case EnumNxlFileStatus.CachedFile:
                                return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_d.ico", UriKind.Relative));
                            case EnumNxlFileStatus.AvailableOffline:
                            //case EnumNxlFileStatus.AvailableOffline_Edited:
                            //case EnumNxlFileStatus.CachedFile_Edited:
                                return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_o.ico", UriKind.Relative));
                            case EnumNxlFileStatus.Uploading:
                                return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_u.ico", UriKind.Relative));
                            case EnumNxlFileStatus.WaitingUpload:
                                return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_w.ico", UriKind.Relative));
                            case EnumNxlFileStatus.UploadSucceed:
                                return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_d.ico", UriKind.Relative));
                            case EnumNxlFileStatus.UploadFailed:
                                return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_w.ico", UriKind.Relative));
                            case EnumNxlFileStatus.RemovedFromLocal:
                                return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_w.ico", UriKind.Relative));
                            case EnumNxlFileStatus.DownLoadedSucceed:
                                return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_d.ico", UriKind.Relative));
                            case EnumNxlFileStatus.DownLoadedFailed:
                                return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_g.ico", UriKind.Relative));
                            case EnumNxlFileStatus.Downloading:
                                return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_u.ico", UriKind.Relative));
                            case EnumNxlFileStatus.FileMissingInLocal:
                                return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_g.ico", UriKind.Relative));
                            case EnumNxlFileStatus.Online:
                                return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_d.ico", UriKind.Relative));
                            case EnumNxlFileStatus.UnknownError:
                                return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_g.ico", UriKind.Relative));
                            case EnumNxlFileStatus.ProtectFailed:
                                return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_g.ico", UriKind.Relative));
                            case EnumNxlFileStatus.ProtectSucceeded:
                                return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_d.ico", UriKind.Relative));
                            default:
                                return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_g.ico", UriKind.Relative));
                        }
                    }
                }
            }
            switch (status)
            {
                case EnumNxlFileStatus.CachedFile:
                    return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_d.ico", UriKind.Relative));
                case EnumNxlFileStatus.AvailableOffline:
                //case EnumNxlFileStatus.AvailableOffline_Edited:
                //case EnumNxlFileStatus.CachedFile_Edited:
                    return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_o.ico", UriKind.Relative));
                case EnumNxlFileStatus.Uploading:
                    return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_u.ico", UriKind.Relative));
                case EnumNxlFileStatus.WaitingUpload:
                    return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_w.ico", UriKind.Relative));
                case EnumNxlFileStatus.UploadSucceed:
                    return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_d.ico", UriKind.Relative));
                case EnumNxlFileStatus.UploadFailed:
                    return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_w.ico", UriKind.Relative));
                case EnumNxlFileStatus.RemovedFromLocal:
                    return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_w.ico", UriKind.Relative));
                case EnumNxlFileStatus.DownLoadedSucceed:
                    return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_d.ico", UriKind.Relative));
                case EnumNxlFileStatus.DownLoadedFailed:
                    return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_g.ico", UriKind.Relative));
                case EnumNxlFileStatus.Downloading:
                    return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_u.ico", UriKind.Relative));
                case EnumNxlFileStatus.Online:
                    return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_d.ico", UriKind.Relative));
                case EnumNxlFileStatus.FileMissingInLocal:
                    return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_g.ico", UriKind.Relative));
                case EnumNxlFileStatus.UnknownError:
                    return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_g.ico", UriKind.Relative));
                case EnumNxlFileStatus.ProtectFailed:
                    return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_g.ico", UriKind.Relative));
                case EnumNxlFileStatus.ProtectSucceeded:
                    return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_d.ico", UriKind.Relative));
                default:
                    return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_g.ico", UriKind.Relative));
            }

        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public class LocalFileStatus2ImageOfArrowHeadConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            EnumNxlFileStatus status = (EnumNxlFileStatus)value;
            switch (status)
            {
                case EnumNxlFileStatus.CachedFile:
                    return null;
                case EnumNxlFileStatus.AvailableOffline:
                    return null;
                case EnumNxlFileStatus.Uploading:
                    return new BitmapImage(new Uri(@"/rmc/resources/icons/uploading_sign.png", UriKind.Relative));
                case EnumNxlFileStatus.WaitingUpload:
                    return new BitmapImage(new Uri(@"/rmc/resources/icons/uploading_sign.png", UriKind.Relative));
                case EnumNxlFileStatus.UploadSucceed:
                    return new BitmapImage(new Uri(@"/rmc/resources/icons/uploading_sign.png", UriKind.Relative));
                case EnumNxlFileStatus.UploadFailed:
                    return new BitmapImage(new Uri(@"/rmc/resources/icons/uploading_sign.png", UriKind.Relative));
                case EnumNxlFileStatus.RemovedFromLocal:
                    return null;
                case EnumNxlFileStatus.Downloading:
                    return new BitmapImage(new Uri(@"/rmc/resources/icons/icon_down_arrow.png", UriKind.Relative));
                case EnumNxlFileStatus.DownLoadedSucceed:
                    return new BitmapImage(new Uri(@"/rmc/resources/icons/icon_down_arrow.png", UriKind.Relative));
                case EnumNxlFileStatus.DownLoadedFailed:
                    return new BitmapImage(new Uri(@"/rmc/resources/icons/icon_down_arrow.png", UriKind.Relative));
                case EnumNxlFileStatus.FileMissingInLocal:
                    return null;
                case EnumNxlFileStatus.UnknownError:
                    return null;
                //case EnumNxlFileStatus.AvailableOffline_Edited:
                //case EnumNxlFileStatus.CachedFile_Edited:
                //    return new BitmapImage(new Uri(@"/rmc/resources/icons/uploading_sign.png", UriKind.Relative));
                default:
                    return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


}
