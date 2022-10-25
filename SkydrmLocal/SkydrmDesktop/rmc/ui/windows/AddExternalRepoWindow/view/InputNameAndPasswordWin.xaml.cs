using SkydrmDesktop.rmc.common.helper;
using SkydrmDesktop.rmc.ui.windows.AddExternalRepoWindow.addRepo;
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

namespace SkydrmDesktop.rmc.ui.windows.AddExternalRepoWindow.view
{
    /// <summary>
    /// InputNameAndPasswordWin.xaml  DataCommands
    /// </summary>
    public class InputPwd_DataCommands
    {
        private static RoutedCommand positive;
        private static RoutedCommand cancel;
        static InputPwd_DataCommands()
        {
            positive = new RoutedCommand(
              "Positive", typeof(InputPwd_DataCommands));

            InputGestureCollection input = new InputGestureCollection();
            input.Add(new KeyGesture(Key.Escape));
            cancel = new RoutedCommand(
              "Cancel", typeof(InputPwd_DataCommands), input);
        }
        /// <summary>
        ///  InputNameAndPasswordWin.xaml positive button command
        /// </summary>
        public static RoutedCommand Positive
        {
            get { return positive; }
        }
        /// <summary>
        /// InputNameAndPasswordWin.xaml cancel button command
        /// </summary>
        public static RoutedCommand Cancel
        {
            get { return cancel; }
        }
    }

    /// <summary>
    /// Interaction logic for InputNameAndPasswordWin.xaml
    /// </summary>
    public partial class InputNameAndPasswordWin : Window
    {
        private bool IsAuthing { get; set; }

        private IInputPwdAuth Opert { get; }

        public InputNameAndPasswordWin(IInputPwdAuth inputPwdAuth)
        {
            InitializeComponent();

            Opert = inputPwdAuth;
        }


        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void PositiveCmdBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            this.gridProBar.Visibility = Visibility.Visible;
            IsAuthing = true;

            AuthConfig authConfig = new AuthConfig(this.userName.Text, this.pwd.Password);
            AsyncHelper.RunAsync((para) => 
            {
                // invoke api to auth
                Opert.AuthSite(para.UserName, para.Password);
                return Opert.AuthResult;
            }, 
            authConfig,
            (rt)=> 
            {
                // re-set
                this.gridProBar.Visibility = Visibility.Collapsed;
                IsAuthing = false;

                if (rt)
                {
                    this.Close();
                }
            });
        }

        private void PositiveCmdBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            string userName = this.userName.Text;
            string pwd = this.pwd.Password;
            if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(pwd))
            {
                e.CanExecute = true;
            }
            else
            {
                e.CanExecute = false;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (IsAuthing)
            {
                e.Cancel = true;
            }
        }

        class AuthConfig
        {
            public AuthConfig(string userName, string pwd)
            {
                UserName = userName;
                Password = pwd;
            }

            public string UserName { get; private set; }
            public string Password { get; private set; }
        }

    }
}
