using ACMESharp;
using Autofac;
using LetsEncrypt.ACME.Simple.Clients;
using LetsEncrypt.ACME.Simple.Extensions;
using LetsEncrypt.ACME.Simple.Plugins;
using LetsEncrypt.ACME.Simple.Plugins.InstallationPlugins;
using LetsEncrypt.ACME.Simple.Plugins.StorePlugins;
using LetsEncrypt.ACME.Simple.Plugins.TargetPlugins;
using LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins;
using LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins.Http;
using LetsEncrypt.ACME.Simple.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Threading;

namespace LetsEncrypt.ACME.Simple
{
    class Program
    {
        private const string _clientName = "letsencrypt-win-simple";
        private static LetsEncryptClient _client;
        private static ISettingsService _settings;
        private static IInputService _input;
        private static CertificateService _certificateService;
        private static RenewalService _renewalService;
        private static TaskSchedulerService _taskScheduler;
        private static IOptionsService _optionsService;
        private static Options _options;
        private static ILogService _log;
        private static PluginService _pluginService;

        public static IContainer Container;

        static bool IsElevated => new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
        static bool IsNET45 => Type.GetType("System.Reflection.ReflectionContext", false) != null;

        private static void Main(string[] args)
        {
            // Setup DI
            Container = AutofacBuilder.Global(args, _clientName);

            // Basic services
            _log = Container.Resolve<ILogService>();
            _optionsService = Container.Resolve<IOptionsService>();
            _options = _optionsService.Options;
            if (_options == null) return;
            _pluginService = Container.Resolve<PluginService>();
            _settings = Container.Resolve<ISettingsService>();
            _input = Container.Resolve<IInputService>();

            // .NET Framework check
            if (!IsNET45) {
                _log.Error(".NET Framework 4.5 or higher is required for this app");
                return;
            }

            // Show version information
            _input.ShowBanner();

            // Advanced services
            _client = Container.Resolve<LetsEncryptClient>();
            _certificateService = new CertificateService(_options, _log, _client, _settings.ConfigPath);
            _renewalService = new RenewalService(_settings, _input, _clientName, _settings.ConfigPath);
            _taskScheduler = new TaskSchedulerService(_options, _input, _log, _clientName);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            // Main loop
            do {
                try {
                    if (_options.Renew)
                    {
                        CheckRenewals();
                        CloseDefault();
                    }
                    else if (!string.IsNullOrEmpty(_options.Plugin))
                    {
                        CreateNewCertificateUnattended();
                        CloseDefault();
                    }
                    else
                    {
                        MainMenu();
                    }
                }
                catch (AcmeClient.AcmeWebException awe)
                {
                    Environment.ExitCode = awe.HResult;
                    _log.Debug("AcmeWebException {@awe}", awe);
                    _log.Error(awe, "ACME Server Returned: {acmeWebExceptionMessage} - Response: {acmeWebExceptionResponse}", awe.Message, awe.Response.ContentAsString);
                }
                catch (AcmeException ae)
                {
                    Environment.ExitCode = ae.HResult;
                    _log.Debug("AcmeException {@ae}", ae);
                    _log.Error(ae, "AcmeException {@ae}", ae.Message);
                }
                catch (Exception e)
                {
                    Environment.ExitCode = e.HResult;
                    _log.Debug("Exception {@e}", e);
                    _log.Error(e, "Exception {@e}", e.Message);
                }
                if (!_options.CloseOnFinish)
                {
                    _options.Plugin = null;
                    _options.Renew = false;
                    _options.ForceRenewal = false;
                    Environment.ExitCode = 0;
                }
            } while (!_options.CloseOnFinish);
        }

        /// <summary>
        /// Present user with the option to close the program
        /// Useful to keep the console output visible when testing
        /// unattended commands
        /// </summary>
        private static void CloseDefault()
        {
            if (_options.Test && !_options.CloseOnFinish)
            {
                _options.CloseOnFinish = _input.PromptYesNo("Quit?");
            }
            else
            {
                _options.CloseOnFinish = true;
            }
        }

