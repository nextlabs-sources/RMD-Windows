using SkydrmLocal.rmc;
using SkydrmLocal.rmc.ui.pages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using SkydrmLocal.rmc.sdk;
using System.Windows.Controls.Primitives;
using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.ui;

namespace SkydrmLocal.Pages
{
    /// <summary>
    /// Interaction logic for SelectDocumentClassifyPage.xaml
    /// </summary>
    public partial class PageSelectDocumentClassify : Page
    {   
       // private IList<ClassificationItem> sensitiveItemSources = new List<ClassificationItem>();
        private IList<ClassificationItem> projectItemSources = new List<ClassificationItem>();

        private SkydrmLocalApp app = (SkydrmLocalApp)SkydrmLocalApp.Current;

        private ObservableCollection<ClassificationItem> sensitiveItemSources = new ObservableCollection<ClassificationItem>();


        public ObservableCollection<ClassificationItem> SensitiveItemSources
        {
            get { return sensitiveItemSources; }
            set { sensitiveItemSources = value; }
        }

        private Dictionary<string, LabelUIElement> ProjectClassifications = new Dictionary<string, LabelUIElement>();


        public PageSelectDocumentClassify()
        {
            InitializeComponent();
        }

        public void SetProject(IMyProject myProject)
        {
            ProjectClassification[] projectClassifications = myProject.ListClassifications();

            InitData(projectClassifications);
        }

        public void SetSystemProject(ISystemProject systemProject)
        {
            ProjectClassification[] projectClassifications = systemProject.GetClassifications();

            InitData(projectClassifications);
        }

        private void InitData(ProjectClassification[] classifications)
        {
            if (classifications.Length != 0)
            {
                WrapPanel.Children.Add(CompanyDefineString(CultureStringInfo.ProtectSuccessPage_RightsDescriptionTB));
            }

            foreach (ProjectClassification section in classifications)
            {
                if (ProjectClassifications.ContainsKey(section.name))
                {
                    continue;
                }

                LabelUIElement labelUIElement = new LabelUIElement
                {
                    IsMultiSelect = section.isMultiSelect,
                    IsMandatory = section.isMandatory
                };

                //Create Classification textblock.
                TextBlock titleTB = CreateClassificationTB(section.name);

                labelUIElement.Title = titleTB;
                //Attach classificaiton textblock to the wrap panel.
                WrapPanel.Children.Add(titleTB);

                List<ToggleButton> labelBTs = new List<ToggleButton>();

                labelUIElement.Lables = labelBTs;

                ProjectClassifications.Add(section.name, labelUIElement);

                UInt16 checkedSize = 0;
                foreach (KeyValuePair<String, Boolean> kv in section.labels)
                {
                    ToggleButton label = new ToggleButton
                    {
                        Tag = section.name,
                        Padding = new Thickness(15, 5, 15, 5)
                    };

                    labelBTs.Add(label);
                    //If tag is selected by default then set the toogle button as checked.
                    if (kv.Value)
                    {
                        checkedSize++;
                        label.IsChecked = true;
                    }
                    else
                    {
                        label.IsChecked = false;
                    }
                    //Bind toggle button checked event.
                    label.Checked+= new RoutedEventHandler(ToggleButton_Checked);
                    //Bind toggle button unchecked event.
                    label.Unchecked+= new RoutedEventHandler(ToggleButton_Unchecked);

                    label.Content = kv.Key;
                    //Bind those label button to wrap panel.
                    WrapPanel.Children.Add(label);
                }

                //If this section is mandatory and no item selected by default.
                //then keep the mandatory run as hint style by set forground color as red.
                if (section.isMandatory)
                {
                    AddMandatory(titleTB, checkedSize == 0);
                }
            }
        }

        private TextBlock CompanyDefineString(string value)
        {
            return new TextBlock
            {
                Width=1000,
                Foreground = new SolidColorBrush(Colors.Gray),
                Background = new SolidColorBrush(Colors.White),
                Padding = new Thickness(10, 20, 10, 10),
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment=HorizontalAlignment.Center,
                FontSize = 14,
                FontFamily = new FontFamily("Lato"),
                FontStyle = FontStyles.Normal,
                FontWeight = FontWeights.Regular,
                Text = value
            };
        }

