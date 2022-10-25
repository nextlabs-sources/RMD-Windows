using nxrmvirtualdrivelib.core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nxrmvirtualdrivelib.logger
{
    public class Log4NetLogger : ILogger
    {
        private log4net.ILog m_log;
        private string m_componentName;

        public string ComponentName
        {
            get => m_componentName;
        }

        public ILogger CreateLogger(string componentName)
        {
            m_componentName = componentName;
            m_log = log4net.LogManager.GetLogger(componentName);
            return this;
        }

        public void Debug(object message)
        {
            m_log?.Debug(message);
        }

        public void Debug(object message, Exception exception)
        {
            m_log?.Debug(message, exception);
        }

        public void Error(object message)
        {
            m_log?.Error(message);
        }

        public void Error(object message, Exception exception)
        {
            m_log?.Error(message, exception);
        }

        public void Fatal(object message)
        {
            m_log?.Fatal(message);
        }

        public void Fatal(object message, Exception exception)
        {
            m_log?.Fatal(message, exception);
        }

        public void Info(object message)
        {
            m_log?.Info(message);
        }

        public void Info(object message, Exception exception)
        {
            m_log?.Info(message, exception);
        }

        public void Warn(object message)
        {
            m_log?.Warn(message);
        }

        public void Warn(object message, Exception exception)
        {
            m_log?.Warn(message, exception);
        }
    }
}
