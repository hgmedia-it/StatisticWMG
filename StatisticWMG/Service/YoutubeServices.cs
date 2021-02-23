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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace StatisticWMG
{
    public static class YoutubeServices
    {
        public static async Task<List<Songs>> GetYoutubeInfoAsync(List<Songs> songs,WebClient client,string fileResultName)
        {
            var tasks = new Dictionary<Songs, Task<Item>>();
            foreach (var song in songs)
            {
                try
                {
                    tasks.Add(song, CountYoutubeView(song.TrackName, song.TrackArtist,client));
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
                    + song.TrackArtist + "\t"
                    + song.YoutubeUrl + "\t"
                    + song.YoutubeViewCount + "\t");
            }
            File.AppendAllLines(fileResultName, lines);
            return songs;
        }

        public static async Task<Item> CountYoutubeView(string trackName, string trackArtist,WebClient client)
        {
            try
            {
                if(client == null)
                {
                    client = new WebClient();
                }
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
                var url = $"https://www.youtube.com/results?search_query={searchQuery}&sp=CAMSAhAB";

                if (string.IsNullOrEmpty(trackArtist) || trackArtist.Trim().Equals("Đang cập nhật"))
                {
                    trackName = RemoveDiacritics(trackName.Trim());                  
                }
                else
                {
                    trackName = RemoveDiacritics(trackName.Trim());
                    trackArtist = RemoveDiacritics(trackArtist.Trim());
                }
                var data = client.DownloadData(url);
                //var data = client.GetAsync(url).GetAwaiter().GetResult();
                //string message = await data.Content.ReadAsStringAsync();
                //string parsedString = Regex.Unescape(message);
                //byte[] isoBites = Encoding.GetEncoding("ISO-8859-1").GetBytes(parsedString);
                //var htmlStr =  Encoding.UTF8.GetString(isoBites, 0, isoBites.Length);
                var htmlStr = Encoding.UTF8.GetString(data);
                //Item item = new Item();
                var initDataStr = htmlStr.Substring(htmlStr.IndexOf(@"{""responseContext"""));
                initDataStr = initDataStr.Substring(0, initDataStr.IndexOf("};") + 1);
                
                var initDataJson = JsonConvert.DeserializeObject<dynamic>(initDataStr);
                var videoJsons = initDataJson.contents.twoColumnSearchResultsRenderer.primaryContents.sectionListRenderer.contents[0]
                    .itemSectionRenderer.contents;
                bool check = true;
                if(videoJsons.Count == 1)
                {
                    try
                    {
                        for (var i = 0; i < videoJsons.Count; i++)
                        {
                            var title1 = RemoveDiacritics((string)videoJsons[i].videoRenderer.title.runs[0].text);
                            check = true;
                            break;
                        }
                    }
                    catch
                    {
                        check = false;
                    }
                }
                if(check == false)
                {
                    return new Item
                    {
                        videoId = "",
                        viewCount = 0
                    };
                }
                if (!string.IsNullOrEmpty(trackArtist) && !trackArtist.Trim().Equals("Đang cập nhật"))
                {
                    for (var i = 0; i < videoJsons.Count; i++)
                    {
                        try
                        {
                            var title = "";
                            try
                            {
                                title = RemoveDiacritics((string)videoJsons[i].videoRenderer.title.runs[0].text);
                            }
                            catch
                            {
                                continue;
                            }

                            if ((title.ToLower().Contains(trackName.Trim().ToLower()) || trackName.Trim().ToLower().Contains(title)) && (title.ToLower().Contains(trackArtist.Trim().ToLower()) || trackArtist.Trim().ToLower().Contains(title)))
                            {
                                string viewCountStr = videoJsons[i].videoRenderer.viewCountText.simpleText;
                                var videoId = (string)videoJsons[i].videoRenderer.videoId;
                                var viewCount = viewCountStr.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries)[0];
                                if (viewCount.ToString().Contains(","))
                                {
                                    viewCount = viewCount.ToString().Replace(",", "");
                                }else if (viewCount.ToString().Contains("."))
                                {
                                    viewCount = viewCount.ToString().Replace(".", "");
                                }
                                return new Item
                                {
                                    videoId = videoId,
                                    viewCount = long.Parse(viewCount)
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
                        var title = "";
                        try
                        {
                            title = RemoveDiacritics((string)videoJsons[i].videoRenderer.title.runs[0].text);
                        }
                        catch
                        {
                            continue;
                        }
                        if (title.ToLower().Contains(trackName.Trim().ToLower()) || trackName.Trim().ToLower().Contains(title))
                        {
                            string viewCountStr = videoJsons[i].videoRenderer.viewCountText.simpleText;
                            var videoId = videoJsons[i].videoRenderer.videoId;                    
                            var viewCount = viewCountStr.Trim().Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries)[0];
                            if (viewCount.ToString().Contains(","))
                            {
                                viewCount = viewCount.ToString().Replace(",", "");
                            }
                            return new Item
                            {
                                videoId = videoId,
                                viewCount = long.Parse(viewCount)
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
                        var title = "";
                        try
                        {
                            title = RemoveDiacritics((string)videoJsons[i].videoRenderer.title.runs[0].text);
                        }
                        catch
                        {
                            continue;
                        }
                        string viewCountStr = videoJsons[i].videoRenderer.viewCountText.simpleText;
                        var videoId = videoJsons[i].videoRenderer.videoId;
                        var viewCount = viewCountStr.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries)[0];
                        if (viewCount.ToString().Contains(","))
                        {
                            viewCount = viewCount.ToString().Replace(",", "");
                        }
                        return new Item
                        {
                            videoId = videoId,
                            viewCount = long.Parse(viewCount)
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
                    viewCount = 0
                };
            }
            catch (Exception ex)
            {
                return null;
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
        public long viewCount { get; set; }
    }

}
