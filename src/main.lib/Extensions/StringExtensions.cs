using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace PKISharp.WACS.Extensions
{
    public static class StringExtensions
    {
        public static string CleanBaseUri(this string fileName)
        {
            fileName = fileName.Replace("https://", "").Replace("http://", "");
            return Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));
        }

        public static string ReplaceNewLines(this string input) => Regex.Replace(input, @"\r\n?|\n", " ");

        public static string ConvertPunycode(this string input)
        {
            if (!string.IsNullOrEmpty(input) && (input.StartsWith("xn--") || input.Contains(".xn--")))
            {
                return new IdnMapping().GetUnicode(input);
            }
            else
            {
                return input;
            }
        }

        public static List<string>? ParseCsv(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }
            return input.
                Split(',').
                Where(x => !string.IsNullOrWhiteSpace(x)).
                Select(x => x.Trim().ToLower()).
                Distinct().
                ToList();
        }

        public static bool ValidFile(this string input, ILogService logService)
        {
            try
            {
                var fi = new FileInfo(Environment.ExpandEnvironmentVariables(input));
                if (!fi.Exists)
                {
                    logService.Error("File {path} does not exist", fi.FullName);
                    return false;
                }
                return true;
            }
            catch
            {
                logService.Error("Unable to parse path {path}", input);
                return false;
            }
        }

        public static bool ValidPath(this string input, ILogService logService)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                logService.Error("No path specified");
                return false;
            }
            try
            {
                var di = new DirectoryInfo(Environment.ExpandEnvironmentVariables(input));
                if (!di.Exists)
                {
                    logService.Error("Directory {path} does not exist", di.FullName);
                    return false;
                }
                return true;
            }
            catch
            {
                logService.Error("Unable to parse path {path}", input);
                return false;
            }
        }
    }
}