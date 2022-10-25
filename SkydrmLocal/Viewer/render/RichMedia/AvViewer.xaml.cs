using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

using System.Windows.Threading;
using Viewer.overlay;
using Viewer.utils;
using Viewer.viewer;

namespace Viewer.render.av.AvViewer
{
    /// <summary>
    /// Interaction logic for AvViewer.xaml
    /// </summary>
    public partial class AvViewer : Page,IOverlay
    {
        // view window
        private ViewerWindow ViewerWin { get; set; }
        private bool SuppressSeek { get; set; }
        private DispatcherTimer DispatcherTimer = new System.Windows.Threading.DispatcherTimer(); 
        private bool play_flag = true;
        private EnumFileType EnumFileType { get; set; }
        private Overlay Overlay = new Overlay();
        private WatermarkInfo WatermarkInfo { get; set; }
        private string mFilePath = string.Empty;

        public AvViewer(ViewerWindow window, 
                        string filePath,
                        EnumFileType enumFileType, 
                        WatermarkInfo watermarkInfo,
                        log4net.ILog log)
        {
            log.Info("\t\t AvViewer \r\n");
            InitializeComponent();
            this.mFilePath = filePath;
            this.WatermarkInfo = watermarkInfo;

            // initialize view window
            ViewerWin = window;
            ViewerWin.Closed += ViewerWin_Closed;      
        

            switch (enumFileType)
            {
                case EnumFileType.FILE_TYPE_AUDIO:
                    this.EnumFileType = EnumFileType.FILE_TYPE_AUDIO;
                    Mp3Panel.Visibility = Visibility.Visible;
                    break;

                case EnumFileType.FILE_TYPE_VIDEO:
                    this.EnumFileType = EnumFileType.FILE_TYPE_VIDEO;
                    Mp3Panel.Visibility = Visibility.Hidden;
                    break;
            }
        }

        private void ViewerWin_Closed(object sender, EventArgs e)
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
            SuppressSeek = true;
            timelineSlider.ToolTip = SecToTime(time);
            timelineSlider.Value = QSMovie.Position.TotalMilliseconds;
            SuppressSeek = false;
        }

        private void TimelineMUp(object sender, MouseButtonEventArgs e)
        {
            if (play_flag==true)
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
            if (EnumFileType== EnumFileType.FILE_TYPE_VIDEO) {
                ControlPanel.Visibility = Visibility.Visible;
            }
        }

        private void MainGridMouseLeave(object sender, MouseEventArgs e)
        {
            if (EnumFileType == EnumFileType.FILE_TYPE_VIDEO)
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
            QSMovie.Source = new Uri(mFilePath);       
            QSMovie.Play();
          
            play_flag = false;
            AttachOverlay();
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
            if (play_flag == true)
            {
                QSMovie.Play();
                PlayOrPauseButton.Tag = "/resources/icons/IconAvPlay.png";
            }
            if (play_flag == false)
            {
                QSMovie.Pause();
                PlayOrPauseButton.Tag = "/resources/icons/IconAvPause.png";
            }
            play_flag = !play_flag;

        }

       private void timelineSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!SuppressSeek)
            {
                QSMovie.Position = new TimeSpan(0, 0, 0, 0, (int)timelineSlider.Value);
            }
         }

        private void QSMovie_MediaEnded(object sender, RoutedEventArgs e)
        {
            QSMovie.Pause();
            QSMovie.Position = new TimeSpan(0, 0, 0, 0, 0);
            PlayOrPauseButton.Tag = "/resources/icons/IconAvPause.png";
            play_flag = true;
        }

        private void AttachOverlay()
        {
            if (IsAttach())
            {
                Overlay.ParentLayer.Add((Adorner)Attach(MainGrid.ActualWidth, MainGrid.ActualHeight));
            }
        }

        public bool IsAttach()
        {
            return RenderHelper.IsAttachOverlay(WatermarkInfo);
        }

        public UIElement Attach(double width, double height)
        {
            return Overlay.CreateOverlayInAdornerLayer(width, height);
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (IsAttach())
            {
                if (!Overlay.Initialze)
                {
                    InitOverlay();
                }
            }
        }
        private void InitOverlay()
        {
            Overlay.Initialize(MainGrid, WatermarkInfo);
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (IsAttach())
            {
                if (Overlay.Initialze)
                {
                    Overlay.OnOverlayChange();
                }
                else
                {
                    InitOverlay();
                }
            }
        }
    }
}
