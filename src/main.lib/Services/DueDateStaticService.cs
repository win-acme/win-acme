using PKISharp.WACS.Context;
using PKISharp.WACS.DomainObjects;
using System;
using System.Linq;

namespace PKISharp.WACS.Services
{
    internal class DueDateStaticService : IDueDateService
    {
        protected const int DefaultMinValidDays = 7;
        protected readonly ISettingsService _settings;
        protected readonly ILogService _log;

        public DueDateStaticService(
            ISettingsService settings,
            ILogService logService)
        {
            _settings = settings;
            _log = logService;
        }

        public DateTime? DueDate(Renewal renewal)
        {
            var lastSuccess = renewal.History.LastOrDefault(x => x.Success == true);
            if (lastSuccess != null)
            {
                var firstOccurance = renewal.History.First(x => x.ThumbprintSummary == lastSuccess.ThumbprintSummary);
                var defaultDueDate = firstOccurance.
                    Date.
                    AddDays(_settings.ScheduledTask.RenewalDays).
                    ToLocalTime();
                if (lastSuccess.ExpireDate == null)
                {
                    return defaultDueDate;
                }
                var minDays = _settings.ScheduledTask.RenewalMinimumValidDays ?? DefaultMinValidDays;
                var expireBasedDueDate = lastSuccess.
                    ExpireDate.
                    Value.
                    AddDays(minDays * -1).
                    ToLocalTime();

                return expireBasedDueDate < defaultDueDate ?
                    expireBasedDueDate :
                    defaultDueDate;
            }
            else
            {
                return null;
            }
        }

        public virtual bool IsDue(Renewal renewal)
        {
            var dueDate = DueDate(renewal);
            return dueDate == null || dueDate < DateTime.Now;
        }

        public virtual bool ShouldRun(OrderContext order)
        {
            var renewalDue = IsDue(order.Renewal);
            if (renewalDue)
            {
                return true;
            }
            if (order.CachedCertificate == null)
            {
                _log.Information(LogType.All, "Renewal {renewal} running prematurely due to source change in order {order}", order.Renewal.LastFriendlyName, order.OrderName);
                return true;
            }
            return false;
        }
    }
}
