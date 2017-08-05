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

        public static void WriteWarning(string message)
        {
            WriteColor(message, ConsoleColor.Yellow);
            Log.Debug(message);
        }

        public static void WriteError(string message)
        {
            WriteColor(message, ConsoleColor.Red);
            Log.Error(message);
        }

        private static void WriteColor(string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        public static bool PromptYesNo(string message)
        {
            Console.WriteLine(message + " (y/n)");
            var response = Console.ReadKey(true);
            switch (response.Key)
            {
                case ConsoleKey.Y:
                    return true;
                case ConsoleKey.N:
                    return false;
            }
            return false;
        }

        // Replaces the characters of the typed in password with asterisks
        // More info: http://rajeshbailwal.blogspot.com/2012/03/password-in-c-console-application.html
        public static string ReadPassword()
        {
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
