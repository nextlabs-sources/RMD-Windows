using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Viewer.overlay;
using Viewer.utils;
using Viewer.viewer;

namespace Viewer.render.preview
{
    /// <summary>
    /// Interaction logic for OverlayWindow.xaml
    /// </summary>
    public partial class OverlayWindow : Window
    {
       // private Overlay Overlay = new Overlay();
        private WatermarkInfo WatermarkInfo { get; set; }
        private static OverlayWindow OW_Global_Ref { get; set; }
        private double DeviceIndependentUnit = Convert.ToDouble(1) / Convert.ToDouble(96);
        private float DpiX = 96;
        private float DpiY = 96;
        private double DisplayMonitorX = SystemParameters.WorkArea.Width;
        private double DisplayMonitorY = SystemParameters.WorkArea.Height;
        private log4net.ILog mLog;
        
        public OverlayWindow(WatermarkInfo watermarkInfo,log4net.ILog log)
        {
            mLog = log;
            mLog.Info("\t\t OverlayWindow \r\n");
            InitializeComponent();
            using (Graphics graphics = Graphics.FromHwnd(IntPtr.Zero))
            {
                DpiX = graphics.DpiX;
                DpiY = graphics.DpiY;
            }
            WindowStyle = WindowStyle.None;
            this.WatermarkInfo = watermarkInfo;
            Host_Grid.Children.Add(CreateOverlayVisual());
        }

