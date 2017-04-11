using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public RegFile(string fileName, string CIName)
        {
            string sAllLines = File.ReadAllText(fileName, Encoding.ASCII);
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
            InitXML(CIName);
            string SettingName = CIName;
            String Description = "Reg2CI (c) 2017 by Roger Zander";

            string sAllLines = File.ReadAllText(fileName, Encoding.ASCII);
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
                        if (iLastDescription == iPos - 1)
                            oKey.Description = lines[iLastDescription].TrimStart().Substring(1);

                        RegKey rKey = new RegKey(sLine, iPos);
                        if (rKey.PSHive == "HKLM")
                            bHKLM = true;
                        if (rKey.PSHive == "HKCU")
                            bHKCU = true;

                        RegKeys.Add(rKey);
                    }
                    //Detect Values
                    if (Line.StartsWith("@") || Line.StartsWith("\""))
                    {
                        RegValue oValue = new RegValue(Line, iLastKey);
                        //Get Description
                        if (iLastDescription == iPos - 1)
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

            if(bPSScript)
            {
                if(bHKLM)
                    CreatePSXMLAll(SettingName, Description, "HKLM");
                if (bHKCU)
                    CreatePSXMLAll(SettingName, Description, "HKCU");
            }
}

        public RegFile(string fileName, bool X64, string CIName, bool MachinePolicy)
        {
            x64 = X64;
            _filename = fileName;

            POL oPol= new POL(fileName, MachinePolicy);


            //generate XML Body
            InitXML(CIName);
            string SettingName = CIName;
            String Description = "Reg2CI (c) 2017 by Roger Zander";

            RegKeys = new List<RegKey>();
            RegValues = new List<RegValue>();

            int iPos = 0;
            bool bHKLM = MachinePolicy;
            bool bHKCU = !MachinePolicy;

            foreach (POLPolicy oPolicy in oPol.Policies)
            {
                RegKey rKey = new RegKey(oPolicy.Key, iPos);
                RegKeys.Add(rKey);

                RegValue oValue = new RegValue(oPolicy.Value, oPolicy.Data, oPolicy.Type, iPos);
                RegValues.Add(oValue);

                iPos++;
            }

            if (bPSScript)
            {
                if (bHKLM)
                    CreatePSXMLAll(SettingName, Description, "HKLM");
                if (bHKCU)
                    CreatePSXMLAll(SettingName, Description, "HKCU");
            }
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
                            return "HKLM";
                        case "HKEY_CURRENT_USER":
                            return "HKCU";
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
                        return NewPath;
                    }
                }
            }

            public string PSCheck
            {
                get
                {
                    if (Action == KeyAction.Add)
                    {
                        return string.Format("Test-Path \"{0}\"", PSHive + ":\\" + Path);
                    }
                    else
                    {
                        return string.Format("!(Test-Path\"{0}\")", PSHive + ":\\" + Path);
                    }
                }
            }

            public string PSRemediate
            {
                get
                {
                    if (Action == KeyAction.Add)
                    {
                        return string.Format("New-Item \"{0}\" -ea SilentlyContinue", PSHive + ":\\" + Path);
                    }
                    else
                    {
                        return string.Format("Remove-Item \"{0}\" -force", PSHive + ":\\" + Path);
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
                _value = regline.Substring(regline.IndexOf('=') + 1).Replace("&","&amp;").Replace("<","&lt;").Replace(">","&gt;");

                if (_value.StartsWith("-"))
                    Action = KeyAction.Remove;
                else
                    Action = KeyAction.Add;
            }

            public RegValue(string Name, string Value, string Type, int keyID)
            {
                KeyID = keyID;
                _name = Name.Replace("**Del.", "").Replace("**del.", ""); 
                _value = Value;

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
                    if (_value.ToLower().StartsWith("qword:"))
                        return ValueType.QWord;

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
                            string sResult = _value.Substring(_value.IndexOf('"') + 1);
                            if(!string.IsNullOrEmpty(sResult))
                                sResult = sResult.Substring(0, _value.LastIndexOf('"') - 1);
                            if (sResult.Contains(@":\\"))
                                sResult = sResult.Replace("\\\\", "\\");
                            _svalue = "\"" + sResult + "\"";
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
                                _svalue = "0x" + iResult.ToString();
                                return iResult;
                            }
                            catch { }

                            return _svalue;
                        }
                        if (DataType == ValueType.QWord)
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
                        return "if((Get-ItemProperty -Path '{PATH}' -Name '{NAME}' -ea SilentlyContinue).'{NAME}' -eq {VALUE}) { $true } else { $false }".Replace("{PATH}", PSHive + ":\\" + Path).Replace("{NAME}", Name).Replace("{VALUE}", _svalue);
                    }
                    else
                    {
                        Value.ToString(); //We need to calculate the Value to have an _sValue
                        return "if((Get-ItemProperty -Path '{PATH}' -Name '{NAME}' -ea SilentlyContinue) -eq $null) { $true } else { $false }".Replace("{PATH}", PSHive + ":\\" + Path).Replace("{NAME}", Name);
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
                        return "New-ItemProperty -Path '{PATH}' -Name '{NAME}' -Value {VALUE} -PropertyType {TARGETTYPE} -Force -ea SilentlyContinue".Replace("{PATH}", PSHive + ":\\" + Path).Replace("{NAME}", Name).Replace("{VALUE}", _svalue).Replace("{TARGETTYPE}", DataType.ToString());
                    }
                    else
                    {
                        Value.ToString(); //We need to calculate the Value to have an _sValue
                        return "Remove-ItemProperty -Path '{PATH}' -Name '{NAME}' -Force -ea SilentlyContinue".Replace("{PATH}", PSHive + ":\\" + Path).Replace("{NAME}", Name).Replace("{VALUE}", _svalue).Replace("{TARGETTYPE}", DataType.ToString());
                    }
                }
            }
        }

        public string GetPSCheckAll()
        {
            string sResult = "#Reg2CI (c) 2017 by Roger Zander" + Environment.NewLine;
            sResult = sResult + "$bResult = $true;" + Environment.NewLine;
            foreach (RegValue oVal in RegValues)
            {
                sResult = sResult + oVal.PSCheck.Replace("$true",""). Replace("$false", "$bResult = $false") + ";" + Environment.NewLine;
            }
            sResult = sResult + "$bResult";
            return sResult;
        }

        public string GetPSCheckAll(string PSHive)
        {
            string sResult = "#Reg2CI (c) 2017 by Roger Zander" + Environment.NewLine;
            sResult = sResult + "$bResult = $true;" + Environment.NewLine;
            foreach (RegValue oVal in RegValues.Where(t=>t.Key.PSHive == PSHive))
            {
                sResult = sResult + oVal.PSCheck.Replace("$true", "").Replace("$false", "$bResult = $false") + ";" + Environment.NewLine;
            }
            sResult = sResult + "$bResult";
            return sResult;
        }

        public string GetPSRemediateAll()
        {
            string sResult = "#Reg2CI (c) 2017 by Roger Zander" + Environment.NewLine;

            //Get Keys only once
            foreach (RegKey oVal in RegKeys.GroupBy(t => t.FullKey).Select(y => y.First()))
            {
                sResult = sResult + oVal.PSRemediate + ";" + Environment.NewLine;
            }

            foreach (RegValue oVal in RegValues)
            {
                sResult = sResult + oVal.PSRemediate + ";" + Environment.NewLine;
            }

            return sResult;
        }

        public string GetPSRemediateAll(string PSHive)
        {
            string sResult = "#Reg2CI (c) 2017 by Roger Zander" + Environment.NewLine;

            //Get Keys only once
            foreach (RegKey oVal in RegKeys.Where(t=>t.PSHive == PSHive).GroupBy(t=>t.FullKey).Select(y => y.First()))
            {
                sResult = sResult + oVal.PSRemediate + ";" + Environment.NewLine;
            }

            foreach (RegValue oVal in RegValues.Where(t=>t.Key.PSHive == PSHive))
            {
                sResult = sResult + oVal.PSRemediate + ";" + Environment.NewLine;
            }

            return sResult;
        }

        internal void InitXML(string CIName)
        {
            if (bApplication)
            {
                xDoc.LoadXml(Properties.Settings.Default.XMLBodyApp);

                XmlNamespaceManager manager = new XmlNamespaceManager(xDoc.NameTable);
                string sDC = "http://schemas.microsoft.com/SystemsCenterConfigurationManager/2009/07/10/DesiredConfiguration";
                //string sRules = "http://schemas.microsoft.com/SystemsCenterConfigurationManager/2009/06/14/Rules";
                manager.AddNamespace("dc", sDC);
                manager.AddNamespace("rules", "http://schemas.microsoft.com/SystemsCenterConfigurationManager/2009/06/14/Rules");

                string ResourceID = "ID-" + Guid.NewGuid().ToString();
                string AuthoringScopeId = "ScopeId_709B4C9D-07C0-476D-9CF4-2E760FA01892"; // + Guid.NewGuid().ToString();
                
                XmlNode RCS = null;
                XmlNode Rules = null;
                //XmlNode xSimpleSetting = null;

                LogicalName = "Application_" + Guid.NewGuid().ToString();
                RCS = xDoc.SelectSingleNode("//dc:DesiredConfigurationDigest/dc:Application/dc:Settings/dc:RootComplexSetting", manager);

                Rules = xDoc.SelectSingleNode("//dc:DesiredConfigurationDigest/dc:Application/dc:Rules", manager);
                xDoc.SelectSingleNode("//dc:DesiredConfigurationDigest/dc:Application", manager).Attributes["AuthoringScopeId"].Value = AuthoringScopeId;
                xDoc.SelectSingleNode("//dc:DesiredConfigurationDigest/dc:Application", manager).Attributes["LogicalName"].Value = LogicalName;
                xDoc.SelectSingleNode("//dc:DesiredConfigurationDigest/dc:Application", manager).Attributes["Version"].Value = "1";

                xDoc.SelectSingleNode("//dc:DesiredConfigurationDigest/dc:Application/rules:Annotation/rules:DisplayName", manager).Attributes["ResourceId"].Value = ResourceID;
                xDoc.SelectSingleNode("//dc:DesiredConfigurationDigest/dc:Application/rules:Annotation/rules:DisplayName", manager).Attributes["Text"].Value = CIName;
                xDoc.SelectSingleNode("//dc:DesiredConfigurationDigest/dc:Application/rules:Annotation/rules:Description", manager).Attributes["Text"].Value = "Reg2CI (c) 2017 by Roger Zander";
            }
        }

        internal void CreatePSXMLAll(string SettingName, string Description)
        {
            XmlNamespaceManager manager = new XmlNamespaceManager(xDoc.NameTable);
            string sDC = "http://schemas.microsoft.com/SystemsCenterConfigurationManager/2009/07/10/DesiredConfiguration";
            string sRules = "http://schemas.microsoft.com/SystemsCenterConfigurationManager/2009/06/14/Rules";
            string AuthoringScopeId = "ScopeId_709B4C9D-07C0-476D-9CF4-2E760FA01892";
            manager.AddNamespace("dc", sDC);
            manager.AddNamespace("rules", sRules);

            string PSLogicalName = "ScriptSetting_" + Guid.NewGuid().ToString();

            XmlNode xSimpleSetting = xDoc.CreateNode(XmlNodeType.Element, "SimpleSetting", sDC);

            var xLogicalname = xDoc.CreateAttribute("LogicalName");
            xLogicalname.Value = PSLogicalName;
            xSimpleSetting.Attributes.Append(xLogicalname);

            //Add DataType Attribute
            var xDataType = xDoc.CreateAttribute("DataType");
            xDataType.Value = "Boolean"; //PS Script return Boolean
            xSimpleSetting.Attributes.Append(xDataType);

            //Load default Structure
            xSimpleSetting.InnerXml = Properties.Settings.Default.PSScript.Replace("{DISPLAYNAME}", SettingName).Replace("{DESC}", Description).Replace("{RESOURCEID}", "ID-" + Guid.NewGuid().ToString());
            xSimpleSetting.InnerXml = xSimpleSetting.InnerXml.Replace("{PSDISC}", GetPSCheckAll()).Replace("{PSREM}", GetPSRemediateAll()).Replace("{X64}", x64.ToString().ToLower());

            XmlNode RCS = xDoc.SelectSingleNode("//dc:DesiredConfigurationDigest/dc:Application/dc:Settings/dc:RootComplexSetting", manager);
            RCS.AppendChild(xSimpleSetting);

            //Add Rule
            XmlNode xRule = xDoc.CreateNode(XmlNodeType.Element, "Rule", sRules);

            var xRuleid = xDoc.CreateAttribute("id");
            xRuleid.Value = "Rule_" + Guid.NewGuid().ToString();
            xRule.Attributes.Append(xRuleid);

            var xSeverity = xDoc.CreateAttribute("Severity");
            xSeverity.Value = "Warning";
            xRule.Attributes.Append(xSeverity);


            var xNonComp = xDoc.CreateAttribute("NonCompliantWhenSettingIsNotFound");
            xNonComp.Value = "true";
            xRule.Attributes.Append(xNonComp);


            xRule.InnerXml = Properties.Settings.Default.RulePSBool.Replace("{DISPLAYNAME}", SettingName).Replace("{RESOURCEID}", "ID-" + Guid.NewGuid().ToString());
            xRule.InnerXml = xRule.InnerXml.Replace("{AUTHORINGSCOPEID}", AuthoringScopeId).Replace("{LOGICALNAME}", LogicalName).Replace("{DATATYPE}", "Boolean");
            xRule.InnerXml = xRule.InnerXml.Replace("{SETTINGLOGICALNAME}", PSLogicalName);

            XmlNode Rules = xDoc.SelectSingleNode("//dc:DesiredConfigurationDigest/dc:Application/dc:Rules", manager);
            Rules.AppendChild(xRule);
        }

        internal void CreatePSXMLAll(string SettingName, string Description, string PSHive)
        {
            XmlNamespaceManager manager = new XmlNamespaceManager(xDoc.NameTable);
            string sDC = "http://schemas.microsoft.com/SystemsCenterConfigurationManager/2009/07/10/DesiredConfiguration";
            string sRules = "http://schemas.microsoft.com/SystemsCenterConfigurationManager/2009/06/14/Rules";
            string AuthoringScopeId = "ScopeId_709B4C9D-07C0-476D-9CF4-2E760FA01892";
            manager.AddNamespace("dc", sDC);
            manager.AddNamespace("rules", sRules);

            string PSLogicalName = "ScriptSetting_" + Guid.NewGuid().ToString();

            XmlNode xSimpleSetting = xDoc.CreateNode(XmlNodeType.Element, "SimpleSetting", sDC);

            var xLogicalname = xDoc.CreateAttribute("LogicalName");
            xLogicalname.Value = PSLogicalName;
            xSimpleSetting.Attributes.Append(xLogicalname);

            //Add DataType Attribute
            var xDataType = xDoc.CreateAttribute("DataType");
            xDataType.Value = "Boolean"; //PS Script return Boolean
            xSimpleSetting.Attributes.Append(xDataType);

            //Load default Structure
            xSimpleSetting.InnerXml = Properties.Settings.Default.PSScript.Replace("{DISPLAYNAME}", SettingName).Replace("{DESC}", Description).Replace("{RESOURCEID}", "ID-" + Guid.NewGuid().ToString());

            string sXML = xSimpleSetting.InnerXml.Replace("{PSDISC}", GetPSCheckAll(PSHive)).Replace("{PSREM}", GetPSRemediateAll(PSHive)).Replace("{X64}", x64.ToString().ToLower());
            xSimpleSetting.InnerXml = sXML;
            if(PSHive == "HKCU")
            {
                xSimpleSetting.InnerXml = xSimpleSetting.InnerXml.Replace("ScriptDiscoverySource Is64Bit=\"" + x64.ToString().ToLower() + "\"", "ScriptDiscoverySource Is64Bit=\"" + x64.ToString().ToLower() + "\" IsPerUser=\"true\"");
            }

            XmlNode RCS = xDoc.SelectSingleNode("//dc:DesiredConfigurationDigest/dc:Application/dc:Settings/dc:RootComplexSetting", manager);
            RCS.AppendChild(xSimpleSetting);

            //Add Rule
            XmlNode xRule = xDoc.CreateNode(XmlNodeType.Element, "Rule", sRules);

            var xRuleid = xDoc.CreateAttribute("id");
            xRuleid.Value = "Rule_" + Guid.NewGuid().ToString();
            xRule.Attributes.Append(xRuleid);

            var xSeverity = xDoc.CreateAttribute("Severity");
            xSeverity.Value = "Warning";
            xRule.Attributes.Append(xSeverity);


            var xNonComp = xDoc.CreateAttribute("NonCompliantWhenSettingIsNotFound");
            xNonComp.Value = "true";
            xRule.Attributes.Append(xNonComp);


            xRule.InnerXml = Properties.Settings.Default.RulePSBool.Replace("{DISPLAYNAME}", SettingName).Replace("{RESOURCEID}", "ID-" + Guid.NewGuid().ToString());
            xRule.InnerXml = xRule.InnerXml.Replace("{AUTHORINGSCOPEID}", AuthoringScopeId).Replace("{LOGICALNAME}", LogicalName).Replace("{DATATYPE}", "Boolean");
            xRule.InnerXml = xRule.InnerXml.Replace("{SETTINGLOGICALNAME}", PSLogicalName);

            XmlNode Rules = xDoc.SelectSingleNode("//dc:DesiredConfigurationDigest/dc:Application/dc:Rules", manager);
            Rules.AppendChild(xRule);
        }
    }

    public enum KeyAction
    {
        Add, Remove
    }

    public enum ValueType
    {
        String, DWord, QWord
    }
}
