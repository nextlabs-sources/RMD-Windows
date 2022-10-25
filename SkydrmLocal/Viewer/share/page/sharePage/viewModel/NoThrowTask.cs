using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.share
{
    public class NoThrowTask
    {
        readonly Action action;
        readonly Action finished;
        readonly bool async = false;
        IAsyncResult ar = null;

        public NoThrowTask(Action action)
        {
            async = false;
            this.action = action;
        }

        public NoThrowTask(bool async, Action action)
        {
            this.async = async;
            this.action = action;
        }

        public NoThrowTask(bool async, Action action, Action finished)
        {
            this.async = async;
            this.action = action;
            this.finished = finished;
        }

        public bool IsComplete()
        {
            if (async && ar != null)
            {
                return ar.IsCompleted;
            }
            else
            {
                return true;
            }
        }

        public void Do()
        {
            if (!async)
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception e)
                {
                    ((ViewerApp)ViewerApp.Current).Log.Error(e);
                }
                OnFinish();

            }
            else
            {
                ar = action?.BeginInvoke((ar) => {
                    OnFinish();
                },
                null);
            }

        }

        private void OnFinish()
        {
            try
            {
                finished?.Invoke();
            }
            catch (Exception e)
            {
                ((ViewerApp)ViewerApp.Current).Log.Error(e);
            }
        }
    }
}
