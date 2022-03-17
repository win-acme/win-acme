using PKISharp.WACS.DomainObjects;
using System;
using System.Linq;

namespace PKISharp.WACS.Services
{
    internal class DueDateStaticService : IDueDateService
    {
        private readonly ISettingsService _settings;

        public DueDateStaticService(ISettingsService settings) => _settings = settings;

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
                var minDays = _settings.ScheduledTask.RenewalMinimumValidDays ?? 7;
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

        public DateTime? DueDate(Renewal renewal, Order order) => DueDate(renewal);

        public bool IsDue(Renewal renewal)
        {
            var dueDate = DueDate(renewal);
            return dueDate == null || dueDate < DateTime.Now;
        }

        public bool IsDue(Renewal renewal, Order order) => IsDue(renewal);
    }
}
