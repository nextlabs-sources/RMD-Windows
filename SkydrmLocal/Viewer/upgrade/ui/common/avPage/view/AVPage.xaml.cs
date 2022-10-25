using System;
using System.Collections.Generic;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Viewer.upgrade.file.basic.utils;
using Viewer.upgrade.ui.common.avPage.viewModel;
using Viewer.upgrade.utils;
using Viewer.upgrade.utils.overlay.utils;
using Viewer.upgrade.utils.overlay.windowOverlay;

namespace Viewer.upgrade.ui.common.avPage.view
{
    /// <summary>
    /// Interaction logic for AVPage.xaml
    /// </summary>
    public partial class AVPage : Page , ISensor
    {
        private Window mParentWindow;
        private bool mSuppressSeek;
        private DispatcherTimer DispatcherTimer = new System.Windows.Threading.DispatcherTimer();
        private bool mPlayFlag = true;
        private EnumFileType mEnumFileType;
        private WatermarkInfo mWatermarkInfo;
        private string mFilePath = string.Empty;
        private WindowOverlay mOverlay;
        public event Action<Exception> OnUnhandledExceptionOccurrence;
        public event Action OnLoadFileSucceed;

        public AVPage(string filePath)
        {
            InitializeComponent();
            mFilePath = filePath;
            string extention = Path.GetExtension(filePath);
            if (ToolKit.AUDIO_EXTENSIONS.Contains(extention))
            {
                mEnumFileType = EnumFileType.FILE_TYPE_AUDIO;
              
            }
            else if (ToolKit.VIDEO_EXTENSIONS.Contains(extention))
            {
                mEnumFileType = EnumFileType.FILE_TYPE_VIDEO;
            }
        }

        public ISensor Sensor
        {
            get { return this; }
        }

        public void Watermark(WatermarkInfo watermarkInfo)
        {
            mWatermarkInfo = watermarkInfo;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            mParentWindow = Window.GetWindow(this);
            mParentWindow.Closed += Window_Closed;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            // Cleanup 
            DispatcherTimer.Stop();
            //try
            //{
            //    File.Delete(this.NxlConverterResult.TmpPath + ".nxl");
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex.ToString());
            //}
        }

        private void TimelineMDown(object sender, MouseButtonEventArgs e)
        {
            DispatcherTimer.Stop();
            QSMovie.Pause();

            QSMovie.Position = new TimeSpan(0, 0, 0, 0, (int)timelineSlider.Value);
        }

        private string SecToTime(int sec)
        {
            int min = sec / 60;
            sec = sec - min * 60;
            int hour = min / 60;
            min = min - hour * 60;
            string h = hour.ToString();
            string mm = ((min < 10) ? "0" : "") + min.ToString();
            string ss = ((sec < 10) ? "0" : "") + sec.ToString();
            string time = h + ":" + mm + ":" + ss;
            return time;
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            int time = (int)QSMovie.Position.TotalSeconds;
            mSuppressSeek = true;
            timelineSlider.ToolTip = SecToTime(time);
            timelineSlider.Value = QSMovie.Position.TotalMilliseconds;
            mSuppressSeek = false;
        }

        private void TimelineMUp(object sender, MouseButtonEventArgs e)
        {
            if (mPlayFlag == true)
            {
                QSMovie.Position = new TimeSpan(0, 0, 0, 0, (int)timelineSlider.Value);
                DispatcherTimer.Start();
                QSMovie.Pause();
            }
            else
            {
                QSMovie.Position = new TimeSpan(0, 0, 0, 0, (int)timelineSlider.Value);
                DispatcherTimer.Start();
                QSMovie.Play();
            }

        }

        private void MainGridMouseEnter(object sender, MouseEventArgs e)
        {
            if (mEnumFileType == EnumFileType.FILE_TYPE_VIDEO)
            {
                ControlPanel.Visibility = Visibility.Visible;
            }
        }

        private void MainGridMouseLeave(object sender, MouseEventArgs e)
        {
            if (mEnumFileType == EnumFileType.FILE_TYPE_VIDEO)
            {
                ControlPanel.Visibility = Visibility.Hidden;
            }
        }

        private void Element_MediaOpened(object sender, RoutedEventArgs e)
        {
            timelineSlider.Maximum = QSMovie.NaturalDuration.TimeSpan.TotalMilliseconds;
            DispatcherTimer.Tick += new EventHandler(DispatcherTimer_Tick);
            DispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 200);
            DispatcherTimer.Start();
        }

        private void QSMovie_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (EnumFileType.FILE_TYPE_AUDIO == mEnumFileType)
                {
                    Mp3Panel.Visibility = Visibility.Visible;
                }
                else if (EnumFileType.FILE_TYPE_VIDEO == mEnumFileType)
                {
                    Mp3Panel.Visibility = Visibility.Hidden;
                }

                QSMovie.Source = new Uri(mFilePath);
                QSMovie.Play();

                mPlayFlag = false;
                if (IsAttachWatermark())
                {
                    AttachWatermark();
                }

                OnLoadFileSucceed?.Invoke();
            }
            catch (Exception ex)
            {
                OnUnhandledExceptionOccurrence?.Invoke(ex);
            }
        }

        private void QSMovie_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            PlayOrPause();
        }

        private void PlayOrPauseButton_Click(object sender, RoutedEventArgs e)
        {
            PlayOrPause();
        }

        private void PlayOrPause()
        {
            if (mPlayFlag == true)
            {
                QSMovie.Play();
                PlayOrPauseButton.Tag = "/resources/icons/IconAvPlay.png";
            }
            if (mPlayFlag == false)
            {
                QSMovie.Pause();
                PlayOrPauseButton.Tag = "/resources/icons/IconAvPause.png";
            }
            mPlayFlag = !mPlayFlag;

        }

        private void TimelineSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!mSuppressSeek)
            {
                QSMovie.Position = new TimeSpan(0, 0, 0, 0, (int)timelineSlider.Value);
            }
        }

        private void QSMovie_MediaEnded(object sender, RoutedEventArgs e)
        {
            QSMovie.Pause();
            QSMovie.Position = new TimeSpan(0, 0, 0, 0, 0);
            PlayOrPauseButton.Tag = "/resources/icons/IconAvPause.png";
            mPlayFlag = true;
        }


        public void AttachWatermark()
        {
            try
            {
                mOverlay = new WindowOverlay();
                Canvas overlayCanvas = new Canvas();
                OverlayUtils.DrawWatermark(mWatermarkInfo, ref overlayCanvas);
                mOverlay.OverlayContent = overlayCanvas;
                MainGrid.Children.Add(mOverlay);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public bool IsAttachWatermark()
        {
            bool result = false;
            if (null != mWatermarkInfo)
            {
                if (!string.IsNullOrEmpty(mWatermarkInfo.Text))
                {
                    result = true;
                }
            }
            else
            {
                result = false;
            }
            return result;
        }

    }
}
