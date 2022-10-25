using System;

using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

using SAP.VE.DVL;
using SAP.VE.DVL.Interop;
using Viewer.overlay;
using Viewer.render.preview;

namespace Viewer.render.sap3dviewer
{
    /// <summary>
    /// Interaction logic for SapThreeDViewer.xaml
    /// </summary>
    public partial class VdsViewer : Page 
    {
        private ICore _dvlCore;
        private IRenderer _dvlRenderer;
        private IOpenGLContext _opengl;
        private IScene _scene;
        private bool _isCGM;
        private System.Windows.Threading.DispatcherTimer dispatcherTimer;
        private float ox, oy;
        private int _mx, _my;
        private bool mouseMoved, mouseDown;
        private System.Windows.Forms.Panel panel;
        private string _fileName;
        private WindowOverlay overlay;
        private Canvas overlayCanvas;
        private DVLStepNameControl.DVLStepNameControl DVLStepName;
      //  private DVLNodeMetaControl.DVLNodeMetaControl DVLNodeMeta;
        private Image imgTextOn, imgTextOff;
        private Image imgHotspotOn, imgHotspotOff;
        ulong _selectedNode;
        bool _isolated;
        string mFilePath;

        private bool showPopups = true;
        private bool showStepName = true;
        private bool showTree = true;
        private bool showSettings = false;
        private bool showSteps = true;
        private bool showHotspots = false;
        private bool playPause = false;
        private ViewerWindow ViewerWin;

        private WatermarkInfo WatermarkInfo;
        private double DeviceIndependentUnit = Convert.ToDouble(1) / Convert.ToDouble(96);
        private float DpiX = 96;
        private float DpiY = 96;
        private double DisplayMonitorX = SystemParameters.WorkArea.Width;
        private double DisplayMonitorY = SystemParameters.WorkArea.Height;
        private log4net.ILog mLog;

        public VdsViewer(ViewerWindow window, string filePath, WatermarkInfo watermarkInfo,log4net.ILog log)
        {
            InitializeComponent();
            
            this.mFilePath = filePath;
            this.ViewerWin = window;           
            this.ViewerWin.Closed += ViewerWin_Closed;
            this.WatermarkInfo = watermarkInfo;
            this.mLog = log;

            showTree = false;
            showSettings = false;

            UpdateViewerLayout();

            DVLSceneTree.NodeSelected += OnSceneTreeNodeSelected;
            lstProcedures.SelectionChanged += ProcedureChanged;
            DVLSettings.ItemChanged += DVLSettingsControlItemChanged;

            imgTextOn = new Image();
            imgTextOn.Source = new BitmapImage(new Uri("/resources/icons/sap_text.png", UriKind.Relative));

            imgTextOff = new Image();
            imgTextOff.Source = new BitmapImage(new Uri("/resources/icons/sap_text_off.png", UriKind.Relative));

            imgHotspotOn = new Image();
            imgHotspotOn.Source = new BitmapImage(new Uri("/resources/icons/sap_hotspot.png", UriKind.Relative));

            imgHotspotOff = new Image();
            imgHotspotOff.Source = new BitmapImage(new Uri("/resources/icons/sap_hotspot_off.png", UriKind.Relative));
        }

        private void ViewerWin_Closed(object sender, EventArgs e)
        {
            dispatcherTimer.Stop();
            if (_dvlRenderer != null)
            {
                _dvlCore.DoneRenderer();
                _dvlRenderer.Dispose();
                _dvlCore.Dispose();
                _opengl.Dispose();
            }
        }
        
        private void LoadFile(string filePath)
        {
            var fname = Path.GetFileName(filePath); 
            var pos = fname.LastIndexOf("\\");
            if (pos >= 0)
            {
                fname = fname.Substring(pos + 1);
            }
            _fileName = fname;
            lblFileName.Content = fname;
            DVLSceneTree.Title = fname;
            LoadVDS(filePath);
        }

