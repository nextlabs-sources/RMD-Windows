using SkydrmLocal.rmc.common.component;
using SkydrmLocal.rmc.ui;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SkydrmLocal.rmc.app
{
    public class TrayIconManager
    {
        public System.Windows.Forms.NotifyIcon ni;

        private SkydrmLocalApp app;

        public System.Windows.Forms.ContextMenu ContextMenu { get; set; }

        public bool IsLogin { get; set; }

        public Window PopupTargetWin { get; set; }

        public TrayIconManager(SkydrmLocalApp app)
        {
            ni = new System.Windows.Forms.NotifyIcon();
            this.app = app;

            IsLogin = false;

            InitIcon();

            InitIconClick();
        }

        public static bool IsWindows7
        {
            get
            {
                return (Environment.OSVersion.Platform == PlatformID.Win32NT) && (Environment.OSVersion.Version.Major == 6) && (Environment.OSVersion.Version.Minor == 1);
            }
        }

        private void InitIcon()
        {
            // init icon
            var stream = new Uri(@"rmc/resources/icons/TrayIcon2.png", UriKind.Relative);
            if (IsWindows7)
            {
                stream = new Uri(@"rmc/resources/icons/TrayIcon.png", UriKind.Relative);
            }
            else
            {
                stream = new Uri(@"rmc/resources/icons/TrayIcon2.png", UriKind.Relative);
            }

            var bitmap = new Bitmap(Application.GetResourceStream(stream).Stream);
            ni.Icon = System.Drawing.Icon.FromHandle(bitmap.GetHicon());

            // init bubble.
            ni.Text = "Service Manager - SkyDRM";
            ni.BalloonTipText = "This Ballon Tip Text Is Used For Debug Only";
            ni.ShowBalloonTip(1000);

            // init right context menu.
            ContextMenu = new System.Windows.Forms.ContextMenu();
            InitMenuItem();

            // display
            ni.Visible = true;
        }

        private void InitIconClick()
        {
            ni.MouseClick += (ss, ee) =>
            {
                try
                {
                    // not login, such as Splash window, login window ...
                    if (!IsLogin)
                    {

                        if (PopupTargetWin == null)
                            return;

                        if (PopupTargetWin.Visibility != Visibility.Visible)
                        {
                            PopupTargetWin.Show();
                            PopupTargetWin.Activate();
                            PopupTargetWin.WindowState = WindowState.Normal;
                        }
                        else
                        {
                            PopupTargetWin.Hide();
                        }
                    }
                    else // login
                    {
                        if (Keyboard.IsKeyDown(Key.LeftCtrl))
                        {
                            app.Mediator.OnShowMain(null);
                        }
                        else
                        {
                            PopupServiceManager(ss, ee);
                        }
                    }
                }catch(Exception e)
                {
                    app.Log.Error(e.Message, e);
                }
            };
        }

        /// <summary>
        /// Will add Logout item after login.
        /// </summary>
        public void RefreshMenuItem()
        {
            ContextMenu.MenuItems.Clear();
            InitMenuItem();
        }

        private void InitMenuItem()
        {
            // About item
            System.Windows.Forms.MenuItem itemAbout = new System.Windows.Forms.MenuItem();
            itemAbout.Name = CultureStringInfo.MenuItem_About;
            itemAbout.Text = CultureStringInfo.MenuItem_About;
            itemAbout.Click += (ss, ee) =>
            {
                //app.ShowBalloonTip(Constant.BALLON_TIP);       
                app.Mediator.OnShowAboutTheProject();
            };
            ContextMenu.MenuItems.Add(itemAbout);

            // logout item
            if (IsLogin)
            {
                System.Windows.Forms.MenuItem itemLogout = new System.Windows.Forms.MenuItem();
                itemLogout.Name = CultureStringInfo.MenuItem_Logout;
                itemLogout.Text = CultureStringInfo.MenuItem_Logout;
                itemLogout.Click += (ss, ee) =>
                {
                    app.Logout();
                };

                MenuDisableMgr.GetSingleton().MenuItemDisabled += (string itemName, bool isDisabled) =>
                {
                    if (itemName == CultureStringInfo.MenuItem_Logout)
                    {
                        if (isDisabled)
                            itemLogout.Enabled = false;
                        else
                            itemLogout.Enabled = true;
                    }
                };

                ContextMenu.MenuItems.Add(itemLogout);

            }

            // Exit item
            System.Windows.Forms.MenuItem itemExit = new System.Windows.Forms.MenuItem();
            itemExit.Name = CultureStringInfo.MenuItem_Exit;
            itemExit.Text = CultureStringInfo.MenuItem_Exit;
            itemExit.Click += (ss, ee) =>
            {
                app.MaunalExit();
            };

            MenuDisableMgr.GetSingleton().MenuItemDisabled += (string itemName, bool isDisabled) =>
            {
                if (itemName == CultureStringInfo.MenuItem_Exit)
                {
                    if (isDisabled)
                        itemExit.Enabled = false;
                    else
                        itemExit.Enabled = true;
                }
            };

            ContextMenu.MenuItems.Add(itemExit);

            ni.ContextMenu = ContextMenu;

        }

        private void PopupServiceManager(object ss, System.Windows.Forms.MouseEventArgs ee)
        {
            SkydrmLocalApp.clickEventfromNotifyIcon = true;

            try
            {
                Monitor.Enter(SkydrmLocalApp.IsOpenSM);

                if (ee.Button != System.Windows.Forms.MouseButtons.Left)
                    return;

                // Show or Hide Service Manager
                if (app.ServiceManager == null)
                    return;

                if (app.ServiceManager.Visibility != Visibility.Visible)
                {
                    SkydrmLocalApp.IsOpenSM = true;
                    app.ServiceManager.Show();
                    app.ServiceManager.Activate();
                    app.ServiceManager.WindowState = WindowState.Normal;
                }
                else
                {
                    SkydrmLocalApp.IsOpenSM = false;
                    app.ServiceManager.Hide();
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                try
                {
                    Monitor.Enter(SkydrmLocalApp.IsOpenSM);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }

                SkydrmLocalApp.clickEventfromNotifyIcon = false;
            }
        }
    }
}
