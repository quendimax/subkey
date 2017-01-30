using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using subkey.Properties;

namespace subkey
{
    public partial class Form : System.Windows.Forms.Form
    {
        [DllImport("gdi32.dll", ExactSpelling = true)]
        private static extern IntPtr AddFontMemResourceEx(byte[] pbFont, int cbFont, IntPtr pdv, out uint pcFonts);

        private PrivateFontCollection fontCollection = new PrivateFontCollection();
        private Dictionary<string, Font> fontMap = new Dictionary<string, Font>();
        private Dictionary<string, FontFamily> fontFamilyMap = new Dictionary<string, FontFamily>();
        private const float DefaultFontSize = 16f;

        public Form()
        {
            InitializeComponent();
            LoadFonts();
            LoadKeyboardScheme();
            TopMost = true;
        }

        private Button BuildButton(string text, string toolTipText, string fontFamily, float fontSize)
        {
            var button = new Button();
            button.Text = text.ToUpper();
            button.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;
            button.Font = getFont(fontFamily, fontSize);
            button.Click += Button_Click;
            var toolTip = new ToolTip();
            toolTip.SetToolTip(button, toolTipText);
            return button;
        }

        private void Button_Click(object sender, EventArgs e)
        {
            Button button = (Button)sender;
            string c = button.Text;
            bool isBig = Control.IsKeyLocked(Keys.CapsLock) && !(Control.ModifierKeys == Keys.Shift) ||
                        !Control.IsKeyLocked(Keys.CapsLock) && Control.ModifierKeys == Keys.Shift;
            if (!isBig) c = c.ToLower();
            else c = c.ToUpper();
            SendKeys.Send(c);
        }

        private void LoadKeyboardScheme()
        {
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(Resources.Keyboard);
            foreach (XmlNode scheme in xml.DocumentElement.ChildNodes)
            {
                string schemeName = scheme.Attributes["name"].Value;
                foreach (XmlNode key in scheme.ChildNodes)
                {
                    string text = "";
                    string title = "";
                    string tooltip = "";
                    string fontFamily = "RomanCyrillic Std";
                    float fontSize = DefaultFontSize;
                    XmlNode node = key.FirstChild;
                    do
                    {
                        switch (node.Name.ToLower())
                        {
                            case "text":
                                text = node.InnerText;
                                if (title.Length == 0) title = text;
                                break;
                            case "tooltip":
                                tooltip = node.InnerText;
                                break;
                            case "title":
                                title = node.InnerText;
                                break;
                            case "font":
                                fontFamily = node.InnerText;
                                if (node.Attributes["size"] != null)
                                    fontSize = (float)Double.Parse(node.Attributes["size"].Value);
                                break;
                        }
                        node = node.NextSibling;
                    }
                    while (node != null);
                    tableLayout.Controls.Add(BuildButton(text, tooltip, fontFamily, fontSize));
                }
            }
        }

        Font getFont(string familyName, float size)
        {
            Font font;
            string hash = String.Format("<{0}, {1}>", familyName, size);
            if (fontMap.ContainsKey(hash))
                return fontMap[hash];
            if (fontFamilyMap.ContainsKey(familyName))
                font = new Font(fontFamilyMap[familyName], size, FontStyle.Bold);
            else
                font = new Font(familyName, size, FontStyle.Bold);
            fontMap[hash] = font;
            return font;
        }

        private void LoadFonts()
        {
            uint dummy;
            byte[] fontData = (byte[])Resources.RomanCyrillic_Std.Clone();
            AddFontMemResourceEx(fontData, fontData.Length, IntPtr.Zero, out dummy);

            IntPtr buf = Marshal.AllocCoTaskMem(fontData.Length);
            if (buf != null)
            {
                Marshal.Copy(fontData, 0, buf, fontData.Length);
                fontCollection.AddMemoryFont(buf, fontData.Length);
                Marshal.FreeCoTaskMem(buf);
            }
            string fontName = "RomanCyrillic Std";
            fontFamilyMap["RomanCyrillic Std"] = fontCollection.Families[0];
            Font font = new Font(fontCollection.Families[0], DefaultFontSize, FontStyle.Bold);
            fontMap[String.Format("<{0}, {1}>", fontName, DefaultFontSize)] = font;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams param = base.CreateParams;
                //param.ExStyle |= 0x08000000;
                return param;
            }
        }

        private void Form_Load(object sender, EventArgs e)
        {
            if (Settings.Default.WindowLocation != null)
            {
                this.Location = Settings.Default.WindowLocation;
            }
            if (Settings.Default.WindowSize != null)
            {
                this.Size = Settings.Default.WindowSize;
            }
        }

        private void Form_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (this.WindowState == FormWindowState.Normal)
            {
                Settings.Default.WindowLocation = this.Location;
                Settings.Default.WindowSize = this.Size;
            }
            else
            {
                Settings.Default.WindowSize = this.RestoreBounds.Size;
                Settings.Default.WindowLocation = this.RestoreBounds.Location;
            }
            Settings.Default.Save();
        }
    }
}
