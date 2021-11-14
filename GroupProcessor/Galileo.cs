using System;
using HostAccess;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace GroupProcessor
{
    class Galileo
    {

        TerminalEmulation te = new TerminalEmulation();
        ComponentResourceManager strings = new ComponentResourceManager(typeof(Locales));
 
        public bool processBooking()
        {
            GP_Form.transaction = ""; // This command will be issued after return to the smartpoint
            string rulaStatus = "";

            string reply = issueCommand("*R"); // Reply to *R determines a console status
            if (reply.Contains("SIGN IN") ) // Check if terminal is signed in
            {
                MessageBox.Show(strings.GetString("son_fail"), 
                    strings.GetString("unable"), 
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                GP_Form.transaction = "SON/";
                return false; // no need to close program if not signed
                //return true;
            }
            if (reply.Contains("NOT AUTHORISED ")) // Check if signed to emulation
            {
                MessageBox.Show(strings.GetString("sem_fail"), 
                    strings.GetString("unable"),
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Warning);
                GP_Form.transaction = "SEM/";
                return true;
            }
            if (String.IsNullOrEmpty(reply)) // Communication failure
            {
                MessageBox.Show(strings.GetString("comms_error"),
                    strings.GetString("unable"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                GP_Form.transaction = "";
                return true;
            }
            if (!reply.Contains("NO B.F.")) // Check for active BF
            {
                MessageBox.Show(strings.GetString("active_bf"), 
                    strings.GetString("unable"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                GP_Form.transaction = "*ALL{ENTER}";
                return true;
            }

            Display.SetOutput(); // Switching to Terminal window layout
            try
            {
                if (!string.IsNullOrEmpty(GP_Form.hostCommand))
                {
                    Display.Comment(strings.GetString("host_command")); // First command
                    Send(GP_Form.hostCommand);
                }
                Display.Comment(strings.GetString("creating_bf")); // Creating segments
                Send("N.G/" + GP_Form.paxNbr + GP_Form.groupName);
                Send("P.T*" + GP_Form.phone);
                Send("T.T*");
                Send("RL." + GP_Form.host + "*" + GP_Form.locator);
                if (!string.IsNullOrEmpty(GP_Form.notification)) Send("NP." + GP_Form.notification);
                string rulaCmd = GP_Form.rula.ToUpper();
                if (!string.IsNullOrEmpty(rulaCmd)) 
                {
                    if (rulaCmd.Contains("RULA")) // accepts all combinations RULAPCC/CODE or PCC/CODE or CODE
                        rulaCmd = rulaCmd.Replace("RULA", "");
                    if (!rulaCmd.Contains("/"))
                        rulaCmd = "/" + rulaCmd;
                    rulaStatus = Send("RULA" + rulaCmd);
                }
                foreach (Flight flight in GP_Form.flights)
                {
                    if (!flight.Number.Contains("ERROR"))
                    {
                        string command = "0" + flight.Number + flight.T_class + flight.Date + flight.From + flight.To
                            + "AK" + GP_Form.paxNbr;
                        if (Send(command).Contains("FLIGHT NOT FOUND"))
                        {
                            // command += "/" + flight.Dep + flight.Arr.Replace("+","/").Replace("-","/-");
                            command += "/" + flight.Dep + flight.Arr.Substring(0, 4);
                            reply = Send(command);
                            if (flight.Arr.Length > 4) // arrival day modification needed
                            {
                                try
                                {
                                    string[] lines = reply.Split('\n'); // find current flight index
                                    int fltIndex = 0;
                                    foreach (string line in lines)
                                    {
                                        if (line.Contains("CHECK FLIGHT DETAILS")) break;
                                        fltIndex++;
                                    }
                                    string modifier = flight.Arr.Substring(4, 2).Replace("+", "");
                                    Send("@" + fltIndex + "/" + flight.Dep + flight.Arr.Substring(0, 4) + "/" + modifier);
                                }
                                catch { }
                            }
                        }
                    }
                }
                Send("R." + Global.Signature);
                reply = "";
                reply = Send("ER");
                if (reply.Contains("ONLY PERMITTED BY AGENT SINE")) // only AG duty can book
                {
                    Send("I");
                    MessageBox.Show(strings.GetString("only_agent"), 
                        strings.GetString("unable"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    GP_Form.transaction = "SEM/";
                    return true;
                }
                if (reply.Contains("MINIMUM CONNECT") | reply.Contains("CHECK CONTINUITY")) reply = Send("ER"); // Revalidate BF
                if (!reply.Contains(GP_Form.groupName)) // Some error - not a BF display
                {
                    GP_Form.transaction = "*ALL{ENTER}";
                    if (MessageBox.Show(strings.GetString("some_error") + "\n\n" + strings.GetString("backToSmartpoint"),
                        strings.GetString("unable"),
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning) == DialogResult.Yes) return true;
                    return false;
                }

                Display.Comment(strings.GetString("creating_names")); // Adding PAX names
                foreach (Passenger pax in GP_Form.outPax)
                {
                    reply = Send("N." + pax.Number + pax.Names);
                    if (reply.Trim() != "*") // Some error - cannot add a PAX
                    {
                        GP_Form.transaction = "*ALL{ENTER}";
                        if (MessageBox.Show(strings.GetString("incomplete") + "\n\n" + strings.GetString("backToSmartpoint"),
                            strings.GetString("unable"),
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning) == DialogResult.Yes) return true;
                        return false;
                    }
                }
                if (GP_Form.infants.Count > 0)
                {
                    Display.Comment(strings.GetString("adding_infants")); // Adding Infants
                    foreach (Infant infant in GP_Form.infants)
                    {
                        Send("N.I/" + infant.Name + "*" + infant.Birth);
                    }
                }
                Send("R." + Global.Signature); // Finishing group booking
                reply = Send("ER");
                if (reply.Contains("MINIMUM CONNECT") | reply.Contains("CHECK CONTINUITY")) reply = Send("ER"); // Revalidate BF
                if (!reply.Contains(GP_Form.groupName)) // Some error - not a BF display
                {
                    GP_Form.transaction = "*ALL{ENTER}";
                    if (MessageBox.Show(strings.GetString("some_error") + "\n\n" + strings.GetString("backToSmartpoint"),
                        strings.GetString("unable"),
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning) == DialogResult.Yes) return true;
                    return false;
                }
                Display.Comment(strings.GetString("result"));
                Send("*ALL");
                if (rulaStatus.Contains("INVALID FORMAT"))
                {
                    if (Global.UiLanguage.Contains("cs"))
                        Display.Alert("Chybný RULA příkaz " + GP_Form.rula);
                    else
                        Display.Alert("Invalid format of RULA command " + GP_Form.rula);
                }
                if (rulaStatus.Contains("DOES NOT EXIST"))
                {
                    // INVALID - RULE 79YE/SKYPRO DOES NOT EXIST
                    Regex reg = new Regex(@"(?<=RULE )\w+(?=/)");
                    string pcc = reg.Match(rulaStatus).Value;
                    if (Global.UiLanguage.Contains("cs"))
                        Display.Alert("Pseudo city " + pcc + " nezná Custom Check pravidlo " + GP_Form.rula);
                    else
                        Display.Alert("Custom Check Rule " + GP_Form.rula + " does not exist in pseudo city " + pcc);
                }
                Display.Comment(strings.GetString("creating_finished"));
                GP_Form.transaction = "*ALL{ENTER}";
                if (MessageBox.Show(strings.GetString("bf_created") + "\n\n" + strings.GetString("backToSmartpoint"), 
                    "   *** OK ***",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information) == DialogResult.Yes) return true;
                return false;
            }
            catch
            {
                MessageBox.Show(strings.GetString("comms_error"), 
                    strings.GetString("unable"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                GP_Form.transaction = "";
                return true;
            }
        }

        public string Send(string Transaction) // Displays and issues command and host reply
        {
            Display.Input(">" + Transaction);
            string reply = issueCommand(Transaction);
            try
            {
                while (reply.Substring(reply.Length - 1, 1) == ")")
                {
                    reply = reply.Substring(0, reply.Length - 1);
                    reply += issueCommand("MR");
                }
            }
            catch { }
            Display.Output(reply);
            if (reply.Contains("PREVIOUS ENTRY IN PROGRESS")) throw new Exception("Communication failure");
            return reply;
        }

        private string issueCommand(string Transaction) // Sends transaction and returns host reply
        {
            string answer = "";
            short i;
            try
            {
                {
                    te.MakeEntry("<FORMAT>" + Transaction + "</FORMAT>");
                    for (i = 0; i <= te.NumResponseLines - 1; i++)
                    {
                        if (i > 0) answer += "\n";
                        string reply = te.get_ResponseLine(i)
                            .Replace("<CARRIAGE_RETURN/>", "")
                            .Replace("<SOM/>", "")
                            .Replace("<EOM/>", "")
                            .Replace("<PILLOW/>", "@")
                            .Replace("<TABSTOP/>", " ")
                            .Replace("NEWLINE", Environment.NewLine);
                        answer += reply;
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Host transaction failed:\n\n" + e,
                    "   *** ERROR ***",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Stop);
            }
            return answer;
        }

    }

    public static class Display
    {
        public static void Output(string text)
        {
            if (!text.TrimStart().StartsWith("*")) text = "\n" + text + "\n";
            sendText(text, Color.YellowGreen);
        }

        public static void Input(string text)
        {
            sendText(text, Color.Yellow);
        }

        public static void Comment(string text)
        {
            sendText(text + "\n", Color.Orange);
        }

        public static void Alert(string text)
        {
            sendText(text + "\n", Color.Red);
        }

        private static void sendText(string text, Color color)
        {
            var form = Form.ActiveForm as GP_Form;
            if (form != null) form.displayText(text, color);
        }

        public static void SetOutput()
        {
            var form = Form.ActiveForm as GP_Form;
            if (form != null) form.setOutput();
        }

    }
}
