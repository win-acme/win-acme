using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Threading;
using ACMESharp;
using ACMESharp.JOSE;
using CommandLine;
using LetsEncrypt.ACME.Simple.Services;
using static LetsEncrypt.ACME.Simple.Services.InputService;
using LetsEncrypt.ACME.Simple.Extensions;
using LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins;
using LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins.Http;
using System.Security.Cryptography.X509Certificates;
namespace LetsEncrypt.ACME.Simple
{
    class Program
    {
        private const string _clientName = "letsencrypt-win-simple";
        private static float _renewalPeriod = 60;
        private static string _configPath;
        private static AcmeClient _client;
        private static Settings _settings;
        private static InputService _input;
        private static TaskSchedulerService _taskScheduler;
        private static CertificateService _certificateService;

        public static Options Options;
        public static LogService Log;
        public static PluginService Plugins;

        static bool IsElevated => new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
        static bool IsNET45 => Type.GetType("System.Reflection.ReflectionContext", false) != null;

        private static void Main(string[] args)
        {
            Log = new LogService();
            if (!TryParseOptions(args)) {
                return;
            }
            if (Options.Verbose) {
                Log.SetVerbose();
            }
            if (Options.Test) {
                SetTestParameters();
            }

            Plugins = new PluginService();
            ParseRenewalPeriod();
            ParseCentralSslStore();
            CreateConfigPath();

            // Basic services
            _settings = new Settings(Log, _clientName, Options.BaseUri);
            _input = new InputService(Options, Log, _settings.HostsPerPage());
            _taskScheduler = new TaskSchedulerService(Options, _input, Log, _clientName);

            // .NET Framework check
            if (!IsNET45) {
                Log.Error(".NET Framework 4.5 or higher is required for this app");
                return;
            }

            // Configure AcmeClient
            var signer = new RS256Signer();
            signer.Init();
            _client = new AcmeClient(new Uri(Options.BaseUri), new AcmeServerDirectory(), signer);
            ConfigureAcmeClient(_client);
            _certificateService = new CertificateService(Options, Log, _client, _configPath);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            _input.ShowBanner();

            if (Options.ForceRenewal) {
                Options.Renew = true;
            }
            bool retry = false;
            do {
                try {
                    if (Options.Renew) {
                        CheckRenewals();
                    } else if (!string.IsNullOrEmpty(Options.Plugin)) {
                        CreateNewCertifcateUnattended();
                    } else {
                        MainMenu();
                    }
                    retry = false; // Success, no exceptions
                } catch (AcmeClient.AcmeWebException awe) {
                    Environment.ExitCode = awe.HResult;
                    Log.Debug("AcmeWebException {@awe}", awe);
                    Log.Error("ACME Server Returned: {acmeWebExceptionMessage} - Response: {acmeWebExceptionResponse}", awe.Message, awe.Response.ContentAsString);
                } catch (AcmeException ae) {
                    Environment.ExitCode = ae.HResult;
                    Log.Debug("AcmeException {@ae}", ae);
                    Log.Error("AcmeException {@ae}", ae.Message);
                } catch (Exception e) {
                    Environment.ExitCode = e.HResult;
                    Log.Debug("Exception {@e}", e);
                    Log.Error("Exception {@e}", e.Message);
                }
                if (!Options.CloseOnFinish && (!Options.Renew || Options.Test)) {
                    Environment.ExitCode = 0;
                    retry = true;
                }
            } while (retry);
        }

