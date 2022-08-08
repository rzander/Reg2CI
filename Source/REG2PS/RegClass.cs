using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;

namespace REG2CI
{
    public class RegFile
    {
        internal string _filename;
        internal List<string> lines;
        public static bool x64 = false;
        public static bool bApplication = true;
        public static bool bPSScript = true;
        public static XmlDocument xDoc = new XmlDocument();
        internal static string LogicalName = "";
        public string Description = "Reg2CI (c) 2022 by Roger Zander";

        public RegFile(string fileName, string CIName)
        {
            string sAllLines = fileName;

            if (File.Exists(fileName))
                sAllLines = File.ReadAllText(fileName, Encoding.ASCII);
            //Detect if reg contains x64 related keys
            if (sAllLines.ToLower().Contains("wow6432node"))
                x64 = true;
            new RegFile(fileName, x64, CIName);
        }
        public RegFile(string fileName, bool X64, string CIName)
        {
            x64 = X64;
            _filename = fileName;

            //generate XML Body
            //InitXML(CIName);
            string SettingName = CIName;

            string sAllLines = fileName;

            if (File.Exists(fileName))
                sAllLines = File.ReadAllText(fileName, Encoding.ASCII);

            sAllLines = sAllLines.Replace("\\" + Environment.NewLine, "");

            lines = sAllLines.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
            RegKeys = new List<RegKey>();
            RegValues = new List<RegValue>();

            int iPos = 0;
            int iLastDescription = -1;
            int iLastKey = -1;
            bool bHKLM = false;
            bool bHKCU = false;

            foreach (string sLine in lines)
            {
                try
                {
                    string Line = sLine.TrimStart();
                    //Detect Comments
                    if (Line.StartsWith(";"))
                    {
                        iLastDescription = iPos;
                    }
                    //Detect Keys
                    if (Line.StartsWith("["))
                    {
                        iLastKey = iPos;
                        RegKey oKey = new RegKey(sLine, iPos);

                        //Get Description
                        if (iLastDescription == iPos - 1 & iLastDescription > -1)
                            oKey.Description = lines[iLastDescription].TrimStart().Substring(1);

                        //RegKey rKey = new RegKey(sLine, iPos);
                        if (oKey.PSHive == "HKLM:")
                            bHKLM = true;
                        if (oKey.PSHive == "HKCU:")
                            bHKCU = true;
                        if (oKey.PSHive == @"Registry::\HKEY_USERS")
                            bHKLM = true;


                        RegKeys.Add(oKey);
                    }
                    //Detect Values
                    if (Line.StartsWith("@") || Line.StartsWith("\""))
                    {
                        RegValue oValue = new RegValue(Line, iLastKey);
                        //Get Description
                        if (iLastDescription == iPos - 1 & iLastDescription > -1)
                            oValue.Description = lines[iLastDescription].TrimStart().Substring(1);

                        RegValues.Add(oValue);
                    }
                }
                catch (Exception ex)
                {
                    ex.Message.ToString();
                }
                iPos++;
            }

            /*if (bPSScript)
            {
                if (bHKLM)
                    CreatePSXMLAll(SettingName, Description, "HKLM:");
                if (bHKCU)
                    CreatePSXMLAll(SettingName, Description, "HKCU:");
            }*/
        }

        public string fileName
        {
            get { return _filename; }
        }

        public static List<RegKey> RegKeys { get; set; }

        public static List<RegValue> RegValues { get; set; }

        public class RegKey
        {
            public RegKey(string fullKey, int id)
            {
                ID = id;
                FullKey = fullKey.TrimStart();

                //Detect if Key must be removed
                if (FullKey[FullKey.IndexOf('[') + 1] == '-')
                {
                    Action = KeyAction.Remove;
                }
                else
                    Action = KeyAction.Add;
            }

            public int ID { get; set; }
            public string FullKey { get; set; }

            public KeyAction Action { get; set; }

            public string Description { get; set; }

            public string Hive
            {
                get
                {
                    if (Action == KeyAction.Remove)
                        return FullKey.Replace("[-", "").Split('\\')[0];
                    else
                        return FullKey.Replace("[", "").Split('\\')[0];
                }
            }

