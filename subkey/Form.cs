using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Xml;
using subkey.Properties;

namespace subkey
{
    public partial class Form : System.Windows.Forms.Form
    {
        [DllImport("gdi32.dll", ExactSpelling = true)]
        private static extern IntPtr AddFontMemResourceEx(byte[] pbFont, int cbFont, IntPtr pdv, out uint pcFonts);

        public const float DefaultFontSize = 14f;
        public const string DefaultFontFamily = "RomanCyrillic Std";

        private PrivateFontCollection fontCollection = new PrivateFontCollection();
        private Dictionary<string, Font> fontMap = new Dictionary<string, Font>();
        private Dictionary<string, FontFamily> fontFamilyMap = new Dictionary<string, FontFamily>();
        private Dictionary<string, List<Button>> schemes = new Dictionary<string, List<Button>>();
        private Dictionary<string, int> schemeOffsetIndeces = new Dictionary<string, int>();

        public Form()
        {
            InitializeComponent();
            LoadFonts();
            LoadKeyboardSchemes();
            InitializeMenu();
        }

        private Button BuildButton(SchemeKey key)
        {
            var button = new Button();
            button.Text = key.Title;
            button.Tag = key;
            button.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;
            button.Font = getFont(key.FontFamily, key.FontSize);
            button.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            button.Click += Button_Click;
            var toolTip = new ToolTip();
            toolTip.ShowAlways = true;
            toolTip.SetToolTip(button, key.Tooltip);
            var contextMenu = new ContextMenu();
            contextMenu.MenuItems.Add("Add to Favorites");
            contextMenu.MenuItems[0].Click += new EventHandler(Button_ContextMenuClick);
            button.ContextMenu = contextMenu;
            return button;
        }

        private void Button_ContextMenuClick(object sender, EventArgs e)
        {
            MessageBox.Show("Not implemented.");
        }

        private void Button_Click(object sender, EventArgs e)
        {
            var button = (Button)sender;
            var key = (SchemeKey)button.Tag;
            string c = key.Text;
            bool isBig = Control.IsKeyLocked(Keys.CapsLock) && !(Control.ModifierKeys == Keys.Shift) ||
                        !Control.IsKeyLocked(Keys.CapsLock) && Control.ModifierKeys == Keys.Shift;
            if (!isBig) c = c.ToLower();
            else c = c.ToUpper();
            SendKeys.Send(c);
        }

        private void LoadKeyboardSchemes()
        {
            XmlDocument xml = new XmlDocument();
            string customKeyboardFile = Path.Combine(Directory.GetCurrentDirectory(), "keyboard.xml");
            if (File.Exists(customKeyboardFile))
                xml.Load(customKeyboardFile);
            else
                xml.LoadXml(Resources.Keyboard);
            foreach (XmlNode scheme in xml.DocumentElement.ChildNodes)
            {
                string schemeFontFamily = DefaultFontFamily;
                if (scheme.Attributes["fontfamily"] != null)
                    schemeFontFamily = scheme.Attributes["fontfamily"].Value;
                float schemeFontSize = DefaultFontSize;
                if (scheme.Attributes["fontsize"] != null)
                    schemeFontSize = (float)Double.Parse(scheme.Attributes["fontsize"].Value);
                string schemeName = scheme.Attributes["name"].Value;
                if (schemes.ContainsKey(schemeName))
                {
                    MessageBox.Show(String.Format("The '{0}' scheme has already added. It is a duplicate.", schemeName),
                                    "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    continue;
                }
                schemes[schemeName] = new List<Button>();
                schemeOffsetIndeces[schemeName] = 0;
                foreach (XmlNode key in scheme.ChildNodes)
                {
                    string text = "";
                    string title = "";
                    string tooltip = "";
                    string fontFamily = schemeFontFamily;
                    float fontSize = schemeFontSize;
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
                    } while (node != null);
                    var schemeKey = new SchemeKey(text, title, tooltip, fontFamily, fontSize);
                    schemes[schemeName].Add(BuildButton(schemeKey));
                }
            }
        }

