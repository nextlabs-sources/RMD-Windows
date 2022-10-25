using SkydrmLocal.Pages;
using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.ui.components.ValiditySpecify.model;
using SkydrmLocal.rmc.ui.pages.model;
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
    /// Interaction logic for PageRightsSelect.xaml
    /// </summary>
    public partial class FileRightsSelect : UserControl
    {
        FileRightsSelectDataMode dataMode;

        PageSelectDigitalRights pageAdhoc;
        PageSelectDocumentClassify pageCentral;

        public FileRightsSelect()
        {
            InitializeComponent();

            dataMode = new FileRightsSelectDataMode();
            this.DataContext = dataMode;

            pageAdhoc = new PageSelectDigitalRights();
            dataMode.RightsSelectConfig = pageAdhoc.RightsSelectConfig;
            pageAdhoc.ValidityDateChanged += PageAdhoc_ValidityDateChanged;
            this.fm_Adhoc.Content = pageAdhoc;

            pageCentral = new PageSelectDocumentClassify();
            this.fm_Central.Content = pageCentral;
        }

        #region Event callbacks
        public event MouseButtonEventHandler ChangeDestClicked;
        public event RoutedEventHandler OkBtnClicked;
        public event RoutedEventHandler CancelBtnClicked;
        public event RoutedEventHandler RadioCheckChanged;
        // Fix bug 53338
        public delegate void ChangeValidity(bool isValidDate);
        public event ChangeValidity ValidityDateChanged;
        #endregion

        #region Attribute Section
        public Visibility DescAndRbVisible { get => dataMode.DescAndRadioVisible; set => dataMode.DescAndRadioVisible = value; }

        public string Path { get => dataMode.Path; set => dataMode.Path = value; }

        public Visibility ChangDestVisible { get => dataMode.ChangDestVisible; set => dataMode.ChangDestVisible = value; }

        public string ProtectBtnContent { get => dataMode.ProtectBtnContent; set => dataMode.ProtectBtnContent = value; }

        public RightsSelectConfig AdHocRights { get => dataMode.RightsSelectConfig; }

        #endregion

        #region Setters&Getters
        public void SetAdhocRadio(bool isChecked, bool isEnable = true)
        {
            rb_Adhoc.IsChecked = isChecked;
            rb_Adhoc.IsEnabled = isEnable;
        }

        public void SetCentralPlcRadio(bool isChecked, bool isEnable = true)
        {
            rb_Central.IsChecked = isChecked;
            rb_Central.IsEnabled = isEnable;
        }

        /// <summary>
        /// Set AdHocPage Share right isEnable. Now this method is invalid.
        /// </summary>
        /// <param name="isEnable"></param>
        /// <param name="reInitialize">reagain init PageSelectDigitalRights</param>
        public void ProcessAdHocPage(bool isEnable, bool reInitialize)
        {
            // fix bug 53944, Now project can set share right.
            return;

            if (reInitialize)
            {
                pageAdhoc = new PageSelectDigitalRights();
                dataMode.RightsSelectConfig = pageAdhoc.RightsSelectConfig;
                pageAdhoc.ValidityDateChanged += PageAdhoc_ValidityDateChanged;
                this.fm_Adhoc.Content = pageAdhoc;
            }
            pageAdhoc.SetShareIsEnable(isEnable);
        }

        /// <summary>
        /// Set CentralPolicyPage Tags by MyProject
        /// </summary>
        /// <param name="project">MyProject</param>
        /// <param name="reInitialize">regain init PageSelectDocumentClassify</param>
        public void ProcessCentralPage(IMyProject project, bool reInitialize)
        {
            if (project == null)
            {
                //throw new Exception("Parameter IMyProject in ProcessCentralPage must be set correctly before ProcessCentralPage invoked. ");
                return;
            }
            if (reInitialize)
            {
                pageCentral = new PageSelectDocumentClassify();
                this.fm_Central.Content = pageCentral;
            }
            pageCentral.SetProject(project);
        }

        /// <summary>
        /// Set CentralPolicyPage project by SystemProject
        /// </summary>
        /// <param name="sProject">SystemProject</param>
        /// <param name="reInitialize">regain init PageSelectDocumentClassify</param>
        public void ProcessCentralPage(ISystemProject sProject, bool reInitialize)
        {
            if (sProject == null)
            {
                //throw new Exception("Parameter IMyProject in ProcessCentralPage must be set correctly before ProcessCentralPage invoked. ");
                return;
            }
            if (reInitialize)
            {
                pageCentral = new PageSelectDocumentClassify();
                this.fm_Central.Content = pageCentral;
            }
            pageCentral.SetSystemProject(sProject);
        }

        public List<string> GetIncorrectSelectedTags()
        {
            return pageCentral?.IsCorrectChooseClassification();
        }

        public UserSelectTags GetSelectedTags()
        {
            return pageCentral?.GetClassification();
        }

        public Dictionary<string, List<string>> GetSelectedTagsForUI()
        {
            return pageCentral?.GetClassificationForUI();
        }

        #region For Share Nxl file add new setters
        /// <summary>
        /// Set adhoc page change waterMark button Visibility
        /// </summary>
        /// <param name="visibility"></param>
        public void ProcessAdHocPageWaterMarkBtn(Visibility visibility)
        {
            pageAdhoc?.SetChangeWaterMarkIsVisibilty(visibility);
        }
        /// <summary>
        /// Set adhoc page change validity button Visibility
        /// </summary>
        /// <param name="visibility"></param>
        public void ProcessAdHocPageValidityBtn(Visibility visibility)
        {
            pageAdhoc?.SetChangeValidityIsVisibilty(visibility);
        }
        /// <summary>
        /// Display original Adhoc rights
        /// </summary>
        /// <param name="rights"></param>
        public void ProcessAdHocPageRights_Cb(List<string> rights)
        {
            pageAdhoc?.SetCheckedRights(rights);
        }
        /// <summary>
        /// Display original watermark
        /// </summary>
        /// <param name="waterMark"></param>
        public void ProcessAdHocPageWaterMark(string waterMark)
        {
            pageAdhoc?.SetWaterMark(waterMark);
        }
        /// <summary>
        /// Display original expire
        /// </summary>
        /// <param name="Expiry"></param>
        /// <param name="expireDateValue"></param>
        public void ProcessAdHocPageExpire(IExpiry Expiry, string expireDateValue)
        {
            pageAdhoc?.SetExpire(Expiry, expireDateValue);
        }
        /// <summary>
        /// Set AdhocPage isEnable
        /// </summary>
        /// <param name="disEnable"></param>
        public void ProcessAdHocPageIsEnable(bool disEnable)
        {
            pageAdhoc?.SetIsEnable(disEnable);
        }
        /// <summary>
        /// Display original Tags string in top of Central page. 
        /// </summary>
        /// <param name="keyValues"></param>
        public void ProcessCentralPageOriginTag(Dictionary<string, List<string>> keyValues)
        {
            pageCentral?.DisplayTags(keyValues);
        }
        /// <summary>
        /// Get project all tags
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, List<string>> GetProjectClassification()
        {
            return pageCentral?.GetProjectClassification();
        }
        #endregion

        #region For ‘add file to project’ or ‘modify rights’ add new setters
        /// <summary>
        /// Display the Inherited Tags on the CentralPolicyPage. if the key existed, will not add.
        /// </summary>
        /// <param name="keyValues"></param>
        public void ProcessCentralPageAddInheritedTag(Dictionary<string, List<string>> keyValues)
        {
            pageCentral?.AddInheritedTags(keyValues);
        }
        /// <summary>
        /// Set defult selected tag by passing parameters
        /// </summary>
        /// <param name="keyValues"></param>
        public void ProcessCentralPageDefultSelectTag(Dictionary<string, List<string>> keyValues)
        {
            pageCentral?.SetDefultSelectTags(keyValues);
        }
        #endregion

        #endregion

        #region Events
        private void On_Adhoc_RadioChecked(object sender, RoutedEventArgs e)
        {
            fm_Adhoc.Visibility = Visibility.Visible;
            fm_Central.Visibility = Visibility.Hidden;
            RadioCheckChanged?.Invoke(sender, e);
        }

        private void On_Central_RadioChecked(object sender, RoutedEventArgs e)
        {
            fm_Central.Visibility = Visibility.Visible;
            fm_Adhoc.Visibility = Visibility.Hidden;
            RadioCheckChanged?.Invoke(sender, e);
        }

        private void On_ProtectOrShare_Btn(object sender, RoutedEventArgs e)
        {
            OkBtnClicked?.Invoke(sender, e);
        }

        private void On_Cacle_Btn(object sender, RoutedEventArgs e)
        {
            CancelBtnClicked?.Invoke(sender, e);
        }

        private void ChangeDest_MouseLeftBtn(object sender, MouseButtonEventArgs e)
        {
            ChangeDestClicked?.Invoke(sender, e);
        }

        private void PageAdhoc_ValidityDateChanged(bool isValidDate)
        {
            // If ValidityBtnChanged event triggered, the expire date is valid.
            ValidityDateChanged?.Invoke(isValidDate);
        }
        #endregion

    }

    /// <summary>
    /// DataMode for PageRightsSelect.xaml
    /// </summary>
    public class FileRightsSelectDataMode : INotifyPropertyChanged
    {
        Visibility descAndRadioVisible;
        string mPath;
        Visibility changDestVisible;
        string mBtnContent;
        
        public FileRightsSelectDataMode()
        {
        }

        public RightsSelectConfig RightsSelectConfig { get; set; }

        public Visibility DescAndRadioVisible { get => descAndRadioVisible; set { descAndRadioVisible = value; OnBindUIPropertyChanged("DescAndRadioVisible"); } }

        public string Path { get => mPath; set { mPath = value; OnBindUIPropertyChanged("Path"); } }

        public Visibility ChangDestVisible { get => changDestVisible; set { changDestVisible = value; OnBindUIPropertyChanged("ChangDestVisible"); } }

        public string ProtectBtnContent { get => mBtnContent; set { mBtnContent = value; OnBindUIPropertyChanged("ProtectBtnContent"); } }


        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnBindUIPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }

}
