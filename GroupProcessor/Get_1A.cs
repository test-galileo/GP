using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GroupProcessor
{
    class Get_1A
    {         
        public void Booking(string pnr)
        {
            GP_Form.outPax.Clear();
            GP_Form.infants.Clear();
            GP_Form.flights.Clear();
            int pointer = 0; // Start to search items from PNR beggining
            //GP_Form.locator = findRegex(@"(\b)(\w{6})((?= *\n*0\. ))", pnr, ref pointer);
            GP_Form.locator = findRegex(@"(\b)(\w{6})(?= *\n)", pnr, ref pointer);
            GP_Form.groupName = findRegex(@"((?<=0\. +0))([\w/ ]*)((?=NM:))", pnr, ref pointer);
            GP_Form.paxNbr = findRegex(@"((?<=\bNM: ?))(\d*)((?=\b))", pnr, ref pointer);

            int index = 1;   // Start to search PAX names from index 1
            List<string[]> paxNames = new List<string[]>();
            List<string> foundNames = new List<string>();
            GP_Form.duplicates.Clear();
            GP_Form.missingFirstNames.Clear();
            for (; ; )
            {
                string pax = findPax(index, pnr, ref pointer);
                if (string.IsNullOrEmpty(pax))
                    break;
                string[] paxRecord = pax.Split('('); // Split to PAX and optional Infant parts

                // Duplicates search
                if (Global.CheckDuplicates)
                {
                    foundNames.Add(paxRecord[0]);
                    List<string> tempNames = new List<string>();
                    foreach (string name in foundNames)
                    {
                        //GP_Form.duplicates.Add(name);
                        if (!tempNames.Contains(name))
                            tempNames.Add(name);
                        else
                            if (!GP_Form.duplicates.Contains(name))
                                GP_Form.duplicates.Add(name);
                    }
                }

                // Passenger processing
                string[] temp = new string[2];
                if (paxRecord[0].Contains("/")) { temp = paxRecord[0].Split('/'); }
                else { temp[0] = paxRecord[0]; temp[1] = ""; }
                paxNames.Add(temp);
                // Infant processing
                if (paxRecord.Length > 1) infantFromList(paxRecord[1], temp[0]);
                index++;
            }

            // Search for valid flight information line by line
            string[] lines = pnr.Split('\n');
            char[] delimiter = new char[] { ' ' };
            foreach (string line in lines)
            {
                string[] items = line.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);

                if (items.Length > 0)
                {
                    // Match line index with expected flight segment (or segment + 1)
                    if ((items[0] == index.ToString()) | (items[0] == (index + 1).ToString()))
                    {
                        if (line.Contains("E*")) processFlight(items);
                        index++;
                        if (items[0] == (index + 1).ToString()) index++; // Jump over missing segment index
                    }
                }
                // Infant processing from SSR
                // 16 SSR INFT OK HK1 ZEMAN/MILOS 10JAN12/S11/P1
                //if (items.Length > 2)
                //{
                //    if (items[1] == "SSR" && items[2] == "INFT") infantFromSsr(items);
                //}
            }

            // Try to get PAX nbr from first flight if NM: PAX number missing
            if (string.IsNullOrEmpty(GP_Form.paxNbr)) 
            {
                if (GP_Form.flights.Count > 0) GP_Form.paxNbr = GP_Form.flights[0].Pax;
            }

            int listSize = paxNames.Count;
            bool[] processed = new bool[listSize];
            for (int i = 0; i < listSize; i++) processed[i] = false;
            int inCount = 0;
            foreach (string[] pax in paxNames) // pax[0] = surname, pax[1] = first name (optional)
            {
                if (!processed[inCount])
                {
                    processed[inCount] = true;
                    string surname = pax[0];
                    //string gender = "MR";
                    //if (surname.Length > 2 && surname.Right(3) == "OVA")
                    //    gender += "S";
                    int familyCount = 1;
                    string family = surname + "/";
                    bool firstName = (!string.IsNullOrEmpty(pax[1]));
                    //if (firstName) 
                    family += pax[1];
                    //else family += gender;
                    int outCount = 0;
                    foreach (string[] nextPax in paxNames)
                    {
                        //if ((!processed[outCount]) && (nextPax[0] == surname))
                        //{
                        //    if (firstName)
                        //    {
                        //        if (((nextPax[1].Length + family.Length) < Global.MaxChar) && (!string.IsNullOrEmpty(nextPax[1])))
                        //        {
                        //            family += ("/" + nextPax[1]);
                        //            familyCount++;
                        //            processed[outCount] = true;
                        //        }
                        //    }
                        //    else
                        //    {
                        //        if (string.IsNullOrEmpty(nextPax[1]))
                        //        {
                        //            //family += ("/" + gender);
                        //            familyCount++;
                        //            processed[outCount] = true;
                        //        }
                        //    }
                        //}
                        if (familyCount == Global.MaxPax)
                            break;
                        outCount++;
                    }
                    if (Global.CheckFirstNames)
                        if (family.Right(1) == "/")
                        {
                            string blankName = family.TrimEnd('/');
                            if (!GP_Form.missingFirstNames.Contains(family))
                                GP_Form.missingFirstNames.Add(family);
                            if (!GP_Form.missingFirstNames.Contains(blankName))
                                GP_Form.missingFirstNames.Add(blankName);
                        }

                    GP_Form.outPax.Add(new Passenger(familyCount, family.TrimEnd('/')));
                }
                inCount++;
            }
        }

        private void processFlight(string[] flightInfo)
        {
            string fltNumber = "";
            try
            {
                int index = 1;
                fltNumber = flightInfo[index]; index++;
                if (fltNumber.Length < 4) { fltNumber += flightInfo[index]; index++; }
                string fltClass = flightInfo[index]; index++;
                string fltDate = flightInfo[index]; index++;
                string fltDay = flightInfo[index]; index++;
                string fltFrom = flightInfo[index].Substring(0, 3);
                string fltTo = flightInfo[index].Substring(3, 3); index++;
                string fltStatus = flightInfo[index].Substring(0, 2);
                string fltPax = flightInfo[index].Substring(2, flightInfo[index].Length - 2); index++;
                string fltTerminal = "";
                if (flightInfo[index].Length < 4) { fltTerminal += flightInfo[index]; index++; }
                string fltDep = flightInfo[index]; index++;
                string fltArr = flightInfo[index];
                if (fltPax != GP_Form.paxNbr | fltStatus != "HK") fltNumber += " ERROR";
                GP_Form.flights.Add(new Flight(fltNumber, fltClass, fltDate, fltDay, fltFrom, fltTo, fltStatus, fltPax, fltTerminal, fltDep, fltArr));
            }
            catch
            {
                if (string.IsNullOrEmpty(fltNumber)) fltNumber = "FLIGHT";
                GP_Form.flights.Add(new Flight(fltNumber + " ERROR", "", "", "", "", "", "", "", "", "", ""));
            }
        }

        //private void infantFromSsr(string[] ssr)
        //{
        //    // 16,SSR,INFT,OK,HK1,ZEMAN/MILOS,10JAN12/S11/P1
        //    try
        //    {
        //        string infName = ssr[5];
        //        string infBirth = ssr[6].Substring(0, 7);
        //        GP_Form.infants.Add(new Infant(infName, infBirth));
        //    }
        //    catch { }
        //}

        private void infantFromList(string record, string surname)
        {
            // INF/BILL/04JUN12
            try
            {
                string[] inf = record.Split('/');
                if (inf[0] == "INF") inf[0] = surname;
                else inf[0] = inf[0].Substring(3, inf[0].Length - 3);
                GP_Form.infants.Add(new Infant(inf[0] + "/" + inf[1], inf[2]));
            }
            catch { }
        }

        private string findRegex(string expression, string inputText, ref int pointer)
        {
            Regex reg = new Regex(expression);
            string result = reg.Match(inputText, pointer).Value;
            if (!string.IsNullOrEmpty(result))
                pointer = (reg.Match(inputText, pointer).Groups[3].Index);
            return result.Replace(" ", "");
        }

        private string findPax(int index, string inputText, ref int pointer)
        {
            Regex reg = new Regex(@"(?<=\b" + index + @"\.)([A-Z /]*)(\([A-Z /]*(\d{2}[A-Z]{3}\d{2}))*((?=\d{1,2}|\n|\)))");
            string paxName = reg.Match(inputText).Value.Trim();

            //string s1 = reg.Match(inputText).Groups[1].Index + " - " + reg.Match(inputText).Groups[1].Value;
            //string s2 = reg.Match(inputText).Groups[2].Index + " - " + reg.Match(inputText).Groups[2].Value;
            //string s3 = reg.Match(inputText).Groups[3].Index + " - " + reg.Match(inputText).Groups[3].Value;
            //string s4 = reg.Match(inputText).Groups[4].Index + " - " + reg.Match(inputText).Groups[4].Value;
            //MessageBox.Show("Index: " + index + "\nResult: " + result + 
            //    "\nPointer: " + pointer + "\n\n1: " + s1 + "<\n2: " + s2 + "<\n3: " + s3 + "<\n4: " + s4 + "<");

            if (!string.IsNullOrEmpty(paxName))
            {
                pointer = (reg.Match(inputText, pointer).Groups[4].Index);
            }
            // insert extra space before gender
            //try
            //{
            //    if (paxName.Right(2) == "MR")
            //        paxName = paxName.Remove(paxName.Length - 2) + " MR";
            //    if (paxName.Right(3) == "MRS")
            //        paxName = paxName.Remove(paxName.Length - 3) + " MRS";
            //    if (paxName.Right(3) == "CHD")
            //        paxName = paxName.Remove(paxName.Length - 3) + " CHD";
            //    if (paxName.Right(4) == "MISS")
            //        paxName = paxName.Remove(paxName.Length - 4) + " MISS";
            //    if (paxName.Right(4) == "MAST")
            //        paxName = paxName.Remove(paxName.Length - 4) + " MAST";
            //}
            //catch { }
            paxName = paxName.Replace("  ", " "); // remove double spaces
            //paxName = paxName.Replace("/ ", "/"); // if gender only
            return paxName;
            //return paxName.Replace(" ", "");
        }
    }
}
