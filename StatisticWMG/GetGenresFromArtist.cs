using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Configuration;

namespace StatisticWMG
{
    public static class GetGenresFromArtist
    {
        //For SPOTIFY API
        private static string _clientId = "dae1e9d3832541e2a224f0d4a9ed4467";
        private static string _clientSecretKey = "81e62e4481fa47b1ba28346cb09a8a26";
        private static string _accessToken = "BQCtoG6v9i8E3H7-ugIfCZ9ACQrzo6Xlo-u3Plwo656gfv7ZLyb_qAyNuQgQ9xiCTNmsAzwmQldksVqcj0LeGcj3N4UTLWhmuqIiwYpFf1rZub4-QdfHlGCmvHFm1h_cw-7zScGJrygzkmPpogO-gwEU1Ebjl0eCsBBKm4-826bR8kN9MQwLwVGWQIlCOAlguJu19SlAdJld-msuHMxrn4LnuVL78kkSqqoS-7AwoBbusxc99N-t44pD9h5IoC157wzlwZjoMSph6lzdUKXARZvHM_dtcBomfJPjLcg";
        private static object locker = new object();          

        public static List<Songs> GetArtistGenresAndRegion(List<Songs> songs)
        {
            try
            {
                List<string> listArtist = new List<string>();
                var keyValuePairs = new List<KeyValuePair<string, string>>();
                foreach(var item in songs)
                {
                    if(!listArtist.Any(p => p.Equals(item.TrackArtist)))
                    {
                        listArtist.Add(item.TrackArtist);
                    }
                }

                var spGroupCount = (int)Math.Ceiling((double)listArtist.Count / (double)50);
                for (var i = 0; i < spGroupCount; i++)
                {
                    var groupSongs = listArtist.Skip(i * 50).Take(50).ToList();
                    Console.WriteLine($"Running {i}...");
                    var list = GetGenreByArtist(groupSongs).GetAwaiter().GetResult();
                    keyValuePairs.AddRange(list);
                    Console.WriteLine($"Done. Wating 10s...");
                    Thread.Sleep(10 * 5);
                }
                foreach(var item in songs)
                {
                    foreach(var pair in keyValuePairs)
                    {
                        if (pair.Key.Equals(item.TrackArtist))
                        {
                            item.Genres = pair.Value;
                        }
                    }
                }
                //var lines = new List<string>();
                foreach (var song in songs)
                {
                    song.Region = GetCountryFromGenre(song.Genres);
                }
                //File.AppendAllLines(fileResultName, lines);
                return songs;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }


        }
        public static async Task<List<KeyValuePair<string,string>>> GetGenreByArtist(List<string> artist)
        {
            var tasks = new List<KeyValuePair<string, Task<string>>>();
            var keyValuePairs = new List<KeyValuePair<string, string>>();
            foreach (var item in artist)
            {
                try
                {
                    tasks.Add(new KeyValuePair<string, Task<string>>(item, GetGenre(item)));
                }
                catch (Exception ex)
                {

                }

            }
            await Task.WhenAll(tasks.Select(t => t.Value));
            foreach (var task in tasks)
            {
                if (task.Value.IsCompleted)
                {
                    try
                    {
                        var taskResult = task.Value.GetAwaiter().GetResult();
                        keyValuePairs.Add(new KeyValuePair<string, string>(task.Key, taskResult));
                    }
                    catch (Exception ex)
                    {
                        // do nothing
                    }
                }
                else
                {
                    // do nothing
                }
            }
            return keyValuePairs;
        }
        private static async Task UpdateAccessToken()
        {
            var url = $@"https://accounts.spotify.com/api/token";
            using (var http = new HttpClient())
            {
                var encodedClient = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecretKey}"));
                http.DefaultRequestHeaders.Add("Authorization", $"Basic {encodedClient}");

                var pairs = new List<KeyValuePair<string, string>>();
                pairs.Add(new KeyValuePair<string, string>("grant_type", "client_credentials"));
                var bodyContent = new FormUrlEncodedContent(pairs);

                var response = await http.PostAsync(url, bodyContent);
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    throw new Exception($"111. Status code: {response.StatusCode}");
                }
                var result = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());

