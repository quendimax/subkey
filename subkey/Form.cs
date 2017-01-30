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
        private PrivateFontCollection fontCollection = new PrivateFontCollection();

        public Form()
        {
            InitializeComponent();
            InitFonts();
            LoadKeyboardScheme();
            TopMost = true;
        }

        private Button BuildButton(string text, string toolTipText)
        {
            var button = new Button();
            button.Text = text;
            button.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;
            button.Font = new Font(fontCollection.Families[0], 16F, FontStyle.Bold);
            var toolTip = new ToolTip();
            toolTip.SetToolTip(button, toolTipText);
            return button;
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
                    XmlNode node = key.FirstChild;
                    do
                    {
                        switch (node.Name)
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
                        }
                        node = node.NextSibling;
                    }
                    while (node != null);
                    tableLayout.Controls.Add(BuildButton(text, tooltip));
                }
            }
        }

        private void InitFonts()
        {
            var fontData = Resources.RomanCyrillic_Std;
            IntPtr buf = Marshal.AllocCoTaskMem(fontData.Length);
            Marshal.Copy(fontData, 0, buf, fontData.Length);
            fontCollection.AddMemoryFont(buf, fontData.Length);
            Marshal.FreeCoTaskMem(buf);
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
