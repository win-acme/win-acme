using Autofac;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Legacy;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PKISharp.WACS
{
    internal partial class Wacs
    {
        /// <summary>
        /// Main user experience
        /// </summary>
        private void MainMenu()
        {
            var options = new List<Choice<Action>>
            {
                Choice.Create<Action>(() => CreateNewCertificate(RunLevel.Interactive | RunLevel.Simple), "Create new certificate", "N"),
                Choice.Create<Action>(() => CreateNewCertificate(RunLevel.Interactive | RunLevel.Advanced), "Create new certificate with advanced options", "M"),
                Choice.Create<Action>(() => ShowRenewals(), "List scheduled renewals", "L"),
                Choice.Create<Action>(() => CheckRenewals(RunLevel.Interactive), "Renew scheduled", "R"),
                Choice.Create<Action>(() => RenewSpecific(), "Renew specific", "S"),
                Choice.Create<Action>(() => CheckRenewals(RunLevel.Interactive | RunLevel.Force), "Renew *all*", "A"),
                //Choice.Create<Action>(() => RevokeCertificate(), "Revoke certificate", "V"),
                Choice.Create<Action>(() => ExtraMenu(), "More options...", "O"),
                Choice.Create<Action>(() => { _arguments.CloseOnFinish = true; _arguments.Test = false; }, "Quit", "Q")
            };
            // Simple mode not available without IIS installed, because
            // it defaults to the IIS installer
            if (!_container.Resolve<IIISClient>().HasWebSites)
            {
                options.RemoveAt(0);
            }
            _input.ChooseFromList("Please choose from the menu", options, false).Invoke();
        }

        /// <summary>
        /// Less common options
        /// </summary>
        private void ExtraMenu()
        {
            var options = new List<Choice<Action>>
            {
                Choice.Create<Action>(() => CancelRenewal(RunLevel.Interactive), "Cancel scheduled renewal", "C"),
                Choice.Create<Action>(() => CancelAllRenewals(), "Cancel *all* scheduled renewals", "X"),
                Choice.Create<Action>(() => CreateScheduledTask(), "(Re)create scheduled task", "T"),
                Choice.Create<Action>(() => TestEmail(), "Test email notification", "E"),
                Choice.Create<Action>(() => Import(RunLevel.Interactive), "Import scheduled renewals from WACS/LEWS 1.9.x", "I"),
                Choice.Create<Action>(() => { }, "Back", "Q")
            };
            // Simple mode not available without IIS installed, because
            // it defaults to the IIS installer
            if (!_container.Resolve<IIISClient>().HasWebSites)
            {
                options.RemoveAt(0);
            }
            _input.ChooseFromList("Please choose from the menu", options, false).Invoke();
        }

        /// <summary>
        /// Show certificate details
        /// </summary>
        private void ShowRenewals()
        {
            var renewal = _input.ChooseFromList("Show details for renewal?",
                _renewalService.Renewals.OrderBy(x => x.Date),
                x => Choice.Create(x, color: x.History.Last().Success ? 
                                                x.Date < DateTime.Now ? 
                                                    ConsoleColor.DarkYellow : 
                                                    ConsoleColor.Green : 
                                                ConsoleColor.Red),
                true);
            if (renewal != null)
            {
                try
                {
                    _input.Show("Renewal");
                    _input.Show("Id", renewal.Id);
                    _input.Show("FriendlyName", renewal.FriendlyName);
                    _input.Show(".pfx password", renewal.PfxPassword);
                    _input.Show("Renewal due", renewal.Date.ToUserString());
                    _input.Show("Renewed", $"{renewal.History.Count} times");
                    renewal.TargetPluginOptions.Show(_input);
                    renewal.ValidationPluginOptions.Show(_input);
                    renewal.CsrPluginOptions.Show(_input);
                    renewal.StorePluginOptions.Show(_input);
                    foreach (var ipo in renewal.InstallationPluginOptions)
                    {
                        ipo.Show(_input);
                    }
                    _input.Show("History");
                    _input.WritePagedList(renewal.History.Select(x => Choice.Create(x)));
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Unable to list details for target");
                }
            }
        }

        /// <summary>
        /// Renew specific certificate
        /// </summary>
        private void RenewSpecific()
        {
            var renewal = _input.ChooseFromList("Which renewal would you like to run?",
                _renewalService.Renewals.OrderBy(x => x.Date),
                x => Choice.Create(x),
                true);
            if (renewal != null)
            {
                ProcessRenewal(renewal, RunLevel.Interactive | RunLevel.Force);
            }
        }

        /// <summary>
        /// Revoke certificate
        /// </summary>
        private void RevokeCertificate()
        {
            var renewal = _input.ChooseFromList("Which certificate would you like to revoke?",
                _renewalService.Renewals.OrderBy(x => x.Date),
                x => Choice.Create(x),
                true);
            if (renewal != null)
            {
                if (_input.PromptYesNo($"Are you sure you want to revoke the most recently issued certificate for {renewal}?"))
                {
                    using (var scope = _scopeBuilder.Configuration(_container, RunLevel.Unattended))
                    {
                        var cs = scope.Resolve<CertificateService>();
                        try
                        {
                            cs.RevokeCertificate(renewal);
                            renewal.History.Add(new RenewResult("Certificate revoked"));
                        }
                        catch (Exception ex)
                        {
                            HandleException(ex);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Cancel all renewals
        /// </summary>
        private void CancelAllRenewals()
        {
            _input.WritePagedList(_renewalService.Renewals.Select(x => Choice.Create(x)));
            if (_input.PromptYesNo("Are you sure you want to delete all of these?"))
            {
                _renewalService.Clear();
                _log.Warning("All scheduled renewals cancelled at user request");
            }
        }

        /// <summary>
        /// Cancel all renewals
        /// </summary>
        private void CreateScheduledTask()
        {
            using (var scope = _scopeBuilder.Execution(_container, null, RunLevel.Interactive | RunLevel.Advanced))
            {
                var taskScheduler = scope.Resolve<TaskSchedulerService>();
                taskScheduler.EnsureTaskScheduler();
            }
        }

        /// <summary>
        /// Test notification email
        /// </summary>
        private void TestEmail()
        {
            if (!_email.Enabled)
            {
                _log.Error("Email notifications not enabled. Input an SMTP server, sender and receiver in settings.config to enable this.");
            } else
            {
                _log.Information("Sending test message...");
                _email.Send("Test notification", "If you are reading this, it means you will receive notifications about critical errors in the future.");
                _log.Information("Test message sent!");
            }
        }

        /// <summary>
        /// Cancel all renewals
        /// </summary>
        private void Import(RunLevel runLevel)
        {
            var importUri = _arguments.GetBaseUri(true);
            if (runLevel.HasFlag(RunLevel.Interactive))
            {
                var alt = _input.RequestString($"Importing renewals for {importUri}, enter to accept or type an alternative");
                if (!string.IsNullOrEmpty(alt))
                {
                    importUri = alt;
                }
            }
            using (var scope = _scopeBuilder.Legacy(_container, importUri, _arguments.GetBaseUri()))
            {
                var importer = scope.Resolve<Importer>();
                importer.Import();
            }
        }
    }
}
