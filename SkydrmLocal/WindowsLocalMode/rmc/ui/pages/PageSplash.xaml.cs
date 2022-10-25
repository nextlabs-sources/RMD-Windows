using SkydrmLocal.rmc.ui.windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SkydrmLocal
{
    /// <summary>
    /// Interaction logic for PageSplash.xaml
    /// </summary>
    public partial class PageSplash : Page
    {
        private Window parentWindow;

        public Window ParentWindow
        {
            get { return parentWindow; }
            set { parentWindow = value; }
        }

        public PageSplash()
        {
            InitializeComponent();
        }

        private void Sign_Up(object sender, RoutedEventArgs e)
        {         
            if (parentWindow != null)
            {
                var app = Application.Current as SkydrmLocal.rmc.SkydrmLocalApp;
                app.Log.Info("user sign_up");
                app.Mediator.OnShowSignUp(parentWindow);
            }
        }

        private void Register(object sender, RoutedEventArgs e)
        {          
            if (parentWindow != null)
            {
                var app = Application.Current as SkydrmLocal.rmc.SkydrmLocalApp;
                app.Log.Info("user Log in");
                app.Mediator.OnShowLogin(parentWindow);
            }
        }
    }
}
