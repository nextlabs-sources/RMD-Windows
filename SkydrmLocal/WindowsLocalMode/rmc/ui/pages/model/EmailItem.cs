using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SkydrmLocal.rmc.ui.pages.model
{
    class EmailItem
    {
        private string emails;
        private EmailStatus emailStatus;

        public EmailItem(string emails, EmailStatus emailStatus)
        {
            this.emails = emails;
            this.emailStatus = emailStatus;
        }

        public string Emails
        {
           get { return emails; }
           set { emails = value; }
        }
        public EmailStatus EmailStatus
        {
            get { return emailStatus; }
            set { emailStatus = value; } 
        }
    }

    public enum EmailStatus
    {
        DIRTY,NORMAL
    }

    public class TextForegroundStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            EmailStatus status = (EmailStatus)value;
            switch(status)
            {
                case EmailStatus.DIRTY:
                    return @"White";
                case EmailStatus.NORMAL:
                    return @"Black";
                default:
                    return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BackgroundStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            EmailStatus status = (EmailStatus)value;
            switch(status)
            {
                case EmailStatus.NORMAL:
                    return @"#E5E5E5";
                case EmailStatus.DIRTY:
                    return @"RED";
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
