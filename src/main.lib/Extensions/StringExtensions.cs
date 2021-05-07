using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace PKISharp.WACS.Extensions
{
    public static class StringExtensions
    {
        public static string? CleanUri(this Uri? uri)
        {
            if (uri == null)
            {
                return null;
            }
            var str = uri.ToString();
            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                str = str.Replace($"{uri.UserInfo}@", "");
            }
            str = str.Replace("https://", "").Replace("http://", "");
            return str.CleanPath();
        }

        public static string? CleanPath(this string? fileName)
        {
            if (fileName == null)
            {
                return null;
            }
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

        public static List<string>? ParseCsv(this string? input)
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

        public static bool ValidFile(this string? input, ILogService logService)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                logService.Error("No path specified");
                return false;
            }
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

        public static bool ValidPath(this string? input, ILogService logService)
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public static string PatternToRegex(this string pattern)
        {
            pattern = pattern.Replace("\\\\", SlashEscape);
            pattern = pattern.Replace("\\,", CommaEscape);
            pattern = pattern.Replace("\\*", StarEscape);
            pattern = pattern.Replace("\\?", QuestionEscape);
            var parts = pattern.ParseCsv()!;
            return $"^({string.Join('|', parts.Select(x => Regex.Escape(x).PatternToRegexPart()))})$";
        }

        private const string SlashEscape = "~slash~";
        private const string CommaEscape = "~comma~";
        private const string StarEscape = "~star~";
        private const string QuestionEscape = "~question~";

        public static string EscapePattern(this string pattern)
        {
            return pattern.
               Replace("\\", "\\\\").
               Replace(",", "\\,").
               Replace("*", "\\*").
               Replace("?", "\\?");
        }

        private static string PatternToRegexPart(this string pattern)
        {
            return pattern.
                Replace("\\*", ".*").
                Replace("\\?", ".").
                Replace(SlashEscape, "\\\\").
                Replace(CommaEscape, ",").
                Replace(StarEscape, "\\*").
                Replace(QuestionEscape, "\\?");
        }

        public static string SHA1(this string original)
        {
            using var sha1 = new SHA1Managed();
            var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(original));
            return string.Concat(hash.Select(b => b.ToString("x2")));
        }

        public static ProtectedString? Protect(this string? original, bool allowEmtpy = false) {
            if (string.IsNullOrWhiteSpace(original) && !allowEmtpy)
            {
                return null;
            }
            return new ProtectedString(original);
        }
    }
}