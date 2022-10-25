using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SkydrmLocal.rmc.ui.components
{
    /// <summary>
    /// Interaction logic for CustomSearchBox.xaml
    /// </summary>
    public partial class CustomSearchBox : UserControl
    {
        private Image searchImg;

        public CustomSearchBox()
        {
            InitializeComponent();
        }

        // define event
        public event EventHandler<SearchEventArgs> SearchEvent;


        private void ExecuteSearch(string text)
        {
            // Console.WriteLine("------" + this.TbxInput.Text);
            if (SearchEvent != null)
            {
                Console.WriteLine("******" + this.TbxInput.Text);
                var args = new SearchEventArgs();
                args.SearchText = text; // why Text always is empty using this.TbxInput.Text?
                // raise the search event
                SearchEvent(this, args); 
            }
        }

        private void Tbx_content_TextChanged(object sender, TextChangedEventArgs e)
        {
            Console.WriteLine("--text changed!!!----" + this.TbxInput.Text);
            string sourceText = (e.Source as TextBox).Text;

            object child;
            FindChild(this.TbxInput, out child);
            searchImg = child as Image;

            if (string.IsNullOrEmpty(sourceText)) // set clear icon
            {
                SetImageIcon(searchImg, @"/rmc/resources/icons/Icon_search24.png");
            } else // reset search icon
            {
                SetImageIcon(searchImg, @"/rmc/resources/icons/Icon_clear_16.png");
            }


            // do search
            ExecuteSearch(sourceText);
        }

        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            if (IsClearImage())
            {
                this.TbxInput.Text = "";
            }
        }

        /// <summary>
        ///  find one child Control by VisualTreeHelper
        /// </summary>
        /// <param name="parent">parent Control</param>
        /// <param name="_child">the child control that want to find</param>
        private void FindChild(DependencyObject parent, out object _child)
        {
            _child = null;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child != null && (child is Grid || child is Button))
                {
                    FindChild(child, out _child);
                    break;
                }

                if (child != null && child is Image)
                {
                    _child = child as Image;
                    break;
                }
            }

        }

        /// <summary>
        /// Set the icon dynamically for the Image control of the search button.
        /// </summary>
        private void SetImageIcon(Image img, string uriPath)
        {
            BitmapImage bmp = new BitmapImage();
            bmp.BeginInit(); // do init
            bmp.UriSource = new Uri(uriPath, UriKind.Relative); // set icon path.
            bmp.EndInit();
            img.Source = bmp;
        }

        /// <summary>
        /// Judge current button is search or clear.
        /// </summary>
        /// <returns></returns>
        private bool IsClearImage()
        {
            if (searchImg==null)
            {
                return false;
            }
            else
            {
                string test = searchImg.Source.ToString();
                return searchImg.Source.ToString().Equals(@"pack://application:,,,/rmc/resources/icons/Icon_clear_16.png");
            }
            
        }


        public class SearchEventArgs : EventArgs
        {
            public string SearchText { get; set; }
        }

    }

    public class Length2BoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int textLength = (int)value;
            return textLength > 0 ? true : false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
