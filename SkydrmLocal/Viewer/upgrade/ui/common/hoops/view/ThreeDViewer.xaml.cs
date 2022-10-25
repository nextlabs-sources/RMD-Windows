#define USING_EXCHANGE
using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;
using Viewer.upgrade.application;
using Viewer.upgrade.file.basic;
using Viewer.upgrade.file.basic.utils;
using Viewer.upgrade.file.utils;
using Viewer.upgrade.ui.common.hoops.commands;
using Viewer.upgrade.ui.common.viewerWindow.view;
using Viewer.upgrade.utils;
using Viewer.upgrade.utils.overlay;
using Viewer.upgrade.ui.common.hoops.viewModel;
using Viewer.upgrade.utils.overlay.windowOverlay;
using Viewer.upgrade.utils.overlay.utils;
using SkydrmLocal.rmc.sdk;
using Alphaleonis.Win32.Filesystem;

namespace Viewer.upgrade.ui.common.hoops.view
{
    /// <summary>
    /// Interaction logic for ThreeDViewer.xaml
    /// </summary>
    public partial class ThreeDViewer : Page, ISensor
    {
        
        private ViewerApp mApplication;
        private log4net.ILog mLog;

        // view window
        private Window viewerWin;

        private string mFilePath = string.Empty;

        /// <summary>
        /// Single HPS World instance
        /// </summary>
        private static HPS.World _hpsWorld;

        /// <summary>
        /// Main distant light in Sprockets view.
        /// </summary>
        private HPS.DistantLightKey _mainDistantLight;

        /// MyErrorHandler
        /// </summary>
        public MyErrorHandler _errorHandler;

        /// <summary>
        /// MyWarningHandler
        /// </summary>
        public MyWarningHandler _warningHandler;

        public bool _enableFrameRate;

        public HPS.Model Model { get; set; }

        public HPS.CADModel CADModel { get; set; }

        private HPS.CameraKit PreZoomToKeyPathCamera { get; set; }

        public HPS.CameraKit DefaultCamera { get; set; }

       //  private Overlay Overlay = new Overlay();

        private WatermarkInfo mWatermarkInfo;

        private WindowOverlay mOverlay;

        public event Action<Exception> OnUnhandledExceptionOccurrence;
        public event Action OnLoadFileSucceed;
        public event Action<bool> EndPrint;
        public event Action<System.Windows.Forms.PrintDialog> BeforePrint;

        public ISensor Sensor
        {
            get { return this; }
        }

        //public ThreeDViewer(_BaseFile baseFile)
        //{
        //    mApplication = (IApplication)Application.Current;
        //    mLog = mApplication.Log;
        //    mLog.Info("\t\t ThreeDViewer \r\n");
        //    InitializeComponent();

        //    //this.mFilePath = filePath;
        //    //this.WatermarkInfo = watermarkInfo;
        //}

        public ThreeDViewer(string filePath)
        {
            InitializeComponent();
            mApplication = (ViewerApp)ViewerApp.Current;
            mLog = mApplication.Log;
            mFilePath = filePath;
        }

        public void Watermark(WatermarkInfo watermarkInfo)
        {
            this.mWatermarkInfo = watermarkInfo;
        }

        private void LoadFile()
        {
            viewerWin = Window.GetWindow(this);
            this.viewerWin.Closed += ViewerWin_Closed;
            //this.viewerWin.SizeChanged += ViewerWin_SizeChanged;

            try
            {
                _enableFrameRate = false;

                // Initialize
                InitializeSprockets();

                // Here mainly used to create one default scene (Canvas background).
                NewCommand.Execute(this);

                // load file
                FileOpenCommand.Execute(this.mFilePath);
                if (_fileOpenCommand is FileOpenCommand)
                {
                    (_fileOpenCommand as FileOpenCommand).ViewWin = viewerWin;
                }

                CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy,  // need this? 
                     new ExecutedRoutedEventHandler(DoCopyToClipboard)));

                if (IsAttachWatermark())
                {
                    AttachWatermark();
                }

