using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace subkey
{
    class SchemeKey
    {
        public SchemeKey(string text, string title, string tooltip,
                         string fontFamily = subkey.Form.DefaultFontFamily,
                         float fontSize = subkey.Form.DefaultFontSize)
        {
            Text = text;
            Title = title;
            if (String.IsNullOrEmpty(title))
                Title = text;
            Tooltip = tooltip;
            FontFamily = fontFamily;
            FontSize = fontSize;
        }

        public string Text { get; set; }
        public string Title { get; set; }
        public string Tooltip { get; set; }
        public string FontFamily { get; set; }
        public float FontSize { get; set; }
    }
}