        private void InitGLContext()
        {
            var host = new WindowsFormsHost();
            panel = new System.Windows.Forms.Panel();
            panel.Width = (int)veviewer.RenderSize.Width;
            panel.Height = (int)veviewer.RenderSize.Height;
            panel.Paint += new PaintEventHandler(onViewPaint);
            host.Child = panel;
            veviewer.Children.Add(host);

            panel.MouseDown += new System.Windows.Forms.MouseEventHandler(Panel_MouseDown);
            panel.MouseMove += new System.Windows.Forms.MouseEventHandler(Panel_MouseMove);
            panel.MouseUp += new System.Windows.Forms.MouseEventHandler(Panel_MouseUp);
            panel.Move += new System.EventHandler(Panel_Move);
            veviewer.SizeChanged += veviewer_SizeChanged;

            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 8);

            _opengl = new OpenGLContext();
            _opengl.Init(panel.Handle, 32);

            InitOverlayLayer();
           
            _dvlCore = new CoreWrapper();
            var res = _dvlCore.Init(new DVLClientImpl(this));
            if (res != Result.OK)
            {
                //System.Windows.MessageBox.Show("Error initializing DVLCore! Error code: " + res);
                ViewerWin.HandlerException(StatusOfView.SYSTEM_INTERNAL_ERROR, "Error initializing DVLCore! Error code: " + res);
                return;
            }

            res = _dvlCore.InitRenderer();
            if (res != Result.OK)
            {
                //System.Windows.MessageBox.Show("Error initializing DVLRenderer! Error code: " + res);
                ViewerWin.HandlerException(StatusOfView.SYSTEM_INTERNAL_ERROR, "Error initializing DVLRenderer! Error code: " + res);
                return;
            }

            _dvlRenderer = _dvlCore.Renderer;
            _dvlRenderer.SetDimensions((uint)panel.Width, (uint)panel.Height);
            _dvlRenderer.SetBackgroundColor(0.2f, 0.2f, 0.2f, 0.8f, 0.8f, 0.8f);

            dispatcherTimer.Start();
        }

        public void Refresh()
        {
            if (_opengl != null)
            {
                _opengl.SwapBuffers(); 
            }
        }

        private void onViewPaint(object sender, PaintEventArgs e)
        {
            Refresh();
            Refresh();
        }

        private void InitOverlayLayer()
        {
            overlay = new WindowOverlay();         
            overlayCanvas = new Canvas();
            overlayCanvas.Margin = new Thickness(0, 40, 0, 0);
            overlay.OverlayContent = overlayCanvas;                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             
            grid1.Children.Add(overlay);
            Grid.SetRow(overlay, 0);
            Grid.SetRowSpan(overlay,2);

            DVLStepName = new DVLStepNameControl.DVLStepNameControl();
            overlayCanvas.Children.Add(DVLStepName);
            Canvas.SetLeft(DVLStepName, (showSettings || showTree) ? DVLSceneTree.Width + 10 : 10);
            Canvas.SetTop(DVLStepName, 0);
            DVLStepName.Visibility = Visibility.Hidden;

            DVLStepName.MouseDown += stepName_MouseDown;
            DVLStepName.MouseMove += stepName_MouseMove;
            DVLStepName.MouseUp += stepName_MouseUp;

            //DVLNodeMeta = new DVLNodeMetaControl.DVLNodeMetaControl();
            //overlayCanvas.Children.Add(DVLNodeMeta);
            //Canvas.SetLeft(DVLNodeMeta, 340);
            //Canvas.SetTop(DVLNodeMeta, 100);

            //DVLNodeMeta.ClearActions();
            //DVLNodeMeta.AddAction("act1", "Add Component");
            //DVLNodeMeta.AddAction("act2", "Add Notification");
            //DVLNodeMeta.ActionSelected += CustomItemClicked;
            //DVLNodeMeta.NodeFlagChanged += NodeFlagChanged;

            if (RenderHelper.IsAttachOverlay(WatermarkInfo))
            {
                AttachWatermark(overlayCanvas);
            }
        }

