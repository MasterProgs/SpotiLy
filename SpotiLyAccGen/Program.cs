using SpotiLy.Commands;
using SpotiLy.Manager;
using System;
using System.IO;
using System.Linq;
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

        static async Task Main()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            for (var i = 0; i < LOGO.Length; i++)
            {
                Console.Write(LOGO[i]);
            }
            Console.ForegroundColor = ConsoleColor.White;

            string proxyPath;
            int accCount;
            do
            {
                proxyPath = Directory.GetCurrentDirectory() + '/' + CommandParser.GetInput<string>("Proxies file fime ? [host:ip:user:password]");
                accCount = CommandParser.GetInput<int>("How many accounts do you want generate ? [max:5000]");
                if (accCount > 5000) { accCount = 5000; }
            } while (!File.Exists(proxyPath));


            var accounts = AccountGenerator.GenerateAccountAsync(proxyPath, accCount);

            File.WriteAllText(Directory.GetCurrentDirectory() + $"/accounts # {DateTime.Now.ToString("MM-dd-yy # H-mm-ss")}", string.Join('\n', accounts.Select(x => x.Email + ':' + x.Password)));
            Console.Read();
        }
    }
}