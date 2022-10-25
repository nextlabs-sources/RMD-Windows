using SkydrmLocal.rmc.fileSystem.basemodel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.ui.windows.mainWindow.model
{
    public class FileStatus : INotifyPropertyChanged
    {
        public string FileName { get; set; }

        public EnumNxlFileStatus status;

        // Note: the default value is "1/1/0001" for DateTime
        //last update time
        private DateTime dateTime;
        public DateTime DateTime
        {
            get { return dateTime; }
            set
            {
                dateTime = value;

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DateTime"));
            }
        }

        public FileStatus(string fileName, EnumNxlFileStatus uploadStatus, DateTime DateTime)
        {
            this.FileName = fileName;
            this.status = uploadStatus;
            this.dateTime = DateTime;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public EnumNxlFileStatus Status
        {
            get { return status; }

            set
            {
                status = value;

                // trigger event
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Status"));
            }
        }
    }

}
