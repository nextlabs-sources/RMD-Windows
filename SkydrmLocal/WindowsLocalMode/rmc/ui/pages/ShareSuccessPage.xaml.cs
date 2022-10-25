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
using System.Windows.Navigation;
using System.Windows.Shapes;
using SkydrmLocal.rmc.ui.pages.model;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.ComponentModel;
using SkydrmLocal.rmc.ui.utils;
using SkydrmLocal.rmc.ui.windows;
using SkydrmLocal.rmc.ui.windows.mainWindow.model;
using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.ui.components.RightsDisplay.model;

namespace SkydrmLocal.rmc.ui.pages
{
    /// <summary>
    /// Interaction logic for ShareSuccessPage.xaml
    /// </summary>
    public partial class ShareSuccessPage : Page
    {
        private ShareWindow parentWindow;
        //right display listBox ItemSource
        private IList<RightsItem> rightsItems = new List<RightsItem>();

        private SharePageSuccessBindConfigs bindConfigs = new SharePageSuccessBindConfigs();

        private ProtectAndShareConfig tempConfig = new ProtectAndShareConfig();

        public ShareWindow ParentWindow
        {
            get { return parentWindow; }
            set { parentWindow = value; }
        }

        public ShareSuccessPage(ProtectAndShareConfig configs)
        {
            InitializeComponent();
            tempConfig = configs;

            InitData(configs);

            if (configs.FileOperation.Action==FileOperation.ActionType.UpdateRecipients)
            {
                this.StackComment.Visibility = Visibility.Collapsed;            
            }
        }

        /// <summary>
        ///Re-bind data to the page based on the parameters passed by sharepage
        /// </summary>
        private void InitData(ProtectAndShareConfig configs)
        {
            if (configs == null)
            {
                return;
            }
            IList<string> rights = configs.RightsSelectConfig.Rights;

            StringBuilder sb = new StringBuilder();
            int length = configs.FileOperation.FileName.Length;
            for (int i = 0; i < length; i++)
            {
                if (string.IsNullOrEmpty(configs.FileOperation.FileName[i]))
                {
                    continue;
                }
                sb.Append(configs.FileOperation.FileName[i]);
                if (i != length - 1)
                {
                    sb.Append(";\r");
                }
            }
            //Set operation title for share success page.
            bindConfigs.OperationTitle = length > 1 ?
                 CultureStringInfo.CreateFileWin_Operation_Title_MShare : //For multiple files share title dispaly.
                 CultureStringInfo.CreateFileWin_Operation_Title_Share; //For single file share title display.

            bindConfigs.FileName = sb.ToString();
            //bindConfigs.FileName = configs.FileOperation.FileName;
            bindConfigs.WatermarkValue = configs.RightsSelectConfig.Watermarkvalue;
            bindConfigs.ValidityValue = configs.RightsSelectConfig.ExpireDateValue;
            //normalPath = configs.FileOperation.FilePath;
            //Add divide line.
            DivideLine.Children.Add(CreateDivideLine());

            //get Right Icons
            rightsItems = CommonUtils.GetRightsIcon(rights);
            foreach (var item in rightsItems)
            {
                if (item.Rights.Equals("Watermark"))
                {
                    this.WatermarkPanel.Visibility = Visibility.Visible;
                }
            }

            if (string.IsNullOrEmpty(configs.FileOperation.FailedFileName))
            {
                this.ShareSuccessText.Text = CultureStringInfo.ShareFileWin_Protect_Successful;
            }
            else
            {
                this.ShareSuccessTextBorder.BorderBrush=new SolidColorBrush(Color.FromRgb(0XFF, 0X00, 0X00));
                this.ShareSuccessText.Text = configs.FileOperation.FailedFileName;
                this.ShareSuccessText.Foreground= new SolidColorBrush(Color.FromRgb(0XFF, 0X00, 0X00));
            }

            this.rightsDisplayBoxes.ItemsSource = rightsItems;
            this.emailListsControl.ItemsSource = configs.SharedWithConfig.SharedEmailLists;
            this.commentTB.Text = configs.SharedWithConfig.Comments;
            this.DataContext = bindConfigs;
        }

        private void Button_Cancel(object sender, RoutedEventArgs e)
        {
            if (parentWindow != null)
            {
                parentWindow.Close();
            }

        }

        //Window Close will excute this method
        private void DoSettingAfterClose()
        {
            // Refresh automatically after protect.
            SkydrmLocalApp app = (SkydrmLocalApp)SkydrmLocalApp.Current;
            if (app.MainWin != null)
            {
                app.MainWin.viewModel.GetCreatedFile(tempConfig.CreatedFiles, tempConfig.SelectProjectFolderPath);
            }

            // close createFile window
            foreach (Window one in Application.Current.Windows)
            {
                if (one.Tag !=null && one.Tag == tempConfig.WinTag)
                {
                    one.Close();
                    break;
                }
            }
        }

        private void ShareSucceedPageLoaded(object sender, RoutedEventArgs e)
        {
            if (tempConfig.FileOperation.Action == FileOperation.ActionType.UpdateRecipients)
            {
                var fp = SkydrmLocalApp.Singleton.Rmsdk.User.GetNxlFileFingerPrint(tempConfig.FileOperation.FilePath[0]);
                bindConfigs.IsOwnerVisibility = fp.isOwner ? Visibility.Visible : Visibility.Collapsed;
            }
            else if (tempConfig.FileOperation.Action == FileOperation.ActionType.Share)
            {
                ParentWindow.Closed += (ss, ee) =>
                {
                    DoSettingAfterClose();
                };
            }

        }

        private Line CreateDivideLine()
        {
            return new Line
            {
                Stroke = (Brush)new BrushConverter().ConvertFromString("#BDBDBD"),
                StrokeThickness = 2.0,
                X1 = 0,
                X2 = 1000,
                Y1 = 0,
                Y2 = 0,
            };
        }
    }

    public class SharePageSuccessBindConfigs : INotifyPropertyChanged
    {
        private string fileName;
        private string watermarkValue;
        private string validityValue;
        private string operationTitle;
        private Visibility isOwnerVisibility = Visibility.Visible;

        public string FileName
        {
            get
            {
                return fileName;
            }
            set
            {
                fileName = value;
                OnPropertyChanged("FileName");
            }
        }
        public string WatermarkValue
        {
            get
            {
                return watermarkValue;
            }
            set
            {
                watermarkValue = value;
                OnPropertyChanged("WatermarkValue");
            }
        }
        public string ValidityValue
        {
            get
            {
                return validityValue;
            }
            set
            {
                validityValue = value;
                OnPropertyChanged("ValidityValue");
            }
        }

        public string OperationTitle
        {
            get
            {
                return operationTitle;
            }
            set
            {
                operationTitle = value;
                OnPropertyChanged("OperationTitle");
            }
        }
        public Visibility IsOwnerVisibility
        {
            get
            {
                return isOwnerVisibility;
            }
            set
            {
                isOwnerVisibility = value;
                OnPropertyChanged("IsOwnerVisibility");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
