using System;

namespace LetsEncrypt.ACME.Simple.Services
{
    public class ConsoleService
    {
        public string ReadCommandFromConsole()
        {
            return Console.ReadLine().ToLowerInvariant();
        }

        public bool PromptYesNo(string message)
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
    }
}
