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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SkydrmLocal.Pages;
using System.Timers;
using System.ComponentModel;

namespace SkydrmLocal
{
    /// <summary>
    /// Interaction logic for ViewPager.xaml
    /// </summary>
    public partial class ViewPager : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private const UInt16 AMOUNT = 4;
       // private const string strImageBackDisabled = "/SkydrmLocal;component/rmc/resources/icons/icon_back_arrow_gray.png";
        private const string strImageBackDisabled = @"/rmc/resources/icons/icon_back_arrow_gray.png";
        //private const string strImageBackEnabled = "/SkydrmLocal;component/rmc/resources/icons/icon_back_arrow_black.png";
        private const string strImageBackEnabled = @"/rmc/resources/icons/icon_back_arrow_black.png";

       // private const string strImageForwarDisabled = "/SkydrmLocal;component/rmc/resources/icons/icon_forward_arrow_gray.png";
        private const string strImageForwarDisabled = @"/rmc/resources/icons/icon_forward_arrow_gray.png";

       // private const string strImageForwardEnabled = "/SkydrmLocal;component/rmc/resources/icons/icon_forward_arrow_black.png";
        private const string strImageForwardEnabled = @"/rmc/resources/icons/icon_forward_arrow_black.png";

       // private const string strImageIntroCircle = "/SkydrmLocal;component/rmc/resources/icons/icon_intro_circle.png";
        private const string strImageIntroCircle = @"/rmc/resources/icons/icon_intro_circle.png";

       // private const string strImageIntroCircleActived = "/SkydrmLocal;component/rmc/resources/icons/icon_intro_circle_actived.png";
        private const string strImageIntroCircleActived = @"/rmc/resources/icons/icon_intro_circle_actived.png";

        private string strImageBackState = strImageBackDisabled;
        private string strImageForwardState = strImageForwardEnabled;

        private Page[] _subpages = new Page[AMOUNT];
        //private Image[] _circleImages = new Image[AMOUNT];
        private Timer timer = new Timer(5000);
        private UInt16 _index;

        //for button enable or disenable
        //Back button <-
        private bool ImageBackEnabled = true;
        public bool IsImageBackEnabled
        {
            get { return ImageBackEnabled; }
            set
            {
                ImageBackEnabled = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsImageBackEnabled"));
            }
        }
        //Forward button ->
        private bool ImageForwardEnabled = true;
        public bool IsImageForwardEnabled
        {
            get { return ImageForwardEnabled; }
            set
            {
                ImageForwardEnabled = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsImageForwardEnabled"));
            }
        }

        public ViewPager()
        {
            InitializeComponent();
            InitializeTaskTimer();
            SetSubPages();
            Navigate(_subpages[_index]);
        }

