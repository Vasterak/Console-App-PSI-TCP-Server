using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCPServerV2
{
    public static class Debug
    {
        const string headerMsg = "-----RAW BYTES-----";
        const string spacingMsg = "-----";

        public static void PrintAsChars(string message)
        {
            Console.WriteLine(headerMsg);
            foreach (char item in message)
            {
                Console.Write($"{item}-");
            }
            Console.WriteLine($"\n{headerMsg}\n");
        }

        public static void PrintAsChars(string message, string blockName)
        {
            Console.WriteLine(spacingMsg + blockName + spacingMsg);
            foreach (char item in message)
            {
                Console.Write($"{item}-");
            }
            Console.WriteLine($"\n{spacingMsg + blockName + spacingMsg}-----\n");
        }

        public static void PrintAsAscii(string message)
        {
            Console.WriteLine(headerMsg);
            foreach (char item in message)
            {
                Console.Write($"{(int)item}-");
            }
            Console.WriteLine($"\n{headerMsg}\n");
        }

        public static void PrintAsAscii(string message, string blockName)
        {
            Console.WriteLine(spacingMsg + blockName + spacingMsg);
            foreach (char item in message)
            {
                Console.Write($"{(int)item}-");
            }
            Console.WriteLine($"\n{spacingMsg + blockName + spacingMsg}-----\n");
        }
    }
}
