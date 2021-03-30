using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using StatisticWMG.Model;
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
        static string range = "All";

        public static void InserViewCountToNewColumn(List<SpotifyInfo> songs,string columnName)
        {
            try
            {
                var service = UserCredential();
                IList<SpotifyInfo> dataList = songs;
                List<Google.Apis.Sheets.v4.Data.ValueRange> data = new List<Google.Apis.Sheets.v4.Data.ValueRange>();
                ValueRange valueDataRange = new ValueRange() { MajorDimension = "ROWS" };
                valueDataRange.Values = new List<IList<object>>() { };
                valueDataRange.Values.Add(new List<object> { DateTime.Now.ToString("MM/dd/yyyy") });
                int max = dataList.Count + 1;
                valueDataRange.Range = range + "!" + columnName +"1:" + columnName + max.ToString();
                for (int i = 0; i < dataList.Count; i++)
                {
                    IList<object> list = new List<object> { dataList[i].StreamCount };
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
        public static void InsertLinkYoutube(List<Songs> songs, int numberOfOldSongs)
        {
            try
            {
                var service = UserCredential();
                IList<Songs> dataList = songs;
                List<Google.Apis.Sheets.v4.Data.ValueRange> data = new List<Google.Apis.Sheets.v4.Data.ValueRange>();
                ValueRange valueDataRange = new ValueRange() { MajorDimension = "ROWS" };
                valueDataRange.Values = new List<IList<object>>() { };
                int max = numberOfOldSongs + songs.Count + 1;
                valueDataRange.Range = range + "!K" + (numberOfOldSongs + 2).ToString() + ":" + "K" + max.ToString();
                for (int i = 0; i < dataList.Count; i++)
                {
                    IList<object> list = new List<object> { dataList[i].YoutubeUrl };
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

        public static void InsertYoutubeVN(List<SpotifyInfo> songs, string range)
        {
            try
            {
                var service = UserCredential();
                IList<IList<object>> newValues = new List<IList<object>>();

                foreach (var song in songs)
                {
                    newValues.Add(new List<object> {
                        song.YoutubeLink,
                        song.ViewYoutube
                    });
                }
                ValueRange newValueRage = new ValueRange();
                newValueRage.Values = newValues;
                newValueRage.Range = range;

                SpreadsheetsResource.ValuesResource.AppendRequest wrequest = service.Spreadsheets.Values.Append(newValueRage, "1k0G4J_HXLzOvaOvoUPHt8m7S-ogMxaeF53SE6ZfgXfo", range);
                wrequest.InsertDataOption = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.OVERWRITE;
                wrequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
                var wresponse = wrequest.Execute();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        public static void InsertViewYoutube(List<Songs> songs, int numberOfOldSongs)
        {
            try
            {
                var service = UserCredential();
                IList<Songs> dataList = songs;
                List<Google.Apis.Sheets.v4.Data.ValueRange> data = new List<Google.Apis.Sheets.v4.Data.ValueRange>();
                ValueRange valueDataRange = new ValueRange() { MajorDimension = "ROWS" };
                valueDataRange.Values = new List<IList<object>>() { };
                int max = numberOfOldSongs + songs.Count + 1;
                valueDataRange.Range = range + "!L" + (numberOfOldSongs + 2).ToString() + ":" + "L" + max.ToString();
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
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;

            }
        }
        public static void AppendNewSongs(List<SpotifyInfo> listSongs, int countRows)
        {
            try
            {
                var service = UserCredential();
                IList<SpotifyInfo> dataList = listSongs;
                List<Google.Apis.Sheets.v4.Data.ValueRange> data = new List<Google.Apis.Sheets.v4.Data.ValueRange>();
                ValueRange valueDataRange = new ValueRange() { MajorDimension = "ROWS" };
                valueDataRange.Values = new List<IList<object>>();
                valueDataRange.Range = range;
                for (int i = 0; i < dataList.Count; i++)
                {
                    IList<object> list = new List<object> { countRows + (i+1) , dataList[i].TrackTitle,dataList[i].Code,
                        dataList[i].Artists,dataList[i].LinkSpotify,dataList[i].Genres, dataList[i].Country, dataList[i].ReleaseDate, dataList[i].Popularity, dataList[i].StreamCount
                    };
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

        public static void InserViewCountToPopularityColumn(List<SpotifyInfo> songs, string columnName)
        {
            try
            {
                var service = UserCredential();
                IList<SpotifyInfo> dataList = songs;
                List<Google.Apis.Sheets.v4.Data.ValueRange> data = new List<Google.Apis.Sheets.v4.Data.ValueRange>();
                ValueRange valueDataRange = new ValueRange() { MajorDimension = "ROWS" };
                valueDataRange.Values = new List<IList<object>>() { };
                valueDataRange.Values.Add(new List<object> { DateTime.Now.ToString("POPULARITY") });
                int max = dataList.Count + 1;
                valueDataRange.Range = range + "!" + columnName + "1:" + columnName + max.ToString();
                for (int i = 0; i < dataList.Count; i++)
                {
                    IList<object> list = new List<object> { dataList[i].Popularity };
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
    }
}
