using Autofac;
using LetsEncrypt.ACME.Simple.Extensions;
using LetsEncrypt.ACME.Simple.Services.RenewalStore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LetsEncrypt.ACME.Simple.Services
{
    class RenewalService
    {
        private ILogService _log;
        private IOptionsService _optionsService;
        private SettingsService _settings;
        private IRenewalStoreService _renewalStore;

        public RenewalService(SettingsService settings, IInputService input, 
            IOptionsService options, ILogService log,  string clientName)
        {
            _log = log;
            _optionsService = options;
            _settings = settings;
            _renewalStore = new RegistryRenewalStore(log, options, settings);
            _log.Debug("Renewal period: {RenewalDays} days", _settings.RenewalDays);
        }
  
        public ScheduledRenewal Find(Target target)
        {
            return _renewalStore.Renewals.Where(r => string.Equals(r.Binding.Host, target.Host)).FirstOrDefault();
        }

        public void Save(ScheduledRenewal renewal, RenewResult result)
        {
            var renewals = _renewalStore.Renewals.ToList();
            if (renewal.New)
            {
                renewal.History = new List<RenewResult>();
                renewals.Add(renewal);
                renewal.New = false;
                _log.Information(true, "Adding renewal for {target}", renewal.Binding.Host);

            }
            else if (result.Success)
            {
                _log.Information(true, "Renewal for {host} succeeded", renewal.Binding.Host);
            }
            else
            {
                _log.Error("Renewal for {host} failed, will retry on next run", renewal.Binding.Host);
            }

            // Set next date
            if (result.Success)
            {
                renewal.Date = DateTime.UtcNow.AddDays(_settings.RenewalDays);
                _log.Information(true, "Next renewal scheduled at {date}", renewal.Date.ToUserString());
            }
            renewal.Updated = true;
            renewal.History.Add(result);
            _renewalStore.Renewals = renewals;
        }

        public IEnumerable<ScheduledRenewal> Renewals {
            get {
                return _renewalStore.Renewals;
            } set {
                _renewalStore.Renewals = Renewals;
            }
        }

        internal void Cancel(ScheduledRenewal renewal)
        {
            _renewalStore.Renewals = _renewalStore.Renewals.Except(new[] { renewal });
            _log.Warning("Renewal {target} cancelled", renewal);
        }
    }
}
