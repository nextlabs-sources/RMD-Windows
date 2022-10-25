using nxrmvirtualdrivelib.core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nxrmvirtualdrivelib.logger
{
    public class ConsoleLogger : ILogger
    {
        private string m_componentName;

        public string ComponentName
        {
            get => m_componentName;
        }

        public ILogger CreateLogger(string componentName)
        {
            this.m_componentName = componentName;
            return this;
        }

        public void Info(object message)
        {
            Console.WriteLine(message);
        }

        public void Info(object message, Exception exception)
        {
            Console.WriteLine(message);
            Console.WriteLine(exception);
        }

        public void Warn(object message)
        {
            Console.WriteLine(message);
        }

        public void Warn(object message, Exception exception)
        {
            Console.WriteLine(message);
            Console.WriteLine(exception);
        }

        public void Debug(object message)
        {
            Console.WriteLine(message);
        }

        public void Debug(object message, Exception exception)
        {
            Console.WriteLine(message);
            Console.WriteLine(exception);
        }

        public void Error(object message)
        {
            Console.WriteLine(message);
        }

        public void Error(object message, Exception exception)
        {
            Console.WriteLine(message);
            Console.WriteLine(exception);
        }

        public void Fatal(object message)
        {
            Console.WriteLine(message);
        }

        public void Fatal(object message, Exception exception)
        {
            Console.WriteLine(message);
            Console.WriteLine(exception);
        }
    }
}
