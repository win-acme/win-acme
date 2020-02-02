using Autofac;
using PKISharp.WACS.Configuration;
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
        private readonly IArgumentsService _arguments;
        private readonly MainArguments _args;
        private readonly IContainer _container;
        private readonly IAutofacBuilder _scopeBuilder;
        private readonly ExceptionHandler _exceptionHandler;
        private readonly RenewalExecutor _renewalExecutor;

        public RenewalManager(
            IArgumentsService arguments, MainArguments args,
            IRenewalStore renewalStore, IContainer container,
            IInputService input, ILogService log, 
            IAutofacBuilder autofacBuilder, ExceptionHandler exceptionHandler,
            RenewalExecutor renewalExecutor)
        {
            _renewalStore = renewalStore;
            _args = args;
            _input = input;
            _log = log;
            _arguments = arguments;
            _container = container;
            _scopeBuilder = autofacBuilder;
            _exceptionHandler = exceptionHandler;
            _renewalExecutor = renewalExecutor;
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
            do
            {
                var all = selectedRenewals.Count() == originalSelection.Count();
                var none = selectedRenewals.Count() == 0;
                var totalLabel = originalSelection.Count() != 1 ? "renewals" : "renewal";
                var selectionLabel = 
                    all ? $"*all* renewals" : 
                    none ? "no renewals" :  
                    $"{selectedRenewals.Count()} of {originalSelection.Count()} {totalLabel}";
                var renewalSelectedLabel = selectedRenewals.Count() != 1 ? "renewals" : "renewal";

                await _input.WritePagedList(
                              selectedRenewals.Select(x => Choice.Create<Renewal?>(x,
                                  description: x.ToString(_input),
                                  color: x.History.Last().Success ?
                                          x.IsDue() ?
                                              ConsoleColor.DarkYellow :
                                              ConsoleColor.Green :
                                          ConsoleColor.Red)));
                
                var options = new List<Choice<Func<Task>>>();
                options.Add(
                    Choice.Create<Func<Task>>(
                        async () => selectedRenewals = await FilterRenewalsMenu(selectedRenewals),
                        all ? "Apply filter" : "Apply additional filter", "F",
                        @disabled: selectedRenewals.Count() < 2,
                        @default: !(selectedRenewals.Count() < 2)));
                options.Add(
                    Choice.Create<Func<Task>>(
                         async () => selectedRenewals = await SortRenewalsMenu(selectedRenewals),
                        "Sort renewals", "S",
                        @disabled: selectedRenewals.Count() < 2));
                options.Add(
                    Choice.Create<Func<Task>>(
                        () => { selectedRenewals = originalSelection; return Task.CompletedTask; },
                        "Reset sorting and filtering", "X",
                        @disabled: all,
                        @default: originalSelection.Count() > 0 && none));
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
                        @disabled: none));
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
                        @disabled: none));
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
                        @disabled: none));
                options.Add(
                    Choice.Create<Func<Task>>(
                        async () => {
                            var confirm = await _input.PromptYesNo($"Are you sure you want to revoke the most recently issued certificate for {selectedRenewals.Count()} currently selected {renewalSelectedLabel}? This should only be done in case of a (suspected) security breach. Cancel the {renewalSelectedLabel} if you simply don't need the certificates anymore.", false);
                            if (confirm)
                            {
                                foreach (var renewal in selectedRenewals)
                                {
                                    using var scope = _scopeBuilder.Execution(_container, renewal, RunLevel.Interactive);
                                    var cs = scope.Resolve<ICertificateService>();
                                    try
                                    {
                                        await cs.RevokeCertificate(renewal);
                                        renewal.History.Add(new RenewResult("Certificate revoked"));
                                    }
                                    catch (Exception ex)
                                    {
                                        _exceptionHandler.HandleException(ex);
                                    }
                                };
                            }
                        },
                        $"Revoke {selectionLabel}", "V",
                        @disabled: none));
                options.Add(
                    Choice.Create<Func<Task>>(
                        () => { quit = true; return Task.CompletedTask; },
                        "Back", "Q",
                        @default: originalSelection.Count() == 0));

  
                _input.Show(null, $"Currently selected {selectedRenewals.Count()} of {originalSelection.Count()} {totalLabel}", true);
                var chosen = await _input.ChooseFromMenu("Please choose from the menu", options);
                await chosen.Invoke();
            }
            while (!quit);
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
                    () => FilterRenewalsById(current),
                    "Pick from displayed list",
                    @default: true),
                Choice.Create<Func<Task<IEnumerable<Renewal>>>>(
                    () => FilterRenewalsByFriendlyName(current),
                    "Filter by friendly name"),
                Choice.Create<Func<Task<IEnumerable<Renewal>>>>(
                    () => Task.FromResult(current.Where(x => x.IsDue())),
                    "Keep only due renewals"),
                Choice.Create<Func<Task<IEnumerable<Renewal>>>>(
                    () => Task.FromResult(current.Where(x => !x.IsDue())),
                    "Remove due renewals"),
                Choice.Create<Func<Task<IEnumerable<Renewal>>>>(
                    () => Task.FromResult(current.Where(x => !x.History.Last().Success)),
                    "Keep only renewals with errors"),
                Choice.Create<Func<Task<IEnumerable<Renewal>>>>(
                    () => Task.FromResult(current.Where(x => x.History.Last().Success)),
                    "Remove renewals with errors"),
                Choice.Create<Func<Task<IEnumerable<Renewal>>>>(
                    () => Task.FromResult(current),
                    "Cancel")
            };
            var chosen = await _input.ChooseFromMenu("How would you like to filter?", options);
            return await chosen.Invoke();
        }

        /// <summary>
        /// Filter specific renewals by list index
        /// </summary>
        /// <param name="current"></param>
        /// <returns></returns>
        private async Task<IEnumerable<Renewal>> FilterRenewalsById(IEnumerable<Renewal> current)
        {
            var rawInput = await _input.RequestString("Please input the list index of the renewal(s) you'd like to select");
            var parts = rawInput.ParseCsv();
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
            _input.Show(null, "Please input friendly name to filter renewals by. " + IISArgumentsProvider.PatternExamples, true);
            var rawInput = await _input.RequestString("Friendly name");
            var ret = new List<Renewal>();
            var regex = new Regex(rawInput.PatternToRegex());
            foreach (var r in current)
            {
                if (regex.Match(r.LastFriendlyName).Success)
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
            if (_arguments.HasFilter())
            {
                var targets = _renewalStore.FindByArguments(
                    _arguments.MainArguments.Id,
                    _arguments.MainArguments.FriendlyName);
                if (targets.Count() == 0)
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
            if (_arguments.HasFilter())
            {
                renewals = _renewalStore.FindByArguments(_args.Id, _args.FriendlyName);
                if (renewals.Count() == 0)
                {
                    _log.Error("No renewals found that match the filter parameters --id and/or --friendlyname.");
                }
            }
            else
            {
                _log.Verbose("Checking renewals");
                renewals = _renewalStore.Renewals;
                if (renewals.Count() == 0)
                {
                    _log.Warning("No scheduled renewals found.");
                }
            }

            if (renewals.Count() > 0)
            {
                WarnAboutRenewalArguments();
                foreach (var renewal in renewals)
                {
                    await ProcessRenewal(renewal, runLevel);
                }
            }
        }

        /// <summary>
        /// Process a single renewal
        /// </summary>
        /// <param name="renewal"></param>
        internal async Task ProcessRenewal(Renewal renewal, RunLevel runLevel)
        {
            var notification = _container.Resolve<NotificationService>();
            try
            {
                var result = await _renewalExecutor.Execute(renewal, runLevel);
                if (result != null)
                {
                    _renewalStore.Save(renewal, result);
                    if (result.Success)
                    {
                        notification.NotifySuccess(runLevel, renewal);
                    }
                    else
                    {
                        notification.NotifyFailure(runLevel, renewal, result.ErrorMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                _exceptionHandler.HandleException(ex);
                notification.NotifyFailure(runLevel, renewal, ex.Message);
            }
        }

        /// <summary>
        /// Show a warning when the user appears to be trying to
        /// use command line arguments in combination with a renew
        /// command.
        /// </summary>
        internal void WarnAboutRenewalArguments()
        {
            if (_arguments.Active)
            {
                _log.Warning("You have specified command line options for plugins. " +
                    "Note that these only affect new certificates, but NOT existing renewals. " +
                    "To change settings, re-create (overwrite) the renewal.");
            }
        }

        /// <summary>
        /// Show certificate details
        /// </summary>
        private async Task ShowRenewal(Renewal renewal)
        {
            try
            {
                _input.Show("Id", renewal.Id, true);
                _input.Show("File", $"{renewal.Id}.renewal.json");
                _input.Show("FriendlyName", string.IsNullOrEmpty(renewal.FriendlyName) ? $"[Auto] {renewal.LastFriendlyName}" : renewal.FriendlyName);
                _input.Show(".pfx password", renewal.PfxPassword?.Value);
                _input.Show("Renewal due", renewal.GetDueDate()?.ToString() ?? "now");
                _input.Show("Renewed", $"{renewal.History.Where(x => x.Success).Count()} times");
                renewal.TargetPluginOptions.Show(_input);
                renewal.ValidationPluginOptions.Show(_input);
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
                _input.Show("History");
                await _input.WritePagedList(renewal.History.Select(x => Choice.Create(x)));
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
            foreach (var renewal in renewals)
            {
                using var scope = _scopeBuilder.Execution(_container, renewal, RunLevel.Unattended);
                var cs = scope.Resolve<ICertificateService>();
                try
                {
                    await cs.RevokeCertificate(renewal);
                    renewal.History.Add(new RenewResult("Certificate revoked"));
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