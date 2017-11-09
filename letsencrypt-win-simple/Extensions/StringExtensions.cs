using LetsEncrypt.ACME.Simple.Services;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace LetsEncrypt.ACME.Simple.Extensions
{
    public static class StringExtensions
    {
        public static string CleanFileName(this string fileName)
        {
            return Path.GetInvalidFileNameChars()
                .Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));
        }

        public static string ReplaceNewLines(this string input)
        {
            return Regex.Replace(input, @"\r\n?|\n", " ");
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
