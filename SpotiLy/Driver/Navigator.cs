using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using SpotiLy.Commands;
using SpotiLy.Config;
using SpotiLy.Manager;
using SpotiLy.SpotifyApi;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace SpotiLy.Driver
{
    public abstract class Navigator
    {
        internal const string SPOTIFY_LISTEN = "https://open.spotify.com";
        internal const string SPOTIFY_LOGIN = "https://accounts.spotify.com/fr/login";
        private const string SPOTIFY_TOKEN = "https://open.spotify.com/get_access_token";
        private const string SPOTIFY_DEVICES = "https://api.spotify.com/v1/me/player/devices";
        private const string PLAYLIST_CREATE = "https://spclient.wg.spotify.com/playlist/v2/playlist/";
        private const string PLAYLIST_API = "https://api.spotify.com/v1/playlists/";

        internal readonly Configuration config;
        internal List<Album> albums;
        internal Album playlist;

        internal IWebDriver driver;

        internal Account listener;
        internal AccessToken token;
        internal Proxy proxy;

        internal HttpClient client;
        private Device device;

        internal int PlayAttempt { get; set; }
        internal int PlayMax { get; set; }

        public Navigator(Configuration config, List<Album> albums, Proxy proxy)
        {
            this.config = config;
            this.albums = albums;
            this.proxy = proxy;
        }

        internal void InitializeDriver()
        {
            var driverPath = Directory.GetCurrentDirectory() + "/Drivers";
            switch (config.Driver)
            {
                case NavigatorEnum.Chrome:
                    ChromeOptions options = new ChromeOptions();

                    options.SetLoggingPreference(LogType.Browser, LogLevel.Off);
                    options.SetLoggingPreference(LogType.Driver, LogLevel.Off);
                    options.SetLoggingPreference(LogType.Client, LogLevel.Off);
                    options.SetLoggingPreference(LogType.Profiler, LogLevel.Off);
                    options.SetLoggingPreference(LogType.Server, LogLevel.Off);

                    options.AddUserProfilePreference("profile.managed_default_content_settings.images", 2);
                    //options.AddUserProfilePreference("profile.managed_default_content_settings.javascript", 2);
                    options.AddUserProfilePreference("profile.managed_default_content_settings.media_stream", 2);
                    options.AddUserProfilePreference("profile.managed_default_content_settings.stylesheets", 2);

                    ChromeDriverService service = ChromeDriverService.CreateDefaultService(driverPath);

                    service.HideCommandPromptWindow = true;
                    options.AddArguments("--disable-extensions", "--disable-gpu", "--log-level=3", "--disable-logging");
                    options.Proxy = this.proxy;

                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        options.AddArgument("--no-sandbox");
                    }
                    options.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.183 Safari/537.36");
                    driver = new ChromeDriver(service, options);
                    break;
            }

            driver.Manage().Timeouts().PageLoad = new TimeSpan(0, 0, 30);
            //driver.Manage().Window.Position = new Point(-3000, -3000);

            var proxy = new WebProxy
            {
                Address = new Uri($"http://{this.proxy.HttpProxy}"),
                BypassProxyOnLocal = false,
                UseDefaultCredentials = false,
            };

            var httpClientHandler = new HttpClientHandler
            {
                Proxy = proxy,
            };

            client = new HttpClient(httpClientHandler);
            client.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.183 Safari/537.36 OPR/72.0.3815.320");
        }

        internal abstract string GetUrlContinue();
        internal abstract void Start();
        internal abstract void GetNewAccount();

        public abstract void LaunchInstance(Account listener, bool isNew = false);

        internal void NewInstance(bool newListener = false)
        {
            //if (!newListener)
            //{
            //    StatManager.Instance.Attemps++;
            //}

            try
            {
                Close();
                LaunchInstance(listener, newListener);
            }
            catch
            {
                //Console.WriteLine(e.StackTrace);
            }
        }

        internal void Close()
        {
            driver.Quit();
        }

        internal void Login()
        {
            var loginButton = WaitUntilElementExists(By.Id("login-button"), 15);
            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;

            var @object = driver.FindElement(By.Id("login-username"));
            js.ExecuteScript($"arguments[0].value='{listener.Email}'", @object);
            js.ExecuteScript("arguments[0].dispatchEvent(new Event('input', { bubbles: true }))", @object);

            @object = driver.FindElement(By.Id("login-password"));
            js.ExecuteScript($"arguments[0].value='{listener.Password}'", @object);
            js.ExecuteScript("arguments[0].dispatchEvent(new Event('input', { bubbles: true }))", @object);

            loginButton.Click();

            var clientInfo = WaitUntilElementExists(By.Id("config"), 30);
            token = JsonSerializer.Deserialize<AccessToken>(clientInfo.GetAttribute("innerHTML"));

            // -> Prepare
            PrepareSongs();

            // -> Get by webplayer
            GetDevice();

            // -> Start
            Play();
        }

        private SpotifyCommandEnum SendCommand(string cmd)
        {
            var result = client.PostAsync($"https://gew-spclient.spotify.com/connect-state/v1/player/command/from/" + device.id + "/to/" + device.id, new StringContent(cmd, Encoding.UTF8, "application/json")).Result;
            if (result.StatusCode != HttpStatusCode.OK)
            {
                var error = JsonSerializer.Deserialize<CommandErrorResult>(result.Content.ReadAsStringAsync().Result);
                return Enum.Parse<SpotifyCommandEnum>(error.error_type);
            }
            else
            {
                return SpotifyCommandEnum.OK;
            }
        }

        internal abstract void PrepareSongs();

        internal void GetDevice()
        {
            try
            {
                WaitUntilElementExists(By.XPath("//button[@data-testid='play-button']"), 15);
            }
            catch
            {
                NewInstance();
            }

            client.DefaultRequestHeaders.Add("authorization", $"Bearer {token.accessToken}");
            var result = client.GetAsync(SPOTIFY_DEVICES).Result;
            if (result.StatusCode == HttpStatusCode.OK)
            {
                device = JsonSerializer.Deserialize<DevicesList>(result.Content.ReadAsStringAsync().Result).devices.First();
            }
            else
            {
                Console.WriteLine("SPOTIFY_DEVICES\n" + result.Content.ReadAsStringAsync().Result);
            }
        }

        internal void Play()
        {
            // -> Play
            SpotifyCommandEnum result;
            if ((result = SendCommand("{\"command\":{\"context\":{\"uri\":\"" + playlist.uri + "\",\"url\":\"context://" + playlist.uri + "\",\"metadata\":{}},\"play_origin\":{\"feature_identifier\":\"harmony\",\"feature_version\":\"4.11.0-af0ef98\"},\"options\":{\"license\":\"on-demand\",\"skip_to\":{},\"player_options_override\":{\"repeating_track\":false,\"repeating_context\":true}},\"endpoint\":\"play\"}}")) == SpotifyCommandEnum.OK)
            {
                Thread.Sleep(5000);
                if (SendCommand("{\"command\":{\"value\":true,\"endpoint\":\"set_shuffling_context\"}}") == SpotifyCommandEnum.OK)
                {
                    // -> Go stream
                    Start();
                }
                else
                {
                    Console.WriteLine($"Suffle error");
                }
            }
            else
            {
                Console.WriteLine(result);
                if (PlayAttempt++ < 3)
                {
                    Thread.Sleep(3000);
                    if (result == SpotifyCommandEnum.DEVICE_NOT_FOUND)
                    {
                        GetDevice();
                    }
                    Play();
                }
                else
                {
                    PlayAttempt = 0;
                    driver.Navigate().Refresh();
                    Play();
                }
            }
        }

        internal void Skip(bool isPremium = false)
        {
            Random rdm = new Random();

            while (true)
            {
                try
                {
                    while (!isPremium && !WaitUntilElementExists(By.XPath("//button[@data-testid='control-button-skip-forward']")).Enabled)
                    {
                        Thread.Sleep(16);
                    }
                }
                catch
                {

                }

                Thread.Sleep(new TimeSpan(0, 0, rdm.Next(config.MinSkip, config.MaxSkip)));

                while (PlayAttempt++ < 3)
                {
                    try
                    {
                        TrySkip(isPremium);
                        break;
                    }
                    catch { }
                }

                if (PlayAttempt >= 3)
                {
                    break;
                }

                PlayAttempt = 0;
            }

            PlayAttempt = 0;
            StatManager.Instance.Attemps++;
            NewInstance();
        }

        private void TrySkip(bool isPremium = false)
        {
            if (!isPremium) // => Detection of ads
            {
                if (SendCommand("{\"command\": {\"endpoint\": \"skip_next\"}}") == SpotifyCommandEnum.OK)
                {
                    StatManager.Instance.FreeStreams++;
                    if (playlist.uri.Contains("playlist"))
                    {
                        StatManager.Instance.FreeStreamsInPlaylist++;
                    }
                }
                else
                {
                    new Exception("Skip error");
                }
            }
            else
            {
                StatManager.Instance.PremiumStreams++;
            }
        }

        internal void CreatePlaylist()
        {
            var proxy = new WebProxy
            {
                Address = new Uri($"http://{this.proxy.HttpProxy}"),
                BypassProxyOnLocal = false,
                UseDefaultCredentials = false,
            };

            var httpClientHandler = new HttpClientHandler
            {
                Proxy = proxy,
            };

            HttpClient client = new HttpClient(httpClientHandler);
            client.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.183 Safari/537.36 OPR/72.0.3815.320");
            client.DefaultRequestHeaders.Add("authorization", $"Bearer {token.accessToken}");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage result = client.PostAsync($"{PLAYLIST_CREATE}", new StringContent("{\"ops\":[{\"kind\":6,\"updateListAttributes\":{\"newAttributes\":{\"values\":{\"name\":\"\"}}}}]}", Encoding.UTF8, "application/json")).Result;
            if (result.StatusCode == HttpStatusCode.OK)
            {
                playlist = JsonSerializer.Deserialize<Album>(result.Content.ReadAsStringAsync().Result);
                string playlistId = playlist.uri.Split(":")[2];
                string uris = string.Join(',', albums.Select(x => string.Join(',', x.tracks.items.Select(y => '"' + y.uri + '"'))));
                result = client.PostAsync($"{PLAYLIST_API}{playlistId}/tracks", new StringContent("{\"uris\":[" + uris + "],\"position\":null}", Encoding.UTF8, "application/json")).Result;
                if (result.StatusCode == HttpStatusCode.Created)
                {
                    playlist = JsonSerializer.Deserialize<Album>(result.Content.ReadAsStringAsync().Result);
                    result = client.GetAsync($"{PLAYLIST_API}{playlistId}").Result;
                    if (result.StatusCode == HttpStatusCode.OK)
                    {
                        playlist = JsonSerializer.Deserialize<Album>(result.Content.ReadAsStringAsync().Result);
                    }
                }
            }
        }

        internal IWebElement WaitUntilElementExists(By elementLocator, int timeout = 10)
        {
            if (timeout > 0)
            {
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeout));
                return wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementExists(elementLocator));
            } 
            else
            {
                return driver.FindElement(elementLocator);
            }
        }

        internal OpenQA.Selenium.Cookie WaitUntilCookieExists(string cookieName, int timeout = 10)
        {
            if (timeout > 0)
            {
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeout));
                return wait.Until(x => x.Manage().Cookies.GetCookieNamed(cookieName));
            }
            else
            {
                return driver.Manage().Cookies.GetCookieNamed(cookieName);
            }
        }
    }
}