        #region Overlay
        public void AttachWatermark(Canvas canvas)
        {
            mLog.Info("\t\t AttachWatermark \r\n");
            try
            {
                using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromHwnd(IntPtr.Zero))
                {
                    DpiX = graphics.DpiX;
                    DpiY = graphics.DpiY;
                }

                int DpiDisplayMonitorX = Convert.ToInt32(Math.Round(DisplayMonitorX * (DeviceIndependentUnit * DpiX)));
                int DpiDisplayMonitorY = Convert.ToInt32(Math.Round(DisplayMonitorY * (DeviceIndependentUnit * DpiY)));

                //This operation is used to get TextBlock's width and height.
                TextBlock tmp = CreateOverlayText(WatermarkInfo);
                tmp.Measure(new System.Windows.Size(Double.PositiveInfinity, Double.PositiveInfinity));
                tmp.Arrange(new Rect(tmp.DesiredSize));

                double startX = 0;
                double startY = 0;
                double hypotenuse = tmp.ActualWidth == 0 ? DisplayMonitorX : tmp.ActualWidth;
                double height = tmp.ActualHeight == 0 ? DisplayMonitorY : tmp.ActualHeight;
                int width = (int)(hypotenuse / Math.Sqrt(2));
                //Create Overlay text according to parent's width and height.
                TextBlock element;
                for (double y = startY; y < DpiDisplayMonitorY; y = y + width + height)
                {
                    for (double x = startX; x <= DpiDisplayMonitorX; x = x + hypotenuse)
                    {
                        element = CreateOverlayText(WatermarkInfo);
                        Canvas.SetLeft(element, x);
                        Canvas.SetTop(element, y);
                        canvas.Children.Add(element);
                    }
                }
            }
            catch (Exception ex)
            {
                mLog.Error(ex);
            }
        }

        private TextBlock CreateOverlayText(WatermarkInfo WatermarkInfo)
        {
            return WrapperTextBlock(WatermarkInfo);
        }

        private TextBlock WrapperTextBlock(WatermarkInfo WatermarkInfo)
        {
            TextBlock element = new TextBlock();
            BrushConverter brushConverter = new BrushConverter();
            System.Windows.Media.Brush brush = (System.Windows.Media.Brush)brushConverter.ConvertFromString(WatermarkInfo.FontColor);
            element.Foreground = brush;
            // test for rotation --- 45
            element.LayoutTransform = new RotateTransform(45);
            element.Text = WatermarkInfo.Text;
            element.FontFamily = new System.Windows.Media.FontFamily(WatermarkInfo.FontName);
            element.FontSize = WatermarkInfo.FontSize;
            element.Opacity = WatermarkInfo.TransparentRatio / 100f > 1.0 ? 1.0 : WatermarkInfo.TransparentRatio / 100f;
            return element;
        }

        #endregion Overlay

        private void stepName_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (mouseDown)
            {
                mouseDown = false;
                DVLStepName.ReleaseMouseCapture();
            }
        }

        private void stepName_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (mouseDown)
            {
                var nx = (int)e.GetPosition(this).X;
                var ny = (int)e.GetPosition(this).Y;

                var x = nx - _mx;
                var y = ny - _my;
                _mx = nx;
                _my = ny;

                updateStepNamePosition(x, y);
            }
        }

        void updateStepNamePosition(int dx, int dy)
        {
            DVLStepName.UpdateLayout();
            var l = Canvas.GetLeft(DVLStepName) + dx;
            var t = Canvas.GetTop(DVLStepName) + dy;
            var boundl = (showSettings || showTree) ? DVLSceneTree.Width + 10 : 10;
            if (l < boundl)
                l = boundl;
            else if (l > grid1.ActualWidth - DVLStepName.ActualWidth - 10)
                l = grid1.ActualWidth - DVLStepName.ActualWidth - 10;
            if (t < 0)
                t = 0;
            else if (t > grid1.ActualHeight - DVLStepName.ActualHeight - 80)
                t = grid1.ActualHeight - DVLStepName.ActualHeight - 80;
            Canvas.SetLeft(DVLStepName, l);
            Canvas.SetTop(DVLStepName, t);
        }

        private void stepName_MouseDown(object sender, MouseButtonEventArgs e)
        {
            mouseDown = true;
            _mx = (int)e.GetPosition(this).X;
            _my = (int)e.GetPosition(this).Y;
            DVLStepName.CaptureMouse();
        }

        //private void NodeFlagChanged(ulong id, NodeFlag flag)
        //{
        //    DVLSceneTree.DVLClientNodeFlagEvent(id, flag);
        //}

        private void LoadVDS(string file)
        {
            if (dispatcherTimer.IsEnabled)
            {
                dispatcherTimer.Stop();
            }

            if (_scene != null)
            {
                _dvlRenderer.AttachScene(null);
                _scene.Dispose();
            }

            var sceneRes = _dvlCore.LoadScene("file://" + file, "", out _scene, 0);

            var pos = file.ToLower().LastIndexOf(".cgm");
            _isCGM = (pos > 0);

            if (sceneRes == Result.OK)
            {
                _dvlRenderer.AttachScene(_scene);
            }
            else
            {
                // System.Windows.MessageBox.Show("Scene load failed");
                ViewerWin.HandlerException(StatusOfView.SYSTEM_INTERNAL_ERROR, "File load failed");
                return;
            }

            DVLSteps.SetScene(_scene);
            DVLSceneTree.SetScene(_scene);

            lstProcedures.Items.Clear();
            // Retrieve procedure list and add to drop down combo box
            for (var i = 0; i < DVLSteps.ProcedureCount; ++i)
            {
                var name = DVLSteps.ProcedureName(i);
                lstProcedures.Items.Add(name);
            }

            lstProcedures.SelectedIndex = 0;

            dispatcherTimer.Start();

            DVLSettings.SetRenderer(_dvlRenderer);

            DVLStepName.SetScene(_scene);
            DVLStepName.SetFontScaling(1.2);

            UpdateViewerLayout();

            _isolated = false;
        }

        //private void CustomItemClicked(string name, int index)
        //{
        //    System.Windows.MessageBox.Show(name);
        //}

        private void DVLSettingsControlItemChanged(int index)
        {
            var name = DVLSettings.GetItemName(index);

            if (name == "Show Selection Popup")
            {
                showPopups = DVLSettings.GetBValue(name);
                UpdateViewerLayout();
            }
        }

        private void UpdateViewerLayout()
        {
            if (_isCGM || _scene == null)
            {
                lblFileName.Margin = new Thickness(47, 0, 47, 0);
                btnSceneTree.Visibility = Visibility.Hidden;
                lstProcedures.Visibility = Visibility.Hidden;
                btnShowStepName.Visibility = Visibility.Hidden;
                btnHome.Visibility = Visibility.Hidden;
                btnPlay.Visibility = Visibility.Hidden;
                btnSettings.Visibility = (_scene == null) ? Visibility.Visible : Visibility.Hidden;
                btnHotspot.Visibility = (_scene == null) ? Visibility.Hidden : Visibility.Visible;
                btnHotspot.Content = showHotspots ? imgHotspotOn : imgHotspotOff;
            }
            else
            {
                lblFileName.Margin = new Thickness(47, 0, 421, 0);
                btnSceneTree.Visibility = Visibility.Visible;
                lstProcedures.Visibility = Visibility.Visible;
                btnShowStepName.Visibility = Visibility.Visible;
                btnHome.Visibility = Visibility.Visible;
                btnPlay.Visibility = Visibility.Visible;
                btnSettings.Visibility = Visibility.Visible;
                btnHotspot.Visibility = Visibility.Hidden;
                btnShowStepName.Content = showStepName ? imgTextOn : imgTextOff;
            }

            if (showTree || showSettings)
            {
                TopBar.Margin = new Thickness(DVLSceneTree.Width, 0, 0, 0);
                veviewer.Margin = new Thickness(DVLSceneTree.Width, TopBar.Height, 0, 0);
                if (DVLStepName != null)
                {
                    if (Canvas.GetLeft(DVLStepName) < DVLSceneTree.Width + 10)
                    {
                        Canvas.SetLeft(DVLStepName, DVLSceneTree.Width + 10);
                    }
                }

                lblFileName.Visibility = Visibility.Hidden;
            }
            else
            {
                TopBar.Margin = new Thickness(0, 0, 0, 0);
                veviewer.Margin = new Thickness(0, TopBar.Height, 0, 0);
                if (DVLStepName != null)
                {
                    var l = (int)Canvas.GetLeft(DVLStepName);
                    if ((l >= DVLSceneTree.Width + 10) && (l < grid1.ActualWidth / 2))
                    {
                        Canvas.SetLeft(DVLStepName, l - DVLSceneTree.Width + 10);
                    }
                }
                lblFileName.Visibility = Visibility.Visible;
            }

            DVLSceneTree.Visibility = showTree ? Visibility.Visible : Visibility.Hidden;
            DVLSettings.Visibility = showSettings ? Visibility.Visible : Visibility.Hidden;
            if (DVLStepName != null)
                DVLStepName.Visibility = (showStepName && DVLSteps.ProcedureCount > 0) ? Visibility.Visible : Visibility.Hidden;

            var stepvis = DVLSteps.ProcedureCount > 0 && showSteps;
            DVLSteps.Visibility = stepvis ? Visibility.Visible : Visibility.Hidden;

            grid1.RowDefinitions[1].Height = new GridLength(stepvis ? 70 : 0);

            //if (!showPopups && DVLNodeMeta != null)
            //{
            //    DVLNodeMeta.Visibility = Visibility.Hidden;
            //}

            if (_dvlRenderer != null && _scene != null && _opengl != null)
            {
                _dvlRenderer.Pan(0, 0);
                Refresh();
            }
        }

        private void ProcedureChanged(object sender, SelectionChangedEventArgs e)
        {
            DVLSteps.SelectProcedure(lstProcedures.SelectedIndex);
        }

        private void veviewer_SizeChanged(object sender, EventArgs e)
        {
            var m = PresentationSource.FromVisual(System.Windows.Application.Current.MainWindow).CompositionTarget.TransformToDevice;
            panel.Width = (int)(veviewer.RenderSize.Width * m.M11);
            panel.Height = (int)(veviewer.RenderSize.Height * m.M22);
            if (_dvlRenderer != null)
            {
                _dvlRenderer.SetDimensions((uint)panel.Width, (uint)panel.Height);
            }
        }

        private void Panel_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (mouseDown)
                return;

            mouseDown = true;
            ox = e.X;
            oy = e.Y;
            mouseMoved = false;

            if (_dvlRenderer == null)
                return;

            panel.Capture = true;
            _dvlRenderer.BeginGesture(ox, oy);
        }

        private void Panel_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (!mouseDown)
                return;

            var dx = e.X - ox;
            var dy = e.Y - oy;

            ox = e.X;
            oy = e.Y;
            mouseMoved = true;

            if (_dvlRenderer == null)
                return;

            if (e.Button == (MouseButtons.Left | MouseButtons.Right))
            {
                _dvlRenderer.Pan(dx, dy);
            }
            else if (e.Button == MouseButtons.Left)
            {
                if (_isCGM)
                {
                    _dvlRenderer.Pan(dx, dy);
                }
                else
                {
                    _dvlRenderer.Rotate(dx, dy);       
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                var z = 1.0f - (dy / 150.0f);
                if (z < 0.1f)
                    z = 0.1f;

                if (z > 10.0f)
                    z = 10.0f;

                _dvlRenderer.Zoom(z);
            }
        }

        private void Panel_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (!mouseDown)
                return;

            mouseDown = false;
            panel.Capture = false;

            //if (!mouseMoved)
            //{
            //    if (DVLNodeMeta != null)
            //    {
            //        var l = (showTree || showSettings) ? DVLSceneTree.Width : 0;
            //        var t = TopBar.Height;

            //        var m = PresentationSource.FromVisual(System.Windows.Application.Current.MainWindow).CompositionTarget.TransformToDevice;

            //        Canvas.SetLeft(DVLNodeMeta, l + ox / m.M11);
            //        Canvas.SetTop(DVLNodeMeta, t + oy / m.M22);
            //    }
            //}

            if (_dvlRenderer == null)
                return;

            _dvlRenderer.EndGesture();

            if (!mouseMoved && e.Button == MouseButtons.Left)
            {
                _dvlRenderer.Tap(ox, oy, false);
            }
        }

        public void Panel_Move(object sender, EventArgs e)
        {

        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            InitGLContext();

            LoadFile(mFilePath);

        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (_dvlRenderer.ShouldRenderFrame())
            {
                _dvlRenderer.RenderFrame();
                _opengl.SwapBuffers();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            dispatcherTimer.Stop();

            if (_dvlRenderer != null)
            {
                _dvlCore.DoneRenderer();
                _dvlRenderer.Dispose();
                _dvlCore.Dispose();
                _opengl.Dispose();
            }
        }

        private void ButtonPlay_Click(object sender, RoutedEventArgs e)
        {
            var menu = new System.Windows.Controls.ContextMenu();

            string[] names = { "Play", "Play All", "Play All Remaining" };

            for (var i = 0; i < names.Length; ++i)
            {
                var item = new System.Windows.Controls.MenuItem();
                item.Click += PlayMenu_Click;
                item.Tag = i;
                item.Header = names[i];
                menu.Items.Add(item);
            }

            menu.IsOpen = true;
        }

        private void PlayMenu_Click(object sender, RoutedEventArgs e)
        {
            var item = (System.Windows.Controls.MenuItem)sender;
            var index = (int)item.Tag;

            switch (index)
            {
                case 0: // Play
                    DVLSteps.Activate(DVLSteps.ActiveIndex);
                    break;
                case 1: // Play All
                    DVLSteps.PlayAll(true);
                    break;
                case 2: // Play All Remaining
                    DVLSteps.PlayAll(false);
                    break;
            }
        }

        private void ButtonPause_Click(object sender, RoutedEventArgs e)
        {
            DVLSteps.Stop();
            updatePlayButton(true);
        }

        private void ButtonLoad_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog();

            if (dlg.ShowDialog().Value)
            {
                LoadVDS(dlg.FileName);
            }
        }

        private void ButtonSettings_Click(object sender, RoutedEventArgs e)
        {
            showSettings = !showSettings;
            UpdateViewerLayout();
        }

        private void ButtonStepName_Click(object sender, RoutedEventArgs e)
        {
            showStepName = !showStepName;
            UpdateViewerLayout();
        }

        private void ButtonHome_Click(object sender, RoutedEventArgs e)
        {
            var menu = new System.Windows.Controls.ContextMenu();

            string[] names = { "Home", _isolated ? "Remove Isolation" : "Isolate", "Hide Selection", "Show All", "Zoom Selection", "Zoom Visible" };

            for (var i = 0; i < names.Length; ++i)
            {
                var item = new System.Windows.Controls.MenuItem();
                item.Click += HomeMenu_Click;
                item.Tag = i;
                item.Header = names[i];
                menu.Items.Add(item);

                if (i == 1 || i == 2 | i == 4)
                {
                    item.IsEnabled = (_selectedNode != 0);
                }

                if (i == 1 && _isolated)
                {
                    item.IsEnabled = true;
                }
            }

            menu.IsOpen = true;
        }

        private void HomeMenu_Click(object sender, RoutedEventArgs e)
        {
            var item = (System.Windows.Controls.MenuItem)sender;
            var index = (int)item.Tag;

            switch (index)
            {
                case 0: // Home
                    _dvlRenderer.ResetView();
                    break;
                case 1: // Isolate/remove Isolation
                    if (!_isolated)
                    {
                        _dvlRenderer.ZoomTo(ZoomTo.NodeSetIsolation, _selectedNode, 0.3f);
                        _isolated = true;
                    }
                    else
                    {
                        _dvlRenderer.ZoomTo(ZoomTo.RestoreRemoveIsolation, _selectedNode, 0.3f);
                        _isolated = false;
                    }
                    break;
                case 2: // Hide Selection
                    {
                        ISceneInfo info;
                        _scene.RetrieveSceneInfo((uint)SceneInfo.Selected, out info);
                        for (var i = 0u; i < info.SelectedNodesCount; ++i)
                        {
                            _scene.ChangeNodeFlags(info.SelectedNodes(i), (uint)NodeFlag.Visible, FlagOperation.Clear | FlagOperation.ModifierRecursive);
                        }
                    }
                    break;
                case 3: // Show All
                    {
                        ISceneInfo info;
                        _scene.RetrieveSceneInfo((uint)SceneInfo.Children, out info);
                        for (var i = 0u; i < info.ChildNodesCount; ++i)
                        {
                            _scene.ChangeNodeFlags(info.ChildNodes(i), (uint)NodeFlag.Visible, FlagOperation.Set | FlagOperation.ModifierRecursive);
                        }
                    }
                    break;
                case 4: // Zoom Selection
                    _dvlRenderer.ZoomTo(ZoomTo.Selected, 0, 0.3f);
                    break;
                case 5: // Zoom Visible
                    _dvlRenderer.ZoomTo(ZoomTo.Visible, 0, 0.3f);
                    break;
                default:
                    break;
            }
        }

        private void HotspotSettings_Click(object sender, RoutedEventArgs e)
        {
            showHotspots = !showHotspots;
            _dvlRenderer.SetOption(RenderOption.ShowAllHotspots, showHotspots);

            btnHotspot.Content = showHotspots ? imgHotspotOn : imgHotspotOff;
        }


        public void OnSceneTreeNodeSelected(IScene scene, ulong numberOfSelectedNodes, ulong idFirstSelectedNode)
        {
            // Selection from scene tree will close node popup
            _selectedNode = idFirstSelectedNode;
         //   DVLNodeMeta.DVLClientNodeSelectionEvent(scene, 0, 0);
        }

        private void ButtonTree_Click(object sender, RoutedEventArgs e)
        {
            showTree = !showTree;
            UpdateViewerLayout();
        }

        void updateLoadProgress(float progress)
        {
            if (progress >= 0.9999)
            {
                lblFileName.Content = _fileName;
            }
            else
            {
                var prog = "Loading " + progress + "%...";
                lblFileName.Content = prog;
                lblFileName.UpdateLayout();
            }
        }

        void updatePlayButton(bool canPlay)
        {
            playPause = !canPlay;

            btnPlay.Visibility = playPause ? Visibility.Hidden : Visibility.Visible;
            btnPause.Visibility = playPause ? Visibility.Visible : Visibility.Hidden;
        }
   
        class DVLClientImpl : IClient
        {
            private VdsViewer m_parent;

            public DVLClientImpl(VdsViewer parent)
            {
                m_parent = parent;
            }

            public override void OnNodeSelectionChanged(IScene scene, ulong numberOfSelectedNodes, ulong idFirstSelectedNode)
            {
                if (numberOfSelectedNodes > 0)
                {
                    m_parent._selectedNode = idFirstSelectedNode;
                }
                else
                {
                    m_parent._selectedNode = 0;
                }

                m_parent.DVLSceneTree.DVLClientNodeSelectionEvent(scene, numberOfSelectedNodes, idFirstSelectedNode);

                //if (m_parent.showPopups)
                //{
                //    m_parent.DVLNodeMeta.DVLClientNodeSelectionEvent(scene, numberOfSelectedNodes, idFirstSelectedNode);
                //}
            }

            public override void LogMessage(ClientLogType type, string source, string text)
            {

            }

            public override void OnStepEvent(StepEvent type, ulong stepID)
            {
              //  m_parent.DVLNodeMeta.Visibility = Visibility.Hidden;
                m_parent.DVLSteps.DVLClientStepEvent(type, stepID);
                m_parent.DVLStepName.DVLClientStepEvent(type, stepID);

                if (type == StepEvent.Started || type == StepEvent.Switched)
                {
                    m_parent.DVLSceneTree.DVLClientNodeFlagEvent(0, (NodeFlag)0);
                    m_parent.updatePlayButton(false);
                    m_parent.updateStepNamePosition(0, 0);
                }

                if (type == StepEvent.Finished)
                {
                    m_parent.updatePlayButton(true);
                }
            }

            public override bool NotifyFileLoadProgress(float progress)
            {
                m_parent.updateLoadProgress(progress);
                return true;
            }
        }
    }
}
