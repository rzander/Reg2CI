using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System.Diagnostics;

namespace REG2CI
{
    class Program
    {
        static int Main(string[] args)
        {
            bool bX64 = true;
            List<string> lArgs = args.ToList();
            Console.WriteLine("****** Reg2CI (c) 2017 by Roger Zander ******");
            if(lArgs.Count == 0 || lArgs.Contains("-?") || lArgs.Contains("/?"))
            {
                Console.WriteLine("Usage: Reg2CI.exe <Reg File> <Cab File> <Name of the CI> [/X86]");
                Console.WriteLine("Usage: Reg2CI.exe <POL File> <Cab File> <Name of the CI> [/X86] [/USER]");
                return 1;
            }
            if (lArgs.Contains("/X86"))
                bX64 = false;
            if (lArgs.Count >= 2)
            {
                string sRegFile = lArgs.FirstOrDefault(t => t.Contains(".reg")) ?? ""; //lArgs[0]; 
                string sPOLFile = lArgs.FirstOrDefault(t => t.Contains(".pol")) ?? ""; //lArgs[0];
                string sCabFile = lArgs.FirstOrDefault(t => t.Contains(".cab")); //lArgs[1];
                string sName = "TEST CI";
                if (lArgs.Count >= 3)
                {
                    sName = lArgs[2];
                }

                RegFile RegFile;
                if (string.IsNullOrEmpty(sPOLFile))
                    RegFile = new RegFile(sRegFile, bX64, sName);
                if (string.IsNullOrEmpty(sRegFile))
                {
                    if (lArgs.Count(t => t.ToLower() == "/user") == 1)
                    {
                        RegFile = new RegFile(sPOLFile, bX64, sName, false);
                    }
                    else
                    {
                        RegFile = new RegFile(sPOLFile, bX64, sName, true);
                    }
                }


                string sXMLPath = Path.Combine(Path.GetTempPath(), System.IO.Path.GetRandomFileName().Split('.')[0] + ".xml");
                RegFile.xDoc.Save(sXMLPath);
                Process oProc =  Process.Start("makecab.exe", '\"' + sXMLPath + '\"' + " " + '\"' + sCabFile + '\"');
                oProc.WaitForExit(5000);
                //Console.WriteLine("makecab.exe " + sXMLPath + " " + sCabFile);
                File.Delete(sXMLPath);
                return 0;
            }

            return 2;
        }
    }
}
