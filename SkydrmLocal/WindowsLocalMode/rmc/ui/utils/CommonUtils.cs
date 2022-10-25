using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.database2.table.project;
using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.featureProvider.common;
using SkydrmLocal.rmc.fileSystem;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.fileSystem.myvault;
using SkydrmLocal.rmc.fileSystem.project;
using SkydrmLocal.rmc.fileSystem.sharedWithMe;
using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.search;
using SkydrmLocal.rmc.ui.components.RightsDisplay.model;
using SkydrmLocal.rmc.ui.components.ValiditySpecify;
using SkydrmLocal.rmc.ui.components.ValiditySpecify.model;
using SkydrmLocal.rmc.ui.pages.model;
using SkydrmLocal.rmc.ui.windows;
using SkydrmLocal.rmc.ui.windows.mainWindow.model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static SkydrmLocal.rmc.fileSystem.project.ProjectRepo;
using SkydrmLocal.rmc.exception;

namespace SkydrmLocal.rmc.ui.utils
{
    public class CommonUtils
    {
        private static SkydrmLocalApp App = (SkydrmLocalApp)SkydrmLocalApp.Current;
        //2017-06-09-15-59-59
        public const int NxlFileDatetimeLength = 19;

        private const string NXL = ".nxl";

        private const string DOLLAR_USER = "$(User)";
        private const string DOLLAR_BREAK = "$(Break)";
        private const string DOLLAR_DATE = "$(Date)";
        private const string DOLLAR_TIME = "$(Time)";

        public const string DATE_FORMATTER = "M/d/yyyy h:mm tt";

        //This function can be dangerous, 
        //if file name is qwerttyuiopasdfghjklzxvcvbnm.doc.nxl , 
        //it can not distinguish the file has timeStamp
        //it will broke original file name
        public static string DateTimeConverter(string datename)
        {
            string Time = "";
            //2017-06-09-15-59-59
            if (datename.Length > NxlFileDatetimeLength)
            {
                string temp = datename.Substring(0, datename.LastIndexOf('.'));
                if (temp.LastIndexOf('.') > NxlFileDatetimeLength)
                {
                    temp = temp.Substring(0, temp.LastIndexOf('.'));
                    temp = temp.Substring(temp.Length - 19, 19);
                }
                else
                {
                    temp = temp.Substring(temp.Length - 19, 19);
                }
                int year = int.Parse(temp.Substring(0, 4));
                int month = int.Parse(temp.Substring(5, 2));
                int day = int.Parse(temp.Substring(8, 2));
                int hour = int.Parse(temp.Substring(11, 2));
                int minute = int.Parse(temp.Substring(14, 2));
                int second = int.Parse(temp.Substring(17, 2));
                DateTime dt = new DateTime(year, month, day, hour, minute, second);
                Time = dt.ToString(DATE_FORMATTER);
            }
            return Time;
        }

