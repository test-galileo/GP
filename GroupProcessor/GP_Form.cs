using System;
using System.ComponentModel;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Runtime.InteropServices;

namespace GroupProcessor
{
    partial class GP_Form : Form
    {
        [DllImport("User32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        private const int SW_RESTORE = 9;
        [DllImport("User32.dll")]
        static extern int SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32", CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowScrollBar(IntPtr hwnd, int wBar, [MarshalAs(UnmanagedType.Bool)] bool bShow);
        [DllImport("user32.dll", EntryPoint = "SendMessage")]
        private static extern IntPtr SendMessage(IntPtr hwnd, int wMsg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(HandleRef hwnd, out RECT lpRect);
        [DllImport("user32.dll")]
        private static extern long LockWindowUpdate(IntPtr Handle);
        const int LVM_FIRST = 0x1000;
        const int LVM_GETHEADER = (LVM_FIRST + 31);

        ComponentResourceManager strings = new ComponentResourceManager(typeof(Locales));
        public Color backgroudColor = Color.DimGray;
        public Color alertColor = Color.Salmon;
        public static string locator = "";
        public static string groupName = "";
        public static string paxNbr = "";
        public static string host;
        public static string phone;
        public static string notification;
        public static string rula;
        public static string hostCommand;
        public static List<Passenger> outPax = new List<Passenger>();
        public static List<Flight> flights = new List<Flight>();
        public static List<Infant> infants = new List<Infant>();
        public static List<string> duplicates = new List<string>();
        public static List<string> missingFirstNames = new List<string>();
        public static int airlineIndex;
        public bool editing = true;
        public bool initialised = false;
        public static string transaction = "";
        ArrayList buffer = new ArrayList();

        [Serializable, StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public GP_Form()
        {
            CultureInfo defaultCulture = new CultureInfo(Global.UiLanguage);
            //Thread.CurrentThread.CurrentCulture = defaultCulture;
            Thread.CurrentThread.CurrentUICulture = defaultCulture;

            Parameters.init();
            this.DoubleBuffered = true;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture(Global.UiLanguage);
            Keyboard kbd = new Keyboard();
            if (Global.EnglishKbd)
                InputLanguage.CurrentInputLanguage = kbd.checkEn();
            InitializeComponent();
        }

        private void GP_Form_Load(object sender, EventArgs e)
        {
            airlineIndex = Global.AirlineIndex;
            int index = 0;
            foreach (string airline in Global.Airlines)
            {
                airlineComboBox.Items.Insert(index, airline);
                index++;
            }
            airlineComboBox.SelectedIndex = airlineIndex;
            phone_textBox.Text = Global.PhoneField;
            notification_TextBox.Text = Global.NoteField;
            rula_textBox.Text = Global.RulaField;
            process_button.Select();
            Application.DoEvents();
            input_Paste();
            readPNR();
            initialised = true;
        }

        private void readPNR()
        {
            switch (host)
            {
                case "1A":
                    Get_1A _1A = new Get_1A();
                    _1A.Booking(inputText_richTextBox.Text.ToUpper() + "\n");
                    host = "1A";
                    break;
                case "EK":
                    Get_EK _EK = new Get_EK();
                    _EK.Booking(inputText_richTextBox.Text.ToUpper() + "\n");
                    host = "EK";
                    break;
                default:
                    MessageBox.Show(host + " not implemented yet");
                    airlineIndex = Global.AirlineIndex;
                    airlineComboBox.SelectedIndex = airlineIndex;
                    return;
            }
            refreshForm();
        }

        private void refreshForm()
        {
            ok_label.Visible = false;
            process_button.Enabled = false;
            errorList_label.Text = "";
            int errorCount = 0;

            locator_textBox.Text = locator;
            if (string.IsNullOrEmpty(locator))
            {
                locator_textBox.BackColor = alertColor;
                errorList_label.Text += strings.GetString("missing_locator") + "\n";
                errorCount++;
            }
            else
                locator_textBox.BackColor = backgroudColor;

            groupName_textBox.Text = groupName;
            if (string.IsNullOrEmpty(groupName))
            {
                groupName_textBox.BackColor = alertColor;
                errorList_label.Text += strings.GetString("missing_groupname") + "\n";
                errorCount++;
            }
            else
                groupName_textBox.BackColor = backgroudColor;

            setBlack();
            if (Global.CheckFirstNames)
            {
                //LockWindowUpdate(this.Handle);
                if (missingFirstNames.Count > 0)
                {
                    errorList_label.Text += strings.GetString("missing_firstname") + "\n";
                    errorCount++;
                    highlightErrors(missingFirstNames);
                }
                //LockWindowUpdate(IntPtr.Zero);
            }
            if (Global.CheckDuplicates)
            {
                //LockWindowUpdate(this.Handle);
                if (duplicates.Count > 0)
                {
                    errorList_label.Text += strings.GetString("duplicated_names") + "\n";
                    errorCount++;
                    highlightErrors(duplicates);
                }
                //LockWindowUpdate(IntPtr.Zero);
            }

            nbrPax_textBox.Text = paxNbr;
            int paxCount = 0;
            this.label5.Text = strings.GetString("pax_list");
            this.label5.ForeColor = Color.Black;
            this.label5.Font = new Font(this.label5.Font, FontStyle.Regular);
            foreach (Passenger pax in outPax)
                paxCount += pax.Number;
            paxList_richTextBox.Clear();
            if (!string.IsNullOrEmpty(paxNbr))
            {
                nbrPax_textBox.BackColor = backgroudColor;
                paxList_richTextBox.BackColor = backgroudColor;
                if (paxCount < Convert.ToInt32(paxNbr))
                {
                    if (Global.Incomplete)
                    {
                        this.label5.Text = strings.GetString("missing_names");
                        this.label5.ForeColor = Color.Red;
                        this.label5.Font = new Font(this.label5.Font, FontStyle.Bold);
                    }
                    else
                    {
                        paxList_richTextBox.BackColor = alertColor;
                        errorList_label.Text += strings.GetString("missing_names") + "\n";
                        errorCount++;
                    }

                }
                if (paxCount > Convert.ToInt32(paxNbr))
                {
                    if (Global.Incomplete)
                    {
                        this.label5.Text = strings.GetString("extra_names");
                        this.label5.ForeColor = Color.Red;
                        this.label5.Font = new Font(this.label5.Font, FontStyle.Bold);
                    }
                    else
                    {
                        paxList_richTextBox.BackColor = alertColor;
                        errorList_label.Text += strings.GetString("extra_names") + "\n";
                        errorCount++;
                    }
                }
                this.label5.Text += (" - " + paxCount + " Pax");

                foreach (Passenger pax in outPax)
                    paxList_richTextBox.AppendText(pax.Number + pax.Names + "\n");
                if (infants.Count > 0)
                {
                    this.label5.Text += (" + " + infants.Count + " Inf");
                    paxList_richTextBox.AppendText("+\n");
                    foreach (Infant infant in infants)
                        paxList_richTextBox.AppendText("INFANT " + infant.Name + "*" + infant.Birth + "\n");
                }
            }
            else
            {
                nbrPax_textBox.BackColor = alertColor;
                paxList_richTextBox.BackColor = alertColor;
                errorList_label.Text += strings.GetString("missing_paxnumber") + "\n";
                errorCount++;
            }

            flight_listView.Clear();
            fltListInit();
            flight_listView.BackColor = backgroudColor;
            Font listFont = new Font("Courier New", 10, FontStyle.Bold, GraphicsUnit.Point, 0);
            int validFlights = flights.Count;
            int rowIndex = 0;
            if (validFlights == 0)
                flight_listView.BackColor = alertColor;
            foreach (Flight flight in flights)
            {
                bool dateBeyond = false;
                if (!checkDate(flight.Date))
                {
                    dateBeyond = true;
                    if (!flight.Number.Contains("ERROR"))
                        flight.Number += " ERROR";
                }
                string[] row = { flight.Number,flight.T_class,flight.Date,flight.Day,
                        flight.From, flight.To,flight.Status,flight.Pax,flight.Terminal,flight.Dep,flight.Arr };
                flight_listView.Items.Add(new ListViewItem(row));
                flight_listView.Items[rowIndex].Font = listFont;
                if (flight.Number.Contains("ERROR")) // Display error item in alert color
                {
                    flight_listView.Items[rowIndex].UseItemStyleForSubItems = false;
                    flight_listView.Items[rowIndex].SubItems[0].BackColor = alertColor;
                    if (flight.Status != "HK") // Incorrect booking status
                        flight_listView.Items[rowIndex].SubItems[6].BackColor = alertColor;
                    if (flight.Pax != paxNbr) // Incorrect PAX number
                        flight_listView.Items[rowIndex].SubItems[7].BackColor = alertColor;
                    if (dateBeyond) // Flight date beyond the limit
                        flight_listView.Items[rowIndex].SubItems[2].BackColor = alertColor;
                    if (flight.Date == "") // Unreadable flight data
                    {
                        flight_listView.Items[rowIndex].UseItemStyleForSubItems = true;
                        flight_listView.Items[rowIndex].BackColor = alertColor;
                    }
                    validFlights--;
                    for (int i = 0; i < 11; i++) // Restore list font
                        flight_listView.Items[rowIndex].SubItems[i].Font = listFont;
                }
                rowIndex++;
            }
            if (flights.Count > 5)
                ShowScrollBar(flight_listView.Handle, 1, true);


            if (flights.Count != validFlights)
                errorList_label.Text += strings.GetString("ignored") + (flights.Count - validFlights) + "\n";

            if (validFlights == 0)
            {
                errorList_label.Text += strings.GetString("no_flights") + "\n";
                errorCount++;
            }

            if (errorCount > 2)
                errorList_label.Text = strings.GetString("no_pnr") + host + " PNR\n";

            if (Global.PhoneMandatory && string.IsNullOrEmpty(phone_textBox.Text.Trim()))
            {
                if (!errorList_label.Text.Contains(strings.GetString("no_phone")))
                {
                    phone_textBox.BackColor = alertColor;
                    if (!string.IsNullOrEmpty(errorList_label.Text)) errorList_label.Text += "\n";
                    errorList_label.Text += strings.GetString("no_phone");
                    errorCount++;
                }
            }
                else
                    phone_textBox.BackColor = Color.White;

            if (Global.HostCommandMandatory && string.IsNullOrEmpty(hostCommand_textBox.Text.Trim()))
            {
                if (!errorList_label.Text.Contains(strings.GetString("no_hostCommand")))
                {
                    hostCommand_textBox.BackColor = alertColor;
                    if (!string.IsNullOrEmpty(errorList_label.Text))
                        errorList_label.Text += "\n"; // Upravit!!!
                    errorList_label.Text += strings.GetString("no_hostCommand");
                    errorCount++;
                }
            }
            else
                hostCommand_textBox.BackColor = Color.White;

            if (errorCount == 0)
            {
                if (String.IsNullOrEmpty(errorList_label.Text))
                    ok_label.Visible = true;
                process_button.Enabled = true;
            }
        }

        private void highlightErrors(List<string> errorNames) // (this RichTextBox myRtb, string word, Color color)
        {
            int cursor = inputText_richTextBox.SelectionStart;
            //MessageBox.Show("Starting hghligtts");
            //List<string> names = errorNames;
            try
            {
                foreach (string name in errorNames)
                {
                    //MessageBox.Show(name);
                    Regex regExp = new Regex(@"(?<=[\. \d\n])" + name + @"(?=[ \(\b\n])");
                    foreach (Match match in regExp.Matches(inputText_richTextBox.Text))
                    {
                        inputText_richTextBox.Select(match.Index, match.Length);
                        //inputText_richTextBox.SelectionBackColor = alertColor;
                        inputText_richTextBox.SelectionColor = Color.Red;
                    }
                }
            }
            //catch(Exception ex) { MessageBox.Show(ex.Message); }
            catch { }
            inputText_richTextBox.DeselectAll();
            inputText_richTextBox.SelectionStart = cursor;
        }

        private void setBlack()
        {
            int cursor = inputText_richTextBox.SelectionStart;
            inputText_richTextBox.SelectAll();
            inputText_richTextBox.SelectionColor = Color.Black;
            inputText_richTextBox.SelectionBackColor = Color.White;
            inputText_richTextBox.DeselectAll();
            inputText_richTextBox.SelectionStart = cursor;
        }

        private bool checkDate(string date)
        {
            try
            {
                DateTime temp = DateTime.ParseExact(date, "ddMMM",
                    System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                if (temp < DateTime.Now.AddDays(-2)) temp = temp.AddYears(1);
                if (DateTime.Now.AddDays(Global.MaxDays) < temp) return false;
            }
            catch { return false; }
            return true;
        }

        private void paste_button_Click(object sender, EventArgs e)
        {
            editing = true;
            setInput();
            input_Paste();
        }

        private void process_button_Click(object sender, EventArgs e)
        {
            editing = false;
            phone = phone_textBox.Text;
            notification = notification_TextBox.Text;
            rula = rula_textBox.Text;
            hostCommand = hostCommand_textBox.Text;
            try
            {
                Galileo galileo = new Galileo();
                if (galileo.processBooking()) exit();
            }
            catch (Exception ex)
            {
                editing = true;
                MessageBox.Show(strings.GetString("missing_viewpoint") + "\n\n" + ex.Message,
                    strings.GetString("unable"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void input_Paste()
        {
            // Check if there is any text on clipboard.
            if (Clipboard.GetDataObject().GetDataPresent(DataFormats.Text))
            {
                string input = (string)Clipboard.GetData("Text");
                inputText_richTextBox.Text = input
                    .Replace(Environment.NewLine + Environment.NewLine, Environment.NewLine)
                    .ToUpper();
            }
            else
            {
                inputText_richTextBox.Text = strings.GetString("empty_clipboard");
                paste_button.Select();
            }
        }

        private void inputText_richTextBox_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            e.KeyChar = Convert.ToChar(e.KeyChar.ToString().ToUpper());
        }

        private void inputText_richTextBox_TextChanged(object sender, EventArgs e)
        {
            if (editing && initialised) 
                readPNR();
        }

        private void fltListInit()
        {
            flight_listView.Clear();
            flight_listView.View = View.Details;
            flight_listView.GridLines = true;
            flight_listView.FullRowSelect = true;
            ShowScrollBar(flight_listView.Handle, 1, false);

            //Add column headers
            int width = flight_listView.Width / 12;
            flight_listView.Columns.Add(strings.GetString("list_flight"), Convert.ToInt32(width * 1.8), HorizontalAlignment.Left);
            flight_listView.Columns.Add(strings.GetString("list_class"), width, HorizontalAlignment.Center);
            flight_listView.Columns.Add(strings.GetString("list_date"), width, HorizontalAlignment.Center);
            flight_listView.Columns.Add(strings.GetString("list_day"), width, HorizontalAlignment.Center);
            flight_listView.Columns.Add(strings.GetString("list_origin"), width, HorizontalAlignment.Center);
            flight_listView.Columns.Add(strings.GetString("list_dest"), width, HorizontalAlignment.Center);
            flight_listView.Columns.Add(strings.GetString("list_status"), width, HorizontalAlignment.Center);
            flight_listView.Columns.Add(strings.GetString("list_pax"), width, HorizontalAlignment.Center);
            flight_listView.Columns.Add(strings.GetString("list_terminal"), width, HorizontalAlignment.Center);
            flight_listView.Columns.Add(strings.GetString("list_dep"), width, HorizontalAlignment.Center);
            flight_listView.Columns.Add(strings.GetString("list_arr"), -2, HorizontalAlignment.Left);
            RECT rc = new RECT();
            int headerHeight = 0;
            IntPtr hwnd = SendMessage(flight_listView.Handle, LVM_GETHEADER, IntPtr.Zero, IntPtr.Zero);
            if (hwnd != null)
                if (GetWindowRect(new HandleRef(null, hwnd), out rc))
                    headerHeight = rc.Bottom - rc.Top;
            int row = (flight_listView.ClientRectangle.Height - headerHeight - 5) / 5;
            ImageList dummy = new ImageList();
            dummy.ImageSize = new Size(1, row);
            dummy.TransparentColor = Color.Transparent;
            flight_listView.SmallImageList = dummy;
        }

        private void setup_button_Click(object sender, EventArgs e)
        {
            string oldInputBox = inputText_richTextBox.Text;
            CultureInfo oldUILang = Thread.CurrentThread.CurrentUICulture;
            Size formSize = this.Size;
            Setup setup = new Setup();
            setup.ShowDialog(this);
            if (Thread.CurrentThread.CurrentUICulture != oldUILang)
            {
                LockWindowUpdate(this.Handle);
                Language.Set(this, Thread.CurrentThread.CurrentUICulture);
                this.Width = formSize.Width;
                this.Height = formSize.Height;
                inputText_richTextBox.Text = oldInputBox;
                LockWindowUpdate(IntPtr.Zero);
            }
            if (!string.IsNullOrEmpty(Global.PhoneField)) 
                phone_textBox.Text = Global.PhoneField;
            if (!string.IsNullOrEmpty(Global.NoteField)) 
                notification_TextBox.Text = Global.NoteField;
            if (!string.IsNullOrEmpty(Global.RulaField)) 
                rula_textBox.Text = Global.RulaField;
            readPNR();
        }

        private void phone_textBox_TextChanged(object sender, EventArgs e)
        {
            if (initialised && Global.PhoneMandatory) readPNR();
        }

        private void exit_button_Click(object sender, EventArgs e)
        {
            exit();
        }

        private void GP_Form_Closing(object sender, EventArgs e)
        {
            exit();
        }

        private void exit()
        {
            //if (!String.IsNullOrEmpty(transaction))
            //    sendToSmartpoint(transaction);
            this.Dispose();
            Application.Exit();
        }

        private void phone_textBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.KeyChar = Convert.ToChar(e.KeyChar.ToString().ToUpper());
        }

        private void notification_TextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.KeyChar = Convert.ToChar(e.KeyChar.ToString().ToUpper());
        }

        private void rula_textBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.KeyChar = Convert.ToChar(e.KeyChar.ToString().ToUpper());
        }

        public void displayText(string text, Color color)
        {
            inputText_richTextBox.SelectionColor = color;
            inputText_richTextBox.AppendText(text);
            inputText_richTextBox.ScrollToCaret();
        }

        public void setInput()
        {
            groupBox1.Text = strings.GetString("groupbox_editing");
            inputText_richTextBox.ReadOnly = false;
            inputText_richTextBox.Clear();
            inputText_richTextBox.BackColor = Color.White;
            inputText_richTextBox.ForeColor = Color.Black;
        }

        public void setOutput()
        {
            groupBox1.Text = strings.GetString("groupbox_terminal");
            //inputText_richTextBox.ReadOnly = true;
            inputText_richTextBox.Clear();
            inputText_richTextBox.BackColor = Color.Black;
            inputText_richTextBox.ForeColor = Color.Yellow;
        }

        private void airlineComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            host = airlineComboBox.Text.Left(2);
            airlineIndex = airlineComboBox.SelectedIndex;
            if (initialised) 
                readPNR();
        }

        //private bool sendToSmartpoint(string transaction)
        //{
        //    int handle = 0;
        //    Process[] processes = Process.GetProcessesByName("viewpoint");
        //    foreach (Process p in processes) { handle = (int)p.MainWindowHandle; }
        //    if (handle != 0)
        //    {
        //        ShowWindow((IntPtr)handle, SW_RESTORE);
        //        SetForegroundWindow((IntPtr)handle);
        //        Thread.Sleep(200);
        //        SendKeys.SendWait(transaction);
        //        return true;
        //    }
        //    else
        //    {
        //        MessageBox.Show("Galileo Smartpoint not running");
        //        return false;
        //    }
        //}

        //private bool sendToDesktop(string transaction)
        //{
        //    IntPtr handle = IntPtr.Zero;
        //    bool smartpoint = false;
        //    transaction = transaction.Replace(">", null).Replace(Environment.NewLine, null);
        //    Clipboard.SetText(transaction); // copy to clipboard
        //    Process[] processes = Process.GetProcessesByName("viewpoint");
        //    foreach (Process p in processes) { handle = p.MainWindowHandle; }
        //    if (handle != IntPtr.Zero)
        //    {
        //        Process[] processlist = Process.GetProcesses();
        //        foreach (Process process in processlist)
        //        {
        //            if (process.ProcessName.Contains("Smartpoint"))
        //            {
        //                smartpoint = true;
        //                alertLabel.Visible = true;
        //                Application.DoEvents();
        //                Thread.Sleep(2000);
        //                alertLabel.Visible = false;
        //                Application.DoEvents();
        //                break;
        //            }
        //        }
        //        if (IsIconic(handle))
        //            ShowWindow(handle, SW_RESTORE); // restore Desktop if minimized
        //        SetForegroundWindow(handle);
        //        if (!smartpoint)  // Smartpoint not running
        //        {
        //            Thread.Sleep(200);
        //            SendKeys.SendWait(@"^v");  // paste FQP to Viewpoint and send ENTER
        //            SendKeys.SendWait("{ENTER}");
        //        }
        //        return true;
        //    }
        //    else
        //    {
        //        MessageBox.Show("Galileo Desktop not running\nFQP command has been placed on Clipboard",
        //            "Unable to issue FQP",
        //            MessageBoxButtons.OK,
        //            MessageBoxIcon.Error);
        //        return false;
        //    }
        //}

        private void inputText_richTextBox_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.V)
            {
                ((RichTextBox)sender).Paste(DataFormats.GetFormat("Text"));
                e.Handled = true;
            }
        }

        private void hostCommand_textBox_TextChanged(object sender, EventArgs e)
        {
            if (initialised && Global.PhoneMandatory) 
                readPNR();
        }

        private void hostCommand_textBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.KeyChar = Convert.ToChar(e.KeyChar.ToString().ToUpper());
        }
    }

