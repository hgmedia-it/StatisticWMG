using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using StatisticWMG.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;

namespace StatisticWMG
{
    public static class SpotifyService
    {
        //For SPOTIFY API
        private static string _clientId = "0db429af58024a668d74cb1b0c373c8e";
        private static string _clientSecretKey = "b86a76f29b964afd9a7e58d06102459c";
        private static string _accessToken = "BQCIgnh93Z1diYJoq8YYUzrH914Mm3RSfZjEJGwNrO3HLpZemKOnv2GPYU9a69tEcHbIXwZqKSUhREYQXzCQcyEKPvERo344By1whBIZYE0njDHHzdI86OBgRWagXSMuWknK47GebiQvsTsBdP4YDWAIBhMLz-v3-YyA96uMW4knB5CjNGsjcKfRa0Fe7bgrMAQ9PtrhxmqDDLQHNa_HDrxJ7nKzRPoLjpzfNQRESb2TKIKcND69nOlbBrHrFm9A2y6Iz2X9iqRBqFHuiif3zWBCoEBOpXU8UBs";
        private static int _groupSp = 100;

        //public static List<Songs> GetArtistGenresAndRegion1(List<Songs> songs)
        //{
        //    try
        //    {
        //        List<string> listArtist = new List<string>();
        //        var keyValuePairs = new List<KeyValuePair<string, string>>();
        //        foreach (var item in songs)
        //        {
        //            if (!listArtist.Any(p => p.Equals(item.TrackArtist)) && !string.IsNullOrEmpty(item.TrackArtist))
        //            {
        //                listArtist.Add(item.TrackArtist);
        //            }
        //        }

        //        var spGroupCount = (int)Math.Ceiling((double)listArtist.Count / (double)50);
        //        for (var i = 0; i < spGroupCount; i++)
        //        {
        //            var groupSongs = listArtist.Skip(i * 50).Take(50).ToList();
        //            Console.WriteLine($"Running {i}...");
        //            var list = GetGenreByArtist(groupSongs).GetAwaiter().GetResult();
        //            keyValuePairs.AddRange(list);
        //            Console.WriteLine($"Done. Wating 10s...");
        //            Thread.Sleep(10 * 5);
        //        }
        //        foreach (var item in songs)
        //        {
        //            foreach (var pair in keyValuePairs)
        //            {
        //                if (pair.Key.Equals(item.TrackArtist))
        //                {
        //                    item.Genres = pair.Value;
        //                }
        //            }
        //        }
        //        var lines = new List<string>();
        //        foreach (var song in songs)
        //        {
        //            song.Region = GetCountryFromGenre(song.Genres);
        //            lines.Add(song.TrackName + "\t"
        //                        + song.Code + "\t"
        //                        + song.TrackArtist + "\t"
        //                        + song.Genres + "\t"
        //                        + song.Region);
        //        }
        //        if (File.Exists("genre.txt"))
        //        {
        //            File.Delete("genre.txt");
        //        }
        //        File.AppendAllLines("genre.txt", lines);
        //        return songs;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception(ex.Message);
        //    }


        //}
        public static List<SpotifyInfo> GetArtistGenresAndRegion(List<SpotifyInfo> songs)
        {
            try
            {
                List<string> listArtist = new List<string>();
                var keyValuePairs = new List<KeyValuePair<string, string>>();
                foreach (var item in songs)
                {
                    if (!listArtist.Any(p => p.Equals(item.Artists)) && !string.IsNullOrEmpty(item.Artists))
                    {
                        listArtist.Add(item.Artists);
                    }
                }

                var spGroupCount = (int)Math.Ceiling((double)listArtist.Count / (double)_groupSp);
                for (var i = 0; i < spGroupCount; i++)
                {
                    var groupSongs = listArtist.Skip(i * _groupSp).Take(_groupSp).ToList();
                    Console.WriteLine($"Running {i}...");
                    var list = GetGenreByArtist(groupSongs).GetAwaiter().GetResult();
                    keyValuePairs.AddRange(list);
                    Console.WriteLine($"Done. Wating 10s...");
                    Thread.Sleep(10000);
                }
                foreach (var item in songs)
                {
                    foreach (var pair in keyValuePairs)
                    {
                        if (pair.Key.Equals(item.Artists))
                        {
                            item.Genres = pair.Value;
                        }
                    }
                }
                var lines = new List<string>();
                foreach (var song in songs)
                {
                    song.Country = GetCountryFromGenre(song.Genres);
                    lines.Add(song.TrackTitle + "\t"
                                + song.Code + "\t"
                                + song.Artists + "\t"
                                + song.Genres + "\t"
                                + song.Country);
                }
                if (File.Exists("genre.txt"))
                {
                    File.Delete("genre.txt");
                }
                File.AppendAllLines("genre.txt", lines);
                return songs;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }


        }
        public static async Task<List<KeyValuePair<string, string>>> GetGenreByArtist(List<string> artist)
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
            var searchQuery = $"{artist}";
            searchQuery = HttpUtility.UrlEncode(searchQuery);
            string uri = $"https://api.spotify.com/v1/search?q={searchQuery}" + "&type=artist&offset=0&limit=3";
            HttpClient httpClient = new HttpClient();
            using (var client = httpClient)
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
                    //Thread.Sleep(3000);
                    var result = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
                    string text = "";
                    if (result.artists.items.Count == 0)
                    {
                        return "";
                    }
                    else
                    {
                        for (int i = 0; i < result.artists.items.Count; i++)
                        {
                            foreach (var item in result.artists.items[i].genres)
                            {
                                text += item + ";";
                            }
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
                if (genres.Contains("korean") || genres.Contains("k-pop"))
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
                if (genres.Contains("c-pop") || genres.Contains("chinese"))
                {
                    country += "China;";
                    check = true;
                }
                if (genres.Contains("j-pop") || genres.Contains("japanese"))
                {
                    country += "Japan;";
                    check = true;
                }
                if (genres.Contains("finnish"))
                {
                    country += "Finland;";
                    check = true;
                }
                if (genres.Contains("malaysian"))
                {
                    country += "Malaysia;";
                    check = true;
                }
                if (genres.Contains("danish"))
                {
                    country += "Denmark;";
                    check = true;
                }
                if (check == false)
                {
                    country += "US";
                }
                return country;
            }
            catch (Exception ex)
            {
                return null;
            }

        }

