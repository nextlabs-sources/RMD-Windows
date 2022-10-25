using SkydrmDesktop;
using SkydrmDesktop.Resources.languages;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.ui.utils;
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
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SkydrmLocal.rmc.ui.windows
{
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


        public class CustomMessageBoxButton
        {
            public static string BTN_OK = CultureStringInfo.ApplicationFindResource("DlgBox_BTN_OK");
            public static string BTN_YES = CultureStringInfo.ApplicationFindResource("DlgBox_BTN_YES");
            public static string BTN_NO = CultureStringInfo.ApplicationFindResource("DlgBox_BTN_NO");
            //public const string BTN_OK = "OK";
            public static string BTN_DELETE = CultureStringInfo.ApplicationFindResource("DlgBox_BTN_DELETE");
            public static string BTN_CANCEL = CultureStringInfo.ApplicationFindResource("DlgBox_BTN_CANCEL");
            public static string BTN_CLOSE = CultureStringInfo.ApplicationFindResource("DlgBox_BTN_CLOSE");

            public static string BTN_OVERWRITE = CultureStringInfo.ApplicationFindResource("DlgBox_BTN_OVERWRITE");
            public static string BTN_DISCARD = CultureStringInfo.ApplicationFindResource("DlgBox_BTN_DISCARD");

            // can also extend other button here ....
        }

        #region Filed

        public string MessageBoxTitle { get; set; }

        public string MessageSubjectText { get; set; }

        public string MessageBoxText { get; set; }
       
        public string ImagePath { get; set; }
           
        public CustomMessageBoxResult Result { get; set; }

        #endregion

        private CustomMessageBoxWindow(bool isShowWindowCloseBtn = true)
        {
            InitializeComponent();

            this.DataContext = this;

            Result = CustomMessageBoxResult.None;

            if (!isShowWindowCloseBtn)
            {
                this.Loaded += delegate {
                    var hwnd = new WindowInteropHelper(this).Handle;
                    Win32Common.SetWindowLong(hwnd, Win32Common.GWL_STYLE, Win32Common.GetWindowLong(hwnd, Win32Common.GWL_STYLE) & ~Win32Common.WS_SYSMENU);
                };
                this.KeyDown += (ss, ee) => {
                    if (ee.Key == Key.System && ee.SystemKey == Key.F4)
                    {
                        ee.Handled = true;
                    }
                };
            }

        }

        /// <summary>
        /// Show CustomMessageBox
        /// </summary>
        /// <param name="title"></param>
        /// <param name="subjectText"></param>
        /// <param name="details"></param>
        /// <param name="image"></param>
        /// <param name="positiveBtnContent"></param>
        /// <param name="neutralBtnContent"></param>
        /// <param name="negativeBtnContent"></param>
        /// <returns></returns>
        public static CustomMessageBoxResult Show(string title,
            string subjectText,
            string details,
            CustomMessageBoxIcon image,
            string positiveBtnContent, // At least one button
            string negativeBtnContent = null, // Should pass this if need two buttons
            string neutralBtnContent = null, // Should pass this if need three buttons
            int fontSize = 14, // default
            bool isShowWindowCloseBtn = true
           )
        {
            try
            {
                CustomMessageBoxWindow window = new CustomMessageBoxWindow(isShowWindowCloseBtn);

                //window.Owner = Application.Current.MainWindow;
                //window.Topmost = true;
                //fix Bug 58746 - Update offline file can pop up multiple windows for prompt update
                window.Owner=SkydrmApp.Singleton.MainWin;

                window.MessageBoxTitle = title;
                window.MessageSubjectText = subjectText;
                window.MessageBoxText = details;

                window.Tb_Details.FontSize = fontSize;

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
            catch (Exception e)
            {
                SkydrmApp.Singleton.Log.Error(e.ToString());
            }

            return CustomMessageBoxResult.None;
        }

        /// <summary>
        /// Show applyAll checkBox CustomWindow
        /// </summary>
        /// <param name="isApplyAll"></param>
        /// <param name="title"></param>
        /// <param name="subjectText"></param>
        /// <param name="details"></param>
        /// <param name="image"></param>
        /// <param name="positiveBtnContent"></param>
        /// <param name="negativeBtnContent"></param>
        /// <param name="neutralBtnContent"></param>
        /// <param name="fontSize"></param>
        /// <param name="isShowWindowCloseBtn"></param>
        /// <returns></returns>
        public static CustomMessageBoxResult Show(out bool isApplyAll,
            string title, string subjectText, string details,
            CustomMessageBoxIcon image,
            string positiveBtnContent, // At least one button
            string negativeBtnContent = null, // Should pass this if need two buttons
            string neutralBtnContent = null, // Should pass this if need three buttons
            int fontSize = 14, // default
            bool isShowWindowCloseBtn = true
           )
        {
            isApplyAll = false;
            try
            {
                CustomMessageBoxWindow window = new CustomMessageBoxWindow(isShowWindowCloseBtn);

                //window.Owner = Application.Current.MainWindow;
                //window.Topmost = true;
                //fix Bug 58746 - Update offline file can pop up multiple windows for prompt update
                window.Owner = SkydrmApp.Singleton.MainWin;

                window.MessageBoxTitle = title;
                window.MessageSubjectText = subjectText;
                window.MessageBoxText = details;

                window.Tb_Details.FontSize = fontSize;

                window.Cb_ApplyAll.Visibility = Visibility.Visible;

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
                isApplyAll = (bool)window.Cb_ApplyAll.IsChecked;
                return window.Result;
            }
            catch (Exception e)
            {
                SkydrmApp.Singleton.Log.Error(e.ToString());
            }

            return CustomMessageBoxResult.None;
        }

        private bool IsCloseByClickX = true;
        private void Positive_Btn_Click(object sender, RoutedEventArgs e)
        {
            Result = CustomMessageBoxResult.Positive;
            IsCloseByClickX = false;
            this.Close();
        }

        private void Neutral_Btn_Click(object sender, RoutedEventArgs e)
        {
            Result = CustomMessageBoxResult.Neutral;
            IsCloseByClickX = false;
            this.Close();
        }

        private void Negative_Btn_Click(object sender, RoutedEventArgs e)
        {
            Result = CustomMessageBoxResult.Negative;
            IsCloseByClickX = false;
            this.Close();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            // omit the cody by osmond, when other buttons pressed it will call this.close()
            // and the later will triger Windows_Closed() being called
            if (IsCloseByClickX)
            {
                Result = CustomMessageBoxResult.None;
            }
        }

        //public delegate void CloseClickDelegate();
        //public delegate void DeleteClickDelegate();
        //public delegate void CancelClickDelegate();

        //private CloseClickDelegate close;

        //private DeleteClickDelegate delete;

        //private CancelClickDelegate cancel;

        //private void Delete_Click(object sender, RoutedEventArgs e)
        //{
        //    delete?.Invoke();
        //}

        //private void Cancel_Click(object sender, RoutedEventArgs e)
        //{
        //    cancel?.Invoke();
        //}

        //private void Close_Click(object sender, RoutedEventArgs e)
        //{
        //    close?.Invoke();




    }

}
