using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.upgrade.utils;
using Alphaleonis.Win32.Filesystem;

namespace Viewer.upgrade.cookie
{
    public class IntentParser
    {
        const int MAX_PATH = 260;
        public static void GetInformation(string cmdArg, out bool AllowEdit, out bool IsFromMainWindow, out bool Share)
        {
            AllowEdit = false;
            IsFromMainWindow = false;
            Share = false;
            try
            {
                if (string.IsNullOrWhiteSpace(cmdArg))
                {
                    throw new ArgumentNullException();
                }

                Int32 information;
                if (Int32.TryParse(cmdArg, out information))
                {
                    if ((information & FileStatus.ClickFromMainWindow) == FileStatus.ClickFromMainWindow)
                    {
                        IsFromMainWindow = true;
                    }
                    if ((information & FileStatus.Edit) == FileStatus.Edit)
                    {
                        AllowEdit = true;
                    }
                    if ((information & FileStatus.Share) == FileStatus.Share)
                    {
                        Share = true;
                    }
                }
                else
                {
                    //throw new ArgumentException();
                }
            }
            catch (Exception ex)
            {
                AllowEdit = false;
                IsFromMainWindow = false;
                Share = false;
                throw ex;
            }
        }

        //public static string ModuleName(string cmdArg)
        //{
        //    string result = string.Empty;
        //    try
        //    {
        //        if (string.IsNullOrWhiteSpace(cmdArg))
        //        {
        //            throw new ArgumentNullException();
        //        }
        //        // first option muse is either View or FromMainInfo
        //        ModuleName = cmdArg;
        //        if (Path.GetExtension(ModuleName).EndsWith(".exe", StringComparison.CurrentCultureIgnoreCase))
        //        {
        //            result = ErrorCode.SUCCEEDED;
        //        }
        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        public static EnumIntent GetIntent(string cmdArg)
        {
            EnumIntent result = EnumIntent.Unknown;
            try
            {
                if (string.IsNullOrWhiteSpace(cmdArg))
                {
                    throw new ArgumentNullException();
                }

                string intention = cmdArg;
                if (String.Equals(intention, "-View", StringComparison.CurrentCultureIgnoreCase))
                {
                    result = EnumIntent.View;
                }
                else
                {

                }
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static string VerifyFilePath(string cmdArg)
        {
            ViewerApp application = (ViewerApp)ViewerApp.Current;
            application.Log.InfoFormat("\t\t File Path:{0}\n",cmdArg);
            string result = string.Empty;
            try
            {
                if (string.IsNullOrWhiteSpace(cmdArg))
                {
                    throw new ArgumentNullException();
                }

                if ((cmdArg.Length >= MAX_PATH) && (!cmdArg.StartsWith(@"\\?\", StringComparison.CurrentCultureIgnoreCase)))
                {
                    cmdArg = @"\\?\" + cmdArg;
                    application.Log.InfoFormat("\t\t File path too long add prefix \\\\?\\, produced:{0}.\n", cmdArg);
                }

                System.IO.FileStream fileStream = File.Open(cmdArg, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                fileStream.Close();

                //if (File.Exists(cmdArg))
                //{
                //if (Path.IsPathRooted(cmdArg))
                //{
                if (Path.HasExtension(cmdArg))
                        {
                            result = cmdArg;
                        }
                        else
                        {
                            throw new NotSupportedException();
                        }
                    //}
                    //else
                    //{
                    //    throw new NotSupportedException();
                    //}
                //}
                //else
                //{
                //    throw new FileNotFoundException();
                //}

                return result;
            }
            catch (ArgumentNullException ex)
            {
                application.Log.Error(ex);
                throw ex;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                application.Log.Error(ex);
                throw ex;
            }
            catch (ArgumentException ex)
            {
                application.Log.Error(ex);
                throw ex;
            }
            catch (UnauthorizedAccessException ex)
            {
                application.Log.Error(ex);
                throw ex;
            }
            catch (NotSupportedException ex)
            {
                application.Log.Error(ex);
                throw ex;
            }
            catch (Exception ex)
            {
                application.Log.Error(ex);
                throw ex;
            }
        }

        private static bool CheckPath(string path)
        {
            bool res = false;
            if (path.Length >= MAX_PATH)
            {
                res = CheckFile_LongPath(path);
            }
            else
            {
                res = File.Exists(path);
                //Console.WriteLine("   *  File: " + path + " does not exist.");
            }
            return res;
        }

        private static bool CheckFile_LongPath(string path)
        {
            string[] subpaths = path.Split('\\');
            StringBuilder sbNewPath = new StringBuilder(subpaths[0]);
            // Build longest subpath that is less than MAX_PATH characters
            for (int i = 1; i < subpaths.Length; i++)
            {
                if (sbNewPath.Length + subpaths[i].Length >= MAX_PATH)
                {
                    subpaths = subpaths.Skip(i).ToArray();
                    break;
                }
                sbNewPath.Append("\\" + subpaths[i]);
            }
            DirectoryInfo dir = new DirectoryInfo(sbNewPath.ToString());
            bool foundMatch = dir.Exists;
            if (foundMatch)
            {
                // Make sure that all of the subdirectories in our path exist.
                // Skip the last entry in subpaths, since it is our filename.
                // If we try to specify the path in dir.GetDirectories(), 
                // We get a max path length error.
                int i = 0;
                while (i < subpaths.Length - 1 && foundMatch)
                {
                    foundMatch = false;
                    foreach (DirectoryInfo subDir in dir.GetDirectories())
                    {
                        if (subDir.Name == subpaths[i])
                        {
                            // Move on to the next subDirectory
                            dir = subDir;
                            foundMatch = true;
                            break;
                        }
                    }
                    i++;
                }
                if (foundMatch)
                {
                    foundMatch = false;
                    // Now that we've gone through all of the subpaths, see if our file exists.
                    // Once again, If we try to specify the path in dir.GetFiles(), 
                    // we get a max path length error.
                    foreach (FileInfo fi in dir.GetFiles())
                    {
                        if (fi.Name == subpaths[subpaths.Length - 1])
                        {
                            foundMatch = true;
                            break;
                        }
                    }
                }
            }
            // If we didn't find a match, write to the console.
            if (!foundMatch)
            {
                Console.WriteLine("   *  File: " + path + " does not exist.");
            }
            return foundMatch;
        }

        //public static bool BuildVieSession(string[] cmdArgs, out VieSession startUpParamater)
        //{
        //    bool result = false;
        //    try
        //    {
        //        startUpParamater = null;
        //        if (null == cmdArgs)
        //        {
        //            return result;
        //        }
        //        string moduleName = string.Empty;
        //        bool isClickFromNxrmApp = false;
        //        bool allowEdit = false;
        //        bool allowShare = false;
        //        string filePath = string.Empty;
        //        EnumIntent intent = EnumIntent.Unknown;

        //        if (cmdArgs.Length == 3)
        //        {
        //            // parse begin
        //            if (GetModuleName(cmdArgs, out moduleName))
        //            {
        //                if (GetIntent(cmdArgs, out intent))
        //                {
        //                    if (intent == EnumIntent.View)
        //                    {
        //                        if (GetFilePath(cmdArgs, out filePath))
        //                        {
        //                            result = true;
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //        else if (cmdArgs.Length == 4)
        //        {
        //            //        // parse begin
        //            if (GetModuleName(cmdArgs, out moduleName))
        //            {
        //                if (GetIntent(cmdArgs, out intent))
        //                {
        //                    if (intent == EnumIntent.View)
        //                    {
        //                        if (GetInformation(cmdArgs, out allowEdit, out isClickFromNxrmApp, out allowShare))
        //                        {
        //                            if (GetFilePath(cmdArgs, out filePath))
        //                            {
        //                                result = true;
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //        startUpParamater = new VieSession(moduleName, isClickFromNxrmApp, allowEdit, allowShare, filePath, intent, cmdArgs);
        //    }
        //    catch (Exception ex)
        //    {
        //        startUpParamater = null;
        //    }

        //    return result;
        //}

        public class FileStatus
        {
            // 0         0                          0                         0 
            // 1         1                          1                         1
            // x         x                          x                         x
            //          [0]no share                [0]no edit                [0] click not from main window    
            //          [1]yes share               [1]yes edit               [1] click from main window    
            public static Int32 ClickFromMainWindow = 0x01;
            public static Int32 Edit = 0x02;
            public static Int32 Share = 0x04;
        }
    }
}
