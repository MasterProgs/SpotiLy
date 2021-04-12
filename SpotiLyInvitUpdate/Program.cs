using SpotiLy.Commands;
using SpotiLy.Driver;
using SpotiLy.Navigator;
using SpotiLyAccUpgrade;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SpotiLyAccGen
{
    class Program
    {
        public static readonly string[] LOGO = new string[] {
            "   _____             __  _ __              \n" +
            "  / ___/____  ____  / /_(_) /   __  __     \n" +
            "  \\__ \\/ __ \\/ __ \\/ __/ / /   / / / / \n" +
            " ___/ / /_/ / /_/ / /_/ / /___/ /_/ /      \n" +
            "/____/ .___/\\____/\\__/_/_____/\\__, /    \n" +
            "    /_/                      /____/        \n\n"
         };
        static async Task Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            for (var i = 0; i < Program.LOGO.Length; i++)
            {
                Console.Write(Program.LOGO[i]);
            }
            Console.ForegroundColor = ConsoleColor.White;

            string ownerPath = "";
            int threadsCount;
            do
            {
                ownerPath = Directory.GetCurrentDirectory() + '/' + CommandParser.GetInput<string>("Owners upgrade file fime ? [email:pwd]");
                threadsCount = CommandParser.GetInput<int>("How many threads you can handle ? [default=1]");
                if (threadsCount == 0)
                {
                    threadsCount = 1;
                }
            } while (!File.Exists(ownerPath));

            var lines = File.ReadAllLines(ownerPath);
            for (int i = 0; i < threadsCount; i++)
            {
                new Thread(() => new SpotiLy.Navigator.Navigator(lines, NavigatorEnum.Chrome).LaunchInstance()).Start();
            }

            Console.Read();
        }
    }
}