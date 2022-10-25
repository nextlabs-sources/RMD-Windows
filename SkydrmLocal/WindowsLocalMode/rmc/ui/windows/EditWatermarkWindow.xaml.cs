using System;
using System.Collections.Generic;
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
using SkydrmLocal.rmc.ui.components;

namespace SkydrmLocal.rmc.ui.windows
{
    /// <summary>
    /// Interaction logic for EditWatermarkWindow.xaml
    /// </summary>
    public partial class EditWatermarkWindow : Window
    {
        //private EditWatermarkComponent edit;

        public delegate void WatermarkUpdateHandler(object sender, WatermarkArgs eventArgs);

        public event WatermarkUpdateHandler WatermarkHandler;

        public EditWatermarkWindow()
        {
            
            InitializeComponent();
            string initValue = "$(User)$(Date)$(Break)$(Time)";
            edit.doInit(initValue);
            // window title style
            //this.Loaded += delegate
            //{
            //    Logo.Visibility = Visibility.Collapsed;
            //    WinTitle.Visibility = Visibility.Visible;
            //    WinTitle.Text = "Watermark";
            //};

        }


        public EditWatermarkWindow(string initValue)
        {  
            
           InitializeComponent();
           edit.doInit(initValue);

            // window title style
            //this.Loaded += delegate
            //{
            //    Logo.Visibility = Visibility.Collapsed;
            //    WinTitle.Visibility = Visibility.Visible;
            //    WinTitle.Text = "Watermark";
            //};

        }

        private void Edit_InvalidInputEvent(bool IsInvalid)
        {
            if (IsInvalid)
                SelectBtn.IsEnabled = false;
            else
                SelectBtn.IsEnabled = true;
        }

        private void Btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Btn_Select_Click(object sender, RoutedEventArgs e)
        {
            string selected = edit.ConvertPresetValue2String();
            string trimmed = selected.Trim();

            if (string.IsNullOrEmpty(trimmed))
            {
                CustomMessageBoxWindow.Show(CultureStringInfo.EditWatermark_DlgBox_Title,
                CultureStringInfo.EditWatermark_DlgBox_Subject,
                CultureStringInfo.EditWatermark_DlgBox_Details,
                CustomMessageBoxWindow.CustomMessageBoxIcon.Warning,
                CustomMessageBoxWindow.CustomMessageBoxButton.BTN_CLOSE);
                return;
            }

            //Invoke delegate event.
            WatermarkHandler?.Invoke(this, new WatermarkArgs(trimmed));
            //Close window.
            this.Close();
        }
    }

    public class WatermarkArgs : EventArgs
    {
        private string watermarkvalue;
        public WatermarkArgs(string value)
        {
            this.watermarkvalue = value;
        }
        public string Watermarkvalue
        {
            get { return watermarkvalue; }
            set { watermarkvalue = value; }
        }
    }
}
