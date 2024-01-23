using Autofac;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Services.Interfaces;
using Serilog.Events;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS.Services
{
    internal class NotificationService
    {
        private readonly ILogService _log;
        private readonly ISettingsService _settings;
        private readonly IEnumerable<INotificationTarget> _targets;

        public NotificationService(
            ILifetimeScope scope,
            ILogService log,
            IPluginService pluginService,
            ISettingsService setttings)
        {
            _log = log;
            _settings = setttings;
            _targets = pluginService.
                GetNotificationTargets().
                Select(b => scope.Resolve(b.Backend)).
                OfType<INotificationTarget>().
                ToList();
        }

        /// <summary>
        /// Handle created notification
        /// </summary>
        /// <param name="runLevel"></param>
        /// <param name="renewal"></param>
        internal async Task NotifyCreated(Renewal renewal, IEnumerable<MemoryEntry> log)
        {
            _log.Information(
                LogType.All, 
                "Certificate {friendlyName} created", 
                renewal.LastFriendlyName);
            if (_settings.Notification.EmailOnSuccess)
            {
                foreach (var target in _targets) {
                    try
                    {
                        await target.SendCreated(renewal, log);
                    } 
                    catch 
                    {
                        _log.Error("Unable to send notification using {n}", target.GetType().Name);
                    }
                }
            }
        }

        /// <summary>
        /// Handle success notification
        /// </summary>
        /// <param name="runLevel"></param>
        /// <param name="renewal"></param>
        internal async Task NotifySuccess(Renewal renewal, IEnumerable<MemoryEntry> log)
        {
            var withErrors = log.Any(l => l.Level == LogEventLevel.Error);
            _log.Information(
                LogType.All, 
                "Renewal for {friendlyName} succeeded" + (withErrors ? " with errors" : ""),
                renewal.LastFriendlyName);
            if (withErrors || _settings.Notification.EmailOnSuccess)
            {
                foreach (var target in _targets)
                {
                    try
                    {
                        await target.SendSuccess(renewal, log);
                    }
                    catch
                    {
                        _log.Error("Unable to send notification using {n}", target.GetType().Name);
                    }
                }
            }
        }

        /// <summary>
        /// Handle failure notification
        /// </summary>
        /// <param name="runLevel"></param>
        /// <param name="renewal"></param>
        internal async Task NotifyFailure(
            RunLevel runLevel, 
            Renewal renewal, 
            RenewResult result,
            IEnumerable<MemoryEntry> log)
        {
            _log.Error("Renewal for {friendlyName} failed, will retry on next run", renewal.LastFriendlyName);
            var errors = result.ErrorMessages?.ToList() ?? new List<string>();
            errors.AddRange(result.OrderResults?.SelectMany(o => o.ErrorMessages ?? Enumerable.Empty<string>()) ?? Enumerable.Empty<string>());
            if (errors.Count == 0)
            {
                errors.Add("No specific error reason provided.");
            }
            errors.ForEach(e => _log.Error(e));
            
            // Do not send emails when running interactively      
            if (runLevel.HasFlag(RunLevel.Unattended))
            {
                foreach (var target in _targets)
                {
                    try
                    {
                        await target.SendFailure(renewal, log, errors);
                    }
                    catch
                    {
                        _log.Error("Unable to send notification using {n}", target.GetType().Name);
                    }
                }
            }
        }

        /// <summary>
        /// Handle failure notification
        /// </summary>
        /// <param name="runLevel"></param>
        /// <param name="renewal"></param>
        internal async Task NotifyTest()
        {
            foreach (var target in _targets)
            {
                try
                {
                    await target.SendTest();
                }
                catch
                {
                    _log.Error("Unable to send notification using {n}", target.GetType().Name);
                }
            }
        }
    }
}