using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.ui.windows.mainWindow.model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using SkydrmLocal.rmc.ui.utils;
using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.fileSystem.basemodel;
using System.Windows.Controls;

namespace SkydrmLocal.rmc.ui.windows.serviceManagerWindow.viewModel
{
    public class ViewModelServiceManagerWindow : INotifyPropertyChanged
    {
        // Application
        private SkydrmLocalApp App = (SkydrmLocalApp)SkydrmLocalApp.Current;
   
        public event PropertyChangedEventHandler PropertyChanged;

        private Window win;

        private string userName;

        public string UserName
        {
            get { return userName; }
            set { userName = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("UserName"));
            }
        }

        private string avatarText;

        public string AvatarText
        {
            get { return avatarText; }
            set
            {
                avatarText = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("avatarText"));
            }
        }


        private string avatarTextColor;

        public string AvatarTextColor
        {
            get { return avatarTextColor; }
            set
            {
                avatarTextColor = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("avatarTextColor"));
            }
        }

        private string userStorageSpace;

        public string UserStorageSpace
        {
            get { return userStorageSpace; }
            set { userStorageSpace = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("UserStorageSpace"));
            }
        }

        private string avatarBackground;

        public string AvatarBackground
        {
            get { return avatarBackground; }
            set
            {
                avatarBackground = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("avatarBackground"));
            }
        }

        private ObservableCollection<FileStatus> nxlFileList = new ObservableCollection<FileStatus>();

        public ObservableCollection<FileStatus> NxlFileList
        {
            get { return nxlFileList; }
            set { nxlFileList = value; }
        }
            
        // network status
        private bool isNetworkAvailable;

        public bool IsNetworkAvailable
        {
            get { return isNetworkAvailable; }
            set
            {
                isNetworkAvailable = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsNetworkAvailable"));
            }
        }

        public void OnFileStatusChanged(EnumNxlFileStatus status, string fileName)
        {          
            try
            {
                bool isFound = false;
                FileStatus temp = null;

                foreach (FileStatus one in NxlFileList)
                {
                    if (String.Equals(one.FileName, fileName, StringComparison.CurrentCultureIgnoreCase))
                    {
                        isFound = true;
                        temp = one;
                        break;
                    }
                }

                if (isFound)
                {
                    temp.Status = status;
                    temp.DateTime = DateTime.Now;
                    //if (NxlFileList.Remove(temp))
                    //{
                    //    NxlFileList.Insert(0, temp);
                    //}
                }
                else
                {
                    temp = new FileStatus(fileName, status, DateTime.Now);
                    NxlFileList.Insert(0, temp);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public ViewModelServiceManagerWindow(Window window)
        {
            this.win = window;

            InitData();

            UserName = GetUserName();

            AvatarText = CommonUtils.ConvertNameToAvatarText(UserName, " ");

            AvatarBackground = CommonUtils.SelectionBackgroundColor(UserName);

            AvatarTextColor = CommonUtils.SelectionTextColor(UserName);

            UserStorageSpace = GetUserStorageSpace();

            // Register MyVaultQuata updated
            App.MyVaultQuataUpdated += () => {

                UserStorageSpace = GetUserStorageSpace();
            };

            App.UserNameUpdated += () =>
            {

              UserName = GetUserName();

              AvatarText = CommonUtils.ConvertNameToAvatarText(UserName, " ");

              AvatarBackground = CommonUtils.SelectionBackgroundColor(UserName);

              AvatarTextColor = CommonUtils.SelectionTextColor(UserName);

            };
    
        }


        /// <summary>
        /// IRecentTouchedFile convert to FileStatus
        /// </summary>
        /// <param name="recentTouchedFile">Need to converted object</param>
        /// <returns>Converted Object</returns>
        private FileStatus Convert(IRecentTouchedFile recentTouchedFile)
        {
                return 
                new FileStatus(
                recentTouchedFile.Name,
                (EnumNxlFileStatus)Enum.Parse(typeof(EnumNxlFileStatus), recentTouchedFile.Status),         
                recentTouchedFile.LastModifiedTime);
        }

        public void InitData()
        {
            nxlFileList.Clear();

            IRecentTouchedFile[] recentTouchedFiles = App.UserRecentTouchedFile.List();

            foreach (IRecentTouchedFile one in recentTouchedFiles)
            {
                NxlFileList.Add(Convert(one));
            }
        }

        //public void Refresh_Data()
        //{
        //    UserName = GetUserName();
        //    UserStorageSpace = GetUserStorageSpace();

        //    AvatarText =CommonUtils.ConvertNameToAvatarText(UserName," ");

        //    AvatarBackground = CommonUtils.SelectionBackgroundColor(UserName);

        //    AvatarTextColor = CommonUtils.SelectionTextColor(UserName);
        //}

        private string GetUserName()
        {
           return App.Rmsdk.User.Name;
        }

        private string GetUserStorageSpace()
        {
           string result = "{0} of {1} used"; 
           Int64 usageSize = 0;
           Int64 totalSize = 0;

           App.Rmsdk.User.GetMyDriveInfo(ref usageSize, ref totalSize);

          string usage = CommonUtils.GetSizeString(usageSize);
          string total = CommonUtils.GetSizeString(totalSize);

          result=string.Format(result, usage, total);

           return result;
        }
    }
}
