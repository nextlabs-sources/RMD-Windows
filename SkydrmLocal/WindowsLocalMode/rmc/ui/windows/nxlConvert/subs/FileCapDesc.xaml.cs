using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
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

namespace SkydrmLocal.rmc.ui.windows.nxlConvert.subs
{
    /// <summary>
    /// Interaction logic for FileCapDesc.xaml
    /// </summary>
    public partial class FileCapDesc : UserControl
    {
        FileCapDescDataMode dataMode;
        public FileCapDesc()
        {
            InitializeComponent();
            dataMode = new FileCapDescDataMode();
            this.DataContext = dataMode;
            // give a default values
            Title = CultureStringInfo.CreateFileWin__Operation_Title_MProtect;
            this.Description = CultureStringInfo.CreateFileWin_Operation_Info_ADhoc;
            this.FileCount =0;
            this.FilesName = "";
        }

        #region Attribute Section
        public string Title { get => dataMode.Title; set => dataMode.Title = value; }
        public string Description { get => dataMode.Description; set => dataMode.Description = value; }
        public int FileCount { get => dataMode.FileCount; set => dataMode.FileCount = value; }
        public string FilesName { get => dataMode.FilesName; set => dataMode.FilesName = value; }

        public void ChangeFileIsVisibilty(Visibility visibility)
        {
            this.tb_ChangeFile.Visibility = visibility;
        }

        // Display Tags
        public Visibility TagVisible { get => dataMode.TagVisible; set => dataMode.TagVisible = value; }
        public void SetDisplayTags(Dictionary<string, List<string>> keyValues)
        {
            var tags = keyValues;
            //Check nonull for tags.
            if (tags != null || tags.Count != 0)
            {
                //Get the iterator of the dictionary.
                var iterator = tags.GetEnumerator();
                //If there is any items inside it.
                while (iterator.MoveNext())
                {
                    //Get the current one.
                    var current = iterator.Current;

                    var key = current.Key;
                    var values = current.Value;
                    for (int i = 0; i < values.Count; i++)
                    {
                        this.tb_FileTag.Inlines.Add(CreateRunValue(values[i]));
                        if (i < values.Count - 1)
                        {
                            this.tb_FileTag.Inlines.Add(CreateRunValue(", "));
                        }
                    }
                    this.tb_FileTag.Inlines.Add(CreateRunValue(" ("));
                    this.tb_FileTag.Inlines.Add(CreateRunKey(key));
                    this.tb_FileTag.Inlines.Add(CreateRunValue(")   "));
                }
                TagVisible = Visibility.Visible;
            }
        }
        private Run CreateRunValue(string value)
        {
            return new Run
            {
                Foreground = new SolidColorBrush(Colors.Black),
                FontSize = 14,
                Text = value,
                FontWeight = FontWeights.Normal,
            };
        }
        private Run CreateRunKey(string value)
        {
            return new Run
            {
                Foreground = new SolidColorBrush(Colors.Black),
                FontSize = 14,
                Text = value,
                FontWeight = FontWeights.DemiBold,
            };
        }
        #endregion

        #region Event callback
        public event MouseButtonEventHandler ChangeBtnClicked;
        #endregion

        #region Event
        private void OnClick_ChangeFile(object sender, MouseButtonEventArgs e)
        {
            ChangeBtnClicked?.Invoke(sender,e);
        }
        #endregion

    }
    /// <summary>
    /// DataMode for FileCapDesc.xaml
    /// </summary>
    public class FileCapDescDataMode : INotifyPropertyChanged
    {
        string mTitle;
        string mDesc;
        int mCount;
        string mFilesName;
        Visibility tagVisible=Visibility.Collapsed;

        public FileCapDescDataMode()
        {
        }

        public string Title { get => mTitle; set { mTitle = value; OnBindUIPropertyChanged("Title"); } }
        public string Description { get => mDesc; set { mDesc = value; OnBindUIPropertyChanged("Description"); } }
        public int FileCount { get => mCount; set { mCount = value; OnBindUIPropertyChanged("FileCount"); } }
        public string FilesName { get => mFilesName; set { mFilesName = value; OnBindUIPropertyChanged("FilesName"); } }

        public Visibility TagVisible { get => tagVisible; set { tagVisible = value; OnBindUIPropertyChanged("TagVisible"); } }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnBindUIPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }

    #region Convert
    /// <summary>
    /// When select mulit files,display file count
    /// </summary>
    public class FileCountTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                int fileCount = (int)value;
                if (fileCount > 1)
                {
                    return string.Format(@"({0})", fileCount);
                }
                return "";
            }
            catch (Exception)
            {
                return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    #endregion

}

