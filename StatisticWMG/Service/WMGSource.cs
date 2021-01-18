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
        public static void AddAllSongsWMGToSource(List<SpotifyInfo> songs)
        {
            try
            {
                string id = "1gVSSRpQw5XVwOcec4E0euiNHZiHXOc5HpYNGzvUWkkM";
                string rangeSheet = "Sheet2";
                var service = UserCredential();
                IList<SpotifyInfo> dataList = songs;
                List<Google.Apis.Sheets.v4.Data.ValueRange> data = new List<Google.Apis.Sheets.v4.Data.ValueRange>();
                ValueRange valueDataRange = new ValueRange() { MajorDimension = "ROWS" };
                valueDataRange.Values = new List<IList<object>>();
                valueDataRange.Values.Add(new List<object> { "STT", "TRACK NAME", "CODE", "TRACK ARTIST", "CREATE DATE" });
                valueDataRange.Range = rangeSheet;
                for (int i = 0; i < dataList.Count; i++)
                {
                    IList<object> list = new List<object> { i+1 , dataList[i].TrackTitle, dataList[i].Code,
                        dataList[i].Artists, dataList[i].CreateDate };
                    valueDataRange.Values.Add(list);

                }
                data.Add(valueDataRange);

                // TODO: Assign values to desired properties of `requestBody`:

                Google.Apis.Sheets.v4.Data.BatchUpdateValuesRequest requestBody = new Google.Apis.Sheets.v4.Data.BatchUpdateValuesRequest();
                requestBody.ValueInputOption = "USER_ENTERED";
                requestBody.Data = data;
                // API to update data to sheet
                SpreadsheetsResource.ValuesResource.BatchUpdateRequest request = service.Spreadsheets.Values.BatchUpdate(requestBody, id);
                Google.Apis.Sheets.v4.Data.BatchUpdateValuesResponse response = request.Execute();
            }
            catch (Exception ex)
            {

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
        public static void Test()
        {
            
        }
    }
}
