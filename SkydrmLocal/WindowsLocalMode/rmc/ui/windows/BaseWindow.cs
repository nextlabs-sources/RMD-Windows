
using SkydrmLocal.rmc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SkydrmLocal
{
    public partial class BaseWindow: Window
    {
        protected Image Logo;
        // For sub window, can hide logo and set title.
        protected TextBlock WinTitle;

        // set style
        public BaseWindow()
        {
            InitStyle();

            this.Loaded += delegate
            {
                InitEvent();
            };

        }

        private void InitStyle()
        {
            SkydrmLocalApp.Current.Resources.MergedDictionaries.Add(Application.LoadComponent(new Uri(string.Format("/Themes/BaseWindowStyle.xaml"), UriKind.Relative)) as ResourceDictionary);
            this.Style = (Style)SkydrmLocalApp.Current.Resources["BaseWindowStyle"];
        }

        private void InitEvent()
        {
            ControlTemplate baseWinTemplate = (ControlTemplate)SkydrmLocalApp.Current.Resources["BaseWindowControlTemplate"];

            // Close button
            Button closeBtn = (Button)baseWinTemplate.FindName("btnClose", this);
            closeBtn.Click += Click_close;


            // Border title
            Border border = (Border)baseWinTemplate.FindName("borderTitle", this);
            border.MouseMove += delegate (object sender, MouseEventArgs e)
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    this.DragMove();
                }
            };

            Logo = (Image)baseWinTemplate.FindName("logo", this);
            WinTitle = (TextBlock)baseWinTemplate.FindName("title", this);

        }

        protected virtual void Click_close(object sender, RoutedEventArgs args)
        {
            this.Close();
        }

    }
}
