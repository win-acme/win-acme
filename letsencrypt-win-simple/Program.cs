using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Threading;
using ACMESharp;
using ACMESharp.ACME;
using ACMESharp.HTTP;
using ACMESharp.JOSE;
using ACMESharp.PKI;
using CommandLine;
using Microsoft.Win32.TaskScheduler;
using Serilog;
using Serilog.Events;
using ACMESharp.Messages;
using Serilog.Sinks.SystemConsole.Themes;
using Serilog.Core;
using System.Diagnostics;

namespace LetsEncrypt.ACME.Simple
{
    class Program
    {
        private const string ClientName = "letsencrypt-win-simple";
        private static string _certificateStore = "WebHosting";
        public static float RenewalPeriod = 60;
        private static string _configPath;
        private static string _certificatePath;
        private static Settings _settings;
        private static AcmeClient _client;
        public static Options Options;

#if DEBUG
        private static LoggingLevelSwitch _levelSwitch = new LoggingLevelSwitch(initialMinimumLevel: LogEventLevel.Verbose);
#else
        private static LoggingLevelSwitch _levelSwitch = new LoggingLevelSwitch(initialMinimumLevel: LogEventLevel.Information);
#endif

        static bool IsElevated
            => new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

        private static void Main(string[] args)
        {
            CreateLogger();

            if (!TryParseOptions(args))
            {
                return;
            }

            if (Options.Verbose)
            {
                _levelSwitch.MinimumLevel = LogEventLevel.Verbose;
            }


            Console.WriteLine();
            Log.Information("Let's Encrypt (Simple Windows ACME Client)", Assembly.GetExecutingAssembly().GetName().Version);
#if DEBUG
            Log.Information("Version {version} (DEBUG)", Assembly.GetExecutingAssembly().GetName().Version);
#else
            Log.Information("version {version} (RELEASE)", Assembly.GetExecutingAssembly().GetName().Version);
#endif
            Log.Verbose("Verbose mode logging enabled");
            Log.Information("Please report issues at https://github.com/Lone-Coder/letsencrypt-win-simple");
            Console.WriteLine();

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            if (Options.Test)
            {
                SetTestParameters();
            }

            if (Options.ForceRenewal)
            {
                Options.Renew = true;
            }

            ParseRenewalPeriod();
            ParseCertificateStore();

            Log.Information("ACME Server: {BaseUri}", Options.BaseUri);

            ParseCentralSslStore();

            CreateSettings();
            CreateConfigPath();

            SetAndCreateCertificatePath();

            bool retry = false;
            do
            {
                try
                {
                    using (var signer = new RS256Signer())
                    {
                        signer.Init();
                        using (_client = new AcmeClient(new Uri(Options.BaseUri), new AcmeServerDirectory(), signer))
                        {
                            ConfigureAcmeClient(_client);

                            if (Options.Renew)
                            {
                                CheckRenewals();
                            }
                            else
                            {
                                List<Target> targets = GetTargets();

                                if (!string.IsNullOrWhiteSpace(Options.Plugin))
                                {
                                    // If there's a plugin in the options, only do ProcessDefaultCommand for the selected plugin
                                    // Plugins that can run automatically should allow for an empty string as menu response to work
                                    ProcessDefaultCommand(targets, string.Empty);
                                }
                                else
                                {
                                    WriteBindings(targets);
                                    PrintMenuForPlugins();
                                    var command = Input.RequestString("Choose from one of the menu options above").ToLowerInvariant();
                                    switch (command)
                                    {
                                        case "q":
                                            return;
                                        default:
                                            ProcessDefaultCommand(targets, command);
                                            break;
                                    }
                                }
                            }
                        }
                        retry = false;
                    }
                }
                catch (AcmeClient.AcmeWebException awe)
                {
                    Environment.ExitCode = awe.HResult;
                    Log.Debug("AcmeWebException {@awe}", awe);
                    Log.Error("ACME Server Returned: {acmeWebExceptionMessage} - Response: {acmeWebExceptionResponse}", awe.Message, awe.Response.ContentAsString);
                }
                catch (AcmeException ae)
                {
                    Environment.ExitCode = ae.HResult;
                    Log.Debug("AcmeException {@ae}", ae);
                    Log.Error("AcmeException {@ae}", ae.Message);
                }
                catch (Exception e)
                {
                    Environment.ExitCode = e.HResult;
                    Log.Debug("Exception {@e}", e);
                    Log.Error("Exception {@e}", e.Message);
                }

                if (Options.Test || (!Options.Renew && !Options.CloseOnFinish))
                {
                    if (Input.PromptYesNo("Would you like to start again?"))
                    {
                        Environment.ExitCode = 0;
                        retry = true;
                    }             
                }
            } while (retry);
        }

        private static bool TryParseOptions(string[] args)
        {
            try
            {
                var commandLineParseResult = Parser.Default.ParseArguments<Options>(args);
                var parsed = commandLineParseResult as Parsed<Options>;
                if (parsed == null)
                {
                    Log.Error("Unable to parse options");
                    return false;
                }
                Options = parsed.Value;
                Log.Debug("{@Options}", Options);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed while parsing options.");
                return false;
            }
        }

