using System;
using System.Collections.Generic;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.upgrade.application;
using Viewer.upgrade.cookie;
using Viewer.upgrade.file.basic;
using Viewer.upgrade.ui.common.viewerWindow.view;
using Viewer.upgrade.utils;

namespace Viewer.upgrade.session
{
    public class Session : ISession
    {
        public string Id => mId;
      //  public UInt64 StatusCode => mStatusCode;
        public Cookie Cookie => mCookie;
     
        private string mId = string.Empty;
        private Cookie mCookie;
       // private UInt64 mStatusCode = SessionStatusCode.DEFAULT;
      
        public Session(Cookie cookie)
        {
            mId = System.Guid.NewGuid().ToString();
            mCookie = cookie;
        }

        public void DoIntent()
        {
            switch (mCookie.Intent)
            {
                case EnumIntent.View:
                    VieweFile(mCookie);
                    break;
                case EnumIntent.Unknown:
                    break;
            }
        }

        private void FeatureAction()
        {

        }

        private void VieweFile(Cookie cookie)
        {
            ViewerWindow viewerWindow = new ViewerWindow(cookie);
            viewerWindow.Show();
        }
        //  private Task<UInt64> mTask_CreateSession;

        //public Session(string[] cmdArgs)
        //{
        //    this.mCmdArgs = cmdArgs;
        //   // mApplication = (IApplication)System.Windows.Application.Current;
        //}

        //public Task<UInt64> Create()
        //{
        //    return new TaskFactory<UInt64>().StartNew(() => {
        //        return ErrorCode.SYSTEM_INTERNAL_ERROR;
        //    });

        //mTask_CreateSession = mApplication.Task_VitalInitialize.ContinueWith<UInt64>((x, y) =>
        //{
        //    // Was cancellation already requested?
        //    if (mApplication.Token.IsCancellationRequested)
        //    {
        //        mApplication.Token.ThrowIfCancellationRequested();
        //    }

        //    mId = System.Guid.NewGuid().ToString();
        //   // mApplication.Sessions.TryAdd(mId, this);

        //    if (x.Result != ErrorCode.SUCCEEDED)
        //    {
        //        VTask vTask = new VitalSystemError(x.Result);
        //        vTask.Action();
        //        mVTasks.Add(vTask);
        //        return x.Result;
        //    }

        //    Cookie cookie;
        //    if (!Cookie.ParseCmdArgs(mCmdArgs, out cookie))
        //    {
        //        mStatusCode |= SessionStatusCode.PARSER_CMD_FAILED;
        //        VTask vTask = new VitalSystemError(ErrorCode.PARSER_CMD_FAILED);
        //        vTask.Action();
        //        mVTasks.Add(vTask);
        //        return ErrorCode.PARSER_CMD_FAILED;
        //    }

        //    if (!(Path.IsPathRooted(cookie.FilePath) && File.Exists(cookie.FilePath) && Path.HasExtension(cookie.FilePath)))
        //    {
        //        VTask vTask = new VitalSystemError(ErrorCode.FILE_DOES_NOT_EXIST);
        //        vTask.Action();
        //        mVTasks.Add(vTask);
        //        return ErrorCode.FILE_DOES_NOT_EXIST;
        //    }

        //    mCookies.Add(cookie);

        //    ISession session = y as ISession;
        //    foreach (Cookie item in session.Cookies)
        //    {
        //        VTask vTask = new ViewFile(item);
        //        vTask.Action();
        //        mVTasks.Add(vTask);
        //    }
        //    return ErrorCode.SUCCEEDED;
        //}, this, mApplication.Token);

        //    return mTask_CreateSession;
        //}


        //public UInt64 Delete()
        //{
        //    mApplication.Sessions.TryRemove(mId , out ISession value);
        //    //bool result = false;
        //    //try
        //    //{
        //    //    if ((mStatusCode & VieSessionStatusCode.DELETE_REGISTRY_ON_CLOSE) == VieSessionStatusCode.DELETE_REGISTRY_ON_CLOSE)
        //    //    {
        //    //        DeleteRecordInRegistry(FilePath);
        //    //    }
        //    //    result = true;
        //    //}
        //    //catch (Exception ex)
        //    //{
        //    //}
        //    //return result;
        //    return ErrorCode.SUCCEEDED;
        //}
    }
}
