using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Viewer.upgrade.application;
using Viewer.upgrade.file.basic.utils;
using Viewer.upgrade.utils.overlay.utils;

namespace Viewer.upgrade.ui.common.overlayWindow.viewModel
{
    public class ViewModel
    {
        private ViewerApp mApplication;
        private log4net.ILog mLog;
        private WatermarkInfo mWatermarkInfo;
        private double DeviceIndependentUnit = Convert.ToDouble(1) / Convert.ToDouble(96);
        private float DpiX = 96;
        private float DpiY = 96;
        private double DisplayMonitorX = SystemParameters.WorkArea.Width;
        private double DisplayMonitorY = SystemParameters.WorkArea.Height;
        private Window mOverlayWindow;
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
        private delegate bool EnumWindowsProc(int hWnd, int lParam);

        public ViewModel(Window overlayWindow)
        {
            WatermarkInfo.Builder builder = new WatermarkInfo.Builder();
            WatermarkInfo watermarkInfo = builder.DefaultSet().Build();
            this.mWatermarkInfo = watermarkInfo;
            this.mOverlayWindow = overlayWindow;
            Initialize();
        }

        public ViewModel(WatermarkInfo watermarkInfo, Window overlayWindow)
        {
            this.mWatermarkInfo = watermarkInfo;
            this.mOverlayWindow = overlayWindow;
            Initialize();
        }

        private void Initialize()
        {
            mApplication = (ViewerApp)ViewerApp.Current;
            mLog = mApplication.Log;
            mLog.Info("\t\t OverlayWindow \r\n");
            using (Graphics graphics = Graphics.FromHwnd(IntPtr.Zero))
            {
                DpiX = graphics.DpiX;
                DpiY = graphics.DpiY;
            }
        }

        public void Window_Loaded(Grid host)
        {
            host.Children.Add(CreateOverlayVisual());
        }

        public void SetRect(System.Windows.Point point, int actualWidth, int actualHeight)
        {
            IntPtr OverLay_hwnd = new WindowInteropHelper(mOverlayWindow).Handle;
            if (null != OverLay_hwnd)
            {
                int width = Convert.ToInt32(Math.Round((actualWidth) * (DeviceIndependentUnit * DpiX)));
                int height = Convert.ToInt32(Math.Round((actualHeight) * (DeviceIndependentUnit * DpiY)));
                MoveWindow((int)OverLay_hwnd,
                            Convert.ToInt32(Math.Round(point.X)),
                            Convert.ToInt32(Math.Round(point.Y)),
                            width, height, false);
            }
        }

        public void Window_MouseWheel(MouseWheelEventArgs e)
        {
            Window Window = mOverlayWindow.Owner;
            IntPtr hand = (new System.Windows.Interop.WindowInteropHelper(Window)).Handle;
            EnumChildWindows(hand, new EnumWindowsProc(EnumMouseWheel), (IntPtr)e.Delta);
        }

        public void Window_MouseLeftButtonDown(MouseButtonEventArgs e)
        {
            Window Window = mOverlayWindow.Owner;
            IntPtr hand = (new System.Windows.Interop.WindowInteropHelper(Window)).Handle;

            System.Windows.Point p = e.GetPosition(mOverlayWindow);

            EnumChildWindows(hand, new EnumWindowsProc(EnumMouseLeftBDown), (IntPtr)MakeLParam((int)p.X, (int)p.Y));
        }

        public void Window_MouseLeftButtonUp(MouseButtonEventArgs e)
        {
            Window Window = mOverlayWindow.Owner;
            IntPtr hand = (new System.Windows.Interop.WindowInteropHelper(Window)).Handle;
            System.Windows.Point p = e.GetPosition(mOverlayWindow);
            EnumChildWindows(hand, new EnumWindowsProc(EnumMouseLeftBUp), (IntPtr)MakeLParam((int)p.X, (int)p.Y));
        }

        public Canvas CreateOverlayVisual()
        {
            mLog.Info("\t\t CreateOverlayVisual \r\n");
            Canvas canvas = new Canvas();
            try
            {
               OverlayUtils.DrawWatermark(mWatermarkInfo , ref canvas);
            }
            catch (Exception ex)
            {

            }
            return canvas;
        }

