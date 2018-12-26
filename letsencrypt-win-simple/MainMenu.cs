using Autofac;
using PKISharp.WACS.Clients;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Resolvers;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Legacy;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PKISharp.WACS
{
    internal partial class Program
    {
        /// <summary>
        /// Main user experience
        /// </summary>
        private static void MainMenu()
        {
            var options = new List<Choice<Action>>
            {
                Choice.Create<Action>(() => CreateNewCertificate(RunLevel.Simple), "Create new certificate", "N"),
                Choice.Create<Action>(() => CreateNewCertificate(RunLevel.Advanced), "Create new certificate with advanced options", "M"),
                Choice.Create<Action>(() => ShowCertificates(), "List scheduled renewals", "L"),
                Choice.Create<Action>(() => CheckRenewals(false), "Renew scheduled", "R"),
                Choice.Create<Action>(() => RenewSpecific(), "Renew specific", "S"),
                Choice.Create<Action>(() => CheckRenewals(true), "Renew *all*", "A"),
                //Choice.Create<Action>(() => RevokeCertificate(), "Revoke certificate", "V"),
                Choice.Create<Action>(() => CancelSingleRenewal(), "Cancel scheduled renewal", "C"),
                Choice.Create<Action>(() => CancelAllRenewals(), "Cancel *all* scheduled renewals", "X"),
                Choice.Create<Action>(() => CreateScheduledTask(), "(Re)create scheduled task", "T"),
                Choice.Create<Action>(() => Import(RunLevel.Simple), "Import scheduled renewals from WACS/LEWS 1.9.x", "I"),
                Choice.Create<Action>(() => { _options.CloseOnFinish = true; _options.Test = false; }, "Quit", "Q")
            };
            // Simple mode not available without IIS installed, because
            // it defaults to the IIS installer
            if (!_container.Resolve<IISClient>().HasWebSites)
            {
                options.RemoveAt(0);
            }
            _input.ChooseFromList("Please choose from the menu", options, false).Invoke();
        }

        /// <summary>
        /// Show certificate details
        /// </summary>
        private static void ShowCertificates()
        {
            var renewal = _input.ChooseFromList("Show details for renewal?",
                _renewalService.Renewals.OrderBy(x => x.Date),
                x => Choice.Create(x),
                true);
            if (renewal != null)
            {
                try
                {
                    using (var scope = AutofacBuilder.Renewal(_container, renewal, RunLevel.Unattended))
                    {
                        var resolver = scope.Resolve<UnattendedResolver>();
                        _input.Show("Name", renewal.Target.Host, true);
                        _input.Show("AlternativeNames", string.Join(", ", renewal.Target.AlternativeNames));
                        _input.Show("CommonName", renewal.Target.CommonName ?? "<not set>");
                        _input.Show("ExcludeBindings", renewal.Target.ExcludeBindings);
                        _input.Show("Target plugin", resolver.GetTargetPlugin(scope).Description);
                        _input.Show("Validation plugin", resolver.GetValidationPlugin(scope).Description);
                        _input.Show("Store plugin", resolver.GetStorePlugin(scope).Description);
                        if (!string.IsNullOrEmpty(renewal.CertificateStore))
                        {
                            _input.Show("Certificate store", renewal.CertificateStore);
                        }
                        _input.Show("Install plugin(s)", string.Join(", ", resolver.GetInstallationPlugins(scope).Select(x => x.Description)));
                        _input.Show("Renewal due", renewal.Date.ToUserString());
                        _input.Show("Script", renewal.Script);
                        _input.Show("ScriptParameters", renewal.ScriptParameters);
                        _input.Show("CentralSslStore", renewal.CentralSslStore);
                        _input.Show("KeepExisting", renewal.KeepExisting.ToString());
                        _input.Show("Warmup", renewal.Warmup.ToString());
                        _input.Show("Renewed", $"{renewal.History.Count} times");
                        _input.WritePagedList(renewal.History.Select(x => Choice.Create(x)));
                    }
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
        private static void RenewSpecific()
        {
            var target = _input.ChooseFromList("Which renewal would you like to run?",
                _renewalService.Renewals.OrderBy(x => x.Date),
                x => Choice.Create(x),
                true);
            if (target != null)
            {
                ProcessRenewal(target);
            }
        }

        /// <summary>
        /// Revoke certificate
        /// </summary>
        private static void RevokeCertificate()
        {
            var renewal = _input.ChooseFromList("Which certificate would you like to revoke?",
                _renewalService.Renewals.OrderBy(x => x.Date),
                x => Choice.Create(x),
                true);
            if (renewal != null)
            {
                if (_input.PromptYesNo($"Are you sure you want to revoke the most recently issued certificate for {renewal.Target}?"))
                {
                    using (var scope = AutofacBuilder.Renewal(_container, renewal, RunLevel.Unattended))
                    {
                        var cs = scope.Resolve<CertificateService>();
                        try
                        {
                            cs.RevokeCertificate(renewal.Target);
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
        /// Cancel a renewal
        /// </summary>
        private static void CancelSingleRenewal()
        {
            var renewal = _input.ChooseFromList("Which renewal would you like to cancel?",
                _renewalService.Renewals.OrderBy(x => x.Date),
                x => Choice.Create(x),
                true);

            if (renewal != null)
            {
                if (_input.PromptYesNo($"Are you sure you want to cancel the renewal for {renewal.Target}"))
                {
                    _renewalService.Cancel(renewal);
                }
            }
        }

        /// <summary>
        /// Cancel all renewals
        /// </summary>
        private static void CancelAllRenewals()
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
        private static void CreateScheduledTask()
        {
            using (var scope = AutofacBuilder.Renewal(_container, null, RunLevel.Advanced))
            {
                var taskScheduler = scope.Resolve<TaskSchedulerService>();
                taskScheduler.EnsureTaskScheduler();
            }
        }

        /// <summary>
        /// Cancel all renewals
        /// </summary>
        private static void Import(RunLevel runLevel)
        {
            var baseUri = _optionsService.Options.ImportBaseUri;
            if (runLevel != RunLevel.Unattended)
            {
                var alt = _input.RequestString($"Importing renewals for {baseUri}, enter to accept or type an alternative");
                if (!string.IsNullOrEmpty(alt))
                {
                    baseUri = alt;
                }
            }
            using (var scope = AutofacBuilder.Legacy(_container, baseUri))
            {
                var importer = scope.Resolve<Importer>();
                importer.Import();
            }
        }
    }
}
