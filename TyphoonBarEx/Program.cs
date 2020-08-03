using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

namespace TyphoonBarEx
{
    class Program
    {
        static List<DateTime> times = new List<DateTime>();
        static SortedDictionary<DateTime, long> dict = new SortedDictionary<DateTime, long>();
        static string post = @"https://tieba.baidu.com/p/{0}?pn={1}&see_lz=0";
        static string lzljson = @"https://tieba.baidu.com/p/totalComment?t={0}&tid={1}&fid=22107&pn={2}&see_lz=0";
        static string lzl = @"https://tieba.baidu.com/p/comment?tid={0}&pid={1}&pn={2}";
        static DateTime peak3, peak6;
        static uint total = 0;
        static readonly DateTime date3 = new DateTime(2002, 1, 3, 0, 0, 3);
        static readonly DateTime date6 = new DateTime(2002, 1, 3, 0, 0, 6);
        static readonly TimeSpan span3 = new TimeSpan(3, 0, 0/*0, 1, 0*/);

        static void Main(string[] args)
        {
            //GetLzls(5125403421ul, 3);
            //foreach (var t in times)
            //{
            //    Console.WriteLine(t);
            //}
            string fileName;
            peak3 = peak6 = new DateTime();
            if (args.Length >= 1)
            {
                fileName = args[0];
            }
            else
            {
                fileName = "input.txt";
            }
            FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            StreamReader sr = new StreamReader(fs);
            Regex format = new Regex(@"(?<pid>\d+?)\s+(?<time>\d{4}-\d{1,2}-\d{1,2}\s*\d{1,2}:\d{1,2})");
            Match match;
            string name, lz;
            ulong pid;
            uint pages;
            DateTime startTime, endTime;
            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;
            while (!sr.EndOfStream)
            {
                match = format.Match(sr.ReadLine());
                if (match.Success)
                {
                    pid = ulong.Parse(match.Groups["pid"].Value);
                    endTime = DateTime.Parse(match.Groups["time"].Value);
                    times.Clear();
                    if ((pages = GetPosts(pid, out startTime, out name, out lz)) != 0 && GetLzls(pid, pages))
                    {
                        Process(startTime, endTime);
                        Output(name, lz);
                    }
                }
            }
            sr.Close();
            fs.Close();
        }

