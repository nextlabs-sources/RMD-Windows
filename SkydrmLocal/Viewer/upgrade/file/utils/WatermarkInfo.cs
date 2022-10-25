using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Viewer.upgrade.application;

namespace Viewer.upgrade.file.basic.utils
{
    public class WatermarkInfo
    {
        private const string DOLLAR_USER = "$(User)";
        private const string DOLLAR_BREAK = "$(Break)";
        private const string DOLLAR_DATE = "$(Date)";
        private const string DOLLAR_TIME = "$(Time)";
        private int transparentRatio;
        private int fontSize;
        private String text;
        private String fontName;
        private String fontColor;
        private int rotation;
        private Boolean bRepeat;
        private string mWaterMarkRaw;

        public int TransparentRatio { get => transparentRatio; set => transparentRatio = value; }
        public int FontSize { get => fontSize; set => fontSize = value; }
        public string Text { get => text; set => text = value; }
        public string FontName { get => fontName; set => fontName = value; }
        public string FontColor { get => fontColor; set => fontColor = value; }
        public int Rotation { get => rotation; set => rotation = value; }
        public bool BRepeat { get => bRepeat; set => bRepeat = value; }
        public string WaterMarkRaw { get => mWaterMarkRaw; }

        private WatermarkInfo(Builder builder)
        {
            transparentRatio = builder.TransparentRatio;
            fontSize = builder.FontSize;
            text = builder.Text;
            fontName = builder.FontName;
            fontColor = builder.FontColor;
            rotation = builder.Rotation;
            bRepeat = builder.BRepeat;
            mWaterMarkRaw = builder.WaterMarkRaw;
        }

        public static void ConvertWatermark2DisplayStyle(string value, string userEmail, ref StringBuilder sb)
        {
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

                    if (subStr.Equals(DOLLAR_USER,StringComparison.CurrentCultureIgnoreCase))
                    {
                        sb.Append(" ");
                        sb.Append(Replace(new ReplaceDollarUser(userEmail)));
                        sb.Append(" ");
                    }
                    else if (subStr.Equals(DOLLAR_BREAK, StringComparison.CurrentCultureIgnoreCase))
                    {
                        sb.Append(Replace(new ReplaceDollarBreak()));
                    }
                    else if (subStr.Equals(DOLLAR_DATE, StringComparison.CurrentCultureIgnoreCase))
                    {
                        sb.Append(" ");
                        sb.Append(Replace(new ReplaceDollarDate()));
                        sb.Append(" ");
                    }
                    else if (subStr.Equals(DOLLAR_TIME, StringComparison.CurrentCultureIgnoreCase))
                    {
                        sb.Append(" ");
                        sb.Append(Replace(new ReplaceDollarTime()));
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
                    ConvertWatermark2DisplayStyle(value.Substring(endIndex + 1), userEmail, ref sb);
                }
            }
        }
        private static string Replace(ReplaceDollar replaceDollar)
        {
            return replaceDollar.Replace();
        }

        #region class Builder
        public class Builder
        {
            private ViewerApp mApplication;
            private log4net.ILog mLog;
            private int transparentRatio;
            private int fontSize;
            private String text;
            private String fontName;
            private String fontColor;
            private int rotation;
            private Boolean bRepeat;
            private string mWaterMarkRaw;

            public int TransparentRatio { get => transparentRatio; }
            public int FontSize { get => fontSize; }
            public string Text { get => text; }
            public string FontName { get => fontName; }
            public string FontColor { get => fontColor; }
            public int Rotation { get => rotation; }
            public bool BRepeat { get => bRepeat; }
            public string WaterMarkRaw { get => mWaterMarkRaw; }

            public Builder DefaultSet()
            {
                mApplication = (ViewerApp)Application.Current;
                mLog = mApplication.Log;
                this.text = string.Empty;
                this.fontName = "Arial";
                this.fontColor = "#008015";
                this.fontSize = 22;
                this.transparentRatio = 30;
                this.rotation = 45;
                this.bRepeat = false;
                return this;
            }

            public Builder DefaultSet(string waterMarkRaw, string UserEmail)
            {
                mApplication = (ViewerApp)Application.Current;
                mLog = mApplication.Log;
                mWaterMarkRaw = waterMarkRaw;
                StringBuilder wmText = new StringBuilder();
                ConvertWatermark2DisplayStyle(waterMarkRaw, UserEmail, ref wmText);
                this.text = wmText.ToString();
                this.fontName = "Arial";
                this.fontColor = "#008015";
                this.fontSize = 22;
                this.transparentRatio = 30;
                this.rotation = 45;
                this.bRepeat = false;
                return this;
            }

            public Builder DefaultSet2(string waterMarkRaw, string waterMark)
            {
                mApplication = (ViewerApp)Application.Current;
                mLog = mApplication.Log;
                mWaterMarkRaw = waterMarkRaw;
                this.text = waterMark;
                this.fontName = "Arial";
                this.fontColor = "#008015";
                this.fontSize = 22;
                this.transparentRatio = 30;
                this.rotation = 45;
                this.bRepeat = false;
                return this;
            }

            public Builder Set(string waterMarkRaw, string UserEmail, string fontName, string fontColor, int fontSize, int transparency, int rotation, bool repeat)
            {
                mApplication = (ViewerApp)Application.Current;
                mLog = mApplication.Log;
                mWaterMarkRaw = waterMarkRaw;
                StringBuilder wmText = new StringBuilder();
                ConvertWatermark2DisplayStyle(waterMarkRaw, UserEmail, ref wmText);
                this.text = wmText.ToString();
                this.fontName = fontName;
                this.fontColor = fontColor;
                this.transparentRatio = transparency;
                this.rotation = rotation;
                this.bRepeat = repeat;
                return this;
            }

            public WatermarkInfo Build()
            {
                return new WatermarkInfo(this);
            }
        }
        #endregion
    }


    public abstract class ReplaceDollar
    {
        public abstract string Replace();
    }

    public class ReplaceDollarUser : ReplaceDollar
    {
        private string UserEmail { get; }

        public ReplaceDollarUser(string userEmail)
        {
            this.UserEmail = userEmail;
        }
        public override string Replace()
        {
            return this.UserEmail;
        }
    }

    public class ReplaceDollarDate : ReplaceDollar
    {
        public override string Replace()
        {
            return DateTime.Now.ToString("yyyy-MM-dd");
        }
    }

    public class ReplaceDollarTime : ReplaceDollar
    {
        public override string Replace()
        {
            return DateTime.Now.ToString("HH:mm:ss");
        }
    }

    public class ReplaceDollarBreak : ReplaceDollar
    {
        public override string Replace()
        {
            return "\n";
        }
    }

}
