using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using StatisticWMG.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xabe.FFmpeg;

namespace StatisticWMG
{
    public static class WMGSource
    {
        private static SheetsService UserCredential()
        {
            try
            {

                ServiceAccountCredential credential1;
                string[] Scopes = { SheetsService.Scope.Spreadsheets };
                string serviceAccountEmail = "trackingnewdara@quickstart-1605058837166.iam.gserviceaccount.com";
                string jsonfile = "trackingNewData.json";
                using (Stream stream = new FileStream(@jsonfile, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    credential1 = (ServiceAccountCredential)
                    GoogleCredential.FromStream(stream).UnderlyingCredential;

                    var initializer = new ServiceAccountCredential.Initializer(credential1.Id)
                    {
                        User = serviceAccountEmail,
                        Key = credential1.Key,
                        Scopes = Scopes
                    };
                    credential1 = new ServiceAccountCredential(initializer);
                }
                var service = new SheetsService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential1,
                    ApplicationName = "",
                });
                return service;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;

            }
        }
        public static List<SpotifyInfo> GetAllSongFromServer()
        {
            try
            {
                List<SpotifyInfo> songs = new List<SpotifyInfo>();
                List<SpotifyInfo> result = new List<SpotifyInfo>();
                string currentDir = Directory.GetCurrentDirectory();
                FFmpeg.SetExecutablesPath(currentDir);
                string route1 = "http://118.69.82.99:9001/film_video_processing/List?length=1000000000";
                string route = "http://1.53.252.34:9001/film_video_processing/List?length=1000000000";
                List<SongInfo> webSongInfos = new List<SongInfo>();
                using (var client = new System.Net.Http.HttpClient())
                {


                    try
                    {
                        var response = client.GetAsync(route).GetAwaiter().GetResult();
                        if (response.IsSuccessStatusCode)
                        {
                            var jsonResponse = response.Content.ReadAsStringAsync().Result;
                            webSongInfos = Newtonsoft.Json.JsonConvert.DeserializeObject<WebResponse>(jsonResponse).Data;
                        }
                        else
                        {
                            throw new Exception("Get web data failed");
                        }
                    }
                    catch
                    {
                        var response = client.GetAsync(route1).GetAwaiter().GetResult();
                        if (response.IsSuccessStatusCode)
                        {
                            var jsonResponse = response.Content.ReadAsStringAsync().Result;
                            webSongInfos = Newtonsoft.Json.JsonConvert.DeserializeObject<WebResponse>(jsonResponse).Data;
                        }
                        else
                        {
                            throw new Exception("Get web data failed");
                        }
                    }

                }
                foreach (var item in webSongInfos)
                {
                    var song = new SpotifyInfo();
                    song.TrackTitle = item.Title == null ? "" : item.Title;
                    song.Code = item.Code == null ? "" : item.Code;
                    song.Artists = item.Actor == null ? "" : item.Actor;
                    string date = "";
                    if (string.IsNullOrEmpty(date))
                    {
                        CultureInfo culture = new CultureInfo("en-US");
                        date = Convert.ToDateTime(item.datecreated, culture).ToString("yyyy-MM-dd");
                    }
                    song.CreateDate = date;
                    songs.Add(song);
                }
                return songs;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
