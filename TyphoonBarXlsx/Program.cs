using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Excel = Microsoft.Office.Interop.Excel;
using Ace.Typhoon.Typhoonbar;

namespace TyphoonBarXlsx
{
    class Program
    {
        static readonly string[] months =
            new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" },
            columns1 = new string[] { "lz", "OC", "Reply", "D", "T" },
            columns2 = new string[] { "lz", "OC", "Reply", "M", "D", "T" };
        static List<ThreadStatistics>[] data = new List<ThreadStatistics>[12];
        static Dictionary<DateTime, int> cols = new Dictionary<DateTime, int>();
        static int workYear;

        static void Main(string[] args)
        {
            workYear = args.Length > 0 ? int.Parse(args[0]) : 2016;
            string filename = workYear.ToString() + "TyphoonBar.xlsx";
            DateTime tmp = new DateTime(workYear, 1, 1);
            for (int i = 0; i < 12; i++)
            {
                data[i] = new List<ThreadStatistics>();
            }
            for (int i = 1; i <= 12; i++)
            {
                GetData(workYear, i);
            }
            GetData(workYear - 1, 12);
            GetData(workYear + 1, 1);
            for (int i = 0; i < 12; i++)
            {
                data[i].Sort(ThreadStatistics.DefaultComparison);
            }
            Console.Write("Processing xlsx");
            Excel.Application excel = new Excel.Application();
            Excel.Workbook wb = null;
            excel.Visible = false;
            excel.DisplayAlerts = false;
            wb = excel.Workbooks.Add(Excel.XlWBATemplate.xlWBATWorksheet);
            Excel.Worksheet month = wb.Worksheets[1];
            Excel.Worksheet year = wb.Worksheets.Add();
            Excel.Worksheet hour = wb.Worksheets.Add();
            Excel.Worksheet day = wb.Worksheets.Add();
            year.Cells[1, 1] = workYear.ToString();
            for (int i = 0; i < 6; i++)
            {
                year.Cells[1, i + 2] = columns2[i];
            }
            for (int i = 1; tmp <= new DateTime(workYear + 1, 1, 1); tmp += new TimeSpan(3, 0, 0), i++)
            {
                hour.Cells[i, 1] = tmp.ToString();
                cols.Add(tmp, i);
            }
            for (int i = 0, row1 = 1, row2 = 2; i < 12; i++)
            {
                Console.Write('.');
                month.Cells[row1, 1] = months[i];
                for (int j = 0; j < 5; j++)
                {
                    month.Cells[row1, j + 2] = columns1[j];
                }
                ++row1;
                for (int j = 0; j < data[i].Count; j++, row1++)
                {
                    month.Cells[row1, 1] = data[i][j].Title;
                    month.Cells[row1, 2] = data[i][j].Lz;
                    month.Cells[row1, 3] = data[i][j].Ocean;
                    month.Cells[row1, 4] = data[i][j].SpecificReply.ToString();
                    if (data[i][j].Time.Year != workYear)
                    {
                        month.Cells[row1, 5] = "0";
                        month.Cells[row1, 6] = "0";
                    }
                    else
                    {
                        month.Cells[row1, 5] = data[i][j].Time.Day.ToString();
                        month.Cells[row1, 6] = data[i][j].Time.Hour.ToString();
                    }
                    if (i == 0 || data[i][j].Time.Year == workYear)
                    {
                        year.Cells[row2, 1] = data[i][j].Title;
                        year.Cells[row2, 2] = data[i][j].Lz;
                        year.Cells[row2, 3] = data[i][j].Ocean;
                        if ((i == 0 && data[i][j].Time.Year != workYear) || i == 11)
                        {
                            year.Cells[row2, 4] = data[i][j].SpecificReply.ToString();
                        }
                        else
                        {
                            year.Cells[row2, 4] = data[i][j].TotalReply.ToString();
                        }
                        if (data[i][j].Time.Year != workYear)
                        {
                            year.Cells[row2, 5] = "0";
                            year.Cells[row2, 6] = "0";
                            year.Cells[row2, 7] = "0";
                        }
                        else
                        {
                            year.Cells[row2, 5] = data[i][j].Time.Month.ToString();
                            year.Cells[row2, 6] = data[i][j].Time.Day.ToString();
                            year.Cells[row2, 7] = data[i][j].Time.Hour.ToString();
                        }
                        foreach (var pair in data[i][j].RepliesByHour)
                        {
                            if (pair.Key <= new DateTime(workYear + 1, 1, 1) && pair.Key > new DateTime(workYear, 1, 1))
                            {
                                hour.Cells[cols[pair.Key], (row2 - 2) % 250 + 2] = pair.Value;
                            }
                        }
                        row2++;
                    }
                }
                row1++;
            }
            Console.Write(".");
            tmp = new DateTime(workYear, 1, 1);
            day.Cells[1, 1] = "Day";
            day.Cells[1, 2] = "Total";
            day.Cells[1, 3] = "Inc";
            for (int i = 2; tmp <= new DateTime(workYear + 1, 1, 1); tmp += new TimeSpan(24, 0, 0), i++)
            {
                day.Cells[i, 1] = tmp.ToLongDateString();
                day.Cells[i, 2] = string.Format("=SUM(Sheet3!B2:Sheet3!IV{0})", i * 8 - 7);
                day.Cells[i, 3] = i == 2 ? "=B2" : string.Format("=B{0}-B{1}", i, i - 1);
            }
            if (File.Exists(filename))
            {
                File.Delete(filename);
            }
            filename = Directory.GetCurrentDirectory() + "\\" + filename;
            wb.SaveAs(filename, 56);
            wb.Close();
        }

        static void GetData(int year, int month)
        {
            string path = string.Format("{0:00}{1:00}", year % 100, month);
            DirectoryInfo di = new DirectoryInfo(path);
            Console.Write("Processing {0}", path);
            if (di.Exists)
            {
                FileInfo[] tcs = di.GetFiles();
                ThreadStatistics ts, tmp;
                DateTime dt;
                foreach (var tc in tcs)
                {
                    Console.Write('.');
                    tmp = ts = new ThreadStatistics(year, month, tc.Name);
                    for (int i = 0; i < ts.Replies.Length; i++)
                    {
                        if (ts.Time.Year == workYear)
                        {
                            tmp.SpecificReply = ts.Replies[i];
                            if (i == 1)
                            {
                                if (tmp.Title.Contains('-'))
                                {
                                    tmp.Title = tmp.Title.Substring(0, 3);
                                }
                                tmp.Title += '\'';
                            }
                            data[ts.Time.Month - 1].Add(tmp);
                        }
                        ts.Time = ts.Time.AddMonths(1);
                        tmp.Time = tmp.Time.AddYears(-1);
                    }
                }
            }
            Console.WriteLine();
        }
    }
}