        private TextBlock CreateClassificationTB(string value)
        {
            return new TextBlock
            {
                Width = 1000,
                Foreground = new SolidColorBrush(Colors.Black),
                Background = new SolidColorBrush(Colors.White),
                Padding = new Thickness(10, 20, 10, 10),
                TextAlignment = TextAlignment.Left,
                FontSize = 16,
                FontFamily = new FontFamily("Lato"),
                FontStyle = FontStyles.Normal,
                FontWeight = FontWeights.SemiBold,
                Text = value
            };
        }

        private void AddMandatory(TextBlock tb, bool hint)
        {
            tb.Inlines.Add(CreateMandatoryRun(hint ? Colors.Red : Colors.DarkGray));
        }

        private Run CreateMandatoryRun(Color color)
        {
            return new Run {
                Foreground = new SolidColorBrush(color),
                FontSize = 14,
                Text = " (Mandatory)",
                FontWeight = FontWeights.Normal,
            };
        }

        private void UpdateTitleTB(TextBlock title,bool hint)
        {
            string value = title.Text;
            title.Inlines.Remove(title.Inlines.LastInline);
            AddMandatory(title, hint);
        }

        private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            ToggleButton label = sender as ToggleButton;

            foreach (KeyValuePair<string, LabelUIElement> kvp in ProjectClassifications)
            {
                if (kvp.Key.Equals(label.Tag.ToString()))
                {
                    LabelUIElement labelUIElement= kvp.Value;

                   if(labelUIElement.IsMandatory)
                    {
                        bool b = false;
                        foreach (ToggleButton item in labelUIElement.Lables)
                        {
                            if (item.IsChecked == true)
                            {
                                b = true;
                                break;
                            }                           
                        }

                        if (!b)
                        {
                            //label.IsChecked = true;
                            TextBlock title = labelUIElement.Title;
                            UpdateTitleTB(title, true);
                        }
                    }
                }
            }
        }

        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            ToggleButton target = sender as ToggleButton;
            //Keep the target tag string.
            string content = target.Content.ToString();
            //Iterate the ProjectClassifications to find the current LabelUIElement belong to the current tag.
            foreach (KeyValuePair<string, LabelUIElement> kvp in ProjectClassifications)
            {
                if (kvp.Key.Equals(target.Tag.ToString()))
                {
                    //The current LabelUIElement section the checked item belongs to.
                    LabelUIElement section = kvp.Value;
                    //If this section not support multi select
                    //then should diselect the rest of items belong to the current section.
                    if (!section.IsMultiSelect)
                    {
                        foreach (ToggleButton label in section.Lables)
                        {
                            string labelContent = label.Content.ToString();
                            if(!content.Equals(labelContent))
                            {
                                if (label.IsChecked == true)
                                {
                                    label.IsChecked = false;
                                }
                            }
                        }
                    }
                    //If this section is mandatory 
                    //then should check if there is no item checked.
                    if (section.IsMandatory)
                    {
                        TextBlock title = section.Title;
                        UpdateTitleTB(title, false);
                    }
                }
            }
        }

        //Return incorrect Choosed Classification key
        public List<string> IsCorrectChooseClassification()
        {
            List<string> result = new List<string>();
            foreach (KeyValuePair<string, LabelUIElement> kvp in ProjectClassifications)
            {
                string key = kvp.Key;
                LabelUIElement labelUIElement = kvp.Value;
                if (labelUIElement.IsMandatory)
                {
                    bool IsCorrectChoose = false;
                    foreach (ToggleButton item in labelUIElement.Lables)
                    {
                        if (item.IsChecked == true)
                        {
                            IsCorrectChoose = true;
                            break;
                        }
                    }
                    if (!IsCorrectChoose)
                    {
                        result.Add(key);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Build tags for protect using central policy.
        /// </summary>
        /// <returns></returns>
        public UserSelectTags GetClassification()
        {
            UserSelectTags userSelectTags = new UserSelectTags();

            foreach (KeyValuePair<string, LabelUIElement> kvp in ProjectClassifications)
            {
                string key = kvp.Key;
                LabelUIElement labelUIElement = kvp.Value;
                List<string> tags = new List<string>();
                foreach (ToggleButton toggleButton in labelUIElement.Lables)
                {
                    if (toggleButton.IsChecked == true)
                    {
                        tags.Add(toggleButton.Content.ToString());
                    }
                }
                if (tags.Count != 0)
                {
                    userSelectTags.AddTag(key, tags);
                }
            }
            Console.WriteLine("UserSelectTags:"+ userSelectTags.ToJsonString());
            return userSelectTags;
        }

        /// <summary>
        /// Build tags for ui display(used by proctect success page.).
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, List<string>> GetClassificationForUI()
        {
            Dictionary<string, List<string>> keyValuePairs = new Dictionary<string, List<string>>();
            foreach (KeyValuePair<string, LabelUIElement> kvp in ProjectClassifications)
            {
                string key = kvp.Key;
                LabelUIElement labelUIElement = kvp.Value;
                List<string> list = new List<string>();
                foreach (ToggleButton toggleButton in labelUIElement.Lables)
                {
                    if (toggleButton.IsChecked == true)
                    {
                        list.Add(toggleButton.Content.ToString());
                    }
                }
                if (list.Count != 0)
                {
                    keyValuePairs.Add(key, list);
                }
            }
            return keyValuePairs;
        }

        #region For modify rights, set defult select tags
        public void SetDefultSelectTags(Dictionary<string, List<string>> keyValues)
        {
            var tags = keyValues;
            //Check nonull for tags.
            //If there is nothing just return.
            if (tags == null || tags.Count == 0)
            {
                return;
            }
            //Get the iterator of the dictionary.
            var iterator = tags.GetEnumerator();
            int i = 0;
            //If there is any items inside it.
            while (iterator.MoveNext())
            {
                //Get the current one.
                var current = iterator.Current;

                var key = current.Key;
                var values = current.Value;

                DefultTgBtnChecked(key, values);
            }
        }
        public void AddInheritedTags(Dictionary<string, List<string>> keyValues)
        {
            foreach (var item in keyValues)
            {
                if (ProjectClassifications.ContainsKey(item.Key))
                {
                    continue;
                }

                LabelUIElement labelUIElement = new LabelUIElement
                {
                    IsMultiSelect = item.Value.Count > 1,
                    IsMandatory = false
                };

                //Create Classification textblock.
                TextBlock titleTB = CreateClassificationTB(item.Key);

                labelUIElement.Title = titleTB;
                //Attach classificaiton textblock to the wrap panel.
                WrapPanel.Children.Add(titleTB);

                List<ToggleButton> labelBTs = new List<ToggleButton>();

                labelUIElement.Lables = labelBTs;

                ProjectClassifications.Add(item.Key, labelUIElement);

                UInt16 checkedSize = 0;
                foreach (var kv in item.Value)
                {
                    ToggleButton label = new ToggleButton
                    {
                        Tag = item.Key,
                        Padding = new Thickness(15, 5, 15, 5)
                    };

                    labelBTs.Add(label);

                    checkedSize++;
                    // Inherited Tags should defult checked.
                    label.IsChecked = true;

                    //Bind toggle button checked event.
                    label.Checked += new RoutedEventHandler(ToggleButton_Checked);
                    //Bind toggle button unchecked event.
                    label.Unchecked += new RoutedEventHandler(ToggleButton_Unchecked);

                    label.Content = kv;
                    //Bind those label button to wrap panel.
                    WrapPanel.Children.Add(label);
                }

                //If this section is mandatory and no item selected by default.
                //then keep the mandatory run as hint style by set forground color as red.
                //if (item.isMandatory)
                //{
                //    AddMandatory(titleTB, checkedSize == 0);
                //}
            }
        }
        #endregion
        private void DefultTgBtnChecked(string key, List<string> values)
        {
            foreach (var value in values)
            {
                foreach (var item in WrapPanel.Children)
                {
                    if (item is ToggleButton)
                    {
                        ToggleButton button = item as ToggleButton;
                        if (button.Tag.ToString() == key)
                        {
                            if (button.Content.ToString() == value)
                            {
                                button.IsChecked = true;
                                break;
                            }
                        }
                    }
                }
            }
        }
        private void DisEnableTgBtn(string key)
        {
            foreach (var item in WrapPanel.Children)
            {
                if (item is ToggleButton)
                {
                    ToggleButton button = item as ToggleButton;
                    if (button.Tag.ToString() == key)
                    {
                        button.IsChecked = false;
                        //remove toggle button checked event.
                        button.Checked -= new RoutedEventHandler(ToggleButton_Checked);
                        //remove toggle button unchecked event.
                        button.Unchecked -= new RoutedEventHandler(ToggleButton_Unchecked);
                        button.Background = new SolidColorBrush(Colors.DarkGray);
                        button.IsEnabled = false;
                    }
                }
            }
        }

        #region For ShareNxlFile display tag
        public void DisplayTags(Dictionary<string, List<string>> keyValues)
        {
            var tags = keyValues;
            //Check nonull for tags.
            //If there is nothing just return.
            if (tags == null || tags.Count == 0)
            {
                return;
            }
            //Get the iterator of the dictionary.
            var iterator = tags.GetEnumerator();
            int i = 0;
            //If there is any items inside it.
            while (iterator.MoveNext())
            {
                //Get the current one.
                var current = iterator.Current;

                var key = current.Key;
                var values = current.Value;
                var tb = CreateDisplayTB(key, values);

                //Add each textbolck which contains classification and values of classification.
                WrapPanel.Children.Insert(i++, tb);
                //If new project tag.key the same as original file tag.key, should ban new project's tag.value
                //DisEnableTgBtn(key);

                DefultTgBtnChecked(key, values);
            }
        }
        private TextBlock CreateDisplayTB(string key, List<string> values)
        {
            TextBlock tb = new TextBlock
            {
                Width = 1000,
                Foreground = new SolidColorBrush(Colors.Black),
                Background = new SolidColorBrush(Colors.White),
                Padding = new Thickness(5, 5, 5, 5),
                TextAlignment = TextAlignment.Left,
                FontSize = 16,
                FontFamily = new FontFamily("Lato"),
                FontStyle = FontStyles.Normal,
                FontWeight = FontWeights.SemiBold,
            };

            //Add classification textblock.
            tb.Inlines.Add(CreateClassificationName(key));
            //Add values belong to classification textblok.
            tb.Inlines.Add(CreateValusTB(values));
            return tb;
        }

        private TextBlock CreateClassificationName(string text)
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

        public Dictionary<string, List<string>> GetProjectClassification()
        {
            Dictionary<string, List<string>> allTags = new Dictionary<string, List<string>>();
            foreach (KeyValuePair<string, LabelUIElement> kvp in ProjectClassifications)
            {
                string key = kvp.Key;
                LabelUIElement labelUIElement = kvp.Value;
                List<string> tags = new List<string>();
                foreach (ToggleButton toggleButton in labelUIElement.Lables)
                {
                    tags.Add(toggleButton.Content.ToString());
                }
                if (tags.Count != 0)
                {
                    allTags.Add(key, tags);
                }
            }
            return allTags;
        }
        #endregion
    }

    public class LabelUIElement
    {
        private TextBlock title;
        private List<ToggleButton> lables;
        private bool isMandatory = false;
        private bool isMultiSelect = false;

        public LabelUIElement()
        {

        }

        public TextBlock Title { get => title; set => title = value; }
        public List<ToggleButton> Lables { get => lables; set => lables = value; }
        public bool IsMandatory { get => isMandatory; set => isMandatory = value; }
        public bool IsMultiSelect { get => isMultiSelect; set => isMultiSelect = value; }
    }
}