        /// <summary>
        /// Main user experience
        /// </summary>
        private static void MainMenu()
        {
            var options = new List<Choice<Action>>();
            options.Add(Choice.Create<Action>(() => CreateNewCertificate(), "Create new certificate", "N"));
            options.Add(Choice.Create<Action>(() => {
                var target = _input.ChooseFromList("Show details for renewal?",
                    _renewalService.Renewals,
                    x => Choice.Create(x),
                    true);
                if (target != null)
                {
                    try
                    {
                        using (var scope = AutofacBuilder.Renewal(Container, _pluginService, target))
                        {
                            var resolver = scope.Resolve<Resolver>();
                            _input.Show("Name", target.Binding.Host, true);
                            _input.Show("AlternativeNames", string.Join(", ", target.Binding.AlternativeNames));
                            _input.Show("ExcludeBindings", target.Binding.ExcludeBindings);
                            _input.Show("Target plugin", resolver.GetTargetPlugin().Description);
                            _input.Show("Validation plugin", resolver.GetValidationPlugin().Description);
                            _input.Show("Store plugin", resolver.GetStorePlugin().Description);
                            _input.Show("Install plugin", resolver.GetInstallationPlugin().Description);
                            _input.Show("Renewal due", target.Date.ToUserString());
                            _input.Show("Script", target.Script);
                            _input.Show("ScriptParameters", target.ScriptParameters);
                            _input.Show("CentralSslStore", target.CentralSslStore);
                            _input.Show("KeepExisting", target.KeepExisting.ToString());
                            _input.Show("Warmup", target.Warmup.ToString());
                            _input.Show("Renewed", $"{target.History.Count} times");
                            _input.WritePagedList(target.History.Select(x => Choice.Create(x)));
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex, "Unable to list details for target");
                    }
                }
            }, "List scheduled renewals", "L"));

            options.Add(Choice.Create<Action>(() => {
                CheckRenewals();
            }, "Renew scheduled", "R"));

            options.Add(Choice.Create<Action>(() => {
                var target = _input.ChooseFromList("Which renewal would you like to run?",
                    _renewalService.Renewals,
                    x => Choice.Create(x),
                    true);
                if (target != null) {
                    ProcessRenewal(target);
                }
            }, "Renew specific", "S"));

            options.Add(Choice.Create<Action>(() => {
                _options.ForceRenewal = true;
                CheckRenewals();
                _options.ForceRenewal = false;
            }, "Renew *all*", "A"));

            options.Add(Choice.Create<Action>(() => {
                var target = _input.ChooseFromList("Which renewal would you like to cancel?",
                    _renewalService.Renewals, 
                    x => Choice.Create(x), 
                    true);

                if (target != null) {
                    if (_input.PromptYesNo($"Are you sure you want to delete {target}")) {
                        _renewalService.Renewals = _renewalService.Renewals.Except(new[] { target });
                        _log.Warning("Renewal {target} cancelled at user request", target);
                    }
                }
            }, "Cancel scheduled renewal", "C"));

            options.Add(Choice.Create<Action>(() => {
                ListRenewals();
                if (_input.PromptYesNo("Are you sure you want to delete all of these?")) {
                    _renewalService.Renewals = new List<ScheduledRenewal>();
                    _log.Warning("All scheduled renewals cancelled at user request");
                }
            }, "Cancel *all* scheduled renewals", "X"));

            options.Add(Choice.Create<Action>(() => {
                _options.CloseOnFinish = true;
                _options.Test = false;
            }, "Quit", "Q"));

            _input.ChooseFromList("Please choose from the menu", options, false).Invoke();
        }

        /// <summary>
        /// Create a new plug in unattended mode, triggered by the --plugin command line switch
        /// </summary>
        private static void CreateNewCertificateUnattended()
        {
            _log.Information(true, "Running in unattended mode.", _options.Plugin);

            // Choose target plugin 
            var targetPlugin = _pluginService.GetByName(_pluginService.Target, _options.Plugin);
            if (targetPlugin == null)
            {
                _log.Error("Target plugin {name} not found.", _options.Plugin);
                return;
            }

            // Generate target
            var target = targetPlugin.Default(_optionsService);
            if (target == null)
            {
                _log.Error("Plugin {name} was unable to generate a target", _options.Plugin);
                return;
            }
            else
            {
                _log.Information("Plugin {name} generated target {target}", _options.Plugin, target);
                target.TargetPluginName = targetPlugin.Name;
            }

            // Choose validation plugin
            IValidationPlugin validationPlugin = null;
            if (!string.IsNullOrWhiteSpace(_options.Validation))
            {
                validationPlugin = _pluginService.GetValidationPlugin($"{_options.ValidationMode}.{_options.Validation}");
                if (validationPlugin == null)
                {
                    _log.Error("Validation plugin {name} not found.", _options.Validation);
                    return;
                }
            }
            else
            {
                validationPlugin = _pluginService.GetByName(_pluginService.Validation, nameof(FileSystem));
            }
            target.ValidationPluginName = $"{validationPlugin.ChallengeType}.{validationPlugin.Name}";

            // Generate validation options
            try
            {
                validationPlugin.Default(_optionsService, target);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Invalid validation input");
                return;
            }

            // Run authorization and installation
            var result = Renew(CreateRenewal(target));
            if (!result.Success)
            {
                _log.Error("Create certificate {target} failed: {message}", target, result.ErrorMessage);
            }
        }

