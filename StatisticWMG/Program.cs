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
        static int YTB_GROUP_SIZE = 1000;
        static string spotifyNewSongs = "spotify_new_song.txt";
        static string spotifyAllSongs = "spotify_all_song.txt";
        static string ytbViewResult = "ytb_view_result.txt";
        static int numberOfOldSongs = 0;
        //column mới trong sheet để cập nhật stream count
        static string column = "S";
        static void Main(string[] args)
        {

            UpdateSpotifySongsToWMGStatistic();
            GetViewOfSongFromYoutube();

        }
        static void UpdateSpotifySongsToWMGStatistic()
        {
            List<SpotifyInfo> listSongsFromStatistic = GetDataFromGoogleSheet.GetAllSongsFromStaticSheet();
            numberOfOldSongs = listSongsFromStatistic.Count();
            List<SpotifyInfo> listSongsFromSource = WMGSource.GetAllSongFromServer();
            List<SpotifyInfo> listSongAfter = new List<SpotifyInfo>();
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
            bool check = false;
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
                    listNew.AddRange(list);
                    Console.WriteLine($"Done. Wating 2s...");
                    Thread.Sleep(5000);
                }
                //lấy thể loại của song trên spotify
                var genres = SpotifyService.GetArtistGenresAndRegion(listNew);
                songAfterGetGenre.AddRange(genres);
                check = true;
            }

            // add bài mới vào sheet bao gồm các trường : tên, code, nhạc sỹ, genre, quốc gia
            if (check)
            {
                UpdateDataToGoogleSheet.AppendNewSongs(songAfterGetGenre,listSongsFromStatistic.Count);
            }

            ////get stream count của cả bài cũ + mới
            //var ytbGroupCount = (int)Math.Ceiling((double)listSongsFromStatistic.Count / (double)SP_GROUP_SIZE);
            //var listAll = new List<SpotifyInfo>();
            //for (var i = 0; i < ytbGroupCount; i++)
            //{
            //    var groupSongs = listSongsFromStatistic.Skip(i * SP_GROUP_SIZE).Take(SP_GROUP_SIZE).ToList();
            //    Console.WriteLine($"Running {i}...");
            //    var list = SpotifyService.GetSpotifyInfoAsync(groupSongs, spotifyAllSongs, false).GetAwaiter().GetResult();
            //    listAll.AddRange(list);
            //    Console.WriteLine($"Done. Wating 2s...");
            //    Thread.Sleep(5000);
            //}
            //if (check)
            //{
            //    listAll.AddRange(songAfterGetGenre);
            //}
            //// insert stream count và popularity của all songs
            //UpdateDataToGoogleSheet.InserViewCountToNewColumn(listAll, column);
            //UpdateDataToGoogleSheet.InserViewCountToPopularityColumn(listAll, "I");
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
    }
}