                OnLoadFileSucceed?.Invoke();
            }
            catch (Exception ex)
            {
                //  string msg = "System internal error. Exception: " + ex.Message;
                // MessageBox.Show(msg, CultureStringInfo.View_DlgBox_Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                //  Application.Current.Shutdown(-1);
                //   mLog.InfoFormat("\t\t System internal error. Exception:{0} \r\n", ex.Message);
                //  return ErrorCode.SYSTEM_INTERNAL_ERROR;
                OnUnhandledExceptionOccurrence?.Invoke(ex);
            }
        }

        public void AttachWatermark()
        {
            try
            {
                mOverlay = new WindowOverlay();
                Canvas overlayCanvas = new Canvas();
                OverlayUtils.DrawWatermark(mWatermarkInfo,ref overlayCanvas);
                overlayCanvas.Margin = new Thickness(0, 0, 0, 0);
                mOverlay.OverlayContent = overlayCanvas;
                _canvasBrowserGrid.Children.Add(mOverlay);
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

        //private async Task<UInt64> LoadFile()
        //{
        //    if (!mBaseFile.IsNxlFile)
        //    {
        //        this.mFilePath = mBaseFile.FilePath;
        //    }
        //    else
        //    {
        //        Task <DecryptResult> taskDecrypt = Decrypt(mBaseFile);
        //        await taskDecrypt;

        //        if (taskDecrypt.IsCanceled)
        //        {
        //            return ErrorCode.Task_Canceled;
        //        }

        //        if ( taskDecrypt.Result.ErrorCode != ErrorCode.SUCCEEDED)
        //        {
        //            return ErrorCode.SYSTEM_INTERNAL_ERROR;
        //        }

        //        mFilePath = taskDecrypt.Result.RpmFilePath;
        //    }

        //    WatermarkInfo = mBaseFile.WatermarkInfo;

        //    viewerWin = Window.GetWindow(this);
        //    this.viewerWin.Closed += ViewerWin_Closed;
        //    this.viewerWin.SizeChanged += ViewerWin_SizeChanged;

        //    try
        //    {
        //        _enableFrameRate = false;

        //        // Initialize
        //        InitializeSprockets();

        //        // Here mainly used to create one default scene (Canvas background).
        //        NewCommand.Execute(this);

        //        // load file
        //        FileOpenCommand.Execute(this.mFilePath);
        //        if (_fileOpenCommand is FileOpenCommand)
        //        {
        //            (_fileOpenCommand as FileOpenCommand).ViewWin = viewerWin;
        //        }

        //        CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy,  // need this? 
        //             new ExecutedRoutedEventHandler(DoCopyToClipboard)));
        //    }
        //    catch (Exception ex)
        //    {
        //        //  string msg = "System internal error. Exception: " + ex.Message;
        //        // MessageBox.Show(msg, CultureStringInfo.View_DlgBox_Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
        //        //  Application.Current.Shutdown(-1);

        //        mLog.InfoFormat("\t\t System internal error. Exception:{0} \r\n", ex.Message);
        //        return ErrorCode.SYSTEM_INTERNAL_ERROR;
        //    }

        //    Initialize(_canvasBrowserGrid, WatermarkInfo);
        //    return ErrorCode.SUCCEEDED;
        //}


        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadFile();
        }

        /// <summary>
        /// Do 3D hoops print.
        /// </summary>
        public void Print()
        {
            try
            {
                using (var printDlg = new System.Windows.Forms.PrintDialog())
                {
                    printDlg.Document = new PrintDocument();
                    PrintController printController = new StandardPrintController();
                    printDlg.Document.PrintController = printController;

                    // do some printing settings.
                    printDlg.AllowSelection = true;
                    printDlg.AllowSomePages = true;

                    //printDlg.Document.EndPrint += delegate (object sender, PrintEventArgs e)
                    //{
                    //    EndPrint?.Invoke(true);
                    //};
           
                    // register print handler
                    printDlg.Document.PrintPage += delegate (object obj, PrintPageEventArgs eventArgs)
                    {
                        var hdc = eventArgs.Graphics.GetHdc();

                        try
                        {
                            var options = new HPS.Hardcopy.GDI.ExportOptionsKit();

                            float width = 0, height = 0;
                            GetSprocketsControl().Canvas.GetWindowKey().GetWindowInfoControl().ShowPhysicalSize(out width, out height);
                            options.SetWYSIWYG(true);
                            options.SetSize(width, height);
                            options.SetResolution(100);
                        
                            // Print to paper directly, and don't need to export file(need GDI support)
                            var ioResult = HPS.Hardcopy.GDI.Export(
                                hdc,
                                hdc,
                                GetSprocketsControl().Canvas.GetWindowKey(),
                                options);
                  
                        }
                        finally
                        {
                            eventArgs.Graphics.ReleaseHdc();
                            PaperSize size = eventArgs.PageSettings.PaperSize;
                            PrintOverlay(eventArgs.Graphics, size.Width, size.Height);
                        }

                    };


                    if (printDlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        BeforePrint?.Invoke(printDlg);
                        printDlg.Document.Print(); // will callback print handler
                        EndPrint?.Invoke(true);
                    }
                }
            }
            catch (Exception ex)
            {
                EndPrint?.Invoke(false);
            }
        }

        // Hide the Quick Access Toolbar when Ribbon Loaded.
        private void Ribbon_Loaded(object sender, RoutedEventArgs e)
        {
            Grid child = VisualTreeHelper.GetChild((DependencyObject)sender, 0) as Grid;
            if (child != null)
            {
                child.RowDefinitions[0].Height = new GridLength(0);
            }


        }

        private void ViewerWin_Closed(object sender, EventArgs e)
        {
            // Cleanup 
            if (GetSprocketsControl() != null)
            {
                if (Model != null)
                {
                    Model.Delete();
                    Model.Dispose();
                    Model = null;
                }
                if (CADModel != null)
                {
                    CADModel.Delete();
                    CADModel.Dispose();
                    CADModel = null;
                }
                GetSprocketsControl().Delete(); // Calls Delete() on View and Canvas
                GetSprocketsControl().Dispose();
                _hpsWorld.Dispose();
                _hpsWorld = null;
            }

          //  base.OnClosed(e); 
        }

        public void DoCopyToClipboard(object sender, ExecutedRoutedEventArgs e)
        {
            SprocketsWPFControl ctrl = GetSprocketsControl();
            HPS.Hardcopy.GDI.ExportOptionsKit kit = new HPS.Hardcopy.GDI.ExportOptionsKit();
            HPS.Hardcopy.GDI.ExportClipboard(ctrl.Canvas.GetWindowKey(), kit);
        }

        /// <summary>
        /// Sets window title
        /// </summary>
        public void SetTitle(String title)
        {
            Title = "WPF Sandbox " + title;
        }

        /// <summary>
        /// Private method used to connect the SprocketsWPFControl to our WPF element
        /// </summary>
        private void SetSprocketsControl(SprocketsWPFControl ctrl)
        {
            _mainBorder.Child = ctrl;
        }

        /// <summary>
        /// Helper method for retrieving SprocketsWPFControl attached to UI
        /// </summary>
        /// <returns>SprocketsWPFControl attached to _mainBorder.Child</returns>
        public SprocketsWPFControl GetSprocketsControl()
        {
            return _mainBorder.Child as SprocketsWPFControl;
        }

        /// <summary>
        /// Sets the main distant light for the Sprockets View.
        /// </summary>
        public void SetMainDistantLight(HPS.DistantLightKit light)
        {
            // Delete previous light before inserting new one
            if (_mainDistantLight != null)
                _mainDistantLight.Delete();
            _mainDistantLight = GetSprocketsControl().Canvas.GetFrontView().GetSegmentKey().InsertDistantLight(light);
        }

        /// <summary>
        /// Sets the direction for a camera-relative, colorless, main distant light for the Sprockets View.
        /// </summary>
        public void SetMainDistantLight(HPS.Vector? lightDirection = null)
        {
            HPS.DistantLightKit light = new HPS.DistantLightKit();
            light.SetDirection(lightDirection.HasValue ? lightDirection.Value : new HPS.Vector(1, 0, -1.5f));
            light.SetCameraRelative(true);
            SetMainDistantLight(light);
        }


        /// <summary>
        /// Initializes Sprockets and creates SprocketsWPFControl
        /// </summary>
        private void InitializeSprockets()
        {
            // Create and initialize Sprockets World
            string key = HOOPS_LICENSE.KEY;
            _hpsWorld = new HPS.World(HOOPS_LICENSE.KEY); // set license key

#if USING_EXCHANGE
            string exchangeInstallDir = System.Environment.GetEnvironmentVariable("HEXCHANGE_INSTALL_DIR");
            if (!String.IsNullOrEmpty(exchangeInstallDir))
            {
                string binFolder;
#if WIN64
                binFolder = "win64";
#else
                binFolder = "win32";
#endif
                _hpsWorld.SetExchangeLibraryDirectory(exchangeInstallDir + "/bin/" + binFolder);
            }
#endif

#if USING_PUBLISH
            string publishInstallDir = System.Environment.GetEnvironmentVariable("HPUBLISH_INSTALL_DIR");
            if (!String.IsNullOrEmpty(publishInstallDir))
                _hpsWorld.SetPublishResourceDirectory(publishInstallDir + "/bin/resource");
#endif

#if USING_PARASOLID
            string PARASOLID_INSTALL_DIR = Environment.GetEnvironmentVariable("PARASOLID_INSTALL_DIR");
            if (PARASOLID_INSTALL_DIR != null)
            {
                string base_string = "/base";
#if _M_X64
                base_string += "64";
#else
                base_string += "32";
#endif

#if VS2013
                base_string += "_vc12";
#elif VS2015
                base_string += "_vc14";
#endif
                _hpsWorld.SetParasolidSchemaDirectory(PARASOLID_INSTALL_DIR + base_string + "/schema");
            }
#endif

#if (USING_DWG && !DEBUG)
 
string REALDWG_INSTALL_DIR = null;

#if VS2012
#if _M_X64
            REALDWG_INSTALL_DIR = Environment.GetEnvironmentVariable("REALDWG_2016_SDK_DIR_X64");
#else
            REALDWG_INSTALL_DIR = Environment.GetEnvironmentVariable("REALDWG_2016_SDK_DIR");
#endif
#endif
#endif
            // Create and attach Sprockets Control
            SprocketsWPFControl ctrl = new SprocketsWPFControl(HPS.Window.Driver.Default3D, "main");
            ctrl.FileDropped += OnFileDrop; // what this? 
            SetSprocketsControl(ctrl);

            // init error and warning handler
            _errorHandler = new MyErrorHandler();
            _warningHandler = new MyWarningHandler();
        }

        private void OnFileDrop(object sender, MyFileDropEventArgs e)
        {
            FileOpenCommand dfoc = new FileOpenCommand(this);
#if USING_EXCHANGE
            dfoc.ThreadedFileLoad(e.Filename, GetSprocketsControl());
#else
            if (HandledByVisualize(e.Filename)
#if USING_PARASOLID
                || HandldedByParasolid(e.Filename)
#endif
                )
                dfoc.ThreadedFileLoad(e.Filename, GetSprocketsControl());
            else
                MessageBox.Show("Unsupported file format", mApplication.FindResource("View_DlgBox_Title").ToString());
#endif
        }

        /// <summary>
        /// Sets up defaults for a Sprockets Control attached to the specified WPF border control
        /// </summary>
        public void SetupSceneDefaults()
        {
            // Grab the SprocketsWPFControl from the border element
            SprocketsWPFControl ctrl = GetSprocketsControl();
            if (ctrl == null)
                return;

            // Attach a model
            HPS.View view = ctrl.Canvas.GetFrontView();
            view.AttachModel(Model);

            // Set default operators.  Orbit is on top and will be replaced when the operator is changed
            view.GetOperatorControl()
                .Push(new HPS.MouseWheelOperator(), HPS.Operator.Priority.Low)
                .Push(new HPS.ZoomOperator(HPS.MouseButtons.ButtonMiddle()))
                .Push(new HPS.PanOperator(HPS.MouseButtons.ButtonRight()))
                .Push(new HPS.OrbitOperator(HPS.MouseButtons.ButtonLeft()));

            // Subscribe _errorHandler to handle errors
            HPS.Database.GetEventDispatcher().Subscribe(_errorHandler, HPS.Object.ClassID<HPS.ErrorEvent>());

            // Subscribe _warningHandler to handle warnings
            HPS.Database.GetEventDispatcher().Subscribe(_warningHandler, HPS.Object.ClassID<HPS.WarningEvent>());

        }

        public void CreateNewModel()
        {
            if (Model != null)
                Model.Delete();
            Model = HPS.Factory.CreateModel();

            if (CADModel != null)
            {
                CADModel.Delete();
                CADModel = null;
            }

            DefaultCamera = null;
        }

        public void UpdatePlanes()
        {
            HPS.View view = GetSprocketsControl().Canvas.GetFrontView();
            view.SetSimpleShadow(view.GetSimpleShadow());
        }
        public void Unhighlight()
        {
            var highlightOptions = new HPS.HighlightOptionsKit();
            highlightOptions.SetStyleName("highlight_style").SetNotification(true);

            var canvas = GetSprocketsControl().Canvas;
            canvas.GetWindowKey().GetHighlightControl().Unhighlight(highlightOptions);

            HPS.Database.GetEventDispatcher().InjectEvent(new HPS.HighlightEvent(HPS.HighlightEvent.Action.Unhighlight, new HPS.SelectionResults(), highlightOptions));
            HPS.Database.GetEventDispatcher().InjectEvent(new HPS.ComponentHighlightEvent(HPS.ComponentHighlightEvent.Action.Unhighlight, canvas, 0, new HPS.ComponentPath(), highlightOptions));
        }

        public void Update()
        {
            GetSprocketsControl().Canvas.Update();
        }

        public void ActivateCapture(HPS.Capture capture)
        {
            HPS.View newView = capture.Activate();
            var newViewSegment = newView.GetSegmentKey();
            HPS.CameraKit newCamera;
            newViewSegment.ShowCamera(out newCamera);

            newCamera.UnsetNearLimit();
            var defaultCameraWithoutNearLimit = HPS.CameraKit.GetDefault().UnsetNearLimit();
            if (newCamera == defaultCameraWithoutNearLimit)
            {
                // there was no camera for this capture - so we'll use the current camera but do a FitWorld on it
                var oldView = GetSprocketsControl().Canvas.GetFrontView();
                HPS.CameraKit oldCamera;
                oldView.GetSegmentKey().ShowCamera(out oldCamera);

                newViewSegment.SetCamera(oldCamera);
                newView.FitWorld();
            }

            AttachViewWithSmoothTransition(newView);
        }
        public void AttachViewWithSmoothTransition(HPS.View newView, bool firstAttach = false)
        {
            HPS.View oldView = GetSprocketsControl().Canvas.GetFrontView();
            HPS.CameraKit oldCamera;
            oldView.GetSegmentKey().ShowCamera(out oldCamera);

            var newViewSegment = newView.GetSegmentKey();
            HPS.CameraKit newCamera;
            newViewSegment.ShowCamera(out newCamera);

            AttachView(newView, firstAttach);

            newViewSegment.SetCamera(oldCamera);
            newView.SmoothTransition(newCamera);
        }

        public void AttachView(HPS.View newView, bool firstAttach = false)
        {
            var canvas = GetSprocketsControl().Canvas;
            if (!firstAttach && CADModel != null)
            {
                CADModel.ResetVisibility(canvas);
                canvas.GetWindowKey().GetHighlightControl().UnhighlightEverything();
            }

            PreZoomToKeyPathCamera = null;

            HPS.View oldView = canvas.GetFrontView();
            canvas.AttachViewAsLayout(newView);

            HPS.Operator[] operators;
            var oldViewOperatorCtrl = oldView.GetOperatorControl();
            var newViewOperatorCtrl = newView.GetOperatorControl();
            oldViewOperatorCtrl.Show(HPS.Operator.Priority.Low, out operators);
            newViewOperatorCtrl.Set(operators, HPS.Operator.Priority.Low);
            oldViewOperatorCtrl.Show(HPS.Operator.Priority.Default, out operators);
            newViewOperatorCtrl.Set(operators, HPS.Operator.Priority.Default);
            oldViewOperatorCtrl.Show(HPS.Operator.Priority.High, out operators);

            SetMainDistantLight();

            oldView.Delete();
        }

        public void ZoomToKeyPath(HPS.KeyPath keyPath)
        {
            HPS.BoundingKit bounding;
            if (keyPath.ShowNetBounding(out bounding))
            {
                var frontView = GetSprocketsControl().Canvas.GetFrontView();
                HPS.CameraKit frontViewCamera;
                frontView.GetSegmentKey().ShowCamera(out frontViewCamera);
                PreZoomToKeyPathCamera = frontViewCamera;

                frontView.ComputeFitWorldCamera(bounding, out frontViewCamera);
                frontView.SmoothTransition(frontViewCamera);
            }
        }

        public void RestorePreZoomToKeyPathCamera()
        {
            if (PreZoomToKeyPathCamera != null)
            {
                GetSprocketsControl().Canvas.GetFrontView().SmoothTransition(PreZoomToKeyPathCamera);
                HPS.Database.Sleep(500);

                InvalidatePreZoomToKeyPathCamera();
            }
        }

        public void InvalidatePreZoomToKeyPathCamera()
        {
            PreZoomToKeyPathCamera = null;
        }

        /// <summary>
        /// OnClosed override used for cleaning up HPS and Sprockets resources.
        /// </summary>
        /// <param name="e"></param>
        //protected override void OnClosed(EventArgs e)
        //{
        //    // Cleanup 
        //    if (GetSprocketsControl() != null)
        //    {
        //        if (Model != null)
        //        {
        //            Model.Delete();
        //            Model.Dispose();
        //            Model = null;
        //        }
        //        if (CADModel != null)
        //        {
        //            CADModel.Delete();
        //            CADModel.Dispose();
        //            CADModel = null;
        //        }
        //        GetSprocketsControl().Delete(); // Calls Delete() on View and Canvas
        //        GetSprocketsControl().Dispose();
        //        _hpsWorld.Dispose();
        //        _hpsWorld = null;
        //    }

        //    base.OnClosed(e);
        //}

        public bool HandldedByParasolid(string filename)
        {
            string extension = Path.GetExtension(filename);
            if (extension == ".x_t" ||
                extension == ".x_b" ||
                extension == ".xmt_txt" ||
                extension == ".xmt_bin")
                return true;
            else
                return false;
        }

        public bool HandledByVisualize(string filename)
        {
            string extension = Path.GetExtension(filename);
            if (extension == ".hsf" ||
                extension == ".stl" ||
                extension == ".obj")
                return true;
            else
                return false;
        }

        #region Commands

        /// This section is for ICommands used for 3D View window.
        /// The commands is instantiated when first referenced.

        ///
        /// Command handles
        /// 

        // File opoen
        private BaseCommand _fileOpenCommand;
        private BaseCommand _newCommand;

        // Operators
        private BaseCommand _orbitCommand;
        private BaseCommand _panCommand;
        private BaseCommand _zoomAreaCommand;
        private BaseCommand _flyCommand;
        private BaseCommand _homeCommand;
        private BaseCommand _zoomFitCommand;
        private BaseCommand _pointSelectCommand;
        private BaseCommand _areaSelectCommand;

        // Modes
        private BaseCommand _simpleShadowModeCommand;
        private BaseCommand _smoothModeCommand;
        private BaseCommand _hiddenLineModeCommand;
        private BaseCommand _frameRateModeCommand;

        ///
        /// public Command Properties
        /// 
        public ICommand FileOpenCommand
        {
            get
            {
                if (_fileOpenCommand == null)
                {
                    _fileOpenCommand = new FileOpenCommand(this);
                }

                return _fileOpenCommand;
            }
        }

        /// <summary>
        /// New command --- mainly used to create one default scene (Canvas background).
        /// </summary>
        public ICommand NewCommand
        {
            get
            {
                if (_newCommand == null)
                {
                    _newCommand = new NewCommand(this);
                }

                return _newCommand;
            }
        }

        // Operators
        public ICommand OrbitCommand
        {
            get
            {
                if (_orbitCommand == null)
                    _orbitCommand = new OrbitCommand(this);
                return _orbitCommand;
            }
        }

        public ICommand PanCommand
        {
            get
            {
                if (_panCommand == null)
                    _panCommand = new PanCommand(this);
                return _panCommand;
            }
        }

        public ICommand ZoomAreaCommand
        {
            get
            {
                if (_zoomAreaCommand == null)
                    _zoomAreaCommand = new ZoomAreaCommand(this);
                return _zoomAreaCommand;
            }
        }

        public ICommand FlyCommand
        {
            get
            {
                if (_flyCommand == null)
                    _flyCommand = new FlyCommand(this);
                return _flyCommand;
            }
        }

        public ICommand HomeCommand
        {
            get
            {
                if (_homeCommand == null)
                    _homeCommand = new HomeCommand(this);
                return _homeCommand;
            }
        }

        public ICommand ZoomFitCommand
        {
            get
            {
                if (_zoomFitCommand == null)
                    _zoomFitCommand = new ZoomFitCommand(this);
                return _zoomFitCommand;
            }
        }

        public ICommand PointSelectCommand
        {
            get
            {
                if (_pointSelectCommand == null)
                    _pointSelectCommand = new PointSelectCommand(this);
                return _pointSelectCommand;
            }
        }

        public ICommand AreaSelectCommand
        {
            get
            {
                if (_areaSelectCommand == null)
                    _areaSelectCommand = new AreaSelectCommand(this);
                return _areaSelectCommand;
            }
        }

        // Modes
        public ICommand SimpleShadowModeCommand
        {
            get
            {
                if (_simpleShadowModeCommand == null)
                    _simpleShadowModeCommand = new SimpleShadowModeCommand(this);
                return _simpleShadowModeCommand;
            }
        }

        public ICommand SmoothModeCommand
        {
            get
            {
                if (_smoothModeCommand == null)
                    _smoothModeCommand = new SmoothModeCommand(this);
                return _smoothModeCommand;
            }
        }

        public ICommand HiddenLineModeCommand
        {
            get
            {
                if (_hiddenLineModeCommand == null)
                    _hiddenLineModeCommand = new HiddenLineModeCommand(this);
                return _hiddenLineModeCommand;
            }
        }

        public ICommand FrameRateModeCommand
        {
            get
            {
                if (_frameRateModeCommand == null)
                    _frameRateModeCommand = new FrameRateModeCommand(this);
                return _frameRateModeCommand;
            }

        }

        #endregion // Commands


        #region Toolbar ToggleButton Dependency Properties

        /// Toolbar ToggleButton Dependency Properties
        /// 
        /// This section is for the Toggle Toolbar button bindings.
        /// Dependency properties are used to keep the toggle state of the buttons in sync with these properties.
        /// 

        /// <summary>
        /// Current Toggle toolbar button
        /// </summary>
        private DependencyProperty CurrentToggleButtonOpProperty = IsCurrentOpOrbitProperty; // Init current toggle is Orbit operator.

        /// 
        /// Public property wrappers for dependency properties
        ///
        public bool IsCurrentOpOrbit
        {
            get { return (bool)GetValue(IsCurrentOpOrbitProperty); }
            set { SetValue(IsCurrentOpOrbitProperty, value); }
        }

        public bool IsCurrentOpPan
        {
            get { return (bool)GetValue(IsCurrentOpPanProperty); }
            set { SetValue(IsCurrentOpPanProperty, value); }
        }

        public bool IsCurrentOpZoomArea
        {
            get { return (bool)GetValue(IsCurrentOpZoomAreaProperty); }
            set { SetValue(IsCurrentOpZoomAreaProperty, value); }
        }

        public bool IsCurrentOpSelectPoint
        {
            get { return (bool)GetValue(IsCurrentOpSelectPointProperty); }
            set { SetValue(IsCurrentOpSelectPointProperty, value); }
        }

        public bool IsCurrentOpSelectArea
        {
            get { return (bool)GetValue(IsCurrentOpSelectAreaProperty); }
            set { SetValue(IsCurrentOpSelectAreaProperty, value); }
        }

        public bool IsCurrentOpFly
        {
            get { return (bool)GetValue(IsCurrentOpFlyProperty); }
            set { SetValue(IsCurrentOpFlyProperty, value); }
        }

        /// 
        /// Dependency Properties
        /// 

        private static readonly DependencyProperty IsCurrentOpOrbitProperty = RegisterToggleButtonOp("IsCurrentOpOrbit", true);
        private static readonly DependencyProperty IsCurrentOpPanProperty = RegisterToggleButtonOp("IsCurrentOpPan", false);
        private static readonly DependencyProperty IsCurrentOpZoomAreaProperty = RegisterToggleButtonOp("IsCurrentOpZoomArea", false);
        private static readonly DependencyProperty IsCurrentOpSelectPointProperty = RegisterToggleButtonOp("IsCurrentOpSelectPoint", false);
        private static readonly DependencyProperty IsCurrentOpSelectAreaProperty = RegisterToggleButtonOp("IsCurrentOpSelectArea", false);
        private static readonly DependencyProperty IsCurrentOpFlyProperty = RegisterToggleButtonOp("IsCurrentOpFly", false);

        /// <summary>
        /// Array of all toggle button properties
        /// </summary>
        private static readonly DependencyProperty[] ToggleButtonProperties =
        {
            IsCurrentOpOrbitProperty,
            IsCurrentOpPanProperty,
            IsCurrentOpZoomAreaProperty,
            IsCurrentOpSelectPointProperty,
            IsCurrentOpSelectAreaProperty,
            IsCurrentOpFlyProperty
        };

        private static DependencyProperty RegisterToggleButtonOp(string name, bool defaultValue)
        {
            return DependencyProperty.Register(name, typeof(bool), typeof(ThreeDViewer),
                new FrameworkPropertyMetadata(defaultValue, new PropertyChangedCallback(OnIsCurrentOpChanged)));
        }

        /// <summary>
        /// Property changed callback for toggle toolbar buttons.
        /// 
        /// Ensures that only one toggle toolbar button is selected, and that you cannot deselect the toggle.
        /// </summary>
        private static void OnIsCurrentOpChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            ThreeDViewer win = obj as ThreeDViewer;

            // Store property which is to be enabled
            if ((bool)args.NewValue == true)
                win.CurrentToggleButtonOpProperty = args.Property;

            // Ensure current property is enabled.
            win.SetValue(win.CurrentToggleButtonOpProperty, true);

            // Disable all other properties
            foreach (DependencyProperty dp in ToggleButtonProperties)
                if (dp != win.CurrentToggleButtonOpProperty)
                    win.SetValue(dp, false);
        }

        #endregion // Toolbar ToggleButton Dependency Properties

        #region Mode ToggleButton Dependency Properties

        private DependencyProperty CurrentToggleButtonModeProperty = IsSmoothProperty;

        public bool IsSmooth
        {
            get { return (bool)GetValue(IsSmoothProperty); }
            set { SetValue(IsSmoothProperty, value); }
        }

        public bool IsHidden
        {
            get { return (bool)GetValue(IsHiddenProperty); }
            set { SetValue(IsHiddenProperty, value); }
        }

        private static readonly DependencyProperty IsSmoothProperty = RegisterToggleButtonModes("IsSmooth", true);
        private static readonly DependencyProperty IsHiddenProperty = RegisterToggleButtonModes("IsHidden", false);

        private static readonly DependencyProperty[] ToggleButtonModeProperties =
        {
            IsSmoothProperty,
            IsHiddenProperty,
        };

        private static DependencyProperty RegisterToggleButtonModes(string name, bool defaultValue)
        {
            return DependencyProperty.Register(name, typeof(bool), typeof(ThreeDViewer),
                new FrameworkPropertyMetadata(defaultValue, new PropertyChangedCallback(OnIsCurrentModeChanged)));
        }

        private static void OnIsCurrentModeChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            ThreeDViewer win = obj as ThreeDViewer;

            // Store property which is to be enabled
            if ((bool)args.NewValue == true)
                win.CurrentToggleButtonModeProperty = args.Property;

            // Ensure current property is enabled.
            win.SetValue(win.CurrentToggleButtonModeProperty, true);

            // Disable all other properties
            foreach (DependencyProperty dp in ToggleButtonModeProperties)
                if (dp != win.CurrentToggleButtonModeProperty)
                    win.SetValue(dp, false);
        }
        #endregion  // Mode ToggleButton Dependency Properties


        private void PrintOverlay(Graphics graphics, double width, double height)
        {
            if (IsAttachWatermark())
            {
                OverlayUtils.DrawOverlayByGraphics(graphics, width, height, mWatermarkInfo);
            }
            //if (IsAttach())
            //{
            //    Overlay.Graphics = graphics;
            //    Overlay.DrawOverlayByGraphics(width, height);
            //}
        }

        //public UIElement Attach(double width, double height)
        //{
        //    return Overlay.CreateOverlay(width,height);
        //}

        //public bool IsAttach()
        //{
        //    return ToolKit.IsAttachOverlay(WatermarkInfo);
        //}

        //private void InitOverlay(FrameworkElement visual, WatermarkInfo waterMark)
        //{
        //    Overlay.Initialize(visual, waterMark);
        //}

        //private void AttachOverlay(double width, double height)
        //{
        //    _canvasBrowserGrid.Children.Add(Attach(width, height));
        //}

        //public void Initialize(FrameworkElement visual, WatermarkInfo waterMark)
        //{
        //    if (IsAttach())
        //    {
        //        if (!Overlay.Initialze)
        //        {
        //            InitOverlay(visual, waterMark);
        //        }
        //        AttachOverlay(visual.ActualWidth, visual.ActualHeight);
        //    }
        //}

        //public void OnSizeChanged(FrameworkElement visual, WatermarkInfo waterMark)
        //{
        //    if (IsAttach())
        //    {
        //        if (!Overlay.Initialze)
        //        {
        //            InitOverlay(visual, waterMark);
        //        }
        //        Overlay.OnOverlayChange();
        //    }
        //}


        // Re-draw overlay when window size changed, Note: don't listen the page size changed, or else,
        // the SprocketsWPFControl canvas redraw will not be executed when change the window size
        // for fix bug: 49914
        //private void ViewerWin_SizeChanged(object sender, SizeChangedEventArgs e)
        //{
        //    OnSizeChanged(_canvasBrowserGrid, WatermarkInfo);
        //}
    }
}
