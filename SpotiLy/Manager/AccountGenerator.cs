using SpotiLy.Api;
using SpotiLy.SpotifyApi;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SpotiLy.Manager
{
    public class AccountGenerator
    {
        public static ConcurrentStack<Account> GenerateAccountAsync(string proxyPath, int count = 1)
        {
            var proxies = File.ReadAllLines(proxyPath);
            var identities = File.ReadAllLines(Environment.CurrentDirectory + "/emails.txt");
            Random rdm = new Random();
            proxies = proxies[0].Split(':');
            ConcurrentStack<Account> accounts = new ConcurrentStack<Account>();

            List<Task> tasks = new List<Task>();

            while (count-- > 0)
            {
                var identity = identities[count];
                tasks.Add(Task.Run(async () =>
                {
                    var result = await CreateAccount(proxies, rdm, identity);
                    accounts.Push(result);
                }));
            }

            Task.WaitAll(tasks.ToArray());

            return accounts;
        }

        private static async Task<Account> CreateAccount(string[] proxyData, Random rdm, string ownerName)
        {
            var proxy = new WebProxy
            {
                Address = new Uri($"http://{proxyData[0]}:{proxyData[1]}"),
                BypassProxyOnLocal = false,
                UseDefaultCredentials = false,
            };

            if (proxyData.Length > 2)
            {
                proxy.Credentials = new NetworkCredential(userName: proxyData[2], password: proxyData[3]);
            }

            var httpClientHandler = new HttpClientHandler
            {
                Proxy = proxy,
            };

            HttpClient client = new HttpClient(httpClientHandler);
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.183 Safari/537.36");

            var queryStr = $"birth_day={rdm.Next(1, 25)}&birth_month=0{rdm.Next(1, 12)}&birth_year={rdm.Next(1990, 2002)}&collect_personal_info=undefined&creation_flow=&creation_point=spotify.com&displayname={ownerName}&email={ownerName}&gender={(rdm.Next(0, 2) == 1 ? "male" : "female")}&iagree=1&key=a1e486e2729f46d6bb368d6b2bcda326&password={ownerName}&password_repeat={ownerName}&platform=www&referrer=&send-email=1&thirdpartyemail=0&fb=0";

            StringContent query = new StringContent(queryStr);
            var result = await client.PostAsync("https://spclient.wg.spotify.com/signup/public/v1/account", query, new CancellationTokenSource(new TimeSpan(0, 0, 5)).Token);
            if (result.StatusCode == HttpStatusCode.OK)
            {
                Console.WriteLine($"{ownerName}:{ownerName}");

                AccountApi accountApi = JsonSerializer.Deserialize<AccountApi>(result.Content.ReadAsStringAsync().Result);

                if (accountApi.is_country_launched)
                {
                    return new Account()
                    {
                        Email = ownerName,
                        Password = ownerName
                    };
                }
            }
            else
            {
                if (result.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    await Task.FromException(new Exception("Too many request"));
                }
            }

            return new Account();
        }
    }
}