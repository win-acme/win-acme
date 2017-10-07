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
    }
}
