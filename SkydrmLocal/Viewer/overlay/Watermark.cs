using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.utils;

namespace Viewer.overlay
{
    public class WatermarkInfo
    {
        private int transparentRatio;
        private int fontSize;
        private String text;
        private String fontName;
        private String fontColor;
        private int rotation;
        private Boolean bRepeat;

        public int TransparentRatio { get => transparentRatio; set => transparentRatio = value; }
        public int FontSize { get => fontSize; set => fontSize = value; }
        public string Text { get => text; set => text = value; }
        public string FontName { get => fontName; set => fontName = value; }
        public string FontColor { get => fontColor; set => fontColor = value; }
        public int Rotation { get => rotation; set => rotation = value; }
        public bool BRepeat { get => bRepeat; set => bRepeat = value; }

        private WatermarkInfo(Builder builder)
        {
            transparentRatio = builder.TransparentRatio;
            fontSize = builder.FontSize;
            text = builder.Text;
            fontName = builder.FontName;
            fontColor = builder.FontColor;
            rotation = builder.Rotation;
            bRepeat = builder.BRepeat;
        }

        #region class Builder
        public class Builder
        {
            int transparentRatio;
            private int fontSize;
            private String text;
            private String fontName;
            private String fontColor;
            private int rotation;
            private Boolean bRepeat;

            public int TransparentRatio { get => transparentRatio; }
            public int FontSize { get => fontSize; }
            public string Text { get => text; }
            public string FontName { get => fontName; }
            public string FontColor { get => fontColor; }
            public int Rotation { get => rotation; }
            public bool BRepeat { get => bRepeat; }

            public Builder DefaultSet(string AdhocWaterMark,string UserEmail, log4net.ILog log)
            {
                log.Info("\t\t WatermarkInfo Builder \r\n");
                StringBuilder wmText = new StringBuilder();
                CommonUtils.ConvertWatermark2DisplayStyle(AdhocWaterMark, UserEmail, ref wmText);
                this.text = wmText.ToString();

                // Todo, Actually we should get thest from heartbeat.
                fontName = "Arial";
                fontColor = "#008015";
                fontSize = 22;
                transparentRatio = 70;
                rotation = 45;
                this.bRepeat = false;

                return this;
            }

            public Builder Set(string AdhocWaterMark, string UserEmail, string fontName, string fontColor, int fontSize, int transparency, int rotation, bool repeat)
            {
                StringBuilder wmText = new StringBuilder();
                CommonUtils.ConvertWatermark2DisplayStyle(AdhocWaterMark, UserEmail, ref wmText);
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
}
