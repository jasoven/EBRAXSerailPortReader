using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace NDCSendMessage
{
    class Program
    {
        [DllImport(@"C:\Program Files\NCR APTRA\Advance NDC\ssdsEXUTIL.dll", CharSet = CharSet.Ansi)]
        public static extern int SendStatus(string msg, bool bSolicited, bool bViaInterceptors);


        static void Main(string[] args)
        {
            String NDCMessage = string.Empty;
            String[] eargs = {
                                 string.Empty, 
                                 string.Empty,
                                 string.Empty,
                                 string.Empty,
                                 string.Empty,
                                 string.Empty,
                                 string.Empty
                             };
            Char FS = (char)0x1c;
            int result = -1;

            int earg = -1;
            foreach (string item in args)
            {
                if (item.ToLower().IndexOf("-e") == 0 && item.Length == 3)
                {
                    try
                    {
                        earg = int.Parse(item.Substring(2, 1));
                        if (!(earg > -1 & earg < 7))
                            earg = -1;
                    }
                    catch { }
                }


                if ((item.ToLower().IndexOf("-e") != 0) && earg > -1)
                {
                    eargs[earg] += " " + item;
                    eargs[earg] = eargs[earg].Trim();
                }
            }

            if (!(eargs[1] == string.Empty ||
                  eargs[2] == string.Empty ||
                  (eargs[5] == string.Empty & eargs[6] != string.Empty) ||
                  eargs[1].Length > 1 ||
                  eargs[2].Length > 154 ||
                  eargs[3].Length > 14 ||
                  eargs[5].Length > 8))
            {
                bool solicited;
                if (eargs[0].ToLower() == "true")
                    solicited = true;
                else
                    solicited = false;

                if (eargs[5] != string.Empty)
                    NDCMessage = FS + eargs[5] + eargs[6];

                if (eargs[4] != string.Empty || NDCMessage != string.Empty)
                    NDCMessage = FS + eargs[4] + NDCMessage;

                if (eargs[3] != string.Empty || NDCMessage != string.Empty)
                    NDCMessage = FS + eargs[3] + NDCMessage;

                NDCMessage = eargs[1].ToUpper() + eargs[2] + NDCMessage;

                try
                {
                    result = SendStatus(NDCMessage, solicited, false);
                    Console.WriteLine(result);
                }
                catch
                {
                    Console.WriteLine("4");
                }
            }
            else
            {
                Console.WriteLine("5");
            }
        }
    }
}
