using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace FreeswitchListenerServer.Class
{
    public class EtraabTts
    {
        private static List<string> ExactFileList = new List<string>()
        {
            "0","1","2","3","4","5","6","7","8","9","10",
            "11","12","13","14","15","16","17","18","19",
            "20","30","40","50","60","70","80","90",
            "200","300","400","500","600","700","800","900"
        };

        public static IEnumerator<byte[]> ReadFilesBytes(List<string> fileNameList)
        {
            foreach (var fileName in fileNameList)
            {
                if (File.Exists(fileName))
                {
                    yield return File.ReadAllBytes(fileName);
                }
            }
        } 

        public static IEnumerable<string> GetFileNames(string numberStr)
        {
            if (IsExactFile(numberStr))
            {
                return new[] { GetExactFileName(numberStr) };
            }
            else
            {
                var number = long.Parse(numberStr);

                if (number > 999999999)
                    return GetFileNamesForBilion(number);
                else if (number > 999999)
                    return GetFileNamesForMilion(number);
                else if (number > 999)
                    return GetFileNamesForThousand(number);
                else if (number > 99)
                    return GetFileNamesForHundred(number);
                else if (number > 9)
                    return GetFileNamesForUnderHundred(number);

            }
            return new List<string>();
        }

        public static IEnumerable<string> GetFileNamesOfDate(string year, int month, int day)
        {
            var files = new List<string> {GetDayOrMonthNumber(day), GetDayOrMonthNumber(month)};
            files.AddRange(GetFileNames(year));
            return files;
        }

        public static IEnumerable<string> GetFileNamesOfTime(string hour, string min)
        {
            var files = new List<string> {"EtIvr/hour.wav", GetFileNamesOfhour(hour)};
            files.AddRange(GetFileNames(min));
            files.Add("EtIvr/min.wav");
            return files;
        }

        private static string GetFileNamesOfhour(string hour)
        {
            return $"EtIvr/{hour}_o.wav";
        }

        private static IEnumerable<string> GetFileNamesForBilion(long number)
        {
            var files = new List<string>();
            var divResult = number / 1000000000;
            if (divResult > 0)
            {
                files.AddRange(divResult > 99 ? GetFileNamesForHundred(divResult) : GetFileNamesForUnderHundred(divResult));
                var remainedByDiv = number % 1000000000;
                if (remainedByDiv == 0)
                {
                    files.Add("EtIvr/bilioin.wav");
                }
                else
                {
                    files.Add("EtIvr/bilioin_o.wav");
                    files.AddRange(GetFileNamesForMilion(remainedByDiv));
                }
            }
            return files;
        }

        private static IEnumerable<string> GetFileNamesForMilion(long number)
        {
            var files = new List<string>();
            var divResult = number / 1000000;
            if (divResult > 0)
            {
                files.AddRange(divResult > 99 ? GetFileNamesForHundred(divResult) : GetFileNamesForUnderHundred(divResult));
                var remainedByDiv = number % 1000000;
                if (remainedByDiv == 0)
                {
                    files.Add("EtIvr/milioin.wav");
                }
                else
                {
                    files.Add("EtIvr/milioin_o.wav");
                    files.AddRange(GetFileNamesForThousand(remainedByDiv));
                }
            }
            return files;
        }

        private static IEnumerable<string> GetFileNamesForThousand(long number)
        {
            var files = new List<string>();
            var divResult = number / 1000;
            if (divResult > 0)
            {
                files.AddRange(divResult > 99 ? GetFileNamesForHundred(divResult) : GetFileNamesForUnderHundred(divResult));

                var remainedByDiv = number % 1000;
                if (remainedByDiv == 0)
                {
                    files.Add("EtIvr/thousand.wav");
                }
                else
                {
                    files.Add("EtIvr/thousand_o.wav");

                    if (IsExactFile(remainedByDiv.ToString()))
                        files.Add(GetExactFileName(remainedByDiv.ToString()));
                    else if (remainedByDiv > 99)
                        files.AddRange(GetFileNamesForHundred(remainedByDiv));
                    else if (remainedByDiv > 9)
                        files.AddRange(GetFileNamesForUnderHundred(remainedByDiv));
                }
            }
            return files;
        }

        private static IEnumerable<string> GetFileNamesForHundred(long number)
        {
            var files = new List<string>();
            if (IsExactFile(number.ToString()))
            {
                files.Add(GetExactFileName(number.ToString()));
                return files;
            }

            var divResult = number / 100;
            if (divResult > 0)
            {
                files.Add(GetHunredAttachedNumber(int.Parse(divResult.ToString(CultureInfo.InvariantCulture))));
                var remainedByDiv = number % 100;

                if (remainedByDiv != 0)
                {
                    var underHundredFiles = GetFileNamesForUnderHundred(remainedByDiv);
                    files.AddRange(underHundredFiles);
                }
            }

            return files;
        }

        private static IEnumerable<string> GetFileNamesForUnderHundred(long number)
        {
            var files = new List<string>();
            if (IsExactFile(number.ToString()))
            {
                files.Add(GetExactFileName(number.ToString()));
                return files;
            }
            
            var divResult = number / 10;
            if (divResult > 0)
            {
                var remainedByDiv = number % 10;
                if (remainedByDiv != 0)
                {
                    var startOFile = GetUnderHunredAttachedNumber(int.Parse(divResult.ToString(CultureInfo.InvariantCulture)));
                    files.Add(startOFile);

                    var remainedByDivStr = remainedByDiv.ToString(CultureInfo.InvariantCulture);
                    if (IsExactFile(remainedByDivStr))
                    {
                        files.Add(GetExactFileName(remainedByDivStr));
                    }
                }
            }

            return files;
        }

        private static string GetHunredAttachedNumber(int start)
        {
            switch (start)
            {
                case 1:
                    return "EtIvr/100_o.wav";
                case 2:
                    return "EtIvr/200_o.wav";
                case 3:
                    return "EtIvr/300_o.wav";
                case 4:
                    return "EtIvr/400_o.wav";
                case 5:
                    return "EtIvr/500_o.wav";
                case 6:
                    return "EtIvr/600_o.wav";
                case 7:
                    return "EtIvr/700_o.wav";
                case 8:
                    return "EtIvr/800_o.wav";
                case 9:
                    return "EtIvr/900_o.wav";
            }

            return String.Empty;
        }

        private static string GetUnderHunredAttachedNumber(int start)
        {
            return $"EtIvr/{start}0_o.wav";
        }

        private static string GetDayOrMonthNumber(int number)
        {
            return $"EtIvr/{number}0_o.wav";
        }

        private static string GetExactFileName(string numberStr)
            => $"EtIvr/{numberStr}.wav";

        private static bool IsExactFile(string numberStr)
            => ExactFileList.Contains(numberStr);
    }
}