        private static void ConfigureAcmeClient(AcmeClient client)
        {
            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.Proxy))
            {
                client.Proxy = new WebProxy(Properties.Settings.Default.Proxy);
                Log.Warning("Proxying via {proxy}", Properties.Settings.Default.Proxy);
            }

            var signerPath = Path.Combine(_configPath, "Signer");
            if (File.Exists(signerPath))
                LoadSignerFromFile(client.Signer, signerPath);

            _client.Init();
            _client.BeforeGetResponseAction = (x) =>
            {
                Log.Debug("Send {method} request to {uri}", x.Method, x.RequestUri);
            };
            Log.Debug("Getting AcmeServerDirectory");
            _client.GetDirectory(true);

            var registrationPath = Path.Combine(_configPath, "Registration");
            if (File.Exists(registrationPath))
                LoadRegistrationFromFile(registrationPath);
            else
            {
                string email = Options.EmailAddress;
                if (string.IsNullOrWhiteSpace(email))
                {
                    email = Input.RequestString("Enter an email address (not public, used for renewal fail notices)");
                }

                string[] contacts = GetContacts(email);

                AcmeRegistration registration = CreateRegistration(contacts);

                if (!Options.AcceptTos && !Options.Renew)
                {
                    if (!Input.PromptYesNo($"Do you agree to {registration.TosLinkUri}?"))
                        return;
                }

                UpdateRegistration();
                SaveRegistrationToFile(registrationPath);
                SaveSignerToFile(_client.Signer, signerPath);
            }
        }

        private static AcmeRegistration CreateRegistration(string[] contacts)
        {
            Log.Debug("Calling register");
            var registration = _client.Register(contacts);
            return registration;
        }

        private static void SetTestParameters()
        {
            Options.BaseUri = "https://acme-staging.api.letsencrypt.org/";
            _levelSwitch.MinimumLevel = LogEventLevel.Verbose;
            Log.Debug("Test parameter set: {BaseUri}", Options.BaseUri);
        }

        private static void ProcessDefaultCommand(List<Target> targets, string command)
        {
            var targetId = 0;
            if (Int32.TryParse(command, out targetId))
            {
                GetCertificateForTargetId(targets, targetId);
                return;
            }

            HandleMenuResponseForPlugins(targets, command);
        }

        private static void HandleMenuResponseForPlugins(List<Target> targets, string command)
        {
            // Only run the plugin specified in the config
            if (!string.IsNullOrWhiteSpace(Options.Plugin))
            {
                var plugin = Target.Plugins.Values.FirstOrDefault(x => string.Equals(x.Name, Options.Plugin, StringComparison.InvariantCultureIgnoreCase));
                if (plugin != null)
                    plugin.HandleMenuResponse(command, targets);
                else
                {
                    Log.Error("Plugin {Plugin} could not be found. Press enter to exit.", Options.Plugin);
                    Console.ReadLine();
                }
            }
            else
            {
                foreach (var plugin in Target.Plugins.Values)
                    plugin.HandleMenuResponse(command, targets);
            }
        }

        private static void GetCertificateForTargetId(List<Target> targets, int targetId)
        {
            var targetIndex = targetId - 1;
            if (targetIndex >= 0 && targetIndex < targets.Count)
            {
                Target binding = targets[targetIndex];
                binding.Plugin.Auto(binding);
            }
        }

        private static void LoadRegistrationFromFile(string registrationPath)
        {
            Log.Debug("Loading registration from {registrationPath}", registrationPath);
            using (var registrationStream = File.OpenRead(registrationPath))
                _client.Registration = AcmeRegistration.Load(registrationStream);
        }

        private static string[] GetContacts(string email)
        {
            var contacts = new string[] { };
            if (!String.IsNullOrEmpty(email))
            {
                Log.Debug("Registration email: {email}", email);
                email = "mailto:" + email;
                contacts = new string[] { email };
            }

            return contacts;
        }

        private static void SaveSignerToFile(ISigner signer, string signerPath)
        {
            Log.Debug("Saving signer");
            using (var signerStream = File.OpenWrite(signerPath))
                signer.Save(signerStream);
        }

        private static void SaveRegistrationToFile(string registrationPath)
        {
            Log.Debug("Saving registration");
            using (var registrationStream = File.OpenWrite(registrationPath))
                _client.Registration.Save(registrationStream);
        }

        private static void UpdateRegistration()
        {
            Log.Debug("Updating registration");
            _client.UpdateRegistration(true, true);
        }

        private static void WriteBindings(List<Target> targets)
        {
            if (targets.Count == 0)
            {
                Log.Warning("No targets found.");
            }
            else
            {
                var hostsPerPage = GetHostsPerPageFromSettings();
                var currentPlugin = "";
                var currentIndex = 0;
                var currentPage = 0;

                while (currentIndex < targets.Count - 1)
                {
                    // Paging
                    if (currentIndex > 0)
                    {
                        Input.Wait();
                        currentPage += 1;
                    }
                    var page = targets.Skip(currentPage * hostsPerPage).Take(hostsPerPage);
                    foreach (var target in page)
                    {
                        // Seperate target from different plugins
                        if (!string.Equals(currentPlugin, target.PluginName, StringComparison.InvariantCultureIgnoreCase))
                        {
                            currentPlugin = target.PluginName;
                            Console.WriteLine();
                        }
                        Console.WriteLine($" {currentIndex + 1}: {targets[currentIndex]}");
                        currentIndex++;
                    }
                }
                Console.WriteLine();
            }
        }

        private static void PrintMenuForPlugins()
        {
            // Check for a plugin specified in the options
            // Only print the menus if there's no plugin specified
            // Otherwise: you actually have no choice, the specified plugin will run
            if (!string.IsNullOrWhiteSpace(Options.Plugin))
                return;

            foreach (var plugin in Target.Plugins.Values)
            {
                plugin.PrintMenu();
            }
            Console.WriteLine(" Q: Quit");
        }

        private static int GetHostsPerPageFromSettings()
        {
            int hostsPerPage = 50;
            try
            {
                hostsPerPage = Properties.Settings.Default.HostsPerPage;
            }
            catch (Exception ex)
            {
                Log.Error("Error getting HostsPerPage setting, setting to default value. Error: {@ex}", ex);
            }

            return hostsPerPage;
        }

        private static void LoadSignerFromFile(ISigner signer, string signerPath)
        {
            Log.Debug("Loading signer from {signerPath}", signerPath);
            using (var signerStream = File.OpenRead(signerPath))
                signer.Load(signerStream);
        }

        private static void SetAndCreateCertificatePath()
        {
            _certificatePath = Properties.Settings.Default.CertificatePath;

            if (string.IsNullOrWhiteSpace(_certificatePath))
                _certificatePath = _configPath;
            else
                CreateCertificatePath();

            Log.Debug("Certificate folder: {_certificatePath}", _certificatePath);

        }

        private static void CreateCertificatePath()
        {
            try
            {
                Directory.CreateDirectory(_certificatePath);
            }
            catch (Exception ex)
            {
                Log.Warning(
                    "Error creating the certificate directory, {_certificatePath}. Defaulting to config path. Error: {@ex}",
                    _certificatePath, ex);

                _certificatePath = _configPath;
            }
        }

        private static void CreateConfigPath()
        {
            // Path configured in settings always wins
            string configBasePath = Properties.Settings.Default.ConfigurationPath;

            if (string.IsNullOrWhiteSpace(configBasePath))
            {
                // The default folder location for compatibility with v1.9.4 and before is 
                // still the ApplicationData folder.
                configBasePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

                // However, if that folder doesn't exist already (so we are either a new install
                // or a new user account), we choose the CommonApplicationData folder instead to
                // be more flexible in who runs the program (interactive or task scheduler).
                if (!Directory.Exists(Path.Combine(configBasePath, ClientName)))
                {
                    configBasePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                }
            }

            _configPath = Path.Combine(configBasePath, ClientName, CleanFileName(Options.BaseUri));
            Log.Debug("Config folder: {_configPath}", _configPath);
            Directory.CreateDirectory(_configPath);
        }

        private static void CreateSettings()
        {
            _settings = new Settings(ClientName, Options.BaseUri);
            Log.Debug("{@_settings}", _settings);
        }

        private static List<Target> GetTargets()
        {
            var targets = new List<Target>();
            foreach (var plugin in Target.Plugins.Values)
            {
                targets.AddRange(!Options.San ? plugin.GetTargets() : plugin.GetSites());
            }
            return targets;
        }

        private static void ParseCentralSslStore()
        {
            if (Options.CentralSsl)
            {
                Log.Debug("Using Centralized SSL path: {CentralSslStore}", Options.CentralSslStore);
            }
        }

        private static void ParseCertificateStore()
        {
            try
            {
                _certificateStore = Properties.Settings.Default.CertificateStore;
                Log.Information("Certificate store: {_certificateStore}", _certificateStore);
            }
            catch (Exception ex)
            {
                Log.Warning("Error reading CertificateStore from config, defaulting to {_certificateStore} Error: {@ex}", _certificateStore, ex);
            }
        }

        private static void ParseRenewalPeriod()
        {
            try
            {
                RenewalPeriod = Properties.Settings.Default.RenewalDays;
                Log.Information("Renewal period: {RenewalPeriod}", RenewalPeriod);
            }
            catch (Exception ex)
            {
                Log.Warning("Error reading RenewalDays from app config, defaulting to {RenewalPeriod} Error: {@ex}", RenewalPeriod.ToString(), ex);
            }
        }

        private static void CreateLogger()
        {
            try { 
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.ControlledBy(_levelSwitch)
                    .WriteTo.Console(outputTemplate: "[{Level:u4}] {Message:l}{NewLine}{Exception}", theme: SystemConsoleTheme.Literate)
                    .WriteTo.EventLog("letsencrypt_win_simple", restrictedToMinimumLevel: LogEventLevel.Warning)
                    .ReadFrom.AppSettings()
                    .CreateLogger();

                Log.Debug("The global logger has been configured");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($" Error while creating logger: {ex.Message} - {ex.StackTrace}");
                Console.ResetColor();
                Console.WriteLine();
                Environment.Exit(ex.HResult);
            }
        }

        private static string CleanFileName(string fileName)
            =>
                Path.GetInvalidFileNameChars()
                    .Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));

        public static void Auto(Target binding)
        {
            try
            {
                var auth = Authorize(binding);
                if (auth.Status == "valid")
                {
                    var pfxFilename = GetCertificate(binding);

                    if (Options.Test && !Options.Renew)
                    {
                        if (!Input.PromptYesNo($"Do you want to install the .pfx into the Certificate Store/ Central SSL Store?"))
                            return;
                    }

                    if (!Options.CentralSsl)
                    {
                        X509Store store;
                        X509Certificate2 certificate;
                        Log.Information("Installing Non-Central SSL Certificate in the certificate store");
                        InstallCertificate(binding, pfxFilename, out store, out certificate);
                        if (Options.Test && !Options.Renew)
                        {
                            if (!Input.PromptYesNo($"Do you want to add/update the certificate to your server software?"))
                                return;
                        }
                        Log.Information("Installing Non-Central SSL Certificate in server software");
                        binding.Plugin.Install(binding, pfxFilename, store, certificate);
                        if (!Options.KeepExisting)
                        {
                            UninstallCertificate(binding.Host, out store, certificate);
                        }
                    }
                    else if (!Options.Renew || !Options.KeepExisting)
                    {
                        //If it is using centralized SSL, renewing, and replacing existing it needs to replace the existing binding.
                        Log.Information("Updating new Central SSL Certificate");
                        binding.Plugin.Install(binding);
                    }

                    if (Options.Test && !Options.Renew)
                    {
                        if (!Input.PromptYesNo($"Do you want to automatically renew this certificate in {RenewalPeriod} days? This will add a task scheduler task."))
                            return;
                    }

                    if (!Options.Renew)
                    {
                        Log.Information("Adding renewal for {binding}", binding);
                        ScheduleRenewal(binding);
                    }
                }
                else
                {
                    var errors = auth.Challenges.
                        Select(c => c.ChallengePart).
                        Where(cp => cp.Status == "invalid").
                        SelectMany(cp => cp.Error);

                    foreach (var error in errors)
                    {
                        Log.Error("ACME server reported {_key} {@value}", error.Key, error.Value);
                    }

                    throw new AuthorizationFailedException(auth, errors.Select(x => x.Value));
                }
            }
            catch (AcmeException)
            {
                // Might want to do some logging/debugging here...
                throw;
            }
 
        }

        public static void InstallCertificate(Target binding, string pfxFilename, out X509Store store,
            out X509Certificate2 certificate)
        {
            try
            {
                store = new X509Store(_certificateStore, StoreLocation.LocalMachine);
                store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadWrite);
            }
            catch (CryptographicException)
            {
                store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadWrite);
            }
            catch (Exception ex)
            {
                Log.Error("Error encountered while opening certificate store. Error: {@ex}", ex);
                throw new Exception(ex.Message);
            }

            Log.Debug("Opened Certificate Store {Name}", store.Name);
            certificate = null;
            try
            {
                X509KeyStorageFlags flags = X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet;
                if (Properties.Settings.Default.PrivateKeyExportable)
                {
                    Log.Debug("Set private key exportable");
                    flags |= X509KeyStorageFlags.Exportable;
                }

                // See http://paulstovell.com/blog/x509certificate2
                certificate = new X509Certificate2(pfxFilename, Properties.Settings.Default.PFXPassword, flags);

                certificate.FriendlyName = $"{binding.Host} {DateTime.Now.ToString(Properties.Settings.Default.FileDateFormat)}";
                Log.Debug("Adding certificate {FriendlyName} to store", certificate.FriendlyName);
                store.Add(certificate);
            }
            catch (Exception ex)
            {
                Log.Error("Error saving certificate {@ex}", ex);
            }
            Log.Debug("Closing certificate store");
            store.Close();
        }

        public static void UninstallCertificate(string host, out X509Store store, X509Certificate2 certificate)
        {
            try
            {
                store = new X509Store(_certificateStore, StoreLocation.LocalMachine);
                store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadWrite);
            }
            catch (CryptographicException)
            {
                store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadWrite);
            }
            catch (Exception ex)
            {
                Log.Error("Error encountered while opening certificate store. Error: {@ex}", ex);
                throw new Exception(ex.Message);
            }

            Log.Debug("Opened certificate store {Name}", store.Name);
            try
            {
                X509Certificate2Collection col = store.Certificates;
                foreach (var cert in col)
                {
                    if ((cert.Issuer.Contains("LE Intermediate") || cert.Issuer.Contains("Let's Encrypt")) && // Only delete Let's Encrypt certificates
                        cert.FriendlyName.StartsWith(host + " ") && // match by friendly name
                        cert.Thumbprint != certificate.Thumbprint) // don't delete the most recently installed one
                    {
                        Log.Information("Removing certificate {@cert}", cert.FriendlyName);
                        store.Remove(cert);
                    }
                }
                Log.Information("Closing certificate store");
            }
            catch (Exception ex)
            {
                Log.Error("Error removing certificate {@ex}", ex);
            }
            store.Close();
        }

        public static string GetCertificate(Target binding)
        {

            List<string> identifiers = new List<string>();
            if (!Options.San)
            {
                identifiers.Add(binding.Host);
            }
            identifiers.AddRange(binding.AlternativeNames);
            identifiers = identifiers.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
            if (identifiers.Count() == 0)
            {
                Log.Error("No DNS identifiers found.");
                throw new Exception("No DNS identifiers found.");
            }

            var identifier = identifiers.First();

            var cp = CertificateProvider.GetProvider();
            var rsaPkp = new RsaPrivateKeyParams();
            try
            {
                if (Properties.Settings.Default.RSAKeyBits >= 1024)
                {
                    rsaPkp.NumBits = Properties.Settings.Default.RSAKeyBits;
                    Log.Debug("RSAKeyBits: {RSAKeyBits}", Properties.Settings.Default.RSAKeyBits);
                }
                else
                {
                    Log.Warning(
                        "RSA Key Bits less than 1024 is not secure. Letting ACMESharp default key bits. http://openssl.org/docs/manmaster/crypto/RSA_generate_key_ex.html");
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Unable to set RSA Key Bits, Letting ACMESharp default key bits, Error: {@ex}", ex);
            }

            var rsaKeys = cp.GeneratePrivateKey(rsaPkp);
            var csrDetails = new CsrDetails()
            {
                CommonName = identifiers.FirstOrDefault(),
                AlternativeNames = identifiers
            };

            var csrParams = new CsrParams
            {
                Details = csrDetails
            };
            var csr = cp.GenerateCsr(csrParams, rsaKeys, Crt.MessageDigest.SHA256);

            byte[] derRaw;
            using (var bs = new MemoryStream())
            {
                cp.ExportCsr(csr, EncodingFormat.DER, bs);
                derRaw = bs.ToArray();
            }
            var derB64U = JwsHelper.Base64UrlEncode(derRaw);

            Log.Information($"Requesting certificate: {identifier}");
            var certRequ = _client.RequestCertificate(derB64U);

            Log.Debug("certRequ {@certRequ}", certRequ);
            Log.Debug("Request Status: {statusCode}", certRequ.StatusCode);

            if (certRequ.StatusCode == System.Net.HttpStatusCode.Created)
            {
                var keyGenFile = Path.Combine(_certificatePath, $"{identifier}-gen-key.json");
                var keyPemFile = Path.Combine(_certificatePath, $"{identifier}-key.pem");
                var csrGenFile = Path.Combine(_certificatePath, $"{identifier}-gen-csr.json");
                var csrPemFile = Path.Combine(_certificatePath, $"{identifier}-csr.pem");
                var crtDerFile = Path.Combine(_certificatePath, $"{identifier}-crt.der");
                var crtPemFile = Path.Combine(_certificatePath, $"{identifier}-crt.pem");
                var chainPemFile = Path.Combine(_certificatePath, $"{identifier}-chain.pem");
                string crtPfxFile = null;
                if (!Options.CentralSsl)
                {
                    crtPfxFile = Path.Combine(_certificatePath, $"{identifier}-all.pfx");
                }
                else
                {
                    crtPfxFile = Path.Combine(Options.CentralSslStore, $"{identifier}.pfx");
                }

                using (var fs = new FileStream(keyGenFile, FileMode.Create))
                    cp.SavePrivateKey(rsaKeys, fs);
                using (var fs = new FileStream(keyPemFile, FileMode.Create))
                    cp.ExportPrivateKey(rsaKeys, EncodingFormat.PEM, fs);
                using (var fs = new FileStream(csrGenFile, FileMode.Create))
                    cp.SaveCsr(csr, fs);
                using (var fs = new FileStream(csrPemFile, FileMode.Create))
                    cp.ExportCsr(csr, EncodingFormat.PEM, fs);

                Log.Information("Saving certificate to {crtDerFile}", crtDerFile);
                using (var file = File.Create(crtDerFile))
                    certRequ.SaveCertificate(file);

                Crt crt;
                using (FileStream source = new FileStream(crtDerFile, FileMode.Open),
                    target = new FileStream(crtPemFile, FileMode.Create))
                {
                    crt = cp.ImportCertificate(EncodingFormat.DER, source);
                    cp.ExportCertificate(crt, EncodingFormat.PEM, target);
                }

                // To generate a PKCS#12 (.PFX) file, we need the issuer's public certificate
                var isuPemFile = GetIssuerCertificate(certRequ, cp);

                using (FileStream intermediate = new FileStream(isuPemFile, FileMode.Open),
                    certificate = new FileStream(crtPemFile, FileMode.Open),
                    chain = new FileStream(chainPemFile, FileMode.Create))
                {
                    certificate.CopyTo(chain);
                    intermediate.CopyTo(chain);
                }

                Log.Debug($"CentralSsl {Options.CentralSsl} - San {Options.San}");

                if (Options.CentralSsl && Options.San)
                {
                    foreach (var host in identifiers)
                    {
                        Log.Debug($"Host: {host}");
                        crtPfxFile = Path.Combine(Options.CentralSslStore, $"{host}.pfx");

                        Log.Information("Saving certificate to {crtPfxFile}", crtPfxFile);
                        using (FileStream source = new FileStream(isuPemFile, FileMode.Open),
                            target = new FileStream(crtPfxFile, FileMode.Create))
                        {
                            try
                            {
                                var isuCrt = cp.ImportCertificate(EncodingFormat.PEM, source);
                                cp.ExportArchive(rsaKeys, new[] { crt, isuCrt }, ArchiveFormat.PKCS12, target,
                                    Properties.Settings.Default.PFXPassword);
                            }
                            catch (Exception ex)
                            {
                                Log.Error("Error exporting archive {@ex}", ex);
                            }
                        }
                    }
                }
                else //Central SSL and San need to save the cert for each hostname
                {
                    Log.Information("Saving certificate to {crtPfxFile}", crtPfxFile);
                    using (FileStream source = new FileStream(isuPemFile, FileMode.Open),
                        target = new FileStream(crtPfxFile, FileMode.Create))
                    {
                        try
                        {
                            var isuCrt = cp.ImportCertificate(EncodingFormat.PEM, source);
                            cp.ExportArchive(rsaKeys, new[] { crt, isuCrt }, ArchiveFormat.PKCS12, target,
                                Properties.Settings.Default.PFXPassword);
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Error exporting archive {@ex}", ex);
                        }
                    }
                }

                cp.Dispose();

                return crtPfxFile;
            }
            Log.Error("Request status = {StatusCode}", certRequ.StatusCode);
            throw new Exception($"Request status = {certRequ.StatusCode}");
        }

        public static void EnsureTaskScheduler()
        {
            var taskName = $"{ClientName} {CleanFileName(Options.BaseUri)}";
            using (var taskService = new TaskService())
            {
                var existingTask = taskService.GetTask(taskName);
                if (existingTask != null)
                {
                    if (!Input.PromptYesNo($"Do you want to replace the existing task?"))
                        return;

                    Log.Information("Deleting existing task {taskName} from Windows Task Scheduler.", taskName);
                    taskService.RootFolder.DeleteTask(taskName, false);
                }
   
                Log.Information("Creating task {taskName} with Windows Task scheduler at 9am every day.", taskName);
               
                // Create a new task definition and assign properties
                var task = taskService.NewTask();
                task.RegistrationInfo.Description = "Check for renewal of ACME certificates.";

                var now = DateTime.Now;
                var runtime = new DateTime(now.Year, now.Month, now.Day, 9, 0, 0);
                task.Triggers.Add(new DailyTrigger { DaysInterval = 1, StartBoundary = runtime });
                task.Settings.ExecutionTimeLimit = new TimeSpan(2, 0, 0);

                var currentExec = Assembly.GetExecutingAssembly().Location;

                // Create an action that will launch the app with the renew parameters whenever the trigger fires
                string actionString = $"--{nameof(Options.Renew).ToLowerInvariant()} --{nameof(Options.BaseUri).ToLowerInvariant()} \"{Options.BaseUri}\"";

                task.Actions.Add(new ExecAction(currentExec, actionString,
                    Path.GetDirectoryName(currentExec)));

                task.Principal.RunLevel = TaskRunLevel.Highest; // need admin
                Log.Debug("{@task}", task);

                if (!Options.UseDefaultTaskUser && Input.PromptYesNo($"Do you want to specify the user the task will run as?"))
                {
                    // Ask for the login and password to allow the task to run 
                    var username = Input.RequestString("Enter the username (Domain\\username)");
                    var password = Input.RequestString("Enter the user's password");
                    Log.Debug("Creating task to run as {username}", username);
                    taskService.RootFolder.RegisterTaskDefinition(
                        taskName, 
                        task, 
                        TaskCreation.Create, 
                        username,
                        password, 
                        TaskLogonType.Password);
                }
                else if (existingTask != null)
                {
                    Log.Debug("Creating task to run as previously chosen user.");
                    task.Principal.UserId = existingTask.Definition.Principal.UserId;
                    task.Principal.LogonType = existingTask.Definition.Principal.LogonType;
                    taskService.RootFolder.RegisterTaskDefinition(
                        taskName,
                        task,
                        TaskCreation.CreateOrUpdate,
                        null,
                        null,
                        existingTask.Definition.Principal.LogonType);
                }
                else
                {
                    Log.Debug("Creating task to run as system user.");
                    task.Principal.UserId = "SYSTEM";
                    task.Principal.LogonType = TaskLogonType.ServiceAccount;
                    taskService.RootFolder.RegisterTaskDefinition(
                        taskName,
                        task,
                        TaskCreation.CreateOrUpdate,
                        null,
                        null,
                        TaskLogonType.ServiceAccount);
                }
            }
        }


        public static void ScheduleRenewal(Target target)
        {
            if (!Options.NoTaskScheduler)
            {
                EnsureTaskScheduler();
            }

            var renewals = _settings.Renewals.ToList();
            foreach (var existing in from r in renewals.ToArray() where r.Binding.Host == target.Host select r)
            {
                Log.Debug("Removing existing scheduled renewal {existing}", existing);
                renewals.Remove(existing);
            }

            var result = new ScheduledRenewal()
            {
                Binding = target,
                CentralSsl = Options.CentralSslStore,
                San = Options.San.ToString(),
                Date = DateTime.UtcNow.AddDays(RenewalPeriod),
                KeepExisting = Options.KeepExisting.ToString(),
                Script = Options.Script,
                ScriptParameters = Options.ScriptParameters,
                Warmup = Options.Warmup,
                //AzureOptions = AzureOptions.From(Options)
            };
            renewals.Add(result);
            _settings.Renewals = renewals;

            Log.Information("Renewal scheduled {result}", result);

        }
        public static void CheckRenewals()
        {
            Log.Information("Checking renewals");

            var renewals = _settings.Renewals.ToList();
            if (renewals.Count == 0)
                Log.Warning("No scheduled renewals found.");

            var now = DateTime.UtcNow;
            foreach (var renewal in renewals)
                ProcessRenewal(renewals, now, renewal);
        }

        private static void ProcessRenewal(List<ScheduledRenewal> renewals, DateTime now, ScheduledRenewal renewal)
        {

            if (!Options.ForceRenewal)
            {
                Log.Information("Checking {renewal}", renewal.Binding.Host);
                if (renewal.Date >= now)
                {
                    Log.Information("Renewal for certificate {renewal} not scheduled", renewal.Binding.Host);
                    return;
                }
            }

            Log.Information("Renewing certificate for {renewal}", renewal.Binding.Host);
            Options.CentralSslStore = renewal.CentralSsl;
            Options.San = string.Equals(renewal.San, "true", StringComparison.InvariantCultureIgnoreCase);
            Options.KeepExisting = string.Equals(renewal.KeepExisting, "true", StringComparison.InvariantCultureIgnoreCase);
            Options.Script = renewal.Script;
            Options.ScriptParameters = renewal.ScriptParameters;
            Options.Warmup = renewal.Warmup;
            Options.WebRoot = renewal.Binding?.WebRootPath ?? Options.WebRootDefault;
            //if (renewal.AzureOptions != null)
            //{
            //    renewal.AzureOptions.ApplyOn(Options);
            //}
            //else
            //{
            //    new AzureOptions().ApplyOn(Options);
            //}
          
            try
            {
                renewal.Binding.Plugin.Renew(renewal.Binding);
                renewal.Date = DateTime.UtcNow.AddDays(RenewalPeriod);
                _settings.Renewals = renewals;
                Log.Information("Renewal scheduled {renewal}", renewal.Binding.Host);
            }
            catch
            {
                Log.Error("Renewal failed {renewal}, will retry on next run", renewal.Binding.Host);
            }
        }

        public static string GetIssuerCertificate(CertificateRequest certificate, CertificateProvider cp)
        {
            var linksEnum = certificate.Links;
            if (linksEnum != null)
            {
                var links = new LinkCollection(linksEnum);
                var upLink = links.GetFirstOrDefault("up");
                if (upLink != null)
                {
                    var temporaryFileName = Path.Combine(_certificatePath, $"crt.tmp");
                    try
                    {
                        using (var web = new WebClient())
                        {
                            var uri = new Uri(new Uri(Options.BaseUri), upLink.Uri);
                            web.DownloadFile(uri, temporaryFileName);
                        }

                        var cacert = new X509Certificate2(temporaryFileName);
                        var sernum = cacert.GetSerialNumberString();

                        var cacertDerFile = Path.Combine(_certificatePath, $"ca-{sernum}-crt.der");
                        var cacertPemFile = Path.Combine(_certificatePath, $"ca-{sernum}-crt.pem");

                        if (!File.Exists(cacertDerFile))
                            File.Copy(temporaryFileName, cacertDerFile, true);

                        Log.Information("Saving issuer certificate to {cacertPemFile}", cacertPemFile);
                        if (!File.Exists(cacertPemFile))
                            using (FileStream source = new FileStream(cacertDerFile, FileMode.Open),
                                target = new FileStream(cacertPemFile, FileMode.Create))
                            {
                                var caCrt = cp.ImportCertificate(EncodingFormat.DER, source);
                                cp.ExportCertificate(caCrt, EncodingFormat.PEM, target);
                            }

                        return cacertPemFile;
                    }
                    finally
                    {
                        if (File.Exists(temporaryFileName))
                            File.Delete(temporaryFileName);
                    }
                }
            }

            return null;
        }

        public static AuthorizationState Authorize(Target target)
        {
            List<string> identifiers = new List<string>();
            if (!Options.San)
            {
                identifiers.Add(target.Host);
            }
            identifiers.AddRange(target.AlternativeNames);
            identifiers = identifiers.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
            if (identifiers.Count() == 0)
            {
                Log.Error("No DNS identifiers found.");
                throw new Exception("No DNS identifiers found.");
            }
            List<AuthorizationState> authStatus = new List<AuthorizationState>();
            foreach (var dnsIdentifier in identifiers)
            {
                string answerUri;
                var challengeType = target.Plugin.ChallengeType;

                Log.Information("Authorizing identifier {dnsIdentifier} using {challengeType} challenge", dnsIdentifier, challengeType);
                var authzState = _client.AuthorizeIdentifier(dnsIdentifier);
                var challenge = _client.DecodeChallenge(authzState, challengeType);
                var cleanUp = challengeType == AcmeProtocol.CHALLENGE_TYPE_HTTP
                              ? PrepareHttpChallenge(target, challenge, out answerUri)
                              : PrepareDnsChallenge(target, challenge, out answerUri);

                try
                {
                    Log.Debug("Submitting answer");
                    authzState.Challenges = new AuthorizeChallenge[] { challenge };
                    _client.SubmitChallengeAnswer(authzState, challengeType, true);

                    // have to loop to wait for server to stop being pending.
                    // TODO: put timeout/retry limit in this loop
                    while (authzState.Status == "pending")
                    {
                        Log.Debug("Refreshing authorization");
                        Thread.Sleep(4000); // this has to be here to give ACME server a chance to think
                        var newAuthzState = _client.RefreshIdentifierAuthorization(authzState);
                        if (newAuthzState.Status != "pending")
                        {
                            authzState = newAuthzState;
                        }
                    }

                    Log.Information("Authorization result: {Status}", authzState.Status);
                    if (authzState.Status == "invalid")
                    {
                        //Log.Error("Authorization Failed {Status}", authzState.Status);
                        //Log.Debug("Full Error Details {@authzState}", authzState);
                        //Console.ForegroundColor = ConsoleColor.Red;
                        //Console.WriteLine("\n******************************************************************************");
                        //Log.Error("The ACME server was probably unable to reach {answerUri}", answerUri);
                        //Console.WriteLine("\nCheck in a browser to see if the answer file is being served correctly. If it is, also check the DNSSEC configuration.");
                        target.Plugin.OnAuthorizeFail(target);
                        //Console.ForegroundColor = ConsoleColor.Red;
                        //Console.WriteLine("\n******************************************************************************");
                        //Console.ResetColor();
                    }
                    authStatus.Add(authzState);
                }
                finally
                {
                    cleanUp(authzState);
                }
            }
            foreach (var authState in authStatus)
            {
                if (authState.Status != "valid")
                {
                    return authState;
                }
            }
            return new AuthorizationState { Status = "valid" };
        }

        private static Action<AuthorizationState> PrepareDnsChallenge(Target target, AuthorizeChallenge challenge, out string answerUri)
        {
            var dnsChallenge = challenge.Challenge as DnsChallenge;

            target.Plugin.CreateAuthorizationFile(dnsChallenge.RecordName, dnsChallenge.RecordValue);
            target.Plugin.BeforeAuthorize(target, dnsChallenge.RecordName, dnsChallenge.Token);
            answerUri = dnsChallenge.RecordName;

            Log.Information("Answer should now be available at {answerUri}", answerUri);

            return authzState =>
            {
                target.Plugin.DeleteAuthorization(dnsChallenge.RecordName, dnsChallenge.Token, null, null);
            };
        }
        private static Action<AuthorizationState> PrepareHttpChallenge(Target target, AuthorizeChallenge challenge, out string answerUri)
        {
            var webRootPath = Environment.ExpandEnvironmentVariables(target.WebRootPath);
            var httpChallenge = challenge.Challenge as HttpChallenge;
            var filePath = httpChallenge.FilePath.Replace('/', '\\');
            var answerPath = $"{webRootPath.TrimEnd('\\')}\\{filePath.TrimStart('\\')}";

            target.Plugin.CreateAuthorizationFile(answerPath, httpChallenge.FileContent);
            target.Plugin.BeforeAuthorize(target, answerPath, httpChallenge.Token);

            answerUri = httpChallenge.FileUrl;

            Log.Information("Answer should now be browsable at {answerUri}", answerUri);
            if (Options.Test)
            {
                if (Input.PromptYesNo("Try in default browser?"))
                {
                    Process.Start(answerUri);
                    Input.Wait();
                }
            }
            if (Options.Warmup)
            {
                Log.Information("Waiting for site to warmup...");
                WarmupSite(new Uri(answerUri));
            }

            return authzState =>
            {
                if (authzState.Status == "valid")
                {
                    target.Plugin.DeleteAuthorization(answerPath, httpChallenge.Token, webRootPath, filePath);
                }
            };
        }

        private static void WarmupSite(Uri uri)
        {
            var request = WebRequest.Create(uri);
            try
            {
                using (var response = request.GetResponse()) { }
            }
            catch (Exception ex)
            {
                Log.Error("Error warming up site: {@ex}", ex);
            }
        }
    }
}