        private void InitializeTaskTimer()
        {
            timer.Elapsed += new ElapsedEventHandler(TimeOut);
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        public void Navigate(Page nextpage)
        {
            this.pageContainer.Content = nextpage;
        }

        public void Navigate(Page nextpage, object state)
        {
            this.pageContainer.Content = nextpage;
            ISwitchable s = nextpage as ISwitchable;
            if (s != null)
            {
                s.Utilize(state);
            }
            else
            {
                throw new ArgumentException("Nextpage is not Iswitchable!" + nextpage.Name.ToString());
            }
        }

        private void SetSubPages()
        {
            _subpages[0] = new PageWelcome1();
            _subpages[1] = new PageWelcome2();
            _subpages[2] = new PageWelcome3();
            _subpages[3] = new PageWelcome4();
            InitIntroCircleDots();
        }

        private void ImageBack_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_index > 0)
            {
                _index--;
                Navigate(_subpages[_index]);
                InitIntroCircleDots();
                MoveAnimation(_index);
                if (_index == 2)
                {
                    strImageForwardState = strImageForwardEnabled;
                    UpdateImageForwardStatus(strImageForwardState);
                }
                if (_index == 0)
                {
                    strImageBackState = strImageBackDisabled;
                    UpdateImageBackStatus(strImageBackState);
                }
            }
        }

        private void ImageForward_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_index < 3)
            {
                _index++;
                Navigate(_subpages[_index]);
                InitIntroCircleDots();
                // MoveAnimation(_index);
                if (_index == 1)
                {
                    strImageBackState = strImageBackEnabled;
                    UpdateImageBackStatus(strImageBackState);
                }
                if (_index == 3)
                {
                    strImageForwardState = strImageForwarDisabled;
                    UpdateImageForwardStatus(strImageForwardState);
                }
            }
        }

        private void UpdateImageBackStatus(string imagePath)
        {
            //BitmapImage imageBack = new BitmapImage(new Uri(imagePath, UriKind.Relative));
            //this.ImageBack.Source = imageBack;
            this.ImageBack.Content = imagePath;
            if (imagePath== strImageBackDisabled)
            {
                IsImageBackEnabled = false;
            }
            else
            {
                IsImageBackEnabled = true;
            }
        }

        private void UpdateImageForwardStatus(string imagePath)
        {
            //BitmapImage imageForward = new BitmapImage(new Uri(imagePath, UriKind.Relative));
            //this.ImageForward.Source = imageForward;
            this.ImageForward.Content = imagePath;
            if (imagePath == strImageForwarDisabled)
            {
                IsImageForwardEnabled = false;
            }
            else
            {
                IsImageForwardEnabled = true;
            }
        }

        private void InitIntroCircleDots()
        {
            this.buttonStack.Children.Clear();
            for (int i = 0; i < AMOUNT; i++)
            {
                Image imageCircle = new Image();
                Thickness thickness;
                string tmpUri;
                if (i == AMOUNT - 1)
                {
                    thickness = new Thickness(24, 0, 0, 0);
                }
                else
                {
                    thickness = new Thickness(24, 0, 0, 0);
                }
                if (i == _index)
                {
                    imageCircle.Width = 25;
                    imageCircle.Height = 25;
                    tmpUri = strImageIntroCircleActived;
                }
                else
                {
                    imageCircle.Width = 8;
                    imageCircle.Height = 8;
                    tmpUri = strImageIntroCircle;
                }
                BitmapImage bitmap = new BitmapImage(new Uri(tmpUri, UriKind.Relative));
                imageCircle.Margin = thickness;
                imageCircle.Source = bitmap;
                this.buttonStack.Children.Add(imageCircle);
            }
        }

        private void MoveAnimation(UInt16 index)
        {
            DoubleAnimation da = new DoubleAnimation
            {
                Duration = new Duration(TimeSpan.FromMilliseconds(200)),
                DecelerationRatio = 0.2,
                AccelerationRatio = 0.2,
                From = 1.0,
                //To = -(index * _width)
                To = 0.0
            };
            this.pageContainer.BeginAnimation(Canvas.LeftProperty, da);
        }

        public void TimeOut(object source, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                this.Dispatcher.Invoke(() =>
                {
                    if (_index >= 0 && _index < 3)
                    {
                        _index++;
                        Navigate(_subpages[_index]);
                        InitIntroCircleDots();
                        if (_index == 1)
                        {
                            strImageBackState = strImageBackEnabled;
                            UpdateImageBackStatus(strImageBackState);
                        }
                        if (_index == 3)
                        {
                            strImageForwardState = strImageForwarDisabled;
                            UpdateImageForwardStatus(strImageForwardState);
                        }
                    }
                    else if (_index == 3)
                    {
                        _index = 0;
                        //_index++;
                        Navigate(_subpages[_index]);
                        InitIntroCircleDots();
                        //reset ui display status.
                        //disable image back status.
                        strImageBackState = strImageBackDisabled;
                        UpdateImageBackStatus(strImageBackState);
                        //enable image forward status.
                        strImageForwardState = strImageForwardEnabled;
                        UpdateImageForwardStatus(strImageForwardState);
                    }
                });
            }
            catch (Exception)
            {
               
            }
            
        }

        private void ImageBack_Click(object sender, RoutedEventArgs e)
        {
            if (_index > 0)
            {
                _index--;
                Navigate(_subpages[_index]);
                InitIntroCircleDots();
                MoveAnimation(_index);
                if (_index == 2)
                {
                    strImageForwardState = strImageForwardEnabled;
                    UpdateImageForwardStatus(strImageForwardState);
                }
                if (_index == 0)
                {
                    strImageBackState = strImageBackDisabled;
                    UpdateImageBackStatus(strImageBackState);
                }
            }
        }

        private void ImageForward_Click(object sender, RoutedEventArgs e)
        {
            if (_index < 3)
            {
                _index++;
                Navigate(_subpages[_index]);
                InitIntroCircleDots();
                // MoveAnimation(_index);
                if (_index == 1)
                {
                    strImageBackState = strImageBackEnabled;
                    UpdateImageBackStatus(strImageBackState);
                }
                if (_index == 3)
                {
                    strImageForwardState = strImageForwarDisabled;
                    UpdateImageForwardStatus(strImageForwardState);
                }
            }
        }
    }

    public interface ISwitchable
    {
        void Utilize(object state);
    }

    public static class Switcher
    {
        public static ViewPager viewPager;

        public static void Switch(Page newpage)
        {
            viewPager.Navigate(newpage);
        }

        public static void Switch(Page newpage, object state)
        {
            viewPager.Navigate(newpage, state);
        }
    }
}
