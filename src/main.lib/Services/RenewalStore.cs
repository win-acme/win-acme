using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PKISharp.WACS.Services
{
    /// <summary>
    /// Manage the collection of renewals. The actual 
    /// implementations handle persistance of the objects
    /// </summary>
    internal class RenewalStore : IRenewalStore
    {
        internal ISettingsService _settings;
        internal ILogService _log;
        internal IInputService _inputService;
        internal IDueDateService _dueDateService;
        internal IRenewalStoreBackend _backend;

        public RenewalStore(
            IRenewalStoreBackend backend,
            ISettingsService settings,
            ILogService log,
            IInputService input,
            IDueDateService dueDateService)
        {
            _backend = backend;
            _log = log;
            _inputService = input;
            _settings = settings;
            _dueDateService = dueDateService;
            _log.Debug("Renewal period: {RenewalDays} days", _settings.ScheduledTask.RenewalDays);
        }

        public IEnumerable<Renewal> FindByArguments(string? id, string? friendlyName)
        {
            // AND filtering by input parameters
            var ret = Renewals;
            if (!string.IsNullOrEmpty(friendlyName))
            {
                var regex = new Regex(friendlyName.ToLower().PatternToRegex());
                ret = ret.Where(x => !string.IsNullOrEmpty(x.LastFriendlyName) && regex.IsMatch(x.LastFriendlyName.ToLower()));
            }
            if (!string.IsNullOrEmpty(id))
            {
                ret = ret.Where(x => string.Equals(id, x.Id, StringComparison.InvariantCultureIgnoreCase));
            }
            return ret;
        }

        public void Save(Renewal renewal, RenewResult result)
        {
            var renewals = Renewals.ToList();
            if (renewal.New)
            {
                renewal.History = new List<RenewResult>();
                renewals.Add(renewal);
                _log.Information(LogType.All, "Adding renewal for {friendlyName}", renewal.LastFriendlyName);
            }

            // Set next date
            renewal.History.Add(result);
            if (result.Success == true)
            {
                var date = _dueDateService.DueDate(renewal);
                if (date != null)
                {
                    _log.Information(LogType.All, "Next renewal due at {date}", _inputService.FormatDate(date.Value));
                }
            }
            renewal.Updated = true;
            Renewals = renewals;
        }

        public void Import(Renewal renewal)
        {
            var renewals = Renewals.ToList();
            renewals.Add(renewal);
            _log.Information(LogType.All, "Importing renewal for {friendlyName}", renewal.LastFriendlyName);
            Renewals = renewals;
        }

        public void Encrypt()
        {
            _log.Information("Updating files in: {settings}", _settings.Client.ConfigurationPath);
            var renewals = Renewals.ToList();
            foreach (var r in renewals)
            {
                r.Updated = true;
                _log.Information("Re-writing password information for {friendlyName}", r.LastFriendlyName);
            }
            _backend.Write(renewals);
        }

        public IEnumerable<Renewal> Renewals
        {
            get => _backend.Read();
            private set => _backend.Write(value);
        }

        /// <summary>
        /// Cancel specific renewal
        /// </summary>
        /// <param name="renewal"></param>
        public void Cancel(Renewal renewal)
        {
            renewal.Deleted = true;
            var renewals = Renewals;
            Renewals = renewals;
            _log.Warning("Renewal {target} cancelled", renewal);
        }

        /// <summary>
        /// Cancel everything
        /// </summary>
        public void Clear()
        {
            var renewals = Renewals;
            _ = renewals.All(x => x.Deleted = true);
            Renewals = renewals;
            _log.Warning("All renewals cancelled");
        }
    }

}