        //private TextBlock CreateOverlayText(WatermarkInfo watermarkInfo)
        //{
        //    return WrapperTextBlock(watermarkInfo);
        //}

        //private TextBlock WrapperTextBlock(WatermarkInfo watermarkInfo)
        //{
        //    TextBlock element = new TextBlock();
        //    BrushConverter brushConverter = new BrushConverter();
        //    System.Windows.Media.Brush brush = (System.Windows.Media.Brush)brushConverter.ConvertFromString(watermarkInfo.FontColor);
        //    element.Foreground = brush;
        //    // test for rotation --- 45
        //    element.LayoutTransform = new RotateTransform(45);
        //    element.Text = watermarkInfo.Text;
        //    element.FontFamily = new System.Windows.Media.FontFamily(watermarkInfo.FontName);
        //    element.FontSize = watermarkInfo.FontSize;
        //    element.Opacity = watermarkInfo.TransparentRatio / 100f > 1.0 ? 1.0 : watermarkInfo.TransparentRatio / 100f;
        //    return element;
        //}

        private string GetClassNameOfWindow(IntPtr hwnd)
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

        private bool EnumMouseWheel(int hWnd, int lParam)
        {
            string wtext;
            StringBuilder wtextb = new StringBuilder("", 256);
            GetWindowText((IntPtr)hWnd, wtextb, 256);
            wtext = wtextb.ToString();
            string wclass = GetClassNameOfWindow((IntPtr)hWnd);
            if (wtext == "Message" || wtext.Contains("xls") || wtext.Contains("PowerPoint") || wtext.Contains("ppt")
                 || wtext.Contains("Vertical") || wtext.Contains("Horizontal") || wclass.Contains("EXCEL7") || wtext.Contains("Chrome Legacy Window"))
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
                    mOverlayWindow.Focus();
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
                    mOverlayWindow.Focus();
                }
            }
            return true;
        }

        private bool EnumMouseLeftBDown(int hWnd, int lParam)
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
                    mOverlayWindow.Focus();
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
                    mOverlayWindow.Focus();
                }
            }
            else if (wtext.Contains("Acrobat") || wtext.Contains("Chrome Legacy Window"))
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
                    mOverlayWindow.Focus();
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
                    mOverlayWindow.Focus();
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
                    mOverlayWindow.Focus();
                }
            }

            return true;
        }

        private bool EnumMouseLeftBUp(int hWnd, int lParam)
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
                wclass.Contains("EXCEL7")
                )
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
                    mOverlayWindow.Focus();

                }
            }
            else if (wtext.Contains("Acrobat") || wtext.Contains("Chrome Legacy Window"))
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
                    mOverlayWindow.Focus();
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
                    mOverlayWindow.Focus();
                }
            }
            return true;
        }

        private IntPtr MakeLParam(int LoWord, int HiWord)
        {
            Int64 i = (HiWord << 16) | (LoWord & 0xffff);
           // i = i & 0xffffffff;
            return new IntPtr(i);
        }

        [DllImport("user32.dll")]
        static extern IntPtr PostMessage(IntPtr hwnd, UInt32 msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern long GetClassName(IntPtr hwnd, StringBuilder lpClassName, long nMaxCount);

        [DllImport("user32")]
        static extern bool EnumChildWindows(IntPtr window, EnumWindowsProc callback, IntPtr lParam);

        [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern long GetWindowText(IntPtr hwnd, StringBuilder lpString, long cch);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll")]
        static extern bool GetCursorPos(ref System.Drawing.Point pPoint);

        [DllImport("user32.dll")]
        static extern bool ScreenToClient(IntPtr hWnd, ref System.Drawing.Point lpPoint);

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

        [DllImport("user32.dll")]
        static extern int MoveWindow(int hwnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool EnableWindow(IntPtr hWnd, bool bEnable);

        [DllImport("user32.dll")]
        static extern int SetFocus(IntPtr hwnd);
    }
}
