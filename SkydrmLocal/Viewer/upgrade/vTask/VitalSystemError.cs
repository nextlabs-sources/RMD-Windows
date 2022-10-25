//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Windows;
//using System.Windows.Threading;
//using Viewer.upgrade.application;
//using Viewer.upgrade.session;
//using Viewer.upgrade.ui.common.viatalError.view;
//using Viewer.upgrade.ui.common.viatalError.viewModel;
//using Viewer.upgrade.utils;

//namespace Viewer.upgrade.vTask
//{
//    public class VitalSystemError : VTask
//    {
//        private UInt64 mErrorCode;
//        private IApplication mApplication;
//        private Task<UInt64> mTask_Action;
//        private Task<UInt64> mTask_Dismiss;
//        private ViatalErrorWindow mViatalErrorWindow;

//        public override Task<ulong> ActionResult { get { return mTask_Action; } } 
//        public override Window Window { get { return mViatalErrorWindow; } }

//        public VitalSystemError(UInt64 errorCode)
//        {
//            this.mErrorCode = errorCode;
//            mApplication = (IApplication)Application.Current;
//        }

//        public override Task<ulong> Dismiss()
//        {
//           return mTask_Dismiss = mTask_Action.ContinueWith<UInt64>((x) => {
//               // Was cancellation already requested?
//               if (mApplication.Token.IsCancellationRequested)
//               {
//                   mApplication.Token.ThrowIfCancellationRequested();
//               }

//               if (x.Result != ErrorCode.SUCCEEDED)
//               {
//                   return x.Result;
//               }
//               mApplication.SystemApplication.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate () {
//                   mViatalErrorWindow?.Close();
//               }));
//               return ErrorCode.SUCCEEDED;
//            }, mApplication.Token);
//        }

//        public override Task<UInt64> Action()
//        {
//           return mTask_Action = Task.Factory.StartNew<UInt64>((x) => {
//               // Was cancellation already requested?
//               if (mApplication.Token.IsCancellationRequested)
//               {
//                   mApplication.Token.ThrowIfCancellationRequested();
//               }

//               UInt64 errorCode = (UInt64)x;
//               switch (errorCode)
//               {
//                   default:
//                       mApplication.SystemApplication.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate () {
//                            mViatalErrorWindow = new ViatalErrorWindow(errorCode);
//                            mViatalErrorWindow.Show();
//                       }));
//                       break;
//               }
//               return ErrorCode.SUCCEEDED;
//            } ,mErrorCode, mApplication.Token,TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
//        }
//    }
//}
