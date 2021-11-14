using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

namespace GroupProcessor
{
    public partial class Setup : Form
    {
        ComponentResourceManager strings = new ComponentResourceManager(typeof(Locales));
        string localUILang = Thread.CurrentThread.CurrentUICulture.ToString();
        //string localUILang = Global.UiLanguage;

        [DllImport("user32.dll")]
        private static extern long LockWindowUpdate(IntPtr Handle);

        public Setup()
        {
            this.DoubleBuffered = true;
            InitializeComponent();
        }

        private void Setup_Load(object sender, EventArgs e)
        {
            Keyboard kbd = new Keyboard();
            if (kbd.checkEn() != null) 
                kbd_checkBox.Checked = Global.EnglishKbd;
            else
            {
                kbd_checkBox.Checked = false;
                kbd_checkBox.Enabled = false;
            }

            if (localUILang.Contains("cs"))
            {
                en_radioButton.Checked = false;
                cs_radioButton.Checked = true;
                localUILang = "cs-CZ";
            }
            else
            {
                en_radioButton.Checked = true;
                cs_radioButton.Checked = false;
                localUILang = "en-US";
            }

            versionLabel.Text = Assembly.GetEntryAssembly().GetName().Version.ToString();
            phone_checkBox.Checked = Global.PhoneMandatory;
            phone_textBox.Text = Global.PhoneField;
            hostCommand_checkBox.Checked = Global.HostCommandMandatory;
            note_textBox.Text = Global.NoteField;
            rula_textBox.Text = Global.RulaField;
            int index = 0;
            foreach (string airline in Global.Airlines)
            {
                defaultComboBox.Items.Insert(index, airline);
                index++;
            }
            defaultComboBox.SelectedIndex = GP_Form.airlineIndex;
            incomplete_checkBox.Checked = Global.Incomplete;
        }

        private void phone_textBox_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            e.KeyChar = Convert.ToChar(e.KeyChar.ToString().ToUpper());
        }

        private void note_textBox_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            e.KeyChar = Convert.ToChar(e.KeyChar.ToString().ToUpper());
        }

        private void rula_textBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.KeyChar = Convert.ToChar(e.KeyChar.ToString().ToUpper());
        }

        private void en_radioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (en_radioButton.Checked && localUILang.Contains("cs"))
            {
                localUILang = "en-US";
                CultureInfo lang = CultureInfo.CreateSpecificCulture(localUILang);
                changeLanguage(lang);
                //Language.Set(this, lang);
                //versionLabel.Text = Assembly.GetEntryAssembly().GetName().Version.ToString();
            }
        }

        private void cs_radioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (cs_radioButton.Checked && localUILang.Contains("en"))
            {
                localUILang = "cs-CZ";
                CultureInfo lang = CultureInfo.CreateSpecificCulture(localUILang);
                changeLanguage(lang);
                //Language.Set(this, lang);
                //versionLabel.Text = Assembly.GetEntryAssembly().GetName().Version.ToString();
            }
        }

        private void changeLanguage (CultureInfo lang)
        {
            LockWindowUpdate(this.Handle);
            Size formSize = this.Size;
            Language.Set(this, lang);
            versionLabel.Text = Assembly.GetEntryAssembly().GetName().Version.ToString();
            this.Width = formSize.Width;
            this.Height = formSize.Height;
            LockWindowUpdate(IntPtr.Zero);
        }

        private void kbd_checkBox_CheckedChanged(object sender, EventArgs e)
        {
            Keyboard kbd = new Keyboard();
            if (kbd_checkBox.Checked) 
                InputLanguage.CurrentInputLanguage = kbd.checkEn(); 
            else
                InputLanguage.CurrentInputLanguage = null;
        }

        private void done_button_Click(object sender, EventArgs e)
        {
            // Save
            if (localUILang.Contains("en") != Thread.CurrentThread.CurrentUICulture.ToString().Contains("en"))
                Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture(localUILang);
            Global.UiLanguage = localUILang;
            Global.EnglishKbd = kbd_checkBox.Checked;
            Global.PhoneMandatory = phone_checkBox.Checked;
            Global.HostCommandMandatory = hostCommand_checkBox.Checked;
            Global.PhoneField = phone_textBox.Text;
            Global.NoteField = note_textBox.Text;
            Global.RulaField = rula_textBox.Text;
            Global.Incomplete = incomplete_checkBox.Checked;
            //Parameters param = new Parameters();
            Parameters.writeIni();
            this.Close();
        }

        private void discard_button_Click(object sender, EventArgs e)
        {
            // Discard
            //Parameters param = new Parameters();
            Parameters.readIni();
            this.Close();
        }

        private void defaultComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            Global.AirlineIndex = defaultComboBox.SelectedIndex;
        }
    }
}
