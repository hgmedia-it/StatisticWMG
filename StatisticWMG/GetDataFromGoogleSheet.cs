using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using Newtonsoft.Json;
using StatisticWMG.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
                string spreadsheetID = "1XyT7DAldGPxWohxTzA8-mpPVp52uNWkBHpwB5UOG4rg";
                string range = "Sheet1";
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
                string range = "Sheet1";
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
                                song.TrackTitle = item[i].ToString();
                            }
                            else if (i == 2)
                            {
                                song.TrackId = item[i].ToString();
                            }
                            else if (i == 3)
                            {
                                song.AlbumId = item[i].ToString();
                            }
                            else if (i == 4)
                            {
                                song.Code = item[i].ToString();
                            }
                            else if (i == 5)
                            {
                                song.Artists = item[i].ToString();
                            }
                            else if (i == 6)
                            {
                                song.Genres = item[i].ToString();
                            }
                            else if (i == 7)
                            {
                                song.Country = item[i].ToString();
                            }
                            else if (i == 8)
                            {
                                song.ReleaseDate = item[i].ToString();
                            }
                            else if (i == 9)
                            {
                                song.StreamCount = long.Parse(item[i].ToString());
                            }
                        }
                        listSongs.Add(song);
                    }
                }
                return listSongs;
            }
            catch
            {
                return null;
            }

        }
    }
}