        /// <summary>
        /// Main user experience
        /// </summary>
        private static void MainMenu()
        {
            var options = new List<Choice<Action>>();
            options.Add(Choice.Create<Action>(() => CreateNewCertificate(), "Create new certificate", "N"));
            options.Add(Choice.Create<Action>(() => ListRenewals(), "List scheduled renewals", "L"));

            options.Add(Choice.Create<Action>(() => {
                Options.Renew = true;
                CheckRenewals();
                Options.Renew = false;
            }, "Renew scheduled", "R"));

            options.Add(Choice.Create<Action>(() => {
                var target = _input.ChooseFromList("Which renewal would you like to run?",
                    _settings.Renewals,
                    x => Choice.Create(x),
                    true);
                if (target != null) {
                    Options.Renew = true;
                    Options.ForceRenewal = true;
                    ProcessRenewal(_settings.Renewals.ToList(), DateTime.Now, target);
                    Options.Renew = false;
                    Options.ForceRenewal = false;
                }
            }, "Renew specific", "S"));

            options.Add(Choice.Create<Action>(() => {
                Options.Renew = true;
                Options.ForceRenewal = true;
                CheckRenewals();
                Options.Renew = false;
                Options.ForceRenewal = false;
            }, "Renew *all*", "A"));

            options.Add(Choice.Create<Action>(() => {
                var target = _input.ChooseFromList("Which renewal would you like to cancel?", 
                    _settings.Renewals, 
                    x => Choice.Create(x), 
                    true);

                if (target != null) {
                    if (_input.PromptYesNo($"Are you sure you want to delete {target}")) {
                        _settings.Renewals = _settings.Renewals.Except(new[] { target });
                        Log.Warning("Renewal {target} cancelled at user request", target);
                    }
                }
            }, "Cancel scheduled renewal", "C"));

            options.Add(Choice.Create<Action>(() => {
                ListRenewals();
                if (_input.PromptYesNo("Are you sure you want to delete all of these?")) {
                    _settings.Renewals = new List<ScheduledRenewal>();
                    Log.Warning("All scheduled renewals cancelled at user request");
                }
            }, "Cancel *all* scheduled renewals", "X"));

            options.Add(Choice.Create<Action>(() => {
                Options.CloseOnFinish = true;
                Options.Test = false;
            }, "Quit", "Q"));

            _input.ChooseFromList("Please choose from the menu", options, false).Invoke();
        }

        /// <summary>
        /// Create a new plug in unattended mode, triggered by the --plugin command line switch
        /// </summary>
        private static void CreateNewCertifcateUnattended()
        {
            Log.Information(true, "Running in unattended mode.", Options.Plugin);
            Options.CloseOnFinish = true;

            var targetPlugin = Plugins.GetByName(Plugins.Target, Options.Plugin);
            if (targetPlugin == null)
            {
                Log.Error("Target plugin {name} not found.", Options.Plugin);
                return;
            }

            var target = targetPlugin.Default(Options);
            if (target == null)
            {
                Log.Error("Plugin {name} was unable to generate a target", Options.Plugin);
                return;
            }

            IValidationPlugin validationPlugin = null;
            if (!string.IsNullOrWhiteSpace(Options.Validation))
            {
                validationPlugin = Plugins.GetByName(Plugins.Validation, Options.Validation);
                if (validationPlugin == null)
                {
                    Log.Error("Validation plugin {name} not found.", Options.Validation);
                    return;
                }
            }
            else
            {
                validationPlugin = Plugins.GetByName(Plugins.Validation, nameof(FileSystem));
            }
            target.ValidationPluginName = $"{validationPlugin.ChallengeType}.{validationPlugin.Name}";
            validationPlugin.Default(Options, target);
            target.Plugin.Auto(target);
        }

        /// <summary>
        /// Print a list of scheduled renewals
        /// </summary>
        private static void ListRenewals()
        {
            _input.WritePagedList(_settings.Renewals.Select(x => Choice.Create(x)));
        }

