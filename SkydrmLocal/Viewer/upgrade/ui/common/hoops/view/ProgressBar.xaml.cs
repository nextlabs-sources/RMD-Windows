using System;
using System.Windows;
using HPS;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.Windows.Interop;


namespace Viewer.upgrade.ui.common.hoops.view
{
    public enum Operation
    {
        Import,
        Export
    }

    /// <summary>
    /// Interaction logic for ProgressBar.xaml
    /// </summary>
    public partial class ProgressBar : System.Windows.Window
    {

        public ProgressBar(ThreeDViewer in_view_window, Operation in_operation = Operation.Import)
        {
            InitializeComponent();

            success = false;
            view_window = in_view_window;
            operation = in_operation;

            dispatcher_timer = new System.Windows.Threading.DispatcherTimer();
            dispatcher_timer.Tick += new System.EventHandler(dispatcherTimer_Tick);
            dispatcher_timer.Interval = new TimeSpan(0, 0, 0, 0, 50);   //fire every 50 milli-seconds
            dispatcher_timer.Start();
        }

        public ProgressBar(ThreeDViewer in_main_window, HPS.IONotifier in_notifier, Operation in_operation = Operation.Import) : this(in_main_window, in_operation)
        {
            Notifier = in_notifier;
        }

        private ThreeDViewer view_window;
        private bool success;
        private Operation operation;
        public HPS.IONotifier Notifier { get; set; }
        private DispatcherTimer dispatcher_timer;
        public string ImportStatusMessage { get; set; }

        //needed to get rid of the close button
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                float complete;
                HPS.IOResult status = Notifier.Status(out complete);

                int progress = (int)(complete * 100.0f + 0.5f);
                ((System.Windows.Controls.ProgressBar)progress_bar).Value = progress;

                if (operation == Operation.Export)
                    this.Title = "Saving ... (" + progress.ToString() + "%)";
                else
                {
                    string titlePrefix = "Loading ...";
                    if (!string.IsNullOrEmpty(ImportStatusMessage))
                        titlePrefix = ImportStatusMessage;
                    this.Title = titlePrefix + " (" + progress.ToString() + "%)";
                }

                if (status != HPS.IOResult.InProgress)
                {
                    if (status == HPS.IOResult.Success && operation != Operation.Export)
                        PerformInitialUpdate();

                    dispatcher_timer.Stop();
                    this.Close();
                }
            }
            catch (System.Exception)
            {
                dispatcher_timer.Stop();
                success = false;
                this.Close();
            }
        }

        private void PerformInitialUpdate()
        {
            HPS.Type notifierType = Notifier.Type();
            if (notifierType == HPS.Type.ParasolidImportNotifier || notifierType == HPS.Type.DWGImportNotifier)
            {
                HPS.CADModel cadModel = null;
#if USING_PARASOLID
                if (notifierType == HPS.Type.ParasolidImportNotifier)
                    cadModel = (Notifier as HPS.Parasolid.ImportNotifier).GetCADModel();
#elif USING_DWG
                if (notifierType == HPS.Type.DWGImportNotifier)
                    cadModel = (Notifier as HPS.DWG.ImportNotifier).GetCADModel();
#endif
                if (cadModel != null)
                {
                    HPS.Canvas canvas = view_window.GetSprocketsControl().Canvas;

                    HPS.View oldView = canvas.GetFrontView();
                    HPS.Operator[] lowPriorityOps;
                    oldView.GetOperatorControl().Show(HPS.Operator.Priority.Low, out lowPriorityOps);
                    HPS.Operator[] normalPriorityOps;
                    oldView.GetOperatorControl().Show(HPS.Operator.Priority.Default, out normalPriorityOps);
                    HPS.Operator[] highPriorityOps;
                    oldView.GetOperatorControl().Show(HPS.Operator.Priority.High, out highPriorityOps);

                    HPS.View view = cadModel.ActivateDefaultCapture();
                    canvas.AttachViewAsLayout(view);
                    view.GetOperatorControl()
                        .Set(lowPriorityOps, HPS.Operator.Priority.Low)
                        .Set(normalPriorityOps, HPS.Operator.Priority.Default)
                        .Set(highPriorityOps, HPS.Operator.Priority.High);

                    // Enable static model for better performance
                    view.GetAttachedModel().GetSegmentKey().GetPerformanceControl().SetStaticModel(HPS.Performance.StaticModel.Attribute);

                    // FitWorld to center the model
                    view.FitWorld();
                }
            }
            else
            {
                HPS.View current_view = view_window.GetSprocketsControl().Canvas.GetFrontView();
                view_window.Model.GetSegmentKey().GetPerformanceControl().SetStaticModel(Performance.StaticModel.Attribute);
                current_view.AttachModel(view_window.Model);

                if (Notifier.Type() == HPS.Type.StreamImportNotifier)
                {
                    HPS.Stream.ImportResultsKit results = (Notifier as HPS.Stream.ImportNotifier).GetResults();
                    HPS.CameraKit default_camera;
                    if (results.ShowDefaultCamera(out default_camera))
                        current_view.GetSegmentKey().SetCamera(default_camera);
                    else
                        current_view.FitWorld();
                }
                else
                    current_view.FitWorld();
            }

            view_window.SetMainDistantLight();

            this.Title = "Performing Initial Update";
            HPS.UpdateNotifier update_notifier = view_window.GetSprocketsControl().Canvas.UpdateWithNotifier(HPS.Window.UpdateType.Exhaustive);
            update_notifier.Wait();

            success = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Notifier.Cancel();

            dispatcher_timer.Stop();
            this.Close();
        }

        public bool WasSuccessful() { return success; }
    }
}