            public string PSHive
            {
                get
                {
                    switch (Hive.ToUpper())
                    {
                        case "HKEY_LOCAL_MACHINE":
                            return "HKLM:";
                        case "HKEY_CURRENT_USER":
                            return "HKCU:";
                        case "HKEY_USERS":
                            return @"Registry::\HKEY_USERS";
                        case "HKEY_CLASSES_ROOT":
                            return "HKLM:\\SOFTWARE\\Classes";
                        default:
                            return "";
                    }
                }
            }

            public string Path
            {
                get
                {
                    if (x64)
                    {
                        if (Action == KeyAction.Remove)
                            return FullKey.Replace("[-" + Hive + "\\", "").Replace("]", "");
                        else
                            return FullKey.Replace("[" + Hive + "\\", "").Replace("]", "");
                    }
                    else
                    {
                        string NewPath = "";
                        if (Action == KeyAction.Remove)
                            NewPath = FullKey.Replace("[-" + Hive + "\\", "").Replace("]", "");
                        else
                            NewPath = FullKey.Replace("[" + Hive + "\\", "").Replace("]", "");
                        int n = NewPath.IndexOf("\\Wow6432Node", System.StringComparison.InvariantCultureIgnoreCase);
                        if (n >= 0)
                        {
                            NewPath = NewPath.Substring(0, n)
                                + ""
                                + NewPath.Substring(n + "\\Wow6432Node".Length);
                        }
                        return NewPath.TrimEnd();
                    }
                }
            }

            public string PSCheck
            {
                get
                {
                    if (Action == KeyAction.Add)
                    {
                        return string.Format("Test-Path -LiteralPath \"{0}\"", PSHive + "\\" + Path);
                    }
                    else
                    {
                        return string.Format("!(Test-Path -LiteralPath \"{0}\")", PSHive + "\\" + Path);
                    }
                }
            }

            public string PSRemediate
            {
                get
                {
                    if (Action == KeyAction.Add)
                    {
                        //Create key if it does not exist
                        return string.Format("if((Test-Path -LiteralPath \"{0}\") -ne $true) {{  New-Item \"{0}\" -force -ea SilentlyContinue }}", PSHive + "\\" + Path);
                        // return string.Format("New-Item \"{0}\" -ea SilentlyContinue", PSHive + ":\\" + Path);
                    }
                    else
                    {
                        return string.Format("Remove-Item -LiteralPath \"{0}\" -force", PSHive + "\\" + Path);
                    }
                }
            }

        }

        public class RegValue
        {
            internal string _name;
            internal string _value;
            internal string _svalue;
            internal Int32 _i32Value;
            internal Int64 _i64Value;
            public RegValue(string regline, int keyID)
            {
                KeyID = keyID;
                _name = regline.Substring(0, regline.IndexOf('='));
                _value = regline.Substring(regline.IndexOf('=') + 1).Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Trim(); //Replace values for XML-File

                if (_value.StartsWith("-"))
                    Action = KeyAction.Remove;
                else
                    Action = KeyAction.Add;
            }

            public RegValue(string Name, string Value, string Type, int keyID)
            {
                KeyID = keyID;
                _name = Name.Replace("**Del.", "").Replace("**del.", "");
                _value = Value.Trim();

                if (Name.ToLower().StartsWith("**del."))
                    Action = KeyAction.Remove;
                else
                    Action = KeyAction.Add;

                if (Type.ToUpper() == "REG_DWORD")
                    _value = "dword:" + _value;
                if (Type.ToUpper() == "REG_QWORD")
                    _value = "qword:" + _value;
                if (Type.ToUpper() == "REG_SZ")
                    _value = "\"" + _value + "\"";
                if (Type.ToUpper() == "REG_EXPAND_SZ")
                    _value = "\"" + _value + "\"";
            }

            public RegKey Key
            {
                get
                {
                    return RegKeys.FirstOrDefault(t => t.ID == KeyID);
                }
            }

            public int KeyID { get; set; }

            public string Description { get; set; }

            public KeyAction Action { get; set; }

            public ValueType DataType
            {
                get
                {
                    if (_value.StartsWith("\""))
                        return ValueType.String;
                    if (_value.ToLower().StartsWith("dword:"))
                        return ValueType.DWord;
                    if (_value.ToLower().StartsWith("hex(b):"))
                        return ValueType.QWord;
                    if (_value.ToLower().StartsWith("qword:"))
                        return ValueType.QWord;
                    if (_value.ToLower().StartsWith("hex(7):"))
                        return ValueType.MultiString;
                    if (_value.ToLower().StartsWith("hex(2):"))
                        return ValueType.ExpandString;
                    if (_value.ToLower().StartsWith("hex(0):"))
                        return ValueType.Binary;
                    if (_value.ToLower().StartsWith("hex:"))
                        return ValueType.Binary;


                    return ValueType.String;
                }
            }