        static uint GetPosts(ulong pid, out DateTime startTime, out string title, out string lz)
        {
            uint totalPage = 999;
            string uri, text;
            DateTime temp;
            WebResponse response;
            Match match;
            MatchCollection matches;
            Regex dateReg = new Regex("<span class=\"tail-info\">(?<time>\\d{4}-\\d{2}-\\d{2}\\s+\\d{2}:\\d{2})</span>");
            bool first = true;
            startTime = new DateTime();
            title = lz = "";
            try
            {
                using (FileStream fs = new FileStream("post.txt", FileMode.Append, FileAccess.Write))
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    for (uint page = 1; page <= totalPage; page++)
                    {
                        uri = string.Format(post, pid, page);
                        response = WebRequest.Create(uri).GetResponse();
                        using (Stream stream = response.GetResponseStream())
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            text = reader.ReadToEnd();
                            sw.Write(text);
                            if (page == 1)
                            {
                                match = new Regex("回复贴，共<span class=\"red\">(?<num>\\d+)</span>").Match(text);
                                if (!match.Success)
                                {
                                    throw new Exception("Unknown error.");
                                } // if
                                totalPage = uint.Parse(match.Groups["num"].Value);
                                //Console.WriteLine(totalPage);
                                match = new Regex("<h3 class=\"core_title_.+?>(?<title>.+?)</h3>").Match(text);
                                if (!match.Success)
                                {
                                    throw new Exception("Unknown error.");
                                } // if
                                title = match.Groups["title"].Value.Replace(':', '：');
                                match = new Regex("<div class=\"louzhubiaoshi.+?author=\"(?<name>.+?)\"").Match(text);
                                if (!match.Success)
                                {
                                    throw new Exception("Unknown error.");
                                } // if
                                lz = match.Groups["name"].Value;
                            } // if
                            matches = dateReg.Matches(text);
                            foreach (Match m in matches)
                            {
                                temp = DateTime.Parse(m.Groups["time"].Value);
                                times.Add(temp = temp.AddHours(-8));
                                if (first)
                                {
                                    startTime = temp.Add(new TimeSpan(-temp.Hour % 3, -temp.Minute, -temp.Second));
                                    // startTime = temp;
                                    first = false;
                                }
                            } // foreach m
                            Console.WriteLine("主题帖 {0} 的第 {1} 页的直接跟帖已统计完毕。", title, page);
                        } // using stream, streamReader
                    } // for page
                } // using fs, sw
                return totalPage;
            } // try
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 0;
            }
        }

        static bool GetLzls(ulong tid, uint totalPage)
        {
            try
            {
                using (FileStream fs = new FileStream("post.txt", FileMode.Append, FileAccess.Write))
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    string uri, json, text;
                    ulong time_t = Time();
                    MatchCollection matches;
                    Regex regex = new Regex("<span class=\"lzl_time\">(?<time>.+?)</span>");
                    WebResponse response;
                    for (uint page = 1; page <= totalPage; page++)
                    {
                        uri = string.Format(lzljson, time_t, tid, page);
                        response = WebRequest.Create(uri).GetResponse();
                        using (Stream stream = response.GetResponseStream())
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            json = reader.ReadToEnd();
                            JObject jo = (JObject)JsonConvert.DeserializeObject(json);
                            if (int.Parse(jo["errno"].ToString()) == 0)
                            {
                                JToken toeken = jo["data"]["comment_list"];
                                if (toeken is JObject)
                                {
                                    JObject pids = (JObject)toeken;
                                    foreach (var _pid in pids)
                                    {
                                        ulong pid = ulong.Parse(_pid.Key);
                                        uint lzlPages = uint.Parse(_pid.Value["comment_num"].ToString());
                                        if (lzlPages % 10 == 0)
                                        {
                                            lzlPages /= 10;
                                        }
                                        else
                                        {
                                            lzlPages = lzlPages / 10 + 1;
                                        }
                                        for (uint p = 1; p <= lzlPages; p++)
                                        {
                                            uri = string.Format(lzl, tid, pid, p);
                                            response = WebRequest.Create(uri).GetResponse();
                                            using (Stream Stream = response.GetResponseStream())
                                            using (StreamReader Reader = new StreamReader(Stream))
                                            {
                                                matches = regex.Matches(text = Reader.ReadToEnd());
                                                foreach (Match match in matches)
                                                {
                                                    times.Add(DateTime.Parse(match.Groups["time"].Value).AddHours(-8));
                                                }
                                                // sw.WriteLine(text);
                                            }
                                        }
                                    }
                                }
                            }
                            Console.WriteLine("第 {0} 页的楼中楼已统计完毕。", page);
                            Thread.Sleep(13000);
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        static void Process(DateTime startTime, DateTime endTime)
        {
            DateTime key;
            long max3, max6;
            dict.Clear();
            dict.Add(startTime, 1);
            dict.Add(startTime + span3, -1);
            max3 = max6 = 0;
            peak3 = peak6 = startTime;
            total = 0;
            foreach (var time in times)
            {
                if (time.Hour % 3 != 0 || time.Minute != 0 || time.Second != 0)
                {
                    key = time.Add(new TimeSpan(3 - time.Hour % 3, -time.Minute, -time.Second));
                }
                else
                {
                    key = time;
                }
                if (key >= startTime && key <= endTime)
                {
                    if (!dict.ContainsKey(key))
                    {
                        dict.Add(key, 1);
                    }
                    else
                    {
                        dict[key]++;
                    }
                    total++;
                }
            }
            for (DateTime time = startTime; time <= endTime; time += span3)
            {
                if (!dict.ContainsKey(time))
                {
                    dict[time] = 0;
                }
                else
                {
                    if (dict[time] > max3)
                    {
                        max3 = dict[time];
                        peak3 = time;
                    }
                    if (time > startTime && dict[time] + dict[time - span3] > max6)
                    {
                        max6 = dict[time] + dict[time - span3];
                        peak6 = time;
                    }
                }
            }
        }

        static void Output(string title, string lz)
        {
            string dir;
            FileStream fs;
            StreamWriter sw;
            dict.Remove(date3);
            dict.Remove(date6);
            dir = string.Format("{0:00}{1:00}", peak3.Year % 100, peak3.Month);
            Directory.CreateDirectory(dir);
            fs = new FileStream(dir + "\\" + title + ".txt", FileMode.Create, FileAccess.Write);
            sw = new StreamWriter(fs);
            sw.WriteLine(title);
            sw.WriteLine("lz：{0}", lz);
            sw.WriteLine("Peak at {0} (3-h) = {1}", peak3, dict[peak3]);
            sw.WriteLine("Peak at {0} (6-h) = {1}", peak6, dict[peak6] + dict[peak6 - span3]);
            sw.WriteLine("Total: {0}", total);
            sw.WriteLine();
            foreach (var pair in dict)
            {
                sw.WriteLine("{0}\t{1}", pair.Key, pair.Value);
            }
            sw.Close();
            fs.Close();
        }

        static ulong Time()
        {
            return Time(DateTime.UtcNow);
        }

        static ulong Time(DateTime time)
        {
            return Convert.ToUInt64((time - new DateTime(1970, 1, 1)).TotalSeconds);
        }

        static DateTime Time(ulong time_t)
        {
            return new DateTime(1970, 1, 1, 8, 0, 0) + TimeSpan.FromSeconds(time_t);
        }
    }
}
