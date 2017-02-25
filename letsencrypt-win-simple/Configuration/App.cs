using System;
using System.IO;
using CommandLine;
using LetsEncrypt.ACME.Simple.Extensions;
using LetsEncrypt.ACME.Simple.Services;
using Serilog;
using Serilog.Events;

namespace LetsEncrypt.ACME.Simple.Configuration
{
    public class App
    {
        public static Options Options { get; set; }
        public static CertificateService CertificateService { get; set; }
        internal static AcmeClientService AcmeClientService { get; set; }
        internal static LetsEncryptService LetsEncryptService { get; set; }
        internal static ConsoleService ConsoleService { get; set; }
        
        static App() { }

        internal void Initialize(string[] args)
        {
            Options = TryParseOptions(args);
            CreateLogger();
            if (Options.Test)
                SetTestParameters();
            TryParseRenewalPeriod();
            TryParseCertificateStore();
            ParseCentralSslStore();
            CreateSettings();
            CreateConfigPath();
            SetAndCreateCertificatePath();
            TryGetHostsPerPageFromSettings();

            CertificateService = new CertificateService();
            AcmeClientService = new AcmeClientService();
            LetsEncryptService = new LetsEncryptService();
            ConsoleService = new ConsoleService();
        }

        private static Options TryParseOptions(string[] args)
        {
            try
            {
                var commandLineParseResult = Parser.Default.ParseArguments<Options>(args);
                var parsed = commandLineParseResult as Parsed<Options>;
                if (parsed == null)
                {
                    LogParsingErrorAndWaitForEnter();
                    return new Options(); // not parsed
                }

                var options = parsed.Value;

                Log.Debug("{@Options}", options);

                return options;
            }
            catch
            {
                Console.WriteLine("Failed while parsing options.");
                throw;
            }
        }

        private static void LogParsingErrorAndWaitForEnter()
        {
#if DEBUG
            Log.Debug("Program Debug Enabled");
            Console.WriteLine("Press enter to continue.");
            Console.ReadLine();
#endif
        }

        private void CreateLogger()
        {
            try
            {
                Log.Logger = new LoggerConfiguration()
                    .WriteTo.LiterateConsole(outputTemplate: "{Message}{NewLine}{Exception}")
                    .WriteTo.EventLog("letsencrypt_win_simple", restrictedToMinimumLevel: LogEventLevel.Warning)
                    .ReadFrom.AppSettings()
                    .CreateLogger();
                Log.Information("The global logger has been configured");
            }
            catch
            {
                Console.WriteLine("Error while creating logger.");
                throw;
            }
        }

        private void SetTestParameters()
        {
            Options.BaseUri = "https://acme-staging.api.letsencrypt.org/";
            Log.Debug("Test paramater set: {BaseUri}", Options.BaseUri);
        }

        private void TryParseRenewalPeriod()
        {
            try
            {
                Options.RenewalPeriodDays = Properties.Settings.Default.RenewalDays;
                Log.Information("Renewal Period: {RenewalPeriod}", Options.RenewalPeriodDays);
            }
            catch (Exception ex)
            {
                Log.Warning("Error reading RenewalDays from app config, defaulting to {RenewalPeriod} Error: {@ex}",
                    Options.RenewalPeriodDays, ex);
            }
        }

        private void TryParseCertificateStore()
        {
            try
            {
                Options.CertificateStore = Properties.Settings.Default.CertificateStore;
                Log.Information("Certificate Store: {_certificateStore}", Options.CertificateStore);
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
            if (string.IsNullOrWhiteSpace(Options.CentralSslStore))
                return;

            Log.Information("Using Centralized SSL Path: {CentralSslStore}", Options.CentralSslStore);
            Options.CentralSsl = true;
        }

        private static void CreateSettings()
        {
            Options.Settings = new Settings(Options.ClientName, Options.BaseUri);
            Log.Debug("{@_settings}", Options.Settings);
        }

        private static void CreateConfigPath()
        {
            if (string.IsNullOrWhiteSpace(Options.ConfigPath))
                Options.ConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    Options.ClientName, Options.BaseUri.CleanFileName());

            Log.Information("Config Folder: {OptionsConfigPath}", Options.ConfigPath);
            Directory.CreateDirectory(Options.ConfigPath);
        }

        private static void SetAndCreateCertificatePath()
        {
            if(string.IsNullOrWhiteSpace(Options.CertOutPath))
                Options.CertOutPath = Properties.Settings.Default.CertificatePath;

            if (string.IsNullOrWhiteSpace(Options.CertOutPath))
                Options.CertOutPath = Options.ConfigPath;

            CreateCertificatePath();

            Log.Information("Certificate Folder: {OptionsCertOutPath}", Options.CertOutPath);
        }

        private static void CreateCertificatePath()
        {
            try
            {
                Directory.CreateDirectory(Options.CertOutPath);
            }
            catch (Exception ex)
            {
                Log.Warning("Error creating the certificate directory, {OptionsCertOutPath}. Error: {@ex}",
                    Options.CertOutPath, ex);
            }
        }

        private static int TryGetHostsPerPageFromSettings()
        {
            int hostsPerPage = 50;
            try
            {
                hostsPerPage = Properties.Settings.Default.HostsPerPage;
                Options.HostsPerPage = hostsPerPage;
            }
            catch (Exception ex)
            {
                Log.Error("Error getting HostsPerPage setting, setting to default value. Error: {@ex}",
                    ex);
            }

            return hostsPerPage;
        }
    }
}