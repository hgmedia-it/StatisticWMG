using Newtonsoft.Json;
using StatisticWMG.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
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
                        if(taskResult != null)
                        {
                            task.Key.YoutubeUrl = string.IsNullOrEmpty(taskResult.videoId) ? "" : $"https://www.youtube.com/watch?v={taskResult.videoId}";
                            task.Key.ReleaseYear = string.IsNullOrEmpty(taskResult.year) ? "" : taskResult.year;
                            task.Key.YoutubeViewCount = taskResult.viewCount;
                        }

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

        public static async Task<Item> CountYoutubeView(string trackName, string trackArtist)
        {
            try
            {

                WebClient client = new WebClient();
                var searchQuery = "";
                if (string.IsNullOrEmpty(trackArtist) || trackArtist.Trim().Equals("Đang cập nhật"))
                {
                    searchQuery = trackName;
                }
                else
                {
                    searchQuery = $"{trackArtist} - {trackName}";
                }               
                searchQuery = HttpUtility.UrlEncode(searchQuery);
                var url = $"https://www.youtube.com/results?search_query={searchQuery}";

                if (string.IsNullOrEmpty(trackArtist) || trackArtist.Trim().Equals("Đang cập nhật"))
                {
                    trackName = RemoveDiacritics(trackName.Trim());                  
                }
                else
                {
                    trackName = RemoveDiacritics(trackName.Trim());
                    trackArtist = RemoveDiacritics(trackArtist.Trim());
                }
                string htmlStr = await client.DownloadStringTaskAsync(url);
                //Item item = new Item();
                var initDataStr = htmlStr.Substring(htmlStr.IndexOf(@"{""responseContext"""));
                initDataStr = initDataStr.Substring(0, initDataStr.IndexOf("};") + 1);
                
                var initDataJson = JsonConvert.DeserializeObject<dynamic>(initDataStr);
                var videoJsons = initDataJson.contents.twoColumnSearchResultsRenderer.primaryContents.sectionListRenderer.contents[0]
                    .itemSectionRenderer.contents;

                //var videoId = "";
                //for (var i = 0; i < videoJsons.Count; i++)
                //{
                //    try
                //    {
                //        try
                //        {
                //            videoId = videoJsons[i].videoRenderer.videoId;
                //        }
                //        catch
                //        {
                //            continue;
                //        }

                //        string dateString = "";
                //        try
                //        {

                //            dateString = videoJsons[i].videoRenderer.publishedTimeText.simpleText;
                //            if (!string.IsNullOrEmpty(dateString))
                //            {
                //                byte[] bytes = Encoding.Default.GetBytes(dateString);
                //                dateString = Encoding.UTF8.GetString(bytes);

                //                if (dateString.Contains("tuần") || dateString.Contains("tháng") || dateString.Contains("ngày")
                //                    || dateString.Contains("day") || dateString.Contains("month") || dateString.Contains("week"))
                //                {
                //                    dateString = DateTime.Now.Year.ToString();
                //                }
                //                else
                //                {
                //                    string[] lines = dateString.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                //                    dateString = (int.Parse(DateTime.Now.Year.ToString()) - int.Parse(lines[0])).ToString();
                //                }
                //            }
                //        }
                //        catch
                //        {
                //            dateString = GetVideoDateTime(videoId,client).GetAwaiter().GetResult();
                //        }

                //        string viewCount = videoJsons[i].videoRenderer.viewCountText.simpleText;
                //        string[] lineView = viewCount.Split(new string[] { " " }, StringSplitOptions.None);
                //        long view = 0;
                //        try
                //        {
                //            view = long.Parse(lineView[0].Replace(".", ""));
                //        }
                //        catch
                //        {
                //            view = long.Parse(lineView[0].Replace(",", ""));
                //        }

                //        item.videoId = videoId;
                //        item.viewCount = view;
                //        item.year = dateString;
                //        break;

                //    }
                //    catch (Exception ex)
                //    {
                //        Console.WriteLine(ex.Message);
                //        return null;
                //    }
                //}
                // title must contains both track name AND track artist
                if(!string.IsNullOrEmpty(trackArtist) && !trackArtist.Trim().Equals("Đang cập nhật"))
                {
                    for (var i = 0; i < videoJsons.Count; i++)
                    {
                        try
                        {
                            var title = RemoveDiacritics((string)videoJsons[i].videoRenderer.title.runs[0].text);
                            if (title.ToLower().Contains(trackName.Trim().ToLower()) && title.ToLower().Contains(trackArtist.Trim().ToLower()))
                            {
                                string viewCountStr = videoJsons[i].videoRenderer.viewCountText.simpleText;
                                var videoId = (string)videoJsons[i].videoRenderer.videoId;
                                var viewCount = long.Parse(viewCountStr.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries)[0].Replace(",", ""));
                                var publishYear = "";
                                try
                                {
                                    publishYear = videoJsons[i].videoRenderer.publishedTimeText.simpleText;
                                    if (!string.IsNullOrEmpty(publishYear))
                                    {
                                        byte[] bytes = Encoding.Default.GetBytes(publishYear);
                                        publishYear = Encoding.UTF8.GetString(bytes);

                                        if (publishYear.Contains("tuần") || publishYear.Contains("tháng") || publishYear.Contains("ngày")
                                            || publishYear.Contains("day") || publishYear.Contains("month") || publishYear.Contains("week"))
                                        {
                                            publishYear = DateTime.Now.Year.ToString();
                                        }
                                        else
                                        {
                                            string[] lines = publishYear.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                                            publishYear = (int.Parse(DateTime.Now.Year.ToString()) - int.Parse(lines[0])).ToString();
                                        }
                                    }
                                }
                                catch
                                {
                                    publishYear = GetVideoDateTime(videoId, client).GetAwaiter().GetResult();
                                }
                                return new Item
                                {
                                    videoId = videoId,
                                    viewCount = viewCount,
                                    year = publishYear
                                };
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                }


                // title must contains track name
                for (var i = 0; i < videoJsons.Count; i++)
                {
                    try
                    {
                        var title = RemoveDiacritics((string)videoJsons[i].videoRenderer.title.runs[0].text);
                        if (title.ToLower().Contains(trackName.Trim().ToLower()))
                        {
                            string viewCountStr = videoJsons[i].videoRenderer.viewCountText.simpleText;
                            var videoId = videoJsons[i].videoRenderer.videoId;
                            //var viewCount = viewCountStr.Replace(",", "");
                            var viewCount = long.Parse(viewCountStr.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries)[0].Replace(",", ""));
                            var publishYear = "";
                            try
                            {
                                publishYear = videoJsons[i].videoRenderer.publishedTimeText.simpleText;
                                if (!string.IsNullOrEmpty(publishYear))
                                {
                                    byte[] bytes = Encoding.Default.GetBytes(publishYear);
                                    publishYear = Encoding.UTF8.GetString(bytes);

                                    if (publishYear.Contains("tuần") || publishYear.Contains("tháng") || publishYear.Contains("ngày")
                                        || publishYear.Contains("day") || publishYear.Contains("month") || publishYear.Contains("week"))
                                    {
                                        publishYear = DateTime.Now.Year.ToString();
                                    }
                                    else
                                    {
                                        string[] lines = publishYear.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                                        publishYear = (int.Parse(DateTime.Now.Year.ToString()) - int.Parse(lines[0])).ToString();
                                    }
                                }
                            }
                            catch
                            {
                                publishYear = GetVideoDateTime(videoId, client).GetAwaiter().GetResult();
                            }
                            return new Item
                            {
                                videoId = videoId,
                                viewCount = viewCount,
                                year = publishYear
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }

                // get first video
                for (var i = 0; i < videoJsons.Count; i++)
                {
                    try
                    {
                        string viewCountStr = videoJsons[i].videoRenderer.viewCountText.simpleText;
                        var videoId = videoJsons[i].videoRenderer.videoId;
                        var viewCount = long.Parse(viewCountStr.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries)[0].Replace(",", ""));
                        var publishYear = "";
                        try
                        {
                            publishYear = videoJsons[i].videoRenderer.publishedTimeText.simpleText;
                            if (!string.IsNullOrEmpty(publishYear))
                            {
                                byte[] bytes = Encoding.Default.GetBytes(publishYear);
                                publishYear = Encoding.UTF8.GetString(bytes);

                                if (publishYear.Contains("tuần") || publishYear.Contains("tháng") || publishYear.Contains("ngày")
                                    || publishYear.Contains("day") || publishYear.Contains("month") || publishYear.Contains("week"))
                                {
                                    publishYear = DateTime.Now.Year.ToString();
                                }
                                else
                                {
                                    string[] lines = publishYear.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                                    publishYear = (int.Parse(DateTime.Now.Year.ToString()) - int.Parse(lines[0])).ToString();
                                }
                            }
                        }
                        catch
                        {
                            publishYear = GetVideoDateTime(videoId, client).GetAwaiter().GetResult();
                        }
                        return new Item
                        {
                            videoId = videoId,
                            viewCount = viewCount,
                            year = publishYear
                        };
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
                return new Item
                {
                    videoId = "",
                    viewCount = 0,
                    year = ""
                };
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        private static async Task<string> GetVideoDateTime(string videoId,WebClient client)
        {
            try
            {
                var url = $"https://www.youtube.com/watch?v={videoId}";
                string htmlStr = await client.DownloadStringTaskAsync(url);
                var initDataStr = htmlStr.Substring(htmlStr.IndexOf(@"{""responseContext"""));
                initDataStr = initDataStr.Substring(0, initDataStr.IndexOf("};") + 1);
                var initDataJson = JsonConvert.DeserializeObject<dynamic>(initDataStr);
                string metaDataJsons = (string)initDataJson.contents.twoColumnWatchNextResults.results.results.contents[0].videoPrimaryInfoRenderer.dateText.simpleText;
                string[] lineDate = metaDataJsons.Split(new string[] { " " }, StringSplitOptions.None);
                string date = lineDate[lineDate.Length - 1];
                return date;
            }
            catch (Exception ex)
            {
                return "";
            }
        }
        private static string RemoveDiacritics(string text)
        {
            try
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
            catch
            {
                return text;
            }

        }
    }
    public class Item
    {
        public string videoId { get; set; }
        public string year { get; set; }
        public long viewCount { get; set; }
    }
}
