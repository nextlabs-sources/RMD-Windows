using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SkydrmLocal.rmc.ui.windows
{
    /// <summary>
    /// Interaction logic for AboutSkyDrmWindow.xaml
    /// </summary>
    public partial class AboutSkyDrmWindow : Window
    {
        private WindowConfigs windowConfigs = new WindowConfigs();
        public AboutSkyDrmWindow()
        {
            InitializeComponent();

            this.DataContext = windowConfigs;


        }
        
        public class WindowConfigs : INotifyPropertyChanged
        {
            public string Version
            {
                get { return "Version 10 (Build " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + ")"; }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape || e.Key == Key.Enter)
            {
                this.Close();
            }
        }
    }
}
