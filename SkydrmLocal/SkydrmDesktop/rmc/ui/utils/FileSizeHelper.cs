using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.ui.utils
{
    class FileSizeHelper
    {
        public static string ConvertFileSize(Int64 size)
        {

            if (size <= 0)
            {
                return "0KB";
            }
            else if (0 < size && size < 1024)
            {
                return "1KB";
            }
            else
            {
                return Math.Round(size / (float)1024) + "KB";
            }
        }

        private enum SizeUnitMode
        {
            Byte,

            KiloByte,

            MegaByte,

            GigaByte,
        }

        public static System.String GetSizeString(System.Double parameter)
        {

            System.Double size = 0;
            SizeUnitMode sizeUnitMode;
            size = GetSize(parameter, out sizeUnitMode);
            string result = string.Empty;

            switch (sizeUnitMode)
            {
                case SizeUnitMode.Byte:
                    result = System.String.Format("{0} B", size.ToString("N", System.Globalization.CultureInfo.InvariantCulture));
                    break;
                case SizeUnitMode.KiloByte:
                    result = System.String.Format("{0} KB", size.ToString("N", System.Globalization.CultureInfo.InvariantCulture));
                    break;
                case SizeUnitMode.MegaByte:
                    result = System.String.Format("{0} MB", size.ToString("N", System.Globalization.CultureInfo.InvariantCulture));
                    break;
                case SizeUnitMode.GigaByte:
                    result = System.String.Format("{0} GB", size.ToString("N", System.Globalization.CultureInfo.InvariantCulture));

                    break;
                default:
                    break;
            }
            return result;
        }

        private static System.Double GetSize(System.Double size, out SizeUnitMode sizeUnitMode)
        {

            if (size >= 0 && size < 1024)
            {
                sizeUnitMode = SizeUnitMode.Byte;
                return size;
            }

            System.Double kb = size / 1024;
            if (kb >= 1 && kb < 1024)
            {
                sizeUnitMode = SizeUnitMode.KiloByte;
                return kb;
            }


            System.Double mb = size / (1024 * 1024);
            if (mb >= 1 && mb < 1024)
            {
                sizeUnitMode = SizeUnitMode.MegaByte;
                return mb;
            }

            System.Double gb = size / (1024 * 1024 * 1024);
            if (gb >= 1)
            {
                sizeUnitMode = SizeUnitMode.GigaByte;
                return gb;
            }

            sizeUnitMode = SizeUnitMode.Byte;
            return size;
        }

    }
}
