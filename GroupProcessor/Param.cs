using System;
using System.IO;
using System.Collections;
using System.Windows.Forms;
using System.ComponentModel;
using System.Threading;
using System.Globalization;
using System.Reflection;

namespace GroupProcessor
{
    static class Global
    {
        // Signature
        public static string Signature = "GROUP-PROCESSOR";

        // Supported Airlines
        public static string[] Airlines =
        { 
            "1A - Amadeus", 
            "EK - Emirates",
        };


        // INI file name & location
        // public static string iniFileName = Path.GetDirectoryName(Application.ExecutablePath) + @"\" + "gp.ini";
        public static string iniFileName = Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData) + @"\GroupProcessor\gp.ini";

        // Default parameters
        public static bool PhoneMandatory = true;
        public static string PhoneField = "";
        public static string NoteField = "";
        public static string RulaField = "";
        public static bool EnglishKbd = false;
        public static string UiLanguage = "en-US";
        public static int AirlineIndex = 0;
        public static bool Incomplete = false;
        public static bool HostCommandMandatory = false;

        // Limitations
        public static int MaxPax = 99;
        public static int MaxChar = 55;
        public static int MaxDays = 335;

        public static bool CheckDuplicates = true;
        public static bool CheckFirstNames = true;

    }

    class iniFileName
    {
        public string get(string x)
        { return x; }
    }

    public static class Parameters
    {

        static ComponentResourceManager strings = new ComponentResourceManager(typeof(Locales));

        public static void init()
        {
            if (!File.Exists(Global.iniFileName))
            {
                // Error - control file does not exist
                MessageBox.Show(String.Format("{0}\n{1}\n{2}",
                    strings.GetString("ctrlfile_missing"), Global.iniFileName,
                    strings.GetString("ctrlfile_default")),
                    strings.GetString("ctrlfile_warning"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                string iniPath = Path.GetDirectoryName(Global.iniFileName);
                DirectoryInfo di = Directory.CreateDirectory(iniPath);
                writeIni();
                Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture(Global.UiLanguage);
                Setup setup = new Setup();
                setup.ShowDialog();
            }
            readIni();
        }

        public static void readIni()
        {
            try
            {
                using (StreamReader sr = File.OpenText(Global.iniFileName))
                {
                    string input = "";
                    while ((input = sr.ReadLine()) != null)
                    {
                        switch (getParam(input))
                        {
                            case "Note:": { Global.NoteField = readParam(input); break; }
                            case "Phone:": { Global.PhoneField = readParam(input); break; }
                            case "RULA:": { Global.RulaField = readParam(input); break; }
                            case "Phone Mandatory:": { Global.PhoneMandatory = Convert.ToBoolean(readParam(input)); break; }
                            case "Command Mandatory:": { Global.HostCommandMandatory = Convert.ToBoolean(readParam(input)); break; }
                            case "Language:": { Global.UiLanguage = readParam(input); break; }
                            case "EN/US kbd:": { Global.EnglishKbd = Convert.ToBoolean(readParam(input)); break; }
                            case "Airline:": { Global.AirlineIndex = Convert.ToInt32(readParam(input)); break; }
                            case "Incomplete:": { Global.Incomplete = Convert.ToBoolean(readParam(input)); break; }
                        }
                    }
                }
            }
            catch { }
        }

        public static void writeIni()
        {
            using (StreamWriter sw = File.CreateText(Global.iniFileName))
            {
                sw.WriteLine("Group Processor INI File");
                sw.WriteLine("------------------------");
                sw.WriteLine("Note:              " + Global.NoteField);
                sw.WriteLine("Phone:             " + Global.PhoneField);
                sw.WriteLine("RULA:              " + Global.RulaField);
                sw.WriteLine("Phone Mandatory:   " + Global.PhoneMandatory);
                sw.WriteLine("Command Mandatory: " + Global.HostCommandMandatory);
                sw.WriteLine("Language:          " + Global.UiLanguage);
                sw.WriteLine("EN/US kbd:         " + Global.EnglishKbd);
                sw.WriteLine("Airline:           " + Global.AirlineIndex);
                sw.WriteLine("Incomplete:        " + Global.Incomplete);                
                sw.WriteLine("");
                sw.WriteLine("Saved by software version " + Assembly.GetEntryAssembly().GetName().Version.ToString());
            }
        }

        static string getParam(string line)
        {
            try { return line.Substring(0, line.IndexOf(":") + 1); }
            catch { return line; }
        }

        static string readParam(string line)
        {
            return line.Substring(line.IndexOf(":") + 1).Trim();
        }    
    }

    public class ReadFile
    {
        private ArrayList inputdata = new ArrayList();

        public ReadFile(string fileName)
        {
            StreamReader sr;
            using (sr = new StreamReader(fileName))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (!string.IsNullOrEmpty(line)) this.inputdata.Add(line);
                }
            }
        }

        public System.Collections.ArrayList Inputdata
        {
            get { return this.inputdata; }
        }
    }

}


