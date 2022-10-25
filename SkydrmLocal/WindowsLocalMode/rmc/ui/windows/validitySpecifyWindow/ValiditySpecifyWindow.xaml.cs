using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using SkydrmLocal.Pages;
using SkydrmLocal.rmc.ui.components.ValiditySpecify.model;
using SkydrmLocal.rmc.ui.utils;

namespace SkydrmLocal.rmc.ui.windows
{
    /// <summary>
    /// Interaction logic for ValiditySpecifyWindow.xaml
    /// </summary>
    public partial class ValiditySpecifyWindow : Window
    {

        public delegate void ValidationUpdatedHandler(object sender, NewValidationEventArgs e);

        public event ValidationUpdatedHandler ValidationUpdated;

        public ValiditySpecifyWindow(IExpiry expiry, string expireDatevalue)
        {
            InitializeComponent();
            // window title style
            //this.Loaded += delegate
            //{
            //    Logo.Visibility = Visibility.Collapsed;
            //    WinTitle.Visibility = Visibility.Visible;
            //    WinTitle.Text = "Specify Rights Expiry Date";
            //};

            this.ValidityComponent.doInitial(expiry, expireDatevalue);

        }

        private void Button_Cancel(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_Select(object sender, RoutedEventArgs e)
        {
            IExpiry expiry = null;
            string validityContent = CultureStringInfo.ValidityWin_Never_Description2;
            this.ValidityComponent.GetExpireValue(out expiry,out validityContent);
            ValidationUpdated?.Invoke(this, new NewValidationEventArgs(expiry, validityContent));
            this.Close();
        }
       
    }
    public class NewValidationEventArgs : EventArgs
    {
        private IExpiry expiry;
        private string validityContent;

        public NewValidationEventArgs(IExpiry expiry, string validityContent)
        {
            this.expiry = expiry;
            this.validityContent = validityContent;
        }

        public IExpiry Expiry
        {
            get { return expiry; }
            set { expiry = value; }
        }
        public string ValidityContent
        {
            get { return validityContent; }
            set { validityContent = value; }
        }
    }
}
