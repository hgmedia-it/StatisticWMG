using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using Newtonsoft.Json;
using StatisticWMG.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xabe.FFmpeg;
using Data = Google.Apis.Sheets.v4.Data;

namespace StatisticWMG
{
    public static class GetDataFromGoogleSheet
    {
        static string ApplicationName = "";
        public static List<SpotifyInfo> GetSongsFromWMGSourceGoogleSheet()
        {
            try
            {
                ServiceAccountCredential credential;
                string[] Scopes = { SheetsService.Scope.Spreadsheets };
                string serviceAccountEmail = "trackingnewdara@quickstart-1605058837166.iam.gserviceaccount.com";
                string jsonfile = "trackingNewData.json";
                string spreadsheetID = "1H3uwofbYkKa8uwiLJhCK5RnM2v9KKpJZ6WfSjpESOrg";
                string range = "Full";
                using (Stream stream = new FileStream(@jsonfile, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    credential = (ServiceAccountCredential)
                        GoogleCredential.FromStream(stream).UnderlyingCredential;

                    var initializer = new ServiceAccountCredential.Initializer(credential.Id)
                    {
                        User = serviceAccountEmail,
                        Key = credential.Key,
                        Scopes = Scopes
                    };
                    credential = new ServiceAccountCredential(initializer);
                }
                var service = new SheetsService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName,
                });
                SpreadsheetsResource.ValuesResource.GetRequest request =
                                        service.Spreadsheets.Values.Get(spreadsheetID, range);

                ValueRange response = request.Execute();
                IList<IList<Object>> values = response.Values;
                values.RemoveAt(0);
                List<SpotifyInfo> listSongs = new List<SpotifyInfo>();
                foreach (var item in values)
                {
                    SpotifyInfo song = new SpotifyInfo();
                    for (int i = 0; i < item.Count; i++)
                    {
                        if (i == 1)
                        {
                            song.TrackTitle = item[i].ToString();
                        }
                        else if (i == 2)
                        {
                            song.Code = item[i].ToString();
                        }
                        else if (i == 3)
                        {
                            song.Artists = item[i].ToString();
                        }
                    }
                    listSongs.Add(song);
                }
                return listSongs;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        public static List<SpotifyInfo> GetAllSongsFromStaticSheet()
        {
            try
            {
                ServiceAccountCredential credential1;
                string[] Scopes = { SheetsService.Scope.Spreadsheets };
                string serviceAccountEmail = "trackingnewdara@quickstart-1605058837166.iam.gserviceaccount.com";
                string jsonfile = "trackingNewData.json";
                string spreadsheetID = "1XsrVqD-Fz1ggj2VX6wEbpt_FO0qguTMJR5YWnytYXV4";
                string range = "All After Fix";
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
                var serices = new SheetsService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential1,
                    ApplicationName = ApplicationName,
                });
                SpreadsheetsResource.ValuesResource.GetRequest.ValueRenderOptionEnum valueRenderOption = (SpreadsheetsResource.ValuesResource.GetRequest.ValueRenderOptionEnum)0;
                SpreadsheetsResource.ValuesResource.GetRequest.DateTimeRenderOptionEnum dateTimeRenderOption = (SpreadsheetsResource.ValuesResource.GetRequest.DateTimeRenderOptionEnum)0;

                SpreadsheetsResource.ValuesResource.GetRequest request = serices.Spreadsheets.Values.Get(spreadsheetID, range);
                request.ValueRenderOption = valueRenderOption;
                request.DateTimeRenderOption = dateTimeRenderOption;

                // To execute asynchronously in an async method, replace `request.Execute()` as shown:
                Data.ValueRange response = request.Execute();
                IList<IList<Object>> values = response.Values;
                values.RemoveAt(0);
                List<SpotifyInfo> listSongs = new List<SpotifyInfo>();
                foreach (var item in values)
                {
                    if (item.Count != 0)
                    {
                        SpotifyInfo song = new SpotifyInfo();
                        for (int i = 0; i < item.Count; i++)
                        {
                            if (i == 1)
                            {
                                if (item[i] != null)
                                {
                                    song.TrackTitle = item[i].ToString();
                                }

                            }
                            else if (i == 2)
                            {
                                song.Code = item[i].ToString();
                            }
                            else if (i == 3)
                            {
                                if (item[i] != null)
                                {
                                    song.Artists = item[i].ToString();
                                }
                            }
                            else if (i == 4)
                            {
                                if (!string.IsNullOrEmpty(item[i].ToString()))
                                {
                                    song.LinkSpotify = item[i].ToString();
                                    song.TrackId = item[i].ToString().Split(new string[] { "=" }, StringSplitOptions.None)[1].Split(new string[] { ":" }, StringSplitOptions.None)[2];
                                    song.AlbumId = item[i].ToString().Split(new string[] { "?" }, StringSplitOptions.None)[0].Split(new string[] { "https://open.spotify.com/album/" }, StringSplitOptions.None)[1];

                                }
                            }
                            else if (i == 5)
                            {
                                if (item[i] != null)
                                {
                                    song.Genres = item[i].ToString();
                                }
                            }
                            else if (i == 6)
                            {
                                if (item[i] != null)
                                {
                                    song.Country = item[i].ToString();
                                }
                            }
                            else if (i == 7)
                            {
                                if (item[i] != null)
                                {
                                    song.ReleaseDate = item[i].ToString();
                                }
                            }
                            else if (i == 8)
                            {
                                if (item[i] != null)
                                {
                                    song.Popularity = item[i].ToString();
                                }
                            }
                            else if (i == 9)
                            {
                                if (item[i] != null)
                                {
                                    if (item[i].ToString() != "")
                                    {
                                        song.StreamCount = long.Parse(item[i].ToString());
                                    }

                                }
                            }
                        }
                        listSongs.Add(song);
                    }
                }
                return listSongs;
            }
            catch (Exception ex)
            {
                return null;
            }

        }
        public static List<SpotifyInfo> GetSongs()
        {
            try
            {
                ServiceAccountCredential credential1;
                string[] Scopes = { SheetsService.Scope.Spreadsheets };
                string serviceAccountEmail = "trackingnewdara@quickstart-1605058837166.iam.gserviceaccount.com";
                string jsonfile = "trackingNewData.json";
                string spreadsheetID = "1vWXHvrejiBe4wEzdqUoehGqgR8l4TOWM_zK2bQQdZ5k";
                string range = "All on server";
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
                var serices = new SheetsService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential1,
                    ApplicationName = ApplicationName,
                });
                SpreadsheetsResource.ValuesResource.GetRequest.ValueRenderOptionEnum valueRenderOption = (SpreadsheetsResource.ValuesResource.GetRequest.ValueRenderOptionEnum)0;
                SpreadsheetsResource.ValuesResource.GetRequest.DateTimeRenderOptionEnum dateTimeRenderOption = (SpreadsheetsResource.ValuesResource.GetRequest.DateTimeRenderOptionEnum)0;

