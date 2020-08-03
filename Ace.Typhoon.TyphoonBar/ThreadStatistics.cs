using Ace.Typhoon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Ace.Typhoon.Typhoonbar
{
    public struct ThreadStatistics
    {
        private string ocean;
        private int[] replies;
        private Dictionary<DateTime, int> repliesByHour;
        private string title;
        private string lz;
        private int totalReply;
        private DateTime time;

        public ThreadStatistics(int year, int month, string threadTitle)
        {
            if (!threadTitle.EndsWith(".txt"))
            {
                threadTitle += ".txt";
            }
            threadTitle = string.Format("{0:00}{1:00}\\{2}", year % 100, month, threadTitle);
            FileStream fs = new FileStream(threadTitle, FileMode.Open, FileAccess.Read);
            StreamReader sr = new StreamReader(fs);
            Regex line = new Regex(@"(?<time>.+?)\t(?<reply>\d+)");
            Match match;
            DateTime dt;
            List<int> replyList = new List<int>();
            int reply = -1, tmp = 0;
            ocean = title = lz = "";
            totalReply = 0;
            time = new DateTime();
            replies = null;
            repliesByHour = new Dictionary<DateTime, int>();
            SpecificReply = 0;
            GetTitle(sr.ReadLine());
            Lz = new Regex("lz：(?<lz>.+)").Match(sr.ReadLine()).Groups["lz"].Value;
            if (long.TryParse(Lz, out long t))
            {
                Lz = '\'' + Lz;
            }
            sr.ReadLine();
            sr.ReadLine();
            TotalReply = int.Parse(new Regex(@"Total:\s(?<total>\d+)").Match(sr.ReadLine()).Groups["total"].Value) - 1;
            sr.ReadLine();
            do
            {
                match = line.Match(sr.ReadLine());
                dt = DateTime.Parse(match.Groups["time"].Value);
                if (!match.Success)
                {
                    break;
                }
                if (reply == -1)
                {
                    Time = dt;
                    month = dt.Month;
                }
                else if (dt.Month != month && dt.Hour != 0)
                {
                    replyList.Add(reply);
                    reply = 0;
                    month = dt.Month;
                }
                reply += (tmp = int.Parse(match.Groups["reply"].Value));
                repliesByHour.Add(dt, tmp);
            } while (!sr.EndOfStream);
            replyList.Add(reply);
            Replies = replyList.ToArray();
        }

        public string Title { get => title; set => title = value; }
        public string Lz { get => lz; set => lz = value; }
        public string Ocean { get => ocean; set => ocean = value; }
        public int TotalReply { get => totalReply; set => totalReply = value; }
        public DateTime Time { get => time; set => time = value; }
        public int[] Replies { get => replies; set => replies = value; }
        public int SpecificReply { get; set; }
        public Dictionary<DateTime, int> RepliesByHour { get => repliesByHour; set => repliesByHour = value; }

        private void GetTitle(string rawTitle)
        {
            try
            {
                rawTitle = rawTitle.Trim();
                if (rawTitle.StartsWith("【讨论扰动】"))
                {
                    TCNumber number = new TCNumber(new Regex(@"\d{2}\w").Match(rawTitle).Value);
                    string m, d;
                    Ocean = number.ShortArea;
                    Match match = new Regex(@"\d{2}\.(?<month>\d{2}?)\.(?<day>\d{2}?)").Match(rawTitle);
                    m = match.Groups["month"].Value;
                    d = match.Groups["day"].Value;
                    Title = number + "-" + m + d;
                }
                else if (rawTitle.StartsWith("【讨论台风】"))
                {
                    bool flag = false;
                    Title = "";
                    Ocean = "WP";
                    for (int i = 0; i < rawTitle.Length; i++)
                    {
                        if (rawTitle[i] == '(' || rawTitle[i] == '（')
                        {
                            flag = true;
                        }
                        else if (rawTitle[i] == ')' || rawTitle[i] == '）')
                        {
                            break;
                        }
                        else if (flag)
                        {
                            Title += rawTitle[i];
                        }
                    }
                }
                else
                {
                    int index;
                    TCNumber number = new TCNumber();
                    if (rawTitle.Contains("西北太平洋"))
                    {
                        number.District = 'W';
                    }
                    else if (rawTitle.Contains("中太平洋") || rawTitle.Contains("中北太平洋"))
                    {
                        number.District = 'C';
                    }
                    else if (rawTitle.Contains("东太平洋"))
                    {
                        number.District = 'E';
                    }
                    else if (rawTitle.Contains("北印度洋"))
                    {
                        number.District = 'B';
                    }
                    else if (rawTitle.Contains("北大西洋"))
                    {
                        number.District = 'L';
                    }
                    else if (rawTitle.Contains("南印度洋"))
                    {
                        number.District = 'S';
                    }
                    else if (rawTitle.Contains("南太平洋"))
                    {
                        number.District = 'P';
                    }
                    else if (rawTitle.Contains("南大西洋"))
                    {
                        number.District = 'Q';
                    }
                    else
                    {
                        throw new Exception();
                    }
                    Ocean = number.ShortArea;
                    if ((index = rawTitle.IndexOf('-')) >= 0)
                    {
                        Title = rawTitle.Remove(0, index + 1);
                        
                    }
                    else
                    {
                        number.Number = int.Parse(rawTitle.Substring(rawTitle.Length - 6, 2));
                        Title = number.ToString();
                    }
                }
                string tmp = "";
                for (int i = 0; i < Title.Length; i++)
                {
                    if (i == 0)
                    {
                        if (char.IsDigit(Title[i]))
                        {
                            break;
                        }
                        tmp += char.ToUpper(Title[i]);
                    }
                    else
                    {
                        tmp += char.ToLower(Title[i]);
                    }
                }
                if (tmp != "")
                {
                    Title = tmp;
                }
            }
            catch (Exception)
            {
                Title = "XXX";
                Ocean = "XX";
            }
        }

        public static int DefaultComparison(ThreadStatistics a, ThreadStatistics b)
        {
            if (a.Time < b.Time)
            {
                return -1;
            }
            else if (a.Time > b.Time)
            {
                return 1;
            }
            else
            {
                return string.Compare(a.Title, b.Title);
            }
        }
    }
}
