using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StatisticWMG
{
    class Program
    {
        static int YTB_GROUP_SIZE = 1000;
        static string youtubeAllResult = "ytb_all_result_first.txt";
        static string range = "Danh sách lọc 34 03/11";
        static string column = "J";
        static bool isFirstTimeRun = true;
        static void Main(string[] args)
        {
            if (isFirstTimeRun)
            {
                RunFirstTime();
                UpdateDataToGoogleSheet.InsertDataAll(youtubeAllResult);
            }
            else
            {
                List<Songs> listNewSongs = GetDataFromGoogleSheet.GetNewSongFromWMGSource(range);
                List<Songs> listSongsFirst = GetDataFromGoogleSheet.GetSongsFromGoogleSheet(); 
                List<Songs> listSongAfterGetYoutubeUrl = new List<Songs>();
                bool check = false;
                //kiểm tra xem có bài mới không, nếu có thì lấy dữ liệu về và chạy api để lấy genre, youtubeUrl, year, view
                if (listSongsFirst.Count < listNewSongs.Count)
                {
                    List<Songs> newSong = new List<Songs>();
                    for (int i = listSongsFirst.Count; i < listNewSongs.Count; i++)
                    {
                        newSong.Add(listNewSongs[i]);
                    }
                    var songSpotify = GetGenresFromArtist.GetArtistGenresAndRegion(newSong);                 
                    var groupCount = (int)Math.Ceiling((double)songSpotify.Count / (double)YTB_GROUP_SIZE);
                    for (var i = 0; i < groupCount; i++)
                    {
                        var groupSongs = songSpotify.Skip(i * YTB_GROUP_SIZE).Take(YTB_GROUP_SIZE).ToList();
                        Console.WriteLine($"Running {i}...");
                        var list = YoutubeServices.GetYoutubeInfoAsync(groupSongs, youtubeAllResult).GetAwaiter().GetResult();
                        listSongAfterGetYoutubeUrl.AddRange(list);
                        Console.WriteLine($"Done. Wating 10s...");
                        Thread.Sleep(10 * 1000);
                    }
                    check = true;
                }
                var songs = GetDataFromGoogleSheet.GetAllSongsFromStaticSheet();
                if (check)
                {
                    UpdateDataToGoogleSheet.AppendNewSongs(listSongAfterGetYoutubeUrl);
                }
                DeleteFile("ytb_all_result_second.txt");
                var ytbGroupCount = (int)Math.Ceiling((double)songs.Count / (double)YTB_GROUP_SIZE);
                var listAll = new List<Songs>();
                for (var i = 0; i < ytbGroupCount; i++)
                {
                    var groupSongs = songs.Skip(i * YTB_GROUP_SIZE).Take(YTB_GROUP_SIZE).ToList();
                    Console.WriteLine($"Running {i}...");
                    var list =  YoutubeServices.GetYoutubeInfoAsync(groupSongs, $"ytb_all_result_second.txt").GetAwaiter().GetResult();
                    listAll.AddRange(list);
                    Console.WriteLine($"Done. Wating 10s...");
                    Thread.Sleep(10 * 1000);
                }
                if (check)
                {
                    listAll.AddRange(listSongAfterGetYoutubeUrl);
                }
                UpdateDataToGoogleSheet.InserViewCountToNewColumn(listAll,column);
            }

        }
        static void DeleteFile(string filePath)
        {
            try
            {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }

            }
            catch(Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        static void RunFirstTime()
        {
            List<Songs> listSongsFirst = GetDataFromGoogleSheet.GetSongsFromGoogleSheet();
            var songSpotify = GetGenresFromArtist.GetArtistGenresAndRegion(listSongsFirst);
            DeleteFile(youtubeAllResult);
            var ytbGroupCount = (int)Math.Ceiling((double)songSpotify.Count / (double)YTB_GROUP_SIZE);
            for (var i = 0; i < ytbGroupCount; i++)
            {
                var groupSongs = songSpotify.Skip(i * YTB_GROUP_SIZE).Take(YTB_GROUP_SIZE).ToList();
                Console.WriteLine($"Running {i}...");
                YoutubeServices.GetYoutubeInfoAsync(groupSongs, youtubeAllResult).GetAwaiter().GetResult();
                Console.WriteLine($"Done. Wating 10s...");
                Thread.Sleep(10*500);
            }

            //var ytbGroupCount2 = (int)Math.Ceiling((double)listSongAfterGetYoutubeUrl.Count / (double)YTB_GROUP_SIZE);
            //for (var i = 0; i < ytbGroupCount2; i++)
            //{
            //    var groupSongs = listSongAfterGetYoutubeUrl.Skip(i * YTB_GROUP_SIZE).Take(YTB_GROUP_SIZE).ToList();
            //    Console.WriteLine($"Running {i}...");
            //    YoutubeServices.GetYoutubeReleaseYearAndView(groupSongs, youtubeAllResult).GetAwaiter().GetResult();
            //    Console.WriteLine($"Done. Wating 10s...");
            //    Thread.Sleep(10 * 500);
            //}
        }
    }
}
