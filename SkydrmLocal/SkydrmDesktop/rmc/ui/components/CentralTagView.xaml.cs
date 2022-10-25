using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CentralRigthsControl
{
    /// <summary>
    /// Interaction logic for CentralTagView.xaml
    /// </summary>
    public partial class CentralTagView : UserControl
    {
        private Dictionary<string, List<string>> mTags;
        private double mMaxWidth = 500;

        public CentralTagView()
        {
            InitializeComponent();
        }

        public CentralTagView(Dictionary<string, List<string>> tags)
        {
            InitializeComponent();

            InitializeTags(tags);
        }

        public void InitializeTags(Dictionary<string, List<string>> tags)
        {
            TagsContainer.Children.Clear();

            this.mTags = tags;
            //Add each panel which contains classification and values of classification.
            foreach (KeyValuePair<string, List<string>> temp in tags)
            {
                var panel = CreateDisplayPanel(temp.Key, temp.Value);
                //Add each panel which contains classification and values of classification.
                TagsContainer.Children.Add(panel);
            }
        }
        
        public void SetMaxWidth(double maxWidth)
        {
            this.mMaxWidth = maxWidth;
        }

        public Dictionary<string,List<string>> GetTags()
        {
            return mTags;
        }

        public double GetMaxWidth()
        {
            return mMaxWidth;
        }

        private StackPanel CreateDisplayPanel(string key, List<string> values)
        {
            StackPanel panel = new StackPanel
            {
                //Set each child panel display horizontally.
                Orientation = Orientation.Horizontal,
            };
            //Add classification textblock.
            var cTb = CreateClassificationTB(key);

            panel.Children.Add(cTb);

            panel.Children.Add(CreateTagValuePanel(values));

            return panel;
        }

        private TextBlock CreateClassificationTB(string text)
        {
            return new TextBlock
            {
                TextAlignment = TextAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(5, 5, 2, 2),
                Foreground = (Brush)new BrushConverter().ConvertFromString("#000000"),
                FontSize = 14,
                FontFamily = new FontFamily("Lato"),
                FontStyle = FontStyles.Normal,
                FontWeight = FontWeights.SemiBold,
                Text = text + " : "
            };
        }

        private WrapPanel CreateTagValuePanel(List<string> values)
        {
            if (values == null || values.Count == 0)
            {
                return new WrapPanel();
            }
            WrapPanel wp = new WrapPanel()
            {
                Orientation = Orientation.Horizontal,
                MaxWidth = mMaxWidth,
            };
            for (int i = 0; i < values.Count; i++)
            {
                wp.Children.Add(new TextBlock
                {
                    TextAlignment = TextAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(5, 5, 2, 2),
                    Foreground = (Brush)new BrushConverter().ConvertFromString("#4F4F4F"),
                    FontSize = 14,
                    FontFamily = new FontFamily("Lato"),
                    Text = i == values.Count - 1 ? values[i] : values[i] + ",  ",
                    TextWrapping = TextWrapping.Wrap
                });
            }
            return wp;
        }
    }
}