            public string Name
            {
                get
                {
                    string sName = _name.Replace("\"", "");
                    if (sName.StartsWith("@"))
                        sName = sName.Replace("@", "(default)");
                    return sName;
                }
            }

            public object Value
            {
                get
                {
                    if (Action == KeyAction.Add)
                    {
                        if (DataType == ValueType.String)
                         {
                            //string sResult = _value.Substring(_value.IndexOf('"') + 1);
                            //string sResult = _value.TrimStart('"').TrimEnd('"').TrimEnd('"');
                            string sResult = _value.TrimStart('"');
                            if (sResult.EndsWith("\""))
                                sResult = sResult.Remove(sResult.Length - 1);

                            //if (!string.IsNullOrEmpty(sResult))
                            //    sResult = sResult.Substring(0, _value.LastIndexOf('"') - 1);
                            //if (sResult.Contains(@":\\"))
                            //    sResult = sResult.Replace("\\\\", "\\");
                            if (sResult.Contains("\\\\"))
                                sResult = sResult.Replace("\\\\", "\\");

                            sResult = sResult.Replace(@"\""", "\"");
                            _svalue = sResult;
                            return sResult;
                        }
                        if (DataType == ValueType.DWord)
                        {
                            _svalue = "0x" + _value.Substring(_value.IndexOf(':') + 1);
                            try
                            {
                                Int32 iResult = Convert.ToInt32(_svalue, 16); //Int32.Parse(_svalue);
                                _i32Value = iResult;
                                _i64Value = iResult;
                                _svalue = iResult.ToString();
                                return iResult;
                            }
                            catch { }

                            return _svalue;
                        }
                        if (DataType == ValueType.QWord)
                        {
                            if (_value.StartsWith("hex(b):", StringComparison.CurrentCultureIgnoreCase))
                            {
                                string[] aHex = _value.Replace("hex(b):", "").Replace(" ", "").Split(',');
                                List<byte> bRes = new List<byte>();
                                _svalue = "0x";
                                foreach (string sVal in aHex.Reverse())
                                {
                                    _svalue += sVal;
                                }

                                Int64 iResult = Convert.ToInt64(_svalue, 16);
                                _i64Value = iResult;
                                _svalue = iResult.ToString();

                                return iResult;
                            }
                            else
                            {
                                _svalue = "0x" + _value.Substring(_value.IndexOf(':') + 1);
                                try
                                {
                                    Int64 iResult = Convert.ToInt64(_svalue, 16);  //Int64.Parse(_svalue);
                                    _i64Value = iResult;
                                    _svalue = "0x" + iResult.ToString();
                                    return iResult;
                                }
                                catch { }
                            }
                            return _svalue;
                        }
                        if (DataType == ValueType.MultiString)
                        {
                            //return Encoding.UTF8.GetString(Encoding.Unicode.GetBytes(_value.Replace("hex(7):", "").Replace(" ", "")));
                            string[] aHex = _value.Replace("hex(7):", "").Replace(" ", "").Split(',');
                            List<byte> bRes = new List<byte>();

                            foreach (string sVal in aHex)
                            {
                                bRes.Add(Convert.ToByte(sVal, 16));
                            }

                            string sResult = Encoding.Unicode.GetString(bRes.ToArray());
                            _svalue = "\"" + string.Join(",", sResult.TrimEnd('\0').Split('\0')) + "\"";
                            return sResult.TrimEnd('\0').Split('\0');
                        }
                        if (DataType == ValueType.Binary)
                        {
                            string[] aHex = _value.Replace("hex:", "").Replace(" ", "").Split(',');

                            if (_value.StartsWith("hex(0):", StringComparison.CurrentCultureIgnoreCase))
                            {
                                aHex = _value.Replace("hex(0):", "").Replace(" ", "").Split(',');
                                _svalue = "";
                                return aHex;
                            }
                            else
                            {
                                _svalue = "([byte[]](";
                                foreach (string sVal in aHex)
                                {
                                    _svalue += "0x" + sVal + ",";
                                }
                                _svalue = _svalue.TrimEnd(',');
                                _svalue += "))";
                                return aHex;
                            }
                        }
                        if (DataType == ValueType.ExpandString)
                        {
                            //return Encoding.UTF8.GetString(Encoding.Unicode.GetBytes(_value.Replace("hex(7):", "").Replace(" ", "")));
                            string[] aHex = _value.Replace("hex(2):", "").Replace(" ", "").Split(',');

                            List<byte> bRes = new List<byte>();

                            foreach (string sVal in aHex)
                            {
                                bRes.Add(Convert.ToByte(sVal, 16));
                            }

                            string sResult = Encoding.Unicode.GetString(bRes.ToArray());
                            sResult = sResult.TrimEnd('\0');
                            _svalue = string.Join(",", sResult.TrimEnd('\0').Split('\0'));
                            return _svalue;
                        }
                    }
                    return "";
                }
            }

