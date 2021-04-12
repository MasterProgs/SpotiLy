using System;
using System.Collections.Generic;
using System.Text;

namespace SpotiLy.Commands
{
    public static class CommandParser
    {
        private const string PREFIX = "[SL] : ";
        private const string PREFIX_ERROR = "[Error] : ";
        private const string PREFIX_WARN = "[Warn] : ";

        public static void Say(string text, ConsoleColor color = ConsoleColor.White, bool newLine = false)
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Write(PREFIX);
            Console.ForegroundColor = color;
            Console.Write(text + (newLine ? "\n" : ""));
            Console.ForegroundColor = ConsoleColor.White;
        }
        
        public static void SayLn(string text, ConsoleColor color = ConsoleColor.White)
        {
            Say(text, color, true);
        }

        private static void Ask(string question)
        {
            Say(question, ConsoleColor.White, true);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" > ");
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void Error(string error)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.Write(PREFIX_ERROR);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(error);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void Warn(string warn)
        {
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.Write(PREFIX_WARN);
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(warn);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static T GetInput<T>(string prompt)
        {
            Ask(prompt);
            string input = Console.ReadLine();
            try 
            {
                if (typeof(T).IsEnum)
                {
                    return (T)Enum.Parse(typeof(T), input);
                }
                else
                {
                    return (T)Convert.ChangeType(input, typeof(T));
                }
            } 
            catch(Exception e)
            {
                Error(e.Message);
            }
            return default;
        }
    }
}
