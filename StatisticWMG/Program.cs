using ExcelDataReader;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StatisticWMG.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StatisticWMG
{
    class Program
    {
        static int SP_GROUP_SIZE = 50;
        static int YTB_GROUP_SIZE = 1000;
        static string spotifyNewSongs = "spotify_new_song.txt";
        static string spotifyAllSongs = "spotify_all_song.txt";
        static string ytbViewResult = "ytb_view_result.txt";
        static int numberOfOldSongs = 0;
        //column mới trong sheet để cập nhật stream count
        static string column = "S";
        static void Main(string[] args)
        {

            UpdateSpotifySongsToWMGStatistic(SouceTypes.GoogleSheet);
            //UpdateSpotifySongsToWMGStatistic(SouceTypes.Excel);
            //GetViewsOfSongFromYoutube_NhacViet();
            GetViewOfSongFromYoutube();

        }
        static void UpdateSpotifySongsToWMGStatistic(SouceTypes souceTypes)
        {
            List<SpotifyInfo> listSongAfter = new List<SpotifyInfo>();
            List<SpotifyInfo> listSongsFromStatistic = new List<SpotifyInfo>();
            bool check = false;
            if (souceTypes == SouceTypes.GoogleSheet)
            {
                listSongsFromStatistic = GetDataFromGoogleSheet.GetAllSongsFromStaticSheet();
                numberOfOldSongs = listSongsFromStatistic.Count();
                List<SpotifyInfo> listSongsFromSource = WMGSource.GetAllSongFromServer();
                foreach (var item in listSongsFromSource)
                {
                    if (string.IsNullOrEmpty(item.Code))
                    {
                        if (listSongsFromStatistic.Any(p => p.TrackTitle.ToLower().Equals(item.TrackTitle.ToLower()) && p.Artists.ToLower().Equals(item.Artists.ToLower())) == false)
                        {
                            listSongAfter.Add(item);
                        }
                    }
                    else
                    {
                        if (listSongsFromStatistic.Any(p => p.Code.ToLower().Equals(item.Code.ToLower())) == false || string.IsNullOrEmpty(item.Code))
                        {
                            listSongAfter.Add(item);
                        }
                    }

                }
            }
            else
            {
                Console.WriteLine("Reading data from file");
                string excutingPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string excelDirPath = Path.Combine(Path.GetDirectoryName(excutingPath), "ExcelFiles");

                var excelFiles = Directory.GetFiles(excelDirPath, "*.xlsx");
                //var fileName = @"C:\ExcelFile.xlsx";
                foreach(var file in excelFiles)
                {
                    using (var stream = File.Open(file, FileMode.Open, FileAccess.Read))
                    {
                        // Auto-detect format, supports:
                        //  - Binary Excel files (2.0-2003 format; *.xls)
                        //  - OpenXml Excel files (2007 format; *.xlsx, *.xlsb)
                        using (var reader = ExcelReaderFactory.CreateReader(stream))
                        {
                            // Choose one of either 1 or 2:

                            // 1. Use the reader methods
                            do
                            {
                                while (reader.Read())
                                {
                                    // reader.GetDouble(0);
                                }
                            } while (reader.NextResult());

                            // 2. Use the AsDataSet extension method
                            var result = reader.AsDataSet();
                            for (int i = 1; i < result.Tables[0].Rows.Count; i++)
                            {
                                var row = result.Tables[0].Rows[i];
                                SpotifyInfo spotifyInfo = new SpotifyInfo();
                                spotifyInfo.Code = row[2].ToString();
                                try
                                {
                                    spotifyInfo.TrackTitle = RemoveBadChars(row[3].ToString());
                                    spotifyInfo.Artists = RemoveBadChars(row[4].ToString());
                                }
                                catch (Exception ex)
                                {
                                    continue;
                                }
                                if (!string.IsNullOrEmpty(spotifyInfo.TrackTitle) && !string.IsNullOrEmpty(spotifyInfo.Artists))
                                {
                                    listSongAfter.Add(spotifyInfo);
                                }
                            }
                        }
                    }
                }
                Console.WriteLine($"Read {listSongAfter.Count} items done!");
            }
            var listNew = new List<SpotifyInfo>();
            var songAfterGetGenre = new List<SpotifyInfo>();
            //kiểm tra xem có bài mới không, nếu có thì lấy dữ liệu về và chạy api để lấy genre, track id, album id, year, stream count
            if (listSongAfter.Count > 0)
            {
                var groupCount = (int)Math.Ceiling((double)listSongAfter.Count / (double)SP_GROUP_SIZE);
                for (var i = 0; i < groupCount; i++)
                {
                    var groupSongs = listSongAfter.Skip(i * SP_GROUP_SIZE).Take(SP_GROUP_SIZE).ToList();
                    Console.WriteLine($"Running {i}...");
                    var list = SpotifyService.GetSpotifyInfoAsync(groupSongs, spotifyNewSongs).GetAwaiter().GetResult();
                    var listAfterGenres = SpotifyService.GetArtistGenresAndRegion(list);
                    listNew.AddRange(listAfterGenres);
                    Console.WriteLine($"Done {(i + 1)* SP_GROUP_SIZE} / {listSongAfter.Count}. Wating 2s...");
                    if(souceTypes == SouceTypes.Excel)
                    {
                        string excutingPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                        string excelDirPath = Path.Combine(Path.GetDirectoryName(excutingPath), "ExcelFiles");
                        // save to file
                        string outputFile = Path.Combine(excelDirPath, "result.csv");
                        //before your loop
                        var csv = new StringBuilder();

                        foreach(var song in listAfterGenres)
                        {
                            if(song.StreamCount < 500000)
                            {
                                continue;
                            }    
                            var newLine = string.Format("{0},{1},{2},{3},{4},{5},{6}", song.Code, song.TrackTitle, song.Artists, song.LinkSpotify, song.StreamCount.ToString(),
                                                                                       song.ReleaseDate, song.Genres);
                            csv.AppendLine(newLine);
                        }
                        File.AppendAllText(outputFile, csv.ToString());
                    }    
                    Thread.Sleep(5000);
                }
                //lấy thể loại của song trên spotify
                if (souceTypes == SouceTypes.GoogleSheet)
                {
                    var genres = SpotifyService.GetArtistGenresAndRegion(listNew);
                    songAfterGetGenre.AddRange(genres);
                }
                check = true;
            }




            // add bài mới vào sheet bao gồm các trường : tên, code, nhạc sỹ, genre, quốc gia
            if (check && souceTypes == SouceTypes.GoogleSheet) 
            {
                UpdateDataToGoogleSheet.AppendNewSongs(songAfterGetGenre,listSongsFromStatistic.Count);
            }

            //get stream count của cả bài cũ + mới
            var ytbGroupCount = (int)Math.Ceiling((double)listSongsFromStatistic.Count / (double)SP_GROUP_SIZE);
            var listAll = new List<SpotifyInfo>();
            for (var i = 0; i < ytbGroupCount; i++)
            {
                var groupSongs = listSongsFromStatistic.Skip(i * SP_GROUP_SIZE).Take(SP_GROUP_SIZE).ToList();
                Console.WriteLine($"Running {i}...");
                var list = SpotifyService.GetSpotifyInfoAsync(groupSongs, spotifyAllSongs, false).GetAwaiter().GetResult();
                listAll.AddRange(list);
                Console.WriteLine($"Done. Wating 2s...");
                Thread.Sleep(5000);
            }
            if (check)
            {
                listAll.AddRange(songAfterGetGenre);
            }
            // insert stream count và popularity của all songs
            UpdateDataToGoogleSheet.InserViewCountToNewColumn(listAll, column);
            UpdateDataToGoogleSheet.InserViewCountToPopularityColumn(listAll, "I");
        }
        static void GetViewOfSongFromYoutube()
        {
            List<SpotifyInfo> listSongsFromStatistic = GetDataFromGoogleSheet.GetAllSongsFromStaticSheet();

            //kiểm tra có bài mới không, nếu có thì bắt đầu lấy link youtube + view 
            if (numberOfOldSongs > 0 && numberOfOldSongs < listSongsFromStatistic.Count)
            {
                listSongsFromStatistic = listSongsFromStatistic.Skip(numberOfOldSongs).ToList();
            }
            var totalSongs = new List<Songs>();
            foreach (var item in listSongsFromStatistic)
            {
                var song = new Songs();
                song.TrackName = item.TrackTitle;
                song.TrackArtist = item.Artists;
                song.Code = item.Code;
                totalSongs.Add(song);
            }
            if (File.Exists(ytbViewResult))
            {
                File.Delete(ytbViewResult);
            }
            var ytbGroupCount = (int)Math.Ceiling((double)totalSongs.Count / (double)YTB_GROUP_SIZE);
            List<Songs> songs = new List<Songs>();
            for (var i = 0; i < ytbGroupCount; i++)
            {
                var groupSongs = totalSongs.Skip(i * YTB_GROUP_SIZE).Take(YTB_GROUP_SIZE).ToList();
                Console.WriteLine($"Running {i}...");
                var account = Proxy.RandomProxy();
                var client = Proxy.ChangeProxy(account);
                songs = YoutubeServices.GetYoutubeInfoAsync(groupSongs, client, ytbViewResult).GetAwaiter().GetResult();
                Console.WriteLine($"Done. Wating 10s...");
                Thread.Sleep(10 * 1000);
            }
            UpdateDataToGoogleSheet.InsertLinkYoutube(songs,numberOfOldSongs);
            UpdateDataToGoogleSheet.InsertViewYoutube(songs,numberOfOldSongs);
        }

        static void GetViewsOfSongFromYoutube_NhacViet()
        {
            List<SpotifyInfo> listSongsFromStatistic = GetDataFromGoogleSheet.GetAllSongsFromStaticSheetVN();
            var ytbGroupCount = (int)Math.Ceiling((double)listSongsFromStatistic.Count / (double)YTB_GROUP_SIZE);
            
            for (var i = 0; i < ytbGroupCount; i++)
            {
                List<SpotifyInfo> songs = new List<SpotifyInfo>();
                var groupSongs = listSongsFromStatistic.Skip(i * YTB_GROUP_SIZE).Take(YTB_GROUP_SIZE).ToList();
                Console.WriteLine($"Running {i}/{ytbGroupCount}...");
                var account = Proxy.RandomProxy();
                var client = Proxy.ChangeProxy(account);
                songs = YoutubeServices.GetYoutubeInfoAsyncVN(groupSongs, client).GetAwaiter().GetResult();
                string rawrange = songs.FirstOrDefault().Range;
                string range = rawrange.Remove(rawrange.IndexOf("J") + 1);
                range = "Danh sách nhạc tổng!" + range;
                UpdateDataToGoogleSheet.InsertYoutubeVN(songs, range);
                Console.WriteLine($"Done. Wating 10s...");
                Thread.Sleep(10 * 1000);
            }
        }

        public static string RemoveBadChars(string word)
        {
            char[] chars = new char[word.Length];
            int myindex = 0;
            for (int i = 0; i < word.Length; i++)
            {
                char c = word[i];

                if ((int)c >= 65 && (int)c <= 90)
                {
                    chars[myindex] = c;
                    myindex++;
                }
                else if ((int)c >= 97 && (int)c <= 122)
                {
                    chars[myindex] = c;
                    myindex++;
                }
                else if ((int)c >= 48 && (int)c <= 57)
                {
                    chars[myindex] = c;
                    myindex++;
                }
                else if ((int)c == 32)
                {
                    chars[myindex] = c;
                    myindex++;
                }
            }

            word = new string(chars);

            return word;
        }
    }

    public enum SouceTypes
    {
        GoogleSheet = 0,
        Excel = 1
    }
}
