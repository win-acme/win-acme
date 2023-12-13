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
        private readonly INotificationTarget _target;

        public NotificationService(
            ILogService log,
            ISettingsService setttings,
            INotificationTarget target)
        {
            _log = log;
            _target = target;
            _settings = setttings;
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
                await _target.SendCreated(renewal, log);
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
                await _target.SendSuccess(renewal, log);
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
                await _target.SendFailure(renewal, log, errors);
            }
        }

        /// <summary>
        /// Handle failure notification
        /// </summary>
        /// <param name="runLevel"></param>
        /// <param name="renewal"></param>
        internal async Task NotifyTest()
        {
            await _target.SendTest();
        }
    }
}