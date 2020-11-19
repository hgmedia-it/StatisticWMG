using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace StatisticWMG
{
    public static class YoutubeServices
    {
        public static async Task<List<Songs>> GetYoutubeInfoAsync(List<Songs> songs,string fileResultName)
        {
            var tasks = new Dictionary<Songs, Task<Item>>();
            foreach (var song in songs)
            {
                try
                {
                    tasks.Add(song, CountYoutubeView(song.TrackName, song.TrackArtist));
                }
                catch (Exception ex)
                {
                    // do nothing
                }
            }

            await Task.WhenAll(tasks.Select(t => t.Value));

            foreach (var task in tasks)
            {
                if (task.Value.IsCompleted)
                {
                    try
                    {
                        var taskResult = task.Value.GetAwaiter().GetResult();
                        task.Key.YoutubeUrl = string.IsNullOrEmpty(taskResult.videoId) ? "" : $"https://www.youtube.com/watch?v={taskResult.videoId}";
                        task.Key.ReleaseYear = string.IsNullOrEmpty(taskResult.year) ? "" : taskResult.year;
                        task.Key.YoutubeViewCount = taskResult.viewCount;
                    }
                    catch (Exception ex)
                    {
                        // do nothing
                    }
                }
                else
                {
                    // do nothing
                }
            }

            var lines = new List<string>();
            foreach (var song in songs)
            {
                lines.Add(song.TrackName + "\t"
                    + song.Code + "\t"
                    + song.TrackArtist + "\t"
                    + song.Genres + "\t"
                    + song.Region + "\t"
                    + song.YoutubeUrl + "\t"
                    + song.ReleaseYear + "\t"
                    + song.YoutubeViewCount + "\t");
            }
            File.AppendAllLines(fileResultName, lines);
            return songs;
        }

        public static async Task GetYoutubeReleaseYearAndView(List<Songs> songs, string fileResultName)
        {
            var tasks = new Dictionary<Songs, Task<KeyValuePair<string, long>>>();
            foreach (var song in songs)
            {
                try
                {
                    string[] line = song.YoutubeUrl.Split(new string[] { "https://www.youtube.com/watch?v=" }, StringSplitOptions.RemoveEmptyEntries);
                    tasks.Add(song, GetVideoDateTimeAndView(line[0]));
                }
                catch (Exception ex)
                {
                    // do nothing
                }
            }

            await Task.WhenAll(tasks.Select(t => t.Value));

            foreach (var task in tasks)
            {
                if (task.Value.IsCompleted)
                {
                    try
                    {
                        var taskResult = task.Value.GetAwaiter().GetResult();
                        task.Key.ReleaseYear = string.IsNullOrEmpty(taskResult.Key) ? "" : taskResult.Key;
                        task.Key.YoutubeViewCount = taskResult.Value;
                    }
                    catch (Exception ex)
                    {
                        // do nothing
                    }
                }
                else
                {
                    // do nothing
                }
            }

            var lines = new List<string>();
            foreach (var song in songs)
            {
                lines.Add(song.TrackName + "\t"
                    + song.Code + "\t"
                    + song.TrackArtist + "\t"
                    + song.Genres + "\t"
                    + song.Region + "\t"
                    + song.YoutubeUrl + "\t"
                    + song.ReleaseYear + "\t"
                    +song.YoutubeViewCount);
            }
            File.AppendAllLines(fileResultName, lines);
        }
        public static async Task<Item> CountYoutubeView(string trackName, string trackArtist)
        {
            try
            {
                //trackName = RemoveDiacritics(trackName);
                //trackArtist = RemoveDiacritics(trackArtist);

                var searchQuery = $"{trackArtist} - {trackName}";
                searchQuery = HttpUtility.UrlEncode(searchQuery);
                var url = $"https://www.youtube.com/results?search_query={searchQuery}";
                WebClient client = new WebClient();
                string htmlStr = await client.DownloadStringTaskAsync(url);
                Item item = new Item();
                var initDataStr = htmlStr.Substring(htmlStr.IndexOf(@"{""responseContext"""));
                initDataStr = initDataStr.Substring(0, initDataStr.IndexOf("};") + 1);
                
                var initDataJson = JsonConvert.DeserializeObject<dynamic>(initDataStr);
                var videoJsons = initDataJson.contents.twoColumnSearchResultsRenderer.primaryContents.sectionListRenderer.contents[0]
                    .itemSectionRenderer.contents;

                var videoId = "";
                for (var i = 0; i < videoJsons.Count; i++)
                {
                    try
                    {
                        videoId = videoJsons[i].videoRenderer.videoId;
                        string dateString = videoJsons[i].videoRenderer.publishedTimeText.simpleText;
                        byte[] bytes = Encoding.Default.GetBytes(dateString);
                        dateString = Encoding.UTF8.GetString(bytes);
                        string viewCount = videoJsons[i].videoRenderer.viewCountText.simpleText;
                        if (dateString.Contains("tuần") || dateString.Contains("tháng") || dateString.Contains("ngày")
                            || dateString.Contains("day") || dateString.Contains("month") || dateString.Contains("week"))
                        {
                            dateString = DateTime.Now.Year.ToString();
                        }
                        else
                        {
                            string[] lines = dateString.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                            dateString = (int.Parse(DateTime.Now.Year.ToString()) - int.Parse(lines[0])).ToString();
                        }
                        string[] lineView = viewCount.Split(new string[] { " " }, StringSplitOptions.None);
                        long view = long.Parse(lineView[0].Replace(".", ""));
                        item.videoId = videoId;
                        item.viewCount = view;
                        item.year = dateString;
                        break;

                    }
                    catch (Exception ex)
                    {
                    }
                }
                return item;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        public static async Task<KeyValuePair<string, long>> GetVideoDateTimeAndView(string videoId)
        {
            try
            {
                var url = $"https://www.youtube.com/watch?v={videoId}";
                WebClient client = new WebClient();
                string htmlStr = await client.DownloadStringTaskAsync(url);
                var initDataStr = htmlStr.Substring(htmlStr.IndexOf(@"{""responseContext"""));
                initDataStr = initDataStr.Substring(0, initDataStr.IndexOf("};") + 1);
                var initDataJson = JsonConvert.DeserializeObject<dynamic>(initDataStr);
                string metaDataJsons = (string)initDataJson.contents.twoColumnWatchNextResults.results.results.contents[0].videoPrimaryInfoRenderer.dateText.simpleText;
                string viewCount = (string)initDataJson.contents.twoColumnWatchNextResults.results.results.contents[0].videoPrimaryInfoRenderer.viewCount.videoViewCountRenderer.viewCount.simpleText;
                string[] lineDate = metaDataJsons.Split(new string[] { " " }, StringSplitOptions.None);
                string[] lineView = viewCount.Split(new string[] { " " }, StringSplitOptions.None);
                string date = lineDate[lineDate.Length - 1];
                long view = long.Parse(lineView[0].Replace(".",""));
                return new KeyValuePair<string, long>(date, view);
            }
            catch (Exception ex)
            {
                return default;
            }
        }
        private static string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }
        private static long GetLastNumberFromString(string str)
        {
            var numStr = "";
            var findLastDigit = false;
            for (var i = str.Length - 1; i >= 0; i--)
            {
                if (Char.IsDigit(str[i]))
                {
                    findLastDigit = true;
                    numStr += str[i];
                }
                else
                {
                    if (findLastDigit)
                    {
                        break;
                    }
                }
            }

            numStr = string.Join("", numStr.Reverse<char>());
            return long.Parse(numStr);
        }
    }
    public class Item
    {
        public string videoId { get; set; }
        public string year { get; set; }
        public long viewCount { get; set; }
    }
}
