using OpenQA.Selenium;
using SpotiLy.Config;
using SpotiLy.Manager;
using SpotiLy.SpotifyApi;
using System;
using System.Collections.Generic;

namespace SpotiLy.Driver
{
    public class NavigatorPremium : Navigator
    {
        public NavigatorPremium(Configuration config, List<Album> playlist, Proxy proxy) : base(config, playlist, proxy)
        {

        }
        override internal string GetUrlContinue()
        {
            return $"{SPOTIFY_LISTEN}";
        }

        public override void LaunchInstance(Account listener, bool isNew = false)
        {
            if (isNew)
            {
                this.listener = listener;
            }

            try
            {
                InitializeDriver();
                driver.Navigate().GoToUrl($"{SPOTIFY_LOGIN}?continue={GetUrlContinue()}");
                try
                {
                    Login();
                }
                catch
                {
                    Console.WriteLine($"LoginPremium error");
                    if (PlayAttempt++ < 3)
                    {
                        //StatManager.Instance.Attemps++;
                        Login();
                    }
                    else
                    {
                        PlayAttempt = 0;
                        try
                        {
                            GetNewAccount();
                            NewInstance(true);
                        }
                        catch
                        {
                            Console.WriteLine($"NewAccountPremium error");
                            Close();
                        }
                    }
                }
            }
            catch
            {
                Console.WriteLine($"LaunchPremium error");
                NewInstance();
            }
        }

        internal override void PrepareSongs()
        {
            CreatePlaylist();
            driver.Navigate().GoToUrl($"{SPOTIFY_LISTEN}/playlist/{playlist.id}");
        }

        internal override void Start()
        {
            Skip(true);
        }

        internal override void GetNewAccount()
        {
           AccountManager.Instance.Disable(listener);
           listener = AccountManager.Instance.GetAccount();
        }
    }
}