        /// <summary>
        /// Interactive creation of new certificate
        /// </summary>
        private static void CreateNewCertificate()
        {
            // List options for generating new certificates
            var targetPlugin = _input.ChooseFromList(
                "Which kind of certificate would you like to create?", 
                Plugins.Target, 
                x => Choice.Create(x, description: x.Description),
                true);
            if (targetPlugin == null) return;

            var target = targetPlugin.Aquire(Options, _input);
            if (target == null) {
                Log.Error("Plugin {Plugin} did not generate a target", targetPlugin.Name);
                return;
            } else {
                Log.Verbose("Plugin {Plugin} generated target {target}", targetPlugin.Name, target);
            }

            // Choose validation method
            var validationPlugin = _input.ChooseFromList(
                "How would you like to validate this certificate?",
                Plugins.Validation.Where(x => x.CanValidate(target)),
                x => Choice.Create(x, description: $"[{x.ChallengeType}] {x.Description}"), 
                false);

            target.ValidationPluginName = $"{validationPlugin.ChallengeType}.{validationPlugin.Name}";
            validationPlugin.Aquire(Options, _input, target);
            target.Plugin.Auto(target);
        }

        private static bool TryParseOptions(string[] args)
        {
            try
            {
                var commandLineParseResult = Parser.Default.ParseArguments<Options>(args).
                    WithNotParsed((errors) =>
                    {
                        foreach (var error in errors)
                        {
                            switch (error.Tag)
                            {
                                case ErrorType.UnknownOptionError:
                                    var unknownOption = (UnknownOptionError)error;
                                    var token = unknownOption.Token.ToLower();
                                    Log.Error("Unknown argument: {tag}", token);
                                    break;
                                case ErrorType.HelpRequestedError:
                                case ErrorType.VersionRequestedError:
                                    break;
                                default:
                                    Log.Error("Argument error: {tag}", error.Tag);
                                    break;
                            }
                        }
                    }).
                    WithParsed((result) =>
                    {
                        Options = result;
                        Log.Debug("Options: {@Options}", Options);
                    });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed while parsing options.");
            }
            return Options != null;
        }

        public static IWebProxy GetWebProxy()
        {
            var system = "[System]";
            var useSystem = Properties.Settings.Default.Proxy.Equals(system, StringComparison.OrdinalIgnoreCase);

            var proxy = string.IsNullOrWhiteSpace(Properties.Settings.Default.Proxy) 
                ? null 
                : useSystem
                    ? WebRequest.GetSystemWebProxy() 
                    : new WebProxy(Properties.Settings.Default.Proxy);

            if (proxy != null)
            {
                Uri testUrl = new Uri("http://proxy.example.com");
                Uri proxyUrl = proxy.GetProxy(testUrl);
                bool useProxy = !string.Equals(testUrl.Host, proxyUrl.Host);
                if (useProxy)
                {
                    Log.Warning("Proxying via {proxy}", proxyUrl.Host);
                }
            } 
            return proxy;
         }
 
        private static void ConfigureAcmeClient(AcmeClient client)
        {
            client.Proxy = GetWebProxy();

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
                    email = _input.RequestString("Enter an email address (not public, used for renewal fail notices)");
                }

                string[] contacts = GetContacts(email);

                AcmeRegistration registration = CreateRegistration(contacts);

                if (!Options.AcceptTos && !Options.Renew)
                {
                    if (!_input.PromptYesNo($"Do you agree to {registration.TosLinkUri}?"))
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
            Log.SetVerbose();
            Log.Debug("Test parameter set: {BaseUri}", Options.BaseUri);
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

        private static void LoadSignerFromFile(ISigner signer, string signerPath)
        {
            Log.Debug("Loading signer from {signerPath}", signerPath);
            using (var signerStream = File.OpenRead(signerPath))
                signer.Load(signerStream);
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
                if (!Directory.Exists(Path.Combine(configBasePath, _clientName)))
                {
                    configBasePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                }
            }

            _configPath = Path.Combine(configBasePath, _clientName, Options.BaseUri.CleanFileName());
            Log.Debug("Config folder: {_configPath}", _configPath);
            Directory.CreateDirectory(_configPath);
        }

