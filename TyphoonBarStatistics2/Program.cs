using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace TyphoonBarStatistics2
{
    class Program
    {
        static readonly int split = 5, max = 4559, outputmax = 130;
        static void Main(string[] args)
        {
            Dictionary<int, int> dict = new Dictionary<int, int>();
            for (int i = 0; i <= max / split; i++)
            {
                dict.Add(i, 0);
            }
            Regex peak6 = new Regex(@"\(6-h\).+= (?<peak>\d+)");
            Match match;
            for (int y = 12; y <= 17; y++)
            {
                for (int m = 1; m <= 12; m++)
                {
                    string path = string.Format("{0:00}{1:00}", y, m);
                    if (Directory.Exists(path))
                    {
                        foreach (var thread in Directory.EnumerateFiles(path))
                        {
                            if (thread.EndsWith(".txt"))
                            {
                                match = null;
                                foreach (var line in File.ReadLines(thread))
                                {
                                    if ((match = peak6.Match(line)).Success)
                                    {
                                        break;
                                    }
                                }
                                if (match != null && match.Success)
                                {
                                    dict[int.Parse(match.Groups["peak"].Value) / split]++;
                                }
                            }
                        }
                    }
                }
            }
            for (int i = 0; i <= outputmax / split; i++)
            {
                Console.WriteLine("{0}-{1}: {2}", i * split, i * split + split - 1, dict[i]);
            }
        }
    }
}
