using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GroupProcessor
{

    //ELSTPJ
    // 1.C/11OAZAGROUP  2. 1DOHNAL/JANMR  3. 1DOHNALOVA/JINDRISKAMRS
    // 4. 1FILIPOVA/MARCELAMRS  5. 1HANULIAKOVA/JARMILAMRS
    // 6. 1NOBILISOVA/MICHAELAMRS  7. 1NOBILISOVA/MILOSLAVAMRS
    // 8. 1OBRATIL/JANMR  9. 1OBRATILOVA/MARIEMRS
    //10. 1ROMANKOVA/MILADAMRS 11. 1STOJANOVA/ALICEMRS
    //12. 1ZILA/ANTONINMR
    // 1 EK  140 G 24FEB PRGDXB HK11      1550-7 0055-1 Y-G
    // 2 EK  650 G 25FEB DXBCMB HK11      0245-1 0830-1 Y-G
    // 3 EK  349 G  5MAR CMBDXB HK11      0255-2 0555-2 Y-G
    // 4 EK  139 G  5MAR DXBPRG HK11      0905-2 1235-2 Y-G
    //FONE
    // 1 PRGEK-T 00420 539 000 253 STUDENT AGENCY - AGT ZBYNEK GRUFIK

    
    class Get_EK
    {        
        public void Booking(string pnr)
        {
            GP_Form.outPax.Clear();
            GP_Form.infants.Clear();
            GP_Form.flights.Clear();
            int pointer = 0; // Start to search items from PNR beggining
            GP_Form.locator = findRegex(@"(\b)(\w{6})((?= *\n *1.C/))", pnr, ref pointer);
            GP_Form.paxNbr = findRegex(@"((?<=1.C/))(\d*)((?=[A-Z]))", pnr, ref pointer);
            GP_Form.groupName = findRegex(@"((?<=1.C/\d*))([A-Z]*)((?=\b))", pnr, ref pointer);

            int index = 2;   // Start to search PAX names from index 2
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
            index = 1;
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
                        //if (line.Contains("E*")) processFlight(items);
                        processFlight(items);
                        index++;
                        if (items[0] == (index + 1).ToString()) index++; // Jump over missing segment index
                    }
                }
           }

            // Try to get PAX nbr from first flight if NM: PAX number missing
            //if (string.IsNullOrEmpty(GP_Form.paxNbr))
            //{
            //    if (GP_Form.flights.Count > 0) GP_Form.paxNbr = GP_Form.flights[0].Pax;
            //}

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
                    //if (surname.Length > 2 && surname.Right(3) == "OVA") gender += "S";
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
                    GP_Form.outPax.Add(new Passenger(familyCount, family.TrimEnd('!').TrimEnd('/')));
                }
                inCount++;
            }
        }

        private void processFlight(string[] flightInfo)
        {
            // 11  OK 636 G 10OCT 4 PRGBRU HK10      2  1805 1935   *1A/E*        
            // 1 EK  140 G 24FEB PRGDXB HK11      1550-7 0055-1 Y-G
            string fltNumber = "";
            try
            {
                int index = 1;
                fltNumber = flightInfo[index]; index++;
                fltNumber += flightInfo[index]; index++; 
                string fltClass = flightInfo[index]; index++;
                string fltDate = flightInfo[index]; index++;
                if (fltDate.Length < 5) fltDate = "0" + fltDate;
                string fltFrom = flightInfo[index].Left(3);
                string fltTo = flightInfo[index].Substring(3, 3); index++;
                string fltStatus = flightInfo[index].Left(2);
                string fltPax = flightInfo[index].Substring(2, flightInfo[index].Length - 2); index++;
                string fltTerminal = "";
                //if (flightInfo[index].Length < 4) { fltTerminal += flightInfo[index]; index++; }
                string fltDay = flightInfo[index].Substring(5,1);
                string fltDep = flightInfo[index].Left(4); index++;
                string fltArr = flightInfo[index].Left(4);
                int depDay = Convert.ToInt32(fltDay);
                int arrDay = Convert.ToInt32(flightInfo[index].Substring(5, 1));
                if (depDay != arrDay)
                {
                    int arrOffset = arrDay - depDay;
                    if (arrOffset < -4) arrOffset += 7;
                    if (arrOffset > 2) arrOffset -= 7;
                    string offsetString = arrOffset.ToString();
                    if (arrOffset > 0) offsetString = "+" + offsetString;
                    if (arrOffset < 3 && arrOffset > -2) fltArr += offsetString;
                }
                if (fltPax != GP_Form.paxNbr | fltStatus != "HK") fltNumber += " ERROR";
                GP_Form.flights.Add(new Flight(fltNumber, fltClass, fltDate, fltDay, fltFrom, fltTo, fltStatus, fltPax, fltTerminal, fltDep, fltArr));
            }
            catch
            {
                if (string.IsNullOrEmpty(fltNumber)) fltNumber = "FLIGHT";
                GP_Form.flights.Add(new Flight(fltNumber + " ERROR", "", "", "", "", "", "", "", "", "", ""));
            }
        }

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
            {
                pointer = (reg.Match(inputText, pointer).Groups[3].Index);
            }
            return result.Replace(" ", "");
        }

        private string findPax(int index, string inputText, ref int pointer)
        {
            //3. 1DOHNALOVA/JINDRISKAMRS
            Regex reg = new Regex(@"(?<=\b" + index + @"\. \d)([A-Z /]*)(\([A-Z /]*(\d{2}[A-Z]{3}\d{2}))*((?=\d{1,2}|\n|\)))");
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
