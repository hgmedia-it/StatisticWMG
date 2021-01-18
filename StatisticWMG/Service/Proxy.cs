using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace StatisticWMG
{
    public static class Proxy
    {
        public static WebClient ChangeProxy(Account account)
        {
            try
            {

                //var baseAddress = new Uri("https://www.youtube.com");
                //var httpClientHandler = new HttpClientHandler
                //{
                //    CookieContainer = new CookieContainer()
                //};
                //httpClientHandler.CookieContainer.Add(baseAddress, new Cookie("PREF", "f6=40000000&hl=en"));

                //if (!string.IsNullOrEmpty(account.ip))
                //{
                //    var proxies = new WebProxy
                //    {
                //        Address = new Uri($"http://{account.ip}:{account.port}"),
                //        BypassProxyOnLocal = true,
                //        UseDefaultCredentials = false
                //    };

                //    if (!string.IsNullOrEmpty(account.username) && !string.IsNullOrEmpty(account.password))
                //    {
                //        proxies.Credentials = new NetworkCredential(userName: account.username, password: account.password);
                //    }
                //    httpClientHandler.Proxy = proxies;
                //}
                //var httpClient = new HttpClient(handler: httpClientHandler, disposeHandler: true);
                WebClient client = new WebClient();
                client.Headers.Add(HttpRequestHeader.Cookie, "PREF=f6=40000000&hl=en");
                WebProxy proxy = new WebProxy(account.ip, account.port);
                //proxy.Address = new Uri("https://www.youtube.com");
                proxy.Credentials = new NetworkCredential(account.username, account.password);
                client.Proxy = proxy;

                return client;
            }
            catch(Exception ex)
            {
                return new WebClient();
            }

        }
        public static Account RandomProxy()
        {
            try
            {
                var listAcc = new List<Account>();
                var lines = File.ReadAllLines("ListProxy.txt");
                foreach (var line in lines)
                {
                    string[] acc = line.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                    var proxy = new Account();
                    proxy.ip = acc[0];
                    proxy.port = int.Parse(acc[1]);
                    proxy.username = acc[2];
                    proxy.password = acc[3];
                    listAcc.Add(proxy);
                }
                Random rnd = new Random();
                Account s = listAcc[rnd.Next(listAcc.Count)];
                return s;
            }
            catch(Exception ex)
            {
                return null;
            }
        }
    }
    public class Account
    {
        public string username { get; set; }
        public string password { get; set; }
        public string ip { get; set; }
        public int port { get; set; }
    }
}
