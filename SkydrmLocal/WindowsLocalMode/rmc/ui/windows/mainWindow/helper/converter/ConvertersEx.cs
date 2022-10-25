using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.ui.utils;
using SkydrmLocal.rmc.ui.windows.mainWindow.model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SkydrmLocal.rmc.ui.windows.mainWindow.helper.converter
{

        /// <summary>
        /// Convert local file status to bool to select large context menu or samll context menu.
        /// </summary>
        public class FileStatusEnum2Bool : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                 try
                 {
                     EnumNxlFileStatus status = (EnumNxlFileStatus)value;
                     switch (status)
                     {
                         case EnumNxlFileStatus.AvailableOffline:
                             return true;
                         default: // other file status, will popup small context menu.
                             break;
                     }
                     return false;
                 }
                 catch (Exception)
                 {
                     return false;
                 }

            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

    /// <summary>
    /// For search combox visibility.
    /// </summary>
    public class SearchComboxVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                EnumCurrentWorkingArea status = (EnumCurrentWorkingArea)value;
                if (status== EnumCurrentWorkingArea.PROJECT)
                {
                    return Visibility.Collapsed;
                }
                return Visibility.Visible;
            }
            catch (Exception)
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
    /// Convert local file status map image to display.
    /// </summary>
    public class LocalFileStatus2ImageConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                string name = (string)value[0];
                EnumNxlFileStatus status = (EnumNxlFileStatus)value[1];
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
                                    case EnumNxlFileStatus.CachedFile:
                                        return bitmap_d;
                                    case EnumNxlFileStatus.AvailableOffline:
                                        return bitmap_o;
                                    case EnumNxlFileStatus.Uploading:
                                        return bitmap_u;
                                    case EnumNxlFileStatus.WaitingUpload:
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
                                case EnumNxlFileStatus.CachedFile:
                                    return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_d.ico", UriKind.Relative));
                                case EnumNxlFileStatus.AvailableOffline:
                                    return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_o.ico", UriKind.Relative));
                                case EnumNxlFileStatus.Uploading:
                                    return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_u.ico", UriKind.Relative));
                                case EnumNxlFileStatus.WaitingUpload:
                                    return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_w.ico", UriKind.Relative));
                                default:
                                    return null;
                            }
                        }
                    }

                }
                switch (status)
                {
                    case EnumNxlFileStatus.CachedFile:
                        return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_d.ico", UriKind.Relative));
                    case EnumNxlFileStatus.AvailableOffline:
                        return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_o.ico", UriKind.Relative));
                    case EnumNxlFileStatus.Uploading:
                        return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_u.ico", UriKind.Relative));
                    case EnumNxlFileStatus.WaitingUpload:
                        return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_w.ico", UriKind.Relative));
                    default:
                        return null;
                }
            }
            catch (Exception)
            {
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

            try
            {
                string name = (string)value[0];

                if (value[1].GetType() != typeof(EnumNxlFileStatus))
                {
                    return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_g.ico", UriKind.Relative));
                }
                EnumNxlFileStatus status = (EnumNxlFileStatus)value[1];
                bool isFolder = (bool)value[2];
                bool isOffline = (bool)value[3];

                if (isFolder)
                {
                    return new BitmapImage(new Uri(@"/rmc/resources/icons/folder-test-00.ico", UriKind.Relative));
                }

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

                            if (bitmap_d != null && bitmap_o != null && bitmap_u != null && bitmap_w != null && bitmap_g != null)
                            {
                                switch (status)
                                {
                                    // Now "Leave a copy" can look as the kind of "Offline", so use the same icon.
                                    case EnumNxlFileStatus.CachedFile:
                                     //return bitmap_d;
                                    case EnumNxlFileStatus.AvailableOffline:
                                        return bitmap_o;
                                    case EnumNxlFileStatus.Uploading:
                                        return bitmap_u;
                                    case EnumNxlFileStatus.WaitingUpload:
                                        return bitmap_w;
                                    case EnumNxlFileStatus.Online:
                                    case EnumNxlFileStatus.Downloading:
                                        return bitmap_g;
                                    case EnumNxlFileStatus.UploadFailed:
                                        // now file support upload when status is waitingUpload or offline(AvailableOffline,CachedFile) file edited
                                        // so these two status file maybe upload failed
                                        if (isOffline)
                                        {
                                            return bitmap_o;
                                        }
                                        return bitmap_w;
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
                                // return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_d.ico", UriKind.Relative));
                                case EnumNxlFileStatus.AvailableOffline:
                                    return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_o.ico", UriKind.Relative));
                                case EnumNxlFileStatus.Uploading:
                                    return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_u.ico", UriKind.Relative));
                                case EnumNxlFileStatus.WaitingUpload:
                                    return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_w.ico", UriKind.Relative));
                                case EnumNxlFileStatus.Downloading:
                                case EnumNxlFileStatus.Online:
                                    return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_g.ico", UriKind.Relative));
                                case EnumNxlFileStatus.UploadFailed:
                                    if (isOffline)
                                    {
                                        return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_o.ico", UriKind.Relative));
                                    }
                                    return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_w.ico", UriKind.Relative));
                                default:
                                    return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_g.ico", UriKind.Relative));
                            }
                        }
                    }

                }
                switch (status)
                {
                    case EnumNxlFileStatus.CachedFile:
                    //return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_d.ico", UriKind.Relative));
                    case EnumNxlFileStatus.AvailableOffline:
                        return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_o.ico", UriKind.Relative));
                    case EnumNxlFileStatus.Uploading:
                        return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_u.ico", UriKind.Relative));
                    case EnumNxlFileStatus.WaitingUpload:
                        return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_w.ico", UriKind.Relative));
                    case EnumNxlFileStatus.Downloading:
                    case EnumNxlFileStatus.Online:
                        return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_g.ico", UriKind.Relative));
                    case EnumNxlFileStatus.UploadFailed:
                        if (isOffline)
                        {
                            return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_o.ico", UriKind.Relative));
                        }
                        return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_w.ico", UriKind.Relative));
                    default:
                        return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_g.ico", UriKind.Relative));
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());

                return new BitmapImage(new Uri(@"/rmc/resources/fileimages/---_g.ico", UriKind.Relative));
                // throw new InvalidOperationException();
            }

        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// For file downloading, upload failed, edited attach extra image and text visibility
    /// </summary>
    public class SpecialFileStatus2SpVisibileConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            //< Binding Path = "FileStatus" />
            //< Binding Path = "IsMarkedOffline" />
            //< Binding Path = "IsEdit" />
            try
            {
                if (value[0].GetType() != typeof(EnumNxlFileStatus))
                {
                    return Visibility.Collapsed;
                }
                EnumNxlFileStatus nxlFileStatus = (EnumNxlFileStatus)value[0];
                bool isOffline = (bool)value[1];
                bool isEdit = (bool)value[2];

                if (nxlFileStatus == EnumNxlFileStatus.Downloading)
                {
                    return Visibility.Visible;
                }
                else if (nxlFileStatus == EnumNxlFileStatus.UploadFailed)
                {
                    return Visibility.Visible;
                }
                else if (isOffline && isEdit)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return Visibility.Collapsed;
            }
        }
        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    /// <summary>
    /// For file downloading, upload failed, edited attach extra image
    /// </summary>
    public class SpecialFileStatus2ImageConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            //< Binding Path = "FileStatus" />
            //< Binding Path = "IsMarkedOffline" />
            //< Binding Path = "IsEdit" />
            try
            {
                if (value[0].GetType() != typeof(EnumNxlFileStatus))
                {
                    return null;
                }
                EnumNxlFileStatus nxlFileStatus = (EnumNxlFileStatus)value[0];
                bool isOffline = (bool)value[1];
                bool isEdit = (bool)value[2];

                if (nxlFileStatus == EnumNxlFileStatus.Downloading)
                {
                    return new BitmapImage(new Uri(@"/rmc/resources/icons/icon_down_arrow.png", UriKind.Relative));
                }
                else if (nxlFileStatus == EnumNxlFileStatus.UploadFailed)
                {
                    return new BitmapImage(new Uri(@"/rmc/resources/icons/uploading_sign.png", UriKind.Relative));
                }
                else if (isOffline && isEdit)
                {
                    return new BitmapImage(new Uri(@"/rmc/resources/icons/icon_yellow_rectangle.png", UriKind.Relative));
                }
                else
                {
                    return null;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return null;
            }
        }
        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    /// <summary>
    /// For file downloading, upload failed, edited attach extra text
    /// </summary>
    public class SpecialFileStatus2TextConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            //< Binding Path = "FileStatus" />
            //< Binding Path = "IsMarkedOffline" />
            //< Binding Path = "IsEdit" />
            try
            {
                if (value[0].GetType() != typeof(EnumNxlFileStatus))
                {
                    return "";
                }
                EnumNxlFileStatus nxlFileStatus = (EnumNxlFileStatus)value[0];
                bool isOffline = (bool)value[1];
                bool isEdit = (bool)value[2];

                if (nxlFileStatus == EnumNxlFileStatus.Downloading)
                {
                    return CultureStringInfo.MainWin_List_Updating;
                }
                else if (nxlFileStatus == EnumNxlFileStatus.UploadFailed)
                {
                    return CultureStringInfo.MainWin_List_UploadFailed;
                }
                else if (isOffline && isEdit)
                {
                    return CultureStringInfo.MainWin_List_Edited_In_Local;
                }
                else
                {
                    return "";
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return "";
            }
        }
        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class FilterListSourcePathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                Visibility visibility = (Visibility)value;
                if (visibility==Visibility.Visible)
                {
                    return Visibility.Collapsed;
                }
                return Visibility.Visible;
            }
            catch (Exception)
            {
                return Visibility.Visible;
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class UpdatingIconVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                EnumNxlFileStatus status = (EnumNxlFileStatus)value;
                switch (status)
                {
                    case EnumNxlFileStatus.CachedFile:
                        return Visibility.Collapsed;
                    case EnumNxlFileStatus.AvailableOffline:
                        return Visibility.Collapsed;
                    case EnumNxlFileStatus.Uploading:
                        return Visibility.Collapsed;
                    case EnumNxlFileStatus.WaitingUpload:
                        return Visibility.Collapsed;
                    case EnumNxlFileStatus.Online:
                        return Visibility.Collapsed;
                    case EnumNxlFileStatus.Downloading:
                        return Visibility.Visible;
                    default:
                        return Visibility.Collapsed;
                }
            }
            catch (Exception)
            {
                return Visibility.Collapsed;
            }
           
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EditedAndModifiedStausConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                EnumNxlFileStatus status = (EnumNxlFileStatus)value;
                switch (status)
                {
                    //case EnumNxlFileStatus.AvailableOffline_Edited:
                    //case EnumNxlFileStatus.CachedFile_Edited: // Need change
                    //    return Visibility.Visible;
                    default:
                        return Visibility.Collapsed;
                }
            }
            catch (Exception)
            {
                return Visibility.Collapsed;
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EditedAndModifiedTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                EnumNxlFileStatus status = (EnumNxlFileStatus)value;
                switch (status)
                {
                    //case EnumNxlFileStatus.AvailableOffline_Edited:
                    //case EnumNxlFileStatus.CachedFile_Edited:
                    //    return "Edited in Local";
                    default:
                        return null;
                }
            }
            catch (Exception)
            {
                return null;
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class FileLocationConentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                string location = value?.ToString();
                //if(location == "Local")
                //{
                //    return "SkyDRM Folder";
                //} else
                //{
                //    return location;
                //}

                return location;
            }
            catch (Exception)
            {
                //throw;
                return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ListViewVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                EnumCurrentWorkingArea CurrentWorkingArea = (EnumCurrentWorkingArea)value;
                switch (CurrentWorkingArea)
                {
                    case EnumCurrentWorkingArea.FILTERS_OFFLINE:
                    case EnumCurrentWorkingArea.FILTERS_OUTBOX:
                        return Visibility.Collapsed;
                    default:
                        return Visibility.Visible;
                }
            }
            catch (Exception)
            {
                return Visibility.Collapsed;
            }
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class FilterListViewVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                EnumCurrentWorkingArea CurrentWorkingArea = (EnumCurrentWorkingArea)value;
                switch (CurrentWorkingArea)
                {
                    case EnumCurrentWorkingArea.FILTERS_OFFLINE:
                    case EnumCurrentWorkingArea.FILTERS_OUTBOX:
                        return Visibility.Visible;
                    default:
                        return Visibility.Collapsed;
                }
            }
            catch (Exception)
            {
                return Visibility.Collapsed;
            }
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class IsProjectShareBtnEnableConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                EnumCurrentWorkingArea CurrentWorkingArea = (EnumCurrentWorkingArea)value;
                switch (CurrentWorkingArea)
                {
                    case EnumCurrentWorkingArea.PROJECT:
                    case EnumCurrentWorkingArea.PROJECT_ROOT:
                    case EnumCurrentWorkingArea.PROJECT_FOLDER:
                        return false;
                    default:
                        return true;
                }
            }
            catch (Exception)
            {
                return true;
            }
           
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Display treeview converter
    /// </summary>
    public class DisplayVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                bool Isvisibility = (bool)value;
                SkydrmLocalApp.Singleton.Log.Info("Display treeview, Isvisibility value: " + Isvisibility.ToString());

                if (Isvisibility)
                {
                    return Visibility.Visible;
                }
                return Visibility.Collapsed;
            }
            catch (Exception)
            {
                SkydrmLocalApp.Singleton.Log.Info("Convert failed in DisplayVisibilityConverter");
                return Visibility.Collapsed;
            }
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DisplayGridWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                bool Isvisibility = (bool)value;
                SkydrmLocalApp.Singleton.Log.Info(" Display grid width, Isvisibility value: " + Isvisibility.ToString());

                if (Isvisibility)
                {
                    //for ColumnDefinition width
                    return @"170";
                }
                return @"auto";
            }
            catch (Exception)
            {
                SkydrmLocalApp.Singleton.Log.Info("Convert failed in DisplayGridWidthConverter");
                return @"auto";
            }
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    //for Loading Page,user first login 
    public class DisplayLoadingPageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                bool isLoading = (bool)value;
                SkydrmLocalApp.Singleton.Log.Info("Display loading page, isLoading value: " + isLoading.ToString());

                if (isLoading)
                {
                    return Visibility.Visible;
                }
                return Visibility.Collapsed;
            }
            catch (Exception)
            {
                SkydrmLocalApp.Singleton.Log.Info("Convert failed in DisplayLoadingPageConverter");
                return Visibility.Collapsed;
            }
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    //for display Project  LixBox
    public class DisplayProjectLixBoxConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                EnumCurrentWorkingArea CurrentWorkingArea = (EnumCurrentWorkingArea)value;
                if (CurrentWorkingArea == EnumCurrentWorkingArea.PROJECT)
                {
                    return Visibility.Visible;
                }

                return Visibility.Collapsed;
            }
            catch (Exception)
            {
                return Visibility.Collapsed;
            }
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    //for display Project group name in LixBox
    public class ProjectLixBoxGroupNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                bool IsCreateByMe = (bool)value;
                if (IsCreateByMe)
                {
                    return CultureStringInfo.MainWin__ProjectListView_GroupByMe;
                }
                return CultureStringInfo.MainWin__ProjectListView_GroupByOther;
            }
            catch (Exception)
            {
                return null;
            }
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    //for display Project group name in LixBox
    public class ProjectLixBoxGroupName2Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                bool IsCreateByMe = (bool)value;
                if (IsCreateByMe)
                {
                    return CultureStringInfo.MainWin__ProjectListView_GroupByMe2;
                }
                return CultureStringInfo.MainWin__ProjectListView_GroupByOther2;
            }
            catch (Exception)
            {
                return null;
            }
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    //for display Project group name in LixBox
    public class ProjectPageListNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                bool IsCreateByMe = (bool)value;
                if (IsCreateByMe)
                {
                    return @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/projectByMe.png";
                }
                return @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/projectByOthers.png";
            }
            catch (Exception)
            {
                return null;
            }
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    //for display ContentLayout_NotData is or not
    public class DisplayContentNotDataConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                EnumCurrentWorkingArea CurrentWorkingArea = (EnumCurrentWorkingArea)value[0];
                int nxlFileListCount = (int)value[1];
                int copyFileListCount = (int)value[2];
                if (CurrentWorkingArea == EnumCurrentWorkingArea.FILTERS_OUTBOX)
                {
                    if (nxlFileListCount > 0 || copyFileListCount > 0)
                    {
                        return Visibility.Collapsed;
                    }
                    return Visibility.Visible;
                }

                return Visibility.Collapsed;
            }
            catch (Exception)
            {
                return Visibility.Collapsed;
            }
            
          
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    //for ListView Column date modified Header content 
    public class IsDisplayDateColumnContentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                EnumCurrentWorkingArea CurrentWorkingArea = (EnumCurrentWorkingArea)value;
                switch (CurrentWorkingArea)
                {
                    case EnumCurrentWorkingArea.SHARED_WITH_ME:
                        return CultureStringInfo.MainWin__FileListView_Shared_date;
                    default:
                        return CultureStringInfo.MainWin__FileListView_Date_modified;
                }
            }
            catch (Exception)
            {
                return CultureStringInfo.MainWin__FileListView_Date_modified;
            }
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    //for ListView Column Share with Header content 
    public class IsDisplayShareColumnContentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                EnumCurrentWorkingArea CurrentWorkingArea = (EnumCurrentWorkingArea)value;
                switch (CurrentWorkingArea)
                {
                    case EnumCurrentWorkingArea.PROJECT:
                    case EnumCurrentWorkingArea.PROJECT_ROOT:
                    case EnumCurrentWorkingArea.PROJECT_FOLDER:
                    case EnumCurrentWorkingArea.FILTERS_OFFLINE:
                        return @"";
                    case EnumCurrentWorkingArea.SHARED_WITH_ME:
                        return CultureStringInfo.MainWin__FileListView_Shared_by;
                    default:
                        return CultureStringInfo.MainWin__FileListView_Shared_with;
                }
            }
            catch (Exception)
            {
                return CultureStringInfo.MainWin__FileListView_Shared_with;
            }
           
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    //for display promptText visibility
    public class PromptTextVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                EnumCurrentWorkingArea CurrentWorkingArea = (EnumCurrentWorkingArea)value[0];
                bool isLoading = (bool)value[1];
                int nxlFileListCount = (int)value[2];
                int copyFileListCount = (int)value[3];

                switch (CurrentWorkingArea)
                {
                    case EnumCurrentWorkingArea.MYVAULT:
                    case EnumCurrentWorkingArea.SHARED_WITH_ME:
                        if (isLoading)
                        {
                            return Visibility.Collapsed;//display loading...   //Now isloading is true will not display PromptText, will display  Loading_Page.
                        }
                        if (nxlFileListCount > 0 || copyFileListCount > 0)
                        {
                            return Visibility.Collapsed;
                        }
                        return Visibility.Visible;//display The folder is empty
                    case EnumCurrentWorkingArea.PROJECT:
                        if (isLoading)
                        {
                            return Visibility.Visible;//display loading...
                        }
                        return Visibility.Visible;//display Select a subfolder for your project
                    case EnumCurrentWorkingArea.PROJECT_ROOT:
                    case EnumCurrentWorkingArea.PROJECT_FOLDER:
                    case EnumCurrentWorkingArea.FILTERS_OFFLINE:
                        if (nxlFileListCount > 0 || copyFileListCount > 0)
                        {
                            return Visibility.Collapsed;
                        }
                        return Visibility.Visible;//display The folder is empty
                    case EnumCurrentWorkingArea.FILTERS_OUTBOX:
                        return Visibility.Collapsed;

                }

                return Visibility.Collapsed;
            }
            catch (Exception)
            {
                return Visibility.Collapsed;
            }
           

        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    //for display promptText content
    public class PromptTextContentConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                EnumCurrentWorkingArea CurrentWorkingArea = (EnumCurrentWorkingArea)value[0];
                bool isLoading = (bool)value[1];
                int nxlFileListCount = (int)value[2];
                int copyFileListCount = (int)value[3];

                switch (CurrentWorkingArea)
                {
                    case EnumCurrentWorkingArea.MYVAULT:
                    case EnumCurrentWorkingArea.SHARED_WITH_ME:
                        if (isLoading)
                        {
                            return CultureStringInfo.MainWin_PromptText_Loading;//display loading...
                        }
                        else
                        {
                            if (nxlFileListCount > 0 || copyFileListCount > 0)
                            {
                                return @"";
                            }
                            return CultureStringInfo.MainWin_PromptText_Empty;//display The folder is empty
                        }
                    case EnumCurrentWorkingArea.PROJECT:
                        if (isLoading)
                        {
                            return CultureStringInfo.MainWin_PromptText_Loading;//display loading...
                        }
                        if (true)
                        {

                        }

                        return CultureStringInfo.MainWin_PromptText_Select;//display Select a subfolder for your project
                    case EnumCurrentWorkingArea.PROJECT_ROOT:
                    case EnumCurrentWorkingArea.PROJECT_FOLDER:
                    case EnumCurrentWorkingArea.FILTERS_OFFLINE:
                        if (nxlFileListCount > 0 || copyFileListCount > 0)
                        {
                            return @"";
                        }
                        return CultureStringInfo.MainWin_PromptText_Empty;//display The folder is empty
                    case EnumCurrentWorkingArea.FILTERS_OUTBOX:
                        return @"";

                }

                return @"";
            }
            catch (Exception)
            {
                return @"";
            }           

        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    //for display Search promptText visibility
    public class SearchPromptTextVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                bool isSearch = (bool)value[0];
                int nxlListCount = (int)value[1];

                if (isSearch)
                {
                    if (nxlListCount < 1)
                    {
                        return Visibility.Visible;
                    }
                }
                return Visibility.Collapsed;
            }
            catch (Exception)
            {
                return Visibility.Collapsed;
            }           

        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    //for display Empty Folder visibility
    public class EmptyFolderVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                EnumCurrentWorkingArea CurrentWorkingArea = (EnumCurrentWorkingArea)value[0];
                bool isLoading = (bool)value[1];
                if (isLoading)
                {
                    return Visibility.Collapsed;
                }
                int nxlFileListCount = (int)value[2];
                int copyFileListCount = (int)value[3];
                int projectCount = (int)value[4];

                switch (CurrentWorkingArea)
                {
                    case EnumCurrentWorkingArea.MYVAULT:
                    case EnumCurrentWorkingArea.SHARED_WITH_ME:
                        if (nxlFileListCount > 0 || copyFileListCount > 0)
                        {
                            return Visibility.Collapsed;
                        }
                        return Visibility.Visible;//display The folder is empty
                    case EnumCurrentWorkingArea.PROJECT:
                        if (projectCount > 0)
                        {
                            return Visibility.Collapsed;//display SelectProject UI
                        }
                        return Visibility.Visible;//display The folder is empty
                    case EnumCurrentWorkingArea.PROJECT_ROOT:
                    case EnumCurrentWorkingArea.PROJECT_FOLDER:
                    case EnumCurrentWorkingArea.FILTERS_OFFLINE:
                        if (nxlFileListCount > 0 || copyFileListCount > 0)
                        {
                            return Visibility.Collapsed;
                        }
                        return Visibility.Visible;//display The folder is empty
                    case EnumCurrentWorkingArea.FILTERS_OUTBOX:
                        return Visibility.Collapsed;

                }

                return Visibility.Collapsed;
            }
            catch (Exception)
            {
                return Visibility.Collapsed;
            }
           

        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // For empty folder visibility when is data loading
    public class EmptyImageVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                bool isDataLoading = (bool)value;
                if (isDataLoading)
                {
                    return Visibility.Collapsed;
                }

                return Visibility.Visible;
            }
            catch (Exception)
            {
                return Visibility.Visible;
            }
            
        }


        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // For empty folder text when is data loading
    public class EmptyFolderTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                bool isDataLoading = (bool)value;
                if (isDataLoading)
                {
                    return CultureStringInfo.MainWin_PromptText_Loading;
                }

                return CultureStringInfo.MainWin_PromptText_Empty;
            }
            catch (Exception)
            {
                return null;
            }
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
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
            try
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
            catch (Exception)
            {
                return null;
            }
           
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
            try
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
            catch (Exception)
            {
                return "";
            }
            
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
            try
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
            catch (Exception)
            {
                return false;
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
            return (int)value > 0 ? true : false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    /// <summary>
    /// Using to Converter network status(bool type) to Visibility prompt Title
    /// </summary>
    public class NetworkStatusBool2VisibilityInfo : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if ((bool)value)
                {
                    return Visibility.Collapsed;
                }
                else
                {
                    return Visibility.Visible;
                }
            }
            catch (Exception)
            {
                return Visibility.Collapsed;
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
            try
            {
                if ((bool)value)
                {
                    return CultureStringInfo.NETWORK_CONNECTED;
                }
                else
                {
                    return CultureStringInfo.NETWORK_ERROR;
                }
            }
            catch (Exception)
            {
                return "";
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
            try
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
            catch (Exception)
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
            try
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
            catch (Exception)
            {
                return "";
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
            try
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
            catch (Exception)
            {
                return new SolidColorBrush(Color.FromRgb(39, 174, 96));
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
            try
            {
                long size = (long)value;
                //tmp for bug fix, if size==0 ,we take it as a folder 
                if (size == 0)
                {
                    return "";
                }

                return CommonUtils.GetSizeString(size);
            }
            catch (Exception)
            {
                return "";
            }
            
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
            try
            {
                bool IsStartUpload = (bool)value;

                if (IsStartUpload == true)
                {
                    return CultureStringInfo.MainWindow__Stop_Upload;
                }
                return CultureStringInfo.MainWindow__Start_Upload;
            }
            catch (Exception)
            {
                return "";
            }
            
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
            try
            {
                bool IsStartUpload = (bool)value;
                string imageUrl = "";
                if (IsStartUpload == true)
                {
                    imageUrl = @"/rmc/resources/icons/Icon_stopUpload.png";
                }
                else
                {
                    imageUrl = @"/rmc/resources/icons/Icon_StartUpload.png";
                }
                return new BitmapImage(new Uri((string)imageUrl, UriKind.Relative));
            }
            catch (Exception)
            {
                return null;
            }
           
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
            try
            {
                bool IsStartUpload = (bool)value;

                if (IsStartUpload == true)
                {
                    return @"Cmd_StopUpload";
                }
                return @"Cmd_StartUpload";
            }
            catch (Exception)
            {
                return "";
            }
            
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
            try
            {
                bool IsStartUpload = (bool)value;

                if (IsStartUpload == true)
                {
                    return false;
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
            
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
            try
            {
                bool IsStartUpload = (bool)value;

                if (IsStartUpload == true)
                {
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
            
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
            try
            {
                INxlFile CurrentSelectedFile = (INxlFile)value;

                if (CurrentSelectedFile != null && CurrentSelectedFile.FileStatus != EnumNxlFileStatus.Uploading &&
                     CurrentSelectedFile.FileStatus != EnumNxlFileStatus.Online)
                {
                    return true;
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
           
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public class MenuitemFileStatusNetworkMultiBindConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                INxlFile CurrentSelectedFile = (INxlFile)values[0];
                bool isNetworkAvailabe = (bool)values[1];

                if (CurrentSelectedFile == null)
                {
                    return false;
                }

                if (CurrentSelectedFile.Location == EnumFileLocation.Local
                    && CurrentSelectedFile.FileStatus != EnumNxlFileStatus.Uploading)
                {
                    return true;
                }

                if (CurrentSelectedFile.Location == EnumFileLocation.Online
                    && CurrentSelectedFile.FileStatus != EnumNxlFileStatus.Downloading)
                {
                    return isNetworkAvailabe;
                }

                    return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class MenuitemRemoveFileStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                INxlFile CurrentSelectedFile = (INxlFile)value;

                // For offline file, we only execute "unmark" instead of "remove".
                if (CurrentSelectedFile != null && CurrentSelectedFile.FileStatus != EnumNxlFileStatus.Uploading &&
                     CurrentSelectedFile.FileStatus != EnumNxlFileStatus.Online && CurrentSelectedFile.FileStatus != EnumNxlFileStatus.AvailableOffline)
                {
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
            
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
            try
            {
                int count = (int)value;

                if (count > 1)
                {
                    return CultureStringInfo.MainWindow_List_items;
                }
                return CultureStringInfo.MainWindow_List_item;
            }
            catch (Exception)
            {
                return "";
            }
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TimestampToDateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                long date = long.Parse(value.ToString());

                return CommonUtils.TimestampToDateTime2(date);
            }
            catch (Exception)
            {
                return "";
            }
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

