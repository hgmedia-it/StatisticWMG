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
        private static string _accessToken = "BQCo5MaaZAI2XVKw2FMpqP9Nbeds6IC1ytESuu9AUAfp8cx1Ojv1NOHC6ZhHII_XzpbEcmjP1sQsN7n-9lDxhk0c-ZZmXZ6RaiG7NBHIzaGlhWqmE68h1tX7hkBC-UfD1DUmzaqgLfs1neXz7p-_XoJQCJGThgsBP7_BaUD0P6_XBm3Ui2z8IuG5gK_F0MQPtcNIygjtwQafmY7xeYJ3SpO7D2T3Wv3Z8bteZ9CcgPMzdNwdqkFY_W1Dt2xlUa5sGmOKa4fhYpJTIgiyDu6gPX7BJBVkyL6ZlD0";
        private static int _groupSp = 50;
        private static string _playlistWMG = "7qmlWD30wui2qRZTYuub3x";

        public static List<SpotifyInfo> GetArtistGenresAndRegion(List<SpotifyInfo> songs)
        {
            try
            {
                List<string> listArtist = new List<string>();
                var keyValuePairs = new List<KeyValuePair<string, string>>();
                foreach (var item in songs)
                {
                    if (!listArtist.Any(p => p.Equals(item.ArtistId)) && !string.IsNullOrEmpty(item.ArtistId))
                    {
                        listArtist.Add(item.ArtistId);
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
                        if (pair.Key.Equals(item.ArtistId))
                        {
                            item.Genres = pair.Value;
                        }
                    }
                }
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
        private static async Task<string> GetGenre(string artistId)
        {
            var searchQuery = $"{artistId}";
            searchQuery = HttpUtility.UrlEncode(searchQuery);
            string uri = $"https://api.spotify.com/v1/artists/{searchQuery}";
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
                    if (result.genres.Count == 0)
                    {
                        return "";
                    }
                    else
                    {
                        foreach (var item in result.genres)
                        {
                            text += item + ";";
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
                if (genres.Contains("spanish") || genres.Contains("galician") || genres.Contains("valenciana"))
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
                if (genres.Contains("colombian") || genres.Contains("colombiana"))
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
                if (genres.Contains("korean") || genres.Contains("k-pop") || genres.Contains("k-rap"))
                {
                    country += "Korea;";
                    check = true;
                }
                if (genres.Contains("russian") || genres.Contains("ukrainian") || genres.Contains("belarusian"))
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
                if (genres.Contains("j-pop") || genres.Contains("japanese") ||genres.Contains("j-poprock") || genres.Contains("j-idol") || genres.Contains("anime") || genres.Contains("j-metal"))
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
                if (genres.Contains("norwegian"))
                {
                    country += "Nauy;";
                }
                if (genres.Contains("polish"))
                {
                    country += "Poland;";
                }
                if (genres.Contains("argentine") || genres.Contains("argentino"))
                {
                    country += "Argentina;";
                }
                if (genres.Contains("croatian"))
                {
                    country += "Croatia;";
                }
                if (genres.Contains("taiwan") ||genres.Contains("taiwanese"))
                {
                    country += "Taiwan;";
                }
                if (genres.Contains("singaporean"))
                {
                    country += "Singapore;";
                }
                if (genres.Contains("svensk"))
                {
                    country += "Switzerland;";
                }
                if (genres.Contains("dominican"))
                {
                    country += "Dominican Republic;";
                }
                if (genres.Contains("thai"))
                {
                    country += "ThaiLand;";
                }
                if (genres.Contains("v-pop") || genres.Contains("vietnamese"))
                {
                    country += "VietNam;";
                }
                if (genres.Contains("scandinavian"))
                {
                    country += "Scandinavia;";
                }
                if (genres.Contains("portuguese"))
                {
                    country += "Portugal;";
                }
                if (genres.Contains("hong kong"))
                {
                    country += "HongKong;";
                }
                if (genres.Contains("puerto rican"))
                {
                    country += "Puerto Rico;";
                }
                if (genres.Contains("slovak"))
                {
                    country += "Slovakia;";
                }
                if (genres.Contains("irish"))
                {
                    country += "Ireland;";
                }
                if (genres.Contains("chilean"))
                {
                    country += "Chile;";
                }
                if (genres.Contains("belgian"))
                {
                    country += "Belgium;";
                }
                if (genres.Contains("georgian"))
                {
                    country += "Georgia;";
                }
                if (genres.Contains("south african"))
                {
                    country += "South Africa;";
                }
                if (genres.Contains("hungarian"))
                {
                    country += "Hungary;";
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
                var searchQuery = "";
                var url = "";
                if (!string.IsNullOrEmpty(spotifyInfo.Code))
                {
                    searchQuery = $"isrc:{spotifyInfo.Code}";
                    searchQuery = HttpUtility.UrlEncode(searchQuery);
                    url = $"https://api.spotify.com/v1/search?q={searchQuery}" + "&market=VN&type=track&offset=0&limit=20";
                    var result = await GetAlbumIdByCode(spotifyInfo, url);
                    return result;
                }
                else
                {
                    if(string.IsNullOrEmpty(spotifyInfo.Artists) || spotifyInfo.Artists.ToLower().Equals("đang cập nhật"))
                    {
                        return null;
                    }
                    else
                    {
                        searchQuery = spotifyInfo.TrackTitle + " " + spotifyInfo.Artists;
                        searchQuery = HttpUtility.UrlEncode(searchQuery);
                        url = $"https://api.spotify.com/v1/search?q={searchQuery}" + "&market=VN&type=track&offset=0&limit=20";
                        var result = await GetAlbumIdByName(spotifyInfo, url);
                        return result;
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + " " + spotifyInfo.TrackTitle);
                return null;
            }
        }
        public static async Task<SpotifyInfo> CountTracksPlay(SpotifyInfo spotifyInfo, bool getAlbumId = true)
        {
            try
            {
                var url = "";
                if (getAlbumId)
                {
                    var trackInfo  = await GetAlbumId(spotifyInfo);
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
                    Thread.Sleep(1000);
                    var response = await client.GetAsync(url);
                    var result = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
                    if (result.success == true)
                    {
                        var discs = result.data.discs;
                        foreach (var item in discs)
                        {
                            var tracks = item.tracks;
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
                                    if (string.IsNullOrEmpty(spotifyInfo.Popularity))
                                    {
                                        spotifyInfo.Popularity = (string)tracks[i].popularity;
                                    }
                                    return spotifyInfo;
                                }

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
        public static async Task<List<SpotifyInfo>> GetSpotifyInfoAsync(List<SpotifyInfo> spotifyInfos, string fileResultName,bool getAlbumId = true)
        {
            try
            {
                var tasks = new List<Task<SpotifyInfo>>();
                foreach (var track in spotifyInfos)
                {
                    try
                    {
                        tasks.Add(CountTracksPlay(track,getAlbumId));
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
                    string link = "";
                    if(string.IsNullOrEmpty(track.TrackId) || string.IsNullOrEmpty(track.AlbumId))
                    {
                        link = "";
                    }
                    else
                    {
                        link = "https://open.spotify.com/album/" + track.AlbumId + "?highlight=spotify:track:" + track.TrackId;
                    }
                    track.LinkSpotify = link;
                    lines.Add(track.TrackTitle + "\t"
                        + track.Code + "\t"
                        + track.Artists + "\t"
                        + link + "\t"
                        + track.Genres + "\t"
                        + track.Country + "\t"
                        + track.ReleaseDate + "\t"
                        + track.Popularity + "\t"
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
        public static async Task AddTrack(List<string> listString)
        {
            try
            {
                var body = new Uris() { Uri = new List<string>(listString) };
                var json = JsonConvert.SerializeObject(body);
                string url = $"https://api.spotify.com/v1/playlists/{_playlistWMG}/tracks";
                using (var http = new HttpClient())
                {
                    http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
                    var stringContent = new StringContent(json, UnicodeEncoding.UTF8, "application/json");
                    var response = await http.PostAsync(url,stringContent);
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.ServiceUnavailable) // 401 - access token expired
                    {
                        await UpdateAccessToken();
                        http.DefaultRequestHeaders.Remove("Authorization");
                        http.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
                        response = await http.GetAsync(url);
                        if (response.StatusCode != System.Net.HttpStatusCode.OK)
                        {
                            throw new Exception($"Error when authorization again");
                        }
                    }
                    else if (response.StatusCode != System.Net.HttpStatusCode.Created)
                    {
                        throw new Exception($"Status code error ");
                    }
                }

                }
            catch
            {

            }
        }
        public static async Task<SpotifyInfo> GetAlbumIdByCode(SpotifyInfo spotifyInfo,string url)
        {
            try
            {
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
                        http.DefaultRequestHeaders.Add("scope", "user-read-private");
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
                        List<SpotifyInfo> newList = new List<SpotifyInfo>();
                        for (var i = 0; i < result.tracks.items.Count; i++)
                        {
                            var track = result.tracks.items[i];
                            var artists = track.artists;
                            for (var j = 0; j < artists.Count; j++)
                            {
                                SpotifyInfo item = new SpotifyInfo();
                                var albumId = ((string)track.album.uri).Replace("spotify:album:", "");
                                item.TrackId = track.id;
                                item.Popularity = (string)track.popularity;
                                item.ReleaseDate = ((string)track.album.release_date);
                                item.AlbumId = albumId;
                                item.AlbumTitle = track.album.name;
                                item.ArtistId = ((string)(track.artists[0].uri)).Replace("spotify:artist:", "");
                                newList.Add(item);

                            }
                        }
                        var items = newList.Where((x) => int.Parse(x.ReleaseDate.Substring(0, 4)) == newList.Min(y => int.Parse(y.ReleaseDate.Substring(0, 4)))).ToList();
                        if (items == null || items.Count == 0)
                        {
                            return null;
                        }
                        else
                        {
                            if (items.Count == 1)
                            {
                                spotifyInfo.TrackId = items[0].TrackId;
                                spotifyInfo.ReleaseDate = items[0].ReleaseDate.Substring(0, 4); ;
                                spotifyInfo.AlbumId = items[0].AlbumId;
                                spotifyInfo.AlbumTitle = items[0].AlbumTitle;
                                spotifyInfo.ArtistId = items[0].ArtistId;
                                spotifyInfo.Popularity = items[0].Popularity;
                                return spotifyInfo;
                            }
                            else
                            {
                                bool check = items.Any(i => i.ReleaseDate.Length == 4);
                                if (check)
                                {
                                    var spo = items.FirstOrDefault(x => x.ReleaseDate.Length == 4);
                                    spotifyInfo.TrackId = spo.TrackId;
                                    spotifyInfo.ReleaseDate = spo.ReleaseDate.Substring(0, 4); ;
                                    spotifyInfo.AlbumId = spo.AlbumId;
                                    spotifyInfo.AlbumTitle = spo.AlbumTitle;
                                    spotifyInfo.ArtistId = spo.ArtistId;
                                    spotifyInfo.Popularity = items[0].Popularity;
                                    return spotifyInfo;
                                }
                                else
                                {
                                    var spo = new SpotifyInfo();
                                    try
                                    {
                                        spo = items.Find((x) => DateTime.Parse(x.ReleaseDate) == newList.Min(y => DateTime.Parse(y.ReleaseDate)));
                                    }
                                    catch
                                    {
                                        spo = items.FirstOrDefault();
                                    }
                                    spotifyInfo.TrackId = spo.TrackId;
                                    spotifyInfo.ReleaseDate = spo.ReleaseDate.Substring(0, 4); ;
                                    spotifyInfo.AlbumId = spo.AlbumId;
                                    spotifyInfo.AlbumTitle = spo.AlbumTitle;
                                    spotifyInfo.ArtistId = spo.ArtistId;
                                    spotifyInfo.Popularity = items[0].Popularity;
                                    return spotifyInfo;
                                }
                            }

                        }

                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message + " " + spotifyInfo.TrackTitle);
                return null;
            }
        }
        public static async Task<SpotifyInfo> GetAlbumIdByName(SpotifyInfo spotifyInfo,string url)
        {
            try
            {
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
                                        spotifyInfo.Popularity = (string)track.popularity;
                                        spotifyInfo.ReleaseDate = ((string)track.album.release_date).Substring(0, 4);
                                        spotifyInfo.AlbumId = albumId;
                                        spotifyInfo.AlbumTitle = track.album.name;
                                        spotifyInfo.ArtistId = ((string)(track.artists[0].uri)).Replace("spotify:artist:", "");
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
                                spotifyInfo.Popularity = (string)track.popularity;
                                spotifyInfo.ReleaseDate = ((string)track.album.release_date).Substring(0, 4);
                                spotifyInfo.AlbumId = albumId;
                                spotifyInfo.AlbumTitle = track.album.name;
                                spotifyInfo.ArtistId = ((string)(track.artists[0].uri)).Replace("spotify:artist:", "");
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
    }
    public class Uris
    {
        [JsonProperty("uris")]
        public List<string> Uri { get; set; }
    }
}