            public string PSCheck
            {
                get
                {
                    string PSHive = Key.PSHive;
                    string Path = Key.Path;

                    if (Action == KeyAction.Add)
                    {
                        Value.ToString(); //We need to calculate the Value to have an _sValue

                        if (DataType == ValueType.Binary)
                        {
                            if(string.IsNullOrEmpty(_svalue))
                                return "try{ if((Get-ItemPropertyValue -LiteralPath '{PATH}' -Name '{NAME}').length -eq 0) { $true } else { $false }} catch { $false }".Replace("{PATH}", PSHive + "\\" + Path).Replace("{NAME}", Name).Replace("{VALUE}", _svalue); //PS Issue in ErrorAction for Get-ItemPropertyValue

                            return "if((Get-ItemPropertyValue -LiteralPath '{PATH}' -Name '{NAME}' -ea SilentlyContinue) -join ',' -eq ({VALUE} -join ',')) { $true } else { $false }".Replace("{PATH}", PSHive + "\\" + Path).Replace("{NAME}", Name).Replace("{VALUE}", _svalue);
                        }

                        if (DataType == ValueType.MultiString)
                        {
                            return "if((Get-ItemPropertyValue -LiteralPath '{PATH}' -Name '{NAME}' -ea SilentlyContinue) -join ',' -eq '{VALUE}') { $true } else { $false }".Replace("{PATH}", PSHive + "\\" + Path).Replace("{NAME}", Name).Replace("{VALUE}", _svalue);
                        }

                        if (DataType == ValueType.ExpandString)
                        {
                            return "if((Get-ItemPropertyValue -LiteralPath '{PATH}' -Name '{NAME}' -ea SilentlyContinue) -eq ({VALUE})) { $true } else { $false }".Replace("{PATH}", PSHive + "\\" + Path).Replace("{NAME}", Name).Replace("{VALUE}", $"[System.Environment]::ExpandEnvironmentVariables('{_svalue}')");
                        }

                        if (DataType == ValueType.String)
                        {
                            return "if((Get-ItemPropertyValue -LiteralPath '{PATH}' -Name '{NAME}' -ea SilentlyContinue) -eq '{VALUE}') { $true } else { $false }".Replace("{PATH}", PSHive + "\\" + Path).Replace("{NAME}", Name).Replace("{VALUE}", _svalue);
                        }

                        return "if((Get-ItemPropertyValue -LiteralPath '{PATH}' -Name '{NAME}' -ea SilentlyContinue) -eq {VALUE}) { $true } else { $false }".Replace("{PATH}", PSHive + "\\" + Path).Replace("{NAME}", Name).Replace("{VALUE}", _svalue);
                    }
                    else
                    {
                        Value.ToString(); //We need to calculate the Value to have an _sValue
                        if (Value.GetType() == typeof(string[]))
                        {
                            return "if((Get-ItemProperty -LiteralPath '{PATH}' -Name '{NAME}' -ea SilentlyContinue) -join ',' -eq $null) { $true } else { $false }".Replace("{PATH}", PSHive + "\\" + Path).Replace("{NAME}", Name);
                        }
                        return "if((Get-ItemProperty -LiteralPath '{PATH}' -Name '{NAME}' -ea SilentlyContinue) -eq $null) { $true } else { $false }".Replace("{PATH}", PSHive + "\\" + Path).Replace("{NAME}", Name);
                    }
                }
            }

