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

        //static List<ConsoleColor> clientColorsList = new List<ConsoleColor>();
        //static Random rand = new Random();
        //static ConsoleColor color;

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
            Console.WriteLine($"\n{spacingMsg + blockName + spacingMsg}\n");
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
            Console.WriteLine($"\n{spacingMsg + blockName + spacingMsg}\n");
        }

        public static void PrintInValue(string message, string inputFormat)
        {
            Console.WriteLine($"IN {inputFormat} :{message}:");
        }
        public static void PrintOutValue(string message)
        {
            Console.WriteLine($"OUT :{message}:");
        }

        public static void PrintCoords(int[] input, int index)
        {
            Console.Write($"Coordinaties{index}: {input[0]} {input[1]}\n");
        }


        //public static ConsoleColor ChooseRandomColor()
        //{
        //    Array array = Enum.GetValues(typeof(ConsoleColor));
        //    ConsoleColor randomColor = ConsoleColor.White;
        //    if (array != null)
        //    {
        //        randomColor = (ConsoleColor)array.GetValue(rand.Next(array.Length));
        //    }

        //    return randomColor;
        //}
    }
}
