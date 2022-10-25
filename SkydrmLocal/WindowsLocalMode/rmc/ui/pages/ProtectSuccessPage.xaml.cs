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
using System.ComponentModel;
using SkydrmLocal.rmc.ui.utils;
using SkydrmLocal.rmc.ui.components.RightsDisplay.model;

namespace SkydrmLocal.rmc.ui.pages
{
    /// <summary>
    /// Interaction logic for ProtectPage.xaml
    /// </summary>
    public partial class ProtectSuccessPage : Page
    {
        private Window parentWindow;
        private ProtectPageBindConfigs bindConfigs = new ProtectPageBindConfigs();
        private IList<RightsItem> rightsItems = new List<RightsItem>();

        public Window ParentWindow
        {
            get { return parentWindow; }
            set { parentWindow = value; }
        }

        public ProtectSuccessPage(ProtectAndShareConfig configs)
        {
            InitializeComponent();
            InitData(configs);
        }

        private void InitData(ProtectAndShareConfig configs)
        {
            if (configs == null)
            {
                return;
            }

            //Means file protected with central policy
            if(configs.CentralPolicyRadioIsChecked)
            {
                //Display central rights view
                Init_CentralRightsView(configs);
            }
            else
            {
                //Display ad-hoc rights view
                Init_AdhocRightsView(configs);
            }
            //Bind data context.
            this.DataContext = bindConfigs;

            if ( !string.IsNullOrEmpty(configs.FileOperation.FailedFileName))
            {
                this.ProtectFailedTextBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0XFF, 0X00, 0X00));
                this.ProtectFailedText.Text = configs.FileOperation.FailedFileName;
            }
     
        }

        private void Init_BindConfig(ProtectAndShareConfig configs)
        {
            //Append file name string from FileOperation.FileName string[].
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
            //Set operation title for protect success page.
            bindConfigs.OperationTitle = length > 1 ?
                        CultureStringInfo.CreateFileWin__Operation_Title_MProtect : //For multiple files protect title display.
                        CultureStringInfo.CreateFileWin__Operation_Title_Protect; //For single file protect title display.
            bindConfigs.OperationPrompt = length > 1 ?
                        CultureStringInfo.ProtectSuccessPage_PathtextHave : //For multiple files have text display.
                        CultureStringInfo.ProtectSuccessPage_PathtextHas; //For single file protect has text display.

            // If is modify rights or add file to project, should display "modified the protected file" title
            if (configs.FileOperation.Action == windows.mainWindow.model.FileOperation.ActionType.ModifyRights)
            {
                bindConfigs.OperationTitle = CultureStringInfo.NxlFileToCvetWin_Header_Title_ModifyRights;
            }
            if (configs.ShareNxlFeature?.GetProjectShareAction() == shareNxlFeature.ShareNxlFeature.ShareNxlAction.AddFileToProject)
            {
                bindConfigs.OperationTitle = CultureStringInfo.NxlFileToCvetWin_Header_Title;
            }

            bindConfigs.FileName = sb.ToString();
            bindConfigs.Desitination = configs.SelectProjectFolderPath;
            bindConfigs.WatermarkValue = configs.RightsSelectConfig.Watermarkvalue;
            bindConfigs.ValidityValue = configs.RightsSelectConfig.ExpireDateValue;
        }

        private void Init_AdhocRightsView(ProtectAndShareConfig  configs)
        {
            IList<string> rights = configs.RightsSelectConfig.Rights;

            Init_BindConfig(configs);

            RightsDescriptionTB.Visibility = Visibility.Collapsed;

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

            //Bind data context.
            this.rightsDisplayBoxes.ItemsSource = rightsItems;
        }

        private void Init_CentralRightsView(ProtectAndShareConfig config)
        {
            //Check nonull for config.
            if (config == null)
            {
                return;
            }
            
            Init_BindConfig(config);

            RightsTypeTB.Text = CultureStringInfo.ProtectSuccessPage_RightsTypeTB;
            RightsDescriptionTB.Text = CultureStringInfo.ProtectSuccessPage_RightsDescriptionTB;

            //rightsDisplayBoxes.Visibility = Visibility.Collapsed;
            //WatermarkPanel.Visibility = Visibility.Collapsed;
            //ValidityPanel.Visibility = Visibility.Collapsed;
            //DivideLine.Visibility = Visibility.Collapsed;
            ////Get tags setting by config.
            //var tags = config.Tags;
            ////Check nonull for tags.
            ////If there is nothing just return.
            //if (tags == null || tags.Count == 0)
            //{
            //    return;
            //}
            ////Get the iterator of the dictionary.
            //var iterator = tags.GetEnumerator();
            ////If there is any items inside it.
            //while(iterator.MoveNext())
            //{
            //    //Get the current one.
            //    var current = iterator.Current;

            //    var key = current.Key;
            //    var values = current.Value;
            //    var panel = CreateDisplayPanel(key, values);

            //    //Add each panel which contains classification and values of classification.
            //    RightsStackPanel.Children.Add(panel);
            //}

            // Use new component
            this.RightsStackPanel.Visibility = Visibility.Collapsed;
            this.TagRightsView.Visibility = Visibility.Visible;
            this.TagRightsDisplay.InitTagView(config.Tags);
            if (config.RightsSelectConfig.Rights !=null && config.RightsSelectConfig.Rights.Count !=0)
            {
                rightsItems = CommonUtils.GetRightsIcon(config.RightsSelectConfig.Rights, false);
            }
            this.TagRightsDisplay.InitRightsDisplay(rightsItems.ToList(), null, null, Visibility.Collapsed, Visibility.Collapsed);
        }

        private StackPanel CreateDisplayPanel(string key, List<string> values)
        {
            StackPanel panel = new StackPanel
            {
                //Set each child panel display horizontally.
                Orientation = Orientation.Horizontal
            };
            //Add classification textblock.
            panel.Children.Add(CreateClassificationTB(key));
            //Add values belong to classification textblok.
            panel.Children.Add(CreateValusTB(values));
            return panel;
        }

        private TextBlock CreateClassificationTB(string text)
        {
            return new TextBlock
            {
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 10, 5, 5),
                Foreground = (Brush)new BrushConverter().ConvertFromString("#000000"),
                FontSize = 16,
                FontFamily = new FontFamily("Lato"),
                FontStyle = FontStyles.Normal,
                FontWeight = FontWeights.SemiBold,
                Text = text + ":"
            };
        }

        private TextBlock CreateValusTB(List<string> values)
        {
            if (values == null || values.Count == 0)
            {
                return new TextBlock { Text = "," };
            }
            StringBuilder valuesStr = new StringBuilder();
            for (int i = 0; i < values.Count; i++)
            {
                valuesStr.Append(values[i]);
                if (i != values.Count - 1)
                {
                    valuesStr.Append(",    ");
                }
            }
            return new TextBlock
            {
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 10, 5, 5),
                Foreground = (Brush)new BrushConverter().ConvertFromString("#4F4F4F"),
                FontSize = 14,
                FontFamily = new FontFamily("Lato"),
                Text = valuesStr.ToString(),
            };
        }

        private void Button_Cancel(object sender, RoutedEventArgs e)
        {
            if (parentWindow != null)
            {
                parentWindow.Close();
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

    public class ProtectPageBindConfigs : INotifyPropertyChanged
    {
        private string fileName;
        private string watermarkValue;
        private string validityValue;
        private string desitination;
        private string operationTitle;
        private string operationPrompt;

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

        public string Desitination { get => desitination; set => desitination = value; }
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

        public string OperationPrompt
        {
            get
            {
                return operationPrompt;
            }
            set
            {
                operationPrompt = value;
                OnPropertyChanged("OperationPrompt");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
