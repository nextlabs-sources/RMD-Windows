using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SkydrmLocal.rmc.ui.utils;
using SkydrmLocal.rmc.ui.windows.mainWindow.model;

namespace SkydrmLocal.rmc.ui.windows.mainWindow.helper
{
    /// <summary>
    /// Convert local file status to bool to select large context menu or samll context menu.
    /// </summary>
    public class FileStatusEnum2Bool : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            EnumLocalNxlFileStatus status = (EnumLocalNxlFileStatus)value;
            switch (status)
            {
                case EnumLocalNxlFileStatus.AvailableOffline:
                    return true;
                default: // other file status, will popup small context menu.
                    break;
            }
            return false;
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
            EnumLocalNxlFileStatus status = (EnumLocalNxlFileStatus)value[1];
            int lastindex = name.LastIndexOf('.');
            string originFilename = name.Substring(0, lastindex);
            string fileType = "---";
            if (originFilename.LastIndexOf('.') > CommonUtils.NxlFileDatetimeLength)
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
                        BitmapImage bitmap_d = new BitmapImage(new Uri(uritemp_d, UriKind.Relative));

                        string uritemp_o = string.Format(@"/rmc/resources/fileimages/{0}_o.ico", fileType);
                        BitmapImage bitmap_o = new BitmapImage(new Uri(uritemp_o, UriKind.Relative));

                        string uritemp_u = string.Format(@"/rmc/resources/fileimages/{0}_u.ico", fileType);
                        BitmapImage bitmap_u = new BitmapImage(new Uri(uritemp_u, UriKind.Relative));

                        string uritemp_w = string.Format(@"/rmc/resources/fileimages/{0}_w.ico", fileType);
                        BitmapImage bitmap_w = new BitmapImage(new Uri(uritemp_w, UriKind.Relative));

                        string uritemp_s = string.Format(@"/rmc/resources/fileimages/{0}_s.ico", fileType);
                        BitmapImage bitmap_s = new BitmapImage(new Uri(uritemp_s, UriKind.Relative));

