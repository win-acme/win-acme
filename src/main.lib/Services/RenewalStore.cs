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
    internal abstract class RenewalStore : IRenewalStore
    {
        internal ISettingsService _settings;
        internal ILogService _log;
        internal IPluginService _plugin;
        internal ICertificateService _certificateService;
        internal IInputService _inputService;
        internal IDueDateService _dueDateService;
        internal PasswordGenerator _passwordGenerator;

        public RenewalStore(
            ISettingsService settings,
            ILogService log,
            IInputService input,
            PasswordGenerator password,
            IPluginService plugin,
            IDueDateService dueDateService,
            ICertificateService certificateService)
        {
            _log = log;
            _plugin = plugin;
            _inputService = input;
            _passwordGenerator = password;
            _settings = settings;
            _certificateService = certificateService;
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
            WriteRenewals(renewals);
        }

        public IEnumerable<Renewal> Renewals
        {
            get => ReadRenewals();
            private set => WriteRenewals(value);
        }

        /// <summary>
        /// Cancel specific renewal
        /// </summary>
        /// <param name="renewal"></param>
        public void Cancel(Renewal renewal)
        {
            renewal.Deleted = true;
            Renewals = Renewals;
            _log.Warning("Renewal {target} cancelled", renewal);
            _certificateService.Delete(renewal);
        }

        /// <summary>
        /// Cancel everything
        /// </summary>
        public void Clear()
        {
            var renewals = Renewals;
            renewals.All(x => x.Deleted = true);
            Renewals = renewals;
            _log.Warning("All renewals cancelled");
        }

        /// <summary>
        /// Parse renewals from store
        /// </summary>
        protected abstract IEnumerable<Renewal> ReadRenewals();

        /// <summary>
        /// Serialize renewal information to store
        /// </summary>
        /// <param name="BaseUri"></param>
        /// <param name="Renewals"></param>
        protected abstract void WriteRenewals(IEnumerable<Renewal> Renewals);
    }

}