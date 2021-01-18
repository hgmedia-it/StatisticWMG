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
        static string spotifyAllResult = "spotify_all_result.txt";
        static string column = "P";
        static bool isFirstTimeRun = false;
        static void Main(string[] args)
        {
            
            //Youtube();
            //PhanloaiGenre();
            //AddTrackToPlaylist();
            //GetInfoSpotifyFromCode();
            //if (isFirstTimeRun)
            //{
                //RunFirstTime();
            //    UpdateDataToGoogleSheet.InsertDataAll(spotifyAllResult);
            //}
            //else
            //{
            RunSecondTime();
            //}

        }

        static void RunFirstTime()
        {
            List<SpotifyInfo> listSongsFirst = GetDataFromGoogleSheet.GetAllSongsFromStaticSheet();//GetDataFromGoogleSheet.GetSongsFromWMGSourceGoogleSheet();
            var listSongAfter = new List<SpotifyInfo>();
            DeleteFile(spotifyAllResult);
            var spGroupCount = (int)Math.Ceiling((double)listSongsFirst.Count / (double)SP_GROUP_SIZE);
            for (var i = 0; i < spGroupCount; i++)
            {
                var groupSongs = listSongsFirst.Skip(i * SP_GROUP_SIZE).Take(SP_GROUP_SIZE).ToList();
                Console.WriteLine($"Running {i}...");
                var list = SpotifyService.GetSpotifyInfoAsync(groupSongs, spotifyAllResult).GetAwaiter().GetResult();
                listSongAfter.AddRange(list);
                Console.WriteLine($"Done. Wating 5s...");
                Thread.Sleep(10 * 500);
            }
            SpotifyService.GetArtistGenresAndRegion(listSongAfter);
        }
        static void RunSecondTime()
        {
            List<SpotifyInfo> listSongsFromStatistic = GetDataFromGoogleSheet.GetAllSongsFromStaticSheet();
            List<SpotifyInfo> listSongsFromSource = WMGSource.GetAllSongFromServer();
            List<SpotifyInfo> listSongAfter = new List<SpotifyInfo>();
            foreach (var item in listSongsFromSource)
            {
                if (string.IsNullOrEmpty(item.Code))
                {
                    if(listSongsFromStatistic.Any(p => p.TrackTitle.ToLower().Equals(item.TrackTitle.ToLower()) && p.Artists.ToLower().Equals(item.Artists.ToLower())) == false)
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
            // //kiểm tra xem có bài mới không, nếu có thì lấy dữ liệu về và chạy api để lấy genre, track id, album id, year, stream count
            if (listSongAfter.Count > 0)
            {
                var groupCount = (int)Math.Ceiling((double)listSongAfter.Count / (double)SP_GROUP_SIZE);
                for (var i = 0; i < groupCount; i++)
                {
                    var groupSongs = listSongAfter.Skip(i * SP_GROUP_SIZE).Take(SP_GROUP_SIZE).ToList();
                    Console.WriteLine($"Running {i}...");
                    var list = SpotifyService.GetSpotifyInfoAsync(groupSongs, spotifyAllResult).GetAwaiter().GetResult();
                    listNew.AddRange(list);
                    Console.WriteLine($"Done. Wating 2s...");
                    Thread.Sleep(5000);
                }
                var genres = SpotifyService.GetArtistGenresAndRegion(listNew);
                songAfterGetGenre.AddRange(genres);
                check = true;
            }
            if (check)
            {
                UpdateDataToGoogleSheet.AppendNewSongs(songAfterGetGenre, listSongsFromStatistic.Count);
            }

            var ytbGroupCount = (int)Math.Ceiling((double)listSongsFromStatistic.Count / (double)SP_GROUP_SIZE);
            var listAll = new List<SpotifyInfo>();
            for (var i = 0; i < ytbGroupCount; i++)
            {
                var groupSongs = listSongsFromStatistic.Skip(i * SP_GROUP_SIZE).Take(SP_GROUP_SIZE).ToList();
                Console.WriteLine($"Running {i}...");
                var list = SpotifyService.GetSpotifyInfoAsync(groupSongs, "Final_final.txt", false).GetAwaiter().GetResult();
                listAll.AddRange(list);
                Console.WriteLine($"Done. Wating 2s...");
                Thread.Sleep(5000);
            }
            if (check)
            {
                listAll.AddRange(songAfterGetGenre);
            }
            UpdateDataToGoogleSheet.InserViewCountToNewColumn(listAll, column);
            UpdateDataToGoogleSheet.InserViewCountToPopularityColumn(listAll, "I");
        }

        static void AddTrackToPlaylist()
        {
            List<SpotifyInfo> listsongs = GetDataFromGoogleSheet.GetSongs();
            List<string> trackList = new List<string>();
            foreach (var item in listsongs)
            {
                if (!string.IsNullOrEmpty(item.LinkSpotify))
                {
                    var song = item.LinkSpotify.Split(new string[] { "=" }, StringSplitOptions.None);
                    trackList.Add(song[1]);
                }
            }
            var group = (int)Math.Ceiling((double)trackList.Count / (double)100);
            for (var i = 0; i < group; i++)
            {
                var groupSongs = trackList.Skip(i * 100).Take(100).ToList();
                Console.WriteLine($"Running {i}...");
                var list = SpotifyService.AddTrack(groupSongs);
                Console.WriteLine($"Done. Wating 2s...");
                Thread.Sleep(10000);
            }
        }

        static void Youtube()
        {
            List<SpotifyInfo> listSongsFromStatistic = GetDataFromGoogleSheet.GetAllSongsFromStaticSheet();
            listSongsFromStatistic = listSongsFromStatistic.Skip(31732).ToList();
            var totalSongs = new List<Songs>();
            foreach (var item in listSongsFromStatistic)
            {
                var song = new Songs();
                song.TrackName = item.TrackTitle;
                song.TrackArtist = item.Artists;
                song.Code = item.Code;
                totalSongs.Add(song);
            }
            if (File.Exists("ytb_view_result.txt"))
            {
                File.Delete("ytb_view_result.txt");
            }
            var ytbGroupCount = (int)Math.Ceiling((double)totalSongs.Count / (double)YTB_GROUP_SIZE);
            for (var i = 0; i < ytbGroupCount; i++)
            {
                var groupSongs = totalSongs.Skip(i * YTB_GROUP_SIZE).Take(YTB_GROUP_SIZE).ToList();
                Console.WriteLine($"Running {i}...");
                var proxy = Proxy.RandomProxy();
                var client = Proxy.ChangeProxy(proxy);
                YoutubeServices.GetYoutubeInfoAsync(groupSongs, client, $"ytb_view_result.txt").GetAwaiter().GetResult();
                Console.WriteLine($"Done. Wating 10s...");
                Thread.Sleep(10 * 1000);
            }
        }
        static void ViralAritst()
        {
            List<SpotifyInfo> listSongsFromStatistic = GetDataFromGoogleSheet.GetAllSongsFromStaticSheet();
            List<string> listArtist = new List<string>();
            foreach(var item in listSongsFromStatistic)
            {
                if(!string.IsNullOrEmpty(item.Artists) || !item.Artists.ToLower().Equals("đang cập nhật"))
                {
                    if (!listArtist.Any(x => x.Contains(item.Artists)))
                    {
                        listArtist.Add(item.Artists);
                    }
                }
            }
            Dictionary<string, List<long>> dictionary = new Dictionary<string, List<long>>();
            Dictionary<string, string> dictionaryAfter = new Dictionary<string, string>();
            foreach(var item in listArtist)
            {
                var list = listSongsFromStatistic.Where(x => x.Artists.Equals(item)).ToList().Select(i => i.StreamCount).ToList();
                dictionary.Add(item, list);
            }
            foreach(var item in dictionary)
            {
                if(item.Value.Any(i => i >= 100000000))
                {
                    dictionaryAfter.Add(item.Key, "Có");
                }else if(item.Value.Any(i => i >= 100000000) == false && item.Value.Any(i=> i < 100000000 && i >= 10000000))
                {
                    dictionaryAfter.Add(item.Key, "Trung Bình");
                }
                else
                {
                    dictionaryAfter.Add(item.Key, "Không");
                }
            }
            foreach(var item in dictionaryAfter)
            {
                var list = listSongsFromStatistic.Where(x => x.Artists.Equals(item.Key)).ToList();
                if(list != null && list.Count > 0)
                {
                    foreach (var song in list)
                    {
                        song.ViralArtist = item.Value;
                    }
                }
            }

            var lines = new List<string>();
            foreach(var song in listSongsFromStatistic)
            {
                if (!string.IsNullOrEmpty(song.ViralArtist))
                {
                    lines.Add(song.ViralArtist);
                }
                else
                {
                    lines.Add("Không");
                }
            }
            if (File.Exists("Artist.txt"))
            {
                File.Delete("Artist.txt");
            }
            File.AppendAllLines("Artist.txt", lines);
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
    }
}
