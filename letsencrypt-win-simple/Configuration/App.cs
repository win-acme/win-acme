using System;
using CommandLine;
using Serilog;
using Serilog.Events;

namespace LetsEncrypt.ACME.Simple.Configuration
{
    public class App
    {
        public static Options Options { get; set; }
        
        static App() { }

        internal void Initialize(string[] args)
        {
            Options = TryParseOptions(args);
            CreateLogger();
            if (Options.Test)
                SetTestParameters();
            TryParseRenewalPeriod();
            TryParseCertificateStore();
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
    }
}