                _accessToken = result.access_token;
            }
        }
        private static async Task<string> GetGenre(string artist)
        {
            string uri = "https://api.spotify.com/v1/search?q=" + artist.Trim() + "&type=artist&offset=0&limit=3";
            using (var client = new HttpClient() { Timeout = TimeSpan.FromSeconds(50) })
            {
                try
                {
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
                    var response = await client.GetAsync(uri);
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.ServiceUnavailable)
                    {
                        await UpdateAccessToken();
                        client.DefaultRequestHeaders.Remove("Authorization");
                        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
                        Thread.Sleep(1000);
                        response = await client.GetAsync(uri);
                        if (response.StatusCode != System.Net.HttpStatusCode.OK)
                        {
                            throw new Exception($"Error. Status code: {response.StatusCode}");
                        }
                        else
                        {
                            Console.WriteLine("Request token access successfully");
                        }
                    }
                    else if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        throw new Exception($"Error. Status code: {response.StatusCode}");
                    }
                    Thread.Sleep(3000);
                    JObject rsObject = JObject.Parse(response.Content.ReadAsStringAsync().Result);
                    var content = rsObject["artists"].ToString();
                    var respone = JsonConvert.DeserializeObject<Artists>(content);
                    string genre = "";
                    string text = "";
                    if (respone.items.Count == 0 || respone.items == null)
                    {
                        text = "";
                    }
                    foreach (var item in respone.items)
                    {
                        if (item.genres.Count == 0 || item.genres == null)
                        {
                            text = "";
                        }
                        else
                        {
                            foreach (var x in item.genres)
                            {
                                genre += x + ";";
                            }
                            text = genre;
                            break;
                        }

                    }
                    return text;
                }
                catch (Exception ex)
                {
                    
                    Console.WriteLine(ex.Message);
                    return null;

                }
            }
        }
        public static string GetCountryFromGenre(string genres)
        {
            try
            {
                string country = "";
                bool check = false;
                if (string.IsNullOrEmpty(genres))
                {
                    return "";
                }
                if (genres.Contains("spanish"))
                {
                    country += "Spain;";
                    check = true;
                }
                if (genres.Contains("mexican"))
                {
                    country += "Mexico;";
                    check = true;
                }
                if (genres.Contains("brazilian"))
                {
                    country += "Brazil;";
                    check = true;
                }
                if (genres.Contains("french"))
                {
                    country += "France;";
                    check = true;
                }
                if (genres.Contains("australian"))
                {
                    country += "Australia;";
                    check = true;
                }
                if (genres.Contains("swedish"))
                {
                    country += "Sweden;";
                    check = true;
                }
                if (genres.Contains("uk") || genres.Contains("british"))
                {
                    country += "UK;";
                    check = true;
                }
                if (genres.Contains("scottish"))
                {
                    country += "Scotland;";
                    check = true;
                }
                if (genres.Contains("colombian"))
                {
                    country += "Columbia;";
                    check = true;
                }
                if (genres.Contains("german"))
                {
                    country += "Germany";
                    check = true;
                }
                if (genres.Contains("italian"))
                {
                    country += "Italia";
                    check = true;
                }
                if(genres.Contains("korean") || genres.Contains("k-pop"))
                {
                    country += "Korea;";
                    check = true;
                }
                if (genres.Contains("russian"))
                {
                    country += "Russia;";
                    check = true;
                }
                if (genres.Contains("canadian"))
                {
                    country += "Canada;";
                    check = true;
                }
                if (genres.Contains("indonesian"))
                {
                    country += "Indonesia;";
                    check = true;
                }
                if(check == false)
                {
                    country += "US";
                }
                return country;
            }
            catch(Exception ex)
            {
                return null;
            }

        }
    }
}