        #region Overlay
        public Canvas CreateOverlayVisual()
        {
            mLog.Info("\t\t CreateOverlayVisual \r\n");
            Canvas canvas = new Canvas();
            try
            {
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
                //guard against memory leak
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
            }
            return canvas;
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {         
            OW_Global_Ref = this;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {

        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
        }

        [DllImport("user32.dll")]
        private static extern IntPtr PostMessage(IntPtr hwnd, UInt32 msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);
        private const Int32 WM_MOVE = 0x0003;
        private const Int32 WM_MOUSEWHEEL = 0x020A;
        private const Int32 WM_LBUTTONUP = 0x0202;
        private const Int32 WM_LBUTTONDOWN = 0x0201;
        private const Int32 WM_PARENTNOTIFY = 0x0210;
        private const Int32 WM_SETCURSOR = 0x0020;
        private const Int32 WM_KILLFOCUS = 0x0008;
        private const Int32 WM_SIZE = 0x0005;
        private const Int32 WM_ACTIVATEAPP = 0x001C;
        private const Int32 WM_ACTIVATE = 0x0006;
        private const Int32 WM_SETFOCUS = 0x0007;
        private const Int32 WM_NCACTIVATE = 0x0086;


        [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern long GetClassName(IntPtr hwnd, StringBuilder lpClassName, long nMaxCount);

        [DllImport("user32")]
        private static extern bool EnumChildWindows(IntPtr window, EnumWindowsProc callback, IntPtr lParam);
        public delegate bool EnumWindowsProc(int hWnd, int lParam);

        [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern long GetWindowText(IntPtr hwnd, StringBuilder lpString, long cch);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(ref System.Drawing.Point pPoint);

        [DllImport("user32.dll")]
        private static extern bool ScreenToClient(IntPtr hWnd, ref System.Drawing.Point lpPoint);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, ref RECT rect);
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;        // x position of upper-left corner
            public int Top;         // y position of upper-left corner
            public int Right;       // x position of lower-right corner
            public int Bottom;      // y position of lower-right corner
        }
        private static IntPtr MakeLParam(int LoWord, int HiWord)
        {
            int i = (HiWord << 16) | (LoWord & 0xffff);
            return new IntPtr(i);
        }

        [Flags()]
        private enum RedrawWindowFlags : uint
        {
            /// <summary>
            /// Invalidates the rectangle or region that you specify in lprcUpdate or hrgnUpdate.
            /// You can set only one of these parameters to a non-NULL value. If both are NULL, RDW_INVALIDATE invalidates the entire window.
            /// </summary>
            Invalidate = 0x1,

            /// <summary>Causes the OS to post a WM_PAINT message to the window regardless of whether a portion of the window is invalid.</summary>
            InternalPaint = 0x2,

            /// <summary>
            /// Causes the window to receive a WM_ERASEBKGND message when the window is repainted.
            /// Specify this value in combination with the RDW_INVALIDATE value; otherwise, RDW_ERASE has no effect.
            /// </summary>
            Erase = 0x4,

            /// <summary>
            /// Validates the rectangle or region that you specify in lprcUpdate or hrgnUpdate.
            /// You can set only one of these parameters to a non-NULL value. If both are NULL, RDW_VALIDATE validates the entire window.
            /// This value does not affect internal WM_PAINT messages.
            /// </summary>
            Validate = 0x8,

            NoInternalPaint = 0x10,

            /// <summary>Suppresses any pending WM_ERASEBKGND messages.</summary>
            NoErase = 0x20,

            /// <summary>Excludes child windows, if any, from the repainting operation.</summary>
            NoChildren = 0x40,

            /// <summary>Includes child windows, if any, in the repainting operation.</summary>
            AllChildren = 0x80,

            /// <summary>Causes the affected windows, which you specify by setting the RDW_ALLCHILDREN and RDW_NOCHILDREN values, to receive WM_ERASEBKGND and WM_PAINT messages before the RedrawWindow returns, if necessary.</summary>
            UpdateNow = 0x100,

            /// <summary>
            /// Causes the affected windows, which you specify by setting the RDW_ALLCHILDREN and RDW_NOCHILDREN values, to receive WM_ERASEBKGND messages before RedrawWindow returns, if necessary.
            /// The affected windows receive WM_PAINT messages at the ordinary time.
            /// </summary>
            EraseNow = 0x200,

            Frame = 0x400,

            NoFrame = 0x800
        }
        [DllImport("user32.dll")]
        static extern bool RedrawWindow(IntPtr hWnd, [In] ref RECT lprcUpdate, IntPtr hrgnUpdate, RedrawWindowFlags flags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool EnableWindow(IntPtr hWnd, bool bEnable);

        private static void InvalidateWindow(IntPtr hand , ref RECT lprcUpdate)
        {
            //RedrawWindow(intPtr, IntPtr.Zero, IntPtr.Zero,
            //0x0400/*RDW_FRAME*/ | 0x0100/*RDW_UPDATENOW*/
            //| 0x0001/*RDW_INVALIDATE*/);
       
            //RedrawWindow(hand, ref lprcUpdate, IntPtr.Zero,
            // RedrawWindowFlags.Frame/*RDW_FRAME*/ | RedrawWindowFlags.UpdateNow/*RDW_UPDATENOW*/
            // | RedrawWindowFlags.Invalidate/*RDW_INVALIDATE*/);

            RedrawWindow(hand, ref lprcUpdate, IntPtr.Zero,                 
            RedrawWindowFlags.InternalPaint| RedrawWindowFlags.NoInternalPaint| RedrawWindowFlags.UpdateNow| RedrawWindowFlags.AllChildren);
        }

        //    ViewerWindow viewerWindow;
        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            ViewerWindow viewerWindow = (ViewerWindow)this.Owner;
            IntPtr hand = (new System.Windows.Interop.WindowInteropHelper(viewerWindow)).Handle;

            EnumChildWindows(hand, new EnumWindowsProc(EnumMouseWheel), (IntPtr)e.Delta);
        }
    
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ViewerWindow viewerWindow = (ViewerWindow)this.Owner;
            IntPtr hand = (new System.Windows.Interop.WindowInteropHelper(viewerWindow)).Handle;

            System.Windows.Point p = e.GetPosition(this);

            EnumChildWindows(hand, new EnumWindowsProc(EnumMouseLeftBDown), (IntPtr)MakeLParam((int)p.X, (int)p.Y));
        }

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ViewerWindow viewerWindow = (ViewerWindow)this.Owner;
            IntPtr hand = (new System.Windows.Interop.WindowInteropHelper(viewerWindow)).Handle;
            System.Windows.Point p = e.GetPosition(this);
            EnumChildWindows(hand, new EnumWindowsProc(EnumMouseLeftBUp), (IntPtr)MakeLParam((int)p.X, (int)p.Y));
        }

