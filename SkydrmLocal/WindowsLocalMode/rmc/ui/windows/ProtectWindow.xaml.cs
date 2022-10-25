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
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.ui.pages;
using SkydrmLocal.rmc.ui.pages.model;
using SkydrmLocal.rmc.ui.windows.mainWindow.model;
using SkydrmLocal.rmc.ui.windows.nxlConvert;

namespace SkydrmLocal.rmc.ui.windows
{
    /// <summary>
    /// Interaction logic for ProtectWindow.xaml
    /// </summary>
    public partial class ProtectWindow : Window
    {
        private Window parentWin;
        public ProtectWindow(ProtectAndShareConfig config)
        {
            InitializeComponent();

            Init(config);
        }

        public ProtectWindow(Window parent, ProtectAndShareConfig config)
        {
            InitializeComponent();

            parentWin = parent;
            Init(config);
        }

        private void Init(ProtectAndShareConfig config)
        {
            this.Closed += (object sender, EventArgs e) =>
            {
                if (config.FileOperation.Action == FileOperation.ActionType.Protect)
                {
                    // Refresh automatically after protect.
                    SkydrmLocalApp app = (SkydrmLocalApp)SkydrmLocalApp.Current;
                    if (app.MainWin != null)
                    {
                        app.MainWin.viewModel.GetCreatedFile(config.CreatedFiles, config.SelectProjectFolderPath);
                    }
                }
                else if (config.FileOperation.Action == FileOperation.ActionType.ModifyRights)
                {
                    config.ModifyRightsFeature?.UploadToRms();
                }


                // close parent window.
                if (parentWin != null)
                {
                    // For debug log
                    if (parentWin is ConvertToNxlFileWindow)
                    {
                        SkydrmLocalApp.Singleton.Log.Info("Protect Window closed event, the Parent isClosing:" + (parentWin as ConvertToNxlFileWindow).IsClosing.ToString());
                    }

                    if (parentWin is ConvertToNxlFileWindow && !(parentWin as ConvertToNxlFileWindow).IsClosing)
                    {
                        parentWin.Close();
                    } else if(parentWin is NxlFileToConvertWindow)
                    {
                        parentWin.Close();
                    }
                }


            };


            ParseConfigs(config);
        }

        private void InitEvent()
        {
            // Used to handle window display issue across processse(open the window from viewer process)
            this.Loaded += new RoutedEventHandler(delegate (object sender, RoutedEventArgs e)
            {
                this.Topmost = false;
            });
            this.Activated += new EventHandler(delegate (object sender, EventArgs e)
            {
                this.Topmost = false;
                this.Focus();
            });
        }

        private void ParseConfigs(ProtectAndShareConfig config)
        {
            if (config == null)
            {
                Console.WriteLine("config is null in ProtectWindow");
                return;
            }
            ProtectSuccessPage protectPage = new ProtectSuccessPage(config)
            {
                ParentWindow = this
            };
            SwitchPage(protectPage);
            
        }

        //public ShareWindow(FileOperation file)
        //{
        //    MessageBox.Show(file.ToString());
        //}

        public void SwitchPage(Page page)
        {
            this.main_frame.Content = page;
        }
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            //Calculate half of the offset to move the form

            if (sizeInfo.HeightChanged)
                this.Top += (sizeInfo.PreviousSize.Height - sizeInfo.NewSize.Height) / 2;

            if (sizeInfo.WidthChanged)
                this.Left += (sizeInfo.PreviousSize.Width - sizeInfo.NewSize.Width) / 2;
        }


        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }

    }
}