        private static void ParseCentralSslStore()
        {
            if (Options.CentralSsl)
            {
                Log.Debug("Using Centralized SSL path: {CentralSslStore}", Options.CentralSslStore);
            }
        }

        private static void ParseRenewalPeriod()
        {
            try
            {
                _renewalPeriod = Properties.Settings.Default.RenewalDays;
                Log.Debug("Renewal period: {RenewalPeriod}", _renewalPeriod);
            }
            catch (Exception ex)
            {
                Log.Warning("Error reading RenewalDays from app config, defaulting to {RenewalPeriod} Error: {@ex}", _renewalPeriod.ToString(), ex);
            }
        }

        public static void Auto(Target binding)
        {
            try
            {
                var auth = Authorize(binding);
                if (auth.Status == "valid")
                {
                    OnAutoSuccess(binding);
                }
                else
                {
                    OnAutoFail(auth);
                }
            }
            catch (AcmeException)
            {
                // Might want to do some logging/debugging here...
                throw;
            }
        }

        public static void OnAutoFail(AuthorizationState auth)
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

        /// <summary>
        /// Steps to take on succesful (re)authorization
        /// </summary>
        /// <param name="binding"></param>
        public static void OnAutoSuccess(Target binding)
        {
            var store = _certificateService.DefaultStore;
            var oldCertificate = _certificateService.GetCertificate(binding, store);
            var newCertificate = _certificateService.RequestCertificate(binding);
            var newCertificatePfx = new FileInfo(_certificateService.PfxFilePath(binding));
          
            if (Options.Test &&
                !Options.Renew &&
                !_input.PromptYesNo($"Do you want to install the certificate?"))
                return;

            SaveCertificate(binding.GetHosts(true), newCertificate, newCertificatePfx, store);

            if (Options.Test &&
                !Options.Renew &&
                _input.PromptYesNo($"Do you want to add/update the certificate to your server software?"))
            {
                Log.Information("Installing SSL certificate in server software");
                if (Options.CentralSsl)
                {
                    binding.Plugin.Install(binding);
                }
                else
                {
                    binding.Plugin.Install(binding, newCertificatePfx.FullName, store, newCertificate);
                }
                if (!Options.KeepExisting && oldCertificate != null)
                {
                    DeleteCertificate(oldCertificate.Thumbprint, store);
                }
            }

            if (Options.Test &&
                !Options.Renew &&
                _input.PromptYesNo($"Do you want to automatically renew this certificate in {_renewalPeriod} days? This will add a task scheduler task."))
            {
                Log.Information("Adding renewal for {binding}", binding);
                ScheduleRenewal(binding);
            }
        }

        /// <summary>
        /// Save certificate in the right place, either to the certifcate store 
        /// or to the central ssl store
        /// </summary>
        /// <param name="bindings">For which bindings is this certificate meant</param>
        /// <param name="certificate">The certificate itself</param>
        /// <param name="certificatePfx">The location of the PFX file in the local filesystem.</param>
        /// <param name="store">Certificate store to use when saving to one</param>
        public static void SaveCertificate(List<string> bindings, X509Certificate2 certificate, FileInfo certificatePfx = null, X509Store store = null)
        {
            if (Options.CentralSsl)
            {
                if (certificatePfx == null || certificatePfx.Exists == false)
                {
                    // PFX doesn't exist yet, let's create one
                    certificatePfx = new FileInfo(_certificateService.PfxFilePath(bindings.First()));
                    File.WriteAllBytes(certificatePfx.FullName, certificate.Export(X509ContentType.Pfx));
                }

                foreach (var identifier in bindings)
                {
                    var dest = Path.Combine(Options.CentralSslStore, $"{identifier}.pfx");
                    Log.Information("Saving certificate to Central SSL location {dest}", dest);
                    try
                    {
                        File.Copy(certificatePfx.FullName, dest, !Options.KeepExisting);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error copying certificate to Central SSL store");
                    }
                }
            }
            else
            {
                Log.Information("Installing certificate in the certificate store");
                _certificateService.InstallCertificate(certificate, store);
            }
        }