        private void InitializeMenu()
        {
            string[] keys = new string[schemes.Keys.Count];
            schemes.Keys.CopyTo(keys, 0);
            schemeComboBox.Items.AddRange(keys);
            if (Settings.Default.SchemeIndex < schemeComboBox.Items.Count)
            {
                schemeComboBox.SelectedIndex = Settings.Default.SchemeIndex;
            }
            else
            {
                Settings.Default.SchemeIndex = 0;
                if (Settings.Default.SchemeIndex < schemeComboBox.Items.Count)
                    schemeComboBox.SelectedIndex = Settings.Default.SchemeIndex;
            }
        }

        private void SchemeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ToolStripComboBox comboBox = (ToolStripComboBox)sender;
            UpdateTableLayout(comboBox.Text);
        }

        private void OffsetIndexButton_Click(object sender, EventArgs e)
        {
            var button = (ToolStripButton)sender;
            string schemeName = schemeComboBox.Text;
            if (button == backButton)
                --schemeOffsetIndeces[schemeName];
            else if (button == nextButton)
                ++schemeOffsetIndeces[schemeName];
            UpdateTableLayout(schemeName);
        }

        private void UpdateTableLayout(string schemeName)
        {
            var scheme = schemes[schemeName];
            int offsetIndex = schemeOffsetIndeces[schemeName];
            int tableSize = tableLayout.ColumnCount * tableLayout.RowCount;
            int offset = offsetIndex * tableSize;
            int count = scheme.Count - offset;
            if (count > tableSize)
                count = tableSize;

            tableLayout.SuspendLayout();
            tableLayout.Controls.Clear();
            tableLayout.Controls.AddRange(scheme.GetRange(offset, count).ToArray());
            tableLayout.ResumeLayout(true);

            if (offset != 0)
                backButton.Enabled = true;
            else
                backButton.Enabled = false;
            if (offset + count == scheme.Count)
                nextButton.Enabled = false;
            else
                nextButton.Enabled = true;
        }

        Font getFont(string familyName, float size)
        {
            Font font;
            string hash = String.Format("<{0}, {1}>", familyName, size);
            if (fontMap.ContainsKey(hash))
                return fontMap[hash];
            if (fontFamilyMap.ContainsKey(familyName))
                font = new Font(fontFamilyMap[familyName], size, FontStyle.Regular);
            else
                font = new Font(familyName, size, FontStyle.Regular);
            fontMap[hash] = font;
            return font;
        }

        private void LoadFonts()
        {
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
                fontFamilyMap[fontName] = fontCollection.Families[0];
                Font font = new Font(fontCollection.Families[0], DefaultFontSize, FontStyle.Regular);
                fontMap[String.Format("<{0}, {1}>", fontName, DefaultFontSize)] = font;
            }
            {
                uint dummy;
                byte[] fontData = (byte[])Resources.BukyVede_Regular.Clone();
                AddFontMemResourceEx(fontData, fontData.Length, IntPtr.Zero, out dummy);

                IntPtr buf = Marshal.AllocCoTaskMem(fontData.Length);
                if (buf != null)
                {
                    Marshal.Copy(fontData, 0, buf, fontData.Length);
                    fontCollection.AddMemoryFont(buf, fontData.Length);
                    Marshal.FreeCoTaskMem(buf);
                }
                string fontName = "BukyVede";
                fontFamilyMap[fontName] = fontCollection.Families[0];
                Font font = new Font(fontCollection.Families[0], DefaultFontSize, FontStyle.Regular);
                fontMap[String.Format("<{0}, {1}>", fontName, DefaultFontSize)] = font;
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams param = base.CreateParams;
                param.ExStyle |= 0x08000000;
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
            Settings.Default.SchemeIndex = schemeComboBox.SelectedIndex;
            Settings.Default.Save();
        }
    }
}
