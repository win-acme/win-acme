using System;
using System.IO;
using System.Linq;

namespace LetsEncrypt.ACME.Simple.Extensions
{
    public static class StringExtensions
    {
        public static string CleanFileName(this string fileName)
            =>
                Path.GetInvalidFileNameChars()
                    .Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));
        
        public static bool PromptYesNo(this string message)
        {
            while (true)
            {
                var response = Console.ReadKey(true);
                if (response.Key == ConsoleKey.Y)
                    return true;
                if (response.Key == ConsoleKey.N)
                    return false;
                Console.WriteLine(message + " (y/n)");
            }
        }
    }
}
