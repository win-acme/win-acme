using Autofac;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.TargetPlugins;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PKISharp.WACS
{
    internal class RenewalManager
    {
        private readonly IInputService _input;
        private readonly ILogService _log;
        private readonly IRenewalStore _renewalStore;
        private readonly ArgumentsParser _arguments;
        private readonly MainArguments _args;
        private readonly IContainer _container;
        private readonly IAutofacBuilder _scopeBuilder;
        private readonly ExceptionHandler _exceptionHandler;
        private readonly RenewalExecutor _renewalExecutor;
        private readonly RenewalCreator _renewalCreator;
        private readonly ISettingsService _settings;

        public RenewalManager(
            ArgumentsParser arguments, MainArguments args,
            IRenewalStore renewalStore, IContainer container,
            IInputService input, ILogService log,
            ISettingsService settings,
            IAutofacBuilder autofacBuilder, ExceptionHandler exceptionHandler,
            RenewalCreator renewalCreator,
            RenewalExecutor renewalExecutor)
        {
            _renewalStore = renewalStore;
            _args = args;
            _input = input;
            _log = log;
            _settings = settings;
            _arguments = arguments;
            _container = container;
            _scopeBuilder = autofacBuilder;
            _exceptionHandler = exceptionHandler;
            _renewalExecutor = renewalExecutor;
            _renewalCreator = renewalCreator;
        }

        /// <summary>
        /// Renewal management mode
        /// </summary>
        /// <returns></returns>
        internal async Task ManageRenewals()
        {
            IEnumerable<Renewal> originalSelection = _renewalStore.Renewals.OrderBy(x => x.LastFriendlyName);
            var selectedRenewals = originalSelection;
            var quit = false;
            var displayAll = false;
            do
            {
                var all = selectedRenewals.Count() == originalSelection.Count();
                var none = !selectedRenewals.Any();
                var totalLabel = originalSelection.Count() != 1 ? "renewals" : "renewal";
                var renewalSelectedLabel = selectedRenewals.Count() != 1 ? "renewals" : "renewal";
                var selectionLabel = 
                    all ? selectedRenewals.Count() == 1 ? "the renewal" : "*all* renewals" : 
                    none ? "no renewals" :  
                    $"{selectedRenewals.Count()} of {originalSelection.Count()} {totalLabel}";

                _input.CreateSpace();
                _input.Show(null, 
                    "Welcome to the renewal manager. Actions selected in the menu below will " +
                    "be applied to the following list of renewals. You may filter the list to target " +
                    "your action at a more specific set of renewals, or sort it to make it easier to " +
                    "find what you're looking for.");

                var displayRenewals = selectedRenewals;
                var displayLimited = !displayAll && selectedRenewals.Count() >= _settings.UI.PageSize;
                var displayHidden = 0;
                var displayHiddenLabel = "";
                if (displayLimited)
                {
                    displayRenewals = displayRenewals.Take(_settings.UI.PageSize - 1);
                    displayHidden = selectedRenewals.Count() - displayRenewals.Count();
                    displayHiddenLabel = displayHidden != 1 ? "renewals" : "renewal";
                }
                var choices = displayRenewals.Select(x => Choice.Create<Renewal?>(x,
                                  description: x.ToString(_input),
                                  color: x.History.LastOrDefault()?.Success ?? false ?
                                          x.IsDue() ?
                                              ConsoleColor.DarkYellow :
                                              ConsoleColor.Green :
                                          ConsoleColor.Red)).ToList();
                if (displayLimited)
                {
                    choices.Add(Choice.Create<Renewal?>(null,
                                  command: "More",
                                  description: $"{displayHidden} additional {displayHiddenLabel} selected but currently not displayed"));
                }
                await _input.WritePagedList(choices);
                displayAll = false;
                
                var options = new List<Choice<Func<Task>>>();
                if (displayLimited)
                {
                    options.Add(
                        Choice.Create<Func<Task>>(
                            () => { displayAll = true; return Task.CompletedTask; },
                            "List all selected renewals", "A"));
                }
                options.Add(
                    Choice.Create<Func<Task>>(
                        async () => { quit = true; await EditRenewal(selectedRenewals.First()); },
                        "Edit renewal", "E",
                        @disabled: (selectedRenewals.Count() != 1, none ? "No renewals selected." : "Multiple renewals selected.")));
                if (selectedRenewals.Count() > 1)
                {
                    options.Add(
                        Choice.Create<Func<Task>>(
                            async () => selectedRenewals = await FilterRenewalsMenu(selectedRenewals),
                            all ? "Apply filter" : "Apply additional filter", "F",
                            @disabled: (selectedRenewals.Count() < 2, "Not enough renewals to filter.")));
                    options.Add(
                        Choice.Create<Func<Task>>(
                             async () => selectedRenewals = await SortRenewalsMenu(selectedRenewals),
                            "Sort renewals", "S",
                            @disabled: (selectedRenewals.Count() < 2, "Not enough renewals to sort.")));
                }
                if (!all)
                {
                    options.Add(
                        Choice.Create<Func<Task>>(
                            () => { selectedRenewals = originalSelection; return Task.CompletedTask; },
                            "Reset sorting and filtering", "X",
                            @disabled: (all, "No filters have been applied yet.")));
                }
                options.Add(
                    Choice.Create<Func<Task>>(
                        async () => { 
                            foreach (var renewal in selectedRenewals) {
                                var index = selectedRenewals.ToList().IndexOf(renewal) + 1;
                                _log.Information("Details for renewal {n}/{m}", index, selectedRenewals.Count());
                                await ShowRenewal(renewal);
                                var cont = false;
                                if (index != selectedRenewals.Count())
                                {
                                    cont = await _input.Wait("Press <Enter> to continue or <Esc> to abort");
                                    if (!cont)
                                    {
                                        break;
                                    }
                                } 
                                else
                                {
                                    await _input.Wait();
                                }

                            } 
                        },
                        $"Show details for {selectionLabel}", "D",
                        @disabled: (none, "No renewals selected.")));
                options.Add(
                    Choice.Create<Func<Task>>(
                        async () => {
                            WarnAboutRenewalArguments();
                            foreach (var renewal in selectedRenewals)
                            {
                                var runLevel = RunLevel.Interactive | RunLevel.ForceRenew;
                                if (_args.Force)
                                {
                                    runLevel |= RunLevel.IgnoreCache;
                                }
                                await ProcessRenewal(renewal, runLevel);
                            }
                        },
                        $"Run {selectionLabel}", "R",
                        @disabled: (none, "No renewals selected.")));
                options.Add(
                    Choice.Create<Func<Task>>(
                        async () => selectedRenewals = await Analyze(selectedRenewals),
                        $"Analyze duplicates for {selectionLabel}", "U",
                        @disabled: (none, "No renewals selected.")));
                options.Add(
                    Choice.Create<Func<Task>>(
                        async () => {
                            var confirm = await _input.PromptYesNo($"Are you sure you want to cancel {selectedRenewals.Count()} currently selected {renewalSelectedLabel}?", false);
                            if (confirm)
                            {
                                foreach (var renewal in selectedRenewals)
                                {
                                    _renewalStore.Cancel(renewal);
                                };
                                originalSelection = _renewalStore.Renewals.OrderBy(x => x.LastFriendlyName);
                                selectedRenewals = originalSelection;
                            }
                        },
                        $"Cancel {selectionLabel}", "C",
                        @disabled: (none, "No renewals selected.")));
                options.Add(
                    Choice.Create<Func<Task>>(
                        async () => {
                            var confirm = await _input.PromptYesNo($"Are you sure you want to revoke the most recently issued certificate for {selectedRenewals.Count()} currently selected {renewalSelectedLabel}? This should only be done in case of a (suspected) security breach. Cancel the {renewalSelectedLabel} if you simply don't need the certificates anymore.", false);
                            if (confirm)
                            {
                                await RevokeCertificates(selectedRenewals);
                            }
                        },
                        $"Revoke certificate(s) for {selectionLabel}", "V",
                        @disabled: (none, "No renewals selected.")));
                options.Add(
                    Choice.Create<Func<Task>>(
                        () => { quit = true; return Task.CompletedTask; },
                        "Back", "Q",
                        @default: !originalSelection.Any()));

                if (selectedRenewals.Count() > 1)
                {
                    _input.CreateSpace();
                    _input.Show(null, $"Currently selected {selectedRenewals.Count()} of {originalSelection.Count()} {totalLabel}");
                }
                var chosen = await _input.ChooseFromMenu(
                    "Choose an action or type numbers to select renewals",
                    options, 
                    (string unexpected) =>
                        Choice.Create<Func<Task>>(
                          async () => selectedRenewals = await FilterRenewalsById(selectedRenewals, unexpected)));
                await chosen.Invoke();
            }
            while (!quit);
        }

        /// <summary>
        /// Check if there are multiple renewals installing to the same site 
        /// or requesting certificates for the same domains
        /// </summary>
        /// <param name="selectedRenewals"></param>
        /// <returns></returns>
        private async Task<IEnumerable<Renewal>> Analyze(IEnumerable<Renewal> selectedRenewals)
        {
            var foundHosts = new Dictionary<Identifier, List<Renewal>>();
            var foundSites = new Dictionary<long, List<Renewal>>();

            foreach (var renewal in selectedRenewals)
            {
                using var targetScope = _scopeBuilder.Target(_container, renewal, RunLevel.Unattended);
                var target = targetScope.Resolve<Target>();
                foreach (var targetPart in target.Parts)
                {
                    if (targetPart.SiteId != null)
                    {
                        var siteId = targetPart.SiteId.Value;
                        if (!foundSites.ContainsKey(siteId))
                        {
                            foundSites.Add(siteId, new List<Renewal>());
                        }
                        foundSites[siteId].Add(renewal);
                    }
                    foreach (var host in targetPart.GetIdentifiers(true))
                    {
                        if (!foundHosts.ContainsKey(host))
                        {
                            foundHosts.Add(host, new List<Renewal>());
                        }
                        foundHosts[host].Add(renewal);
                    }
                }
            }

            // List results
            var options = new List<Choice<List<Renewal>>>();
            foreach (var site in foundSites)
            {
                if (site.Value.Count > 1)
                {
                    options.Add(
                      Choice.Create(
                          site.Value,
                          $"Select {site.Value.Count} renewals covering IIS site {site.Key}"));
                }
            }
            foreach (var host in foundHosts)
            {
                if (host.Value.Count > 1)
                {
                    options.Add(
                      Choice.Create(
                          host.Value,
                          $"Select {host.Value.Count} renewals covering host {host.Key}"));
                }
            }
            _input.CreateSpace();
            if (options.Count == 0)
            {
                _input.Show(null, "Analysis didn't find any overlap between renewals.");
                return selectedRenewals;
            }
            else
            {
                options.Add(
                    Choice.Create(
                        selectedRenewals.ToList(),
                        $"Back"));
                _input.Show(null, "Analysis found some overlap between renewals. You can select the overlapping renewals from the menu.");
                return await _input.ChooseFromMenu("Please choose from the menu", options);
            }
        }

        /// <summary>
        /// Offer user different ways to sort the renewals
        /// </summary>
        /// <param name="current"></param>
        /// <returns></returns>
        private async Task<IEnumerable<Renewal>> SortRenewalsMenu(IEnumerable<Renewal> current)
        {
            var options = new List<Choice<Func<IEnumerable<Renewal>>>>
            {
                Choice.Create<Func<IEnumerable<Renewal>>>(
                    () => current.OrderBy(x => x.LastFriendlyName),
                    "Sort by friendly name",
                    @default: true),
                Choice.Create<Func<IEnumerable<Renewal>>>(
                    () => current.OrderByDescending(x => x.LastFriendlyName),
                    "Sort by friendly name (descending)"),
                Choice.Create<Func<IEnumerable<Renewal>>>(
                    () => current.OrderBy(x => x.GetDueDate()),
                    "Sort by due date"),
                Choice.Create<Func<IEnumerable<Renewal>>>(
                    () => current.OrderByDescending(x => x.GetDueDate()),
                    "Sort by due date (descending)")
            };
            var chosen = await _input.ChooseFromMenu("How would you like to sort the renewals list?", options);
            return chosen.Invoke();
        }

        /// <summary>
        /// Offer user different ways to filter the renewals
        /// </summary>
        /// <param name="current"></param>
        /// <returns></returns>
        private async Task<IEnumerable<Renewal>> FilterRenewalsMenu(IEnumerable<Renewal> current)
        {
            var options = new List<Choice<Func<Task<IEnumerable<Renewal>>>>>
            {
                Choice.Create<Func<Task<IEnumerable<Renewal>>>>(
                    () => FilterRenewalsByFriendlyName(current),
                    "Filter by friendly name"),
                Choice.Create<Func<Task<IEnumerable<Renewal>>>>(
                    () => Task.FromResult(current.Where(x => x.IsDue())),
                    "Filter by due status (keep due)"),
                Choice.Create<Func<Task<IEnumerable<Renewal>>>>(
                    () => Task.FromResult(current.Where(x => !x.IsDue())),
                    "Filter by due status (remove due)"),
                Choice.Create<Func<Task<IEnumerable<Renewal>>>>(
                    () => Task.FromResult(current.Where(x => !x.History.Last().Success)),
                    "Filter by error status (keep errors)"),
                Choice.Create<Func<Task<IEnumerable<Renewal>>>>(
                    () => Task.FromResult(current.Where(x => x.History.Last().Success)),
                    "Filter by error status (remove errors)"),
                Choice.Create<Func<Task<IEnumerable<Renewal>>>>(
                    () => Task.FromResult(current),
                    "Cancel")
            };
            var chosen = await _input.ChooseFromMenu("How would you like to filter?", options);
            return await chosen.Invoke();
        }

        private async Task<IEnumerable<Renewal>> FilterRenewalsById(IEnumerable<Renewal> current, string input)
        {
            var parts = input.ParseCsv();
            if (parts == null)
            {
                return current;
            }
            var ret = new List<Renewal>();
            foreach (var part in parts)
            {
                if (int.TryParse(part, out var index))
                {
                    if (index > 0 && index <= current.Count())
                    {
                        ret.Add(current.ElementAt(index - 1));
                    }
                    else
                    {
                        _log.Warning("Input out of range: {part}", part);
                    }
                }
                else
                {
                    _log.Warning("Invalid input: {part}", part);
                }
            }
            return ret;
        }

        /// <summary>
        /// Filter specific renewals by friendly name
        /// </summary>
        /// <param name="current"></param>
        /// <returns></returns>
        private async Task<IEnumerable<Renewal>> FilterRenewalsByFriendlyName(IEnumerable<Renewal> current)
        {
            _input.CreateSpace();
            _input.Show(null, "Please input friendly name to filter renewals by. " + IISArguments.PatternExamples);
            var rawInput = await _input.RequestString("Friendly name");
            var ret = new List<Renewal>();
            var regex = new Regex(rawInput.PatternToRegex(), RegexOptions.IgnoreCase);
            foreach (var r in current)
            {
                if (!string.IsNullOrEmpty(r.LastFriendlyName) && regex.Match(r.LastFriendlyName).Success)
                {
                    ret.Add(r);
                }
            }
            return ret;
        }

        /// <summary>
        /// Filters for unattended mode
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        private async Task<IEnumerable<Renewal>> FilterRenewalsByCommandLine(string command)
        {
            if (_args.HasFilter)
            {
                var targets = _renewalStore.FindByArguments(
                    _args.Id,
                    _args.FriendlyName);
                if (!targets.Any())
                {
                    _log.Error("No renewals matched.");
                }
                return targets;
            }
            else
            {
                _log.Error($"Specify which renewal to {command} using the parameter --id or --friendlyname.");
            }
            return new List<Renewal>();
        }

        /// <summary>
        /// Loop through the store renewals and run those which are
        /// due to be run
        /// </summary>
        internal async Task CheckRenewals(RunLevel runLevel)
        {
            IEnumerable<Renewal> renewals;
            if (_args.HasFilter)
            {
                renewals = _renewalStore.FindByArguments(_args.Id, _args.FriendlyName);
                if (!renewals.Any())
                {
                    _log.Error("No renewals found that match the filter parameters --id and/or --friendlyname.");
                }
            }
            else
            {
                _log.Verbose("Checking renewals");
                renewals = _renewalStore.Renewals;
                if (!renewals.Any())
                {
                    _log.Warning("No scheduled renewals found.");
                }
            }

            if (renewals.Any())
            {
                WarnAboutRenewalArguments();
                foreach (var renewal in renewals)
                {
                    try
                    {
                        var success = await ProcessRenewal(renewal, runLevel);
                        if (success == false)
                        {
                            // Make sure the ExitCode is set
                            _exceptionHandler.HandleException();
                        }
                    } 
                    catch (Exception ex)
                    {
                        _exceptionHandler.HandleException(ex, "Unhandled error processing renewal");
                        continue;
                    }
                }
            }
        }

        /// <summary>
        /// Process a single renewal
        /// </summary>
        /// <param name="renewal"></param>
        internal async Task<bool?> ProcessRenewal(Renewal renewal, RunLevel runLevel)
        {
            var notification = _container.Resolve<NotificationService>();
            try
            {
                var result = await _renewalExecutor.HandleRenewal(renewal, runLevel);
                if (!result.Abort)
                {
                    _renewalStore.Save(renewal, result);
                    if (result.Success)
                    {
                        await notification.NotifySuccess(renewal, _log.Lines);
                        return true;
                    }
                    else
                    {
                        await notification.NotifyFailure(runLevel, renewal, result.ErrorMessages, _log.Lines);
                        return false;
                    }
                } 
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                _exceptionHandler.HandleException(ex);
                await notification.NotifyFailure(runLevel, renewal, new List<string> { ex.Message }, _log.Lines);
                return false;
            }
        }

        /// <summary>
        /// Show a warning when the user appears to be trying to
        /// use command line arguments in combination with a renew
        /// command.
        /// </summary>
        internal void WarnAboutRenewalArguments()
        {
            if (_arguments.Active())
            {
                _log.Warning("You have specified command line options for plugins. " +
                    "Note that these only affect new certificates, but NOT existing renewals. " +
                    "To change settings, re-create (overwrite) the renewal.");
            }
        }

        /// <summary>
        /// "Edit" renewal
        /// </summary>
        private async Task EditRenewal(Renewal renewal) => 
            await _renewalCreator.SetupRenewal(RunLevel.Interactive | RunLevel.Advanced, renewal);

        /// <summary>
        /// Show certificate details
        /// </summary>
        private async Task ShowRenewal(Renewal renewal)
        {
            try
            {
                _input.CreateSpace();
                _input.Show("Id", renewal.Id);
                _input.Show("File", $"{renewal.Id}.renewal.json");
                _input.Show("FriendlyName", string.IsNullOrEmpty(renewal.FriendlyName) ? $"[Auto] {renewal.LastFriendlyName}" : renewal.FriendlyName);
                _input.Show(".pfx password", renewal.PfxPassword?.Value);
                _input.Show("Renewal due", renewal.GetDueDate()?.ToString() ?? "now");
                _input.Show("Renewed", $"{renewal.History.Where(x => x.Success).Count()} times");
                renewal.TargetPluginOptions.Show(_input);
                renewal.ValidationPluginOptions.Show(_input);
                if (renewal.OrderPluginOptions != null)
                {
                    renewal.OrderPluginOptions.Show(_input);
                }
                if (renewal.CsrPluginOptions != null)
                {
                    renewal.CsrPluginOptions.Show(_input);
                }
                foreach (var ipo in renewal.StorePluginOptions)
                {
                    ipo.Show(_input);
                }
                foreach (var ipo in renewal.InstallationPluginOptions)
                {
                    ipo.Show(_input);
                }
                var historyLimit = 10;
                if (renewal.History.Count <= historyLimit)
                {
                    _input.Show("History");
                }
                else
                {
                    _input.Show($"History (most recent {historyLimit} of {renewal.History.Count} entries)");
                   
                }
                await _input.WritePagedList(
                    renewal.History.
                    AsEnumerable().
                    Reverse().
                    Take(historyLimit).
                    Reverse().
                    Select(x => Choice.Create(x)));
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Unable to list details for target");
            }
        }

        #region  Unattended 

        /// <summary>
        /// For command line --list
        /// </summary>
        /// <returns></returns>
        internal async Task ShowRenewalsUnattended()
        {
            await _input.WritePagedList(
                 _renewalStore.Renewals.Select(x => Choice.Create<Renewal?>(x,
                    description: x.ToString(_input),
                    color: x.History.Last().Success ?
                            x.IsDue() ?
                                ConsoleColor.DarkYellow :
                                ConsoleColor.Green :
                            ConsoleColor.Red)));
        }

        /// <summary>
        /// Cancel certificate from the command line
        /// </summary>
        /// <returns></returns>
        internal async Task CancelRenewalsUnattended()
        {
            var targets = await FilterRenewalsByCommandLine("cancel");
            foreach (var t in targets)
            {
                _renewalStore.Cancel(t);
            }
        }

        /// <summary>
        /// Revoke certifcate from the command line
        /// </summary>
        /// <returns></returns>
        internal async Task RevokeCertificatesUnattended()
        {
            _log.Warning($"Certificates should only be revoked in case of a (suspected) security breach. Cancel the renewal if you simply don't need the certificate anymore.");
            var renewals = await FilterRenewalsByCommandLine("revoke");
            await RevokeCertificates(renewals);
        }

        /// <summary>
        /// Shared code for command line and renewal manager
        /// </summary>
        /// <param name="renewals"></param>
        /// <returns></returns>
        internal async Task RevokeCertificates(IEnumerable<Renewal> renewals)
        {
            foreach (var renewal in renewals)
            {
                using var scope = _scopeBuilder.Execution(_container, renewal, RunLevel.Unattended);
                var cs = scope.Resolve<ICertificateService>();
                try
                {
                    await cs.RevokeCertificate(renewal);
                    renewal.History.Add(new RenewResult("Certificate(s) revoked"));
                }
                catch (Exception ex)
                {
                    _exceptionHandler.HandleException(ex);
                }
            }
        }

        #endregion

    }
}
