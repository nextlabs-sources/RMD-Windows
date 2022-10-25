//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Windows.Threading;
//using Viewer.upgrade.application;
//using Viewer.upgrade.file.basic;
//using Viewer.upgrade.session;
//using Viewer.upgrade.utils;
//using System.Windows;
//using Viewer.upgrade.cookie;
//using Viewer.upgrade.ui.common.viewerWindow.view;
//using Viewer.upgrade.ui.common.viatalError.view;

//namespace Viewer.upgrade.vTask
//{
//    public class ViewFile : VTask
//    {
//        private Cookie mCookie;
//        private Task<UInt64> mTask_Action;
//        private Task<UInt64> mTask_Dismiss;
//        private IApplication mApplication;
//        private Window mWindow;

//        public override Task<ulong> ActionResult { get { return mTask_Action; } }
//        public override Window Window { get { return mWindow; } }

//        public ViewFile(Cookie cookie)
//        {
//            this.mCookie = cookie;
//            this.mApplication = (IApplication)Application.Current;
//        }

//        public override Task<ulong> Action()  
//        {
//            return mTask_Action = Task<UInt64>.Factory.StartNew((object x)=> {
//                // Was cancellation already requested?
//                if (mApplication.Token.IsCancellationRequested)
//                {
//                    mApplication.Token.ThrowIfCancellationRequested();
//                }

//                Cookie cookie = x as Cookie;
//                IntPtr hWnd = IntPtr.Zero;
//                if(ErrorCode.SUCCEEDED == ToolKit.GetHwndFromRegistry(cookie.FilePath, out hWnd))
//                {

//                    if (Win32Common.IsWindow(hWnd))
//                    {
//                        if (Win32Common.BringWindowToTopEx(hWnd))
//                        {
//                            //Int64 code = ToolKit.RunningMode();
//                            //if (code == 0)
//                            //{
//                                mApplication.SystemApplication.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
//                                {
//                                    mApplication.SystemApplication.Shutdown(0);
//                                }));
//                            //}
//                            return ErrorCode.WINDOW_REDIRECT;
//                        }
//                        else
//                        {
//                            ToolKit.DeleteHwndFromRegistry(cookie.FilePath);
//                        }
//                    }
//                    else
//                    {
//                        ToolKit.DeleteHwndFromRegistry(cookie.FilePath);
//                    }

//                    //string wtext;
//                    //StringBuilder wtextb = new StringBuilder("", 256);
//                    //Win32Common. GetWindowText((IntPtr)hWnd, wtextb, 256);
//                    //wtext = wtextb.ToString();

//                    //if (Win32Common.IsWindow(hWnd) && string.Equals(CultureStringInfo.Viewer_window_title, wtext, StringComparison.CurrentCultureIgnoreCase))
//                    //{
//                    //    if (Win32Common.BringWindowToTopEx(hWnd))
//                    //    {
//                    //        return ErrorCode.WINDOW_REDIRECT;
//                    //    }
//                    //    else
//                    //    {
//                    //        ToolKit.DeleteHwndFromRegistry(cookie.FilePath);
//                    //    }
//                    //}
//                    //else
//                    //{
//                    //    ToolKit.DeleteHwndFromRegistry(cookie.FilePath);
//                    //}
//                }

//                mApplication.SystemApplication.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
//                {
//                    mWindow = new ViewerWindow(new _BaseFile(cookie));
//                    mWindow.Show();
//                }));

//                //UInt64 res = ErrorCode.SUCCEEDED;
//                //_BaseFile _BaseFile = new _BaseFile(cookie);
//                //res = _BaseFile.Open();
//                //if (ErrorCode.SUCCEEDED == res)
//                //{
//                //    mApplication.SystemApplication.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
//                //    {
//                //        mWindow = new ViewerWindow(_BaseFile);
//                //        mWindow.Show();
//                //    }));
//                //}
//                //else
//                //{
//                //    //open failed
//                //    mApplication.SystemApplication.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
//                //    {
//                //        ViatalErrorWindow mViatalErrorWindow = new ViatalErrorWindow(res);
//                //        mViatalErrorWindow.Show();
//                //    }));
//                //}


//                return ErrorCode.SUCCEEDED;
//            }, mCookie, mApplication.Token, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
//        }

//        public override Task<UInt64> Dismiss()
//        {
//            return mTask_Dismiss = mTask_Action.ContinueWith<UInt64>((x)=> 
//            {
//                if (x.Result != ErrorCode.SUCCEEDED)
//                {
//                    return x.Result;
//                }

//                mApplication.SystemApplication.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate () {
//                    mWindow?.Close();
//                }));

//                return ErrorCode.SUCCEEDED;
//            }, mApplication.Token);
//        }
//    }
//}