                SpreadsheetsResource.ValuesResource.GetRequest request = serices.Spreadsheets.Values.Get(spreadsheetID, range);
                request.ValueRenderOption = valueRenderOption;
                request.DateTimeRenderOption = dateTimeRenderOption;

                // To execute asynchronously in an async method, replace `request.Execute()` as shown:
                Data.ValueRange response = request.Execute();
                IList<IList<Object>> values = response.Values;
                values.RemoveAt(0);
                List<SpotifyInfo> listSongs = new List<SpotifyInfo>();
                foreach (var item in values)
                {
                    if (item.Count != 0)
                    {
                        SpotifyInfo song = new SpotifyInfo();
                        for (int i = 0; i < item.Count; i++)
                        {
                            if (i == 1)
                            {
                                if (item[i] != null)
                                {
                                    song.TrackTitle = item[i].ToString();
                                }

                            }
                            else if (i == 2)
                            {
                                if (item[i] != null)
                                {
                                    song.Code = item[i].ToString();
                                }
                            }
                            else if (i == 3)
                            {
                                if (item[i] != null)
                                {
                                    song.Artists = item[i].ToString();
                                }
                            }
                            else if (i == 4)
                            {
                                if (item[4] != null)
                                {
                                    song.LinkSpotify = item[i].ToString();
                                }
                            }
                        }
                        listSongs.Add(song);
                    }
                }
                return listSongs;
            }
            catch (Exception ex)
            {
                return null;
            }

        }

    }
    public class SongInfo
    {
        public string Title { get; set; }
        public string Code { get; set; }
        public int? Duration { get; set; }
        public string Actor { get; set; }
        public string Url { get; set; }
        public string upload_file { get; set; }
        public string datecreated { get; set; }
    }
    public class WebResponse
    {
        public List<SongInfo> Data { get; set; }
    }
}