        public static void DeleteCertificate(string thumbprint, X509Store store = null)
        {
            if (!Options.CentralSsl)
            {
                _certificateService.UninstallCertificate(thumbprint, store);
            }
            else
            {
                var di = new DirectoryInfo(Options.CentralSslStore);
                foreach (var fi in di.GetFiles("*.pfx"))
                {
                    var cert = new X509Certificate2(fi.FullName);
                    if (string.Equals(cert.Thumbprint, thumbprint, StringComparison.InvariantCultureIgnoreCase))
                    {
                        fi.Delete();
                    }
                }
            }
        }

        public static void ScheduleRenewal(Target target)
        {
            if (!Options.NoTaskScheduler)
            {
                _taskScheduler.EnsureTaskScheduler();
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
                Date = DateTime.UtcNow.AddDays(_renewalPeriod),
                KeepExisting = Options.KeepExisting.ToString(),
                Script = Options.Script,
                ScriptParameters = Options.ScriptParameters,
                Warmup = Options.Warmup
            };
            renewals.Add(result);
            _settings.Renewals = renewals;

            Log.Information("Renewal scheduled {result}", result);

        }

        public static void CheckRenewals()
        {
            Log.Verbose("Checking renewals");

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
                Log.Verbose("Checking {renewal}", renewal.Binding.Host);
                if (renewal.Date >= now)
                {
                    Log.Information("Renewal for certificate {renewal} not scheduled", renewal.Binding.Host);
                    return;
                }
            }

            Log.Information(true, "Renewing certificate for {renewal}", renewal.Binding.Host);
            Options.CentralSslStore = renewal.CentralSsl;
            Options.KeepExisting = string.Equals(renewal.KeepExisting, "true", StringComparison.InvariantCultureIgnoreCase);
            Options.Script = renewal.Script;
            Options.ScriptParameters = renewal.ScriptParameters;
            Options.Warmup = renewal.Warmup;
            try
            {
                renewal.Binding.Plugin.Auto(renewal.Binding);
                renewal.Date = DateTime.UtcNow.AddDays(_renewalPeriod);
                _settings.Renewals = renewals;
                Log.Information(true, "Renewal for {host} succeeded, rescheduled for {date}", renewal.Binding.Host, renewal.Date.ToString(Properties.Settings.Default.FileDateFormat));
            }
            catch
            {
                Log.Error("Renewal for {host} failed, will retry on next run", renewal.Binding.Host);
            }
        }

        public static AuthorizationState Authorize(Target target)
        {
            List<string> identifiers = target.GetHosts(false);
            List<AuthorizationState> authStatus = new List<AuthorizationState>();
            foreach (var identifier in identifiers)
            {
                var authzState = _client.AuthorizeIdentifier(identifier);
                if (authzState.Status == "valid" && !Options.Test)
                {
                    Log.Information("Cached authorization result: {Status}", authzState.Status);
                    authStatus.Add(authzState);
                }
                else
                {
                    var validation = target.GetValidationPlugin();
                    Log.Information("Authorizing {dnsIdentifier} using {challengeType} validation ({name})", identifier, validation.ChallengeType, validation.Name);
                    var challenge = _client.DecodeChallenge(authzState, validation.ChallengeType);
                    var cleanUp = validation.PrepareChallenge(target, challenge, identifier, Options, _input);

                    try
                    {
                        Log.Debug("Submitting answer");
                        authzState.Challenges = new AuthorizeChallenge[] { challenge };
                        _client.SubmitChallengeAnswer(authzState, validation.ChallengeType, true);

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
                        authStatus.Add(authzState);
                    }
                    finally
                    {
                        cleanUp(authzState);
                    }
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

    }
}