            public string PSRemediate
            {
                get
                {
                    string PSHive = Key.PSHive;
                    string Path = Key.Path;

                    if (Action == KeyAction.Add)
                    {
                        Value.ToString(); //We need to calculate the Value to have an _sValue
                        if (DataType == ValueType.Binary)
                        {
                            if (string.IsNullOrEmpty(_svalue))
                                return "New-ItemProperty -LiteralPath '{PATH}' -Name '{NAME}' -Value (New-Object Byte[] 0) -PropertyType None -Force -ea SilentlyContinue".Replace("{PATH}", PSHive.Trim() + "\\" + Path.Trim()).Replace("{NAME}", Name.Trim());

                            return "New-ItemProperty -LiteralPath '{PATH}' -Name '{NAME}' -Value {VALUE} -PropertyType {TARGETTYPE} -Force -ea SilentlyContinue".Replace("{PATH}", PSHive.Trim() + "\\" + Path.Trim()).Replace("{NAME}", Name.Trim()).Replace("{VALUE}", _svalue).Replace("{TARGETTYPE}", DataType.ToString());
                        }
                        if (DataType == ValueType.ExpandString)
                        {
                            return "New-ItemProperty -LiteralPath '{PATH}' -Name '{NAME}' -Value '{VALUE}' -PropertyType {TARGETTYPE} -Force -ea SilentlyContinue".Replace("{PATH}", PSHive.Trim() + "\\" + Path.Trim()).Replace("{NAME}", Name.Trim()).Replace("{VALUE}", Value.ToString().Trim()).Replace("{TARGETTYPE}", DataType.ToString());
                        }

                        if (DataType == ValueType.String)
                        {
                            return "New-ItemProperty -LiteralPath '{PATH}' -Name '{NAME}' -Value '{VALUE}' -PropertyType {TARGETTYPE} -Force -ea SilentlyContinue".Replace("{PATH}", PSHive.Trim() + "\\" + Path.Trim()).Replace("{NAME}", Name.Trim()).Replace("{VALUE}", Value.ToString().Trim()).Replace("{TARGETTYPE}", DataType.ToString());
                        }

                        if (DataType == ValueType.MultiString)
                        {
                            string sPSVal = "@(";
                            foreach (string sVal in Value as string[])
                            {
                                sPSVal += "\"" + sVal + "\",";
                            }
                            sPSVal = sPSVal.TrimEnd(',');
                            sPSVal += ")";
                            return "New-ItemProperty -LiteralPath '{PATH}' -Name '{NAME}' -Value {VALUE} -PropertyType {TARGETTYPE} -Force -ea SilentlyContinue".Replace("{PATH}", PSHive.Trim() + "\\" + Path.Trim()).Replace("{NAME}", Name.Trim()).Replace("{VALUE}", sPSVal).Replace("{TARGETTYPE}", DataType.ToString());
                        }

                        string sResult = _svalue; //.Replace(@"\""", @"`"""); Why?
                        return "New-ItemProperty -LiteralPath '{PATH}' -Name '{NAME}' -Value {VALUE} -PropertyType {TARGETTYPE} -Force -ea SilentlyContinue".Replace("{PATH}", PSHive.Trim() + "\\" + Path.Trim()).Replace("{NAME}", Name.Trim()).Replace("{VALUE}", sResult.Trim()).Replace("{TARGETTYPE}", DataType.ToString());
                    }
                    else
                    {
                        Value.ToString(); //We need to calculate the Value to have an _sValue
                        if (Value.GetType() == typeof(string[]))
                        {
                            return "Remove-ItemProperty -LiteralPath '{PATH}' -Name '{NAME}' -Force -ea SilentlyContinue".Replace("{PATH}", PSHive.Trim() + "\\" + Path.Trim()).Replace("{NAME}", Name.Trim()).Replace("{VALUE}", _svalue).Replace("{TARGETTYPE}", DataType.ToString());
                        }
                        return "Remove-ItemProperty -LiteralPath '{PATH}' -Name '{NAME}' -Force -ea SilentlyContinue".Replace("{PATH}", PSHive.Trim() + "\\" + Path.Trim()).Replace("{NAME}", Name.Trim()).Replace("{VALUE}", _svalue).Replace("{TARGETTYPE}", DataType.ToString());
                    }
                }
            }
        }

