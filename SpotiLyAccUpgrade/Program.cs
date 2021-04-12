using Microsoft.VisualBasic;
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

            string tokenPath, listenersPath = "";
            do
            {
                tokenPath = Directory.GetCurrentDirectory() + '/' + CommandParser.GetInput<string>("Token file fime ? [token:adresse]");
                listenersPath = Directory.GetCurrentDirectory() + '/' + CommandParser.GetInput<string>("Account file fime ? [email:pwd]");
            } while (!File.Exists(tokenPath) || !File.Exists(listenersPath));

            var lines = File.ReadAllLines(tokenPath);
            var listeners = File.ReadAllLines(listenersPath);

            List<Token> tokensAvailable = new List<Token>();
            List<Task> threads = new List<Task>();

            List<string> upgradedAccounts = new List<string>();
            List<string> invalidTokens = new List<string>();

            var newListeners = new List<string>();
            newListeners.AddRange(listeners);

            foreach (var line in lines)
            {
                var data = line.Split(":");
                var token = new Token
                {
                    InviteCode = data[0],
                    FullAdresse = data[1]
                };
                threads.Add(Task.Run(() => CheckToken(tokensAvailable, token)));
            }

            await Task.WhenAll(threads);
            CommandParser.SayLn($"{tokensAvailable.Count} tokens available", ConsoleColor.Yellow);

            for (int i = 0; i < tokensAvailable.Count; i++)
            {
                var navigator = new SpotiLy.Navigator.Navigator(newListeners, NavigatorEnum.Chrome, tokensAvailable[i]);
                Task.WaitAny(navigator.LaunchInstance());
                newListeners.RemoveAll(x => navigator.UpgradedListeners.Contains(x) || navigator.InvalidListeners.Keys.Contains(x));
                upgradedAccounts.AddRange(navigator.UpgradedListeners);
                if (navigator.IsInvalidToken)
                {
                    invalidTokens.Add(tokensAvailable[i].ToString());
                }
            }

            File.WriteAllLines(listenersPath, newListeners);
            CommandParser.SayLn($"{upgradedAccounts.Count} accounts has been upgrade (upgraded.txt)", ConsoleColor.Yellow);
            File.WriteAllLines(Directory.GetCurrentDirectory() + "/upgrades.txt", upgradedAccounts);

            CommandParser.SayLn($"{invalidTokens.Count} tokens has been detected invalid (invalides.txt)", ConsoleColor.Yellow);
            File.WriteAllLines(Directory.GetCurrentDirectory() + "/invalides.txt", invalidTokens);

            Console.Read();
        }

        static Task CheckToken(List<Token> tokensAvailable, Token token)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.183 Safari/537.36");
            return Task.Run(async delegate
            {
                var result = await client.GetAsync("https://www.spotify.com/us/home-hub/api/v1/family/invite/" + token.InviteCode);
                if (result.StatusCode == HttpStatusCode.OK)
                {
                    CommandParser.SayLn($"{token.InviteCode} is available", ConsoleColor.Cyan);
                    tokensAvailable.Add(token);
                }
                else
                {
                    if (result.StatusCode == HttpStatusCode.UnprocessableEntity)
                    {
                        CommandParser.SayLn($"{token.InviteCode} is full", ConsoleColor.Red);
                    }
                }
            });
        }
    }
}