                        if (bitmap_d != null && bitmap_o != null && bitmap_u != null && bitmap_w != null)
                        {
                            switch (status)
                            {
                                case EnumLocalNxlFileStatus.CachedFile:
                                    return bitmap_d;
                                case EnumLocalNxlFileStatus.AvailableOffline:
                                    return bitmap_o;
                                case EnumLocalNxlFileStatus.Uploading:
                                    return bitmap_u;
                                case EnumLocalNxlFileStatus.WaitingUpload:
                                    return bitmap_w;
                                default:
                                    return null;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        switch (status)
                        {
                            case EnumLocalNxlFileStatus.CachedFile:
                                return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_d.ico", UriKind.Relative));
                            case EnumLocalNxlFileStatus.AvailableOffline:
                                return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_o.ico", UriKind.Relative));
                            case EnumLocalNxlFileStatus.Uploading:
                                return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_u.ico", UriKind.Relative));
                            case EnumLocalNxlFileStatus.WaitingUpload:
                                return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_w.ico", UriKind.Relative));
                            default:
                                return null;
                        }
                    }
                }
                    
            }
            switch (status)
            {
                case EnumLocalNxlFileStatus.CachedFile:
                    return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_d.ico", UriKind.Relative));
                case EnumLocalNxlFileStatus.AvailableOffline:
                    return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_o.ico", UriKind.Relative));
                case EnumLocalNxlFileStatus.Uploading:
                    return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_u.ico", UriKind.Relative));
                case EnumLocalNxlFileStatus.WaitingUpload:
                    return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_w.ico", UriKind.Relative));
                default:
                    return null;
            }

        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convert local file status map image to display. --- Extend for supporting folder.
    /// </summary>
    public class LocalFileStatus2ImageConverterEx : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            string name = (string)value[0];
            EnumLocalNxlFileStatus status = (EnumLocalNxlFileStatus)value[1];
            bool isFolder = (bool)value[2];
            if (isFolder)
            {
                return new BitmapImage(new Uri(@"/rmc/resources/icons/Folder.png", UriKind.Relative));
            }

            int lastindex = name.LastIndexOf('.');
            string originFilename = name.Substring(0, lastindex);
            string fileType = "---";
            if (originFilename.LastIndexOf('.') > CommonUtils.NxlFileDatetimeLength)
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
                        BitmapImage bitmap_d = new BitmapImage(new Uri(uritemp_d, UriKind.Relative));

                        string uritemp_o = string.Format(@"/rmc/resources/fileimages/{0}_o.ico", fileType);
                        BitmapImage bitmap_o = new BitmapImage(new Uri(uritemp_o, UriKind.Relative));

                        string uritemp_u = string.Format(@"/rmc/resources/fileimages/{0}_u.ico", fileType);
                        BitmapImage bitmap_u = new BitmapImage(new Uri(uritemp_u, UriKind.Relative));

                        string uritemp_w = string.Format(@"/rmc/resources/fileimages/{0}_w.ico", fileType);
                        BitmapImage bitmap_w = new BitmapImage(new Uri(uritemp_w, UriKind.Relative));

                        string uritemp_s = string.Format(@"/rmc/resources/fileimages/{0}_s.ico", fileType);
                        BitmapImage bitmap_s = new BitmapImage(new Uri(uritemp_s, UriKind.Relative));

                        if (bitmap_d != null && bitmap_o != null && bitmap_u != null && bitmap_w != null)
                        {
                            switch (status)
                            {
                                case EnumLocalNxlFileStatus.CachedFile:
                                    return bitmap_d;
                                case EnumLocalNxlFileStatus.AvailableOffline:
                                    return bitmap_o;
                                case EnumLocalNxlFileStatus.Uploading:
                                    return bitmap_u;
                                case EnumLocalNxlFileStatus.WaitingUpload:
                                    return bitmap_w;
                                default:
                                    return null;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        switch (status)
                        {
                            case EnumLocalNxlFileStatus.CachedFile:
                                return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_d.ico", UriKind.Relative));
                            case EnumLocalNxlFileStatus.AvailableOffline:
                                return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_o.ico", UriKind.Relative));
                            case EnumLocalNxlFileStatus.Uploading:
                                return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_u.ico", UriKind.Relative));
                            case EnumLocalNxlFileStatus.WaitingUpload:
                                return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_w.ico", UriKind.Relative));
                            default:
                                return null;
                        }
                    }
                }

            }
            switch (status)
            {
                case EnumLocalNxlFileStatus.CachedFile:
                    return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_d.ico", UriKind.Relative));
                case EnumLocalNxlFileStatus.AvailableOffline:
                    return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_o.ico", UriKind.Relative));
                case EnumLocalNxlFileStatus.Uploading:
                    return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_u.ico", UriKind.Relative));
                case EnumLocalNxlFileStatus.WaitingUpload:
                    return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_w.ico", UriKind.Relative));
                default:
                    return null;
            }

        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Shared with converter
    /// </summary>
    public class SharedWithConverter : IValueConverter
    {
        private string[] array;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string emails = value as string;
            string ret = "";

            if (string.IsNullOrEmpty(emails))
            {
                return ret;
            }

            if (!emails.Contains(",")) // only one shared email
            {
                return emails;
            }
            else
            {
                array = emails.Split(',');
                if (array.Length == 2)
                {
                    return emails;
                }

                if (array.Length > 2)
                {
                    ret += array[0];
                    ret += ",";
                    ret += array[1];
                    ret += "...";
                    return ret;
                }
            }

            return ret;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Shared with more converter
    /// </summary>
    public class SharedWithMoreConverter : IValueConverter
    {
        private string[] array;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string emails = value as string;

            if (!string.IsNullOrEmpty(emails) && emails.Contains(","))
            {
                array = emails.Split(',');
                if (array.Length > 2)
                {
                    return "+" + (array.Length - 2).ToString();
                }

            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Judge the sharedWith count, return true when count is more than 2, means set "More" control visiable.
    /// </summary>
    public class Count2BoolConverter : IValueConverter
    {
        private string[] array;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string emails = value as string;

            if (!string.IsNullOrEmpty(emails) && emails.Contains(","))
            {
                array = emails.Split(',');
                if (array.Length == 2 && emails.EndsWith("..."))
                {
                    return true;
                }

            }

            return false;
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
            return (int)value > 0 ? true : false;
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
                return CultureStringInfo.NETWORK_CONNECTED;
            } else
            {
                return CultureStringInfo.NETWORK_ERROR;
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
    public class NetworkStatusBool2ImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
            {
                return @"/rmc/resources/icons/Icon_net_connect.png";
            }
            else
            {
                return @"/rmc/resources/icons/Icon_net_error.png";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Using to Converter network status(bool type) to line status(online\offline)
    /// </summary>
    public class NetStatusBool2LineStatus : IValueConverter
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
    /// Convert file size into readable value (now only use KB as the unit.)
    /// </summary>
    public class ReadableFileSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            long size = (long)value;

            //if(size <= 0)
            //{
            //    return "0KB";
            //} else if( 0 < size && size < 1024)
            //{
            //    return "1KB";
            //} else 
            //{
            //long integerPart = size / 1024;
            //long remainder = size % size;

            //if (remainder == 0)
            //    return integerPart + "KB";
            //else
            //    return (integerPart + 1) + "KB";

            //return Math.Round( size / (float)1024) + "KB";

            // }

            return CommonUtils.GetSizeString(size);

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    #region For Upload Button Converter
    /// <summary>
    /// Convert upload button content
    /// </summary>
    public class ButtonUploadContentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool IsStartUpload = (bool)value;

            if (IsStartUpload==true)
            {                
                return CultureStringInfo.MainWindow__Stop_Upload;
            }
            return CultureStringInfo.MainWindow__Start_Upload;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    /// <summary>
    /// Convert upload button Tag
    /// </summary>
    public class ButtonUploadTagConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool IsStartUpload = (bool)value;
            string imageUrl = "";
            if (IsStartUpload == true)
            {
                imageUrl = @"/rmc/resources/icons/Icon_stopUpload.png";
            }
            else
            {
                imageUrl= @"/rmc/resources/icons/Icon_StartUpload.png";
            }
            return new BitmapImage(new Uri((string)imageUrl, UriKind.Relative));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    /// <summary>
    /// Convert upload button  CommandParameter
    /// </summary>
    public class ButtonUploadCommandParameterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool IsStartUpload = (bool)value;

            if (IsStartUpload == true)
            {
                return @"Cmd_StopUpload";
            }
            return @"Cmd_StartUpload";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    #endregion

    #region For Menuitem start/stop upload status
    /// <summary>
    /// Convert Menuitem Start Upload Status
    /// </summary>
    public class MenuitemStartUploadStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool IsStartUpload = (bool)value;

            if (IsStartUpload == true)
            {
                return false;
            }
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    /// <summary>
    /// Convert Menuitem Stop Upload Status
    /// </summary>
    public class MenuitemStopUploadStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool IsStartUpload = (bool)value;

            if (IsStartUpload == true)
            {
                return true;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    #endregion

    #region For Menuitem status need select File
    /// <summary>
    /// Convert Menuitem Status, if current select File is not null, isEnable is true. or else,isEnable is false
    /// </summary>
    public class MenuitemSelectFileStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //LocalNxlFile CurrentSelectedFile = (LocalNxlFile)value;

            //if (CurrentSelectedFile != null && CurrentSelectedFile.LocalNxlFileStatus != EnumLocalNxlFileStatus.Uploading) 
            //{
            //    return true;
            //}
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    #endregion

    public class ItemCountsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int count = (int)value;

            if (count>1)
            {
                return CultureStringInfo.MainWindow_List_items;
            }
            return CultureStringInfo.MainWindow_List_item;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
