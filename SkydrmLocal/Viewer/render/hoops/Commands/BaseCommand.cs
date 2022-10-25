using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Viewer.render.hoops.ThreeDView;

namespace Viewer.hoops.Commands
{
    /// <summary>
    /// Base class for Commands
    /// </summary>
    abstract class BaseCommand : ICommand
    {
        // MainWindow instance
        protected ThreeDViewer _win;

        public BaseCommand(ThreeDViewer window)
        {
            if (window == null)
            {
                throw new ArgumentNullException("window is null");
            }
            _win = window;
        }

        #region ICommand members

        /// <summary>
        /// Occurs when changes occur that affect whether or not the command should execute.
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// Defines the method that determines whether the command can execute in its current state.
        /// 
        /// Here are executable by default.
        /// </summary>
        /// <param name="parameter">Command parameters, can is passed null</param>
        /// <returns> true if this command can executed; otherwise, false.</returns>
        public bool CanExecute(object parameter)
        {
            return true;
        }

        /// <summary>
        ///  Defines the method to be called when the command is invoked.
        ///  
        /// Override method to provide the command implementation.
        /// </summary>
        /// <param name="parameter"> the command parameter, can pass null if don't need to pass data.</param>
        public abstract void Execute(object parameter);

        #endregion // ICommand members
    }
}
