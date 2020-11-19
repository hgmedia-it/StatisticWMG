using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using Newtonsoft.Json;
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
        static string[] Scopes = { SheetsService.Scope.SpreadsheetsReadonly };
        static string ApplicationName = "";
        static string spreadsheetId = "1XyT7DAldGPxWohxTzA8-mpPVp52uNWkBHpwB5UOG4rg";
        static string range = "Sheet1";
        public static List<Songs> GetSongsFromGoogleSheet()
        {
            try
            {
                UserCredential credential;
                using (var stream =
                    new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
                {
                    // The file token.json stores the user's access and refresh tokens, and is created
                    // automatically when the authorization flow completes for the first time.
                    string credPath = "token.json";
                    credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.Load(stream).Secrets,
                        Scopes,
                        "user",
                        CancellationToken.None,
                        new FileDataStore(credPath, true)).Result;
                    Console.WriteLine("Credential file saved to: " + credPath);
                }
                var service = new SheetsService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName,
                });
                SpreadsheetsResource.ValuesResource.GetRequest request =
                                        service.Spreadsheets.Values.Get(spreadsheetId, range);

                ValueRange response = request.Execute();
                IList<IList<Object>> values = response.Values;
                values.RemoveAt(0);
                List<Songs> listSongs = new List<Songs>();
                foreach (var item in values)
                {
                    Songs song = new Songs();
                    for (int i = 0; i < item.Count; i++)
                    {
                        if (i == 0)
                        {
                            song.TrackName = item[i].ToString();
                        }
                        else if (i == 1)
                        {
                            song.Code = item[i].ToString();
                        }
                        else if (i == 2)
                        {
                            song.TrackArtist = item[i].ToString();
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
        public static List<Songs> GetNewSongFromWMGSource(string range)
        {
            try
            {
                ServiceAccountCredential credential1;
                string[] Scopes = { SheetsService.Scope.Spreadsheets };
                string serviceAccountEmail = "trackingnewdara@quickstart-1605058837166.iam.gserviceaccount.com";
                string jsonfile = "trackingNewData.json";
                string spreadsheetID = "1_agBSwQRV0dh-lvawCjsKe8ISM4jATkWo-I5InVrq3U";
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
                // The A1 notation of the values to retrieve.

                // How values should be represented in the output.
                // The default render option is ValueRenderOption.FORMATTED_VALUE.
                SpreadsheetsResource.ValuesResource.GetRequest.ValueRenderOptionEnum valueRenderOption = (SpreadsheetsResource.ValuesResource.GetRequest.ValueRenderOptionEnum)0;  // TODO: Update placeholder value.

                // How dates, times, and durations should be represented in the output.
                // This is ignored if value_render_option is
                // FORMATTED_VALUE.
                // The default dateTime render option is [DateTimeRenderOption.SERIAL_NUMBER].
                SpreadsheetsResource.ValuesResource.GetRequest.DateTimeRenderOptionEnum dateTimeRenderOption = (SpreadsheetsResource.ValuesResource.GetRequest.DateTimeRenderOptionEnum)0;  // TODO: Update placeholder value.

                SpreadsheetsResource.ValuesResource.GetRequest request = serices.Spreadsheets.Values.Get(spreadsheetID, range);
                request.ValueRenderOption = valueRenderOption;
                request.DateTimeRenderOption = dateTimeRenderOption;

                // To execute asynchronously in an async method, replace `request.Execute()` as shown:
                Data.ValueRange response = request.Execute();
                IList<IList<Object>> values = response.Values;
                values.RemoveAt(0);
                List<Songs> listSongs = new List<Songs>();
                foreach (var item in values)
                {
                    if(item.Count != 0)
                    {
                        Songs song = new Songs();
                        for (int i = 0; i < item.Count; i++)
                        {
                            if (i == 1)
                            {
                                song.TrackName = item[i].ToString();
                            }
                            else if (i == 2)
                            {
                                song.Code = item[i].ToString();
                            }
                            else if (i == 3)
                            {
                                song.TrackArtist = item[i].ToString();
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
        public static List<Songs> GetAllSongsFromStaticSheet()
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
                List<Songs> listSongs = new List<Songs>();
                foreach (var item in values)
                {
                    if (item.Count != 0)
                    {
                        Songs song = new Songs();
                        for (int i = 0; i < item.Count; i++)
                        {
                            if (i == 1)
                            {
                                song.TrackName = item[i].ToString();
                            }
                            else if (i == 2)
                            {
                                song.Code = item[i].ToString();
                            }
                            else if (i == 3)
                            {
                                song.TrackArtist = item[i].ToString();
                            }
                            else if (i == 4)
                            {
                                song.Genres = item[i].ToString();
                            }
                            else if (i == 5)
                            {
                                song.Region = item[i].ToString();
                            }
                            else if (i == 6)
                            {
                                song.YoutubeUrl = item[i].ToString();
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
