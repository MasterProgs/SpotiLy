using DSharpPlus;
using OpenQA.Selenium;
using SpotiLy.Commands;
using SpotiLy.Config;
using SpotiLy.Driver;
using SpotiLy.Manager;
using SpotiLy.SpotifyApi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SpotiLy
{
    class Program
    {
        public static readonly string[] LOGO = new string[] {
            "   _____             __  _ __              \n" +
            "  / ___/____  ____  / /_(_) /   __  __     \n" +
            "  \\__ \\/ __ \\/ __ \\/ __/ / /   / / / / \n" +
            " ___/ / /_/ / /_/ / /_/ / /___/ /_/ /      \n" +
            "/____/ .___/\\____/\\__/_/_____/\\__, /    \n" +
            "    /_/                      /____/        \n" +
            "by MaSTeR                             1.0\n\n"
         };

        static async Task Main()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            for (var i = 0; i < LOGO.Length; i++)
            {
                Console.Write(LOGO[i]);
            }
            Console.ForegroundColor = ConsoleColor.White;

            var configPath = Directory.GetCurrentDirectory() + "/config.json";
            Configuration config = null;
            if (File.Exists(configPath))
            {
                CommandParser.SayLn($"Config file exists", ConsoleColor.Yellow);
                config = JsonSerializer.Deserialize<Configuration>(File.ReadAllText(configPath));

                var answer = CommandParser.GetInput<string>("Do you want edit the config ? (y/N)");
                if (answer == "y")
                {
                    config = null;
                }
            }
            else
            {
                CommandParser.SayLn($"Config file not exists", ConsoleColor.Yellow);
            }

            HttpClient client = new HttpClient();

            if (config == null)
            {
                config = new Configuration();
                do
                {
                    config.AccountPath = Directory.GetCurrentDirectory() + '/' + CommandParser.GetInput<string>("Accounts file fime ?");
                    config.AlbumPath = Directory.GetCurrentDirectory() + '/' + CommandParser.GetInput<string>("Albums file fime ? [only ID per line] https://open.spotify.com/album/ID");
                    config.MinSkip = CommandParser.GetInput<int>("Minimum time before skipping (in secondes) ? [<= 30 is dangerous]");
                    config.MaxSkip = CommandParser.GetInput<int>($"Maximum time before skipping (in secondes) ? [< {config.MinSkip + 20} is not a good idea]");
                    config.Driver = CommandParser.GetInput<NavigatorEnum>("Driver name ? (Chrome, Firefox, Opera) [default=Chrome]");
                    config.Threads = CommandParser.GetInput<int>($"Maximum threads ?");
                    config.ProxyPath = Directory.GetCurrentDirectory() + '/' + CommandParser.GetInput<string>("Proxy file name for premium listeners <one IP per account> ? [host:password<?user:password?>]");
                    config.GeneratePlaylistRatio = CommandParser.GetInput<double>($"Generate playlist with all albums ratio ? [other=random album]");
                } while (!File.Exists(config.AccountPath) || !File.Exists(config.ProxyPath) || !File.Exists(config.AlbumPath) || config.MinSkip == 0 || config.MaxSkip == 0);

                JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                File.WriteAllText(configPath, JsonSerializer.Serialize(config, jsonSerializerOptions));
            }

            DiscordConfiguration discordConfiguration = new DiscordConfiguration()
            {
                AutoReconnect = true,
                LargeThreshold = 250,
                Token = "DISCORD_TOKEN_BOT",
                TokenType = TokenType.Bot,
                MessageCacheSize = 2048,
                LogTimestampFormat = "dd-MM-yyyy HH:mm:ss zzz"
            };

            await new Discord(config, discordConfiguration).Initialize();

            var listeners = File.ReadAllLines(config.AccountPath);
            var premiumProxies = File.ReadAllLines(config.ProxyPath);
            var albumsString = File.ReadAllLines(config.AlbumPath);

            if(premiumProxies.Length < listeners.Length)
            {
                CommandParser.Say($"({ premiumProxies.Length}) proxies detected for {listeners.Length} accounts.", ConsoleColor.Red);
                return;
            }

            List<Album> albums = new List<Album>();

            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.183 Safari/537.36");

            var result = await client.GetAsync("https://open.spotify.com/get_access_token?reason=transport&productType=web_player");
            AccessToken accessToken = JsonSerializer.Deserialize<AccessToken>(result.Content.ReadAsStringAsync().Result);
            client.DefaultRequestHeaders.Add("authorization", $"Bearer {accessToken.accessToken}");

            foreach(var albumId in albumsString)
            {
                result = await client.GetAsync("https://api.spotify.com/v1/albums/" + albumId);
                albums.Add(JsonSerializer.Deserialize<Album>(result.Content.ReadAsStringAsync().Result));
            }

            for (var i = 0; i < config.Threads; i++)
            {
                Navigator navigator = new NavigatorPremium(config, albums, new Proxy() { HttpProxy = premiumProxies[i], SslProxy = premiumProxies[i], });

                try
                {
                    new Thread(() => navigator.LaunchInstance(AccountManager.Instance.GetAccount(), true)).Start();
                    await Task.Delay(10000);
                }
                catch
                {
                    i--;
                }
            }

            Console.Clear();
            CommandParser.Say($"({ albums.Count}) ", ConsoleColor.Blue);
            Console.WriteLine("albums detected");
            CommandParser.Say($"({ config.Threads}) ", ConsoleColor.Yellow);
            Console.WriteLine("threads loaded");
            Console.ReadLine();
        }
    }
}