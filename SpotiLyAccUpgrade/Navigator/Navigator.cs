using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Opera;
using OpenQA.Selenium.Support.UI;
using SpotiLy.Commands;
using SpotiLy.Driver;
using SpotiLy.SpotifyApi;
using SpotiLyAccUpgrade;
using SpotiLyAccUpgrade.SpotifyApi;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SpotiLy.Navigator
{
    public class Navigator
    {
        private const string SPOTIFY_LOGIN = "https://accounts.spotify.com/fr/login";

        private IWebDriver driver;
        private NavigatorEnum navigator;
        private readonly IEnumerable<string> listeners;
        public List<string> UpgradedListeners { get; private set; }

        public Dictionary<string, string> InvalidListeners { get; } = new Dictionary<string, string>();

        public int ActualListener { get; private set; }
        public Token Token { get; set; }
        public bool IsInvalidToken { get; internal set; }

        public Navigator(IEnumerable<string> listeners, NavigatorEnum driver, Token token)
        {
            this.listeners = listeners;
            this.navigator = driver;
            this.Token = token;
            this.UpgradedListeners = new List<string>();
        }

        private void InitializeDriver()
        {
            var driverPath = Directory.GetCurrentDirectory() + "/Drivers";
            switch (navigator)
            {
                case NavigatorEnum.Chrome:
                    ChromeOptions options = new ChromeOptions();
                    ChromeDriverService service = ChromeDriverService.CreateDefaultService(driverPath);

                    service.HideCommandPromptWindow = true;
                    options.AddArguments("--disable-extensions", "--disable-gpu", "--headless");
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        options.AddArgument("--no-sandbox");
                    }
                    this.driver = new ChromeDriver(service, options);
                    break;
                case NavigatorEnum.Firefox:
                    this.driver = new FirefoxDriver(driverPath);
                    break;
                case NavigatorEnum.Opera:
                    this.driver = new OperaDriver(driverPath);
                    break;
            }

            this.driver.Manage().Timeouts().PageLoad = new TimeSpan(0, 0, 10);
            this.driver.Manage().Window.Size = new Size(20, 20);
            this.driver.Manage().Window.Position = new Point(0, 0);
        }

        private string GetUrl()
        {
            return $"https://open.spotify.com/get_access_token?reason=transport&productType=web_player";
        }

        public Task LaunchInstance() //-> FIFO implementation (Linked)
        {
            if (ActualListener == listeners.Count()) // -> All listners has been upgraded
            {
                CommandParser.SayLn($"All actions of thread done!", ConsoleColor.Green);
                this.driver.Close();
                return Task.CompletedTask;
            }

            InitializeDriver();
            try
            {
                driver.Navigate().GoToUrl($"{SPOTIFY_LOGIN}?continue={GetUrl()}");
                string[] credentials = listeners.ElementAt(ActualListener).Split(':');
                return Login(credentials[0], credentials[1]);
            }
            catch
            {
                return Reset(false);
            }
        }

        private Task Login(string email, string password)
        {
            driver.FindElement(By.Name("username")).SendKeys(email);
            driver.FindElement(By.Name("password")).SendKeys(password + Keys.Enter);
            
            Thread.Sleep(2000);

            string sp_dc;
            try
            {
                var cookie = driver.Manage().Cookies.GetCookieNamed("sp_dc");
                if (cookie != null)
                {
                    sp_dc = cookie.Value;
                } 
                else
                {
                    CommandParser.SayLn($"{email} wrong credentials", ConsoleColor.Red);
                    InvalidListeners.Add(email, "wrong password|email");
                    return Reset();
                }
            }
            catch
            {
                return Login(email, password);
            }

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("cookie", $"sp_dc={sp_dc}");

            var result = client.PostAsync("https://www.spotify.com/us/home-hub/api/v1/family/address/autocomplete/", new StringContent("{\"address\":\"" + Token.FullAdresse + "\",\"country\":\"GB\",\"sessionToken\":\"50765267-e0b8-4b9c-bfa9-4718c30d8a2e\"}", Encoding.UTF8, "application/json")).Result;
            if (result.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var home = JsonSerializer.Deserialize<HomeHub>(result.Content.ReadAsStringAsync().Result);
                if (home.addresses.Count > 0)
                {
                    var placeId = home.addresses.First().placeId;
                    result = client.PostAsync("https://www.spotify.com/us/home-hub/api/v1/family/member/", new StringContent("{\"address\":\"" + Token.FullAdresse + "\",\"inviteToken\":\"" + Token.InviteCode + "\",\"placeId\":\"" + placeId + "\"}", Encoding.UTF8, "application/json")).Result;
                    if (result.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var family = JsonSerializer.Deserialize<Family>(result.Content.ReadAsStringAsync().Result);
                        CommandParser.SayLn($"Successfully ugprade for {email} with {Token.InviteCode} token | places ({family.members.Count}/6)", ConsoleColor.Yellow);

                        UpgradedListeners.Add($"{email}:{password}");

                        if (family.members.Count != 6)
                        {
                            return Reset();
                        }
                    }
                    else
                    {
                        if (result.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
                        {
                            var detail = JsonSerializer.Deserialize<dynamic>(result.Content.ReadAsStringAsync().Result);
                            CommandParser.SayLn($"{Token.InviteCode} error on join : {detail}", ConsoleColor.Red);
                            IsInvalidToken = true;
                        }
                    }
                }
                else
                {
                    CommandParser.SayLn($"{Token.InviteCode} has no address", ConsoleColor.Red);
                }
            }
            else
            {
                if(result.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
                {
                    CommandParser.SayLn($"{Token.InviteCode} is full", ConsoleColor.Red);
                }
            }

            CommandParser.SayLn($"All actions of thread done!", ConsoleColor.Green);
            this.driver.Close();

            return Task.CompletedTask;
        }

        private async Task NewInstance(bool isNewListener = true)
        {
            driver.Quit();

            if (isNewListener)
            {
                ActualListener++;
            }

            await LaunchInstance();
        }

        private async Task Reset(bool isNew = true)
        {
            CommandParser.SayLn($"New instance...", ConsoleColor.Gray);
            await NewInstance(isNew);
        }
    }
}