        public static string GetClassNameOfWindow(IntPtr hwnd)
        {
            string className = "";
            StringBuilder classText = null;
            try
            {
                int cls_max_length = 256;
                classText = new StringBuilder("", cls_max_length + 5);
                GetClassName(hwnd, classText, cls_max_length + 2);

                if (!String.IsNullOrEmpty(classText.ToString()) && !String.IsNullOrWhiteSpace(classText.ToString()))
                    className = classText.ToString();
            }
            catch (Exception ex)
            {
                className = ex.Message;
            }
            finally
            {
                classText = null;
            }
            return className;
        }
    
        public static bool EnumMouseWheel(int hWnd, int lParam)
        {
            string wtext;
            StringBuilder wtextb = new StringBuilder("", 256);
            GetWindowText((IntPtr)hWnd, wtextb, 256);
            wtext = wtextb.ToString();
            string wclass = GetClassNameOfWindow((IntPtr)hWnd);
            if (wtext == "Message" || wtext.Contains("xls") || wtext.Contains("PowerPoint") || wtext.Contains("ppt")
                 || wtext.Contains("Vertical") || wtext.Contains("Horizontal") || wclass.Contains("EXCEL7"))
            {
                System.Drawing.Point defPnt = new System.Drawing.Point();
                GetCursorPos(ref defPnt);
                RECT rect = new RECT();
                GetWindowRect((IntPtr)hWnd, ref rect);
                if (defPnt.X >= rect.Left && defPnt.X <= rect.Right &&
                    defPnt.Y >= rect.Top && defPnt.Y <= rect.Bottom)
                {
                    ScreenToClient((IntPtr)hWnd, ref defPnt);
                    PostMessage((IntPtr)hWnd, WM_MOUSEWHEEL, MakeLParam(0, lParam), MakeLParam(defPnt.X, defPnt.Y));
                    OW_Global_Ref.Focus();
                }
            }
            else if (wtext.Contains("AVScrollView")) // for Acrobat Previewer Handle
            {
                System.Drawing.Point defPnt = new System.Drawing.Point();
                GetCursorPos(ref defPnt);
                RECT rect = new RECT();
                GetWindowRect((IntPtr)hWnd, ref rect);
                if (defPnt.X >= rect.Left && defPnt.X <= rect.Right &&
                    defPnt.Y >= rect.Top && defPnt.Y <= rect.Bottom)
                {
                    IntPtr hChild1 = GetWindow((IntPtr)hWnd, 5); // 5 - GW_CHILD
                    if (hChild1 != null)
                    {
                        // here is the tricky part for Acrobat Previewer Window
                        // Acrobat Preview Window have:
                        //  "Acrobat Preview Window"
                        //      |--  AVScrollView Window
                        //          |--  "Unknown" MainView Window
                        //          |--  "" Right Scrollbar
                        //          |--  "" Bottom Scrollbar

                        // hChild1 : "Unknown" MainView Window
                        GetWindowRect((IntPtr)hChild1, ref rect);
                        if (defPnt.X >= rect.Left && defPnt.X <= rect.Right &&
                            defPnt.Y >= rect.Top && defPnt.Y <= rect.Bottom)
                        {
                            ScreenToClient((IntPtr)hChild1, ref defPnt);
                            PostMessage((IntPtr)hChild1, WM_MOUSEWHEEL, MakeLParam(0, lParam), MakeLParam(defPnt.X, defPnt.Y));
                        }
                    }
                    OW_Global_Ref.Focus();
                }
            }
            return true;
        }