        /// <summary>
        /// Create initial renewal object
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        private static ScheduledRenewal CreateRenewal(Target target)
        {
            var renewal = _renewalService.Find(target);
            if (renewal == null)
            {
                renewal = new ScheduledRenewal();
            }
            renewal.New = true;
            renewal.Test = _options.Test;
            renewal.Binding = target;
            renewal.CentralSslStore = _options.CentralSslStore;
            renewal.KeepExisting = _options.KeepExisting;
            renewal.Script = _options.Script;
            renewal.ScriptParameters = _options.ScriptParameters;
            renewal.Warmup = _options.Warmup;
            return renewal;
        }

        /// <summary>
        /// Print a list of scheduled renewals
        /// </summary>
        private static void ListRenewals()
        {
            _input.WritePagedList(_renewalService.Renewals.Select(x => Choice.Create(x)));
        }

        /// <summary>
        /// Interactive creation of new certificate
        /// </summary>
        private static void CreateNewCertificate()
        {
            // List options for generating new certificates
            var targetPlugin = _input.ChooseFromList(
                "Which kind of certificate would you like to create?", 
                _pluginService.Target, 
                x => Choice.Create(x, description: x.Description),
                true);
            if (targetPlugin == null) return;

            var target = targetPlugin.Aquire(_optionsService, _input);
            if (target == null) {
                _log.Error("Plugin {Plugin} did not generate a target", targetPlugin.Name);
                return;
            } else {
                _log.Verbose("Plugin {Plugin} generated target {target}", targetPlugin.Name, target);
                target.TargetPluginName = targetPlugin.Name;
            }

            // Choose validation method
            var validationPlugin = _input.ChooseFromList(
                "How would you like to validate this certificate?",
                _pluginService.Validation.Where(x => x.CanValidate(target)),
                x => Choice.Create(x, description: $"[{x.ChallengeType}] {x.Description}"), 
                false);

            target.ValidationPluginName = $"{validationPlugin.ChallengeType}.{validationPlugin.Name}";
            validationPlugin.Aquire(_optionsService, _input, target);
            var result = Renew(CreateRenewal(target));
            if (!result.Success)
            {
                _log.Error("Create certificate {target} failed: {message}", target, result.ErrorMessage);
            }
        }
 
        public static RenewResult Renew(ScheduledRenewal renewal)
        {
            using (var scope = AutofacBuilder.Renewal(Container, _pluginService, renewal))
            {
                var targetPlugin = scope.Resolve<ITargetPlugin>();
                foreach (var target in targetPlugin.Split(renewal.Binding))
                {
                    var auth = Authorize(scope, renewal);
                    if (auth.Status != "valid")
                    {
                        return OnRenewFail(auth);
                    }
                }
                return OnRenewSuccess(scope, renewal);
            }
        }

        /// <summary>
        /// Steps to take on authorization failed
        /// </summary>
        /// <param name="auth"></param>
        /// <returns></returns>
        public static RenewResult OnRenewFail(AuthorizationState auth)
        {
            var errors = auth.Challenges.
                Select(c => c.ChallengePart).
                Where(cp => cp.Status == "invalid").
                SelectMany(cp => cp.Error);

            if (errors.Count() > 0)
            {
                _log.Error("ACME server reported:");
                foreach (var error in errors)
                {
                    _log.Error("[{_key}] {@value}", error.Key, error.Value);
                }
            }

            return new RenewResult(new AuthorizationFailedException(auth, errors.Select(x => x.Value)));
        }

        /// <summary>
        /// Steps to take on succesful (re)authorization
        /// </summary>
        /// <param name="target"></param>
        public static RenewResult OnRenewSuccess(ILifetimeScope scope, ScheduledRenewal renewal)
        {
            RenewResult result = null;
            try
            {
                var targetPlugin = scope.Resolve<ITargetPlugin>();
                var storePlugin = scope.Resolve<IStorePlugin>();
                var oldCertificate = renewal.Certificate(storePlugin);
                var newCertificate = _certificateService.RequestCertificate(renewal.Binding);
                result = new RenewResult(newCertificate);

                // Early escape for testing validation only
                if (_options.Test &&
                    renewal.New &&
                    !_input.PromptYesNo($"Do you want to save the certificate?"))
                    return result;

                // Save to store
                storePlugin.Save(newCertificate);

                if (!renewal.New ||
                    !_options.Test ||
                    _input.PromptYesNo($"Do you want to add/update the certificate to your server software?"))
                {
                    // Run installation plugin(s)
                    _log.Information("Installing SSL certificate in server software");
                    try
                    {
                        var installationPlugin = scope.Resolve<IInstallationPlugin>();
                        foreach (var subTarget in targetPlugin.Split(renewal.Binding))
                        {
                            var tempRenewal = renewal.Copy();
                            tempRenewal.Binding = subTarget;
                            installationPlugin.Install(tempRenewal, newCertificate, oldCertificate);
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex, "Unable to install certificate");
                        result.Success = false;
                        result.ErrorMessage = $"Install failed: {ex.Message}";
                    }

                    // Delete the old certificate if specified and found
                    if (!renewal.KeepExisting && oldCertificate != null)
                    {
                        try
                        {
                            storePlugin.Delete(oldCertificate);
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex, "Unable to delete previous certificate");
                            //result.Success = false; // not a show-stopper, consider the renewal a success
                            result.ErrorMessage = $"Delete failed: {ex.Message}";
                        }
                    }
                }

                // Add or update renewal
                if (renewal.New &&
                    !_options.NoTaskScheduler &&
                    (!_options.Test ||
                    _input.PromptYesNo($"Do you want to automatically renew this certificate in {_renewalService.RenewalPeriod} days? This will add a task scheduler task.")))
                {
                    _taskScheduler.EnsureTaskScheduler();
                    _renewalService.Save(renewal, result);
                }
                return result;
            }
            catch (Exception ex)
            {
                // Result might still contain the Thumbprint of the certificate 
                // that was requested and (partially? installed, which might help
                // with debugging
                _log.Error(ex, "Unknown failure");
                if (result == null)
                {
                    result = new RenewResult(ex);
                }
                else
                {
                    result.Success = false;
                    result.ErrorMessage = ex.Message;
                }            
            }
            return result;
        }

