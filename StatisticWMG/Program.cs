using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StatisticWMG.Model;
using System;
using System.Collections.Generic;
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
        static string spotifyAllResult = "ytb_all_result.txt";
        static string column = "K";
        static bool isFirstTimeRun = false;
        static void Main(string[] args)
        {
            if (isFirstTimeRun)
            {
                RunFirstTime();
                UpdateDataToGoogleSheet.InsertDataAll(spotifyAllResult);
            }
            else
            {
                RunSecondTime();
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
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        static void RunFirstTime()
        {
            List<SpotifyInfo> listSongsFirst = GetDataFromGoogleSheet.GetSongsFromWMGSourceGoogleSheet();
            var songSpotify = SpotifyService.GetArtistGenresAndRegion(listSongsFirst);
            DeleteFile(spotifyAllResult);
            var spGroupCount = (int)Math.Ceiling((double)songSpotify.Count / (double)SP_GROUP_SIZE);
            for (var i = 0; i < spGroupCount; i++)
            {
                var groupSongs = songSpotify.Skip(i * SP_GROUP_SIZE).Take(SP_GROUP_SIZE).ToList();
                Console.WriteLine($"Running {i}...");
                SpotifyService.GetSpotifyInfoAsync(groupSongs, spotifyAllResult).GetAwaiter().GetResult();
                //YoutubeServices.GetYoutubeInfoAsync(groupSongs, spotifyAllResult,proxy).GetAwaiter().GetResult();
                Console.WriteLine($"Done. Wating 5s...");
                Thread.Sleep(10 * 500);
            }
        }
        static void RunSecondTime()
        {
            List<SpotifyInfo> listSongsFromStatistic = GetDataFromGoogleSheet.GetAllSongsFromStaticSheet();
            List<SpotifyInfo> listSongsFromSource = GetDataFromGoogleSheet.GetSongsFromWMGSourceGoogleSheet();
            List<SpotifyInfo> listSongAfter= new List<SpotifyInfo>();
            foreach (var item in listSongsFromSource)
            {
                if (listSongsFromStatistic.Any(p => p.Code.ToLower().Equals(item.Code.ToLower())) == false)
                {
                    listSongAfter.Add(item);
                }
            }
            bool check = false;
            var listNew = new List<SpotifyInfo>();
            //kiểm tra xem có bài mới không, nếu có thì lấy dữ liệu về và chạy api để lấy genre, track id, album id, year, stream count
            if (listSongAfter.Count > 0)
            {
                var songSpotify = SpotifyService.GetArtistGenresAndRegion(listSongAfter);
                var groupCount = (int)Math.Ceiling((double)songSpotify.Count / (double)SP_GROUP_SIZE);
                for (var i = 0; i < groupCount; i++)
                {
                    var groupSongs = songSpotify.Skip(i * SP_GROUP_SIZE).Take(SP_GROUP_SIZE).ToList();
                    Console.WriteLine($"Running {i}...");
                    var list = SpotifyService.GetSpotifyInfoAsync(groupSongs, spotifyAllResult).GetAwaiter().GetResult();
                    listNew.AddRange(list);
                    Console.WriteLine($"Done. Wating 2s...");
                    Thread.Sleep(2000);
                }
                check = true;
            }
            if (check)
            {
                UpdateDataToGoogleSheet.AppendNewSongs(listNew, listSongsFromStatistic.Count);
            }

            var ytbGroupCount = (int)Math.Ceiling((double)listSongsFromStatistic.Count / (double)SP_GROUP_SIZE);
            var listAll = new List<SpotifyInfo>();
            for (var i = 0; i < ytbGroupCount; i++)
            {
                var groupSongs = listSongsFromStatistic.Skip(i * SP_GROUP_SIZE).Take(SP_GROUP_SIZE).ToList();
                Console.WriteLine($"Running {i}...");
                var list = SpotifyService.GetListCountTracksPlay(groupSongs).GetAwaiter().GetResult();
                listAll.AddRange(list);
                Console.WriteLine($"Done. Wating 2s...");
                Thread.Sleep(2000);
            }
            if (check)
            {
                listAll.AddRange(listNew);
            }
            UpdateDataToGoogleSheet.InserViewCountToNewColumn(listAll, column);
        }
    }
}