        public string GetPSCheckAll()
        {
            string sResult = "# " + Description + Environment.NewLine;
            sResult += "try {" + Environment.NewLine;

            foreach (RegKey oKey in RegKeys)
            {
                sResult = sResult + "\tif(-NOT (" + oKey.PSCheck + ")){ return $false };" + Environment.NewLine;
            }

            if (RegValues.Count > 0)
            {
                foreach (RegValue oVal in RegValues)
                {
                    if (!string.IsNullOrEmpty(oVal.Description))
                    {
                        sResult += Environment.NewLine;
                        sResult += "# " + oVal.Description + Environment.NewLine;
                    }

                    sResult = sResult + "\t" + oVal.PSCheck.Replace("$true", "").Replace("$false", "return $false") + ";" + Environment.NewLine;
                }
            }

            sResult += "}" + Environment.NewLine; ;
            sResult += "catch { return $false }" + Environment.NewLine; ;
            sResult += "return $true";
            return sResult;
        }

        public string GetPSCheckAll(string PSHive)
        {
            string sResult = "# " + Description + Environment.NewLine;

            foreach (RegValue oVal in RegValues.Where(t => t.Key.PSHive == PSHive))
            {
                sResult = sResult + oVal.PSCheck.Replace("$true", "").Replace("$false", "return $false") + ";" + Environment.NewLine;
            }
            sResult += "return $true";
            return sResult;
        }

        public string GetPSRemediateAll()
        {
            string sResult = "# " + Description + Environment.NewLine;

            //Get Keys only once
            foreach (RegKey oKey in RegKeys.GroupBy(t => t.FullKey).Select(y => y.First()))
            {
                if (!string.IsNullOrEmpty(oKey.Description))
                {
                    sResult += Environment.NewLine;
                    sResult += "# " + oKey.Description + Environment.NewLine;
                }
                sResult = sResult + oKey.PSRemediate + ";" + Environment.NewLine;
            }

            foreach (RegValue oVal in RegValues)
            {
                if (!string.IsNullOrEmpty(oVal.Description))
                {
                    sResult += Environment.NewLine;
                    sResult += "# " + oVal.Description + Environment.NewLine;
                }
                sResult = sResult + oVal.PSRemediate + ";" + Environment.NewLine;
            }

            return sResult;
        }

        public string GetPSRemediateAll(string PSHive)
        {
            string sResult = "# " + Description + Environment.NewLine;

            if (PSHive == "HKLM:")
            {
                //Get Keys only once
                foreach (RegKey oVal in RegKeys.Where(t => t.PSHive != "HKCU:").GroupBy(t => t.FullKey).Select(y => y.First()))
                {
                    if (!string.IsNullOrEmpty(oVal.Description))
                    {
                        sResult += Environment.NewLine;
                        sResult += "# " + oVal.Description + Environment.NewLine;
                    }

                    sResult = sResult + oVal.PSRemediate + ";" + Environment.NewLine;
                }

                foreach (RegValue oVal in RegValues.Where(t => t.Key.PSHive != "HKCU:"))
                {
                    if (!string.IsNullOrEmpty(oVal.Description))
                    {
                        sResult += Environment.NewLine;
                        sResult += "# " + oVal.Description + Environment.NewLine;
                    }

                    sResult = sResult + oVal.PSRemediate + ";" + Environment.NewLine;
                }
            }
            else
            {
                //Get Keys only once
                foreach (RegKey oVal in RegKeys.Where(t => t.PSHive == PSHive).GroupBy(t => t.FullKey).Select(y => y.First()))
                {
                    if (!string.IsNullOrEmpty(oVal.Description))
                    {
                        sResult += Environment.NewLine;
                        sResult += "# " + oVal.Description + Environment.NewLine;
                    }

                    sResult = sResult + oVal.PSRemediate + ";" + Environment.NewLine;
                }

                foreach (RegValue oVal in RegValues.Where(t => t.Key.PSHive == PSHive))
                {
                    if (!string.IsNullOrEmpty(oVal.Description))
                    {
                        sResult += Environment.NewLine;
                        sResult += "# " + oVal.Description + Environment.NewLine;
                    }

                    sResult = sResult + oVal.PSRemediate + ";" + Environment.NewLine;
                }
            }

            return sResult;
        }

    }

    public enum KeyAction
    {
        Add, Remove
    }

    public enum ValueType
    {
        String, DWord, QWord, MultiString, Binary, ExpandString
    }
}
