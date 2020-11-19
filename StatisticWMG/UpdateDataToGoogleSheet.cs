using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StatisticWMG
{
    public static class UpdateDataToGoogleSheet
    {
        static string[] Scopes = { SheetsService.Scope.Spreadsheets };
        static string ApplicationName = "Statistic WMG";
        static string spreadsheetId = "1XsrVqD-Fz1ggj2VX6wEbpt_FO0qguTMJR5YWnytYXV4";
        static string range = "Sheet1";
        public static void InsertDataAll(string filePath)
        {
            try
            {
                var service = UserCredential(); 
                IList<Songs> dataList = GetDataFromFile(filePath);
                List<Google.Apis.Sheets.v4.Data.ValueRange> data = new List<Google.Apis.Sheets.v4.Data.ValueRange>();
                ValueRange valueDataRange = new ValueRange() { MajorDimension = "ROWS" };
                valueDataRange.Values = new List<IList<object>>();
                valueDataRange.Values.Add(new List<object> { "STT" ,"TRACK NAME", "CODE", "TRACK ARTIST", "GENRE", "REGION", "LINK YOUTUBE", "YEAR", DateTime.Now.ToString("MM/dd/yyyy")});
                valueDataRange.Range = range;
                for (int i = 0; i< dataList.Count; i++)
                {
                    IList<object> list = new List<object> { i+1 , dataList[i].TrackName, dataList[i].Code, dataList[i].TrackArtist,dataList[i].Genres, 
                        dataList[i].Region,dataList[i].YoutubeUrl, dataList[i].ReleaseYear, dataList[i].YoutubeViewCount };
                    valueDataRange.Values.Add(list);

                }
                data.Add(valueDataRange);
                Google.Apis.Sheets.v4.Data.BatchUpdateValuesRequest requestBody = new Google.Apis.Sheets.v4.Data.BatchUpdateValuesRequest();
                requestBody.ValueInputOption = "USER_ENTERED";
                requestBody.Data = data;
                // API to update data to sheet
                SpreadsheetsResource.ValuesResource.BatchUpdateRequest request = service.Spreadsheets.Values.BatchUpdate(requestBody, spreadsheetId);
                Google.Apis.Sheets.v4.Data.BatchUpdateValuesResponse response = request.Execute();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        public static void InserViewCountToNewColumn(List<Songs> songs,string columnName)
        {
            try
            {
                var service = UserCredential();
                IList<Songs> dataList = songs;
                List<Google.Apis.Sheets.v4.Data.ValueRange> data = new List<Google.Apis.Sheets.v4.Data.ValueRange>();
                ValueRange valueDataRange = new ValueRange() { MajorDimension = "ROWS" };
                valueDataRange.Values = new List<IList<object>>() { };
                valueDataRange.Values.Add(new List<object> { DateTime.Now.ToString("MM/dd/yyyy") });
                int max = dataList.Count + 1;
                valueDataRange.Range = range + "!" + columnName +"1:" + columnName + max.ToString();
                for (int i = 0; i < dataList.Count; i++)
                {
                    IList<object> list = new List<object> { dataList[i].YoutubeViewCount };
                    valueDataRange.Values.Add(list);
                }
                data.Add(valueDataRange);
                Google.Apis.Sheets.v4.Data.BatchUpdateValuesRequest requestBody = new Google.Apis.Sheets.v4.Data.BatchUpdateValuesRequest();
                requestBody.ValueInputOption = "USER_ENTERED";
                requestBody.Data = data;
                // API to update data to sheet
                SpreadsheetsResource.ValuesResource.BatchUpdateRequest request = service.Spreadsheets.Values.BatchUpdate(requestBody, spreadsheetId);
                Google.Apis.Sheets.v4.Data.BatchUpdateValuesResponse response = request.Execute();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
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
                    ApplicationName = ApplicationName,
                });
                return service;
                //UserCredential credential;

                //using (var stream =
                //    new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
                //{
                //    // The file token.json stores the user's access and refresh tokens, and is created
                //    // automatically when the authorization flow completes for the first time.
                //    string credPath = "token.json";
                //    credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                //        GoogleClientSecrets.Load(stream).Secrets,
                //        Scopes,
                //        "user",
                //        CancellationToken.None,
                //        new FileDataStore(credPath, true)).Result;
                //    Console.WriteLine("Credential file saved to: " + credPath);
                //}
                //var service = new SheetsService(new BaseClientService.Initializer()
                //{
                //    HttpClientInitializer = credential,
                //    ApplicationName = ApplicationName,
                //});
                //return service;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;

            }
        }
        private static List<Songs> GetDataFromFile(string filePath)
        {
            var songs = new List<Songs>();
            var lines = File.ReadAllLines(filePath);
            foreach (var line in lines)
            {
                var parts = line.Split(new string[] { "\t" }, StringSplitOptions.None);
                songs.Add(new Songs
                {
                    TrackName = parts[0],
                    Code = parts[1],
                    TrackArtist = parts[2],
                    Genres = parts[3],
                    Region = parts[4],
                    YoutubeUrl = parts[5],
                    ReleaseYear = parts[6],
                    YoutubeViewCount = long.Parse(parts[7])
                });
            }

            return songs;
        }
        public static void AppendNewSongs(List<Songs> listSongs)
        {
            try
            {
                var service = UserCredential();
                IList<Songs> dataList = listSongs;
                List<Google.Apis.Sheets.v4.Data.ValueRange> data = new List<Google.Apis.Sheets.v4.Data.ValueRange>();
                ValueRange valueDataRange = new ValueRange() { MajorDimension = "ROWS" };
                valueDataRange.Values = new List<IList<object>>();
                valueDataRange.Range = range;
                for (int i = 0; i < dataList.Count; i++)
                {
                    IList<object> list = new List<object> { i+1 , dataList[i].TrackName, dataList[i].Code, dataList[i].TrackArtist,dataList[i].Genres,
                        dataList[i].Region,dataList[i].YoutubeUrl, dataList[i].ReleaseYear };
                    valueDataRange.Values.Add(list);

                }
                data.Add(valueDataRange);

                // How the input data should be interpreted.
                SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum valueInputOption = (SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum)1;  // TODO: Update placeholder value.

                // How the input data should be inserted.
                SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum insertDataOption = (SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum)1;  // TODO: Update placeholder value.

                // TODO: Assign values to desired properties of `requestBody`:
                Google.Apis.Sheets.v4.Data.ValueRange requestBody = data[0];

                SpreadsheetsResource.ValuesResource.AppendRequest request = service.Spreadsheets.Values.Append(requestBody, spreadsheetId, range);
                request.ValueInputOption = valueInputOption;
                request.InsertDataOption = insertDataOption;

                // To execute asynchronously in an async method, replace `request.Execute()` as shown:
                Google.Apis.Sheets.v4.Data.AppendValuesResponse response = request.Execute();
            }
            catch(Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