    static class Language
    {
        // http://stackoverflow.com/questions/3558406/how-to-change-language-at-runtime-without-layout-troubles

        internal static void Set(this Form form, CultureInfo lang)
        {
            ComponentResourceManager resources = new ComponentResourceManager(form.GetType());

            ApplyResourceToControl(resources, form, lang);
            resources.ApplyResources(form, "$this", lang);
        }

        private static void ApplyResourceToControl(ComponentResourceManager resources, Control control, CultureInfo lang)
        {
            foreach (Control c in control.Controls)
            {
                ApplyResourceToControl(resources, c, lang);
                resources.ApplyResources(c, c.Name, lang);
            }
        }
    }

    class Keyboard
    {
        public InputLanguage checkEn()
        {
            foreach (InputLanguage lang in InputLanguage.InstalledInputLanguages)
                if (lang.Culture.ToString().ToLower().Contains("en")) 
                    return lang;

            return null;
        }
    }

    public class Passenger
    {
        public int Number;
        public string Names;
        public Passenger(int number, string names)
        { Number = number; Names = names; }
    }

    public class Infant
    {
        public string Name;
        public string Birth;
        public Infant(string name, string birth)
        { Name = name; Birth = birth; }
    }

    public class Flight
    {
        public string Number;
        public string T_class;
        public string Date;
        public string Day;
        public string From;
        public string To;
        public string Status;
        public string Pax;
        public string Terminal;
        public string Dep;
        public string Arr;
        //public bool DateError;

        public Flight(string number, string t_class, string date, string day,
            string from, string to, string status, string pax, string terminal, string dep, string arr) //, bool dateError)
        {
            Number = number; T_class = t_class; Date = date; Day = day; From = from; To = to;
            Status = status; Pax = pax; Terminal = terminal; Dep = dep; Arr = arr; //DateError = dateError;
        }
    }

    public class MyRtb : RichTextBox
    {

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetScrollPos(IntPtr hWnd, int nBar);

        [DllImport("user32.dll")]
        public static extern int SetScrollPos(IntPtr hWnd, int nBar, int nPos, bool bRedraw);

        private const int SB_VERT = 0x1;

        public int VerticalPosition
        {
            get { return GetScrollPos((IntPtr)this.Handle, SB_VERT); }
            set { SetScrollPos((IntPtr)this.Handle, SB_VERT, value, true); }
        }
    }

}

