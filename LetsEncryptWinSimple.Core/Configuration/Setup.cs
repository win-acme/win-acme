using System;
using System.IO;
using LetsEncryptWinSimple.Core.Extensions;
using LetsEncryptWinSimple.Core.Interfaces;
using Serilog;
using Serilog.Events;

namespace LetsEncryptWinSimple.Core.Configuration
{
    public class Setup
    {
        protected IOptions Options;
        public void Initialize(IOptions options)
        {
            CreateLogger();
            Options = options;
            if (Options.Test)
                SetTestParameters();
            TryParseRenewalPeriod();
            TryParseCertificateStore();
            ParseCentralSslStore();
            CreateSettings();
            CreateConfigPath();
            SetAndCreateCertificatePath();
            TryGetHostsPerPageFromSettings();
        }

        private void CreateLogger()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.LiterateConsole(outputTemplate: "{Message}{NewLine}{Exception}")
                .WriteTo.EventLog("letsencrypt_win_simple", restrictedToMinimumLevel: LogEventLevel.Warning)
                .ReadFrom.AppSettings()
                .CreateLogger();
            Log.Information("The global logger has been configured");
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

        private void ParseCentralSslStore()
        {
            if (string.IsNullOrWhiteSpace(Options.CentralSslStore))
                return;

            Log.Information("Using Centralized SSL Path: {CentralSslStore}", Options.CentralSslStore);
            Options.CentralSsl = true;
        }

        private void CreateSettings()
        {
            Options.Settings = new Settings(Options.ClientName, Options.BaseUri);
            Log.Debug("{@_settings}", Options.Settings);
        }

        private void CreateConfigPath()
        {
            string configBasePath;
            if (string.IsNullOrWhiteSpace(Options.ConfigPath))
            {
                configBasePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            }
            else
            {
                configBasePath = Options.ConfigPath;
            }
            Options.ConfigPath = Path.Combine(configBasePath, Options.ClientName, Options.BaseUri.CleanFileName());
            Log.Information("Config Folder: {OptionsConfigPath}", Options.ConfigPath);
            Directory.CreateDirectory(Options.ConfigPath);
        }

        private void SetAndCreateCertificatePath()
        {
            if (string.IsNullOrWhiteSpace(Options.CertOutPath))
                Options.CertOutPath = Properties.Settings.Default.CertificatePath;

            if (string.IsNullOrWhiteSpace(Options.CertOutPath))
                Options.CertOutPath = Options.ConfigPath;

            CreateCertificatePath();

            Log.Information("Certificate Folder: {OptionsCertOutPath}", Options.CertOutPath);
        }

        private void CreateCertificatePath()
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

        private int TryGetHostsPerPageFromSettings()
        {
            var hostsPerPage = 50;
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
