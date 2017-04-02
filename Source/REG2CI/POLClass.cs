using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace REG2CI
{
    public class POL
    {
        public POL(string Filename, bool MachinePolicy = true)
        {
            string sFile = File.ReadAllText(Filename, Encoding.Unicode);
            string sResult = sFile.Substring(4);

            List<POLPolicy> lResult = new List<POLPolicy>();

            foreach (string sLine in sResult.Split(new string[] { "][" }, StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    string sKey = sLine.Split(';')[0].Replace("[", "").Replace("]", "").Trim('\0');
                    if (MachinePolicy)
                        sKey = "[HKEY_LOCAL_MACHINE\\" + sKey;
                    else
                        sKey = "[HKEY_CURRENT_USER\\" + sKey;

                    string sValue = sLine.Split(';')[1].Trim('\0').Trim();
                    string sType = "REG_NONE";

                    switch (BitConverter.ToInt32(Encoding.Unicode.GetBytes(sLine.Split(';')[2].Trim()), 0))
                    {
                        case 0:
                            sType = "REG_NONE";
                            break;
                        case 1:
                            sType = "REG_SZ";
                            break;
                        case 2:
                            sType = "REG_EXPAND_SZ";
                            break;
                        case 3:
                            sType = "REG_BINARY";
                            break;
                        case 4:
                            sType = "REG_DWORD";
                            break;
                        case 7:
                            sType = "REG_MULTI_SZ";
                            break;
                        case 11:
                            sType = "REG_QWORD";
                            break;

                    }

                    Int32 iSize = BitConverter.ToInt32(Encoding.Unicode.GetBytes(sLine.Split(';')[3]), 0);

                    string sData = "";
                    if (sType == "REG_DWORD")
                        sData = BitConverter.ToInt32(Encoding.Unicode.GetBytes(sLine.Split(';')[4].Trim()), 0).ToString();
                    if (sType == "REG_QWORD")
                        sData = BitConverter.ToInt64(Encoding.Unicode.GetBytes(sLine.Split(';')[4].Trim()), 0).ToString();
                    if (sType == "REG_SZ")
                        sData = sLine.Split(';')[4].ToString().TrimEnd(']').Trim('\0').Trim();
                    if (sType == "REG_EXPAND_SZ")
                        sData = sLine.Split(';')[4].ToString().TrimEnd(']').Trim('\0').Trim();


                    lResult.Add(new POLPolicy() { Key = sKey, Value = sValue, Type = sType, Size = iSize.ToString(), Data = sData });

                }
                catch { }
            }

            Policies = lResult;
        }

        public List<POLPolicy> Policies;
    }

    public class POLPolicy
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public string Type { get; set; }
        public string Size { get; set; }
        public string Data { get; set; }
    }
}