        public static bool EnumMouseLeftBDown(int hWnd, int lParam)
        {
            string wtext;
            StringBuilder wtextb = new StringBuilder("", 256);
            GetWindowText((IntPtr)hWnd, wtextb, 256);
            wtext = wtextb.ToString();
            string wclass = GetClassNameOfWindow((IntPtr)hWnd);
            if (wtext == "Message" || wtext.Contains("PowerPoint") || wtext.Contains("ppt")) 
            {
                System.Drawing.Point defPnt = new System.Drawing.Point();
                GetCursorPos(ref defPnt);
                RECT rect = new RECT();
                GetWindowRect((IntPtr)hWnd, ref rect);
                if (defPnt.X >= rect.Left && defPnt.X <= rect.Right &&
                    defPnt.Y >= rect.Top && defPnt.Y <= rect.Bottom)
                {
                    ScreenToClient((IntPtr)hWnd, ref defPnt);
                    PostMessage((IntPtr)hWnd, WM_LBUTTONDOWN, MakeLParam(0, 1), MakeLParam(defPnt.X, defPnt.Y));                
                    OW_Global_Ref.Focus();
                }
            }
            else if (wtext.Contains("xlsx") ||
                     wtext.Contains("xls") ||
                     wtext.Contains("xltx") ||
                     wtext.Contains("xlt") || 
                     wtext.Contains("xlsb") || wclass.Contains("EXCEL7"))
            {
                System.Drawing.Point defPnt = new System.Drawing.Point();
                GetCursorPos(ref defPnt);
                RECT rect = new RECT();
                GetWindowRect((IntPtr)hWnd, ref rect);
                if (defPnt.X >= rect.Left && defPnt.X <= rect.Right &&
                    defPnt.Y >= rect.Top && defPnt.Y <= rect.Bottom)
                {
                    ScreenToClient((IntPtr)hWnd, ref defPnt);

                    PostMessage((IntPtr)hWnd, WM_LBUTTONDOWN, MakeLParam(0, 1), MakeLParam(defPnt.X, defPnt.Y));

                    EnableWindow((IntPtr)hWnd, false);
                    OW_Global_Ref.Focus();
                }
            }
            else if (wtext.Contains("Acrobat"))
            {
                System.Drawing.Point defPnt = new System.Drawing.Point();
                GetCursorPos(ref defPnt);
                RECT rect = new RECT();
                GetWindowRect((IntPtr)hWnd, ref rect);
                if (defPnt.X >= rect.Left && defPnt.X <= rect.Right &&
                    defPnt.Y >= rect.Top && defPnt.Y <= rect.Bottom)
                {
                    ScreenToClient((IntPtr)hWnd, ref defPnt);
                    PostMessage((IntPtr)hWnd, WM_LBUTTONDOWN, MakeLParam(0, 1), MakeLParam(defPnt.X, defPnt.Y));
                    OW_Global_Ref.Focus();
                }
            }
            else if (wtext.Contains("Vertical") || wtext.Contains("Horizontal"))
            {
                System.Drawing.Point defPnt = new System.Drawing.Point();
                GetCursorPos(ref defPnt);
                RECT rect = new RECT();
                GetWindowRect((IntPtr)hWnd, ref rect);
                if (defPnt.X >= rect.Left && defPnt.X <= rect.Right &&
                    defPnt.Y >= rect.Top && defPnt.Y <= rect.Bottom)
                {
                    ScreenToClient((IntPtr)hWnd, ref defPnt);
                    // SendMessage((IntPtr)hWnd, WM_PARENTNOTIFY, MakeLParam((int)WM_LBUTTONDOWN, 0), MakeLParam(defPnt.X, defPnt.Y));
                    IntPtr hChild = GetWindow((IntPtr)hWnd, 5); // 5 - GW_CHILD
                    if (hChild != null)
                        PostMessage((IntPtr)hChild, WM_LBUTTONDOWN, MakeLParam(0, 1), MakeLParam(defPnt.X, defPnt.Y));
                    OW_Global_Ref.Focus();
                }
            }
            else if (wtext.Contains("AVScrollView")) // for Acrobat Previewer Handle
            {
                System.Drawing.Point defPnt = new System.Drawing.Point();
                GetCursorPos(ref defPnt);
                RECT rect = new RECT();
                GetWindowRect((IntPtr)hWnd, ref rect);
                if (defPnt.X >= rect.Left && defPnt.X <= rect.Right &&
                    defPnt.Y >= rect.Top && defPnt.Y <= rect.Bottom)
                {
                    // SendMessage((IntPtr)hWnd, WM_LBUTTONDOWN, MakeLParam(0, 1), MakeLParam(defPnt.X, defPnt.Y));
                    IntPtr hChild1 = GetWindow((IntPtr)hWnd, 5); // 5 - GW_CHILD
                    if (hChild1 != null)
                    {
                        // here is the tricky part for Acrobat Previewer Window
                        // Acrobat Preview Window have:
                        //  "Acrobat Preview Window"
                        //      |--  AVScrollView Window
                        //          |--  "Unknown" MainView Window
                        //          |--  "" Right Scrollbar
                        //          |--  "" Bottom Scrollbar

                        // hChild1 : "Unknown" MainView Window
                        if (hChild1 != null)
                        {
                            GetWindowRect((IntPtr)hChild1, ref rect);
                            if (defPnt.X >= rect.Left && defPnt.X <= rect.Right &&
                                defPnt.Y >= rect.Top && defPnt.Y <= rect.Bottom)
                            {
                                ScreenToClient((IntPtr)hChild1, ref defPnt);
                                PostMessage((IntPtr)hChild1, WM_LBUTTONDOWN, MakeLParam(0, 1), MakeLParam(defPnt.X, defPnt.Y));
                            }
                        }
                        IntPtr hChild2 = GetWindow((IntPtr)hChild1, 2); // 1 - GW_HWNDNEXT
                        if (hChild2 != null)
                        {
                            GetWindowRect((IntPtr)hChild2, ref rect);
                            if (defPnt.X >= rect.Left && defPnt.X <= rect.Right &&
                                defPnt.Y >= rect.Top && defPnt.Y <= rect.Bottom)
                            {
                                ScreenToClient((IntPtr)hChild2, ref defPnt);
                                PostMessage((IntPtr)hChild2, WM_LBUTTONDOWN, MakeLParam(0, 1), MakeLParam(defPnt.X, defPnt.Y));
                            }
                        }
                        IntPtr hChild3 = GetWindow((IntPtr)hChild1, 1); // 1 - GW_HWNDLAST
                        if (hChild3 != null)
                        {
                            GetWindowRect((IntPtr)hChild3, ref rect);
                            if (defPnt.X >= rect.Left && defPnt.X <= rect.Right &&
                                defPnt.Y >= rect.Top && defPnt.Y <= rect.Bottom)
                            {
                                ScreenToClient((IntPtr)hChild3, ref defPnt);
                                PostMessage((IntPtr)hChild3, WM_LBUTTONDOWN, MakeLParam(0, 1), MakeLParam(defPnt.X, defPnt.Y));
                            }
                        }
                    }
                    OW_Global_Ref.Focus();
                }
            }

            return true;
        }