        public static long DateTimeToTimestamp(DateTime time)
        {
            DateTime startDateTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1, 0, 0, 0));
            return Convert.ToInt64((time - startDateTime).TotalMilliseconds);
        }
        public static string TimestampToDateTime(long time)
        {
            DateTime startDateTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1, 0, 0, 0));
            DateTime newTime = startDateTime.AddMilliseconds(time);
            return newTime.ToString("MMMM dd, yyyy");
        }

        //for mainWindow
        public static string TimestampToDateTime2(long time)
        {
            DateTime startDateTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1, 0, 0, 0));
            DateTime newTime = startDateTime.AddMilliseconds(time);
            return newTime.ToString(DATE_FORMATTER);
        }


        public static string ConvertNameToAvatarText(string name, string rule)
        {
            name = name.Trim();
            if (string.IsNullOrEmpty(name))
            {
                return "";
            }
            string letter = "";
            if (string.Equals(" ", rule))
            {
                rule = "\\s+";
            }
            String[] split = System.Text.RegularExpressions.Regex.Split(name, rule);
            if (split.Length > 1)
            {

                letter = string.Concat(letter, split[0].Substring(0, 1).ToUpper());
                //fix bug 51464
                //letter = string.Concat(letter," ");
                letter = string.Concat(letter, split[split.Length - 1].Substring(0, 1).ToUpper());
            } else
            {
                letter = name.Substring(0, 1).ToUpper();
            }
            return letter;
        }

        public static void OpenSkyDrmWeb()
        {
            try
            {
                string url = App.DBFunctionProvider.GetUrl();
                if (string.IsNullOrEmpty(url))
                {
                    url = App.Config.Router;
                }
                System.Diagnostics.Process.Start(url);
            }
            catch (Exception e)
            {
                App.Log.Error("Error happend in Get Url:", e);
            }
        }

        public static string ConvertFileSize(Int64 size)
        {

            if (size <= 0)
            {
                return "0KB";
            }
            else if (0 < size && size < 1024)
            {
                return "1KB";
            }
            else
            {
                return Math.Round(size / (float)1024) + "KB";
            }
        }
        public static string SelectionBackgroundColor(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "#9D9FA2";
            }
            switch (name.Substring(0, 1).ToUpper())
            {
                case "A":
                    return "#DD212B";

                case "B":
                    return "#FDCB8A";

                case "C":
                    return "#98C44A";

                case "D":
                    return "#1A5279";

                case "E":
                    return "#EF6645";

                case "F":
                    return "#72CAC1";

                case "G":
                    return "#B7DCAF";

                case "H":
                    return "#705A9E";

                case "I":
                    return "#FCDA04";

                case "J":
                    return "#ED1D7C";

                case "K":
                    return "#F7AAA5";

                case "L":
                    return "#4AB9E6";

                case "M":
                    return "#603A18";

                case "N":
                    return "#88B8BC";

                case "O":
                    return "#ECA81E";

                case "P":
                    return "#DAACD0";

                case "Q":
                    return "#6D6E73";

                case "R":
                    return "#9D9FA2";

                case "S":
                    return "#B5E3EE";

                case "T":
                    return "#90633D";

                case "U":
                    return "#BDAE9E";

                case "V":
                    return "#C8B58E";

                case "W":
                    return "#F8BDD2";

                case "X":
                    return "#FED968";

                case "Y":
                    return "#F69679";

                case "Z":
                    return "#EE6769";

                case "0":
                    return "#D3E050";

                case "1":
                    return "#D8EBD5";

                case "2":
                    return "#F27EA9";

                case "3":
                    return "#1782C0";

                case "4":
                    return "#CDECF9";

                case "5":
                    return "#FDE9E6";

                case "6":
                    return "#FCED95";

                case "7":
                    return "#F99D21";

                case "8":
                    return "#F9A85D";

                case "9":
                    return "#BCE2D7";

                default:
                    return "#333333";
            }
        }

        public static string SelectionTextColor(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "#ffffff";
            }
            switch (name.Substring(0, 1).ToUpper())
            {
                case "A":
                    return "#ffffff";

                case "B":
                    return "#8F9394";

                case "C":
                    return "#ffffff";

                case "D":
                    return "#ffffff";

                case "E":
                    return "#ffffff";

                case "F":
                    return "#ffffff";

                case "G":
                    return "#8F9394";

                case "H":
                    return "#ffffff";

                case "I":
                    return "#8F9394";

                case "J":
                    return "#ffffff";

                case "K":
                    return "#ffffff";

                case "L":
                    return "#ffffff";

                case "M":
                    return "#ffffff";

                case "N":
                    return "#ffffff";

                case "O":
                    return "#ffffff";

                case "P":
                    return "#ffffff";

                case "Q":
                    return "#ffffff";

                case "R":
                    return "#ffffff";

                case "S":
                    return "#ffffff";


                case "T":
                    return "#ffffff";


                case "U":
                    return "ffffff";


                case "V":
                    return "#ffffff";


                case "W":
                    return "#ffffff";


                case "X":
                    return "#8F9394";


                case "Y":
                    return "#ffffff";


                case "Z":
                    return "#ffffff";


                case "0":
                    return "#8F9394";


                case "1":
                    return "#8F9394";


                case "2":
                    return "#ffffff";


                case "3":
                    return "#ffffff";

                case "4":
                    return "#8F9394";


                case "5":
                    return "#8F9394";


                case "6":
                    return "#8F9394";

                case "7":
                    return "#ffffff";

                case "8":
                    return "#ffffff";

                case "9":
                    return "#8F9394";

                default:
                    return "#ffffff";

            }
        }

        public enum SizeUnitMode
        {
            Byte,

            KiloByte,

            MegaByte,

            GigaByte,
        }

        public static System.String GetSizeString(System.Double parameter)
        {

            System.Double size = 0;
            SizeUnitMode sizeUnitMode;
            size = GetSize(parameter, out sizeUnitMode);
            string result = string.Empty;

            switch (sizeUnitMode)
            {
                case SizeUnitMode.Byte:
                    result = System.String.Format("{0} B", size.ToString("N", System.Globalization.CultureInfo.InvariantCulture));
                    break;
                case SizeUnitMode.KiloByte:
                    result = System.String.Format("{0} KB", size.ToString("N", System.Globalization.CultureInfo.InvariantCulture));
                    break;
                case SizeUnitMode.MegaByte:
                    result = System.String.Format("{0} MB", size.ToString("N", System.Globalization.CultureInfo.InvariantCulture));
                    break;
                case SizeUnitMode.GigaByte:
                    result = System.String.Format("{0} GB", size.ToString("N", System.Globalization.CultureInfo.InvariantCulture));

                    break;
                default:
                    break;
            }
            return result;
        }

        public static System.Double GetSize(System.Double size, out SizeUnitMode sizeUnitMode)
        {

            if (size >= 0 && size < 1024)
            {
                sizeUnitMode = SizeUnitMode.Byte;
                return size;
            }

            System.Double kb = size / 1024;
            if (kb >= 1 && kb < 1024)
            {
                sizeUnitMode = SizeUnitMode.KiloByte;
                return kb;
            }


            System.Double mb = size / (1024 * 1024);
            if (mb >= 1 && mb < 1024)
            {
                sizeUnitMode = SizeUnitMode.MegaByte;
                return mb;
            }

            System.Double gb = size / (1024 * 1024 * 1024);
            if (gb >= 1)
            {
                sizeUnitMode = SizeUnitMode.GigaByte;
                return gb;
            }

            sizeUnitMode = SizeUnitMode.Byte;
            return size;
        }

        public static void ConvertWatermark2DisplayStyle(string value, ref StringBuilder sb)
        {
            // value = " aa$(user)bb$(tmie)cc$(date)dd$(break)eee"

            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            char[] array = value.ToCharArray();
            // record preset value begin index
            int beginIndex = -1;
            // record preset value end index
            int endIndex = -1;
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] == '$')
                {
                    beginIndex = i;
                }
                else if (array[i] == ')')
                {
                    endIndex = i;
                }

                if (beginIndex != -1 && endIndex != -1 && beginIndex < endIndex)
                {


                    sb.Append(value.Substring(0, beginIndex));


                    // judge if is preset
                    string subStr = value.Substring(beginIndex, endIndex - beginIndex + 1);

                    if (subStr.Equals(DOLLAR_USER))
                    {
                        //sb.Append(" ");
                        sb.Append(ReplaceDollar(DOLLAR_USER));
                        sb.Append(" ");
                    }
                    else if (subStr.Equals(DOLLAR_BREAK))
                    {
                        sb.Append(ReplaceDollar(DOLLAR_BREAK));
                    }
                    else if (subStr.Equals(DOLLAR_DATE))
                    {
                        //sb.Append(" ");
                        sb.Append(ReplaceDollar(DOLLAR_DATE));
                        sb.Append(" ");
                    }
                    else if (subStr.Equals(DOLLAR_TIME))
                    {
                        //sb.Append(" ");
                        sb.Append(ReplaceDollar(DOLLAR_TIME));
                        sb.Append(" ");
                    }
                    else
                    {
                        sb.Append(subStr);
                    }

                    // quit
                    break;
                }
            }

            if (beginIndex == -1 || endIndex == -1 || beginIndex > endIndex) // have not preset
            {
                sb.Append(value);

            }
            else if (beginIndex < endIndex)
            {
                if (endIndex + 1 < value.Length)
                {
                    // Converter the remaining by recursive
                    ConvertWatermark2DisplayStyle(value.Substring(endIndex + 1), ref sb);
                }
            }

        }

        private static string ReplaceDollar(string dollarStr)
        {
            string ret = "";
            switch (dollarStr)
            {
                case DOLLAR_USER:
                    ret = App.Rmsdk.User.Email;
                    break;
                case DOLLAR_DATE:
                    ret = DateTime.Now.ToString("dd MMMM yyyy");
                    break;
                case DOLLAR_TIME:
                    ret = DateTime.Now.ToString("hh:mm");
                    break;
                case DOLLAR_BREAK:
                    ret = " ";
                    break;
                default:
                    break;
            }

            return ret;
        }

        public static uint GetUserId(string loginJson)
        {
            uint nRet = 0;
            try
            {
                JObject jo = (JObject)JsonConvert.DeserializeObject(loginJson);

                if (jo != null && jo.ContainsKey("extra"))
                {
                    JObject job = (JObject)jo.GetValue("extra");
                    if (job != null && job.ContainsKey("userId"))
                    {
                        nRet = (uint)job.GetValue("userId");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                App.Log.Error("Parse user id failed!");
            }


            return nRet;
        }

        public static string ConvertList2String(List<string> list)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < list.Count; i++)
            {
                if (i == list.Count - 1)
                {
                    sb.Append(list[i]);
                }
                else
                {
                    sb.Append(list[i]);
                    sb.Append(",");
                }
            }

            return sb.ToString();
        }

        public static string ApplicationFindResource(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }
            try
            {
                string ResourceString = SkydrmLocalApp.Current.FindResource(key).ToString();
                return ResourceString;
            }
            catch (Exception e)
            {
                App.Log.Error(e.Message);
                return string.Empty;
            }

        }

        public static void RemoveListFile(IList<INxlFile> listfiles, IList<INxlFile> copyfiles, string toRemoveName)
        {
            INxlFile toFind = null;
            foreach (var one in listfiles)
            {
                if (one.Name == toRemoveName)
                {
                    toFind = one;
                    break;
                }
            }

            if (toFind != null)
            {
                listfiles.Remove(toFind);
                copyfiles.Remove(toFind);
            }
        }

        /// <summary>
        /// Used to update the specified file status of current working listview. 
        ///   -- Fix bug 51388, because there are may different file node objects when re-get from local db 
        ///   since the refresh which caused by switch treeview item during downloading.
        /// </summary>
        public static void UpdateListViewFileStatus(IList<INxlFile> listfiles, IList<INxlFile> copyfiles, INxlFile specified)
        {
            foreach (var one in listfiles)
            {
                if (one.Name == specified.Name)
                {
                    one.FileStatus = specified.FileStatus;
                    one.Location = specified.Location;
                    break;
                }
            }

            foreach (var one in copyfiles)
            {
                if (one.Name == specified.Name)
                {
                    one.FileStatus = specified.FileStatus;
                    one.Location = specified.Location;
                    break;
                }
            }

        }

        // Fix bug that handle project "leave a copy" file delete issue, sometimes need to delete twice.
        // Need update the listView leave copy file with new node after uploading and sync.
        public static void MergeLeaveAcopyFile(IList<INxlFile> syncResults, 
            IList<INxlFile> nxlFileList, 
            IList<INxlFile> copyFileList)
        {
            // Record the updated node index, <OldIndex, NewIndex>
            Dictionary<int, int> IndexMap = new Dictionary<int, int>();

            for (int i = 0; i < syncResults.Count; i++)
            {
                int newIndex = -1;
                int oldIndex = -1;

                for (int j = 0; j < nxlFileList.Count; j++)
                {
                    if (syncResults[i].Name == nxlFileList[j].Name
                        && syncResults[i].FileStatus == EnumNxlFileStatus.CachedFile
                        && syncResults[i].IsCreatedLocal == false
                        && nxlFileList[j].IsCreatedLocal == true)
                    {
                        newIndex = i;
                        oldIndex = j;
                        break;
                    }
                }

                if (newIndex != -1 && oldIndex != -1)
                {
                    IndexMap.Add(oldIndex, newIndex);
                }

            }

            foreach (var one in IndexMap)
            {
                // Replace old node(ProjectLocalFile) using new node(ProjectFile).
                nxlFileList[one.Key] = syncResults[one.Value];
                copyFileList[one.Key] = syncResults[one.Value];
            }

        }


        /// <summary>
        /// Merge listview ui nodes after sync from rms.
        /// </summary>
        public static void MergeListView(
            IList<INxlFile> newfiles,
            IList<INxlFile> oldfiles,
            IList<INxlFile> oldCopyfiles)
        {
            for (int i = oldfiles.Count - 1; i >= 0; i--)
            {
                INxlFile one = oldfiles[i];

                INxlFile find = null;
                for (int j = 0; j < newfiles.Count; j++)
                {
                    INxlFile f = newfiles[j];
                    if (one.Name == f.Name)
                    {
                        // "one" belongs to old set and belongs to new set
                        find = f;
                        break;
                    }
                }

                // "one" belongs to old set but not belongs to new set -- should remove it from old set.
                // should exclude local created file
                // If local file status is waiting upload | uploading we should keep it in the file list.
                if (find == null 
                    && one.FileStatus != EnumNxlFileStatus.WaitingUpload 
                    && one.FileStatus != EnumNxlFileStatus.Uploading
                    && one.FileStatus != EnumNxlFileStatus.UploadFailed)
                {
                    oldfiles.Remove(one);
                    oldCopyfiles.Remove(one);
                }
            }


            for (int j = 0; j < newfiles.Count; j++)
            {
                INxlFile one = newfiles[j];

                INxlFile find = null;
                for (int i = 0; i < oldfiles.Count; i++)
                {
                    INxlFile f = oldfiles[i];
                    if (one.Name == f.Name)
                    {
                        // "one" belongs to new set and belongs to old set
                        find = f;
                        break;
                    }
                }

                // "one" belongs to new set but not belongs to old set -- should add it 
                if (find == null)
                {
                    oldfiles.Add(one);
                    oldCopyfiles.Add(one);
                }
            }
        }

        /// <summary>
        /// Merge listview ui nodes after sync from rms, and record the added & removed files.
        /// </summary>
        public static void MergeListView(
            IList<INxlFile> newfiles, 
            IList<INxlFile> oldfiles, 
            IList<INxlFile> oldCopyfiles, 
            out IList<INxlFile> addFiles, 
            out IList<INxlFile> removeFiles)
        {
            addFiles = new List<INxlFile>();
            removeFiles = new List<INxlFile>();

            for (int i = oldfiles.Count - 1; i >= 0; i--)
            {
                INxlFile one = oldfiles[i];

                INxlFile find = null;
                for(int j = 0; j < newfiles.Count; j++)
                {
                    INxlFile f = newfiles[j];
                    if (one.Name == f.Name && one.RawDateModified ==  f.RawDateModified)
                    {
                        // "one" belongs to old set and belongs to new set
                        find = f;
                        break;
                    }
                }

                // "one" belongs to old set but not belongs to new set -- should remove it from old set.
                // should exclude local created file
                // If local file status is waiting upload | uploading we should keep it in the file list.
                if (find == null 
                    && one.FileStatus != EnumNxlFileStatus.WaitingUpload 
                    && one.FileStatus != EnumNxlFileStatus.Uploading
                    && one.FileStatus != EnumNxlFileStatus.UploadFailed)
                {
                    oldfiles.Remove(one);
                    oldCopyfiles.Remove(one);

                    removeFiles.Add(one);
                }
            }


            for(int j = 0; j < newfiles.Count; j++)
            {
                INxlFile one = newfiles[j];

                INxlFile find = null;
                for (int i = 0; i < oldfiles.Count; i++)
                {
                    INxlFile f = oldfiles[i];
                    if (one.Name == f.Name && one.RawDateModified == f.RawDateModified)
                    {
                        // "one" belongs to new set and belongs to old set
                        find = f;
                        break;
                    }
                }

                // "one" belongs to new set but not belongs to old set -- should add it 
                if (find == null)
                {
                    oldfiles.Add(one);
                    oldCopyfiles.Add(one);

                    addFiles.Add(one);
                }
            }

        }

        // Judge the nxl file if is supported.
        public static EnumFileType GetFileTypeByExtension(string fileName)
        {

            if (string.IsNullOrEmpty(fileName))
            {
                return EnumFileType.FILE_TYPE_NOT_SUPPORT;
            }

            if (fileName.EndsWith(".nxl"))
            {
                fileName = fileName.Substring(0, fileName.Length - NXL.Length);
            }

            if (!fileName.Contains("."))
            {
                return EnumFileType.FILE_TYPE_NOT_SUPPORT;
            }

            // Should handle the Team center automatically rename for the postfix, like: Jack.prt-2019-01-24-07-04-28.1
            if (!StringHelper.Replace(fileName,
                                     out fileName,
                                     StringHelper.TIMESTAMP_PATTERN + StringHelper.POSTFIX_1_249,
                                     RegexOptions.IgnoreCase))
            {
                // Should handle the Team center automatically rename for the postfix, like: Jack-2019-4-10-07-40-13.prt.1
                StringHelper.Replace(fileName,
                                    out fileName,
                                    StringHelper.POSTFIX_1_249,
                                    RegexOptions.IgnoreCase);
            }

            string ext = fileName.Substring(fileName.LastIndexOf('.'));

            // Hoops
            if (string.Equals(ext, ".hsf", StringComparison.CurrentCultureIgnoreCase))
            {
                return EnumFileType.FILE_TYPE_HOOPS_3D;
            }

            // Audio & Vedio
            if (string.Equals(ext, ".mp3", StringComparison.CurrentCultureIgnoreCase))
            {
                return EnumFileType.FILE_TYPE_AUDIO;
            }

            if (string.Equals(ext, ".mp4", StringComparison.CurrentCultureIgnoreCase))
            {
                return EnumFileType.FILE_TYPE_VIDEO;
            }

            // Image
            if (string.Equals(ext, ".png", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".gif", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".jpg", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".bmp", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".tif", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".tiff", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".jpe", StringComparison.CurrentCultureIgnoreCase)
                )
            {
                return EnumFileType.FILE_TYPE_IMAGE;
            }

            // Note: for other office file: .docm, .xltm, .xlsm, .potm, .dotm --- preview don't support currently.

            // word
            if (string.Equals(ext, ".docx", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".doc", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".dot", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".dotx", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".rtf", StringComparison.CurrentCultureIgnoreCase)
               /* || string.Equals(ext, ".vsd", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".vsdx", StringComparison.CurrentCultureIgnoreCase)*/)
            {
                return EnumFileType.FILE_TYPE_OFFICE;
            }

            // ppt
            if (string.Equals(ext, ".pptx", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".ppt", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".ppsx", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".potx", StringComparison.CurrentCultureIgnoreCase))
            {
                return EnumFileType.FILE_TYPE_OFFICE;
            }

            // Excel
            if (string.Equals(ext, ".xlsx", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".xls", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".xltx", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".xlt", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".xlsb", StringComparison.CurrentCultureIgnoreCase))
            {
                return EnumFileType.FILE_TYPE_OFFICE;
            }

            // pdf
            if (string.Equals(ext, ".pdf", StringComparison.CurrentCultureIgnoreCase))
            {
                return EnumFileType.FILE_TYPE_PDF;
            }

            // Plain text.
            if (string.Equals(ext, ".cpp", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".htm", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".xml", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".json", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".h", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".js", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".java", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".err", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".m", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".swift", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".txt", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".log", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".sql", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".c", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".py", StringComparison.CurrentCultureIgnoreCase))
            {
                return EnumFileType.FILE_TYPE_PLAIN_TEXT;
            }

            // Using exchange to view directly
            if ( // Common
                string.Equals(ext, ".jt", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".igs", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".stp", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".stl", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".step", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".iges", StringComparison.CurrentCultureIgnoreCase)
                // Solid Edge
                || string.Equals(ext, ".par", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".psm", StringComparison.CurrentCultureIgnoreCase)
                // Parasolid
                || string.Equals(ext, ".x_t", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".x_b", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".xmt_txt", StringComparison.CurrentCultureIgnoreCase)
                // CREO, Pro/Engineer
                || string.Equals(ext, ".prt", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".neu", StringComparison.CurrentCultureIgnoreCase)
                // CATIA
                || string.Equals(ext, ".model", StringComparison.CurrentCultureIgnoreCase)
                // CATIA V6
                || string.Equals(ext, ".3dxml", StringComparison.CurrentCultureIgnoreCase)
                // CATIA V5
                || string.Equals(ext, ".catpart", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".cgr", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".catshape", StringComparison.CurrentCultureIgnoreCase)
                // Solid works
                || string.Equals(ext, ".prt", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".sldprt", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".sldasm", StringComparison.CurrentCultureIgnoreCase)
                // AutoCAD,Inventor,TrueView
                || string.Equals(ext, ".dwg", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".dxf", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".ipt", StringComparison.CurrentCultureIgnoreCase)
                )
            {
                return EnumFileType.FILE_TYPE_HPS_EXCHANGE_3D;
            }

            if (string.Equals(ext, ".vds", StringComparison.CurrentCultureIgnoreCase))
            {
                return EnumFileType.FILE_TYPE_SAP_VDS;
            }

            return EnumFileType.FILE_TYPE_NOT_SUPPORT;
        }

        public enum EnumFileType
        {
            FILE_TYPE_HOOPS_3D,
            FILE_TYPE_HPS_EXCHANGE_3D,
            FILE_TYPE_OFFICE,
            FILE_TYPE_PDF,
            FILE_TYPE_3D_PDF,
            FILE_TYPE_IMAGE,
            FILE_TYPE_PLAIN_TEXT,
            FILE_TYPE_VIDEO,
            FILE_TYPE_AUDIO,
            FILE_TYPE_NOT_SUPPORT,
            FILE_TYPE_SAP_VDS
        }

        #region Sdk Expiration and UI Expiration interconvert
        public static void SdkExpiration2ValiditySpecifyModel(rmc.sdk.Expiration expiration, out IExpiry expiry, out string expireDateValue, bool isUserPreference)
        {
            expiry = new NeverExpireImpl(); ;
            expireDateValue = CultureStringInfo.ValidityWin_Never_Description2;
            switch (expiration.type)
            {
                case rmc.sdk.ExpiryType.NEVER_EXPIRE:
                    expiry = new NeverExpireImpl();
                    expireDateValue = CultureStringInfo.ValidityWin_Never_Description2;
                    break;
                case rmc.sdk.ExpiryType.RELATIVE_EXPIRE:
                    if (isUserPreference)
                    {
                        int years = (int)(expiration.Start >> 32);
                        int months = (int)expiration.Start;
                        int weeks = (int)(expiration.End >> 32);
                        int days = (int)expiration.End;
                        expiry = new RelativeImpl(years, months, weeks, days);

                        DateTime dateStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);
                        string dateRelativeS = dateStart.ToString("MMMM dd, yyyy");

                        if (years == 0 && months == 0 && weeks == 0 && days == 0)
                        {
                            days = 1;
                        }

                        DateTime dateEnd = dateStart.AddYears(years).AddMonths(months).AddDays(7 * weeks + days - 1).AddHours(23).AddMinutes(59).AddSeconds(59);
                        string dateRelativeE = dateEnd.ToString("MMMM dd, yyyy");

                        expireDateValue = dateRelativeS + " To " + dateRelativeE;
                    }
                    else
                    {
                        string dateRelativeS = CommonUtils.TimestampToDateTime(expiration.Start);
                        string dateRelativeE = CommonUtils.TimestampToDateTime(expiration.End);
                        expiry = new RelativeImpl(0, 0, 0, CountDays(Convert.ToDateTime(dateRelativeS).Ticks, Convert.ToDateTime(dateRelativeE).Ticks));

                        expireDateValue = "Until " + dateRelativeE;
                    }
                   
                    break;
                case rmc.sdk.ExpiryType.ABSOLUTE_EXPIRE:
                    string dateAbsoluteS = CommonUtils.TimestampToDateTime(expiration.Start);
                    string dateAbsoluteE = CommonUtils.TimestampToDateTime(expiration.End);
                    expiry = new AbsoluteImpl(expiration.End);
                    expireDateValue = "Until " + dateAbsoluteE;
                    break;
                case rmc.sdk.ExpiryType.RANGE_EXPIRE:
                    string dateRangeS = CommonUtils.TimestampToDateTime(expiration.Start);
                    string dateRangeE = CommonUtils.TimestampToDateTime(expiration.End);
                    expiry = new RangeImpl(expiration.Start, expiration.End);
                    expireDateValue = dateRangeS + " To " + dateRangeE;
                    break;

            }

        }
        private static int CountDays(long startMillis, long endMillis)
        {
            long elapsedTicks = endMillis - startMillis;
            TimeSpan elapsedSpan = new TimeSpan(elapsedTicks);
            return elapsedSpan.Days + 1;
        }

        public static void ValiditySpecifyModel2SdkExpiration(out rmc.sdk.Expiration expiration, IExpiry expiry, string expirationDate, bool isUserPreference)
        {
            expiration = new rmc.sdk.Expiration();

            int exType = expiry.GetOpetion();
            //Get current year,month,day.
            int year = DateTime.Now.Year;
            int month = DateTime.Now.Month;
            int day = DateTime.Now.Day;
            DateTime dateStart = new DateTime(year, month, day, 0, 0, 0);
            switch (exType)
            {
                case 0:
                    INeverExpire neverExpire = (INeverExpire)expiry;
                    expiration.type = rmc.sdk.ExpiryType.NEVER_EXPIRE;
                    break;
                case 1:
                    IRelative relative = (IRelative)expiry;
                    int years = relative.GetYears();
                    int months = relative.GetMonths();
                    int weeks = relative.GetWeeks();
                    int days = relative.GetDays();
                    Console.WriteLine("years:{0}-months:{1}-weeks:{2}-days{3}", years, months, weeks, days);

                    expiration.type = rmc.sdk.ExpiryType.RELATIVE_EXPIRE;

                    if (isUserPreference)
                    {
                        expiration.Start = ((long)years << 32) + months;
                        expiration.End = ((long)weeks << 32) + days;
                    }
                    else
                    {
                        DateTime relativeEnd = dateStart.AddYears(years).AddMonths(months).AddDays(7 * weeks + days - 1).AddHours(23).AddMinutes(59).AddSeconds(59);
                        expiration.Start = 0;
                        expiration.End = CommonUtils.DateTimeToTimestamp(relativeEnd);
                    }
                    
                    break;
                case 2:
                    IAbsolute absolute = (IAbsolute)expiry;
                    long endAbsDate = absolute.EndDate();
                    Console.WriteLine("absEndDate:{0}", endAbsDate);

                    expiration.type = rmc.sdk.ExpiryType.ABSOLUTE_EXPIRE;
                    expiration.Start = CommonUtils.DateTimeToTimestamp(dateStart);
                    expiration.End = endAbsDate;
                    break;
                case 3:
                    IRange range = (IRange)expiry;
                    long startDate = range.StartDate();
                    long endDate = range.EndDate();
                    Console.WriteLine("StartDate:{0},EndDate{1}", startDate, endDate);

                    expiration.type = rmc.sdk.ExpiryType.RANGE_EXPIRE;
                    expiration.Start = startDate;
                    expiration.End = endDate;
                    break;
            }

        }
        #endregion

        #region NxlFile Convert
        public static List<RightsItem> GetRightsIcon(IList<string> rights, bool isAddValidity=true)
        {
            List<RightsItem>  rightsItems = new List<RightsItem>();
            rightsItems.Add(new RightsItem(@"/rmc/resources/icons/icon_rights_view.png", CultureStringInfo.SelectRights_View));
            if (rights != null && rights.Count != 0)
            {
                //In order to keep the rihts display order use the method below traversal list manually instead of 
                //using foreach loop.
                if (rights.Contains("Edit"))
                {
                    rightsItems.Add(new RightsItem(@"/rmc/resources/icons/icon_rights_edit.png", CultureStringInfo.SelectRights_Edit));
                }
                if (rights.Contains("Print"))
                {
                    rightsItems.Add(new RightsItem(@"/rmc/resources/icons/icon_rights_print.png", CultureStringInfo.SelectRights_Print));
                }
                if (rights.Contains("Share"))
                {
                    rightsItems.Add(new RightsItem(@"/rmc/resources/icons/icon_rights_share.png", CultureStringInfo.SelectRights_Share));
                }
                if (rights.Contains("SaveAs"))
                {
                    rightsItems.Add(new RightsItem(@"/rmc/resources/icons/icon_rights_save_as.png", CultureStringInfo.SelectRights_SaveAs));
                }
                // Fix bug 54210
                if (rights.Contains("Decrypt"))
                {
                    rightsItems.Add(new RightsItem(@"/rmc/resources/icons/icon_rights_extract.png", CultureStringInfo.SelectRights_Extract));
                }
                if (rights.Contains("Watermark"))
                {
                    rightsItems.Add(new RightsItem(@"/rmc/resources/icons/icon_rights_watermark.png", CultureStringInfo.SelectRights_Watermark));
                }
            }
            if (isAddValidity)
            {
                rightsItems.Add(new RightsItem(@"/rmc/resources/icons/icon_rights_validity.png", CultureStringInfo.SelectRights_Validity));
            }
            return rightsItems;
        }

        public static bool ModifyRights(ProtectAndShareConfig config, out string message)
        {
            message = "";
            bool Invoke = false;

            var app = App;
            if (app.Rmsdk.User == null)
            {
                app.Log.Info("app.Session.User is null");
                return false;
            }

            try
            {
                bool isBuild = CommonUtils.BuildPara(config, out List<SkydrmLocal.rmc.sdk.FileRights> fileRights, 
                    out rmc.sdk.WaterMarkInfo waterMark, out rmc.sdk.Expiration expiration, ref message);

                if (isBuild == false)
                {
                    app.Log.Info("Build parameter failed");
                    return false;
                }

                try
                {
                    if (config.FileOperation.Action == FileOperation.ActionType.ModifyRights)
                    {
                        UserSelectTags userSelectTags;
                        if (config.CentralPolicyRadioIsChecked)
                        {
                            userSelectTags = config.UserSelectTags;
                            fileRights.Clear();
                        }
                        else
                        {
                            userSelectTags = new UserSelectTags();
                        }
                        var fp = App.Rmsdk.User.GetNxlFileFingerPrint(config.FileOperation.FilePath[0]);
                        if (fp.isFromPorject)
                        {
                            ISearchFileInProject SearchFileInMyVault = new SearchProjectFileByLocalPath();
                            IProjectFile projectFile = SearchFileInMyVault.SearchInRmsFiles(fp.localPath);
                            if (projectFile != null)
                            {
                                Invoke = projectFile.ModifyRights(fileRights, waterMark, expiration, userSelectTags);
                            }
                            else
                            {
                                // In project saveAs file that is new nxlFile.
                                Invoke = App.Rmsdk.User.UpdateNxlFileRights(config.FileOperation.FilePath[0], fileRights,
                               waterMark, expiration, userSelectTags);
                            }
                        }
                        else if (fp.isFromSystemBucket)
                        {
                            Invoke = App.Rmsdk.User.UpdateNxlFileRights(config.FileOperation.FilePath[0], fileRights,
                                waterMark, expiration, userSelectTags);
                        }
                    }

                    if (!Invoke)
                    {
                        message = CultureStringInfo.CreateFileWin_Notify_File_Protect_Failed;
                    }

                }
                catch (RmSdkException e)
                {
                    Invoke = false;
                    app.Log.Error("Error in ModifyRights:", e);
                    //message = e.Message;
                    message = CultureStringInfo.CreateFileWin_Notify_File_Protect_Failed;
                }
                catch (Exception msg)
                {
                    Invoke = false;
                    app.Log.Error("Error in ModifyRights:", msg);
                    message = CultureStringInfo.CreateFileWin_Notify_File_Protect_Failed;
                }

                return Invoke;

            }
            catch (Exception msg)
            {
                App.Log.Error("Error in ModifyRights:", msg);
                return false;
            }

        }

        private static bool BuildPara(ProtectAndShareConfig config, out List<SkydrmLocal.rmc.sdk.FileRights> fileRights, out rmc.sdk.WaterMarkInfo waterMark,
            out rmc.sdk.Expiration expiration, ref string message)
        {
            bool invoke = true;

            fileRights = new List<rmc.sdk.FileRights>();
            waterMark = new rmc.sdk.WaterMarkInfo();
            // ---- Note: below is test value, avoid crash in wrappersdk.
            waterMark.text = "";
            waterMark.fontColor = "";
            waterMark.fontSize = 10;
            waterMark.fontName = "";

            //Set Sdk File rights
            fileRights = NxlHelper.FromRightStrings(config.RightsSelectConfig.Rights.ToArray()).ToList();

            //for waterMark
            foreach (var rights in config.RightsSelectConfig.Rights)
            {
                // set watermark.
                if (rights == "Watermark")
                {
                    waterMark.text = config.RightsSelectConfig.Watermarkvalue;
                }
            }

            invoke = CheckExpiry(config, out expiration, out message);

            return invoke;
        }

        public static bool CheckExpiry(ProtectAndShareConfig config, out rmc.sdk.Expiration expiration, out string message)
        {
            bool result = true;
            message = "";
            //expiration pass by value
            string expirationDate = config.RightsSelectConfig.ExpireDateValue;
            IExpiry expiry = config.RightsSelectConfig.Expiry;

            expiration = new rmc.sdk.Expiration();
            ValiditySpecifyModel2SdkExpiration(out expiration, expiry, expirationDate, false);

            if (!config.CentralPolicyRadioIsChecked)
            {
                if (expiration.type != ExpiryType.NEVER_EXPIRE && CommonUtils.DateTimeToTimestamp(DateTime.Now) > expiration.End)
                {
                    result = false;
                    message = CultureStringInfo.Validity_DlgBox_Details;
                    //app.ShowBalloonTip(CultureStringInfo.Validity_DlgBox_Details);
                }
            }
            return result;
        }

        private static PendingUploadFile InnerProtectOrShare(ProtectAndShareConfig config,
            string filePath,
            List<INxlFile> nxlFiles,
            List<FileRights> fileRights,
            WaterMarkInfo waterMark,
            Expiration expiration,
            ref string message)
        {
            try
            {
                if (config.FileOperation.Action == FileOperation.ActionType.Protect)
                {
                    // do protect
                    return DoProtect(config, filePath, fileRights, waterMark, expiration, nxlFiles);
                }
                else
                {
                    // do share
                    UserSelectTags userSelectTags = new UserSelectTags();
                    return CommonUtils.MyVaultAddLocalFile(config, filePath,
                        fileRights,
                        (List<string>)config.SharedWithConfig.SharedEmailLists,
                        config.SharedWithConfig.Comments,
                        waterMark,
                        expiration,
                        nxlFiles,
                        userSelectTags);
                }
            }
            catch (RmRestApiException e)
            {
                App.Log.Error("Error in InnerProtectOrShare: " + e.ToString());
                message = e.Message;
                // Force user logout
                if(e.ErrorCode == 401)
                {
                    SkydrmLocalApp.Singleton.Dispatcher.Invoke((Action)delegate
                    {
                        GeneralHandler.HandleSessionExpiration();
                    });
                }
            }
            catch (Exception e)
            {
                App.Log.Error("Error in InnerProtectOrShare: " + e.ToString());
            }

            return null;
        }

        public static bool ProtectOrShare(ProtectAndShareConfig config, out string message)
        {
            message = "";
            bool Invoke = true;

            if (App.Rmsdk.User == null)
            {
                App.Log.Info("app.Session.User is null");
                return false;
            }
            
            try
            {
                bool isBuild = CommonUtils.BuildPara(config, out List<SkydrmLocal.rmc.sdk.FileRights> fileRights,
                    out rmc.sdk.WaterMarkInfo waterMark, out rmc.sdk.Expiration expiration, ref message);

                if (isBuild == false)
                {
                    App.Log.Info("Build parameter failed");
                    return false;
                }
                try
                {
                    bool IsHaveFailed = false;
                    string failedFileName = "";

                    //for copy protect file success
                    string[] FilePathCopy = new string[config.FileOperation.FilePath.Length];
                    int y = 0;
                    List<INxlFile> nxlFiles = new List<INxlFile>();
                    PendingUploadFile doc = null;

                    for (int i = 0; i < config.FileOperation.FilePath.Length; i++)
                    {
                        // Execute protect or share.
                        doc = InnerProtectOrShare(config, config.FileOperation.FilePath[i], nxlFiles, fileRights, waterMark, expiration, ref message);

                        if (doc != null)
                        {
                            //if protect successful
                            config.FileOperation.FileName[i] = doc.Name;
                            FilePathCopy[y++] = config.FileOperation.FileName[i];
                        }
                        else
                        {
                            //if protect failed
                            App.Log.Info("User ProtectFile is failed, doc is null");                          
                            IsHaveFailed = true;
                            failedFileName += config.FileOperation.FileName[i] + ";" + "\n";
                            // Notify serviceManger
                            App.UserRecentTouchedFile.UpdateOrInsert(EnumNxlFileStatus.ProtectFailed, config.FileOperation.FileName[i]);
                        }

                    }     
                    
                    config.CreatedFiles = nxlFiles;

                    if (IsHaveFailed)
                    {
                        if (y < 1)//no success file
                        {
                            Invoke = false;
                            message = CultureStringInfo.CreateFileWin_Notify_File_Protect_Failed;
                        }
                        else//have successful file
                        {
                            config.FileOperation.FileName = FilePathCopy;
                            config.FileOperation.FailedFileName = CultureStringInfo.ProtectOrShareSuccessPage_Protect_Failed + "\n" + failedFileName;
                        }
                    }

                }
                catch (Exception msg)
                {
                    Invoke = false;
                    App.Log.Info("app.Session.User.ProtectFile is failed", msg);
                    message = CultureStringInfo.CreateFileWin_Notify_File_Protect_Failed;
                }
                return Invoke;
            }
            catch (Exception msg)
            {
                App.Log.Error("Error in ProtectOrShare:",msg);
                return false;
            }
           
        }
        #region For Share File
        private static PendingUploadFile MyVaultAddLocalFile(ProtectAndShareConfig config, 
            string filePath, 
            List<FileRights> fileRights,
            List<string> shareWith, 
            string comment,
            WaterMarkInfo waterMark, 
            Expiration expiration, 
            List<INxlFile> nxlFiles, 
            UserSelectTags userSelectTags)
        {
            try
            {
                IMyVaultLocalFile localFile = App.MyVault.AddLocalAdded(filePath, fileRights,
                                                                           (List<string>)config.SharedWithConfig.SharedEmailLists, 
                                                                           config.SharedWithConfig.Comments,
                                                                           waterMark,
                                                                           expiration, 
                                                                           userSelectTags);

                PendingUploadFile doc = new PendingUploadFile(localFile);
                doc.Location = EnumFileLocation.Local;
                doc.FileStatus = EnumNxlFileStatus.WaitingUpload;
                nxlFiles.Add(doc);

                return doc;
            }
            catch (RmRestApiException e)
            {
                App.Log.Error("Error in CommonUtils.MyVaultAddLocalFile:", e);
                throw e;
            }
            catch (Exception e)
            {
                App.Log.Error("Error in CommonUtils.MyVaultAddLocalFile:", e);
                throw e;
            }
        }
        #endregion

        #region For Protect File
        private static PendingUploadFile DoProtect(ProtectAndShareConfig config, 
            string filePath, 
            List<FileRights> fileRights,
            WaterMarkInfo waterMark, 
            Expiration expiration, 
            List<INxlFile> nxlFiles)
        {
            if (config.IsProtectToProject)
            {
                // do protect file to Project
                UserSelectTags userSelectTags;
                if (config.CentralPolicyRadioIsChecked)
                {
                    userSelectTags = config.UserSelectTags;
                    fileRights.Clear();
                }
                else
                {
                    userSelectTags = new UserSelectTags();
                }

                if (config.LocalDriveIsChecked)
                {
                    // protect to local drive
                    if (Directory.Exists(config.SelectProjectFolderPath))
                    {
                        // systemProject
                        if (config.sProject.IsFeatureEnabled)
                        {
                            if (config.CentralPolicyRadioIsChecked)
                            {
                                return CommonUtils.sProtectFileCentrolPolicy(config, filePath, userSelectTags);
                            }
                            else
                            {
                                return CommonUtils.sProtectFileAdHoc(config, filePath, fileRights, waterMark, expiration);
                            }
                        }
                        else
                        {
                            // myProject
                            return CommonUtils.ProjectCopyLocalFile(config, filePath, fileRights, waterMark,
                                                        expiration, userSelectTags);
                        }
                    }
                    else
                    {
                        // The local drive path does not exist
                       var message = CultureStringInfo.NxlFileToCvetWin_Error_Message;
                       throw new Exception(message);
                    }

                }
                else
                {
                    // protect to central project
                    return CommonUtils.ProjectAddLocalFile(config, filePath, fileRights, waterMark,
                                                         expiration, userSelectTags, nxlFiles);
                }
            }
            else
            {
                // do protect file to myVault.
                UserSelectTags userSelectTags = new UserSelectTags();
                return CommonUtils.MyVaultAddLocalFile(filePath, fileRights, waterMark,
                                                 expiration, nxlFiles, userSelectTags);
            }
        }

        private static PendingUploadFile MyVaultAddLocalFile(string filePath, 
            List<FileRights> fileRights,
            WaterMarkInfo waterMark, 
            Expiration expiration, 
            List<INxlFile> nxlFiles, 
            UserSelectTags userSelectTags
            )
        {
            try
            {              
                IMyVaultLocalFile localFile = App.MyVault.AddLocalAdded(filePath, fileRights,
                                                waterMark, expiration, userSelectTags);

                PendingUploadFile doc = new PendingUploadFile(localFile);
                doc.Location = EnumFileLocation.Local;
                doc.FileStatus = EnumNxlFileStatus.WaitingUpload;
                nxlFiles.Add(doc);

                return doc;
            }
            catch (RmRestApiException e)
            {
                App.Log.Error("Error in CommonUtils.MyVaultAddLocalFile:", e);
                throw e;
            }
            catch (Exception e)
            {
                App.Log.Error("Error in CommonUtils.MyVaultAddLocalFile:", e);
                throw e;
            }
        }
        private static PendingUploadFile ProjectAddLocalFile(ProtectAndShareConfig config, string filePath, List<SkydrmLocal.rmc.sdk.FileRights> fileRights,
                                                              rmc.sdk.WaterMarkInfo waterMark, rmc.sdk.Expiration expiration, UserSelectTags userSelectTags,
                                                              List<INxlFile> nxlFiles)
        {
            try
            {
                string ProjectFilePath = config.SelectProjectFolderPath.Substring(config.SelectProjectFolderPath.IndexOf('/'));

                IProjectLocalFile localfile = config.myProject.AddLocalFile(ProjectFilePath,
                                                     filePath, fileRights, waterMark, expiration, userSelectTags);

                PendingUploadFile doc = new PendingUploadFile(localfile, config.myProject.Id);
                doc.Location = EnumFileLocation.Local;
                doc.FileStatus = EnumNxlFileStatus.WaitingUpload;
                nxlFiles.Add(doc);

                return doc;
            }
            catch (RmRestApiException e)
            {
                App.Log.Error("Error in CommonUtils.ProjectAddLocalFile:", e);
                throw e;
            }
            catch (Exception e)
            {
                App.Log.Error("Error in CommonUtils.ProjectAddLocalFile:", e);
                throw e;
            }
        }
        private static PendingUploadFile ProjectCopyLocalFile(ProtectAndShareConfig config, string filePath, List<SkydrmLocal.rmc.sdk.FileRights> fileRights,
                                                    rmc.sdk.WaterMarkInfo waterMark, rmc.sdk.Expiration expiration, UserSelectTags userSelectTags)
        {
            try
            {
                IProjectLocalFile localfile = config.myProject.CopyLocalFile(config.SelectProjectFolderPath,
                                                                                 filePath, fileRights, waterMark, expiration, userSelectTags);

                PendingUploadFile doc = new PendingUploadFile(localfile, config.myProject.Id);
                // Notify serviceManger
                App.UserRecentTouchedFile.UpdateOrInsert(EnumNxlFileStatus.ProtectSucceeded, doc.Name);
                return doc;
            }
            catch (RmRestApiException e)
            {
                App.Log.Error("Error in CommonUtils.ProjectCopyLocalFile:", e);
                throw e;
            }
            catch (Exception e)
            {
                App.Log.Error("Error in CommonUtils.ProjectCopyLocalFile:", e);
                throw e;
            }
        }
        private static PendingUploadFile sProtectFileCentrolPolicy(ProtectAndShareConfig config, string filePath, UserSelectTags userSelectTags)
        {
            try
            {
                string outPath = config.sProject.ProtectFileCentrolPolicy(filePath, config.SelectProjectFolderPath, userSelectTags);

                ProjectLocalAddedFile localfile = new ProjectLocalAddedFile(config.sProject.Id,
                    new ProjectLocalFile()
                    {
                        Id = config.sProject.Id,
                        Name = Path.GetFileName(outPath),
                        Path = outPath,
                        Last_modified_time = File.GetLastAccessTime(outPath),
                        Size = new FileInfo(outPath).Length
                    }
                );

                PendingUploadFile doc = new PendingUploadFile(localfile, config.sProject.Id);
                // Notify serviceManger
                App.UserRecentTouchedFile.UpdateOrInsert(EnumNxlFileStatus.ProtectSucceeded, doc.Name);
                return doc;
            }
            catch (RmRestApiException e)
            {
                App.Log.Error("Error in CommonUtils.sProtectFileCentrolPolicy:", e);
                throw e;
            }
            catch (Exception e)
            {
                App.Log.Error("Error in CommonUtils.sProtectFileCentrolPolicy:", e);
                throw e;
            }
        }
        private static PendingUploadFile sProtectFileAdHoc(ProtectAndShareConfig config, string filePath, List<SkydrmLocal.rmc.sdk.FileRights> fileRights,
                                                   rmc.sdk.WaterMarkInfo waterMark, rmc.sdk.Expiration expiration)
        {
            try
            {
                string outPath = config.sProject.ProtectFileAdhoc(filePath, config.SelectProjectFolderPath,
                                                                                  fileRights, waterMark, expiration);

                ProjectLocalAddedFile localfile = new ProjectLocalAddedFile(config.sProject.Id,
                    new ProjectLocalFile()
                    {
                        Id = config.sProject.Id,
                        Name = Path.GetFileName(outPath),
                        Path = outPath,
                        Last_modified_time = File.GetLastAccessTime(outPath),
                        Size = new FileInfo(outPath).Length
                    }
                );

                PendingUploadFile doc = new PendingUploadFile(localfile, config.sProject.Id);
                // Notify serviceManger
                App.UserRecentTouchedFile.UpdateOrInsert(EnumNxlFileStatus.ProtectSucceeded, doc.Name);
                return doc;
            }
            catch (RmRestApiException e)
            {
                App.Log.Error("Error in CommonUtils.sProtectFileAdHoc:", e);
                throw e;
            }
            catch (Exception e)
            {
                App.Log.Error("Error in CommonUtils.sProtectFileAdHoc:", e);
                throw e;
            }
        }
        #endregion

        #endregion

        /// <summary>
        /// Check select file path
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="tag"></param>
        /// <param name="rightFilePath"></param>
        /// <returns></returns>
        public static bool CheckFilePathDoProtect(string[] filePath, out string tag, out List<string> rightFilePath)
        {
            bool isCheck = false;
            tag = "";

            rightFilePath = new List<string>();
            List<string> emptyFileName = new List<string>();
            List<string> nxlFileName = new List<string>();

            for (int i = 0; i < filePath.Length; i++)
            {
                // Sanity check
                if (filePath[i] == null || filePath[i].Length == 0)
                {
                    return false;
                }
                // new feature request, deny ops in rmp folder
                if (App.Rmsdk.RMP_IsSafeFolder(filePath[i]))
                {
                    App.ShowBalloonTip(CultureStringInfo.Common_DenyOp_InRMP);
                    return false;
                }
                // Required to FILTER OUT 0-SIZED FILE
                if (new FileInfo(filePath[i]).Length == 0)
                {
                    emptyFileName.Add(new FileInfo(filePath[i]).Name);
                    continue;
                }
                // Check is not nxlFile
                int startIndex = filePath[i].LastIndexOf('.');
                if (startIndex < 0)
                {
                    rightFilePath.Add(filePath[i]);
                    continue;
                }
                if (filePath[i].Substring(startIndex).ToLower().Trim().Contains("nxl"))
                {
                    nxlFileName.Add(new FileInfo(filePath[i]).Name);
                    continue;
                }
                rightFilePath.Add(filePath[i]);
                tag += filePath[i];
            }

            if (emptyFileName.Count > 0)
            {
                StringBuilder builder = new StringBuilder();
                foreach (var item in emptyFileName)
                {
                    builder.Append(item + "\n");
                }
                App.ShowBalloonTip(CultureStringInfo.CreateFileWin_Notify_File_Not_Created + "\n" + builder.ToString());
            }

            if (nxlFileName.Count > 0)
            {
                StringBuilder builder = new StringBuilder();
                foreach (var item in nxlFileName)
                {
                    builder.Append(item + "\n");
                }
                App.ShowBalloonTip(CultureStringInfo.CreateFileWin_Notify_NxlFile_Not_Protect + "\n" + builder.ToString());
            }

            if (rightFilePath.Count > 0)
            {
                isCheck = true;
            }
            return isCheck;
        }
        /// <summary>
        /// Check file is or not Nxl File
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool CheckNxlFile(string[] path)
        {
            bool isNxlFile = false;

            for (int i = 0; i < path.Length; i++)
            {
                int startIndex = path[i].LastIndexOf('.');
                if (startIndex < 0)
                {
                    continue;
                }
                if (path[i].Substring(startIndex).ToLower().Trim().Contains("nxl"))
                {
                    isNxlFile = true;
                    break;
                }
            }
            return isNxlFile;
        }

        // Check the file if is conflict when view project offline file.
        public static void CheckOfflineFileVersion(IFileRepo currentWorkRepo, EnumCurrentWorkingArea area, INxlFile nxlFile, Action<bool> callback)
        {
            App.Log.Info("Check Project Offline File Version -->");
           if (area == EnumCurrentWorkingArea.FILTERS_OFFLINE)
            {
                currentWorkRepo.SyncDestFile(nxlFile, (bool bSuccess, INxlFile updatedFile) =>
                {
                    NotifyModified(bSuccess, updatedFile, callback);
                }, true);
            }
            else
            {
                currentWorkRepo.SyncDestFile(nxlFile, (bool bSuccess, INxlFile updatedFile) =>
                {
                    NotifyModified(bSuccess, updatedFile, callback);
                }, false);
            }

        }

        private static void NotifyModified(bool bSuccess, INxlFile updatedFile, Action<bool> callback)
        {
            if (bSuccess && updatedFile != null)
            {
                Console.WriteLine("$$$$$$$$$$$ NotifyModified, file: " + updatedFile.Name);
                callback?.Invoke(updatedFile.IsMarkedFileRemoteModified);
            }
            else
            {
                callback?.Invoke(false);
            }
        }

        public static INxlFile GetFileFromListByLocalPath(string localPath, IList<INxlFile> list)
        {
            INxlFile ret = null;
            foreach (var one in list)
            {
                if (one.LocalPath == localPath)
                {
                    ret = one;
                    break;
                }
            }

            return ret;
        }

    }

}
