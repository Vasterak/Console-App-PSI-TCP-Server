using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCPServerV2
{
    public static class Debug
    {
        public static void PrintAsChars(string message)
        {
            Console.WriteLine("---RAW BYTES---");
            foreach (char item in message)
            {
                Console.Write($"{item}-");
            }
            Console.WriteLine("\n---RAW BYTES---\n");
        }
        public static void PrintAsChars(string message, string blockName)
        {
            Console.WriteLine($"---{blockName}---");
            foreach (char item in message)
            {
                Console.Write($"{item}-");
            }
            Console.WriteLine($"\n---{blockName}---\n");
        }
        public static void PrintAsAscii(string message)
        {
            Console.WriteLine("---RAW BYTES---");
            foreach (char item in message)
            {
                Console.Write($"{(int)item}-");
            }
            Console.WriteLine("\n---RAW BYTES---\n");
        }
        public static void PrintAsAscii(string message, string blockName)
        {
            Console.WriteLine($"---{blockName}---");
            foreach (char item in message)
            {
                Console.Write($"{(int)item}-");
            }
            Console.WriteLine($"\n---{blockName}---\n");
        }
    }
}