        public static async Task<SpotifyInfo> GetAlbumId(SpotifyInfo spotifyInfo)
        {
            try
            {
                //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                var searchQuery = $"{spotifyInfo.TrackTitle} {spotifyInfo.Artists}";
                searchQuery = HttpUtility.UrlEncode(searchQuery);
                var url = $"https://api.spotify.com/v1/search?q={searchQuery}" + "&type=track&offset=0&limit=3";
                //var client = new RestClient("https://api.spotify.com/v1/search?query=The+Intruder+Spacelab&type=track&offset=0&limit=3");
                //client.Timeout = -1;
                //var request = new RestRequest(Method.GET);
                //request.AddHeader("Authorization", $"Bearer {_accessToken}");
                //IRestResponse response = client.Execute(request);
                //if(response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.ServiceUnavailable)
                //{
                //    await UpdateAccessToken();
                //    var authParam = request.Parameters.Find(p => p.Name == "Authorization");
                //    request.Parameters.Remove(authParam);
                //    request.AddHeader("Authorization", $"Bearer {_accessToken}");
                //    response = client.Execute(request);
                //    if (response.StatusCode != System.Net.HttpStatusCode.OK)
                //    {
                //        throw new Exception($"Error when search album '{spotifyInfo.TrackTitle}' of '{spotifyInfo.Artists}'. Status code: {response.StatusCode}");
                //    }
                //}
                //var result = JsonConvert.DeserializeObject<dynamic>(response.Content.ToString());
                //return null;
                using (var http = new HttpClient())
                {
                    http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
                    var response = await http.GetAsync(url);
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.ServiceUnavailable) // 401 - access token expired
                    {
                        await UpdateAccessToken();
                        http.DefaultRequestHeaders.Remove("Authorization");
                        http.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
                        response = await http.GetAsync(url);
                        if (response.StatusCode != System.Net.HttpStatusCode.OK)
                        {
                            throw new Exception($"Error when search album '{spotifyInfo.TrackTitle}' of '{spotifyInfo.Artists}'. Status code: {response.StatusCode}");
                        }
                    }
                    else if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        throw new Exception($"Error when search album '{spotifyInfo.TrackTitle}' of '{spotifyInfo.Artists}'. Status code: {response.StatusCode}");
                    }

                    var result = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
                    if (result.tracks.items == null || result.tracks.items.Count == 0)
                    {
                        return null;
                    }
                    else
                    {
                        for (var i = 0; i < result.tracks.items.Count; i++)
                        {
                            var track = result.tracks.items[i];
                            if (RemoveDiacritics(((string)track.name)).ToLower().Contains(RemoveDiacritics(spotifyInfo.TrackTitle.ToLower())) || (RemoveDiacritics(spotifyInfo.TrackTitle.ToLower())).Contains(RemoveDiacritics((string)track.name)))
                            {
                                var artists = track.artists;
                                for (var j = 0; j < artists.Count; j++)
                                {
                                    if (RemoveDiacritics((string)artists[j].name).ToLower().Contains(RemoveDiacritics(spotifyInfo.Artists.ToLower())) || (RemoveDiacritics(spotifyInfo.Artists.ToLower())).Contains(RemoveDiacritics((string)artists[j].name)))
                                    {
                                        var albumId = ((string)track.album.uri).Replace("spotify:album:", "");
                                        spotifyInfo.TrackId = track.id;
                                        spotifyInfo.ReleaseDate = ((string)track.album.release_date).Substring(0, 4);
                                        spotifyInfo.AlbumId = albumId;
                                        spotifyInfo.AlbumTitle = track.album.name;
                                        spotifyInfo.ArtistId = ((string)(track.artists[0].uri)).Replace("spotify:artist", "");
                                        //if (string.IsNullOrEmpty(spotifyInfo.Genres))
                                        //{
                                        //    spotifyInfo.Genres = await GetGenreByArtistId(spotifyInfo.ArtistId);
                                        //    spotifyInfo.Country = GetCountryFromGenre(spotifyInfo.Genres);
                                        //}

                                        return spotifyInfo;
                                    }
                                }
                            }
                        }
                        for (var i = 0; i < result.tracks.item.Count; i++)
                        {
                            var track = result.tracks.items[i];
                            if (RemoveDiacritics((string)track.name).ToLower().Contains(RemoveDiacritics(spotifyInfo.TrackTitle.ToLower())) || (RemoveDiacritics(spotifyInfo.TrackTitle.ToLower())).Contains(RemoveDiacritics((string)track.name)))
                            {
                                var albumId = ((string)track.album.uri).Replace("spotify:album:", "");
                                spotifyInfo.TrackId = track.id;
                                spotifyInfo.ReleaseDate = ((string)track.album.release_date).Substring(0, 4);
                                spotifyInfo.AlbumId = albumId;
                                spotifyInfo.AlbumTitle = track.album.name;
                                spotifyInfo.ArtistId = ((string)(track.artists[0].uri)).Replace("spotify:artist", "");
                                //if (string.IsNullOrEmpty(spotifyInfo.Genres))
                                //{
                                //    spotifyInfo.Genres = await GetGenreByArtistId(spotifyInfo.ArtistId);
                                //    spotifyInfo.Country = GetCountryFromGenre(spotifyInfo.Genres);
                                //}
                                return spotifyInfo;
                            }
                        }
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + " " + spotifyInfo.TrackTitle);
                return null;
            }
        }
        public static async Task<List<SpotifyInfo>> GetListCountTracksPlay(List<SpotifyInfo> spotifyInfos)
        {
            var tasks = new List<Task<SpotifyInfo>>();
            foreach (var item in spotifyInfos)
            {
                try
                {
                    tasks.Add(CountTracksPlay(item,false));
                }
                catch (Exception ex)
                {

                }

            }
            await Task.WhenAll(tasks.Select(t => t));
            return spotifyInfos;
        }
        private static async Task<SpotifyInfo> CountTracksPlay(SpotifyInfo spotifyInfo, bool getAlbumId = true)
        {
            try
            {
                var url = "";
                if (getAlbumId)
                {
                    var trackInfo = await GetAlbumId(spotifyInfo);
                    if (trackInfo == null)
                    {
                        return spotifyInfo;
                    }
                    url = $"http://localhost:8080/albumPlayCount?albumid={trackInfo.AlbumId}";
                }
                else
                {
                    url = $"http://localhost:8080/albumPlayCount?albumid={spotifyInfo.AlbumId}";
                }

                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(url);
                    var result = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
                    if (result.success == true)
                    {
                        var tracks = result.data.discs[0].tracks;

                        for (var i = 0; i < tracks.Count; i++)
                        {
                            var id = ((string)tracks[i].uri).Replace("spotify:track:", "");
                            if (spotifyInfo.TrackId.Equals(id))
                            {
                                spotifyInfo.StreamCount = long.Parse(tracks[i].playcount.ToString());
                                if (spotifyInfo.StreamCount == 0)
                                {
                                    spotifyInfo.StreamCount = 1000;
                                }
                                return spotifyInfo;
                            }

                        }
                        return spotifyInfo;
                    }
                    else
                    {
                        return spotifyInfo;
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + " " + spotifyInfo.TrackTitle);
                return spotifyInfo;
            }
        }
        public static async Task<List<SpotifyInfo>> GetSpotifyInfoAsync(List<SpotifyInfo> spotifyInfos, string fileResultName)
        {
            try
            {
                var tasks = new List<Task<SpotifyInfo>>();
                foreach (var track in spotifyInfos)
                {
                    try
                    {
                        tasks.Add(CountTracksPlay(track));
                    }
                    catch (Exception ex)
                    {
                        // do nothing
                    }
                }

                await Task.WhenAll(tasks.Select(t => t));
                var lines = new List<string>();
                foreach (var track in spotifyInfos)
                {
                    lines.Add(track.TrackTitle + "\t"
                        + track.TrackId + "\t"
                        + track.AlbumId + "\t"
                        + track.Code + "\t"
                        + track.Artists + "\t"
                        + track.Genres + "\t"
                        + track.Country + "\t"
                        + track.ReleaseDate + "\t"
                        + track.StreamCount);
                }
                File.AppendAllLines(fileResultName, lines);
                return spotifyInfos;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return spotifyInfos;
            }

        }
        private static string RemoveDiacritics(string text)
        {
            try
            {
                var normalizedString = text.Normalize(NormalizationForm.FormD);
                var stringBuilder = new StringBuilder();

                foreach (var c in normalizedString)
                {
                    var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                    if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                    {
                        stringBuilder.Append(c);
                    }
                }

                return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
            }
            catch
            {
                return text;
            }

        }
    }
}
