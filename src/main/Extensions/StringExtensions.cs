using PKISharp.WACS.Services;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace PKISharp.WACS.Extensions
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

        /// <summary>
        /// Gets the full path for a file which is potentially too long for the 
        /// Windows file system, falling back to a hashed version of the file name
        /// when a PathTooLongException arises.
        /// </summary>
        /// <param name="basePath"></param>
        /// <param name="longPart"></param>
        /// <param name="extension"></param>
        /// <param name="_log"></param>
        /// <returns></returns>
        public static FileInfo LongFile(this string basePath, string prefix, string longPart, string postfix, ILogService _log)
        {
            FileInfo fi = null;
            var shortForm = $"{prefix}{longPart.SHA256()}{postfix}";
            var longForm = $"{prefix}{longPart}{postfix}";
            try
            {
                // First test the hashed name
                fi = new FileInfo(Path.Combine(basePath, shortForm));
                if (!fi.Exists)
                {
                    fi = new FileInfo(Path.Combine(basePath, longForm));
                }
            }
            catch (PathTooLongException)
            {
                if (fi == null)
                {
                    _log.Error("File name {shortForm} too long", shortForm, basePath);
                }
                else
                {
                    _log.Verbose("File name {longForm} too long, falling back to {shortForm}", longForm, shortForm);
                }
            }
            catch (Exception ex)
            {
                if (fi == null)
                {
                    _log.Warning("Unable access {longForm} ({ex}), but fallback {shortForm} available.", longForm, ex.Message, shortForm);
                }
                else
                {
                    _log.Error(ex, "Unable access {longForm} nor its fallback {shortForm} ({ex}).", longForm, shortForm, ex.Message);
                }
            }
            return fi;
        }

        /// <summary>
        /// Get hexadecimal representation of SHA256 hash from string
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static string SHA256(this string token)
        {
            var bytes = Encoding.UTF8.GetBytes(token);
            var algorithm = new SHA256Managed();
            var hash = algorithm.ComputeHash(bytes);
            var hashString = string.Empty;
            var array = hash;
            foreach (var x in array)
            {
                hashString += $"{x:x2}";
            }
            return hashString.ToLower();
        }

    }
}
