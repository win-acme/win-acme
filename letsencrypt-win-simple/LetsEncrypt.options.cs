using CommandLine;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Principal;

namespace LetsEncrypt.ACME.Simple
{
    partial class LetsEncrypt
    {
        internal const string CLIENT_NAME = "letsencrypt-win-simple";

        internal static Options Options;

        private static Dictionary<string, Plugin> Plugins = new Dictionary<string, Plugin>();
        private static Plugin SelectedPlugin = null;

        private static void CreateLogger()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.LiterateConsole(outputTemplate: "{Message}{NewLine}{Exception}")
                .WriteTo.EventLog("letsencrypt_win_simple", restrictedToMinimumLevel: LogEventLevel.Warning)
                .ReadFrom.AppSettings()
                .CreateLogger();
            Log.Information("The global logger has been configured");
        }

        private static void ParseOptions(string[] args)
        {
            try
            {
                ParserResult<Options> commandLineParseResult = Parser.Default.ParseArguments<Options>(args);
                Parsed<Options> parsed = commandLineParseResult as Parsed<Options>;
                if (parsed == null)
                {
                    Log.Error("Failed to parse commandline");
                    Environment.Exit(-1);
                }

                Options = parsed.Value;

                if (Options.Test)
                {
                    Options.BaseUri = "https://acme-staging.api.letsencrypt.org/";
                }

                if (Options.San)
                {
                    Log.Debug("San Option Enabled: Running per site and not per host");
                }

                Log.Debug("ACME Server: {BaseUri}", Options.BaseUri);

                ParseRenewalPeriod();

                ParseCertificateStore();

                ParseCentralSslStore();

                CreateConfigPath();

                SetAndCreateCertificatePath();

                Log.Debug("{@Options}", Options);
            }
            catch
            {
                Log.Error("Invalid command line options");
                throw;
            }
        }

        private static void ParseRenewalPeriod()
        {
            try
            {
                if (Options.RenewalPeriod <= 0)
                {
                    Options.RenewalPeriod = Properties.Settings.Default.RenewalDays;
                }
                Log.Information("Renewal Period: {RenewalPeriod}", Options.RenewalPeriod);
            }
            catch (Exception ex)
            {
                Log.Warning("Error reading RenewalDays from app config, defaulting to {RenewalPeriod} Error: {@ex}",
                    Options.RenewalPeriod.ToString(), ex);
            }
        }

        private static void ParseCertificateStore()
        {
            try
            {
                if (string.IsNullOrEmpty(Options.CertificateStore))
                {
                    Options.CertificateStore = Properties.Settings.Default.CertificateStore;
                }
                Log.Information("Certificate Store: {CertificateStore}", Options.CertificateStore);
            }
            catch (Exception ex)
            {
                Log.Warning(
                    "Error reading CertificateStore from app config, defaulting to {CertificateStore} Error: {@ex}",
                    Options.CertificateStore, ex);
            }
        }
        
        private static void ParseCentralSslStore()
        {
            if (!string.IsNullOrWhiteSpace(Options.CentralSslStore))
            {
                Log.Information("Using Centralized SSL Path: {CentralSslStore}", Options.CentralSslStore);
                Options.CentralSsl = true;
            }
        }

        private static void CreateConfigPath()
        {
            if (string.IsNullOrEmpty(Options.ConfigPath))
            {
                Options.ConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), CLIENT_NAME,
                    CleanFileName(Options.BaseUri));
            }
            Log.Information("Config Folder: {ConfigPath}", Options.ConfigPath);
            CreateDir(Options.ConfigPath);
        }

        private static void CreateDir(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
        
        private static void SetAndCreateCertificatePath()
        {
            if (string.IsNullOrWhiteSpace(Options.CertOutPath))
            {
                Options.CertOutPath = Properties.Settings.Default.CertificatePath;
            }

            CreateCertificatePath();

            Log.Information("Certificate Folder: {CertOutPath}", Options.CertOutPath);

        }

        private static void CreateCertificatePath()
        {
            bool failed = true;
            try
            {
                if (!string.IsNullOrEmpty(Options.CertOutPath))
                {
                    CreateDir(Options.CertOutPath);
                }
                failed = false;
            }
            catch (Exception ex)
            {
                Log.Warning(
                    "Error creating the certificate directory, {CertOutPath}. {@ex}",
                    Options.CertOutPath, ex);
            }
            if (failed)
            {
                throw new DirectoryNotFoundException("Certificate directory could not be created.");
            }
        }

        private static void SelectPlugin()
        {
            IEnumerable<Type> pluginTypes = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsSubclassOf(typeof(Plugin)));
            foreach (Type pluginType in pluginTypes)
            {
                Plugin plugin = pluginType.GetConstructor(new Type[] { }).Invoke(null) as Plugin;
                Plugins.Add(plugin.Name, plugin);
            }

            if (!string.IsNullOrWhiteSpace(Options.Plugin) && Plugins.ContainsKey(Options.Plugin))
            {
                SelectedPlugin = Plugins[Options.Plugin];
            }

            while (SelectedPlugin == null)
            {
                if (Options.Silent)
                {
                    throw new Exception("No plugin supplied");
                }
                Console.WriteLine();
                foreach (Plugin plugin in Target.Plugins.Values)
                {
                    plugin.PrintMenu();
                }
                Console.WriteLine(" Q: Quit");
                Console.Write("Choose from one of the menu options above: ");
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
        }

        private static bool Validate()
        {
            bool IsElevated = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
            if (SelectedPlugin.RequiresElevated && !IsElevated)
            {
                Console.WriteLine("This program must be run from an administrative command prompt.");
                Environment.ExitCode = 2;
                return false;
            }
            return SelectedPlugin.Validate();
        }

        internal static List<string> GetAlternativeNames()
        {
            var sanList = new List<string>();
            if (Options.San)
            {
                Console.Write("Enter all alternative names separated by commas: ");
                Console.SetIn(new System.IO.StreamReader(Console.OpenStandardInput(8192)));
                var input = Console.ReadLine();
                string[] alternativeNames = input.Split(',');

                if (alternativeNames.Length > 100)
                {
                    Log.Error("You entered too many hosts for a San certificate. Let's Encrypt currently has a maximum of 100 alternative names per certificate.");
                }
                else
                {
                    sanList.AddRange(alternativeNames);
                }
            }
            return sanList;
        }
        
        internal static ConsoleKey ReadCharFromConsole()
        {
            if (!Options.Silent)
            {
                return Console.ReadKey().Key;
            }
            return ConsoleKey.Escape;
        }

        internal static bool PromptYesNo(string message, bool defaultResponse = true)
        {
            if (Options.Silent)
            {
                return defaultResponse;
            }
            else
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
            }
            return false;
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
    }
}
