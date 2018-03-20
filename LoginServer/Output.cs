using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace LoginServer
{
    static class Output
    {
        public enum OutType
        {
            Console,
            Window,
            Stream
        }
        //for avoid console freezes antil key inout receive
        [DllImport("kernel32.dll")]
        public static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);
        private const uint ENABLE_EXTENDED_FLAGS = 0x0080;

        private static OutType outType = OutType.Console;

        public static void SetOut(OutType type)
        {
            outType = type;
            if (type == OutType.Console)
            {
                IntPtr handle = Process.GetCurrentProcess().MainWindowHandle;
                SetConsoleMode(handle, ENABLE_EXTENDED_FLAGS);
            }
        }

        public static void Write(ConsoleColor color, string text)
        {
            switch (outType)
            {
                case OutType.Console:
                    Console.ForegroundColor = color;
                    Console.Write(text);
                    break;
                case OutType.Window:
                    break;
                case OutType.Stream:
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write(text);
                    break;
            }
        }

        public static void Write(string text)
        {
            Write(ConsoleColor.White, text);
        }

        public static void WriteLine(ConsoleColor color, string text)
        {
            switch (outType)
            {
                case OutType.Console:
                    Console.ForegroundColor = color;
                    Console.WriteLine(text);
                    break;
                case OutType.Window:
                    break;
                case OutType.Stream:
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(text);
                    break;
            }
        }

        public static void WriteLine(string text)
        {
            WriteLine(ConsoleColor.White, text);
        }


        private static void ResetColor()
        {
            switch (outType)
            {
                case OutType.Console:
                    Console.ResetColor();
                    break;
                case OutType.Window:
                    break;
                case OutType.Stream:
                    break;
                default:
                    Console.ResetColor();
                    break;
            }
        }

        public static void WaitForKeyPress()
        {
            switch (outType)
            {
                case OutType.Console:
                    Console.ReadKey();
                    break;
                case OutType.Window:
                    Thread.Sleep(4000);
                    break;
                case OutType.Stream:
                    Thread.Sleep(4000);
                    break;
                default:
                    Console.ReadKey();
                    break;
            }
        }

        public static string ReadLine()
        {
            string input = "";
            switch (outType)
            {
                case OutType.Console:
                    input = Console.ReadLine();
                    break;
                case OutType.Window:
                    break;
                case OutType.Stream:
                    break;
                default:
                    input = Console.ReadLine();
                    break;
            }
            return input;
        }

        public static void Clear()
        {
            switch (outType)
            {
                case OutType.Console:
                    Console.Clear();
                    break;
                case OutType.Window:
                    break;
                case OutType.Stream:
                    break;
                default:
                    Console.Clear();
                    break;
            }
        }

    }
}
