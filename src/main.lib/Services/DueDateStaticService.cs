using PKISharp.WACS.DomainObjects;
using System;
using System.Linq;

namespace PKISharp.WACS.Services
{
    internal class DueDateStaticService : IDueDateService
    {
        private readonly ISettingsService _settings;
        private readonly ICertificateService _certificateService;
        private readonly ILogService _logService;

        public DueDateStaticService(
            ISettingsService settings,
            ICertificateService certificateService,
            ILogService logService)
        {
            _settings = settings;
            _certificateService = certificateService;
            _logService = logService;
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

        public bool IsDue(Renewal renewal)
        {
            var dueDate = DueDate(renewal);
            return dueDate == null || dueDate < DateTime.Now;
        }

        public bool IsDue(Order order)
        {
            if (_certificateService.CachedInfo(order) == null)
            {
                _logService.Information(LogType.All, "Renewal {renewal} running prematurely due to source change in order {order}", order.Renewal.LastFriendlyName, order.FriendlyNamePart);
                return true;
            }
            return IsDue(order.Renewal);
        }
    }
}
