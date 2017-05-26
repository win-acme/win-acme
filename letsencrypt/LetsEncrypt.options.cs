using letsencrypt.Support;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security;

namespace letsencrypt
{
    partial class LetsEncrypt
    {
        public const string CLIENT_NAME = "letsencrypt-win-simple";

        public static Plugin SelectPlugin(Options options)
        {
            Plugin SelectedPlugin = null;
            Dictionary<string, Plugin> Plugins = new Dictionary<string, Plugin>();
            IEnumerable<Type> pluginTypes = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsSubclassOf(typeof(Plugin)));
            foreach (Type pluginType in pluginTypes)
            {
                Plugin plugin = pluginType.GetConstructor(new Type[] { }).Invoke(null) as Plugin;
                Plugins.Add(plugin.Name, plugin);
            }

            if (!string.IsNullOrWhiteSpace(options.Plugin) && Plugins.ContainsKey(options.Plugin))
            {
                SelectedPlugin = Plugins[options.Plugin];
            }

            while (SelectedPlugin == null)
            {
                if (options.Silent)
                {
                    throw new Exception(R.Nopluginsupplied);
                }
                Console.WriteLine();
                foreach (Plugin plugin in Target.Plugins.Values)
                {
                    plugin.PrintMenu();
                }
                Console.WriteLine(R.QuitMenu);
                Console.Write(R.Choosefromoneofthemenuoptionsabove);
                ConsoleKeyInfo menuSelection = Console.ReadKey();
                Console.WriteLine();

                if (menuSelection.Key == ConsoleKey.Q)
                {
                    break;
                }
                foreach (Plugin plugin in Target.Plugins.Values)
                {
                    if (plugin.GetSelected(menuSelection))
                    {
                        SelectedPlugin = plugin;
                    }
                }
            }
            return SelectedPlugin;
        }
        
        public static ConsoleKey ReadCharFromConsole(Options options)
        {
            if (!options.Silent)
            {
                return Console.ReadKey().Key;
            }
            return ConsoleKey.Escape;
        }

        public static string PromptForText(Options options, string message)
        {
            if (options.Silent)
            {
                return "";
            }
            Console.WriteLine(message);
            var response = Console.ReadLine().Trim();
            return response;
        }

        public static bool PromptYesNo(Options options, string message, bool defaultResponse = true)
        {
            if (options.Silent)
            {
                return defaultResponse;
            }
            else
            {
                Console.WriteLine(message + R.PromptYesNo);
                var response = Console.ReadKey(true);
                switch (response.Key)
                {
                    case ConsoleKey.J: //Ja
                    case ConsoleKey.O: //Oui
                    case ConsoleKey.S: //Si
                    case ConsoleKey.Y:
                        return true;
                    case ConsoleKey.N:
                        return false;
                }
            }
            return false;
        }

        internal static string DisplayMenuOptions(Options options, JArray list, string message, string displayKey, string valueKey, bool multiSelection)
        {
            if (list.Count == 1)
            {
                return GetString(list.First, valueKey);
            }
            Console.WriteLine();
            int i = 1;
            int width = list.Count.ToString().Length;
            foreach (var sub in list)
            {
                string index = Pad(i, width);
                Console.WriteLine($"{index}: {sub[displayKey]}");
                i++;
            }
            string value = "";
            List<string> values = new List<string>();
            while (values.Count == 0 && !options.Silent)
            {
                Console.Write("\n" + message + ": ");
                value = Console.ReadLine();
                string[] entries = value.Split(", ;".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                foreach (string v in entries)
                {
                    if (int.TryParse(v, out i) && 0 < i && i <= list.Count)
                    {
                        var option = list[i - 1];
                        values.Add(GetString(option, valueKey));
                        if (!multiSelection)
                        {
                            break;
                        }
                    }
                    else
                    {
                        value = "";
                    }
                }
            }
            return string.Join(",", values);
        }

        private static string Pad(int number, int width)
        {
            string result = number.ToString();
            while (result.Length < width) { result = " " + result; }
            return result;
        }

        // Replaces the characters of the typed in password with asterisks
        // More info: http://rajeshbailwal.blogspot.com/2012/03/password-in-c-console-application.html
        internal static SecureString ReadPassword()
        {
            var password = new SecureString();
            try
            {
                ConsoleKeyInfo info = Console.ReadKey(true);
                while (info.Key != ConsoleKey.Enter)
                {
                    if (info.Key != ConsoleKey.Backspace)
                    {
                        Console.Write("*");
                        password.AppendChar(info.KeyChar);
                    }
                    else if (info.Key == ConsoleKey.Backspace)
                    {
                        if (password != null)
                        {
                            // remove one character from the list of password characters
                            password.RemoveAt(password.Length - 1);
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

            return password;
        }

        internal static string GetString(Dictionary<string, string> dict, string key, string defaultValue = null)
        {
            if (dict.ContainsKey(key))
            {
                return dict[key];
            }
            return defaultValue;
        }

        internal static string GetString(ObjectDictionary dict, string key, string defaultValue = null)
        {
            if (dict.ContainsKey(key))
            {
                return (string)dict[key];
            }
            return defaultValue;
        }

        internal static string GetString(JToken obj, string key, string defaultValue = null)
        {
            try
            {
                return (string)obj[key];
            }
            catch { }
            return defaultValue;
        }
    }
}
