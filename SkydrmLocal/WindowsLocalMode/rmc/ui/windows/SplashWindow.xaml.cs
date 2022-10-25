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
using SkydrmLocal.rmc;
using SkydrmLocal.rmc.ui.components;
using SkydrmLocal.rmc.ui.windows;

namespace SkydrmLocal
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class SplashWindow : Window
    {
        public SplashWindow()
        {
            SkydrmLocal.rmc.SkydrmLocalApp app = Application.Current as SkydrmLocal.rmc.SkydrmLocalApp;

            app.Log.Info("Init SplashWindow");

            InitializeComponent();
            PageSplash page = new PageSplash()
            {
                ParentWindow = this
            };
            this.main_frame.Content = page;


            this.Loaded += delegate
            {
                // register trayIcon click popup window.
                ((SkydrmLocalApp)SkydrmLocalApp.Current).TrayIconMger.PopupTargetWin = this;
            };
        }
    }
}
