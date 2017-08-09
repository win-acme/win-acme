using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.Simple
{
    public class Input
    {
        public static string RequestString(string what)
        {
            var answer = string.Empty;
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($" {what}: ");
            Console.ResetColor();
            answer = Console.ReadLine();
            Console.WriteLine();
            return answer.Trim();
        }

        public static bool PromptYesNo(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine();
            Console.Write($" {message} ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"(y/n): ");
            Console.ResetColor();
            var response = Console.ReadKey(true);
            switch (response.Key)
            {
                case ConsoleKey.Y:
                    Console.WriteLine("-- yes");
                    Console.WriteLine();
                    return true;
                case ConsoleKey.N:
                    Console.WriteLine("-- no");
                    Console.WriteLine();
                    return false;
            }
            Console.WriteLine("-- default to no");
            Console.WriteLine();
            return false;
        }

        // Replaces the characters of the typed in password with asterisks
        // More info: http://rajeshbailwal.blogspot.com/2012/03/password-in-c-console-application.html
        public static string ReadPassword(string what)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($" {what}: ");
            Console.ResetColor();
            var password = new StringBuilder();
            try
            {
                ConsoleKeyInfo info = Console.ReadKey(true);
                while (info.Key != ConsoleKey.Enter)
                {
                    if (info.Key != ConsoleKey.Backspace)
                    {
                        Console.Write("*");
                        password.Append(info.KeyChar);
                    }
                    else if (info.Key == ConsoleKey.Backspace)
                    {
                        if (password != null)
                        {
                            // remove one character from the list of password characters
                            password.Remove(password.Length - 1, 1);
                            // get the location of the cursor
                            int pos = Console.CursorLeft;
                            // move the cursor to the left by one character
                            Console.SetCursorPosition(pos - 1, Console.CursorTop);
                            // replace it with space
                            Console.Write(" ");
                            // move the cursor to the left by one character again
                            Console.SetCursorPosition(pos - 1, Console.CursorTop);
                        }
                    }
                    info = Console.ReadKey(true);
                }
                // add a new line because user pressed enter at the end of their password
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Log.Error("Error Reading Password: {@ex}", ex);
            }

            return password.ToString();
        }

        public static string ReadCommandFromConsole()
        {
            return Console.ReadLine().ToLowerInvariant();
        }
    }
}
