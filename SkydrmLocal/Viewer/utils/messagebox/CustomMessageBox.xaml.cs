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

namespace Viewer.utils.messagebox
{
    /// <summary>
    /// Interaction logic for CustomMessageBox.xaml
    /// </summary>
    public partial class CustomMessageBoxWindow : Window
    {

        public enum CustomMessageBoxResult
        {
            None = 0,
            Positive = 1,
            Neutral = 2,
            Negative = 3
        }


        public enum CustomMessageBoxIcon
        {
            None = 0,
            Error = 1,
            Question = 2,
            Warning = 3
        }

        #region Filed

        public string MessageBoxTitle { get; set; }

        public string MessageSubjectText { get; set; }

        public string MessageBoxText { get; set; }

        public string ImagePath { get; set; }

        public CustomMessageBoxResult Result { get; set; }

        #endregion

        public CustomMessageBoxWindow()
        {
            InitializeComponent();

            this.DataContext = this;

            Result = CustomMessageBoxResult.None;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="title"></param>
        /// <param name="subjectText"></param>
        /// <param name="details"></param>
        /// <param name="image"></param>
        /// <param name="positiveBtnContent"></param>
        /// <param name="neutralBtnContent"></param>
        /// <param name="negativeBtnContent"></param>
        /// <returns></returns>
        public static CustomMessageBoxResult Show(Window ownerWin, string title,
            string subjectText,
            string details,
            CustomMessageBoxIcon image,
            string positiveBtnContent, // At least one button
            string negativeBtnContent = null, // Should pass this if need two buttons
            string neutralBtnContent = null // Should pass this if need three buttons
           )
        {
            CustomMessageBoxWindow window = new CustomMessageBoxWindow();
            window.Owner = ownerWin;

            //window.Owner = Application.Current.MainWindow;
            window.Topmost = true;

            window.MessageBoxTitle = title;
            window.MessageSubjectText = subjectText;
            window.MessageBoxText = details;

            // Set box image
            switch (image)
            {
                case CustomMessageBoxIcon.Question:
                    window.ImagePath = @"/rmc/resources/icons/Icon_red_warning.png";
                    break;
                case CustomMessageBoxIcon.Error:
                case CustomMessageBoxIcon.Warning:
                    window.ImagePath = @"/rmc/resources/icons/Icon_red_warning.png";
                    break;
                case CustomMessageBoxIcon.None:
                    window.ImagePath = @"/rmc/resources/icons/Icon_red_warning.png";
                    window.Icon.Visibility = Visibility.Collapsed;
                    break;
            }

            // Need Positive button
            if (positiveBtnContent != null)
            {
                window.Positive_Btn.Visibility = Visibility.Visible;
                window.Positive_Btn.Content = positiveBtnContent;
            }

            // Need negative button
            if (negativeBtnContent != null)
            {
                window.Negative_Btn.Visibility = Visibility.Visible;
                window.Negative_Btn.Content = negativeBtnContent;
            }

            // Need Neutral button
            if (neutralBtnContent != null)
            {
                window.Neutral_Btn.Visibility = Visibility.Visible;
                window.Neutral_Btn.Content = neutralBtnContent;
            }


            window.ShowDialog();
            return window.Result;
        }

        private void Positive_Btn_Click(object sender, RoutedEventArgs e)
        {
            Result = CustomMessageBoxResult.Positive;
            this.Close();
        }

        private void Neutral_Btn_Click(object sender, RoutedEventArgs e)
        {
            Result = CustomMessageBoxResult.Neutral;
            this.Close();
        }

        private void Negative_Btn_Click(object sender, RoutedEventArgs e)
        {
            Result = CustomMessageBoxResult.Negative;
            this.Close();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Result = CustomMessageBoxResult.None;
        }

        public class CustomMessageBoxButton
        {
            public const string BTN_OK = "OK";
            public const string BTN_DELETE = "Delete";
            public const string BTN_CANCEL = "Cancel";
            public const string BTN_CLOSE = "Close";

            // can also extend other button here ....
        }

    }
}
