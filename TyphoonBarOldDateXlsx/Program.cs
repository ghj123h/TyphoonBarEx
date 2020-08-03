using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using Excel = Microsoft.Office.Interop.Excel;

namespace TyphoonBarOldDateXlsx
{
    class Program
    {
        static readonly DateTime startTime = new DateTime(2017, 1, 1), endTime = new DateTime(2018, 1, 1);
        static void Main(string[] args)
        {
            Regex line = new Regex(@"(?<time>.+?\s.+?)\s+(?<reply>\d+)");
            Dictionary<DateTime, int> rows = new Dictionary<DateTime, int>();
            FileStream fs;
            StreamReader sr;
            Excel.Application excel = new Excel.Application();
            Excel.Workbook wb = null;
            excel.Visible = false;
            excel.DisplayAlerts = false;
            wb = excel.Workbooks.Add(Excel.XlWBATemplate.xlWBATWorksheet);
            Excel.Worksheet hour = wb.Worksheets[1];
            Excel.Worksheet day = wb.Worksheets.Add();
            DateTime tmp = startTime;
            for (int i = 1; tmp <= endTime; i++, tmp += new TimeSpan(3, 0, 0))
            {
                hour.Cells[i, 1] = tmp.ToString();
                rows.Add(tmp, i);
            }
            for (int m = 1701, k = 0; m <= 1712; m++)
            {
                if (Directory.Exists(m.ToString()))
                {
                    foreach (var file in Directory.EnumerateFiles(m.ToString()))
                    {
                        fs = new FileStream(file, FileMode.Open, FileAccess.Read);
                        sr = new StreamReader(fs);
                        while (!sr.EndOfStream)
                        {
                            Match match = line.Match(sr.ReadLine());
                            if (!match.Success)
                            {
                                break;
                            }
                            try
                            {
                                hour.Cells[rows[DateTime.Parse(match.Groups["time"].Value)], k + 2] = match.Groups["reply"].Value;
                            }
                            catch (Exception)
                            {
                                break;
                            }
                        }
                        sr.Close();
                        fs.Close();
                        k++;
                        k %= 250;
                    }
                }
            }
            tmp = startTime;
            day.Cells[1, 1] = "Day";
            day.Cells[1, 2] = "Total";
            day.Cells[1, 3] = "Inc";
            for (int i = 2; tmp <= endTime; tmp += new TimeSpan(24, 0, 0), i++)
            {
                day.Cells[i, 1] = tmp.ToLongDateString();
                day.Cells[i, 2] = string.Format("=SUM(Sheet1!B2:Sheet1!IV{0})", i * 8 - 7);
                day.Cells[i, 3] = i == 2 ? "=B2" : string.Format("=B{0}-B{1}", i, i - 1);
            }
            wb.SaveAs(Directory.GetCurrentDirectory() + "\\2017hahaha.xlsx", 56);
            wb.Close();
        }
    }
}
