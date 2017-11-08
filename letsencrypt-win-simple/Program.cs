using ACMESharp;
using Autofac;
using LetsEncrypt.ACME.Simple.Clients;
using LetsEncrypt.ACME.Simple.Extensions;
using LetsEncrypt.ACME.Simple.Plugins;
using LetsEncrypt.ACME.Simple.Plugins.InstallationPlugins;
using LetsEncrypt.ACME.Simple.Plugins.StorePlugins;
using LetsEncrypt.ACME.Simple.Plugins.TargetPlugins;
using LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins;
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
        private static IInputService _input;
        private static RenewalService _renewalService;
        private static Options _options;
        private static ILogService _log;
        private static IContainer _container;

        static bool IsElevated => new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

        private static void Main(string[] args)
        {
            // Setup DI
            _container = AutofacBuilder.Global(args, _clientName, new PluginService());

            // Basic services
            _log = _container.Resolve<ILogService>();
            _options = _container.Resolve<IOptionsService>().Options;
            if (_options == null) return;
            _input = _container.Resolve<IInputService>();

            // .NET Framework check
            var dn = _container.Resolve<GetDotNetVersionService>();
            if (!dn.Check()) {
                return;
            }

            // Show version information
            _input.ShowBanner();

            // Advanced services
            _renewalService = _container.Resolve<RenewalService>();
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
                catch (Exception e)
                {
                    HandleException(e);
                    Environment.ExitCode = e.HResult;
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

        private static void HandleException(Exception ex)
        {
            _log.Debug($"{ex.GetType().Name}: {{@e}}", ex);
            _log.Error($"{ex.GetType().Name}: {{e}}", ex.Message);
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
                        using (var scope = AutofacBuilder.Renewal(_container, target, false))
                        {
                            var resolver = scope.Resolve<UnattendedResolver>();
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
            var tempRenewal = CreateRenewal(_options);
            using (var scope = AutofacBuilder.Renewal(_container, tempRenewal, false))
            {
                var targetPluginFactory = scope.Resolve<ITargetPluginFactory>();
                if (targetPluginFactory is INull)
                {
                    return;
                }
                var targetPlugin = scope.Resolve<ITargetPlugin>();
                var target = targetPlugin.Default();
                tempRenewal.Binding = target;
                if (target == null)
                {
                    _log.Error("Target plugin {name} was unable to generate a target", targetPluginFactory.Name);
                    return;
                }
                else
                {
                    tempRenewal.Binding.TargetPluginName = targetPluginFactory.Name;
                    _log.Information("Target plugin {name} generated {target}", targetPluginFactory.Name, tempRenewal.Binding);
                }

                var validationPluginFactory = scope.Resolve<IValidationPluginFactory>();
                if (validationPluginFactory is INull)
                {
                    return;
                }
                if (!validationPluginFactory.CanValidate(target))
                {
                    _log.Error("Validation plugin {name} is unable to validate target", validationPluginFactory.Name);
                    return;
                }
                var validationPlugin = scope.Resolve<IValidationPlugin>();
                try
                {
                    validationPlugin.Default(target);
                    tempRenewal.Binding.ValidationPluginName = $"{_options.ValidationMode}.{_options.Validation}";
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Invalid validation input");
                    return;
                }
                var result = Renew(scope, CreateRenewal(tempRenewal));
                if (!result.Success)
                {
                    _log.Error("Create certificate failed");
                }
            }       
        }

        /// <summary>
        /// Create new ScheduledRenewal from the options
        /// </summary>
        /// <returns></returns>
        private static ScheduledRenewal CreateRenewal(Options options)
        {
            return new ScheduledRenewal
            {
                Binding = new Target
                {
                    TargetPluginName = options.Plugin,
                    ValidationPluginName = $"{options.ValidationMode}.{options.Validation}"
                },
                New = true,
                Test = options.Test,
                CentralSslStore = options.CentralSslStore,
                KeepExisting = options.KeepExisting,
                Script = options.Script,
                ScriptParameters = options.ScriptParameters,
                Warmup = options.Warmup
            };
        }

        /// <summary>
        /// If renewal is already Scheduled, replace it with the new options
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        private static ScheduledRenewal CreateRenewal(ScheduledRenewal temp)
        {
            var renewal = _renewalService.Find(temp.Binding);
            if (renewal == null)
            {
                renewal = temp;
            }
            renewal.New = true;
            renewal.Test = temp.Test;
            renewal.Binding = temp.Binding;
            renewal.CentralSslStore = temp.CentralSslStore;
            renewal.KeepExisting = temp.KeepExisting;
            renewal.Script = temp.Script;
            renewal.ScriptParameters = temp.ScriptParameters;
            renewal.Warmup = temp.Warmup;
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
            var tempRenewal = CreateRenewal(_options);
            using (var scope = AutofacBuilder.Renewal(_container, tempRenewal, true))
            {
                // Choose target plugin
                var targetPluginFactory = scope.Resolve<ITargetPluginFactory>();
                if (targetPluginFactory is INull)
                {
                    return; // User cancelled
                }
                else
                {
                    tempRenewal.Binding.TargetPluginName = targetPluginFactory.Name;
                }

                // Aquire target
                var targetPlugin = scope.Resolve<ITargetPlugin>();
                var target = targetPlugin.Aquire();
                tempRenewal.Binding = target;
                if (target == null)
                {
                    _log.Error("Plugin {name} was unable to generate a target", targetPluginFactory.Name);
                    return;
                }
                else
                {
                    _log.Information("Plugin {name} generated target {target}", targetPluginFactory.Name, tempRenewal.Binding);
                }
                
                // Choose validation plugin
                var validationPluginFactory = scope.Resolve<IValidationPluginFactory>();
                if (validationPluginFactory is INull)
                {
                   
                    return; // User cancelled
                }
                else
                {
                    tempRenewal.Binding.ValidationPluginName = $"{_options.ValidationMode}.{_options.Validation}";
                }

                // Configure validation
                try
                {
                    var validationPlugin = scope.Resolve<IValidationPlugin>();
                    validationPlugin.Aquire(target);
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Invalid validation input");
                    return;
                }
           
                var result = Renew(scope, CreateRenewal(tempRenewal));
                if (!result.Success)
                {
                    _log.Error("Create certificate failed");
                }
            }
        }
 
        private static RenewResult Renew(ScheduledRenewal renewal)
        {
            using (var scope = AutofacBuilder.Renewal(_container, renewal, false))
            {
                return Renew(scope, renewal);
            }
        }

        private static RenewResult Renew(ILifetimeScope scope, ScheduledRenewal renewal)
        {
            var targetPlugin = scope.Resolve<ITargetPlugin>();
            renewal.Binding = targetPlugin.Refresh(renewal.Binding);
            if (renewal.Binding == null)
            {
                _log.Error("Renewal target not found");
                return new RenewResult(new Exception("Renewal target not found"));
            }
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

        /// <summary>
        /// Steps to take on authorization failed
        /// </summary>
        /// <param name="auth"></param>
        /// <returns></returns>
        public static RenewResult OnRenewFail(AuthorizationState auth)
        {
            var errors = auth.Challenges?.
                Select(c => c.ChallengePart).
                Where(cp => cp.Status == "invalid").
                SelectMany(cp => cp.Error);

            if (errors?.Count() > 0)
            {
                _log.Error("ACME server reported:");
                foreach (var error in errors)
                {
                    _log.Error("[{_key}] {@value}", error.Key, error.Value);
                }
            }

            return new RenewResult(new AuthorizationFailedException(auth, errors?.Select(x => x.Value)));
        }

        /// <summary>
        /// Steps to take on succesful (re)authorization
        /// </summary>
        /// <param name="target"></param>
        private static RenewResult OnRenewSuccess(ILifetimeScope scope, ScheduledRenewal renewal)
        {
            RenewResult result = null;
            try
            {
                var certificateService = scope.Resolve<CertificateService>();
                var storePlugin = scope.Resolve<IStorePlugin>();
                var oldCertificate = renewal.Certificate(storePlugin);
                var newCertificate = certificateService.RequestCertificate(renewal.Binding);
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
                        var installInstance = scope.Resolve<IInstallationPlugin>();
                        installInstance.Install(newCertificate, oldCertificate);
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
                    var taskScheduler = _container.Resolve<TaskSchedulerService>();
                    taskScheduler.EnsureTaskScheduler();
                    _renewalService.Save(renewal, result);
                }
                return result;
            }
            catch (Exception ex)
            {
                // Result might still contain the Thumbprint of the certificate 
                // that was requested and (partially? installed, which might help
                // with debugging
                HandleException(ex);
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
        private static void CheckRenewals()
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
            catch (Exception ex)
            {
                HandleException(ex);
                _log.Error("Renewal for {host} failed, will retry on next run", renewal.Binding.Host);
            }
        }

        /// <summary>
        /// Make sure we have authorization for every host in target
        /// </summary>
        /// <param name="renewal"></param>
        /// <returns></returns>
        private static AuthorizationState Authorize(ILifetimeScope scope, ScheduledRenewal renewal)
        {
            List<string> identifiers = renewal.Binding.GetHosts(false);
            List<AuthorizationState> authStatus = new List<AuthorizationState>();
            var client = scope.Resolve<LetsEncryptClient>();
            foreach (var identifier in identifiers)
            {
                var authzState = client.Acme.AuthorizeIdentifier(identifier);
                if (authzState.Status == "valid" && !_options.Test)
                {
                    _log.Information("Cached authorization result: {Status}", authzState.Status);
                    authStatus.Add(authzState);
                }
                else
                {
                    IValidationPluginFactory validationPluginFactory = null;
                    IValidationPlugin validationPlugin = null;
                    try
                    {
                        validationPluginFactory = scope.Resolve<IValidationPluginFactory>();
                        validationPlugin = scope.Resolve<IValidationPlugin>();
                    }
                    catch { }
                    if (validationPluginFactory == null || validationPluginFactory is INull || validationPlugin == null)
                    {
                        return new AuthorizationState { Status = "invalid" };
                    }

                    _log.Information("Authorizing {dnsIdentifier} using {challengeType} validation ({name})", identifier, validationPluginFactory.ChallengeType, validationPluginFactory.Name);
                    var challenge = client.Acme.DecodeChallenge(authzState, validationPluginFactory.ChallengeType);
                    var cleanUp = validationPlugin.PrepareChallenge(renewal, challenge, identifier);
                    try
                    {
                        _log.Debug("Submitting answer");
                        authzState.Challenges = new AuthorizeChallenge[] { challenge };
                        client.Acme.SubmitChallengeAnswer(authzState, validationPluginFactory.ChallengeType, true);

                        // have to loop to wait for server to stop being pending.
                        // TODO: put timeout/retry limit in this loop
                        while (authzState.Status == "pending")
                        {
                            _log.Debug("Refreshing authorization");
                            Thread.Sleep(4000); // this has to be here to give ACME server a chance to think
                            var newAuthzState = client.Acme.RefreshIdentifierAuthorization(authzState);
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