        /// <summary>
        /// Loop through the store renewals and run those which are
        /// due to be run
        /// </summary>
        public static void CheckRenewals()
        {
            _log.Verbose("Checking renewals");

            var renewals = _renewalService.Renewals.ToList();
            if (renewals.Count == 0)
                _log.Warning("No scheduled renewals found.");

            var now = DateTime.UtcNow;
            foreach (var renewal in renewals)
            {
                if (_options.ForceRenewal)
                {
                    ProcessRenewal(renewal);
                }
                else
                {
                    _log.Verbose("Checking {renewal}", renewal.Binding.Host);
                    if (renewal.Date >= now)
                    {
                        _log.Information("Renewal for certificate {renewal} not scheduled, due after {date}", renewal.Binding.Host, renewal.Date.ToUserString());
                        return;
                    }
                    else
                    {
                        ProcessRenewal(renewal);
                    }
                }              
            }
        }

        /// <summary>
        /// Process a single renewal
        /// </summary>
        /// <param name="renewal"></param>
        private static void ProcessRenewal(ScheduledRenewal renewal)
        {
            _log.Information(true, "Renewing certificate for {renewal}", renewal.Binding.Host);
            try
            {
                // Let the plugin run
                var result = Renew(renewal);
                _renewalService.Save(renewal, result);
            }
            catch
            {
                _log.Error("Renewal for {host} failed, will retry on next run", renewal.Binding.Host);
            }
        }

        /// <summary>
        /// Make sure we have authorization for every host in target
        /// </summary>
        /// <param name="renewal"></param>
        /// <returns></returns>
        public static AuthorizationState Authorize(ILifetimeScope scope, ScheduledRenewal renewal)
        {
            List<string> identifiers = renewal.Binding.GetHosts(false);
            List<AuthorizationState> authStatus = new List<AuthorizationState>();
            foreach (var identifier in identifiers)
            {
                var authzState = _client.Acme.AuthorizeIdentifier(identifier);
                if (authzState.Status == "valid" && !_options.Test)
                {
                    _log.Information("Cached authorization result: {Status}", authzState.Status);
                    authStatus.Add(authzState);
                }
                else
                {
                    var validation = scope.Resolve<IValidationPlugin>();
                    if (validation == null)
                    {
                        return new AuthorizationState { Status = "invalid" };
                    }
                    _log.Information("Authorizing {dnsIdentifier} using {challengeType} validation ({name})", identifier, validation.ChallengeType, validation.Name);
                    var challenge = _client.Acme.DecodeChallenge(authzState, validation.ChallengeType);
                    var cleanUp = validation.PrepareChallenge(renewal, challenge, identifier);

                    try
                    {
                        _log.Debug("Submitting answer");
                        authzState.Challenges = new AuthorizeChallenge[] { challenge };
                        _client.Acme.SubmitChallengeAnswer(authzState, validation.ChallengeType, true);

                        // have to loop to wait for server to stop being pending.
                        // TODO: put timeout/retry limit in this loop
                        while (authzState.Status == "pending")
                        {
                            _log.Debug("Refreshing authorization");
                            Thread.Sleep(4000); // this has to be here to give ACME server a chance to think
                            var newAuthzState = _client.Acme.RefreshIdentifierAuthorization(authzState);
                            if (newAuthzState.Status != "pending")
                            {
                                authzState = newAuthzState;
                            }
                        }

                        _log.Information("Authorization result: {Status}", authzState.Status);
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