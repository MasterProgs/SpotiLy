using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Opera;
using SpotiLy.Commands;
using SpotiLy.Driver;
using SpotiLyAccUpgrade.SpotifyApi;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
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
        private IEnumerable<string> listeners;

        public int ActualListener { get; private set; }
        public string Owner { get; set; }

        public Navigator(IEnumerable<string> listeners, NavigatorEnum driver)
        {
            this.listeners = listeners;
            this.navigator = driver;
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
                    options.AddArguments("--disable-extensions", "--disable-gpu");
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
            this.driver.Manage().Window.Size = new Size(700, 700);
            this.driver.Manage().Window.Position = new Point(0, -1000);
        }

        private string GetUrl()
        {
            return $"https://open.spotify.com/get_access_token?reason=transport&productType=web_player";
        }

        public Task LaunchInstance() //-> FIFO implementation (Linked)
        {
            if (ActualListener == listeners.Count()) // -> All listners has been upgraded
            {
                CommandParser.SayLn($"All done", ConsoleColor.Green);
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
                    //CommandParser.SayLn($"{email} wrong credentials", ConsoleColor.Red);
                    return Reset(true);
                }
            }
            catch
            {
                return Login(email, password);
            }

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("cookie", $"sp_dc={sp_dc}");

            var result = client.GetAsync("https://www.spotify.com/fr/home-hub/api/v1/family/home/").Result;
            if (result.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var family = JsonSerializer.Deserialize<Family>(result.Content.ReadAsStringAsync().Result);
                //CommandParser.SayLn($"{family.inviteToken}:{family.address}", ConsoleColor.Cyan);
                Console.WriteLine($"{family.inviteToken}:{family.address}");
            }
            else
            {
                if(result.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
                {
                    //CommandParser.SayLn($"{email} is not owner", ConsoleColor.Red);
                }
            }

            return Reset(true);
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
            await NewInstance(isNew);
        }
    }
}