        public static bool EnumMouseLeftBUp(int hWnd, int lParam)
        {
            string wtext;
            StringBuilder wtextb = new StringBuilder("", 256);
            GetWindowText((IntPtr)hWnd, wtextb, 256);
            wtext = wtextb.ToString();
            string wclass = GetClassNameOfWindow((IntPtr)hWnd);
            if (wtext == "Message" || 
                wtext.Contains("xlsx") ||
                wtext.Contains("xls") ||
                wtext.Contains("xltx") ||
                wtext.Contains("xlt") ||
                wtext.Contains("xlsb") || 
                wtext.Contains("ppt") || 
                wtext.Contains("Vertical") || 
                wtext.Contains("Horizontal") ||
                    wtext.Contains("PowerPoint") ||
                    wclass.Contains("EXCEL7"))
            {
                System.Drawing.Point defPnt = new System.Drawing.Point();
                GetCursorPos(ref defPnt);
                RECT rect = new RECT();
                GetWindowRect((IntPtr)hWnd, ref rect);
                if (defPnt.X >= rect.Left && defPnt.X <= rect.Right &&
                    defPnt.Y >= rect.Top && defPnt.Y <= rect.Bottom)
                {
                    ScreenToClient((IntPtr)hWnd, ref defPnt);
                    PostMessage((IntPtr)hWnd, WM_LBUTTONUP, MakeLParam(0, 0), MakeLParam(defPnt.X, defPnt.Y));
                    OW_Global_Ref.Focus();

                }
            }
            else if (wtext.Contains("Acrobat"))
            {
                System.Drawing.Point defPnt = new System.Drawing.Point();
                GetCursorPos(ref defPnt);
                RECT rect = new RECT();
                GetWindowRect((IntPtr)hWnd, ref rect);
                if (defPnt.X >= rect.Left && defPnt.X <= rect.Right &&
                    defPnt.Y >= rect.Top && defPnt.Y <= rect.Bottom)
                {
                    ScreenToClient((IntPtr)hWnd, ref defPnt);
                    PostMessage((IntPtr)hWnd, WM_LBUTTONUP, MakeLParam(0, 0), MakeLParam(defPnt.X, defPnt.Y));
                    OW_Global_Ref.Focus();
                }
            }
            else if (wtext.Contains("AVScrollView")) // for Acrobat Previewer Handle
            {
                System.Drawing.Point defPnt = new System.Drawing.Point();
                GetCursorPos(ref defPnt);
                RECT rect = new RECT();
                GetWindowRect((IntPtr)hWnd, ref rect);
                if (defPnt.X >= rect.Left && defPnt.X <= rect.Right &&
                    defPnt.Y >= rect.Top && defPnt.Y <= rect.Bottom)
                {
                    // SendMessage((IntPtr)hWnd, WM_LBUTTONUP, MakeLParam(0, 0), MakeLParam(defPnt.X, defPnt.Y));
                    IntPtr hChild1 = GetWindow((IntPtr)hWnd, 5); // 5 - GW_CHILD
                    if (hChild1 != null)
                    {
                        // here is the tricky part for Acrobat Previewer Window
                        // Acrobat Preview Window have:
                        //  "Acrobat Preview Window"
                        //      |--  AVScrollView Window
                        //          |--  "Unknown" MainView Window
                        //          |--  "" Right Scrollbar
                        //          |--  "" Bottom Scrollbar

                        // hChild1 : "Unknown" MainView Window
                        if (hChild1 != null)
                        {
                            GetWindowRect((IntPtr)hChild1, ref rect);
                            if (defPnt.X >= rect.Left && defPnt.X <= rect.Right &&
                                defPnt.Y >= rect.Top && defPnt.Y <= rect.Bottom)
                            {
                                ScreenToClient((IntPtr)hChild1, ref defPnt);
                                PostMessage((IntPtr)hChild1, WM_LBUTTONUP, MakeLParam(0, 0), MakeLParam(defPnt.X, defPnt.Y));
                            }
                        }

                        IntPtr hChild2 = GetWindow((IntPtr)hChild1, 2); // 1 - GW_HWNDNEXT
                        if (hChild2 != null)
                        {
                            GetWindowRect((IntPtr)hChild2, ref rect);
                            if (defPnt.X >= rect.Left && defPnt.X <= rect.Right &&
                                defPnt.Y >= rect.Top && defPnt.Y <= rect.Bottom)
                            {
                                ScreenToClient((IntPtr)hChild2, ref defPnt);
                                PostMessage((IntPtr)hChild2, WM_LBUTTONUP, MakeLParam(0, 0), MakeLParam(defPnt.X, defPnt.Y));
                            }
                        }
                        IntPtr hChild3 = GetWindow((IntPtr)hChild1, 1); // 1 - GW_HWNDLAST
                        if (hChild3 != null)
                        {
                            GetWindowRect((IntPtr)hChild3, ref rect);
                            if (defPnt.X >= rect.Left && defPnt.X <= rect.Right &&
                                defPnt.Y >= rect.Top && defPnt.Y <= rect.Bottom)
                            {
                                ScreenToClient((IntPtr)hChild3, ref defPnt);
                                PostMessage((IntPtr)hChild3, WM_LBUTTONUP, MakeLParam(0, 0), MakeLParam(defPnt.X, defPnt.Y));
                            }
                        }
                    }
                    OW_Global_Ref.Focus();
                }
            }

            return true;
        }

        private void Window_FocusableChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
  
        }
    }

}
